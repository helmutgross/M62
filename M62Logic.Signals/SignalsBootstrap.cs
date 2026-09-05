using System;
using System.Linq;
using System.Reflection;
using M62Logic;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace M62Logic.Signals
{
	public static class SignalsBootstrap
	{
		private static UnityModManager.ModEntry modEntry;
		private static DvSignalsAlsnProvider provider;
		private static bool subscribed;
		private static bool worldHooked;
		private static GameObject retryHostGo;

		public static void Load(UnityModManager.ModEntry entry)
		{
			modEntry = entry;
			Subscribe();
			EnsureRetryHost();
			HookWorldStreaming();
			TryRegister();
		}

		public static void Unload()
		{
			AlsnLampRegistry.Provider = null;
			provider = null;
			UnhookWorldStreaming();
			if (retryHostGo != null)
			{
				Object.Destroy(retryHostGo);
				retryHostGo = null;
			}
			if (subscribed)
			{
				UnityModManager.toggleModsListen -= OnModToggled;
				subscribed = false;
			}

			modEntry = null;
		}

		public static void Refresh()
		{
			TryRegister();
		}

		private static void Subscribe()
		{
			if (subscribed)
			{
				return;
			}

			UnityModManager.toggleModsListen += OnModToggled;
			subscribed = true;
		}

		private static void EnsureRetryHost()
		{
			if (retryHostGo != null)
			{
				return;
			}

			retryHostGo = new GameObject("M62SignalsRetryHost");
			Object.DontDestroyOnLoad(retryHostGo);
			retryHostGo.AddComponent<SignalsRetryHost>();
		}

		private static void HookWorldStreaming()
		{
			if (worldHooked)
			{
				return;
			}

			WorldStreamingInit.LoadingStatusChanged += OnWorldLoadingChanged;
			worldHooked = true;
		}

		private static void UnhookWorldStreaming()
		{
			if (!worldHooked)
			{
				return;
			}

			WorldStreamingInit.LoadingStatusChanged -= OnWorldLoadingChanged;
			worldHooked = false;
		}

		private static void OnWorldLoadingChanged(string msg, bool isError, float percent)
		{
			TryRegister();
		}

		private static void OnModToggled(UnityModManager.ModEntry entry, bool newState)
		{
			if (entry.Info.Id == "wiz.signals" || entry.Info.Id == "M62_Logic")
			{
				TryRegister();
			}
		}

		private static void TryRegister()
		{
			if (!IsDvSignalsReady())
			{
				AlsnLampRegistry.Provider = null;
				return;
			}

			try
			{
				if (provider == null)
				{
					provider = new DvSignalsAlsnProvider();
				}

				AlsnLampRegistry.Provider = provider;
				if (modEntry != null)
				{
					modEntry.Logger.Log("[M62] DV Signals ALSN provider active.");
				}
			}
			catch (Exception)
			{
				AlsnLampRegistry.Provider = null;
			}
		}

		private static bool IsDvSignalsReady()
		{
			Assembly signalsGame = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Signals.Game");
			if (signalsGame == null)
			{
				return false;
			}

			Type managerType = signalsGame.GetType("Signals.Game.SignalManager");
			if (managerType == null)
			{
				return false;
			}

			PropertyInfo runningProp = managerType.GetProperty("Running", BindingFlags.Public | BindingFlags.Static);
			return runningProp != null && (bool)runningProp.GetValue(null, null);
		}
	}
}
