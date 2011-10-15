using System;

namespace Microsoft.Xna.Framework
{
	[Flags]
	public enum SpriteEffects
	{
		None = 0,
		FlipHorizontally = 1,
		FlipVertically = 256
	}
	
	
	public enum PlayerIndex
	{
		One,
		Two,
		Three,
		Four
	}

	[Flags]
	public enum DisplayOrientation
	{
		Default = 0,
		LandscapeLeft = 1,
		LandscapeRight = 2,
		Portrait = 4
	}


	public enum CurveTangent
	{
		Flat,
		Linear,
		Smooth
	}

	public enum CurveLoopType
	{
		Constant,
		Cycle,
		CycleOffset,
		Oscillate,
		Linear
	}

	public enum CurveContinuity
	{
		Smooth,
		Step
	}

	public enum PlaneIntersectionType
	{
		Front,
		Back,
		Intersecting
	}

	public enum ContainmentType
	{
		Disjoint,
		Contains,
		Intersects
	}
}
