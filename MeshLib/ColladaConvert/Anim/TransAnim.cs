using System;

namespace ColladaConvert
{
	//subclasses for animating translation values
	public class TransXAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Translate)mOperand).mValue.X	=val;
		}
		public TransXAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class TransYAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Translate)mOperand).mValue.Y	=val;
		}
		public TransYAnim(AnimCreationParameters acp) : base(acp) { }
	}

	public class TransZAnim : Anim
	{
		protected override void ApplyValueToOperand(float val)
		{
			((Translate)mOperand).mValue.Z	=val;
		}
		public TransZAnim(AnimCreationParameters acp) : base(acp) { }
	}
}