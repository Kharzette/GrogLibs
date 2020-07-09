namespace EntityLib
{
	public class TargetName : Component
	{
		public readonly	string	mTargetName;


		public TargetName(string name, Entity owner) : base(owner)
		{
			mTargetName	=name;
		}
	}
}