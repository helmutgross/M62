using System;
using DV.ModularAudioCar;
using HarmonyLib;

namespace M62Logic
{
	// Token: 0x02000003 RID: 3
	[HarmonyPatch(typeof(BrakesAudioModule), "UpdateModule")]
	public static class BrakesAudioModule_UpdateModule_Prefix
	{
		// Token: 0x06000002 RID: 2 RVA: 0x000020EC File Offset: 0x000002EC
		public static bool Prefix(BrakesAudioModule __instance)
		{
			return !M62PatchCache.IsUnderM62Hierarchy(__instance.transform);
		}
	}
}
