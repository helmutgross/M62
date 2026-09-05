using System;
using HarmonyLib;

namespace M62Logic
{
	// Token: 0x02000006 RID: 6
	[HarmonyPatch(typeof(TrainCar), "Start")]
	public static class TrainCar_Start_Patch
	{
		// Token: 0x06000009 RID: 9 RVA: 0x000024E0 File Offset: 0x000006E0
		public static void Postfix(TrainCar __instance)
		{
			bool flag = __instance.gameObject.name.Contains("DM62") || __instance.gameObject.name.Contains("M62");
			if (flag)
			{
				if (AlsnLampRegistry.Provider == null)
				{
					Main.RefreshSignalsProvider();
				}
				bool flag2 = __instance.gameObject.GetComponent<M62Controller>() == null;
				if (flag2)
				{
					__instance.gameObject.AddComponent<M62Controller>();
				}
				bool flag3 = __instance.gameObject.GetComponent<AlsnSystem>() == null;
				if (flag3)
				{
					__instance.gameObject.AddComponent<AlsnSystem>();
				}
				bool flag4 = __instance.gameObject.GetComponent<M62_394_Behavior>() == null;
				if (flag4)
				{
					__instance.gameObject.AddComponent<M62_394_Behavior>();
				}
				bool flag5 = __instance.gameObject.GetComponent<M62_Clock_Behavior>() == null;
				if (flag5)
				{
					__instance.gameObject.AddComponent<M62_Clock_Behavior>();
				}
				bool flag6 = __instance.gameObject.GetComponent<M62_OilLouverBehavior>() == null;
				if (flag6)
				{
					__instance.gameObject.AddComponent<M62_OilLouverBehavior>();
				}
			}
		}
	}
}
