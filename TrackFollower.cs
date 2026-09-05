using System;
using System.Collections.Generic;
using System.Reflection;
using DV.PointSet;
using UnityEngine;

namespace M62Logic
{
	// Token: 0x02000013 RID: 19
	public static class TrackFollower
	{
		// Token: 0x0600004B RID: 75 RVA: 0x000071F0 File Offset: 0x000053F0
		public static void GetSpeedLimits(TrainCar car, out float current, out float next, out float dist)
		{
			current = (next = (dist = 0f));
			bool flag = car == null || car.Bogies.Length == 0;
			if (!flag)
			{
				bool flag2 = car.GetForwardSpeed() >= -0.1f;
				Bogie bogie = (!flag2) ? car.Bogies[car.Bogies.Length - 1] : car.Bogies[0];
				bool flag3 = bogie.track == null;
				if (!flag3)
				{
					bool flag4 = bogie.TrackDirectionSign > 0f == flag2;
					IEnumerable<TrackEvent> enumerable = TrackFollower.ResolveJunctionSpeedLimits(TrackFollower.FollowTrack(bogie.track, bogie.traveller.Span, (!flag4) ? 5000.0 : -5000.0));
					foreach (TrackEvent trackEvent in enumerable)
					{
						SpeedLimitEvent speedLimitEvent = trackEvent as SpeedLimitEvent;
						bool flag5 = speedLimitEvent != null && speedLimitEvent.direction == flag4;
						if (flag5)
						{
							current = (float)speedLimitEvent.limit;
							break;
						}
					}
					IEnumerable<TrackEvent> enumerable2 = TrackFollower.ResolveJunctionSpeedLimits(TrackFollower.FollowTrack(bogie.track, bogie.traveller.Span, (!flag4) ? -3500.0 : 3500.0));
					foreach (TrackEvent trackEvent2 in enumerable2)
					{
						SpeedLimitEvent speedLimitEvent2 = trackEvent2 as SpeedLimitEvent;
						bool flag6 = speedLimitEvent2 != null && speedLimitEvent2.direction == flag4 && trackEvent2.span > 1.0;
						if (flag6)
						{
							next = (float)speedLimitEvent2.limit;
							dist = (float)trackEvent2.span;
							break;
						}
					}
				}
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000073E0 File Offset: 0x000055E0
		private static IEnumerable<TrackEvent> FollowTrack(RailTrack track, double startSpan, double distance)
		{
			double distanceFromStart = 0.0;
			bool travelDir = distance > 0.0;
			double absDist = Math.Abs(distance);
			for (int i = 0; i < 150; i++)
			{
				List<TrackEvent> trackEvents = TrackIndexer.GetTrackEvents(track);
				if (!travelDir)
				{
					for (int e = trackEvents.Count - 1; e >= 0; e--)
					{
						TrackEvent ev = trackEvents[e];
						double rel = startSpan - ev.span;
						bool flag = rel >= 0.0;
						if (flag)
						{
							double total = rel + distanceFromStart;
							bool flag2 = total > absDist;
							if (flag2)
							{
								yield break;
							}
							yield return ev.WithSpan(total);
						}
					}
				}
				else
				{
					for (int j = 0; j < trackEvents.Count; j++)
					{
						TrackEvent ev2 = trackEvents[j];
						double rel2 = ev2.span - startSpan;
						bool flag3 = rel2 >= 0.0;
						if (flag3)
						{
							double total2 = rel2 + distanceFromStart;
							bool flag4 = total2 > absDist;
							if (flag4)
							{
								yield break;
							}
							yield return ev2.WithSpan(total2);
						}
					}
				}
				Junction nb_j = travelDir ? track.outJunction : track.inJunction;
				Junction.Branch nb = travelDir ? track.GetOutBranch() : track.GetInBranch();
				object value = TrackFollower.psField.GetValue(track);
				EquiPointSet valueAsPointSet = value as EquiPointSet;
				double tLen = (valueAsPointSet != null) ? valueAsPointSet.span : 0.0;
				distanceFromStart += travelDir ? (tLen - startSpan) : startSpan;
				bool flag5 = nb == null || nb.track == null;
				if (flag5)
				{
					break;
				}
				bool flag6 = nb_j != null;
				if (flag6)
				{
					yield return new JunctionEvent(distanceFromStart, nb_j);
				}
				track = nb.track;
				double num;
				if (!nb.first)
				{
					object value2 = TrackFollower.psField.GetValue(track);
					EquiPointSet value2AsPointSet = value2 as EquiPointSet;
					num = (value2AsPointSet != null) ? value2AsPointSet.span : 0.0;
				}
				else
				{
					num = 0.0;
				}
				startSpan = num;
				bool flag7 = distanceFromStart > absDist;
				if (flag7)
				{
					break;
				}
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000073FE File Offset: 0x000055FE
		private static IEnumerable<TrackEvent> ResolveJunctionSpeedLimits(IEnumerable<TrackEvent> events)
		{
			List<TrackEvent> eventList = new List<TrackEvent>(events);
			for (int i = 0; i < eventList.Count; i++)
			{
				TrackEvent trackEvent = eventList[i];
				DualSpeedLimitEvent dual = trackEvent as DualSpeedLimitEvent;
				bool flag = dual != null;
				if (flag)
				{
					JunctionEvent nextJ = null;
					for (int j = i + 1; j < eventList.Count; j++)
					{
						trackEvent = eventList[j];
						JunctionEvent je = trackEvent as JunctionEvent;
						bool flag2 = je != null;
						if (flag2)
						{
							nextJ = je;
							break;
						}
					}
					bool flag3 = nextJ == null || nextJ.junction.selectedBranch <= 0;
					yield return new SpeedLimitEvent(dual.span, dual.direction, flag3 ? dual.limit : dual.rightLimit);
				}
				else
				{
					yield return eventList[i];
				}
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00007410 File Offset: 0x00005610
		public static float GetObstacleDistance(TrainCar loco, float maxSearchDist)
		{
			bool flag = loco == null || loco.trainset == null || loco.trainset.cars.Count == 0;
			float result;
			if (flag)
			{
				result = -1f;
			}
			else
			{
				bool flag2 = loco.GetForwardSpeed() >= -0.1f;
				TrainCar trainCar = (!flag2) ? loco.trainset.cars[loco.trainset.cars.Count - 1] : loco.trainset.cars[0];
				Bogie bogie = (!flag2) ? trainCar.Bogies[trainCar.Bogies.Length - 1] : trainCar.Bogies[0];
				bool flag3 = bogie.track == null;
				if (flag3)
				{
					result = -1f;
				}
				else
				{
					bool flag4 = bogie.TrackDirectionSign > 0f == flag2;
					RailTrack track = bogie.track;
					double num = bogie.traveller.Span;
					float num2 = 0f;
					Vector3 currentMove = WorldMover.currentMove;
					PointSetTraveller pointSetTraveller = null;
					RailTrack travellerTrack = null;
					for (int i = 0; i < 60; i++)
					{
						EquiPointSet equiPointSet = TrackFollower.psField.GetValue(track) as EquiPointSet;
						bool flag5 = equiPointSet == null;
						if (flag5)
						{
							break;
						}
						if (pointSetTraveller == null || travellerTrack != track)
						{
							pointSetTraveller = new PointSetTraveller(equiPointSet, false);
							travellerTrack = track;
						}
						double span = equiPointSet.span;
						float num3 = ObstacleSearchStepM;
						while (num2 < maxSearchDist)
						{
							num += (double)((!flag4) ? (-(double)num3) : num3);
							num2 += num3;
							bool flag6 = (!flag4) ? (num < 0.0) : (num > span);
							if (flag6)
							{
								break;
							}
							pointSetTraveller.MoveToSpan(num);
							int num4 = Physics.OverlapSphereNonAlloc((Vector3)pointSetTraveller.worldPosition + currentMove, 2.5f, TrackFollower.overlapColliders, 1024);
							for (int j = 0; j < num4; j++)
							{
								Collider collider = TrackFollower.overlapColliders[j];
								TrainCar componentInParent = collider.GetComponentInParent<TrainCar>();
								bool flag7 = componentInParent != null && componentInParent.trainset != loco.trainset;
								if (flag7)
								{
									return num2;
								}
							}
						}
						bool flag8 = num2 >= maxSearchDist;
						if (flag8)
						{
							break;
						}
						Junction.Branch branch = (!flag4) ? track.GetInBranch() : track.GetOutBranch();
						bool flag9 = branch == null || branch.track == null;
						if (flag9)
						{
							return num2;
						}
						track = branch.track;
						double num5;
						if (!branch.first)
						{
							EquiPointSet equiPointSet2 = TrackFollower.psField.GetValue(track) as EquiPointSet;
							num5 = ((equiPointSet2 != null) ? equiPointSet2.span : 0.0);
						}
						else
						{
							num5 = 0.0;
						}
						num = num5;
						flag4 = branch.first;
					}
					result = -1f;
				}
			}
			return result;
		}

		// Token: 0x04000083 RID: 131
		private static readonly FieldInfo psField = typeof(RailTrack).GetField("pointSet", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly Collider[] overlapColliders = new Collider[128];

		private const float ObstacleSearchStepM = 10f;
	}
}
