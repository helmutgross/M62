using System;

namespace M62Logic
{
	// Token: 0x02000010 RID: 16
	public class DualSpeedLimitEvent : SpeedLimitEvent
	{
		// Token: 0x06000041 RID: 65 RVA: 0x00006E04 File Offset: 0x00005004
		public DualSpeedLimitEvent(double span, bool direction, int limit, int rightLimit) : base(span, direction, limit)
		{
			this.rightLimit = rightLimit;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00006E1C File Offset: 0x0000501C
		public override TrackEvent WithSpan(double span)
		{
			return new DualSpeedLimitEvent(span, this.direction, this.limit, this.rightLimit);
		}

		// Token: 0x0400007C RID: 124
		public readonly int rightLimit;
	}
}
