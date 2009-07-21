using System;
using Microsoft.Xna.Framework;

namespace ColladaConvert
{
	//subclasses for animating translation values
	public class RotateXAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Rotate)mOperand).mValue.X	=val;
		}
		public RotateXAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class RotateYAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Rotate)mOperand).mValue.Y	=val;
		}
		public RotateYAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class RotateZAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Rotate)mOperand).mValue.Z	=val;
		}
		public RotateZAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class RotateWAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Rotate)mOperand).mValue.W	*=MathHelper.ToRadians(val);
		}
		public RotateWAnim(AnimCreationParameters acp) : base(acp) { }
	}
}