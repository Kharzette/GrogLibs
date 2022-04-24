using System;

namespace InputLib;

public class GameAction
{
	public enum MoveActions
	{
		MoveForward, MoveBackward, MoveLeft, MoveRight,
		TurnLeft, TurnRight,
		LookUp, LookDown,
		Crouch, Jump
	}

	public enum UseActions
	{
		UseWorldUnderCursor,
		UsePotionSlot0, UsePotionSlot1, UsePotionSlot2, UsePotionSlot3, UsePotionSlot4
	}

	public enum AbilityActions
	{

	}
}
