using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace M62Logic
{
	public class AlsnSystem : MonoBehaviour
	{
		private bool isInitialized;
		private float lastUpdate;
		private TrainCar car;
		private bool lastRb;
		private float lastEpkKey;
		private int stableLampCode = AlsnLampCode.Green;
		private int pendingLampCode = AlsnLampCode.Green;
		private float pendingLampTimer;
		private readonly EpkLogic epkLogic = new EpkLogic();
		private Dictionary<string, FastPort> fPorts = new Dictionary<string, FastPort>(StringComparer.OrdinalIgnoreCase);
		private bool initStarted;

		private void Start()
		{
			car = base.GetComponent<TrainCar>();
		}

		private void OnDestroy()
		{
			base.StopAllCoroutines();
			initStarted = false;
			isInitialized = false;
			if (fPorts != null)
			{
				fPorts.Clear();
				fPorts = null;
			}
		}

		private IEnumerator InitRoutine()
		{
			if (car == null)
			{
				car = base.GetComponent<TrainCar>();
			}

			int retries = 0;
			Component sim = null;
			PropertyInfo flowProp = null;
			while (sim == null && retries < 20)
			{
				yield return new WaitForSeconds(0.5f);
				retries++;
				Component[] componentsInChildren = base.GetComponentsInChildren<Component>(true);
				foreach (Component c in componentsInChildren)
				{
					if (c == null)
					{
						continue;
					}

					flowProp = c.GetType().GetProperty("SimulationFlow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (flowProp != null)
					{
						sim = c;
						break;
					}
				}
			}

			if (sim != null && flowProp != null)
			{
				object simFlow = flowProp.GetValue(sim, null);
				if (simFlow != null)
				{
					FieldInfo[] fields = simFlow.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo f in fields)
					{
						IDictionary dict;
						if (!typeof(IDictionary).IsAssignableFrom(f.FieldType) || (dict = f.GetValue(simFlow) as IDictionary) == null)
						{
							continue;
						}

						foreach (DictionaryEntry entry in dict)
						{
							string key = entry.Key.ToString();
							object pObj = entry.Value;
							PropertyInfo p = pObj.GetType().GetProperty("Value");
							MethodInfo m = pObj.GetType().GetMethod("ExternalValueUpdate");
							fPorts[key] = new FastPort(pObj, p, m);
						}
					}
				}
			}

			isInitialized = true;
		}

		private float GetVal(string id)
		{
			FastPort fastPort;
			return fPorts.TryGetValue(id, out fastPort) ? fastPort.Get() : 0f;
		}

		private void SetVal(string id, float val)
		{
			FastPort fastPort;
			if (fPorts.TryGetValue(id, out fastPort))
			{
				fastPort.Set(val);
			}
		}

		private void Update()
		{
			if (!isInitialized)
			{
				if (car != PlayerManager.Car)
				{
					return;
				}

				if (!initStarted)
				{
					initStarted = true;
					base.StartCoroutine(InitRoutine());
				}

				return;
			}

			if (car != PlayerManager.Car)
			{
				return;
			}

			lastUpdate += Time.deltaTime;
			if (lastUpdate > 0.1f)
			{
				RunLogic(lastUpdate);
				lastUpdate = 0f;
			}
		}

		private void RunLogic(float dt)
		{
			UpdateEptIndicators();

			float epkKey = GetVal("Kluch_EPK.EXT_IN");
			bool rbPressed = GetVal("Knopka_RB.EXT_IN") > 0.5f;
			bool rbEdge = rbPressed && !lastRb;
			lastRb = rbPressed;

			if (epkKey < 0.5f)
			{
				lastEpkKey = 0f;
				epkLogic.Reset();
				SetVal("EPK_Zvuk.EXT_IN", 0f);
				SetVal("EPK_control.EXT_IN", 0f);
				SetAllLamps(false);
				return;
			}

			bool epkEnabledEdge = epkKey >= 0.5f && lastEpkKey < 0.5f;
			lastEpkKey = epkKey;

			int lampCode = ResolveLampCode(car, dt);
			SetLampOutputs(lampCode);

			float speedKmh = Math.Abs(car.GetForwardSpeed() * 3.6f);
			float throttle = GetVal("throttle.EXT_IN");
			float brakePressure = car.brakeSystem != null ? car.brakeSystem.brakeCylinderPressure : 1f;
			epkLogic.Tick(lampCode, speedKmh, rbPressed, rbEdge, epkEnabledEdge, throttle, brakePressure, dt, SetVal);
		}

		private void UpdateEptIndicators()
		{
			float mainControl = GetVal("ext_main_control.EXT_IN");
			float eptPower = GetVal("ONOFF_Ept.EXT_IN");
			int brakeStep = Mathf.RoundToInt(GetVal("TrainBrake.EXT_IN") * 6f);
			bool eptActive = mainControl > 0.5f && eptPower > 0.5f;

			SetVal("EPT_Z_Control.EXT_IN", eptActive && (brakeStep == 0 || brakeStep == 1) ? 1f : 0f);
			SetVal("EPT_ZH_Control.EXT_IN", eptActive && (brakeStep == 2 || brakeStep == 3) ? 1f : 0f);
			SetVal("EPT_K_Control.EXT_IN", eptActive && brakeStep >= 4 ? 1f : 0f);
		}

		private int ResolveLampCode(TrainCar trainCar, float dt)
		{
			float signalDistance;
			int rawCode = GetRawLampCode(trainCar, out signalDistance);
			bool immediateRed = rawCode == AlsnLampCode.Red && signalDistance <= 100f;
			bool relaxToNonRed = rawCode != AlsnLampCode.Red;

			if (rawCode != stableLampCode)
			{
				if (rawCode != pendingLampCode)
				{
					pendingLampCode = rawCode;
					pendingLampTimer = 0f;
				}

				pendingLampTimer += dt;
				if (immediateRed || relaxToNonRed || pendingLampTimer >= 0.4f)
				{
					stableLampCode = rawCode;
				}
			}
			else
			{
				pendingLampCode = rawCode;
				pendingLampTimer = 0f;
			}

			return stableLampCode;
		}

		private static int GetRawLampCode(TrainCar trainCar, out float signalDistance)
		{
			signalDistance = float.MaxValue;
			AlsnTrackState state;
			IAlsnLampProvider provider = AlsnLampRegistry.Provider;
			if (provider != null && provider.TryGetLampState(trainCar, out state))
			{
				signalDistance = state.DistanceToLimitM;
				return ApplyObstacleOverride(trainCar, state.LampCode);
			}

			if (SignBasedAlsnProvider.TryGetLampState(trainCar, out state))
			{
				signalDistance = state.DistanceToLimitM;
				return ApplyObstacleOverride(trainCar, state.LampCode);
			}

			return AlsnLampCode.Green;
		}

		private static int ApplyObstacleOverride(TrainCar trainCar, int lampCode)
		{
			float obstacleDistance = TrackFollower.GetObstacleDistance(trainCar, 400f);
			if (obstacleDistance > 0f && obstacleDistance < 400f)
			{
				return AlsnLampCode.Red;
			}

			return lampCode;
		}

		private void SetLampOutputs(int lampCode)
		{
			SetVal("Alsn_Green_Control.EXT_IN", lampCode == AlsnLampCode.Green ? 1f : 0f);
			SetVal("Alsn_Y_Control.EXT_IN", lampCode == AlsnLampCode.Yellow ? 1f : 0f);
			SetVal("Alsn_RY_Control.EXT_IN", lampCode == AlsnLampCode.YellowRed ? 1f : 0f);
			SetVal("Alsn_R_Control.EXT_IN", lampCode == AlsnLampCode.Red ? 1f : 0f);
			SetVal("Alsn_W_Control.EXT_IN", lampCode == AlsnLampCode.White ? 1f : 0f);
		}

		private void SetAllLamps(bool on)
		{
			float val = on ? 1f : 0f;
			SetVal("Alsn_Green_Control.EXT_IN", val);
			SetVal("Alsn_Y_Control.EXT_IN", val);
			SetVal("Alsn_RY_Control.EXT_IN", val);
			SetVal("Alsn_R_Control.EXT_IN", val);
			SetVal("Alsn_W_Control.EXT_IN", val);
		}
	}
}
