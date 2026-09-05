using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace M62Logic
{
	public static class SeasonsIntegrationBootstrap
	{
		private static UnityModManager.ModEntry modEntry;
		private static DvSeasonsAmbientProvider seasonsProvider;
		private static bool subscribed;
		private static bool worldHooked;
		private static GameObject retryHostGo;
		private static bool loggedActive;

		public static void Load(UnityModManager.ModEntry entry, M62ModSettings settings)
		{
			modEntry = entry;
			AmbientTemperatureRegistry.Settings = settings;
			seasonsProvider = new DvSeasonsAmbientProvider();
			Subscribe();
			EnsureRetryHost();
			HookWorldStreaming();
			TryRegister();
		}

		public static void Unload()
		{
			AmbientTemperatureRegistry.SeasonsProvider = null;
			AmbientTemperatureRegistry.Settings = null;
			seasonsProvider = null;
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

			DvSeasonsAmbientProvider.ResetReflectionCache();
			loggedActive = false;
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

			retryHostGo = new GameObject("M62SeasonsRetryHost");
			Object.DontDestroyOnLoad(retryHostGo);
			retryHostGo.AddComponent<SeasonsRetryHost>();
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
			if (entry == null || entry.Info == null)
			{
				return;
			}

			if (entry.Info.AssemblyName == "DVSeasons" || entry.Info.Id == "M62_Logic")
			{
				if (!newState && entry.Info.AssemblyName == "DVSeasons")
				{
					DvSeasonsAmbientProvider.ResetReflectionCache();
				}

				TryRegister();
			}
		}

		private static void TryRegister()
		{
			if (seasonsProvider == null)
			{
				AmbientTemperatureRegistry.SeasonsProvider = null;
				return;
			}

			if (!DvSeasonsAmbientProvider.IsDvSeasonsModActive())
			{
				AmbientTemperatureRegistry.SeasonsProvider = null;
				return;
			}

			float temp;
			if (seasonsProvider.TryGetAmbientCelsius(out temp))
			{
				AmbientTemperatureRegistry.SeasonsProvider = seasonsProvider;
				if (modEntry != null && !loggedActive)
				{
					loggedActive = true;
					modEntry.Logger.Log(string.Format("[M62] DV Seasons ambient provider active ({0:F1} °C).", temp));
				}

				return;
			}

			AmbientTemperatureRegistry.SeasonsProvider = seasonsProvider;
		}
	}

	internal class SeasonsRetryHost : MonoBehaviour
	{
		private float nextRetry;

		private void Update()
		{
			if (AmbientTemperatureRegistry.IsSeasonsAmbientAvailable())
			{
				return;
			}

			if (Time.unscaledTime < this.nextRetry)
			{
				return;
			}

			this.nextRetry = Time.unscaledTime + 2f;
			SeasonsIntegrationBootstrap.Refresh();
		}
	}
}
