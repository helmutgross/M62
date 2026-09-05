using M62Logic;
using UnityEngine;

namespace M62Logic.Signals
{
	internal class SignalsRetryHost : MonoBehaviour
	{
		private float nextRetry;

		private void Update()
		{
			if (AlsnLampRegistry.Provider != null)
			{
				return;
			}

			if (Time.unscaledTime < nextRetry)
			{
				return;
			}

			nextRetry = Time.unscaledTime + 2f;
			SignalsBootstrap.Refresh();
		}
	}
}
