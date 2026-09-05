using System;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace M62Logic
{
	public sealed class DvSeasonsAmbientProvider : IAmbientTemperatureProvider
	{
		private static FieldInfo runtimeField;
		private static PropertyInfo currentStateProp;
		private static PropertyInfo temperatureProp;
		private static bool reflectionResolved;
		private static bool reflectionAvailable;

		public static bool IsDvSeasonsModActive()
		{
			if (UnityModManager.modEntries != null)
			{
				foreach (UnityModManager.ModEntry entry in UnityModManager.modEntries)
				{
					if (entry != null && entry.Active && entry.Info != null && entry.Info.AssemblyName == "DVSeasons")
					{
						return true;
					}
				}
			}

			return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "DVSeasons");
		}

		public static void ResetReflectionCache()
		{
			runtimeField = null;
			currentStateProp = null;
			temperatureProp = null;
			reflectionResolved = false;
			reflectionAvailable = false;
		}

		public bool TryGetAmbientCelsius(out float temp)
		{
			temp = DefaultAmbientProvider.DefaultCelsius;
			if (!IsDvSeasonsModActive())
			{
				return false;
			}

			if (!EnsureReflection())
			{
				return false;
			}

			try
			{
				object runtime = runtimeField.GetValue(null);
				if (runtime == null)
				{
					return false;
				}

				object state = currentStateProp.GetValue(runtime, null);
				if (state == null)
				{
					return false;
				}

				object value = temperatureProp.GetValue(state, null);
				if (value == null)
				{
					return false;
				}

				temp = Convert.ToSingle(value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool EnsureReflection()
		{
			if (reflectionResolved)
			{
				return reflectionAvailable;
			}

			reflectionResolved = true;
			try
			{
				Assembly seasonsAssembly = AppDomain.CurrentDomain.GetAssemblies()
					.FirstOrDefault(a => a.GetName().Name == "DVSeasons");
				if (seasonsAssembly == null)
				{
					return false;
				}

				Type mainType = seasonsAssembly.GetType("DVSeasons.Mod.Main", false);
				if (mainType == null)
				{
					return false;
				}

				runtimeField = mainType.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic);
				if (runtimeField == null)
				{
					return false;
				}

				Type runtimeType = runtimeField.FieldType;
				currentStateProp = runtimeType.GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public);
				if (currentStateProp == null)
				{
					return false;
				}

				Type stateType = currentStateProp.PropertyType;
				temperatureProp = stateType.GetProperty("TemperatureCelsius", BindingFlags.Instance | BindingFlags.Public);
				if (temperatureProp == null)
				{
					return false;
				}

				reflectionAvailable = true;
			}
			catch
			{
				reflectionAvailable = false;
			}

			return reflectionAvailable;
		}
	}
}
