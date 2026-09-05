using System;
using DV.Signs;
using HarmonyLib;
using UnityEngine;

namespace M62Logic
{
	// Token: 0x02000007 RID: 7
	[HarmonyPatch(typeof(Streamer), "AddSceneGO")]
	public static class StreamerPatch
	{
		// Token: 0x0600000A RID: 10 RVA: 0x000025AC File Offset: 0x000007AC
		public static void Postfix(GameObject sceneGO)
		{
			SignDebug[] componentsInChildren = sceneGO.GetComponentsInChildren<SignDebug>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				TrackIndexer.SetupSign(componentsInChildren[i]);
			}
		}
	}
}
