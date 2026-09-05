namespace M62Logic
{
	public struct AlsnTrackState
	{
		public int LampCode;
		public float CurrentSpeedKmh;
		public float NextSpeedKmh;
		public float DistanceToLimitM;
		public bool FromExternalSource;

		public AlsnTrackState(int lampCode, float currentSpeedKmh, float nextSpeedKmh, float distanceToLimitM, bool fromExternalSource)
		{
			LampCode = lampCode;
			CurrentSpeedKmh = currentSpeedKmh;
			NextSpeedKmh = nextSpeedKmh;
			DistanceToLimitM = distanceToLimitM;
			FromExternalSource = fromExternalSource;
		}
	}
}
