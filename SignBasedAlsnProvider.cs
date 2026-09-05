namespace M62Logic
{
	public static class SignBasedAlsnProvider
	{
		public static bool TryGetLampState(TrainCar car, out AlsnTrackState state)
		{
			state = default(AlsnTrackState);
			if (car == null)
			{
				return false;
			}

			float current;
			float next;
			float dist;
			TrackFollower.GetSpeedLimits(car, out current, out next, out dist);
			int lampCode = AlsnLampCode.Green;

			if (next <= 0f || dist > 1200f)
			{
				lampCode = AlsnLampCode.White;
			}
			else if (dist <= 1000f && next < current)
			{
				lampCode = (next <= 30f) ? AlsnLampCode.YellowRed : AlsnLampCode.Yellow;
			}

			state = new AlsnTrackState(lampCode, current, next, dist, false);
			return true;
		}
	}
}
