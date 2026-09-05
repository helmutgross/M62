using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace M62Logic
{
	public class EpkLogic
	{
		private const float VzhCheck = 65f;
		private const float VzhAt = 80f;
		private const float VkzhAt = 60f;
		private const float Vk = 20f;
		private const float VwhiteMax = 60f;
		private const float EmergencyDelaySec = 7f;
		private const float ShortWhistleSec = 1f;
		private const float CoastSpeedKmh = 5f;
		private const float StandstillSpeedKmh = 5f;
		private const float StandstillBrakePressure = 1f;

		private int currentLamp = -1;
		private float epkTimer;
		private float periodicTimer;
		private float shortWhistleTimer;
		private float targetPeriodicTime = 105f;
		private bool isEpkBlowing;
		private bool isShortWhistle;
		private bool isEmergency;

		public void Reset()
		{
			isEmergency = false;
			isEpkBlowing = false;
			isShortWhistle = false;
			epkTimer = 0f;
			periodicTimer = 0f;
			shortWhistleTimer = 0f;
			currentLamp = -1;
			targetPeriodicTime = GetNextPeriodicInterval(AlsnLampCode.Green);
		}

		public void Tick(int lampCode, float speedKmh, bool rbPressed, bool rbPressedEdge, bool epkEnabledEdge, float throttle, float brakeCylinderPressure, float dt, Action<string, float> setVal)
		{
			if (isEmergency)
			{
				setVal("EPK_control.EXT_IN", 1f);
				setVal("EPK_Zvuk.EXT_IN", 1f);
				setVal("TrainBrake.EXT_IN", 1f);
				setVal("throttle.EXT_IN", 0f);
				return;
			}

			if (isShortWhistle)
			{
				setVal("EPK_control.EXT_IN", 1f);
				setVal("EPK_Zvuk.EXT_IN", 1f);
				shortWhistleTimer += dt;
				if (shortWhistleTimer >= ShortWhistleSec)
				{
					isShortWhistle = false;
					shortWhistleTimer = 0f;
					setVal("EPK_control.EXT_IN", 0f);
					setVal("EPK_Zvuk.EXT_IN", 0f);
				}

				currentLamp = lampCode;
				return;
			}

			bool overspeed = IsOverspeed(lampCode, speedKmh);
			bool nonCancellableOverspeed = IsNonCancellableOverspeed(lampCode, speedKmh);
			bool coasting = speedKmh > CoastSpeedKmh && throttle < 0.05f && brakeCylinderPressure < 1.5f;
			bool shouldBlow = false;
			bool startShortWhistle = epkEnabledEdge;

			if (currentLamp != -1 && currentLamp != lampCode)
			{
				if (currentLamp == AlsnLampCode.White && lampCode == AlsnLampCode.Green)
				{
					startShortWhistle = true;
				}
				else if (lampCode == AlsnLampCode.White)
				{
					startShortWhistle = true;
				}
				else
				{
					shouldBlow = ShouldBlowOnLampChange(currentLamp, lampCode, speedKmh, rbPressed);
				}
			}

			currentLamp = lampCode;

			if (startShortWhistle)
			{
				isShortWhistle = true;
				shortWhistleTimer = 0f;
				setVal("EPK_control.EXT_IN", 1f);
				setVal("EPK_Zvuk.EXT_IN", 1f);
				return;
			}

			if (coasting && lampCode != AlsnLampCode.Green && lampCode != AlsnLampCode.White)
			{
				shouldBlow = true;
			}

			if (IsPeriodicActive(lampCode, speedKmh, brakeCylinderPressure))
			{
				periodicTimer += dt;
				if (periodicTimer >= targetPeriodicTime)
				{
					shouldBlow = true;
					periodicTimer = 0f;
					targetPeriodicTime = GetNextPeriodicInterval(lampCode);
				}
			}
			else
			{
				periodicTimer = 0f;
			}

			if (overspeed)
			{
				shouldBlow = true;
			}

			if (shouldBlow && rbPressed && !nonCancellableOverspeed && !overspeed)
			{
				shouldBlow = false;
			}

			if (shouldBlow)
			{
				isEpkBlowing = true;
			}

			if (isEpkBlowing)
			{
				setVal("EPK_control.EXT_IN", 1f);
				setVal("EPK_Zvuk.EXT_IN", 1f);
				epkTimer += dt;

				if (rbPressedEdge && !nonCancellableOverspeed && !overspeed)
				{
					isEpkBlowing = false;
					epkTimer = 0f;
					periodicTimer = 0f;
					targetPeriodicTime = GetNextPeriodicInterval(lampCode);
					setVal("EPK_control.EXT_IN", 0f);
					setVal("EPK_Zvuk.EXT_IN", 0f);
				}
				else if (epkTimer >= EmergencyDelaySec)
				{
					isEmergency = true;
				}
			}
			else
			{
				setVal("EPK_control.EXT_IN", 0f);
				setVal("EPK_Zvuk.EXT_IN", 0f);
				epkTimer = 0f;
			}
		}

		private static bool ShouldBlowOnLampChange(int fromLamp, int toLamp, float speedKmh, bool rbPressed)
		{
			if (rbPressed)
			{
				return false;
			}

			if (toLamp == AlsnLampCode.Green)
			{
				return false;
			}

			if (fromLamp == AlsnLampCode.Green && toLamp == AlsnLampCode.Yellow && speedKmh <= VzhCheck)
			{
				return false;
			}

			if (GetPermissiveness(toLamp) > GetPermissiveness(fromLamp))
			{
				return false;
			}

			return true;
		}

		private static int GetPermissiveness(int lampCode)
		{
			switch (lampCode)
			{
				case AlsnLampCode.Red:
					return 0;
				case AlsnLampCode.YellowRed:
					return 1;
				case AlsnLampCode.Yellow:
					return 2;
				case AlsnLampCode.White:
					return 3;
				case AlsnLampCode.Green:
					return 4;
				default:
					return 0;
			}
		}

		private static bool IsPeriodicActive(int lampCode, float speedKmh, float brakeCylinderPressure)
		{
			if (lampCode == AlsnLampCode.Green || lampCode == AlsnLampCode.White)
			{
				return false;
			}

			if (speedKmh < StandstillSpeedKmh && brakeCylinderPressure >= StandstillBrakePressure)
			{
				return false;
			}

			switch (lampCode)
			{
				case AlsnLampCode.Yellow:
				case AlsnLampCode.YellowRed:
					return true;
				case AlsnLampCode.Red:
					return speedKmh < Vk;
				default:
					return false;
			}
		}

		private static bool IsOverspeed(int lampCode, float speedKmh)
		{
			switch (lampCode)
			{
				case AlsnLampCode.Yellow:
					return speedKmh > VzhCheck;
				case AlsnLampCode.YellowRed:
					return speedKmh > VkzhAt;
				case AlsnLampCode.Red:
					return speedKmh > Vk;
				case AlsnLampCode.White:
					return speedKmh > VwhiteMax;
				default:
					return false;
			}
		}

		private static bool IsNonCancellableOverspeed(int lampCode, float speedKmh)
		{
			switch (lampCode)
			{
				case AlsnLampCode.Yellow:
					return speedKmh > VzhAt;
				case AlsnLampCode.YellowRed:
					return speedKmh > VkzhAt;
				case AlsnLampCode.Red:
					return speedKmh > Vk;
				default:
					return false;
			}
		}

		private static float GetNextPeriodicInterval(int lampCode)
		{
			switch (lampCode)
			{
				case AlsnLampCode.Green:
					return Random.Range(90f, 120f);
				default:
					return Random.Range(30f, 40f);
			}
		}
	}
}
