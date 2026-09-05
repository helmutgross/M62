using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace M62Logic
{
	// Token: 0x02000014 RID: 20
	public class M62_Clock_Behavior : MonoBehaviour
	{
		private static bool weatherReflectionResolved;
		private static PropertyInfo weatherDriverInstanceProp;
		private static FieldInfo weatherPresetManagerField;
		private static PropertyInfo weatherPresetManagerProp;
		private static FieldInfo timeOfDayField;
		private static PropertyInfo timeOfDayProp;

		private void Start()
		{
			this.car = base.GetComponent<TrainCar>();
			EnsureWeatherReflectionAccess();
			base.StartCoroutine(this.InitRoutine());
		}

		private void OnDestroy()
		{
			base.StopAllCoroutines();
		}

		private static void EnsureWeatherReflectionAccess()
		{
			if (weatherReflectionResolved)
			{
				return;
			}

			weatherReflectionResolved = true;
			try
			{
				Type weatherDriverType = null;
				Type singletonBehaviourType = null;
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (weatherDriverType == null)
					{
						weatherDriverType = assembly.GetType("DV.WeatherSystem.WeatherDriver") ?? assembly.GetType("WeatherDriver");
					}

					if (singletonBehaviourType == null)
					{
						singletonBehaviourType = assembly.GetType("DV.Utils.SingletonBehaviour`1") ?? assembly.GetType("SingletonBehaviour`1");
					}

					if (weatherDriverType != null && singletonBehaviourType != null)
					{
						break;
					}
				}

				if (weatherDriverType == null || singletonBehaviourType == null)
				{
					return;
				}

				Type singletonWeatherDriverType = singletonBehaviourType.MakeGenericType(weatherDriverType);
				weatherDriverInstanceProp = singletonWeatherDriverType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
				weatherPresetManagerField = weatherDriverType.GetField("manager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				weatherPresetManagerProp = weatherDriverType.GetProperty("manager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				Type weatherPresetManagerType = null;
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					weatherPresetManagerType = assembly.GetType("DV.WeatherSystem.WeatherPresetManager") ?? assembly.GetType("WeatherPresetManager");
					if (weatherPresetManagerType != null)
					{
						break;
					}
				}

				if (weatherPresetManagerType != null)
				{
					timeOfDayField = weatherPresetManagerType.GetField("timeOfDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					timeOfDayProp = weatherPresetManagerType.GetProperty("timeOfDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
			}
			catch (Exception ex)
			{
				if (Main.mod != null)
				{
					Main.mod.Logger.Log("[M62 Clock] Ошибка инициализации рефлексии погоды: " + ex.Message);
				}
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00007920 File Offset: 0x00005B20
		private float GetGameTime()
		{
			try
			{
				bool flag = weatherDriverInstanceProp != null;
				if (flag)
				{
					object value = weatherDriverInstanceProp.GetValue(null, null);
					bool flag2 = value != null;
					if (flag2)
					{
						object obj = null;
						bool flag3 = weatherPresetManagerField != null;
						if (flag3)
						{
							obj = weatherPresetManagerField.GetValue(value);
						}
						else
						{
							bool flag4 = weatherPresetManagerProp != null;
							if (flag4)
							{
								obj = weatherPresetManagerProp.GetValue(value, null);
							}
						}
						bool flag5 = obj != null;
						if (flag5)
						{
							float num = -1f;
							bool flag6 = timeOfDayField != null;
							if (flag6)
							{
								num = (float)timeOfDayField.GetValue(obj);
							}
							else
							{
								bool flag7 = timeOfDayProp != null;
								if (flag7)
								{
									num = (float)timeOfDayProp.GetValue(obj, null);
								}
							}
							bool flag8 = num >= 0f;
							if (flag8)
							{
								return num * 24f;
							}
						}
					}
				}
			}
			catch
			{
			}
			return (float)DateTime.Now.TimeOfDay.TotalHours;
		}

		private float GetGameTimeCached()
		{
			int frame = Time.frameCount;
			bool flag = frame >= this.nextGameTimeRefreshFrame;
			if (flag)
			{
				this.cachedGameTime = this.GetGameTime();
				this.nextGameTimeRefreshFrame = frame + 5;
			}
			return this.cachedGameTime;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00007A58 File Offset: 0x00005C58
		private IEnumerator InitRoutine()
		{
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
					bool flag = c != null;
					if (flag)
					{
						flowProp = c.GetType().GetProperty("SimulationFlow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						bool flag2 = flowProp != null;
						if (flag2)
						{
							sim = c;
							break;
						}
					}
				}
			}
			bool flag3 = sim != null && flowProp != null;
			if (flag3)
			{
				object simFlow = flowProp.GetValue(sim, null);
				bool flag4 = simFlow != null;
				if (flag4)
				{
					FieldInfo[] fields = simFlow.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo f in fields)
					{
						IDictionary dict = null;
						bool flag5 = !typeof(IDictionary).IsAssignableFrom(f.FieldType) || !((dict = (f.GetValue(simFlow) as IDictionary)) != null);
						if (!flag5)
						{
							foreach (DictionaryEntry entry in dict)
							{
								string key = entry.Key.ToString();
								object pObj = entry.Value;
								PropertyInfo p = pObj.GetType().GetProperty("Value");
								MethodInfo m = pObj.GetType().GetMethod("ExternalValueUpdate");
								this.fPorts[key] = new FastPort(pObj, p, m);
							}
						}
					}
				}
			}
			this.isInitialized = true;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00007A68 File Offset: 0x00005C68
		private float GetVal(string id)
		{
			FastPort fastPort;
			return this.fPorts.TryGetValue(id, out fastPort) ? fastPort.Get() : 0f;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00007A98 File Offset: 0x00005C98
		private void LateUpdate()
		{
			bool flag = !this.isInitialized;
			if (!flag)
			{
				bool flag2 = this.car != null && this.car.loadedInterior != null;
				bool flag3 = flag2 && !this.wasInteriorLoaded;
				if (flag3)
				{
					this.lastSearchTime = 0f;
				}
				this.wasInteriorLoaded = flag2;
				this.RefreshHands();
				bool flag4 = !this.isWound;
				if (flag4)
				{
					float val = this.GetVal("vzvodS.EXT_IN");
					bool flag5 = val >= 0.95f;
					if (flag5)
					{
						this.isWound = true;
					}
					for (int i = 0; i < this.minHands.Count; i++)
					{
						bool flag6 = this.minHands[i].transform != null;
						if (flag6)
						{
							this.minHands[i].transform.localEulerAngles = this.minHands[i].initEuler;
						}
					}
					for (int j = 0; j < this.chasHands.Count; j++)
					{
						bool flag7 = this.chasHands[j].transform != null;
						if (flag7)
						{
							this.chasHands[j].transform.localEulerAngles = this.chasHands[j].initEuler;
						}
					}
				}
				else
				{
					this.UpdateTime();
				}
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00007C24 File Offset: 0x00005E24
		private bool HasHand(List<M62_Clock_Behavior.HandData> list, Transform t)
		{
			for (int i = 0; i < list.Count; i++)
			{
				bool flag = list[i].transform == t;
				if (flag)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00007C68 File Offset: 0x00005E68
		private void RefreshHands()
		{
			bool flag = Time.time - this.lastSearchTime < 2f;
			if (!flag)
			{
				this.lastSearchTime = Time.time;
				bool flag2 = this.car == null;
				if (!flag2)
				{
					this.minHands.RemoveAll((M62_Clock_Behavior.HandData h) => h.transform == null);
					this.chasHands.RemoveAll((M62_Clock_Behavior.HandData h) => h.transform == null);
					this.allTransformsBuffer.Clear();
					this.car.GetComponentsInChildren<Transform>(true, this.allTransformsBuffer);
					this.ProcessTransformsForHands(this.allTransformsBuffer);
					bool flag3 = this.car.loadedInterior != null;
					if (flag3)
					{
						this.interiorTransformsBuffer.Clear();
						this.car.loadedInterior.GetComponentsInChildren<Transform>(true, this.interiorTransformsBuffer);
						this.ProcessTransformsForHands(this.interiorTransformsBuffer);
					}
				}
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00007D7C File Offset: 0x00005F7C
		private void ProcessTransformsForHands(List<Transform> transforms)
		{
			for (int i = 0; i < transforms.Count; i++)
			{
				Transform transform = transforms[i];
				bool flag = transform.name == "min" && !this.HasHand(this.minHands, transform);
				if (flag)
				{
					this.minHands.Add(new M62_Clock_Behavior.HandData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, Vector3.zero),
						initLocalPos = transform.localPosition
					});
				}
				bool flag2 = transform.name == "chas" && !this.HasHand(this.chasHands, transform);
				if (flag2)
				{
					this.chasHands.Add(new M62_Clock_Behavior.HandData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, new Vector3(0f, 180f, 0f)),
						initLocalPos = transform.localPosition
					});
				}
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00007E8C File Offset: 0x0000608C
		// Token: 0x0600005A RID: 90 RVA: 0x00007EF8 File Offset: 0x000060F8
		private void UpdateTime()
		{
			float gameTime = this.GetGameTimeCached();
			float num = gameTime % 1f * 60f;
			float num2 = num * 6f;
			float num3 = -(gameTime * 15f);
			for (int i = 0; i < this.minHands.Count; i++)
			{
				bool flag = this.minHands[i].transform != null;
				if (flag)
				{
					this.minHands[i].transform.localEulerAngles = new Vector3(this.minHands[i].initEuler.x, this.minHands[i].initEuler.y, this.minHands[i].initEuler.z + num2);
					this.minHands[i].transform.localPosition = this.minHands[i].initLocalPos;
				}
			}
			for (int j = 0; j < this.chasHands.Count; j++)
			{
				bool flag2 = this.chasHands[j].transform != null;
				if (flag2)
				{
					this.chasHands[j].transform.localEulerAngles = new Vector3(this.chasHands[j].initEuler.x, this.chasHands[j].initEuler.y, this.chasHands[j].initEuler.z + num3);
					this.chasHands[j].transform.localPosition = this.chasHands[j].initLocalPos;
				}
			}
		}

		// Token: 0x04000085 RID: 133
		private bool isInitialized = false;

		// Token: 0x04000086 RID: 134
		private bool isWound = false;

		// Token: 0x04000087 RID: 135
		private TrainCar car;

		// Token: 0x04000088 RID: 136
		private Dictionary<string, FastPort> fPorts = new Dictionary<string, FastPort>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x04000089 RID: 137
		private List<M62_Clock_Behavior.HandData> minHands = new List<M62_Clock_Behavior.HandData>();

		// Token: 0x0400008A RID: 138
		private List<M62_Clock_Behavior.HandData> chasHands = new List<M62_Clock_Behavior.HandData>();

		// Token: 0x0400008B RID: 139
		private float lastSearchTime = 0f;

		// Token: 0x0400008C RID: 140
		private bool wasInteriorLoaded = false;

		// Token: 0x0400008D RID: 141
		private List<Transform> allTransformsBuffer = new List<Transform>(256);

		// Token: 0x0400008E RID: 142
		private List<Transform> interiorTransformsBuffer = new List<Transform>(256);

		private float cachedGameTime;

		private int nextGameTimeRefreshFrame = -1;

		// Token: 0x0200001E RID: 30
		private class HandData
		{
			// Token: 0x04000114 RID: 276
			public Transform transform;

			// Token: 0x04000115 RID: 277
			public Vector3 initEuler;

			// Token: 0x04000116 RID: 278
			public Vector3 initLocalPos;
		}
	}
}
