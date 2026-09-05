using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;

namespace M62Logic
{
	public static class Main
	{
		public static UnityModManager.ModEntry mod;

		public static M62ModSettings Settings { get; private set; }

		private static Harmony harmony;
		private static GameObject audioManagerGO;
		private static Assembly signalsAssembly;
		private static MethodInfo signalsUnloadMethod;
		private static MethodInfo signalsRefreshMethod;

		public static void RefreshSignalsProvider()
		{
			if (signalsRefreshMethod != null)
			{
				signalsRefreshMethod.Invoke(null, null);
			}
		}

		public static bool Load(UnityModManager.ModEntry modEntry)
		{
			mod = modEntry;
			try
			{
				Settings = UnityModManager.ModSettings.Load<M62ModSettings>(modEntry) ?? new M62ModSettings();
				Settings.Clamp();
				harmony = new Harmony(modEntry.Info.Id);
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				mod.Logger.Log("[M62] Ядро логики V14.0 (EPK UKBM phase 1) активировано!");
				audioManagerGO = new GameObject("M62AudioManager");
				Object.DontDestroyOnLoad(audioManagerGO);
				audioManagerGO.AddComponent<M62AudioManager>();
				TryLoadSignalsAddon(modEntry);
				SeasonsIntegrationBootstrap.Load(modEntry, Settings);
				modEntry.OnGUI = OnGui;
				modEntry.OnSaveGUI = OnSaveGui;
				modEntry.OnUnload = entry => Unload(entry);
			}
			catch (Exception ex)
			{
				mod.Logger.Log(string.Format("[M62] [Ошибка] Критическая ошибка при загрузке: {0}", ex));
				return false;
			}
			return true;
		}

		private static void OnGui(UnityModManager.ModEntry modEntry)
		{
			if (Settings == null)
			{
				return;
			}

			Settings.Clamp();
			GUILayout.Label("Температура окружающей среды", Array.Empty<GUILayoutOption>());
			Settings.UseDvSeasonsAmbient = GUILayout.Toggle(Settings.UseDvSeasonsAmbient, "Использовать DV Seasons (если установлен)", Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Fallback ambient, °C:", GUILayout.Width(160f));
			Settings.FallbackAmbientCelsius = GUILayout.HorizontalSlider(Settings.FallbackAmbientCelsius, -30f, 40f, GUILayout.Width(180f));
			GUILayout.Label(Settings.FallbackAmbientCelsius.ToString("F0"), GUILayout.Width(40f));
			GUILayout.EndHorizontal();
			Settings.ColdStartEnabled = GUILayout.Toggle(Settings.ColdStartEnabled, "Cold start: вода/масло = ambient при spawn", Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Смещение масла cold start, °C:", GUILayout.Width(200f));
			Settings.ColdStartOilOffset = GUILayout.HorizontalSlider(Settings.ColdStartOilOffset, -10f, 10f, GUILayout.Width(160f));
			GUILayout.Label(Settings.ColdStartOilOffset.ToString("F0"), GUILayout.Width(40f));
			GUILayout.EndHorizontal();
			Settings.ShowAmbientInGui = GUILayout.Toggle(Settings.ShowAmbientInGui, "Показывать текущий ambient (debug)", Array.Empty<GUILayoutOption>());
			if (Settings.ShowAmbientInGui)
			{
				float ambient = AmbientTemperatureRegistry.GetEffectiveAmbientCelsius();
				string source = AmbientTemperatureRegistry.LastReadFromSeasons ? "DV Seasons" : "fallback";
				GUILayout.Label(string.Format("Текущий ambient: {0:F1} °C ({1})", ambient, source), Array.Empty<GUILayoutOption>());
			}
		}

		private static void OnSaveGui(UnityModManager.ModEntry modEntry)
		{
			if (Settings != null)
			{
				Settings.Save(modEntry);
			}
		}

		private static void TryLoadSignalsAddon(UnityModManager.ModEntry modEntry)
		{
			string path = Path.Combine(modEntry.Path, "M62Logic.Signals.dll");
			if (!File.Exists(path))
			{
				modEntry.Logger.Log("[M62] M62Logic.Signals.dll not found — sign-based ALSN only.");
				return;
			}

			try
			{
				signalsAssembly = Assembly.LoadFile(path);
				Type bootstrapType = signalsAssembly.GetType("M62Logic.Signals.SignalsBootstrap");
				bootstrapType.GetMethod("Load").Invoke(null, new object[] { modEntry });
				signalsUnloadMethod = bootstrapType.GetMethod("Unload");
				signalsRefreshMethod = bootstrapType.GetMethod("Refresh");
				modEntry.Logger.Log("[M62] M62Logic.Signals.dll loaded.");
			}
			catch (Exception ex)
			{
				modEntry.Logger.Warning("[M62] Signals addon not loaded: " + ex.Message);
				signalsAssembly = null;
				signalsUnloadMethod = null;
				signalsRefreshMethod = null;
			}
		}

		private static bool Unload(UnityModManager.ModEntry modEntry)
		{
			try
			{
				SeasonsIntegrationBootstrap.Unload();
				if (signalsUnloadMethod != null)
				{
					signalsUnloadMethod.Invoke(null, null);
					signalsUnloadMethod = null;
					signalsRefreshMethod = null;
					signalsAssembly = null;
				}

				if (harmony != null)
				{
					harmony.UnpatchAll(modEntry.Info.Id);
					harmony = null;
				}

				if (audioManagerGO != null)
				{
					Object.Destroy(audioManagerGO);
					audioManagerGO = null;
				}

				Settings = null;
				mod = null;
			}
			catch (Exception ex)
			{
				mod.Logger.Log(string.Format("[M62] [Ошибка] Ошибка при выгрузке: {0}", ex));
			}
			return true;
		}
	}
}
