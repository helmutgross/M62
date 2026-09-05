namespace M62Logic
{
	public sealed class DefaultAmbientProvider : IAmbientTemperatureProvider
	{
		public const float DefaultCelsius = 10f;

		private readonly float fallbackCelsius;

		public DefaultAmbientProvider(float fallbackCelsius = DefaultCelsius)
		{
			this.fallbackCelsius = fallbackCelsius;
		}

		public bool TryGetAmbientCelsius(out float temp)
		{
			temp = this.fallbackCelsius;
			return true;
		}
	}
}
