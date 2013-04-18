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

		//sunlight entity
		ZoneEntity	mSunEnt;

		//lighting stuff on the move
		Mover3					mBestLightMover	=new Mover3();
		Mover4					mBestColorMover	=new Mover4();
		Zone.ZoneLight			mBestLight;
		Zone.GetStyleStrength	mStyleStrength;
		bool					mbLerpingToDark;

		//current light values post update
		Vector4	mLightColor;
		Vector3	mCurLightDir;

		//constants
		const float	LightLerpTime	=0.25f;	//in seconds
		const float	LightEaseIn		=0.2f;
		const float	LightEaseOut	=0.2f;


		public void Initialize(Zone z, Zone.GetStyleStrength styleStrength)
		{
			mBestLight	=null;	//make sure this doesn't hold up free

			mZone			=z;
			mStyleStrength	=styleStrength;

			GrabSun();
		}


		public void GetCurrentValues(out Vector4 color0, out Vector3 lightDir)
		{
			color0		=mLightColor;
			lightDir	=mCurLightDir;
		}


		public bool NeedsShadow()
		{
			return	!(mBestLight == null && mBestLightMover.Done());
		}


		//TODO: mover3 might cause nans if lerping between
		//opposite axiseesseseseeseses, use quat slerp?
		public void Update(int msDelta, Vector3 pos)
		{
			Zone.ZoneLight	zl	=mZone.GetStrongestLightInLOS(pos, mSunEnt, mStyleStrength);
			if(zl == null)
			{
				//no lights in LOS, lerp to dark
				if(mbLerpingToDark)
				{
					if(mBestColorMover.Done())
					{
						mLightColor	=Vector4.Zero;
					}
					else
					{
						mBestColorMover.Update(msDelta);
						mLightColor	=mBestColorMover.GetPos();
					}
				}
				else
				{
					mBestColorMover.SetUpMove(mLightColor, Vector4.Zero,
						LightLerpTime, LightEaseIn, LightEaseOut);

					mBestColorMover.Update(msDelta);
					mLightColor		=mBestColorMover.GetPos();
					mbLerpingToDark	=true;
				}
				mBestLight		=null;
				return;
			}

			mbLerpingToDark	=false;

			Vector3	curPos		=Vector3.Zero;
			Vector4 curColor	=Vector4.One;
			float	curStrength	=0f;
			bool	bReady		=false;

			if(mBestLight != zl)
			{
				if(mBestLight == null)
				{
					//lerp color from dark
					//lerp strength in the color w
					Vector4	start	=Vector4.Zero;
					Vector4	end		=Vector4.Zero;

					end.X	=zl.mColor.X;
					end.Y	=zl.mColor.Y;
					end.Z	=zl.mColor.Z;
					end.W	=zl.mStrength;

					//see if still lerping
					if(!mBestColorMover.Done())
					{
						start	=mBestColorMover.GetPos();
					}

					mBestColorMover.SetUpMove(start, end,
						LightLerpTime, LightEaseIn, LightEaseOut);

					curPos		=zl.mPosition;
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

			if(zl.mbSun)
			{
				mCurLightDir	=-zl.mPosition;	//direction stored in pos
				mLightColor.X	=curColor.X;
				mLightColor.Y	=curColor.Y;
				mLightColor.Z	=curColor.Z;
				mLightColor.W	=1f;
			}
			else
			{
				Vector3	curLightDir	=curPos - pos;

				float	dist	=curLightDir.Length();

				curLightDir	/=dist;

				float	atten	=curStrength - dist;
				if(atten <= 0f)
				{
					//too far to affect us
					mLightColor		=Vector4.Zero;
					mLightColor.W	=1.0f;
					return;
				}

				mLightColor.X	=curColor.X;
				mLightColor.Y	=curColor.Y;
				mLightColor.Z	=curColor.Z;

				atten	/=curStrength;

				mLightColor	*=atten;

				//check the light style if applicable
				if(mBestLight.mStyle != 0)
				{
					mLightColor	*=mStyleStrength(mBestLight.mStyle);
				}

				mLightColor.W	=1.0f;
				mCurLightDir	=curLightDir;
			}
		}


		void GrabSun()
		{
			List<ZoneEntity>	suns	=mZone.GetEntities("light_sun");
			if(suns.Count == 0)
			{
				mSunEnt	=null;
				return;
			}

			Vector3	angles;
			if(!suns[0].GetVectorNoConversion("angles", out angles))
			{
				mSunEnt	=null;
				return;
			}
			mSunEnt	=suns[0];
		}
	}
}
