using System;

namespace ColladaConvert
{
	//subclasses for animating translation values
	public class ScaleXAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Scale)mOperand).mValue.X	=val;
		}
		public ScaleXAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class ScaleYAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Scale)mOperand).mValue.Y	=val;
		}
		public ScaleYAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class ScaleZAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Scale)mOperand).mValue.Z	=val;
		}
		public ScaleZAnim(AnimCreationParameters acp) : base(acp) { }
	}
}