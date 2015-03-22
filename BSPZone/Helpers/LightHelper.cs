using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UtilityLib;
using BSPZone;
using SharpDX;

using DynLight	=MaterialLib.DynamicLights.DynLight;
using ZoneLight	=BSPZone.Zone.ZoneLight;

namespace BSPZone
{
	//an instance of this is created for every object
	//in the world that needs lighting
	public class LightHelper
	{
		//see http://home.comcast.net/~tom_forsyth/blog.wiki.html#Trilights
		internal class TriLightFill
		{
			internal Vector3	mPosition;
			internal Vector4	mColor1;
			internal Vector4	mColor2;
		}

		Zone	mZone;

		//sunlight entity
		ZoneEntity	mSunEnt;

		//lighting stuff on the move
		Mover3					mBestLightMover	=new Mover3();
		Mover4					mBestColorMover	=new Mover4();
		object					mBestLight;
		Zone.GetStyleStrength	mStyleStrength;
		bool					mbLerpingToDark;

		//current light values post update
		Vector4	mLightColor;
		Vector3	mCurLightDir;
		Vector3	mCurLightPos;
		Vector4	mFill1, mFill2;

		//list of all affecting lights
		//either dyn or zone
		List<object>	mAffecting	=new List<object>();

		//list of all trilight fill entity values
		List<TriLightFill>	mFills	=new List<TriLightFill>();

		//constants
		const float	LightLerpTime	=0.25f;	//in seconds
		const float	LightEaseIn		=0.2f;
		const float	LightEaseOut	=0.2f;
		const int	FillDistance	=1000;


		public void Initialize(Zone z, Zone.GetStyleStrength styleStrength)
		{
			mBestLight	=null;	//make sure this doesn't hold up free

			mZone			=z;
			mStyleStrength	=styleStrength;

			GrabSun();

			GrabFills();
		}


		public List<object> GetAffecting()
		{
			return	mAffecting;
		}


		public bool GetCurrentValues(out Vector4 color0,
			out Vector4 color1,
			out Vector4 color2,
			out float intensity,
			out Vector3 lightPos,
			out Vector3 lightDir,
			out bool bDirectional)
		{
			color0		=mLightColor;
			lightDir	=mCurLightDir;
			lightPos	=mCurLightPos;
			color1		=mFill1;
			color2		=mFill2;

			if(mBestLight == null)
			{
				bDirectional	=true;
				intensity		=1;
				return	false;
			}

			bDirectional	=GetLightSun(mBestLight);
			intensity		=GetLightStrength(mBestLight);

			return	true;
		}


		public bool NeedsShadow()
		{
			return	!(mBestLight == null && mBestLightMover.Done());
		}


		//TODO: mover3 might cause nans if lerping between
		//opposite axiseesseseseeseses, use quat slerp?
		public void Update(int msDelta, Vector3 pos, MaterialLib.DynamicLights dyn)
		{
			//calculate fill lights
			CalcFill(pos);

			mAffecting	=mZone.GetAffectingLights(pos, mSunEnt, mStyleStrength, dyn);

			//look for the strongest for the trilight lighting
			float	bestDist	=float.MaxValue;
			object	bestLight	=null;
			foreach(object light in mAffecting)
			{
				Vector3	lightPos	=GetLightPosition(light);
				float	strength	=GetLightStrength(light);

				float	dist	=Vector3.Distance(pos, lightPos);

				if(dist >= strength)
				{
					continue;
				}

				int	style	=GetLightStyle(light);

				if(style != 0)
				{
					dist	-=(strength * mStyleStrength(style));
				}
				else
				{
					dist	-=strength;
				}

				if(dist < bestDist)
				{
					bestLight	=light;
					bestDist	=dist;
				}
			}

			float	bestStrength	=GetLightStrength(bestLight);
			Vector3	bestColor		=GetLightColor(bestLight);
			Vector3	bestPosition	=GetLightPosition(bestLight);
			bool	bestSun			=GetLightSun(bestLight);

			//check for a sun
			foreach(object light in mAffecting)
			{
				ZoneLight	zl	=light as ZoneLight;
				if(zl == null || !zl.mbSun || !zl.mbOn)
				{
					continue;
				}

				if(bestLight != null)
				{
					float	bestLightPower	=bestDist;
					if(bestDist > bestStrength)
					{
						bestLightPower	*=(bestStrength / bestDist);
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
						mFill1		=Vector4.Zero;
						mFill2		=Vector4.Zero;
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

					end.X	=bestColor.X;
					end.Y	=bestColor.Y;
					end.Z	=bestColor.Z;
					end.W	=bestStrength;

					//see if still lerping
					if(!mBestColorMover.Done())
					{
						start	=mBestColorMover.GetPos();
					}

					mBestColorMover.SetUpMove(start, end,
						LightLerpTime, LightEaseIn, LightEaseOut);

					//works for suns or points
					curPos		=bestPosition;
				}
				else
				{
					bool	prevBestSun	=GetLightSun(mBestLight);
					Vector3	prevBestPos	=GetLightPosition(mBestLight);

					if(!prevBestSun && !bestSun		//lerping from point to point?
						|| prevBestSun && bestSun)	//or sun to sun?
					{
						//if still lerping, use the lerp position
						if(!mBestLightMover.Done())
						{
							mBestLightMover.SetUpMove(mBestLightMover.GetPos(), bestPosition,
								LightLerpTime, LightEaseIn, LightEaseOut);
						}
						else
						{
							mBestLightMover.SetUpMove(prevBestPos, bestPosition,
								LightLerpTime, LightEaseIn, LightEaseOut);
						}
					}
					else if(!prevBestSun && bestSun)	//from point to sun?
					{
						Vector3	lerpPos	=Vector3.Zero;

						//if still lerping, use the lerp position
						if(!mBestLightMover.Done())
						{
							lerpPos	=mBestLightMover.GetPos();
						}
						else
						{
							lerpPos	=prevBestPos;
						}

						//convert position to a direction
						Vector3	lerpDir	=lerpPos - pos;
						lerpDir.Normalize();

						//set up lerp
						mBestLightMover.SetUpMove(lerpDir, bestPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}
					else if(prevBestSun && !bestSun)	//from sun to point
					{
						Vector3	lerpDir	=Vector3.Zero;

						//if still lerping, use the lerp direction
						if(!mBestLightMover.Done())
						{
							lerpDir	=mBestLightMover.GetPos();
						}
						else
						{
							lerpDir	=prevBestPos;
						}

						//convert direction to a position to lerp from
						Vector3	lerpPos	=pos + (lerpDir * -1000f);

						//set up lerp
						mBestLightMover.SetUpMove(lerpPos, bestPosition,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}

					//lerp strength in the color w
					Vector4	start	=Vector4.Zero;
					Vector4	end		=Vector4.Zero;

					end.X	=bestColor.X;
					end.Y	=bestColor.Y;
					end.Z	=bestColor.Z;
					end.W	=bestStrength;

					Vector3	prevBestColor	=GetLightColor(mBestLight);
					float	prevBestStr		=GetLightStrength(mBestLight);

					//see if still lerping
					if(mBestColorMover.Done())
					{
						start.X	=prevBestColor.X;
						start.Y	=prevBestColor.Y;
						start.Z	=prevBestColor.Z;
						start.W	=prevBestStr;
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
					curPos		=GetLightPosition(mBestLight);
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
					Vector3	prevBestColor	=GetLightColor(mBestLight);
					float	prevBestStr		=GetLightStrength(mBestLight);

					curColor.X	=prevBestColor.X;
					curColor.Y	=prevBestColor.Y;
					curColor.Z	=prevBestColor.Z;
					curColor.W	=1f;
					curStrength	=prevBestStr;
				}
			}

			if(bestSun)
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
					mFill1			=mLightColor;
					mFill2			=mLightColor;
					return;
				}

				mLightColor.X	=curColor.X;
				mLightColor.Y	=curColor.Y;
				mLightColor.Z	=curColor.Z;

				atten	/=curStrength;

				mLightColor	*=atten;
				mFill1		*=atten;
				mFill2		*=atten;

				//check the light style if applicable
				int	prevStyle	=GetLightStyle(mBestLight);
				if(prevStyle != 0)
				{
					float	styleStrength	=mStyleStrength(prevStyle);
					mLightColor	*=styleStrength;
					mFill1		*=styleStrength;
					mFill2		*=styleStrength;
				}

				mLightColor.W	=1f;
				mFill1.W		=1f;
				mFill2.W		=1f;
				mCurLightDir	=curLightDir;
				mCurLightPos	=curPos;
			}
		}


		void CalcFill(Vector3 pos)
		{
			Debug.Assert(mFills.Count > 0);

			List<TriLightFill>	inRange	=new List<TriLightFill>();

			//find all fills in range
			foreach(TriLightFill fill in mFills)
			{
				float	dist	=Vector3.Distance(fill.mPosition, pos);
				if(dist > FillDistance)
				{
					continue;
				}
				inRange.Add(fill);
			}

			mFill1	=Vector4.Zero;
			mFill2	=Vector4.Zero;

			if(inRange.Count <= 0)
			{
				float	bestDist	=float.MaxValue;

				//just use nearest
				foreach(TriLightFill fill in mFills)
				{
					float	dist	=Vector3.Distance(fill.mPosition, pos);
					if(dist < bestDist)
					{
						bestDist	=dist;
						mFill1		=fill.mColor1;
						mFill2		=fill.mColor2;
					}
				}
				return;
			}

			//average out weighting by distance
			float	total	=0;
			foreach(TriLightFill fill in inRange)
			{
				float	dist	=Vector3.Distance(fill.mPosition, pos);

				float	ratio	=1f - (dist / FillDistance);

				total	+=ratio;
			}

			float	totalPieces	=0f;
			foreach(TriLightFill fill in inRange)
			{
				float	dist	=Vector3.Distance(fill.mPosition, pos);
				float	ratio	=(1f - (dist / FillDistance)) / total;

				totalPieces	+=ratio;

				mFill1	+=fill.mColor1 * ratio;
				mFill2	+=fill.mColor2 * ratio;
			}

			Debug.Assert(totalPieces > 0.99f && totalPieces < 1.01f);
		}


		void GrabFills()
		{
			List<ZoneEntity>	fills	=mZone.GetEntities("misc_trilight_info");
			if(fills.Count == 0)
			{
				//I guess just use a default
				TriLightFill	defaultFill	=new TriLightFill();

				//sort of a blue sky type thing
				defaultFill.mColor1	=new Vector4(0.3f, 0.5f, 0.7f, 1f);

				//earthy color
				defaultFill.mColor2	=new Vector4(0.6f, 0.5f, 0.4f, 1f);

				defaultFill.mPosition	=Vector3.Zero;

				mFills.Add(defaultFill);
				return;
			}

			foreach(ZoneEntity ze in fills)
			{
				TriLightFill	fill	=new TriLightFill();

				Vector3	cVal;
				ze.GetVectorNoConversion("trilight1", out cVal);
				fill.mColor1	=new Vector4(cVal.X, cVal.Y, cVal.Z, 1f);

				ze.GetVectorNoConversion("trilight2", out cVal);
				fill.mColor2	=new Vector4(cVal.X, cVal.Y, cVal.Z, 1f);

				ze.GetOrigin(out fill.mPosition);

				mFills.Add(fill);
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

			if(suns.Count > 1)
			{
				Debug.WriteLine("Dakkon Warning!  There cannot be two skies!\n");
			}

			Vector3	angles;
			if(!suns[0].GetVectorNoConversion("angles", out angles))
			{
				mSunEnt	=null;
				return;
			}
			//there cannot be two skies
			mSunEnt	=suns[0];
		}

		#region LightGetHelpers
		internal static float	GetLightStrength(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mStrength;
			}
			else if(light is DynLight)
			{
				return	(light as DynLight).mStrength;
			}
			return	0f;
		}

		internal static Vector3	GetLightPosition(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mPosition;
			}
			else if(light is DynLight)
			{
				return	(light as DynLight).mPosition;
			}
			return	Vector3.Zero;
		}

		internal static Vector3	GetLightColor(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mColor;
			}
			else if(light is DynLight)
			{
				return	(light as DynLight).mColor;
			}
			return	Vector3.Zero;
		}

		internal static int	GetLightStyle(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mStyle;
			}
			return	0;
		}

		internal static bool	GetLightOn(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mbOn;
			}
			else if(light is DynLight)
			{
				return	(light as DynLight).mbOn;
			}
			return	false;
		}

		internal static bool	GetLightSwitchable(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mbSwitchable;
			}
			return	false;
		}

		internal static bool	GetLightSun(object light)
		{
			if(light is ZoneLight)
			{
				return	(light as ZoneLight).mbSun;
			}
			return	false;
		}
		#endregion
	}
}
