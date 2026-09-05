using System;

namespace M62Logic
{
	// Token: 0x02000011 RID: 17
	public class JunctionEvent : TrackEvent
	{
		// Token: 0x06000043 RID: 67 RVA: 0x00006E46 File Offset: 0x00005046
		public JunctionEvent(double span, Junction junction) : base(span)
		{
			this.junction = junction;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00006E58 File Offset: 0x00005058
		public override TrackEvent WithSpan(double span)
		{
			return new JunctionEvent(span, this.junction);
		}

		// Token: 0x0400007D RID: 125
		public readonly Junction junction;
	}
}
