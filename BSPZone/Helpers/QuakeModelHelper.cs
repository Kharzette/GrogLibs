using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using AudioLib;
using UtilityLib;


namespace BSPZone
{
	internal partial class BasicModelHelper
	{
		internal void ConvertDoors(Zone zone)
		{
			//grab doors and lifts and such
			List<ZoneEntity>	funcs	=zone.GetEntitiesStartsWith("func_");

			foreach(ZoneEntity ze in funcs)
			{
				int		modelIdx;
				Vector3	org;

				if(!ze.GetInt("Model", out modelIdx))
				{
					continue;
				}
				if(!ze.GetVectorNoConversion("ModelOrigin", out org))
				{
					continue;
				}

				string	targetName	=ze.GetTargetName();

				int		wait, speed, angle, lip;

				ze.GetInt("wait", out wait);
				ze.GetInt("speed", out speed);
				ze.GetInt("angle", out angle);
				ze.GetInt("lip", out lip);

				if(!mModelStages.ContainsKey(modelIdx))
				{
					ModelStages	stages	=new ModelStages();
					mModelStages.Add(modelIdx, stages);
				}

				BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

				ModelMoveStage	mms	=new ModelMoveStage();
				mms.mModelIndex		=modelIdx;
				mms.mOrigin			=org;
//				mms.mAudio			=aud;
//				mms.mEmitter		=Audio.MakeEmitter(org);
				mms.mStageInterval	=2f;
				mms.mEaseIn			=0.2f;
				mms.mEaseOut		=0.2f;

				if(angle >= 0)
				{
					Matrix	rot;
					Matrix.RotationY(MathUtil.DegreesToRadians(angle), out rot);
					mms.mMoveAxis	=Vector3.TransformNormal(Vector3.Right, rot);
				}
				else if(angle == -1)
				{
					//up
					mms.mMoveAxis	=Vector3.Up;
				}
				else if(angle == -2)
				{
					//down
					mms.mMoveAxis	=Vector3.Down;
				}

				//find size of model along move vec
				float	minDot	=Vector3.Dot(mms.mMoveAxis, bbox.Minimum);
				float	maxDot	=Vector3.Dot(mms.mMoveAxis, bbox.Maximum);

				mms.mMoveAmount	=Math.Abs(minDot) + Math.Abs(maxDot);
				mms.mMoveAmount	-=lip;

				mModelStages[modelIdx].mStages.Add(mms);

				List<ZoneEntity>	entsAttached	=zone.GetEntitiesByTarget(targetName);
				foreach(ZoneEntity zea in entsAttached)
				{
					string	cname	=zea.GetValue("classname");
				}
			}
		}
	}
}