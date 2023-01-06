using SharpDX;


namespace EntityLib
{
	//shoot me for things to happen
	public class ShootObject : Component
	{
		enum State
		{
			Idle, Shot
		}

		Vector3		mPosition;


		public ShootObject(Vector3 pos, Entity owner) : base(owner)
		{
			mPosition	=pos;
		}
	}
}