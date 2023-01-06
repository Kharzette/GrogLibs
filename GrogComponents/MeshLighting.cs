using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using BSPZone;
using UtilityLib;


namespace EntityLib
{
	//use this with any mesh object that needs lighting
	public class MeshLighting : Component
	{
		//see http://home.comcast.net/~tom_forsyth/blog.wiki.html#Trilights
		internal class TriLightFill
		{
			internal Vector3	mPosition;
			internal Vector4	mColor1;
			internal Vector4	mColor2;
		}

		Zone	mZone;

		//Designated sun if any
		Light	mSunLight;

		//lighting stuff on the move
		//position or direction in xyz, attenuation factor in w
		Mover4	mBestLightMover	=new Mover4();

		//color in xyz, intensity (for shadow attenuation) in w
		Mover4	mBestColorMover	=new Mover4();

		Light				mBestLight;
		GetStyleStrength	mStyleStrength;
		bool				mbLerpingToDark;
		float				mLightTraceOffset;	//adjust from base to center

		//current light values post update
		Vector4	mLightColor;
		Vector3	mCurLightDir;
		Vector3	mCurLightPos;
		Vector4	mFill1, mFill2;

		//list of all affecting lights
		//either dyn or zone
		List<Light>	mAffecting	=new List<Light>();

		//list of all trilight fill entity values
		List<TriLightFill>	mFills	=new List<TriLightFill>();

		//delegate for light style strength
		public delegate float GetStyleStrength(int styleIndex);

		//constants
		const float	LightLerpTime	=0.25f;	//in seconds
		const float	LightEaseIn		=0.2f;
		const float	LightEaseOut	=0.2f;
		const int	FillDistance	=1000;


		public MeshLighting(Entity owner, Zone z, float lightTraceOffset, GetStyleStrength gss) : base(owner)
		{
			mBestLight	=null;	//make sure this doesn't hold up free

			//so stuff on the ground doesn't immediately
			//hit the floor when raycasting to lights
			mLightTraceOffset	=lightTraceOffset;

			mZone			=z;
			mStyleStrength	=gss;

			GrabSun();

			GrabFills();
		}


		public List<Light> GetAffecting()
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
			lightDir	=-mCurLightDir;	//note the negate
			lightPos	=mCurLightPos;
			color1		=mFill1;
			color2		=mFill2;

			if(mBestLight == null)
			{
				bDirectional	=true;
				intensity		=1;
				return	false;
			}

			bDirectional	=mBestLight.mbSun;
			intensity		=mBestLight.GetStrength();

			return	true;
		}


		public bool NeedsShadow()
		{
			return	!(mBestLight == null && mBestLightMover.Done());
		}


		//TODO: mover3 might cause nans if lerping between
		//opposite axiseesseseseeseses, use quat slerp?
		public void Update(float secDelta, Vector3 pos)
		{
			Debug.Assert(secDelta > 0f);	//zero deltatimes are not good for this stuff

			//calculate fill lights
			CalcFill(pos);

			Light	bestLight	=FindBestLight(pos);

			//special case for no lights affecting
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
						mBestColorMover.Update(secDelta);
						mLightColor	=mBestColorMover.GetPos();
					}
				}
				else
				{
					mBestColorMover.SetUpMove(mLightColor, Vector4.Zero,
						LightLerpTime, LightEaseIn, LightEaseOut);

					mBestColorMover.Update(secDelta);
					mLightColor		=mBestColorMover.GetPos();
					mbLerpingToDark	=true;
				}

				//hacky fill light fade out
				float	hackMag	=mLightColor.X + mLightColor.Y + mLightColor.Z;
				hackMag	/=3f;

				mBestLight	=null;
				mFill1		*=hackMag;
				mFill2		*=hackMag;
				return;
			}

			mbLerpingToDark	=false;

			if(mBestLight != bestLight)
			{
				Vector4	targPos	=ComputeTargetPositionAttenuation(pos, bestLight);
				SetUpLightInterpolation(pos, bestLight, targPos);
				mBestLight	=bestLight;
			}

			if(!mBestLightMover.Done())
			{
				mBestLightMover.Update(secDelta);
				mBestColorMover.Update(secDelta);

				Vector4	curLight	=mBestLightMover.GetPos();
				Vector4	curColor	=mBestColorMover.GetPos();

				mLightColor		=curColor;
				mCurLightDir	=curLight.XYZ();
				mCurLightPos	=curLight.XYZ();

				//attenuate during interpolation always
				mLightColor	=mLightColor.MulXYZ(curLight.W);
				mFill1		=mFill1.MulXYZ(curLight.W);
				mFill2		=mFill2.MulXYZ(curLight.W);
			}
			else
			{
				mCurLightDir	=mCurLightPos	=mBestLight.mPosition;

				mLightColor	=mBestLight.mColor.ToV4(mBestLight.GetStrength());

				if(!mBestLight.mbSun)
				{
					//attenuate
					float	atten	=ComputeTargetPositionAttenuation(pos, mBestLight).W;

					mLightColor	=mLightColor.MulXYZ(atten);
					mFill1		=mFill1.MulXYZ(atten);
					mFill2		=mFill2.MulXYZ(atten);

					//check the light style if applicable
					int	style	=mBestLight.mStyle;
					if(style != 0)
					{
						float	styleStrength	=mStyleStrength(style);

						mLightColor	=mLightColor.MulXYZ(styleStrength);
						mFill1		=mFill1.MulXYZ(styleStrength);
						mFill2		=mFill2.MulXYZ(styleStrength);
					}
				}
			}

			if(!mBestLight.mbSun)
			{
				//make direction
				mCurLightDir	=pos - mCurLightPos;
				mCurLightDir.Normalize();
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


		void SetUpLightInterpolation(Vector3 pos, Light newBest, Vector4 targPos)
		{
			if(mBestLight == null)
			{
				//lerp color from dark
				//lerp strength in the color w
				Vector4	start	=Vector4.Zero;
				Vector4	end		=newBest.mColor.ToV4(newBest.GetStrength());

				//see if still lerping
				if(!mBestColorMover.Done())
				{
					start	=mBestColorMover.GetPos();
				}

				mBestColorMover.SetUpMove(start, end,
					LightLerpTime, LightEaseIn, LightEaseOut);
			}
			else
			{
				bool	bestSun		=newBest.mbSun;
				bool	prevBestSun	=mBestLight.mbSun;
				Vector4	prevBestPos	=ComputeTargetPositionAttenuation(pos, mBestLight);
				float	prevBestStr	=mBestLight.GetStrength();

				if(!prevBestSun && !bestSun		//lerping from point to point?
					|| prevBestSun && bestSun)	//or sun to sun?
				{
					//if still lerping, use the lerp position
					if(!mBestLightMover.Done())
					{
						mBestLightMover.SetUpMove(mBestLightMover.GetPos(), targPos,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}
					else
					{
						mBestLightMover.SetUpMove(prevBestPos, targPos,
							LightLerpTime, LightEaseIn, LightEaseOut);
					}
				}
				else if(!prevBestSun && bestSun)	//from point to sun?
				{
					Vector4	lerpPos	=Vector4.Zero;

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
					Vector3	lerpDir	=pos - lerpPos.XYZ();
					lerpDir.Normalize();

					Vector4	lerpDirAtten	=lerpDir.ToV4(lerpPos.W);

					//set up lerp
					mBestLightMover.SetUpMove(lerpDirAtten, targPos,
						LightLerpTime, LightEaseIn, LightEaseOut);
				}
				else if(prevBestSun && !bestSun)	//from sun to point
				{
					Vector4	lerpDir	=Vector4.Zero;

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
					Vector3	lerpPos	=pos - (lerpDir.XYZ() * prevBestStr);

					//set up lerp
					mBestLightMover.SetUpMove(lerpPos.ToV4(prevBestPos.W), targPos,
						LightLerpTime, LightEaseIn, LightEaseOut);
				}

				//lerp strength in the color w
				Vector4	start	=mBestLight.mColor.ToV4(mBestLight.GetStrength());
				Vector4	end		=newBest.mColor.ToV4(newBest.GetStrength());

				//see if still lerping
				if(!mBestColorMover.Done())
				{
					start	=mBestColorMover.GetPos();
				}

				mBestColorMover.SetUpMove(start, end,
					LightLerpTime, LightEaseIn, LightEaseOut);
			}
		}


		Vector4	ComputeTargetPositionAttenuation(Vector3 pos, Light bestLight)
		{
			Vector4	ret		=Vector4.Zero;
			Vector3	bestPos	=bestLight.mPosition;
			bool	bSun	=bestLight.mbSun;

			if(bSun)
			{
				//this will be a direction
				ret	=bestPos.ToV4(1f);	//no atten for suns
			}
			else
			{
				Vector3	curLightDir	=bestPos - pos;

				float	dist	=curLightDir.Length();
				float	bestStr	=bestLight.GetStrength();
				float	atten	=bestStr - dist;
				if(atten <= 0f)
				{
					//too far to affect mesh
					atten	=0f;
				}
				else
				{
					atten	/=bestStr;
				}
				ret	=bestPos.ToV4(atten);
			}
			return	ret;
		}


		//check line of sight and distance for all lights
		//to see which affects our mesh owner
		void	SetAffectingLights(Vector3 pos)
		{
			//pull list of lights
			List<Component>	lights	=mOwner.mBoss.GetEntityComponents(typeof(Light));

			//grab positions for LOS check
			List<Vector3>	positions	=new List<Vector3>();

			foreach(Light lt in lights)
			{
				//sun will have a broken position but
				//that is corrected below
				positions.Add(lt.mPosition);
			}

			//zone returns indexes
			List<int>	losIndexes	=new List<int>();

			mZone.GetInLOS(pos, positions, ref losIndexes);

			mAffecting.Clear();

			//check attenuation
			foreach(int idx in losIndexes)
			{
				Light	lt	=lights[idx] as Light;

				bool	bOn		=lt.IsOn();
				bool	bSun	=lt.mbSun;
				if(!bOn || bSun)
				{
					continue;
				}
				Vector3	lpos	=lt.mPosition;
				float	dist	=Vector3.Distance(lpos, pos);
				float	atten	=0;
				int		style	=lt.mStyle;
				float	str		=lt.GetStrength();

				if(style != 0)
				{
					atten	=(str * mStyleStrength(style));
				}
				else
				{
					atten	=str;
				}

				if(dist <= atten)
				{
					mAffecting.Add(lt);
				}
			}

			//see if the sun is shining on pos
			//this is checked via collision with a sky face
			//along the sun's ray direction
			if(mSunLight != null)
			{
				BSPZone.Collision	col;
				if(mZone.TraceAll(null, null, pos,
					-mSunLight.mPosition * 10000 + pos, out col))
				{
					if(col.mFaceHit != null)
					{
						if(Misc.bFlagSet(BSPZone.Zone.SKY, col.mFaceHit.mFlags))
						{
							mAffecting.Add(mSunLight);
						}
					}
				}
			}
		}


		Light FindBestLight(Vector3 pos)
		{
			SetAffectingLights(pos);

			//look for the strongest for the trilight lighting
			float	bestDist	=float.MaxValue;
			Light	bestLight	=null;
			foreach(Light light in mAffecting)
			{
				if(light.mbSun)
				{
					continue;
				}
				
				Vector3	lightPos	=light.mPosition;
				float	strength	=light.GetStrength();

				float	dist	=Vector3.Distance(pos, lightPos);

				if(dist >= strength)
				{
					continue;
				}

				int	style	=light.mStyle;

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

			float	bestStrength	=float.MinValue;
			if(bestLight != null)
			{
				bestStrength	=bestLight.GetStrength();
			}

			//check for a sun
			foreach(Light light in mAffecting)
			{
				if(!light.mbSun)
				{
					continue;
				}

				//compare to best light so far
				if(bestLight != null)
				{
					float	bestLightPower	=bestDist;
					if(bestDist > bestStrength)
					{
						bestLightPower	*=(bestStrength / bestDist);
					}

					if(bestLightPower < light.GetStrength())
					{
						bestLight		=light;
					}				
				}
				else
				{
					bestLight		=light;
				}
			}

			return	bestLight;
		}


		void GrabFills()
		{
			mFills.Clear();

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
				fill.mColor1	=cVal.ToV4(1f);

				ze.GetVectorNoConversion("trilight2", out cVal);
				fill.mColor2	=cVal.ToV4(1f);

				ze.GetOrigin(out fill.mPosition);

				mFills.Add(fill);
			}
		}


		void GrabSun()
		{
			mSunLight	=null;

			List<Component>	lights	=mOwner.mBoss.GetEntityComponents(typeof(Light));

			foreach(Light lt in lights)
			{
				if(lt.mbSun)
				{
					mSunLight	=lt;
				}
			}
		}
	}
}