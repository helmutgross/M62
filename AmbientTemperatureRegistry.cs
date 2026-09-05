namespace M62Logic
{
	public static class AmbientTemperatureRegistry
	{
		public static IAmbientTemperatureProvider SeasonsProvider { get; set; }

		public static M62ModSettings Settings { get; set; }

		public static float LastAmbientCelsius { get; private set; } = DefaultAmbientProvider.DefaultCelsius;

		public static bool LastReadFromSeasons { get; private set; }

		public static float GetEffectiveAmbientCelsius()
		{
			float ambient;
			if (Settings != null && Settings.UseDvSeasonsAmbient && SeasonsProvider != null && SeasonsProvider.TryGetAmbientCelsius(out ambient))
			{
				LastReadFromSeasons = true;
				LastAmbientCelsius = ambient;
				return ambient;
			}

			LastReadFromSeasons = false;
			LastAmbientCelsius = Settings != null ? Settings.FallbackAmbientCelsius : DefaultAmbientProvider.DefaultCelsius;
			return LastAmbientCelsius;
		}

		public static bool IsSeasonsAmbientAvailable()
		{
			float temp;
			return Settings != null
				&& Settings.UseDvSeasonsAmbient
				&& SeasonsProvider != null
				&& SeasonsProvider.TryGetAmbientCelsius(out temp);
		}
	}
}
