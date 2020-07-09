using System;
using System.Collections.Generic;
using UtilityLib;
using BSPZone;
using SharpDX;


namespace EntityLib
{
	public class Light : Component
	{
		public enum State
		{
			On, Damaged, Destroyed
		}

		//delegate for switching lights on and off in the rendering
		public delegate void SwitchLight(int light, bool bOn);

		int		mRenderIndex;	//index for rendering
		float	mStrength;
		Vector3	mPosition;
		Vector3	mColor;
		int		mStyle;
		bool	mbOn;			//on by default
		bool	mbSwitchable;	//switchable lights
		bool	mbSun;			//sun light

		SwitchLight	mSwitch;



		public Light(Entity owner, float str, int style, Vector3 pos,
					Vector3 color, bool bOn, bool bSwitchable,
					bool bSun, int rendIndex, SwitchLight switchLight) : base(owner)
		{
			mStrength		=str;
			mPosition		=pos;
			mColor			=color;
			mStyle			=style;
			mbOn			=bOn;
			mbSwitchable	=bSwitchable;
			mbSun			=bSun;
			mSwitch			=switchLight;
			mRenderIndex	=rendIndex;
		}


		override public void StateChange(Enum state, uint value)
		{
			if(state.Equals(State.Damaged))
			{
			}
			else if(state.Equals(State.Destroyed))
			{
			}
			else if(state.Equals(State.On))
			{
				mSwitch(mRenderIndex, value != 0);
			}
		}
	}
}