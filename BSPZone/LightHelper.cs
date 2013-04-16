using System;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using BSPZone;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	public class LightHelper
	{
		Zone	mZone;

		//lighting stuff on the move
		Mover3					mBestLightMover	=new Mover3();
		Mover4					mBestColorMover	=new Mover4();
		Zone.ZoneLight			mBestLight;
		Zone.GetStyleStrength	mStyleStrength;

		//current light values post update
		Vector4	mCurColor0;
		Vector3	mCurLightDir;

		//constants
		const float	LightLerpTime	=0.5f;	//in seconds
		const float	LightEaseIn		=0.2f;
		const float	LightEaseOut	=0.2f;


		public void Initialize(Zone z, Zone.GetStyleStrength styleStrength)
		{
			mBestLight	=null;	//make sure this doesn't hold up free

			mZone			=z;
			mStyleStrength	=styleStrength;
		}


		public void GetCurrentValues(out Vector4 color0, out Vector3 lightDir)
		{
			color0		=mCurColor0;
			lightDir	=mCurLightDir;
		}


		//TODO: mover3 might cause nans if lerping between
		//opposite axiseesseseseeseses, use quat slerp?
		public void Update(int msDelta, Vector3 pos)
		{
			Zone.ZoneLight	zl	=mZone.GetStrongestLightInLOS(pos, mStyleStrength);
			if(zl == null)
			{
				//superdark!
				mCurColor0		=Vector4.Zero;
				mCurLightDir	=Vector3.UnitX;
				mBestLight		=null;
				return;
			}

			Vector3	curPos		=Vector3.Zero;
			Vector4 curColor	=Vector4.One;
			float	curStrength	=0f;
			bool	bReady		=false;

			if(mBestLight != zl)
			{
				if(mBestLight == null)
				{
					curPos		=zl.mPosition;
					curColor.X	=zl.mColor.X;
					curColor.Y	=zl.mColor.Y;
					curColor.Z	=zl.mColor.Z;
					curColor.W	=1f;
					curStrength	=zl.mStrength;
					bReady		=true;
				}
				else
				{
					//if still lerping, use the lerp position
					if(!mBestLightMover.Done())
					{
						mBestLightMover.SetUpMove(mBestLightMover.GetPos(), zl.mPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}
					else
					{
						mBestLightMover.SetUpMove(mBestLight.mPosition, zl.mPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}

					//lerp strength in the color w
					Vector4	start	=Vector4.Zero;
					Vector4	end		=Vector4.Zero;

					end.X	=zl.mColor.X;
					end.Y	=zl.mColor.Y;
					end.Z	=zl.mColor.Z;
					end.W	=zl.mStrength;

					//see if still lerping
					if(mBestColorMover.Done())
					{
						start.X	=mBestLight.mColor.X;
						start.Y	=mBestLight.mColor.Y;
						start.Z	=mBestLight.mColor.Z;
						start.W	=mBestLight.mStrength;
					}
					else
					{
						start	=mBestColorMover.GetPos();
					}

					mBestColorMover.SetUpMove(start, end,
						LightLerpTime, LightEaseIn, LightEaseOut);
				}
				mBestLight	=zl;
			}

			if(!bReady)
			{
				if(!mBestLightMover.Done())
				{
					mBestLightMover.Update(msDelta);
					curPos		=mBestLightMover.GetPos();
				}
				else
				{
					curPos		=mBestLight.mPosition;
				}

				if(!mBestColorMover.Done())
				{
					mBestColorMover.Update(msDelta);
					curColor	=mBestColorMover.GetPos();
					curStrength	=curColor.W;
					curColor.W	=1.0f;
				}
				else
				{
					curColor.X	=mBestLight.mColor.X;
					curColor.Y	=mBestLight.mColor.Y;
					curColor.Z	=mBestLight.mColor.Z;
					curColor.W	=1f;
					curStrength	=mBestLight.mStrength;
				}
			}

			mCurLightDir	=curPos - pos;

			float	dist	=mCurLightDir.Length();

			mCurColor0	=Vector4.One;

			mCurColor0.X	=curColor.X;
			mCurColor0.Y	=curColor.Y;
			mCurColor0.Z	=curColor.Z;

			if(dist > curStrength)
			{
				mCurColor0	*=(curStrength / dist);
			}

			//check the light style if applicable
			if(mBestLight.mStyle != 0)
			{
				mCurColor0	*=mStyleStrength(mBestLight.mStyle);
			}

			mCurColor0.W	=1.0f;

			mCurLightDir.Normalize();
		}
	}
}
