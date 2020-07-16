using SharpDX;


namespace EntityLib
{
	public class StaticMeshComp : Component
	{
		public enum State
		{
			Visible
		}

		public Matrix	mMat;
		public object	mDrawObject;


		public StaticMeshComp(object drawObj, Entity owner) : base(owner)
		{
			mDrawObject	=drawObj;

			mMat	=Matrix.Identity;
		}
	}
}