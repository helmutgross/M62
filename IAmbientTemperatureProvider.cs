namespace M62Logic
{
	public interface IAmbientTemperatureProvider
	{
		bool TryGetAmbientCelsius(out float temp);
	}
}
