using System.Collections.Generic;
using UnityEngine;

namespace M62Logic
{
	// Shared helpers previously duplicated across M62Controller, M62_394_Behavior and M62_Clock_Behavior.
	public static class GaugeUtils
	{
		public static Vector3 CleanVector3(Vector3 input, Vector3 fallback)
		{
			bool flag = float.IsNaN(input.x) || float.IsInfinity(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.y) || float.IsNaN(input.z) || float.IsInfinity(input.z);
			Vector3 result;
			if (flag)
			{
				result = fallback;
			}
			else
			{
				result = input;
			}
			return result;
		}

		public static bool HasGauge(List<GaugeData> list, Transform t)
		{
			for (int i = 0; i < list.Count; i++)
			{
				bool flag = list[i].transform == t;
				if (flag)
				{
					return true;
				}
			}
			return false;
		}

		// buffer is provided by the caller so each MonoBehaviour keeps reusing its own
		// preallocated List<Component> instead of allocating a new one per call.
		public static void StripGameScripts(Transform target, List<Component> buffer)
		{
			target.GetComponents<Component>(buffer);
			for (int i = 0; i < buffer.Count; i++)
			{
				Component component = buffer[i];
				bool flag = !(component is Transform) && !(component is MeshFilter) && !(component is MeshRenderer);
				if (flag)
				{
					Object.Destroy(component);
				}
			}
		}
	}
}
