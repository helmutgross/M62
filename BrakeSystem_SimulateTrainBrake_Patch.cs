using System;
using DV.Simulation.Brake;
using HarmonyLib;

namespace M62Logic
{
	// Token: 0x0200000B RID: 11
	[HarmonyPatch(typeof(BrakeSystem), "SimulateTrainBrake")]
	public static class BrakeSystem_SimulateTrainBrake_Patch
	{
		// Token: 0x06000025 RID: 37 RVA: 0x000044A4 File Offset: 0x000026A4
		public static bool Prefix(BrakeSystem __instance)
		{
			return __instance == null || !M62PatchCache.HasM62_394Behavior(__instance);
		}
	}
}
