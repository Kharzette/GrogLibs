using System;

namespace InputLib;

public enum ActionTypes
{
	ContinuousHold,		//sends an event each update with an amount (for key movement etc)
	PressAndRelease,	//fires on release
	AnalogAmount,		//for a stick axis or mouse or something
	Toggle,				//keydown on, keyup off?
	ActivateOnce		//fires on press, but must be released to fire again
}

public enum Modifiers
{
	None		=0,
	ShiftHeld	=1,
	ControlHeld	=2,
	AltHeld		=4
}

internal class ActionMapping
{
	internal ActionTypes	mActionType;
	internal Enum			mAction;	//user specified per game?
	internal Enum			mActionOff;	//for toggle off state
	internal int			mKeyCode;
	internal Modifiers		mModifier;
}
