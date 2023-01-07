using System;
using System.Collections.Generic;
using UtilityLib;
using BSPZone;
using SharpDX;


namespace EntityLib;

public class Light : Component
{
	public enum State
	{
		On, Damaged, Destroyed
	}

	//delegate for switching lights on and off in the rendering
	public delegate void SwitchLight(int light, bool bOn);

	public readonly int		mRenderIndex;	//index for rendering
	public readonly Vector3	mPosition;
	public readonly Vector3	mColor;
	public readonly int		mStyle;
	public readonly bool	mbSwitchable;	//switchable lights
	public readonly bool	mbSun;			//sun light

	float	mStrength;
	bool	mbOn, mbDamaged, mbDestroyed;

	SwitchLight	mSwitch;



	public Light(Entity owner, float str, int style, Vector3 pos,
				Vector3 color, bool bOn, bool bSwitchable,
				bool bSun, int rendIndex, SwitchLight switchLight) : base(owner)
	{
		mStrength		=str;
		mPosition		=pos;
		mColor			=color;
		mStyle			=style;
		mbSwitchable	=bSwitchable;
		mbSun			=bSun;
		mSwitch			=switchLight;
		mRenderIndex	=rendIndex;

		StateChange(State.On, (bOn)? 1u : 0u);
	}


	public float GetStrength()
	{
		return	mStrength;
	}


	public bool IsOn()
	{
		return	mbOn;
	}


	override public void StateChange(Enum state, uint value)
	{
		if(state.Equals(State.Damaged))
		{
			mbDamaged	=value != 0;
		}
		else if(state.Equals(State.Destroyed))
		{
			mbDestroyed	=value != 0;
		}
		else if(state.Equals(State.On))
		{
			mbOn	=value != 0;
			if(mbSwitchable)
			{
				mSwitch(mRenderIndex, value != 0);
			}
		}
	}
}