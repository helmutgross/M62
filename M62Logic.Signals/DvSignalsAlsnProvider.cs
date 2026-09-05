using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using M62Logic;
using UnityEngine;

namespace M62Logic.Signals
{
	public sealed class DvSignalsAlsnProvider : IAlsnLampProvider
	{
		private const float AheadDotThreshold = 0.15f;
		private readonly Type trackDirectionType;
		private readonly Type junctionSignalControllerType;
		private readonly MethodInfo getTracksMethod;
		private readonly MethodInfo getControllerSignalMethod;
		private readonly PropertyInfo isOffProp;
		private readonly PropertyInfo signalDefinitionProp;
		private readonly FieldInfo controllerInfoSignalField;
		private readonly FieldInfo controllerInfoDeadEndField;
		private readonly FieldInfo controllerInfoSelfLoopField;
		private readonly PropertyInfo trackInfoLengthProp;
		private readonly MethodInfo getTrackLengthMethod;

		public DvSignalsAlsnProvider()
		{
			Assembly signalsGame = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Signals.Game");
			Type trackWalkerType = signalsGame.GetType("Signals.Game.Railway.TrackWalker");
			trackDirectionType = signalsGame.GetType("Signals.Game.TrackDirection");
			junctionSignalControllerType = signalsGame.GetType("Signals.Game.Controllers.JunctionSignalController");
			getTracksMethod = trackWalkerType
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(m => m.Name == "GetTracksUntilMainSignal" && m.GetParameters().Length == 4);
			Type controllerType = signalsGame.GetType("Signals.Game.Controllers.BasicSignalController");
			getControllerSignalMethod = controllerType.GetMethod("GetControllerSignal", BindingFlags.Public | BindingFlags.Instance);
			Type controllerInfoType = trackWalkerType.GetNestedType("ControllerInfo");
			controllerInfoSignalField = controllerInfoType.GetField("Signal");
			controllerInfoDeadEndField = controllerInfoType.GetField("IsDeadEnd");
			controllerInfoSelfLoopField = controllerInfoType.GetField("IsSelfLoop");
			Type signalType = signalsGame.GetType("Signals.Game.Signal");
			isOffProp = signalType.GetProperty("IsOff", BindingFlags.Public | BindingFlags.Instance);
			signalDefinitionProp = signalType.GetProperty("Definition", BindingFlags.Public | BindingFlags.Instance);
			Type trackInfoType = signalsGame.GetType("Signals.Game.Railway.TrackInfo");
			trackInfoLengthProp = trackInfoType.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
			Type extensionsType = signalsGame.GetType("Signals.Game.Extensions");
			getTrackLengthMethod = extensionsType?.GetMethod("GetLength", BindingFlags.Public | BindingFlags.Static);
		}

		public bool TryGetLampState(TrainCar car, out AlsnTrackState state)
		{
			state = default(AlsnTrackState);
			try
			{
				if (car == null || car.Bogies == null || car.Bogies.Length == 0)
				{
					return false;
				}

				bool forward = car.GetForwardSpeed() >= -0.1f;
				Bogie bogie = forward ? car.Bogies[0] : car.Bogies[car.Bogies.Length - 1];
				if (bogie.track == null || bogie.traveller == null)
				{
					return false;
				}

				bool alongPositive = bogie.TrackDirectionSign > 0f == forward;
				object trackDirection = Enum.ToObject(trackDirectionType, alongPositive ? 0 : 1);
				object ignoreController = null;
				object info = null;
				IList tracks = null;

				for (int attempt = 0; attempt < 16; attempt++)
				{
					object[] args = new object[] { bogie.track, trackDirection, ignoreController, null };
					tracks = getTracksMethod.Invoke(null, args) as IList;
					info = args[3];
					if (info == null)
					{
						break;
					}

					object foundController = controllerInfoSignalField.GetValue(info);
					if (foundController == null)
					{
						break;
					}

					object foundSignal = getControllerSignalMethod.Invoke(foundController, null);
					bool isJunction = junctionSignalControllerType != null && junctionSignalControllerType.IsInstanceOfType(foundController);
					bool isBehind = !IsSignalAheadOfTrain(bogie, car, forward, foundSignal);

					if (!isJunction && !isBehind)
					{
						break;
					}

					ignoreController = foundController;
				}

				if (info == null)
				{
					state = new AlsnTrackState(AlsnLampCode.White, 0f, 0f, 0f, true);
					return true;
				}

				object controller = controllerInfoSignalField.GetValue(info);
				bool isDeadEnd = (bool)controllerInfoDeadEndField.GetValue(info);
				bool isSelfLoop = (bool)controllerInfoSelfLoopField.GetValue(info);
				float distance = GetDistanceToController(bogie, alongPositive, tracks);

				int lampCode = AlsnLampCode.White;
				bool hasSignalAhead = controller != null && !isDeadEnd && !isSelfLoop;

				if (hasSignalAhead)
				{
					object signal = getControllerSignalMethod.Invoke(controller, null);
					if (signal != null && !(bool)isOffProp.GetValue(signal, null))
					{
						lampCode = AspectToAlsnMapper.MapNextSignal(signal);
					}
				}

				state = new AlsnTrackState(lampCode, 0f, 0f, distance, true);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool IsSignalAheadOfTrain(Bogie bogie, TrainCar car, bool movingForward, object signal)
		{
			if (signal == null || bogie == null || car == null || signalDefinitionProp == null)
			{
				return true;
			}

			object definition = signalDefinitionProp.GetValue(signal, null);
			Component definitionBehaviour = definition as Component;
			if (definitionBehaviour == null)
			{
				return true;
			}

			Vector3 trainForward = movingForward ? car.transform.forward : -car.transform.forward;
			Vector3 toSignal = definitionBehaviour.transform.position - bogie.transform.position;
			trainForward.y = 0f;
			toSignal.y = 0f;
			if (trainForward.sqrMagnitude < 0.0001f || toSignal.sqrMagnitude < 0.0001f)
			{
				return true;
			}

			return Vector3.Dot(trainForward.normalized, toSignal.normalized) > AheadDotThreshold;
		}

		private float GetDistanceToController(Bogie bogie, bool alongPositive, IList tracks)
		{
			double trackLength = GetTrackLength(bogie.track);
			double span = bogie.traveller.Span;
			float distance = (float)(alongPositive ? (trackLength - span) : span);
			if (tracks != null && trackInfoLengthProp != null)
			{
				for (int i = 0; i < tracks.Count; i++)
				{
					object trackInfo = tracks[i];
					if (trackInfo == null)
					{
						continue;
					}

					object lengthValue = trackInfoLengthProp.GetValue(trackInfo, null);
					if (lengthValue != null)
					{
						distance += Convert.ToSingle(lengthValue);
					}
				}
			}

			return distance;
		}

		private double GetTrackLength(RailTrack track)
		{
			if (getTrackLengthMethod == null || track == null)
			{
				return 0.0;
			}

			return (double)getTrackLengthMethod.Invoke(null, new object[] { track });
		}
	}
}
