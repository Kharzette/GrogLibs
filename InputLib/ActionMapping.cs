using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UtilityLibSharpDX
{
	class ActionMapping
	{
		enum ActionTypes
		{
			ContinuousHold,
			PressAndRelease,
			AnalogAmount
		}

		ActionTypes	mActionType;
		InputAction	mAction;
	}
}
