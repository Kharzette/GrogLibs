using System;
using System.Collections.Generic;
using UtilityLib;

namespace EntityLib
{
    public class Component
    {
		public Entity	mOwner;

		public Component(Entity owner)
		{
			mOwner	=owner;
		}

		virtual public void Update(UpdateTimer timer) {}
		virtual public void StateChange(Enum state, UInt32 value) {}
    }
}
