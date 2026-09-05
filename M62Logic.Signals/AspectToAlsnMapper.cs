using System;
using System.Reflection;
using M62Logic;
using UnityEngine;

namespace M62Logic.Signals
{
	/// <summary>
	/// Maps the next DV Signals aspect (optical lamps) to M62 ALSN cab codes (RZD-style duplicate).
	/// </summary>
	internal static class AspectToAlsnMapper
	{
		private struct LampProfile
		{
			public bool HasGreen;
			public bool HasYellow;
			public bool HasRed;
			public bool HasWhite;
			public bool HasAny;
		}

		/// <summary>
		/// Duplicate the next signal's displayed aspect onto ALSN. White only when aspect has no lamps.
		/// </summary>
		public static int MapNextSignal(object signal)
		{
			LampProfile lamps = CollectLampProfile(signal);
			if (!lamps.HasAny)
			{
				return AlsnLampCode.White;
			}

			return MapFromLampProfile(lamps);
		}

		private static int MapFromLampProfile(LampProfile profile)
		{
			if (profile.HasRed)
			{
				return AlsnLampCode.Red;
			}

			if (profile.HasGreen)
			{
				return AlsnLampCode.Green;
			}

			if (profile.HasYellow)
			{
				return AlsnLampCode.Yellow;
			}

			if (profile.HasWhite)
			{
				return AlsnLampCode.White;
			}

			return AlsnLampCode.White;
		}

		private static LampProfile CollectLampProfile(object signal)
		{
			try
			{
				object aspect = GetCurrentAspect(signal);
				if (aspect != null)
				{
					LampProfile runtime = default(LampProfile);
					CollectLampProfileFromRuntimeAspect(aspect, ref runtime);
					if (runtime.HasAny)
					{
						return runtime;
					}
				}

				LampProfile configured = default(LampProfile);
				CollectLampProfileFromConfiguredAspect(signal, ref configured);
				return configured;
			}
			catch
			{
				return default(LampProfile);
			}
		}

		private static void CollectLampProfileFromDefinition(object definition, ref LampProfile profile)
		{
			if (definition == null)
			{
				return;
			}

			Type defType = definition.GetType();
			CollectLampProfileFromLightArray(GetFieldIncludingBase(defType, "OnLights")?.GetValue(definition) as Array, ref profile);
			CollectLampProfileFromLightArray(GetFieldIncludingBase(defType, "BlinkingLights")?.GetValue(definition) as Array, ref profile);

			Array sequences = GetFieldIncludingBase(defType, "LightSequences")?.GetValue(definition) as Array;
			if (sequences != null)
			{
				for (int i = 0; i < sequences.Length; i++)
				{
					object sequence = sequences.GetValue(i);
					if (sequence == null)
					{
						continue;
					}

					CollectLampProfileFromLightArray(GetFieldIncludingBase(sequence.GetType(), "Lights")?.GetValue(sequence) as Array, ref profile);
				}
			}
		}

		private static void CollectLampProfileFromRuntimeAspect(object aspect, ref LampProfile profile)
		{
			CollectLampProfileFromSignalLights(GetInstanceField(aspect.GetType(), "_on")?.GetValue(aspect) as Array, ref profile);
			CollectLampProfileFromSignalLights(GetInstanceField(aspect.GetType(), "_blink")?.GetValue(aspect) as Array, ref profile);

			Array sequences = GetInstanceField(aspect.GetType(), "_sequences")?.GetValue(aspect) as Array;
			if (sequences == null)
			{
				return;
			}

			for (int i = 0; i < sequences.Length; i++)
			{
				object sequence = sequences.GetValue(i);
				if (sequence == null)
				{
					continue;
				}

				CollectLampProfileFromSignalLights(GetInstanceField(sequence.GetType(), "Lights")?.GetValue(sequence) as Array, ref profile);
			}
		}

		private static void CollectLampProfileFromConfiguredAspect(object signal, ref LampProfile profile)
		{
			object aspect = GetCurrentAspect(signal);
			if (aspect == null)
			{
				return;
			}

			MethodInfo getDefinition = aspect.GetType().GetMethod("GetDefinition", BindingFlags.Public | BindingFlags.Instance);
			if (getDefinition != null)
			{
				CollectLampProfileFromDefinition(getDefinition.Invoke(aspect, null), ref profile);
			}

			if (!profile.HasAny)
			{
				CollectLampProfileFromRuntimeAspect(aspect, ref profile);
			}
		}

		private static void CollectLampProfileFromLightArray(Array lightDefinitions, ref LampProfile profile)
		{
			if (lightDefinitions == null)
			{
				return;
			}

			for (int i = 0; i < lightDefinitions.Length; i++)
			{
				ApplyLightDefinitionColor(lightDefinitions.GetValue(i), ref profile);
			}
		}

		private static void CollectLampProfileFromSignalLights(Array signalLights, ref LampProfile profile)
		{
			if (signalLights == null)
			{
				return;
			}

			Type lightType = signalLights.GetType().GetElementType();
			FieldInfo definitionField = lightType?.GetField("Definition", BindingFlags.Instance | BindingFlags.Public);
			if (definitionField == null)
			{
				return;
			}

			PropertyInfo isActiveProp = lightType.GetProperty("IsActive", BindingFlags.Instance | BindingFlags.Public);
			FieldInfo internalStateField = GetInstanceField(lightType, "InternalState");

			for (int i = 0; i < signalLights.Length; i++)
			{
				object light = signalLights.GetValue(i);
				if (light == null || !IsSignalLightLit(light, isActiveProp, internalStateField))
				{
					continue;
				}

				ApplyLightDefinitionColor(definitionField.GetValue(light), ref profile);
			}
		}

		private static bool IsSignalLightLit(object light, PropertyInfo isActiveProp, FieldInfo internalStateField)
		{
			if (isActiveProp != null)
			{
				return (bool)isActiveProp.GetValue(light, null);
			}

			if (internalStateField != null)
			{
				int state = Convert.ToInt32(internalStateField.GetValue(light));
				return state == 1 || state == 2;
			}

			return true;
		}

		private static void ApplyLightDefinitionColor(object lightDefinition, ref LampProfile profile)
		{
			if (lightDefinition == null)
			{
				return;
			}

			FieldInfo colourField = lightDefinition.GetType().GetField("Colour", BindingFlags.Instance | BindingFlags.Public);
			if (colourField == null)
			{
				return;
			}

			ClassifyColor((Color)colourField.GetValue(lightDefinition), ref profile);
		}

		private static void ClassifyColor(Color color, ref LampProfile profile)
		{
			profile.HasAny = true;
			float r = color.r;
			float g = color.g;
			float b = color.b;

			if (r > 0.75f && g > 0.75f && b > 0.75f)
			{
				profile.HasWhite = true;
				return;
			}

			if (g > 0.4f && r > 0.4f && g >= b)
			{
				profile.HasYellow = true;
				return;
			}

			if (g > 0.35f && g >= r && g >= b)
			{
				profile.HasGreen = true;
				return;
			}

			if (r > 0.35f && r >= g && r >= b)
			{
				profile.HasRed = true;
			}
		}

		private static FieldInfo GetInstanceField(Type type, string fieldName)
		{
			while (type != null)
			{
				FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null)
				{
					return field;
				}

				type = type.BaseType;
			}

			return null;
		}

		private static FieldInfo GetFieldIncludingBase(Type type, string fieldName)
		{
			return GetInstanceField(type, fieldName);
		}

		private static object GetCurrentAspect(object signal)
		{
			if (signal == null)
			{
				return null;
			}

			PropertyInfo currentAspectProp = signal.GetType().GetProperty("CurrentAspect", BindingFlags.Public | BindingFlags.Instance);
			return currentAspectProp != null ? currentAspectProp.GetValue(signal, null) : null;
		}
	}
}
