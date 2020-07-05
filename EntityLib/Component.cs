using System;
using System.Collections.Generic;
using UtilityLib;

namespace EntityLib
{
    public interface Component
    {
		void Update(UpdateTimer timer);
		void StateChange(Enum state, UInt32 value);
    }
}
