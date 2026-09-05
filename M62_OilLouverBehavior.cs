using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace M62Logic
{
	public class M62_OilLouverBehavior : MonoBehaviour
	{
		private const string PortId = "Zaluzi_Oil.EXT_IN";
		private const string OilBaseTransformName = "b.r.base";
		private const string WaterBaseTransformName = "b.r.base 1";
		private const string SlatNamePrefix = "b.r.stvor";
		private const int SlatCount = 13;
		private const float OpenAngleDeg = 80f;
		private const float OpenDurationSec = 1.5f;
		private static readonly Vector3 LocalOpenAxis = Vector3.right;

		private TrainCar car;
		private bool isInitialized;
		private float openAmount;
		private float lastSlatSearchTime;
		private readonly List<LouverSlat> slats = new List<LouverSlat>();
		private readonly List<Transform> transformBuffer = new List<Transform>(64);
		private readonly Dictionary<Transform, Quaternion> closedRotationsCache = new Dictionary<Transform, Quaternion>();
		private Dictionary<string, FastPort> fPorts = new Dictionary<string, FastPort>(StringComparer.OrdinalIgnoreCase);

		private struct LouverSlat
		{
			public Transform transform;
			public Quaternion closedLocalRotation;
		}

		private void Start()
		{
			car = base.GetComponent<TrainCar>();
			base.StartCoroutine(InitRoutine());
		}

		private void OnDestroy()
		{
			base.StopAllCoroutines();
			slats.Clear();
			closedRotationsCache.Clear();
			if (fPorts != null)
			{
				fPorts.Clear();
				fPorts = null;
			}
		}

		private IEnumerator InitRoutine()
		{
			int retries = 0;
			Component sim = null;
			PropertyInfo flowProp = null;
			while (sim == null && retries < 20)
			{
				yield return new WaitForSeconds(0.5f);
				retries++;
				Component[] componentsInChildren = base.GetComponentsInChildren<Component>(true);
				foreach (Component c in componentsInChildren)
				{
					if (c == null)
					{
						continue;
					}

					flowProp = c.GetType().GetProperty("SimulationFlow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (flowProp != null)
					{
						sim = c;
						break;
					}
				}
			}

			if (sim != null && flowProp != null)
			{
				object simFlow = flowProp.GetValue(sim, null);
				if (simFlow != null)
				{
					FieldInfo[] fields = simFlow.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo f in fields)
					{
						IDictionary dict;
						if (!typeof(IDictionary).IsAssignableFrom(f.FieldType) || (dict = f.GetValue(simFlow) as IDictionary) == null)
						{
							continue;
						}

						foreach (DictionaryEntry entry in dict)
						{
							string key = entry.Key.ToString();
							object pObj = entry.Value;
							PropertyInfo p = pObj.GetType().GetProperty("Value");
							MethodInfo m = pObj.GetType().GetMethod("ExternalValueUpdate");
							fPorts[key] = new FastPort(pObj, p, m);
						}
					}
				}
			}

			RefreshSlats();
			isInitialized = true;
		}

		private void Update()
		{
			if (!isInitialized || car == null)
			{
				return;
			}

			if (Time.time - lastSlatSearchTime > 2f)
			{
				RefreshSlats();
			}

			if (slats.Count == 0)
			{
				return;
			}

			bool targetOpen = GetVal(PortId) > 0.5f;
			float target = targetOpen ? 1f : 0f;
			openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime / OpenDurationSec);

			Quaternion delta = Quaternion.AngleAxis(OpenAngleDeg * openAmount, LocalOpenAxis);
			for (int i = 0; i < slats.Count; i++)
			{
				LouverSlat slat = slats[i];
				if (slat.transform == null)
				{
					continue;
				}

				slat.transform.localRotation = slat.closedLocalRotation * delta;
			}
		}

		private void RefreshSlats()
		{
			lastSlatSearchTime = Time.time;
			if (car == null)
			{
				return;
			}

			var existing = new Dictionary<Transform, Quaternion>(slats.Count);
			for (int i = 0; i < slats.Count; i++)
			{
				LouverSlat slat = slats[i];
				if (slat.transform != null)
				{
					existing[slat.transform] = slat.closedLocalRotation;
				}
			}

			slats.Clear();
			transformBuffer.Clear();
			car.GetComponentsInChildren<Transform>(true, transformBuffer);

			var selectedSet = new HashSet<Transform>();
			for (int i = 0; i < transformBuffer.Count; i++)
			{
				Transform t = transformBuffer[i];
				if (!IsOilSlatCandidate(t))
				{
					continue;
				}

				selectedSet.Add(t);
				slats.Add(new LouverSlat
				{
					transform = t,
					closedLocalRotation = GetOrCacheClosedRotation(t)
				});
			}

			foreach (KeyValuePair<Transform, Quaternion> entry in existing)
			{
				Transform t = entry.Key;
				if (t == null || selectedSet.Contains(t))
				{
					continue;
				}

				Quaternion resetRotation;
				if (!closedRotationsCache.TryGetValue(t, out resetRotation))
				{
					resetRotation = entry.Value;
				}

				t.localRotation = resetRotation;
			}

			for (int i = 0; i < transformBuffer.Count; i++)
			{
				Transform t = transformBuffer[i];
				if (t == null || !IsNamedOilSlat(t) || selectedSet.Contains(t))
				{
					continue;
				}

				Quaternion resetRotation;
				if (closedRotationsCache.TryGetValue(t, out resetRotation))
				{
					t.localRotation = resetRotation;
				}
			}

			slats.Sort((a, b) => string.Compare(a.transform.name, b.transform.name, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsOilSlatCandidate(Transform t)
		{
			if (!IsNamedOilSlat(t))
			{
				return false;
			}

			return t.parent != null
				&& string.Equals(t.parent.name, OilBaseTransformName, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsUnderNamedAncestor(Transform t, string ancestorName)
		{
			Transform parent = t.parent;
			while (parent != null)
			{
				if (string.Equals(parent.name, ancestorName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}

				parent = parent.parent;
			}

			return false;
		}

		private static bool IsNamedOilSlat(Transform t)
		{
			if (t == null || IsUnderNamedAncestor(t, WaterBaseTransformName))
			{
				return false;
			}

			if (!t.name.StartsWith(SlatNamePrefix, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			string suffix = t.name.Substring(SlatNamePrefix.Length);
			int index;
			return suffix.Length == 2
				&& int.TryParse(suffix, out index)
				&& index >= 1
				&& index <= SlatCount;
		}

		private Quaternion GetOrCacheClosedRotation(Transform t)
		{
			Quaternion closedRotation;
			if (!closedRotationsCache.TryGetValue(t, out closedRotation))
			{
				closedRotation = t.localRotation;
				closedRotationsCache[t] = closedRotation;
			}

			return closedRotation;
		}

		private float GetVal(string id)
		{
			FastPort fastPort;
			return fPorts != null && fPorts.TryGetValue(id, out fastPort) ? fastPort.Get() : 0f;
		}
	}
}
