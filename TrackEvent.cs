using System;

namespace M62Logic
{
	// Token: 0x0200000E RID: 14
	public abstract class TrackEvent
	{
		// Token: 0x0600003D RID: 61 RVA: 0x00006DB3 File Offset: 0x00004FB3
		protected TrackEvent(double span)
		{
			this.span = span;
		}

		// Token: 0x0600003E RID: 62
		public abstract TrackEvent WithSpan(double span);

		// Token: 0x04000079 RID: 121
		public readonly double span;
	}
}
