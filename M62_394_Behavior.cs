using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.Simulation.Brake;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace M62Logic
{
	// Token: 0x0200000C RID: 12
	public class M62_394_Behavior : MonoBehaviour
	{
		// Token: 0x06000026 RID: 38 RVA: 0x000044DA File Offset: 0x000026DA
		private void Start()
		{
			this.car = base.GetComponent<TrainCar>();
			this.bs = this.car.brakeSystem;
			base.StartCoroutine(this.InitRoutine());
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00004508 File Offset: 0x00002708
		private void OnDestroy()
		{
			base.StopAllCoroutines();
			bool flag = this.compressorSource != null;
			if (flag)
			{
				Object.Destroy(this.compressorSource);
			}
			bool flag2 = this.click395Source != null;
			if (flag2)
			{
				Object.Destroy(this.click395Source);
			}
			bool flag3 = this.hiss395Source != null;
			if (flag3)
			{
				Object.Destroy(this.hiss395Source);
			}
			bool flag4 = this.charge395Source != null;
			if (flag4)
			{
				Object.Destroy(this.charge395Source);
			}
			bool flag5 = this.click254Source != null;
			if (flag5)
			{
				Object.Destroy(this.click254Source);
			}
			bool flag6 = this.airCharge254Source != null;
			if (flag6)
			{
				Object.Destroy(this.airCharge254Source);
			}
			bool flag7 = this.airRelease254Source != null;
			if (flag7)
			{
				Object.Destroy(this.airRelease254Source);
			}
			for (int i = 0; i < 4; i++)
			{
				bool flag8 = this.blowdownAudio[i] != null;
				if (flag8)
				{
					Object.Destroy(this.blowdownAudio[i]);
				}
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x0000473C File Offset: 0x0000293C
		private void EradicateVanillaPneumatics()
		{
			bool flag = this.car == null;
			if (!flag)
			{
				this.audioSourceBuffer.Clear();
				this.car.GetComponentsInChildren<AudioSource>(true, this.audioSourceBuffer);
				this.KillAudioIfVanillaBrake(this.audioSourceBuffer);
				bool flag2 = this.car.loadedInterior != null;
				if (flag2)
				{
					this.interiorAudioBuffer.Clear();
					this.car.loadedInterior.GetComponentsInChildren<AudioSource>(true, this.interiorAudioBuffer);
					this.KillAudioIfVanillaBrake(this.interiorAudioBuffer);
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000047D0 File Offset: 0x000029D0
		private void KillAudioIfVanillaBrake(List<AudioSource> sources)
		{
			for (int i = 0; i < sources.Count; i++)
			{
				AudioSource audioSource = sources[i];
				bool flag = audioSource == null;
				if (!flag)
				{
					bool flag2 = audioSource == this.compressorSource || audioSource == this.click395Source || audioSource == this.hiss395Source || audioSource == this.charge395Source || audioSource == this.click254Source || audioSource == this.airCharge254Source || audioSource == this.airRelease254Source;
					if (!flag2)
					{
						bool flag3 = false;
						for (int j = 0; j < 4; j++)
						{
							bool flag4 = this.blowdownAudio[j] == audioSource;
							if (flag4)
							{
								flag3 = true;
							}
						}
						bool flag5 = flag3;
						if (!flag5)
						{
							bool flag6 = audioSource.clip != null || audioSource.gameObject.name != null;
							if (flag6)
							{
								string text = (audioSource.clip != null) ? audioSource.clip.name.ToLower() : "";
								string text2 = audioSource.gameObject.name.ToLower();
								bool flag7 = text2.Contains("brake") || text2.Contains("air") || text2.Contains("valve") || text2.Contains("ind") || text2.Contains("exhaust") || text2.Contains("flow") || text2.Contains("395") || text2.Contains("254") || text2.Contains("locobrake") || text2.Contains("trainbrake") || text2.Contains("kran") || text2.Contains("367");
								bool flag8 = text.Contains("brake") || text.Contains("air") || text.Contains("hiss") || text.Contains("valve") || text.Contains("ind") || text.Contains("exhaust") || text.Contains("flow") || text.Contains("notch") || text.Contains("lever") || text.Contains("release");
								bool flag9 = (flag7 || flag8) && !text.Contains("squeal") && !text2.Contains("squeal") && !text.Contains("shoe") && !text2.Contains("shoe") && !text.Contains("slid") && !text2.Contains("slid") && !text.Contains("horn") && !text.Contains("bell");
								if (flag9)
								{
									audioSource.Stop();
									audioSource.volume = 0f;
									audioSource.mute = true;
									audioSource.maxDistance = 0.01f;
									audioSource.clip = null;
									audioSource.enabled = false;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00004B1C File Offset: 0x00002D1C
		private void RefreshNeedles()
		{
			bool flag = Time.time - this.lastNeedleSearchTime < 2f;
			if (!flag)
			{
				this.lastNeedleSearchTime = Time.time;
				bool flag2 = this.car == null;
				if (!flag2)
				{
					this.needleURs.Clear();
					this.needleTMs.Clear();
					this.needleTCs.Clear();
					this.needlePMs.Clear();
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

		// Token: 0x0600002E RID: 46 RVA: 0x00004C0C File Offset: 0x00002E0C
		private void ProcessTransformsForNeedles(List<Transform> transforms)
		{
			for (int i = 0; i < transforms.Count; i++)
			{
				Transform transform = transforms[i];
				bool flag = transform.name.Contains("Cylinder47") && !GaugeUtils.HasGauge(this.needleURs, transform);
				if (flag)
				{
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
					this.needleURs.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, Vector3.zero)
					});
				}
				bool flag2 = transform.name.Contains("Cylinder46") && !GaugeUtils.HasGauge(this.needleTCs, transform);
				if (flag2)
				{
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
					this.needleTCs.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, Vector3.zero)
					});
				}
				bool flag3 = transform.name.Contains("Cylinder34") && !GaugeUtils.HasGauge(this.needleTMs, transform);
				if (flag3)
				{
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
					this.needleTMs.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, Vector3.zero)
					});
				}
				bool flag4 = transform.name.Contains("Cylinder33") && !GaugeUtils.HasGauge(this.needlePMs, transform);
				if (flag4)
				{
					GaugeUtils.StripGameScripts(transform, this.compBuffer);
					this.needlePMs.Add(new GaugeData
					{
						transform = transform,
						initEuler = GaugeUtils.CleanVector3(transform.localEulerAngles, Vector3.zero)
					});
				}
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00004DCD File Offset: 0x00002FCD
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
			this.SetupAudio();
			this.isInitialized = true;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00004DDC File Offset: 0x00002FDC
		private float GetVal(string id)
		{
			FastPort fastPort;
			return this.fPorts.TryGetValue(id, out fastPort) ? fastPort.Get() : 0f;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00004E0C File Offset: 0x0000300C
		private void SetupAudio()
		{
			this.compressorSource = base.gameObject.AddComponent<AudioSource>();
			this.compressorSource.spatialBlend = 1f;
			this.compressorSource.minDistance = 10f;
			this.compressorSource.maxDistance = 100f;
			this.compressorSource.volume = 0f;
			this.compressorSource.priority = 128;
			this.compressorSource.loop = true;
			this.click395Source = base.gameObject.AddComponent<AudioSource>();
			this.click395Source.spatialBlend = 1f;
			this.click395Source.minDistance = 2f;
			this.click395Source.maxDistance = 15f;
			this.hiss395Source = base.gameObject.AddComponent<AudioSource>();
			this.hiss395Source.spatialBlend = 0.85f;
			this.hiss395Source.minDistance = 5f;
			this.hiss395Source.maxDistance = 60f;
			this.hiss395Source.loop = true;
			this.charge395Source = base.gameObject.AddComponent<AudioSource>();
			this.charge395Source.spatialBlend = 0.85f;
			this.charge395Source.minDistance = 5f;
			this.charge395Source.maxDistance = 60f;
			this.charge395Source.loop = true;
			this.click254Source = base.gameObject.AddComponent<AudioSource>();
			this.click254Source.spatialBlend = 1f;
			this.click254Source.minDistance = 2f;
			this.click254Source.maxDistance = 15f;
			this.airCharge254Source = base.gameObject.AddComponent<AudioSource>();
			this.airCharge254Source.spatialBlend = 1f;
			this.airCharge254Source.minDistance = 2f;
			this.airCharge254Source.maxDistance = 15f;
			this.airCharge254Source.loop = true;
			this.airRelease254Source = base.gameObject.AddComponent<AudioSource>();
			this.airRelease254Source.spatialBlend = 1f;
			this.airRelease254Source.minDistance = 2f;
			this.airRelease254Source.maxDistance = 15f;
			this.airRelease254Source.loop = true;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x0000505C File Offset: 0x0000325C
		private void FixedUpdate()
		{
			bool flag = !this.isInitialized || this.bs == null;
			if (!flag)
			{
				float fixedDeltaTime = Time.fixedDeltaTime;
				float val = this.GetVal("TrainBrake.EXT_IN");
				this.state = Mathf.Clamp(Mathf.RoundToInt(val * 6f), 0, 6);
				float val2 = this.GetVal("brakeCutout.EXT_IN");
				bool flag2 = val2 > 0.5f;
				float mainReservoirPressure = this.bs.mainReservoirPressure;
				float num = mainReservoirPressure;
				bool flag3 = this.GetVal("de.ENGINE_ON") > 0.5f;
				float val3 = this.GetVal("de.RPM_NORMALIZED");
				bool flag4 = flag3;
				if (flag4)
				{
					bool flag5 = num <= 7.5f;
					if (flag5)
					{
						this.isCompressorRunning = true;
					}
					else
					{
						bool flag6 = num >= 8.5f;
						if (flag6)
						{
							this.isCompressorRunning = false;
						}
					}
				}
				else
				{
					this.isCompressorRunning = false;
				}
				bool flag7 = this.isCompressorRunning;
				if (flag7)
				{
					num += Mathf.Lerp(0.04f, 0.08f, val3) * fixedDeltaTime;
				}
				float val4 = this.GetVal("hornControl.EXT_IN");
				bool flag8 = val4 > 0.1f;
				if (flag8)
				{
					num -= 0.05f * fixedDeltaTime * val4;
				}
				num -= 0.001f * fixedDeltaTime;
				int num2 = 0;
				for (int i = 0; i < 4; i++)
				{
					bool flag9 = this.GetVal(this.blowdownPorts[i]) > 0.5f;
					if (flag9)
					{
						num2++;
					}
				}
				bool flag10 = num2 > 0 && num > 1f;
				if (flag10)
				{
					num -= 0.15f * (float)num2 * fixedDeltaTime;
				}
				switch (this.state)
				{
				case 0:
					this.virtualER = Mathf.MoveTowards(this.virtualER, Mathf.Min(5.7f, mainReservoirPressure), 0.5f * fixedDeltaTime);
					break;
				case 1:
				{
					bool flag11 = this.virtualER > 5.2f;
					if (flag11)
					{
						this.virtualER -= 0.002f * fixedDeltaTime;
					}
					else
					{
						this.virtualER = Mathf.MoveTowards(this.virtualER, Mathf.Min(5.2f, mainReservoirPressure), 0.15f * fixedDeltaTime);
					}
					break;
				}
				case 2:
					this.virtualER -= 0.015f * fixedDeltaTime;
					break;
				case 4:
					this.virtualER = Mathf.MoveTowards(this.virtualER, 0f, 0.05f * fixedDeltaTime);
					break;
				case 5:
					this.virtualER = Mathf.MoveTowards(this.virtualER, 0f, 0.25f * fixedDeltaTime);
					break;
				case 6:
					this.virtualER = Mathf.MoveTowards(this.virtualER, 0f, 1.5f * fixedDeltaTime);
					break;
				}
				this.virtualER = Mathf.Clamp(this.virtualER, 0f, 6f);
				float num3 = this.virtualER + 1f;
				float num4 = this.visualTC_Train + 1f;
				float num5 = this.visualTC_Loco + 1f;
				bool flag12 = flag2;
				if (flag12)
				{
					this.bs.selfLappingController = false;
					this.bs.trainBrakePosition = 0f;
					float num6 = num3;
					float pipePressure = this.bs.brakeset.pipePressure;
					float num7 = num6 - pipePressure;
					bool flag13 = num7 > 0.005f;
					if (flag13)
					{
						float num8 = this.bs.brakeset.pipeVolume / this.bs.mainResVolume;
						float num9 = Mathf.Min(num7, 0.5f * fixedDeltaTime);
						bool flag14 = num > pipePressure;
						if (flag14)
						{
							num -= num9 * num8 * 0.1f;
							this.bs.brakeset.pipePressure = Mathf.MoveTowards(pipePressure, num6, num9);
						}
					}
					else
					{
						bool flag15 = num7 < -0.005f;
						if (flag15)
						{
							this.bs.brakeset.pipePressure = Mathf.MoveTowards(pipePressure, num6, ((this.state == 6) ? 1.5f : 0.4f) * fixedDeltaTime);
						}
					}
					this.bs.SetBrakePipePressure(this.bs.brakeset.pipePressure);
					bool flag16 = this.car.trainset != null && this.car.trainset.cars != null;
					if (flag16)
					{
						for (int j = 0; j < this.car.trainset.cars.Count; j++)
						{
							TrainCar trainCar = this.car.trainset.cars[j];
							BrakeSystem brakeSystem = trainCar.brakeSystem;
							bool flag17 = brakeSystem != null;
							if (flag17)
							{
								bool flag18 = brakeSystem.controlReservoirPressure > 6.2f;
								if (flag18)
								{
									brakeSystem.controlReservoirPressure = 6.2f;
								}
								bool flag19 = brakeSystem.auxReservoirPressure > 6.2f;
								if (flag19)
								{
									brakeSystem.auxReservoirPressure = 6.2f;
								}
								bool flag20 = trainCar == this.car;
								if (flag20)
								{
									brakeSystem.ForceCylinderPressure(num5);
								}
								else
								{
									brakeSystem.ForceCylinderPressure(num4);
								}
							}
						}
					}
					else
					{
						this.bs.ForceCylinderPressure(num5);
					}
				}
				this.bs.independentBrakePosition = 0f;
				float num10 = flag2 ? Mathf.Min(this.virtualER, mainReservoirPressure) : 0f;
				float num11 = (num10 > this.visualTM) ? 1.5f : ((this.state == 6) ? 1.5f : 0.4f);
				bool flag21 = !flag2;
				if (flag21)
				{
					num11 = 0.015f;
				}
				this.visualTM = Mathf.MoveTowards(this.visualTM, num10, num11 * fixedDeltaTime);
				float num12 = this.GetVal("IndBrake.EXT_IN");
				bool flag22 = num12 < 0.05f;
				if (flag22)
				{
					num12 = 0f;
				}
				float num13 = Mathf.Max(0f, 5.2f - this.visualTM);
				float num14 = (num13 >= 0.15f) ? Mathf.Clamp((num13 - 0.1f) * 2.8f, 0f, 4f) : 0f;
				float num15 = num12 * 4f;
				float num16 = this.visualTC_Loco;
				this.visualTC_Train = Mathf.MoveTowards(this.visualTC_Train, num14, 1.5f * fixedDeltaTime);
				this.visualTC_Loco = Mathf.MoveTowards(this.visualTC_Loco, Mathf.Max(num14, num15), 1.5f * fixedDeltaTime);
				float num17 = this.visualTC_Loco - num16;
				bool flag23 = num17 > 0f;
				if (flag23)
				{
					num -= num17 * 0.05f;
				}
				this.tcRate = num17 / fixedDeltaTime;
				this.bs.SetMainReservoirPressure(Mathf.Clamp(num, 0f, 10f));
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00005710 File Offset: 0x00003910
		private void Update()
		{
			bool flag = !this.isInitialized;
			if (!flag)
			{
				float deltaTime = Time.deltaTime;
				bool flag2 = this.state != this.lastState;
				if (flag2)
				{
					bool flag3 = this.click395Source != null && M62AudioManager.click395Clip != null;
					if (flag3)
					{
						this.click395Source.pitch = Random.Range(0.9f, 1.1f);
						this.click395Source.PlayOneShot(M62AudioManager.click395Clip, 1f);
					}
					this.lastState = this.state;
				}
				float val = this.GetVal("IndBrake.EXT_IN");
				int num = Mathf.Clamp(Mathf.RoundToInt(val * 5f), 0, 5);
				bool flag4 = num != this.lastIndState;
				if (flag4)
				{
					bool flag5 = this.click254Source != null && M62AudioManager.click254Clip != null;
					if (flag5)
					{
						this.click254Source.pitch = Random.Range(0.95f, 1.05f);
						this.click254Source.PlayOneShot(M62AudioManager.click254Clip, 0.8f);
					}
					this.lastIndState = num;
				}
				this.smoothTcRate = Mathf.Lerp(this.smoothTcRate, this.tcRate, deltaTime * 10f);
				bool flag6 = this.smoothTcRate > 0.05f;
				if (flag6)
				{
					bool flag7 = this.airCharge254Source.clip == null;
					if (flag7)
					{
						this.airCharge254Source.clip = M62AudioManager.charge395Clip;
					}
					bool flag8 = !this.airCharge254Source.isPlaying;
					if (flag8)
					{
						this.airCharge254Source.Play();
					}
					this.airCharge254Source.volume = Mathf.Clamp01(this.smoothTcRate * 0.4f);
					this.airCharge254Source.pitch = 1.3f;
				}
				else
				{
					this.airCharge254Source.volume = Mathf.MoveTowards(this.airCharge254Source.volume, 0f, deltaTime * 5f);
					bool flag9 = this.airCharge254Source.volume <= 0.01f;
					if (flag9)
					{
						this.airCharge254Source.volume = 0f;
						this.airCharge254Source.Stop();
					}
				}
				bool flag10 = this.smoothTcRate < -0.05f;
				if (flag10)
				{
					bool flag11 = this.airRelease254Source.clip == null;
					if (flag11)
					{
						this.airRelease254Source.clip = M62AudioManager.hiss395Clip;
					}
					bool flag12 = !this.airRelease254Source.isPlaying;
					if (flag12)
					{
						this.airRelease254Source.Play();
					}
					this.airRelease254Source.volume = Mathf.Clamp01(Mathf.Abs(this.smoothTcRate) * 0.5f);
					this.airRelease254Source.pitch = 1.4f;
				}
				else
				{
					this.airRelease254Source.volume = Mathf.MoveTowards(this.airRelease254Source.volume, 0f, deltaTime * 5f);
					bool flag13 = this.airRelease254Source.volume <= 0.01f;
					if (flag13)
					{
						this.airRelease254Source.volume = 0f;
						this.airRelease254Source.Stop();
					}
				}
				float val2 = this.GetVal("brakeCutout.EXT_IN");
				bool flag14 = val2 > 0.5f;
				bool flag15 = this.hiss395Source != null;
				if (flag15)
				{
					float num2 = 0f;
					bool flag16 = flag14;
					if (flag16)
					{
						bool flag17 = this.state == 4;
						if (flag17)
						{
							num2 = 0.3f;
						}
						else
						{
							bool flag18 = this.state == 5;
							if (flag18)
							{
								num2 = 0.7f;
							}
							else
							{
								bool flag19 = this.state == 6;
								if (flag19)
								{
									num2 = 1f;
								}
							}
						}
					}
					bool flag20 = num2 > 0.01f;
					if (flag20)
					{
						bool flag21 = this.hiss395Source.clip == null;
						if (flag21)
						{
							this.hiss395Source.clip = M62AudioManager.hiss395Clip;
						}
						bool flag22 = !this.hiss395Source.isPlaying && this.hiss395Source.clip != null;
						if (flag22)
						{
							this.hiss395Source.Play();
						}
						this.hiss395Source.volume = Mathf.MoveTowards(this.hiss395Source.volume, num2, deltaTime * 5f);
						this.hiss395Source.pitch = ((this.state == 6) ? 1.08f : 1f);
					}
					else
					{
						this.hiss395Source.Stop();
					}
				}
				bool flag23 = this.charge395Source != null;
				if (flag23)
				{
					float num3 = 0f;
					bool flag24 = flag14;
					if (flag24)
					{
						bool flag25 = this.state == 0;
						if (flag25)
						{
							num3 = 1f;
						}
						else
						{
							bool flag26 = this.state == 1 && this.visualTM < 5.1899996f;
							if (flag26)
							{
								num3 = 0.65f;
							}
						}
					}
					bool flag27 = num3 > 0.01f;
					if (flag27)
					{
						bool flag28 = this.charge395Source.clip == null;
						if (flag28)
						{
							this.charge395Source.clip = M62AudioManager.charge395Clip;
						}
						bool flag29 = !this.charge395Source.isPlaying && this.charge395Source.clip != null;
						if (flag29)
						{
							this.charge395Source.Play();
						}
						this.charge395Source.volume = Mathf.MoveTowards(this.charge395Source.volume, num3, deltaTime * 3f);
					}
					else
					{
						this.charge395Source.Stop();
					}
				}
				bool flag30 = this.compressorSource != null;
				if (flag30)
				{
					bool flag31 = this.isCompressorRunning;
					if (flag31)
					{
						bool flag32 = this.compressorSource.clip == null;
						if (flag32)
						{
							this.compressorSource.clip = M62AudioManager.compressorClip;
						}
						bool flag33 = !this.compressorSource.isPlaying;
						if (flag33)
						{
							this.compressorSource.Play();
						}
						this.compressorSource.volume = Mathf.MoveTowards(this.compressorSource.volume, 0.75f, deltaTime * 2f);
						this.compressorSource.pitch = 0.9f + this.GetVal("de.RPM_NORMALIZED") * 0.25f;
					}
					else
					{
						this.compressorSource.volume = Mathf.MoveTowards(this.compressorSource.volume, 0f, deltaTime * 3f);
						bool flag34 = this.compressorSource.volume <= 0.01f;
						if (flag34)
						{
							this.compressorSource.Stop();
						}
					}
				}
				bool flag35 = this.blowdownInitialized;
				if (flag35)
				{
					float mainReservoirPressure = this.bs.mainReservoirPressure;
					float num4 = Mathf.Clamp01((mainReservoirPressure - 1f) / 7.5f);
					for (int i = 0; i < 4; i++)
					{
						bool flag36 = this.blowdownVFX[i] == null || this.blowdownAudio[i] == null;
						if (!flag36)
						{
							bool flag37 = this.GetVal(this.blowdownPorts[i]) > 0.5f;
							bool flag38 = flag37 && mainReservoirPressure > 1.05f;
							if (flag38)
							{
								this.blowdownTimers[i] += deltaTime;
								bool flag39 = !this.blowdownVFX[i].isPlaying;
								if (flag39)
								{
									this.blowdownVFX[i].Play();
								}
								bool flag40 = !this.blowdownAudio[i].isPlaying;
								if (flag40)
								{
									this.blowdownAudio[i].Play();
								}
								this.blowdownAudio[i].volume = Mathf.Clamp01(num4 * 2f);
								ParticleSystem.MainModule main = this.blowdownVFX[i].main;
								main.startSpeed = Mathf.Lerp(2f, 20f, num4);
								float num5 = Mathf.Clamp01(1f - this.blowdownTimers[i] / 2.5f);
								main.startColor = new Color(1f, 1f, 1f, num5);
							}
							else
							{
								this.blowdownTimers[i] = 0f;
								bool isPlaying = this.blowdownVFX[i].isPlaying;
								if (isPlaying)
								{
									this.blowdownVFX[i].Stop();
								}
								bool isPlaying2 = this.blowdownAudio[i].isPlaying;
								if (isPlaying2)
								{
									this.blowdownAudio[i].Stop();
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00005F98 File Offset: 0x00004198
		private void LateUpdate()
		{
			bool flag = !this.isInitialized || this.bs == null;
			if (!flag)
			{
				bool flag2 = this.car != null && this.car.loadedInterior != null;
				bool flag3 = flag2 && !this.wasInteriorLoaded;
				if (flag3)
				{
					this.lastNeedleSearchTime = 0f;
					this.lastEradicateTime = 0f;
				}
				this.wasInteriorLoaded = flag2;
				// Run immediately on interior (re)load, then only as a periodic safety-net poll
				// instead of a full AudioSource scan of the whole car+interior every single frame.
				bool flag3b = flag3 || Time.time - this.lastEradicateTime >= 1f;
				if (flag3b)
				{
					this.EradicateVanillaPneumatics();
					this.lastEradicateTime = Time.time;
				}
				bool flag4 = this.needleURs.Count == 0 || (this.needleURs.Count > 0 && this.needleURs[0].transform == null);
				if (flag4)
				{
					this.RefreshNeedles();
				}
				bool flag5 = !this.blowdownInitialized && this.car != null;
				if (flag5)
				{
					ParticleSystem[] componentsInChildren = this.car.GetComponentsInChildren<ParticleSystem>(true);
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						bool flag6 = componentsInChildren[i].name == "BlowdownVFX1";
						if (flag6)
						{
							this.blowdownVFX[0] = componentsInChildren[i];
						}
						else
						{
							bool flag7 = componentsInChildren[i].name == "BlowdownVFX2";
							if (flag7)
							{
								this.blowdownVFX[1] = componentsInChildren[i];
							}
							else
							{
								bool flag8 = componentsInChildren[i].name == "BlowdownVFX3";
								if (flag8)
								{
									this.blowdownVFX[2] = componentsInChildren[i];
								}
								else
								{
									bool flag9 = componentsInChildren[i].name == "BlowdownVFX4";
									if (flag9)
									{
										this.blowdownVFX[3] = componentsInChildren[i];
									}
								}
							}
						}
					}
					for (int j = 0; j < 4; j++)
					{
						bool flag10 = this.blowdownVFX[j] != null;
						if (flag10)
						{
							this.blowdownAudio[j] = this.blowdownVFX[j].gameObject.AddComponent<AudioSource>();
							this.blowdownAudio[j].spatialBlend = 1f;
							this.blowdownAudio[j].minDistance = 5f;
							this.blowdownAudio[j].maxDistance = 60f;
							this.blowdownAudio[j].loop = true;
							this.blowdownAudio[j].clip = M62AudioManager.blowdownClip;
						}
					}
					this.blowdownInitialized = true;
				}
				float num = Mathf.Max(0f, this.bs.mainReservoirPressure + (this.isCompressorRunning ? (Random.Range(-1f, 1f) * (0.02f + this.GetVal("de.RPM_NORMALIZED") * 0.03f)) : 0f));
				bool flag11 = this.needleTCs.Count > 0;
				if (flag11)
				{
					float num2 = Mathf.Lerp(0f, 161f, this.visualTC_Loco / 6f);
					for (int k = 0; k < this.needleTCs.Count; k++)
					{
						bool flag12 = this.needleTCs[k].transform != null;
						if (flag12)
						{
							this.needleTCs[k].transform.localEulerAngles = this.needleTCs[k].initEuler + new Vector3(0f, 0f, num2);
						}
					}
				}
				bool flag13 = this.needleURs.Count > 0;
				if (flag13)
				{
					float num3 = Mathf.Lerp(0f, 172f, this.virtualER / 10f);
					for (int l = 0; l < this.needleURs.Count; l++)
					{
						bool flag14 = this.needleURs[l].transform != null;
						if (flag14)
						{
							this.needleURs[l].transform.localEulerAngles = this.needleURs[l].initEuler + new Vector3(0f, 0f, num3);
						}
					}
				}
				bool flag15 = this.needleTMs.Count > 0;
				if (flag15)
				{
					float num4 = Mathf.Lerp(0f, 174f, this.visualTM / 10f);
					for (int m = 0; m < this.needleTMs.Count; m++)
					{
						bool flag16 = this.needleTMs[m].transform != null;
						if (flag16)
						{
							this.needleTMs[m].transform.localEulerAngles = this.needleTMs[m].initEuler + new Vector3(0f, 0f, num4);
						}
					}
				}
				bool flag17 = this.needlePMs.Count > 0;
				if (flag17)
				{
					float num5 = Mathf.Lerp(0f, 174f, num / 10f);
					for (int n = 0; n < this.needlePMs.Count; n++)
					{
						bool flag18 = this.needlePMs[n].transform != null;
						if (flag18)
						{
							this.needlePMs[n].transform.localEulerAngles = this.needlePMs[n].initEuler + new Vector3(0f, 0f, num5);
						}
					}
				}
			}
		}

		// Token: 0x04000046 RID: 70
		private int state = 1;

		// Token: 0x04000047 RID: 71
		private int lastState = 1;

		// Token: 0x04000048 RID: 72
		private int lastIndState = 0;

		// Token: 0x04000049 RID: 73
		private bool isInitialized = false;

		// Token: 0x0400004A RID: 74
		private bool isCompressorRunning = false;

		// Token: 0x0400004B RID: 75
		private bool blowdownInitialized = false;

		// Token: 0x0400004C RID: 76
		private TrainCar car;

		// Token: 0x0400004D RID: 77
		private BrakeSystem bs;

		// Token: 0x0400004E RID: 78
		private AudioSource compressorSource;

		// Token: 0x0400004F RID: 79
		private AudioSource click395Source;

		// Token: 0x04000050 RID: 80
		private AudioSource hiss395Source;

		// Token: 0x04000051 RID: 81
		private AudioSource charge395Source;

		// Token: 0x04000052 RID: 82
		private AudioSource click254Source;

		// Token: 0x04000053 RID: 83
		private AudioSource airCharge254Source;

		// Token: 0x04000054 RID: 84
		private AudioSource airRelease254Source;

		// Token: 0x04000055 RID: 85
		private Dictionary<string, FastPort> fPorts = new Dictionary<string, FastPort>(StringComparer.OrdinalIgnoreCase);

		// Token: 0x04000056 RID: 86
		private const float NOMINAL_BP = 5.2f;

		// Token: 0x04000057 RID: 87
		private const float MAX_BC = 4f;

		// Token: 0x04000058 RID: 88
		private const float LEAK_RATE = 0.015f;

		// Token: 0x04000059 RID: 89
		private float virtualER = 5.2f;

		// Token: 0x0400005A RID: 90
		private float visualTM = 5.2f;

		// Token: 0x0400005B RID: 91
		private float visualTC_Train = 0f;

		// Token: 0x0400005C RID: 92
		private float visualTC_Loco = 0f;

		// Token: 0x0400005D RID: 93
		private float tcRate = 0f;

		// Token: 0x0400005E RID: 94
		private float smoothTcRate = 0f;

		// Token: 0x0400005F RID: 95
		private List<GaugeData> needleURs = new List<GaugeData>();

		// Token: 0x04000060 RID: 96
		private List<GaugeData> needleTMs = new List<GaugeData>();

		// Token: 0x04000061 RID: 97
		private List<GaugeData> needleTCs = new List<GaugeData>();

		// Token: 0x04000062 RID: 98
		private List<GaugeData> needlePMs = new List<GaugeData>();

		// Token: 0x04000063 RID: 99
		private float lastNeedleSearchTime = 0f;

		private float lastEradicateTime = 0f;

		// Token: 0x04000064 RID: 100
		private bool wasInteriorLoaded = false;

		// Token: 0x04000065 RID: 101
		private List<Component> compBuffer = new List<Component>(32);

		// Token: 0x04000066 RID: 102
		private List<Transform> allTransformsBuffer = new List<Transform>(256);

		// Token: 0x04000067 RID: 103
		private List<Transform> interiorTransformsBuffer = new List<Transform>(256);

		// Token: 0x04000068 RID: 104
		private List<AudioSource> audioSourceBuffer = new List<AudioSource>(64);

		// Token: 0x04000069 RID: 105
		private List<AudioSource> interiorAudioBuffer = new List<AudioSource>(64);

		// Token: 0x0400006A RID: 106
		private ParticleSystem[] blowdownVFX = new ParticleSystem[4];

		// Token: 0x0400006B RID: 107
		private AudioSource[] blowdownAudio = new AudioSource[4];

		// Token: 0x0400006C RID: 108
		private string[] blowdownPorts = new string[]
		{
			"produv1.EXT_IN",
			"produv2.EXT_IN",
			"produv3.EXT_IN",
			"produv4.EXT_IN"
		};

		// Token: 0x0400006D RID: 109
		private float[] blowdownTimers = new float[4];
	}
}
