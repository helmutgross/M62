using UnityModManagerNet;

namespace M62Logic
{
	public sealed class M62ModSettings : UnityModManager.ModSettings
	{
		public bool UseDvSeasonsAmbient = true;

		public float FallbackAmbientCelsius = DefaultAmbientProvider.DefaultCelsius;

		public bool ColdStartEnabled = true;

		public float ColdStartOilOffset = 0f;

		public bool ShowAmbientInGui = false;

		public void Clamp()
		{
			if (this.FallbackAmbientCelsius < -50f)
			{
				this.FallbackAmbientCelsius = -50f;
			}

			if (this.FallbackAmbientCelsius > 50f)
			{
				this.FallbackAmbientCelsius = 50f;
			}

			if (this.ColdStartOilOffset < -20f)
			{
				this.ColdStartOilOffset = -20f;
			}

			if (this.ColdStartOilOffset > 20f)
			{
				this.ColdStartOilOffset = 20f;
			}
		}

		public new void Save(UnityModManager.ModEntry entry)
		{
			this.Clamp();
			UnityModManager.ModSettings.Save(this, entry);
		}
	}
}
