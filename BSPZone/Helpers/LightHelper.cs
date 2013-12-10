using System;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using BSPZone;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	//an instance of this is created for every object
	//in the world that needs lighting
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
		Vector3	mCurLightPos;

		//list of all affecting lights
		List<Zone.ZoneLight>	mAffecting	=new List<Zone.ZoneLight>();

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


		public List<Zone.ZoneLight> GetAffecting()
		{
			return	mAffecting;
		}


		public bool GetCurrentValues(out Vector4 color0,
			out float intensity,
			out Vector3 lightPos,
			out Vector3 lightDir,
			out bool bDirectional)
		{
			color0			=mLightColor;
			lightDir		=mCurLightDir;
			lightPos		=mCurLightPos;

			if(mBestLight == null)
			{
				bDirectional	=true;
				intensity		=1;
				return	false;
			}

			bDirectional	=mBestLight.mbSun;
			intensity		=mBestLight.mStrength;

			return	true;
		}


		public bool NeedsShadow()
		{
			return	!(mBestLight == null && mBestLightMover.Done());
		}


		//TODO: mover3 might cause nans if lerping between
		//opposite axiseesseseseeseses, use quat slerp?
		public void Update(int msDelta, Vector3 pos, DynamicLights dyn)
		{
			mAffecting	=mZone.GetAffectingLights(pos, mSunEnt, mStyleStrength, dyn);

			//look for the strongest for the trilight lighting
			float			bestDist	=float.MaxValue;
			Zone.ZoneLight	bestLight	=null;
			foreach(Zone.ZoneLight zl in mAffecting)
			{
				float	dist	=Vector3.Distance(pos, zl.mPosition);

				if(dist >= zl.mStrength)
				{
					continue;
				}

				if(zl.mStyle != 0)
				{
					dist	-=(zl.mStrength * mStyleStrength(zl.mStyle));
				}
				else
				{
					dist	-=zl.mStrength;
				}

				if(dist < bestDist)
				{
					bestLight	=zl;
					bestDist	=dist;
				}
			}

			//check for a sun
			foreach(Zone.ZoneLight zl in mAffecting)
			{
				if(!zl.mbSun || !zl.mbOn)
				{
					continue;
				}

				if(bestLight != null)
				{
					float	bestLightPower	=bestDist;
					if(bestDist > bestLight.mStrength)
					{
						bestLightPower	*=(bestLight.mStrength / bestDist);
					}

					if(bestLightPower < zl.mStrength)
					{
						bestLight	=zl;
					}				
				}
			}

			if(bestLight == null)
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

			if(mBestLight != bestLight)
			{
				if(mBestLight == null)
				{
					//lerp color from dark
					//lerp strength in the color w
					Vector4	start	=Vector4.Zero;
					Vector4	end		=Vector4.Zero;

					end.X	=bestLight.mColor.X;
					end.Y	=bestLight.mColor.Y;
					end.Z	=bestLight.mColor.Z;
					end.W	=bestLight.mStrength;

					//see if still lerping
					if(!mBestColorMover.Done())
					{
						start	=mBestColorMover.GetPos();
					}

					mBestColorMover.SetUpMove(start, end,
						LightLerpTime, LightEaseIn, LightEaseOut);

					//works for suns or points
					curPos		=bestLight.mPosition;
				}
				else
				{
					if(!mBestLight.mbSun && !bestLight.mbSun		//lerping from point to point?
						|| mBestLight.mbSun && bestLight.mbSun)	//or sun to sun?
					{
						//if still lerping, use the lerp position
						if(!mBestLightMover.Done())
						{
							mBestLightMover.SetUpMove(mBestLightMover.GetPos(), bestLight.mPosition,
								LightLerpTime, LightEaseIn, LightEaseOut);
						}
						else
						{
							mBestLightMover.SetUpMove(mBestLight.mPosition, bestLight.mPosition,
								LightLerpTime, LightEaseIn, LightEaseOut);
						}
					}
					else if(!mBestLight.mbSun && bestLight.mbSun)	//from point to sun?
					{
						Vector3	lerpPos	=Vector3.Zero;

						//if still lerping, use the lerp position
						if(!mBestLightMover.Done())
						{
							lerpPos	=mBestLightMover.GetPos();
						}
						else
						{
							lerpPos	=mBestLight.mPosition;
						}

						//convert position to a direction
						Vector3	lerpDir	=lerpPos - pos;
						lerpDir.Normalize();

						//set up lerp
						mBestLightMover.SetUpMove(lerpDir, bestLight.mPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}
					else if(mBestLight.mbSun && !bestLight.mbSun)	//from sun to point
					{
						Vector3	lerpDir	=Vector3.Zero;

						//if still lerping, use the lerp direction
						if(!mBestLightMover.Done())
						{
							lerpDir	=mBestLightMover.GetPos();
						}
						else
						{
							lerpDir	=mBestLight.mPosition;
						}

						//convert direction to a position to lerp from
						Vector3	lerpPos	=pos + (lerpDir * -1000f);

						//set up lerp
						mBestLightMover.SetUpMove(lerpPos, bestLight.mPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}

					//lerp strength in the color w
					Vector4	start	=Vector4.Zero;
					Vector4	end		=Vector4.Zero;

					end.X	=bestLight.mColor.X;
					end.Y	=bestLight.mColor.Y;
					end.Z	=bestLight.mColor.Z;
					end.W	=bestLight.mStrength;

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
				mBestLight	=bestLight;
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

			if(bestLight.mbSun)
			{
				mCurLightDir	=-curPos;	//direction stored in pos
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
				mCurLightPos	=curPos;
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
