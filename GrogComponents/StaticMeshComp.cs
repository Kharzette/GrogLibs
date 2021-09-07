namespace EntityLib
{
	public class StaticMeshComp : Component
	{
		public enum State
		{
			Visible
		}

		public PosOrient	mPO;
		public object		mDrawObject;


		public StaticMeshComp(object drawObj, Entity owner) : base(owner)
		{
			mDrawObject	=drawObj;

			mPO	=mOwner.GetComponent(typeof(PosOrient)) as PosOrient;
		}
	}
}