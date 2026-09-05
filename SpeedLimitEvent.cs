using System;

namespace M62Logic
{
	// Token: 0x0200000F RID: 15
	public class SpeedLimitEvent : TrackEvent
	{
		// Token: 0x0600003F RID: 63 RVA: 0x00006DC4 File Offset: 0x00004FC4
		public SpeedLimitEvent(double span, bool direction, int limit) : base(span)
		{
			this.direction = direction;
			this.limit = limit;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00006DE0 File Offset: 0x00004FE0
		public override TrackEvent WithSpan(double span)
		{
			return new SpeedLimitEvent(span, this.direction, this.limit);
		}

		// Token: 0x0400007A RID: 122
		public readonly bool direction;

		// Token: 0x0400007B RID: 123
		public readonly int limit;
	}
}
