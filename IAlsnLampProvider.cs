namespace M62Logic
{
	public interface IAlsnLampProvider
	{
		bool TryGetLampState(TrainCar car, out AlsnTrackState state);
	}
}
