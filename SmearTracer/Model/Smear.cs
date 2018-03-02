﻿using SmearsMaker.Common.BaseTypes;

namespace SmearTracer.Model
{
	public class Smear
	{
		public BrushStroke BrushStroke { get; }
		public Segment Segment { get; set; }
		public Cluster Cluster { get; set; }

		public Smear(BrushStroke brushStroke)
		{
			BrushStroke = brushStroke;
		}
	}
}