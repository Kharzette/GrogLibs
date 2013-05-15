using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace UtilityLib
{
	public class Rumble
	{
		Input.PlayerInput	mPI;

		Mover2	mRumbleMover	=new Mover2();
		bool	mbRising;

		float	mDuration;
		bool	mbOn;


		public Rumble(Input.PlayerInput inp)
		{
			mPI	=inp;
		}


		public void Update(int msDelta, bool bOn)
		{
			mbOn	=bOn;

			if(mRumbleMover.Done())
			{
				return;
			}

			Vector2	pos	=Vector2.Zero;

			lock(mRumbleMover)
			{
				mRumbleMover.Update(msDelta);

				pos	=mRumbleMover.GetPos();

				if(mRumbleMover.Done())
				{
					if(mbRising)
					{
						mbRising	=false;

						mRumbleMover.SetUpMove(pos, Vector2.Zero, mDuration, 0.2f, 0.2f);
					}
				}
			}

			if(mPI == null)
			{
				return;
			}

			if(!mPI.mGPS.IsConnected || !mbOn)
			{
				return;
			}

			GamePad.SetVibration(mPI.mIndex, pos.X, pos.Y);
		}


		public void ActivateRumble(float leftAmount, float rightAmount, float duration)
		{
			if(!mbOn)
			{
				return;
			}

			lock(mRumbleMover)
			{
				mDuration	=duration * 0.5f;
				mbRising	=true;

				if(mRumbleMover.Done())
				{
					//not currently rumbling anything
					mRumbleMover.SetUpMove(Vector2.Zero,
						Vector2.UnitX * leftAmount + Vector2.UnitY * rightAmount,
						mDuration, 0.2f, 0.2f);
				}
				else
				{
					//continue from previous position
					mRumbleMover.SetUpMove(mRumbleMover.GetPos(),
						Vector2.UnitX * leftAmount + Vector2.UnitY * rightAmount,
						mDuration, 0.2f, 0.2f);
				}
			}
		}
	}
}
