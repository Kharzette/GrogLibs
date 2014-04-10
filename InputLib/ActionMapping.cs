﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputLib
{
	internal class ActionMapping
	{
		internal enum ActionTypes
		{
			ContinuousHold,
			PressAndRelease,
			AnalogAmount
		}

		internal ActionTypes	mActionType;
		internal Enum			mAction;	//user specified per game?
		internal int			mKeyCode;
	}
}