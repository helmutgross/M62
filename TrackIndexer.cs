using System;
using System.Collections.Generic;
using System.Reflection;
using DV.PointSet;
using DV.Signs;
using UnityEngine;

namespace M62Logic
{
	// Token: 0x02000012 RID: 18
	public static class TrackIndexer
	{
		// Token: 0x06000045 RID: 69 RVA: 0x00006E76 File Offset: 0x00005076
		public static void Clear()
		{
			TrackIndexer.indexedTracks.Clear();
		}

		// Removes only entries whose RailTrack key has been destroyed (Unity "fake null"),
		// instead of wiping the whole cache whenever a single locomotive despawns/streams out.
		public static void PurgeStale()
		{
			bool flag = TrackIndexer.indexedTracks.Count == 0;
			if (!flag)
			{
				List<RailTrack> list = null;
				foreach (RailTrack railTrack in TrackIndexer.indexedTracks.Keys)
				{
					bool flag2 = railTrack == null;
					if (flag2)
					{
						bool flag3 = list == null;
						if (flag3)
						{
							list = new List<RailTrack>();
						}
						list.Add(railTrack);
					}
				}
				bool flag4 = list != null;
				if (flag4)
				{
					for (int i = 0; i < list.Count; i++)
					{
						TrackIndexer.indexedTracks.Remove(list[i]);
					}
				}
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00006E84 File Offset: 0x00005084
		public static List<TrackEvent> GetTrackEvents(RailTrack track)
		{
			bool flag = track == null;
			List<TrackEvent> result;
			if (flag)
			{
				result = new List<TrackEvent>();
			}
			else
			{
				List<TrackEvent> list;
				bool flag2 = !TrackIndexer.indexedTracks.TryGetValue(track, out list);
				if (flag2)
				{
					list = TrackIndexer.GenerateTrackEvents(track);
					TrackIndexer.indexedTracks[track] = list;
				}
				result = list;
			}
			return result;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00006ED4 File Offset: 0x000050D4
		private static List<TrackEvent> GenerateTrackEvents(RailTrack track)
		{
			List<TrackEvent> list = new List<TrackEvent>();
			EquiPointSet equiPointSet = ((TrackIndexer.psField != null) ? TrackIndexer.psField.GetValue(track) : null) as EquiPointSet;
			bool flag = equiPointSet == null;
			List<TrackEvent> result;
			if (flag)
			{
				result = list;
			}
			else
			{
				EquiPointSet equiPointSet2 = EquiPointSet.ResampleEquidistant(equiPointSet, Mathf.Min(10f, (float)equiPointSet.span / 3f), 0f, false, true);
				Vector3 currentMove = WorldMover.currentMove;
				foreach (EquiPointSet.Point point in equiPointSet2.points)
				{
					int num = Physics.RaycastNonAlloc(new Ray((Vector3)point.position + currentMove, point.forward), TrackIndexer.raycastHits, (float)point.spanToNextPoint, 1073741824);
					for (int j = 0; j < num; j++)
					{
						RaycastHit raycastHit = TrackIndexer.raycastHits[j];
						TrackIndexer.ParseSign(raycastHit.collider.name, Vector3.Dot(raycastHit.collider.transform.forward, point.forward) < 0f, point.span + (double)raycastHit.distance, list);
					}
				}
				result = list;
			}
			return result;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00007024 File Offset: 0x00005224
		private static void ParseSign(string text, bool direction, double span, List<TrackEvent> events)
		{
			bool flag = string.IsNullOrEmpty(text);
			if (!flag)
			{
				string[] array = text.Split(TrackIndexer.newlineSeparators, StringSplitOptions.RemoveEmptyEntries);
				bool flag2 = array.Length == 0;
				if (!flag2)
				{
					int num;
					bool flag3 = int.TryParse(array[0].Trim(), out num);
					if (flag3)
					{
						int limit = (num >= 20) ? num : (num * 10);
						int num2 = 0;
						bool flag4 = array.Length > 1 && int.TryParse(array[1].Trim(), out num2);
						if (flag4)
						{
							events.Add(new DualSpeedLimitEvent(span, direction, limit, (num2 >= 20) ? num2 : (num2 * 10)));
						}
						else
						{
							events.Add(new SpeedLimitEvent(span, direction, limit));
						}
					}
				}
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000070D0 File Offset: 0x000052D0
		public static void SetupSign(SignDebug sd)
		{
			bool flag = sd == null;
			if (!flag)
			{
				CapsuleCollider capsuleCollider = sd.gameObject.GetComponent<CapsuleCollider>();
				bool flag2 = capsuleCollider != null && capsuleCollider.center.x > 1.5f && sd.gameObject.layer == 30;
				if (!flag2)
				{
					sd.gameObject.layer = 30;
					bool flag3 = capsuleCollider == null;
					if (flag3)
					{
						capsuleCollider = sd.gameObject.AddComponent<CapsuleCollider>();
					}
					capsuleCollider.name = sd.text;
					capsuleCollider.isTrigger = true;
					capsuleCollider.radius = 1.2f;
					capsuleCollider.height = 10f;
					capsuleCollider.center = new Vector3(2f, 0f, 0f);
				}
			}
		}

		// Token: 0x0400007E RID: 126
		private const int HUD_LAYER = 30;

		// Token: 0x0400007F RID: 127
		private static readonly Dictionary<RailTrack, List<TrackEvent>> indexedTracks = new Dictionary<RailTrack, List<TrackEvent>>();

		// Token: 0x04000080 RID: 128
		private static readonly FieldInfo psField = typeof(RailTrack).GetField("pointSet", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000081 RID: 129
		private static readonly RaycastHit[] raycastHits = new RaycastHit[128];

		// Token: 0x04000082 RID: 130
		private static readonly char[] newlineSeparators = new char[]
		{
			'\n'
		};
	}
}
