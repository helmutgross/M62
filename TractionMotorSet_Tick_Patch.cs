using System;
using HarmonyLib;
using LocoSim.Implementations;

namespace M62Logic
{
	// Token: 0x0200000A RID: 10
	[HarmonyPatch(typeof(TractionMotorSet), "Tick")]
	public static class TractionMotorSet_Tick_Patch
	{
		// Token: 0x06000024 RID: 36 RVA: 0x0000443C File Offset: 0x0000263C
		public static void Postfix()
		{
			for (int i = 0; i < M62Controller.allControllers.Count; i++)
			{
				bool flag = M62Controller.allControllers[i] != null && M62Controller.allControllers[i].IsInitialized;
				if (flag)
				{
					M62Controller.allControllers[i].InjectTorque();
				}
			}
		}
	}
}
