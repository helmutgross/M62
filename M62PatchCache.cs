using System;
using System.Runtime.CompilerServices;
using DV.Simulation.Brake;
using UnityEngine;

namespace M62Logic
{
	internal static class M62PatchCache
	{
		private sealed class Brake394Slot
		{
			public bool LookupDone;
			public M62_394_Behavior Behavior;
		}

		private static readonly ConditionalWeakTable<BrakeSystem, Brake394Slot> Brake394BySystem = new ConditionalWeakTable<BrakeSystem, Brake394Slot>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsM62Name(string name)
		{
			return !string.IsNullOrEmpty(name) && (name.IndexOf("M62", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("DM62", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		public static bool HasM62_394Behavior(BrakeSystem brakeSystem)
		{
			if (brakeSystem == null)
			{
				return false;
			}
			Brake394Slot slot = Brake394BySystem.GetValue(brakeSystem, key => new Brake394Slot());
			if (!slot.LookupDone)
			{
				slot.Behavior = brakeSystem.GetComponent<M62_394_Behavior>();
				slot.LookupDone = true;
			}
			return slot.Behavior != null;
		}

		public static bool IsUnderM62Hierarchy(Transform transform)
		{
			Transform node = transform;
			while (node != null)
			{
				if (M62PatchCache.IsM62Name(node.name))
				{
					return true;
				}
				node = node.parent;
			}
			return false;
		}
	}
}
