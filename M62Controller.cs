using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace M62Logic
{
	// Token: 0x02000009 RID: 9
	public class M62Controller : MonoBehaviour
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000025E6 File Offset: 0x000007E6
		public bool IsInitialized
		{
			get
			{
				return this.initialized;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000D RID: 13 RVA: 0x000025EE File Offset: 0x000007EE
		// (set) Token: 0x0600000E RID: 14 RVA: 0x000025F6 File Offset: 0x000007F6
		public float FinalTorque { get; private set; }

		// Token: 0x0600000F RID: 15 RVA: 0x00002600 File Offset: 0x00000800
		private void Awake()
		{
			bool flag = !M62Controller.allControllers.Contains(this);
			if (flag)
			{
				M62Controller.allControllers.Add(this);
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000262C File Offset: 0x0000082C
		private void Start()
		{
			this.car = base.GetComponent<TrainCar>();
			this.rb = base.GetComponent<Rigidbody>();
			bool flag = this.rb == null;
			if (flag)
			{
				this.rb = base.GetComponentInParent<Rigidbody>();
			}
			base.StartCoroutine(this.CapturePortsRoutine());
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000267C File Offset: 0x0000087C
		private void OnDestroy()
		{
			base.StopAllCoroutines();
			bool flag = M62Controller.allControllers.Contains(this);
			if (flag)
			{
				M62Controller.allControllers.Remove(this);
			}
			bool flag2 = this.typhonSource != null;
			if (flag2)
			{
				Object.Destroy(this.typhonSource);
			}
			bool flag3 = this.whistleSource != null;
			if (flag3)
			{
				Object.Destroy(this.whistleSource);
			}
			bool flag4 = this.oilpumpSource != null;
			if (flag4)
			{
				Object.Destroy(this.oilpumpSource);
			}
			bool flag5 = this.contactorMainSource != null;
			if (flag5)
			{
				Object.Destroy(this.contactorMainSource);
			}
			bool flag6 = this.contactorOpSource != null;
			if (flag6)
			{
				Object.Destroy(this.contactorOpSource);
			}
			// Only purge stale (destroyed) track cache entries on a normal despawn/stream-out.
			// Do a full clear only when this was the last M62 on the map, as a safety net for level/save transitions.
			TrackIndexer.PurgeStale();
			bool flag7 = M62Controller.allControllers.Count == 0;
			if (flag7)
			{
				TrackIndexer.Clear();
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002818 File Offset: 0x00000A18
		private void SetupAudio()
		{
			foreach (AudioSource audioSource in base.GetComponentsInChildren<AudioSource>(true))
			{
				bool flag = audioSource.clip != null;
				if (flag)
				{
					string text = audioSource.clip.name.ToLower();
					bool flag2 = text.Contains("horn");
					if (flag2)
					{
						this.defaultHornSource = audioSource;
					}
					bool flag3 = text.Contains("bell");
					if (flag3)
					{
						this.defaultBellSource = audioSource;
					}
				}
			}
			this.typhonSource = base.gameObject.AddComponent<AudioSource>();
			this.typhonSource.spatialBlend = 1f;
			this.typhonSource.minDistance = 50f;
			this.typhonSource.maxDistance = 2500f;
			this.typhonSource.loop = true;
			this.whistleSource = base.gameObject.AddComponent<AudioSource>();
			this.whistleSource.spatialBlend = 1f;
			this.whistleSource.minDistance = 50f;
			this.whistleSource.maxDistance = 2000f;
			this.whistleSource.loop = true;
			this.oilpumpSource = base.gameObject.AddComponent<AudioSource>();
			this.oilpumpSource.spatialBlend = 1f;
			this.oilpumpSource.minDistance = 3f;
			this.oilpumpSource.maxDistance = 25f;
			this.oilpumpSource.loop = true;
			this.oilpumpSource.volume = 0.8f;
			this.contactorMainSource = base.gameObject.AddComponent<AudioSource>();
			this.contactorMainSource.spatialBlend = 1f;
			this.contactorMainSource.minDistance = 3f;
			this.contactorMainSource.maxDistance = 25f;
			this.contactorOpSource = base.gameObject.AddComponent<AudioSource>();
			this.contactorOpSource.spatialBlend = 1f;
			this.contactorOpSource.minDistance = 3f;
			this.contactorOpSource.maxDistance = 25f;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002A70 File Offset: 0x00000C70
		private void RefreshNeedles()
		{
			bool flag = Time.time - this.lastNeedleSearchTime < 2f;
			if (!flag)
			{
				this.lastNeedleSearchTime = Time.time;
				bool flag2 = this.car == null;
				if (!flag2)
				{
					this.needleVolts.Clear();
					this.needleAmps.Clear();
					this.allTransformsBuffer.Clear();
					this.car.GetComponentsInChildren<Transform>(true, this.allTransformsBuffer);
					this.ProcessTransformsForNeedles(this.allTransformsBuffer);
					bool flag3 = this.car.loadedInterior != null;
					if (flag3)
					{
						this.interiorTransformsBuffer.Clear();
						this.car.loadedInterior.GetComponentsInChildren<Transform>(true, this.interiorTransformsBuffer);
						this.ProcessTransformsForNeedles(this.interiorTransformsBuffer);
					}
				}
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002B48 File Offset: 0x00000D48
		private void ProcessTransformsForNeedles(List<Transform> transforms)
		{
			for (int i = 0; i < transforms.Count; i++)
			{
				Transform transform = transforms[i];
				bool flag = transform.name.Contains("Plane16") && !GaugeUtils.HasGauge(this.needleVolts, transform);
				if (flag)
				{
					this.needleVolts.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, new Vector3(-108.5f, 0f, 136.2f))
					});
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
				}
				bool flag2 = transform.name.Contains("Plane10") && !GaugeUtils.HasGauge(this.needleAmps, transform);
				if (flag2)
				{
					this.needleAmps.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, new Vector3(-109f, -1.5f, 134.8f))
					});
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
				}
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002C5F File Offset: 0x00000E5F
		private IEnumerator CapturePortsRoutine()
		{
			Component sim = null;
			PropertyInfo flowProp = null;
			int retries = 0;
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
			bool flag3 = sim == null || flowProp == null;
			if (!flag3)
			{
				object simFlow = flowProp.GetValue(sim, null);
				bool flag4 = simFlow == null;
				if (!flag4)
				{
					this.validTorquePorts.Clear();
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
								string upperKey = key.ToUpper();
								bool flag6 = (upperKey.Contains("RPM") || upperKey.Contains("WHEEL")) && (upperKey.Contains("OUT") || upperKey.Contains("MOTOR") || upperKey.Contains("AXLE")) && !upperKey.Contains("GEN") && !upperKey.Contains("TURB");
								if (flag6)
								{
									this.motorRpmPortId = key;
								}
								bool flag7 = upperKey.EndsWith("TORQUE_OUT") && !upperKey.Contains("TRANS") && !upperKey.Contains("GEAR") && !upperKey.Contains("WHEEL");
								if (flag7)
								{
									this.validTorquePorts.Add(key);
								}
							}
						}
					}
					bool flag8 = this.validTorquePorts.Count == 0;
					if (flag8)
					{
						foreach (KeyValuePair<string, FastPort> kvp in this.fPorts)
						{
							bool flag9 = kvp.Key.EndsWith("TORQUE_OUT", StringComparison.OrdinalIgnoreCase);
							if (flag9)
							{
								this.validTorquePorts.Add(kvp.Key);
							}
						}
					}
					bool flag10 = this.fPorts.ContainsKey("ext_battery.EXT_IN");
					if (flag10)
					{
						this.initialized = true;
						this.TryApplyColdStart();
					}
				}
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002C70 File Offset: 0x00000E70
		private float GetVal(string id)
		{
			FastPort fastPort;
			return this.fPorts.TryGetValue(id, out fastPort) ? fastPort.Get() : 0f;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002CA0 File Offset: 0x00000EA0
		private void SetVal(string id, float val)
		{
			FastPort fastPort;
			bool flag = this.fPorts.TryGetValue(id, out fastPort);
			if (flag)
			{
				fastPort.Set(val);
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002CC8 File Offset: 0x00000EC8
		public void InjectTorque()
		{
			bool flag = this.fPorts.ContainsKey("reverser.Control_EXT_IN");
			float num;
			if (flag)
			{
				num = this.GetVal("reverser.Control_EXT_IN");
			}
			else
			{
				bool flag2 = this.fPorts.ContainsKey("ext_reverser.EXT_IN");
				if (flag2)
				{
					num = this.GetVal("ext_reverser.EXT_IN");
				}
				else
				{
					bool flag3 = this.fPorts.ContainsKey("reverser.EXT_IN");
					if (flag3)
					{
						num = this.GetVal("reverser.EXT_IN");
					}
					else
					{
						num = 1f;
					}
				}
			}
			float num2 = (num - 0.5f) * 2f;
			int num3 = (this.validTorquePorts.Count > 0) ? this.validTorquePorts.Count : 1;
			float val = this.FinalTorque / (float)num3 * num2;
			for (int i = 0; i < this.validTorquePorts.Count; i++)
			{
				FastPort fastPort;
				bool flag4 = this.fPorts.TryGetValue(this.validTorquePorts[i], out fastPort);
				if (flag4)
				{
					fastPort.Set(val);
				}
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002DD2 File Offset: 0x00000FD2
		private IEnumerator PlayContactorSequence(AudioSource source, AudioClip clip, int count, float vol = 1f)
		{
			bool flag = !(source == null) && !(clip == null);
			if (flag)
			{
				for (int i = 0; i < count; i++)
				{
					source.pitch = Random.Range(0.85f, 1.15f);
					source.PlayOneShot(clip, vol);
					yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
				}
			}
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002E00 File Offset: 0x00001000
		private void FixedUpdate()
		{
			bool flag = !this.initialized || this.rb == null;
			if (!flag)
			{
				float fixedDeltaTime = Time.fixedDeltaTime;
				this.SetVal("tractionGenerator.EXTERNAL_POWER_LIMIT_EXT_IN", 0f);
				this.SetVal("tractionGenerator.EXTERNAL_CURRENT_LIMIT_EXT_IN", 0f);
				bool flag2 = this.GetVal("de.ENGINE_ON") > 0.5f;
				bool flag3 = this.GetVal("ext_battery.EXT_IN") > 0.5f;
				bool flag4 = this.GetVal("ext_main_control.EXT_IN") > 0.5f;
				float val = this.GetVal("throttle.EXT_IN");
				int num = Mathf.Clamp(Mathf.RoundToInt(val * 15f), 0, 15);
				float num2 = M62Controller.RPM_TARGETS[num];
				float num3 = (num2 > this.diesel_rpm_actual) ? 25f : 60f;
				this.diesel_rpm_actual = Mathf.MoveTowards(this.diesel_rpm_actual, num2, num3 * fixedDeltaTime);
				this.FinalTorque = 0f;
				this.isSlipActive = false;
				bool flag5 = this.waterTemp >= 92f || this.oilTemp >= 85f;
				if (flag5)
				{
					this.isLoadSheddingTripped = true;
				}
				bool flag6 = this.isLoadSheddingTripped && num == 0 && this.waterTemp < 85f && this.oilTemp < 75f;
				if (flag6)
				{
					this.isLoadSheddingTripped = false;
				}
				bool flag7 = flag2 && flag4 && flag3 && num > 0 && !this.isLoadSheddingTripped;
				if (flag7)
				{
					float num4 = (float)num / 15f;
					float num5 = Mathf.Pow(num4, 1.5f);
					float num6 = 1250000f * num5;
					float num7 = Mathf.Lerp(1000f, 4500f, num4);
					float num8 = Mathf.Abs(this.rb.velocity.magnitude);
					float num9 = num8 * 3.6f;
					float num10 = num8 / 0.525f * 4.533f;
					float num11 = num10;
					bool flag8 = this.motorRpmPortId.ToUpper().Contains("WHEEL");
					bool flag9 = !string.IsNullOrEmpty(this.motorRpmPortId);
					if (flag9)
					{
						float num12 = Mathf.Abs(this.GetVal(this.motorRpmPortId));
						float num13 = num12 * 0.10471976f;
						bool flag10 = flag8;
						if (flag10)
						{
							num13 *= 4.533f;
						}
						bool flag11 = num13 > 0.1f;
						if (flag11)
						{
							num11 = num13;
						}
					}
					float num14 = 1f;
					bool flag12 = num11 > 5f && num11 > num10 + 5f;
					if (flag12)
					{
						float num15 = (num11 - num10) / num11;
						bool flag13 = num15 > 0.05f;
						if (flag13)
						{
							num14 = Mathf.Clamp01(1f - num15 * 4f);
							this.isSlipActive = true;
						}
					}
					num6 *= num14;
					num7 *= num14;
					float num16 = this.i_tm_prev * this.alpha_transition;
					float num17 = 1f - Mathf.Exp(-num16 / 300f);
					float num18 = 14f * num17 * num11;
					float num19 = 0f;
					float num20 = num18 * num18 + 0.14f * num6;
					bool flag14 = num20 > 0f;
					if (flag14)
					{
						num19 = (-num18 + Mathf.Sqrt(num20)) / 0.07f;
					}
					bool flag15 = num19 > num7;
					if (flag15)
					{
						num19 = num7;
					}
					float num21 = num18 + num19 * 0.035f;
					bool flag16 = num21 > 900f;
					if (flag16)
					{
						num21 = 900f;
						num19 = (num21 - num18) / 0.035f;
						bool flag17 = num19 < 0f;
						if (flag17)
						{
							num19 = 0f;
						}
					}
					bool flag18 = this.alpha_transition > 0.9f;
					if (flag18)
					{
						bool flag19 = num21 > 840f;
						if (flag19)
						{
							this.alpha_transition = 0.6f;
						}
					}
					else
					{
						bool flag20 = this.alpha_transition > 0.5f;
						if (flag20)
						{
							bool flag21 = num21 > 840f;
							if (flag21)
							{
								this.alpha_transition = 0.35f;
							}
							else
							{
								bool flag22 = num19 > 3200f;
								if (flag22)
								{
									this.alpha_transition = 1f;
								}
							}
						}
						else
						{
							bool flag23 = num19 > 3200f;
							if (flag23)
							{
								this.alpha_transition = 0.6f;
							}
						}
					}
					float num22 = num19 / 6f;
					this.i_tm_prev = num22;
					float num23 = num22 * this.alpha_transition;
					float num24 = 1f - Mathf.Exp(-num23 / 300f);
					float num25 = 14f * num24 * num22;
					bool flag24 = num9 > 105f;
					if (flag24)
					{
						num25 = 0f;
						num19 = 0f;
					}
					this.FinalTorque = num25 * 6f;
					bool flag25 = num9 > 70f;
					if (flag25)
					{
						float num26 = num9 - 70f;
						float num27 = num26 * num26 * 4f;
						this.FinalTorque -= num27;
						bool flag26 = this.FinalTorque < 0f;
						if (flag26)
						{
							this.FinalTorque = 0f;
						}
					}
					this.targetDisplayAmps = num19;
					this.targetDisplayVolts = num21;
				}
				else
				{
					this.i_tm_prev = 0f;
					this.alpha_transition = 1f;
					this.targetDisplayAmps = 0f;
					this.targetDisplayVolts = 0f;
				}
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000333C File Offset: 0x0000153C
		private void Update()
		{
			bool flag = !this.initialized;
			if (!flag)
			{
				float deltaTime = Time.deltaTime;
				bool flag2 = this.typhonSource == null;
				if (flag2)
				{
					this.SetupAudio();
				}
				bool flag3 = this.GetVal("de.ENGINE_ON") > 0.5f;
				bool flag4 = this.GetVal("ext_battery.EXT_IN") > 0.5f;
				bool flag5 = this.GetVal("ext_main_control.EXT_IN") > 0.5f;
				float val = this.GetVal("throttle.EXT_IN");
				int num = Mathf.Clamp(Mathf.RoundToInt(val * 15f), 0, 15);
				bool flag6 = flag3 && flag5 && flag4 && num > 0 && !this.isLoadSheddingTripped;
				bool flag7 = flag6 && !this.isTractionCircuitClosed;
				if (flag7)
				{
					base.StartCoroutine(this.PlayContactorSequence(this.contactorMainSource, M62AudioManager.contactorMainClip, 6, 1f));
					this.isTractionCircuitClosed = true;
				}
				else
				{
					bool flag8 = !flag6 && this.isTractionCircuitClosed;
					if (flag8)
					{
						base.StartCoroutine(this.PlayContactorSequence(this.contactorMainSource, M62AudioManager.contactorMainClip, 6, 0.7f));
						this.isTractionCircuitClosed = false;
					}
				}
				bool flag9 = Mathf.Abs(this.alpha_transition - this.lastAlphaTransition) > 0.05f && flag6;
				if (flag9)
				{
					base.StartCoroutine(this.PlayContactorSequence(this.contactorOpSource, M62AudioManager.contactorOpClip, 2, 1f));
					this.lastAlphaTransition = this.alpha_transition;
				}
				bool flag10 = this.typhonSource != null && this.whistleSource != null;
				if (flag10)
				{
					bool flag11 = this.typhonSource.clip == null;
					if (flag11)
					{
						this.typhonSource.clip = M62AudioManager.typhonClip;
					}
					bool flag12 = this.whistleSource.clip == null;
					if (flag12)
					{
						this.whistleSource.clip = M62AudioManager.whistleClip;
					}
					float val2 = this.GetVal("hornControl.EXT_IN");
					float val3 = this.GetVal("bellControl.EXT_IN");
					float num2 = 0f;
					float num3 = 0f;
					bool flag13 = val3 < 0.05f && val2 > 0.01f && val2 < 0.99f && Mathf.Abs(val2 - 0.5f) < 0.45f;
					if (flag13)
					{
						bool flag14 = val2 < 0.45f;
						if (flag14)
						{
							num2 = Mathf.Clamp01((0.5f - val2) * 2.2f);
						}
						else
						{
							bool flag15 = val2 > 0.55f;
							if (flag15)
							{
								num3 = Mathf.Clamp01((val2 - 0.5f) * 2.2f);
							}
						}
					}
					else
					{
						bool flag16 = val3 < 0.05f && (val2 <= 0.01f || val2 >= 0.99f);
						if (flag16)
						{
							bool flag17 = val2 <= 0.01f;
							if (flag17)
							{
								num2 = 1f;
							}
							bool flag18 = val2 >= 0.99f;
							if (flag18)
							{
								num3 = 1f;
							}
						}
						else
						{
							num2 = val2;
							num3 = val3;
						}
					}
					bool flag19 = num2 > 0.05f;
					if (flag19)
					{
						bool flag20 = !this.typhonSource.isPlaying;
						if (flag20)
						{
							this.typhonSource.Play();
						}
						this.typhonSource.volume = num2;
					}
					else
					{
						this.typhonSource.Stop();
					}
					bool flag21 = num3 > 0.05f;
					if (flag21)
					{
						bool flag22 = !this.whistleSource.isPlaying;
						if (flag22)
						{
							this.whistleSource.Play();
						}
						this.whistleSource.volume = num3;
					}
					else
					{
						this.whistleSource.Stop();
					}
				}
				bool flag23 = this.defaultHornSource != null;
				if (flag23)
				{
					this.defaultHornSource.volume = 0f;
				}
				bool flag24 = this.defaultBellSource != null;
				if (flag24)
				{
					this.defaultBellSource.volume = 0f;
				}
				float val4 = this.GetVal("de.RPM_NORMALIZED");
				bool flag25 = this.GetVal("ext_fuel_pump.EXT_IN") > 0.5f;
				bool flag26 = this.GetVal("ext_engine_control.EXT_IN") > 0.5f;
				bool flag27 = this.GetVal("ext_oil_pump_btn.EXT_IN") > 0.5f;
				bool flag28 = this.GetVal("ext_start_btn.EXT_IN") > 0.5f;
				bool flag29 = flag27 && !this.prevOilPumpBtn && flag26 && !flag3;
				if (flag29)
				{
					this.isAutoPumpingOil = true;
				}
				this.prevOilPumpBtn = flag27;
				bool flag30 = this.isAutoPumpingOil && this.oilPress >= 5.9f;
				if (flag30)
				{
					this.isAutoPumpingOil = false;
				}
				bool flag31 = flag3 || !flag26;
				if (flag31)
				{
					this.isAutoPumpingOil = false;
				}
				bool flag32 = this.oilpumpSource != null;
				if (flag32)
				{
					bool flag33 = flag26 && (flag27 || this.isAutoPumpingOil);
					if (flag33)
					{
						bool flag34 = this.oilpumpSource.clip == null;
						if (flag34)
						{
							this.oilpumpSource.clip = M62AudioManager.oilpumpClip;
						}
						bool flag35 = !this.oilpumpSource.isPlaying;
						if (flag35)
						{
							this.oilpumpSource.Play();
						}
					}
					else
					{
						this.oilpumpSource.Stop();
					}
				}
				bool flag36 = flag28 && flag26 && this.oilPress >= 0.3f && this.fuelPress >= 1.5f && !flag3;
				if (flag36)
				{
					this.isAutoStartingEngine = true;
				}
				bool flag37 = flag3 || !flag26;
				if (flag37)
				{
					this.isAutoStartingEngine = false;
				}
				bool flag38 = (!this.wasRunning && flag3) || (this.prevRpm < 0.01f && val4 > 0.01f);
				bool flag39 = flag38 && !flag5;
				if (flag39)
				{
					this.autoStartGracePeriod = 2f;
					this.SetVal("ext_battery.EXT_IN", 1f);
					this.SetVal("ext_main_control.EXT_IN", 1f);
					this.SetVal("ext_fuel_pump.EXT_IN", 1f);
					this.SetVal("ext_engine_control.EXT_IN", 1f);
					this.SetVal("Kluch_EPK.EXT_IN", 1f);
					this.SetVal("ONOFF_Ept.EXT_IN", 1f);
					this.SetVal("Zaluzi.EXT_IN", 1f);
					this.SetVal("Zaluzi_Oil.EXT_IN", 1f);
					this.SetVal("TrainBrake.EXT_IN", 0.16666667f);
					flag4 = true;
					flag5 = true;
					flag25 = true;
					flag26 = true;
					this.fuelPress = 2.5f;
					this.oilPress = 5f;
				}
				this.wasRunning = flag3;
				this.prevRpm = val4;
				bool flag40 = this.autoStartGracePeriod > 0f;
				if (flag40)
				{
					this.autoStartGracePeriod -= deltaTime;
				}
				bool flag41 = !flag4 && this.autoStartGracePeriod <= 0f;
				if (flag41)
				{
					this.Shutdown();
				}
				else
				{
					float num4 = (flag5 && flag25) ? (flag3 ? (2.2f + val4 * 1.3f) : 2f) : 0f;
					this.SetVal("FP_sound.EXT_IN", (num4 > 0f && this.fuelPress < num4 - 0.005f) ? 1f : 0f);
					this.fuelPress = Mathf.MoveTowards(this.fuelPress, num4, deltaTime * 0.05f);
					this.SetVal("out_fuel_press.EXT_IN", this.fuelPress + (flag3 ? (Random.Range(-1f, 1f) * (0.15f + val4 * 0.4f)) : 0f));
					bool flag42 = flag26 && (flag27 || this.isAutoPumpingOil);
					this.oilPress = (flag3 ? Mathf.Lerp(this.oilPress, 5f + val4 * 3f, deltaTime * 0.5f) : Mathf.MoveTowards(this.oilPress, flag42 ? 6f : 0f, deltaTime * (flag42 ? 0.5f : 0.02f)));
					this.SetVal("out_oil_press.EXT_IN", this.oilPress + (flag3 ? (Random.Range(-1f, 1f) * (0.25f + val4 * 0.6f)) : 0f));
					bool flag43 = this.autoStartGracePeriod <= 0f;
					if (flag43)
					{
						this.SetVal("de.IGNITION_EXT_IN", this.isAutoStartingEngine ? 1f : 0f);
						this.SetVal("de.EMERGENCY_ENGINE_OFF_EXT_IN", (flag3 && (!flag5 || !flag25 || !flag26)) ? 1f : 0f);
					}
					else
					{
						this.SetVal("de.EMERGENCY_ENGINE_OFF_EXT_IN", 0f);
					}
					float speed = (this.rb != null) ? (this.rb.velocity.magnitude * 3.6f) : 0f;
					bool zWater = this.GetVal("Zaluzi.EXT_IN") > 0.5f;
					bool zOil = this.GetVal("Zaluzi_Oil.EXT_IN") > 0.5f;
					float loadFactor = (num > 0) ? Mathf.Pow((float)num / 15f, 1.5f) : 0f;
					this.ProcessTemperature(flag3, deltaTime, speed, zWater, zOil, loadFactor);
					float num5 = flag3 ? (Random.Range(-1f, 1f) * (0.08f + val4 * 0.15f)) : 0f;
					this.SetVal("temp_eng.EXT_IN", flag4 ? (this.waterTemp + num5 * 2f) : 0f);
					this.SetVal("Temp_Oil.EXT_IN", flag4 ? (this.oilTemp + num5 * 2f) : 0f);
				}
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00003CF0 File Offset: 0x00001EF0
		private void LateUpdate()
		{
			bool flag = !this.initialized || this.rb == null;
			if (!flag)
			{
				float deltaTime = Time.deltaTime;
				bool flag2 = this.GetVal("de.ENGINE_ON") > 0.5f;
				bool flag3 = this.GetVal("ext_battery.EXT_IN") > 0.5f;
				this.displayAmps = Mathf.MoveTowards(this.displayAmps, this.targetDisplayAmps, 4500f * deltaTime);
				this.displayVolts = Mathf.MoveTowards(this.displayVolts, this.targetDisplayVolts, 900f * deltaTime);
				this.SetVal("out_voltmeter.EXT_IN", flag3 ? (64f + (flag2 ? Random.Range(-2f, 2f) : 0f)) : 0f);
				this.SetVal("OP1.EXT_IN", (this.alpha_transition < 0.9f && this.targetDisplayVolts > 50f) ? 1f : 0f);
				this.SetVal("OP2.EXT_IN", (this.alpha_transition < 0.4f && this.targetDisplayVolts > 50f) ? 1f : 0f);
				this.SetVal("Sbros_Nagruzki.EXT_IN", this.isLoadSheddingTripped ? 1f : 0f);
				this.SetVal("Boksovanie.EXT_IN", this.isSlipActive ? 1f : 0f);
				bool flag4 = this.car != null && this.car.loadedInterior != null;
				bool flag5 = flag4 && !this.wasInteriorLoaded;
				if (flag5)
				{
					this.lastNeedleSearchTime = 0f;
				}
				this.wasInteriorLoaded = flag4;
				bool flag6 = this.needleVolts.Count == 0 || (this.needleVolts.Count > 0 && this.needleVolts[0].transform == null);
				if (flag6)
				{
					this.RefreshNeedles();
				}
				float val = this.GetVal("de.RPM_NORMALIZED");
				float num = flag2 ? (Random.Range(-1f, 1f) * (0.05f + val * 0.1f)) : 0f;
				bool flag7 = this.needleVolts.Count > 0;
				if (flag7)
				{
					float num2 = Mathf.Lerp(0f, 86f, this.displayVolts / 1000f) + ((this.displayVolts > 10f) ? (num * 2f) : 0f);
					for (int i = 0; i < this.needleVolts.Count; i++)
					{
						bool flag8 = this.needleVolts[i].transform != null;
						if (flag8)
						{
							this.needleVolts[i].transform.localEulerAngles = this.needleVolts[i].initEuler + new Vector3(0f, 0f, num2);
						}
					}
				}
				bool flag9 = this.needleAmps.Count > 0;
				if (flag9)
				{
					float num3 = Mathf.Lerp(2f, 90f, this.displayAmps / 6000f) + ((this.displayAmps > 10f) ? (num * 2.5f) : 0f);
					for (int j = 0; j < this.needleAmps.Count; j++)
					{
						bool flag10 = this.needleAmps[j].transform != null;
						if (flag10)
						{
							this.needleAmps[j].transform.localEulerAngles = this.needleAmps[j].initEuler + new Vector3(0f, 0f, num3);
						}
					}
				}
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000040CC File Offset: 0x000022CC
		private void Shutdown()
		{
			this.SetVal("de.EMERGENCY_ENGINE_OFF_EXT_IN", 1f);
			this.oilPress = (this.fuelPress = (this.displayVolts = (this.displayAmps = (this.targetDisplayAmps = (this.targetDisplayVolts = 0f)))));
			this.isAutoPumpingOil = false;
			this.isAutoStartingEngine = false;
			this.SetVal("out_fuel_press.EXT_IN", 0f);
			this.SetVal("out_oil_press.EXT_IN", 0f);
			this.SetVal("OP1.EXT_IN", 0f);
			this.SetVal("OP2.EXT_IN", 0f);
			this.SetVal("FP_sound.EXT_IN", 0f);
			this.ProcessTemperature(false, Time.deltaTime, 0f, false, false, 0f);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x000041A0 File Offset: 0x000023A0
		private float GetEffectiveAmbientCelsius()
		{
			return AmbientTemperatureRegistry.GetEffectiveAmbientCelsius();
		}

		private void TryApplyColdStart()
		{
			if (this.coldStartApplied)
			{
				return;
			}

			M62ModSettings settings = Main.Settings;
			if (settings == null || !settings.ColdStartEnabled)
			{
				this.coldStartApplied = true;
				return;
			}

			float ambient = this.GetEffectiveAmbientCelsius();
			this.waterTemp = ambient;
			this.oilTemp = ambient + settings.ColdStartOilOffset;
			this.coldStartApplied = true;
		}

		private void ProcessTemperature(bool isRunning, float dt, float speed, bool zWater, bool zOil, float loadFactor)
		{
			float ambient = this.GetEffectiveAmbientCelsius();
			if (isRunning)
			{
				if (this.waterTemp < ambient)
				{
					this.waterTemp = ambient;
				}

				if (this.oilTemp < ambient)
				{
					this.oilTemp = ambient;
				}

				float num = (0.05f + loadFactor * 0.6f) * dt;
				float num2 = (0.04f + loadFactor * 0.5f) * dt;
				float num3 = zWater ? (0.008f + speed * 5E-05f) : (0.0005f + speed * 1E-05f);
				float num4 = zOil ? (0.008f + speed * 5E-05f) : (0.0005f + speed * 1E-05f);
				float num5 = (this.waterTemp - ambient) * num3 * dt;
				float num6 = (this.oilTemp - ambient) * num4 * dt;
				this.waterTemp += num - num5;
				this.oilTemp += num2 - num6;
			}
			else
			{
				this.waterTemp = ambient;
				this.oilTemp = ambient;
			}
		}

		// Token: 0x04000013 RID: 19
		public static List<M62Controller> allControllers = new List<M62Controller>();

		// Token: 0x04000014 RID: 20
		private Dictionary<string, FastPort> fPorts = new Dictionary<string, FastPort>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x04000015 RID: 21
		private bool initialized = false;

		// Token: 0x04000016 RID: 22
		private string motorRpmPortId = "";

		// Token: 0x04000017 RID: 23
		private List<string> validTorquePorts = new List<string>();

		// Token: 0x04000018 RID: 24
		private float fuelPress = 0f;

		// Token: 0x04000019 RID: 25
		private float oilPress = 0f;

		// Token: 0x0400001A RID: 26
		private float waterTemp = 10f;

		// Token: 0x0400001B RID: 27
		private float oilTemp = 10f;

		// Token: 0x0400001C RID: 28
		private const float DefaultAmbientTemp = 10f;

		private bool coldStartApplied = false;

		// Token: 0x0400001D RID: 29
		private bool wasRunning = false;

		// Token: 0x0400001E RID: 30
		private float prevRpm = 0f;

		// Token: 0x0400001F RID: 31
		private float autoStartGracePeriod = 0f;

		// Token: 0x04000020 RID: 32
		private bool isAutoPumpingOil = false;

		// Token: 0x04000021 RID: 33
		private bool prevOilPumpBtn = false;

		// Token: 0x04000022 RID: 34
		private bool isAutoStartingEngine = false;

		// Token: 0x04000023 RID: 35
		private bool isLoadSheddingTripped = false;

		// Token: 0x04000024 RID: 36
		private bool isSlipActive = false;

		// Token: 0x04000025 RID: 37
		private Rigidbody rb;

		// Token: 0x04000026 RID: 38
		private TrainCar car;

		// Token: 0x04000027 RID: 39
		private List<GaugeData> needleVolts = new List<GaugeData>();

		// Token: 0x04000028 RID: 40
		private List<GaugeData> needleAmps = new List<GaugeData>();

		// Token: 0x04000029 RID: 41
		private AudioSource typhonSource;

		// Token: 0x0400002A RID: 42
		private AudioSource whistleSource;

		// Token: 0x0400002B RID: 43
		private AudioSource oilpumpSource;

		// Token: 0x0400002C RID: 44
		private AudioSource defaultHornSource;

		// Token: 0x0400002D RID: 45
		private AudioSource defaultBellSource;

		// Token: 0x0400002E RID: 46
		private AudioSource contactorMainSource;

		// Token: 0x0400002F RID: 47
		private AudioSource contactorOpSource;

		// Token: 0x04000030 RID: 48
		private float lastNeedleSearchTime = 0f;

		// Token: 0x04000031 RID: 49
		private bool wasInteriorLoaded = false;

		// Token: 0x04000032 RID: 50
		private List<Component> compBuffer = new List<Component>(32);

		// Token: 0x04000033 RID: 51
		private List<Transform> allTransformsBuffer = new List<Transform>(256);

		// Token: 0x04000034 RID: 52
		private List<Transform> interiorTransformsBuffer = new List<Transform>(256);

		// Token: 0x04000036 RID: 54
		private float diesel_rpm_actual = 400f;

		// Token: 0x04000037 RID: 55
		private float alpha_transition = 1f;

		// Token: 0x04000038 RID: 56
		private float i_tm_prev = 0f;

		// Token: 0x04000039 RID: 57
		private bool isTractionCircuitClosed = false;

		// Token: 0x0400003A RID: 58
		private float lastAlphaTransition = 1f;

		// Token: 0x0400003B RID: 59
		private float targetDisplayVolts = 0f;

		// Token: 0x0400003C RID: 60
		private float targetDisplayAmps = 0f;

		// Token: 0x0400003D RID: 61
		private float displayVolts = 0f;

		// Token: 0x0400003E RID: 62
		private float displayAmps = 0f;

		// Token: 0x0400003F RID: 63
		private const float R_wheel = 0.525f;

		// Token: 0x04000040 RID: 64
		private const float gear_ratio = 4.533f;

		// Token: 0x04000041 RID: 65
		private const float R_circuit = 0.035f;

		// Token: 0x04000042 RID: 66
		private const float c_tm = 14f;

		// Token: 0x04000043 RID: 67
		private const float MAX_UG = 900f;

		// Token: 0x04000044 RID: 68
		private const float TM_COUNT = 6f;

		// Token: 0x04000045 RID: 69
		public static readonly float[] RPM_TARGETS = new float[]
		{
			400f,
			420f,
			445f,
			470f,
			495f,
			515f,
			540f,
			560f,
			585f,
			610f,
			635f,
			660f,
			685f,
			705f,
			730f,
			750f
		};
	}
}
