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
		internal void ConvertFuncs(Zone zone)
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

				string	className	=ze.GetValue("classname");
				if(className == "func_train")
				{
					ConvertTrain(zone, ze, modelIdx, org);
				}
				else if(className == "func_button")
				{
					ConvertButton(zone, ze, modelIdx, org);
				}
				else
				{
					ConvertDoor(zone, ze, modelIdx, org);
				}
			}
		}


		void ConvertTrain(Zone zone, ZoneEntity zeDoor, int modelIdx, Vector3 org)
		{
		}


		void ConvertDoor(Zone zone, ZoneEntity zeDoor, int modelIdx, Vector3 org)
		{
			int		wait, speed, angle, lip;

			zeDoor.GetInt("wait", out wait);
			zeDoor.GetInt("speed", out speed);
			zeDoor.GetInt("angle", out angle);
			zeDoor.GetInt("lip", out lip);

			if(!mModelStages.ContainsKey(modelIdx))
			{
				ModelStages	stages	=new ModelStages();
				mModelStages.Add(modelIdx, stages);
			}

			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			ModelMoveStage	mms	=new ModelMoveStage();
			mms.mModelIndex		=modelIdx;
			mms.mOrigin			=org;
//			mms.mAudio			=aud;
//			mms.mEmitter		=Audio.MakeEmitter(org);
			mms.mStageInterval	=1f;
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

			string	targetName	=zeDoor.GetTargetName();
			List<ZoneEntity>	entsAttached	=zone.GetEntitiesByTarget(targetName);
			if(entsAttached.Count == 0)
			{
				int	trigModNum	=zone.CopyModelToTrigger(modelIdx);

				//the door acts as it's own trigger
				ZoneTrigger	zt		=new ZoneTrigger();
				zt.mModelNum		=trigModNum;
				zt.mBox				=bbox;
				zt.mbTriggered		=false;
				zt.mbTriggerOnce	=(wait == -1);
				zt.mbTriggerStandIn	=false;

				//make a zone entity for this fakey trigger
				ZoneEntity	ze	=new ZoneEntity();
				if(wait == -1)
				{
					ze.mData.Add("classname", "trigger_once");
				}
				else
				{
					ze.mData.Add("classname", "trigger_stand_in");
				}

				if(targetName == "")
				{
					targetName	=zone.GenerateUniqueTargetName();
					zeDoor.mData.Add("targetname", targetName);
				}
				ze.mData.Add("target", targetName);

				zt.mEntity	=ze;

				zone.AddEntity(ze);
				zone.AddTrigger(zt);
			}
			foreach(ZoneEntity zea in entsAttached)
			{
				string	cname	=zea.GetValue("classname");
			}
		}


		void ConvertButton(Zone zone, ZoneEntity zeDoor, int modelIdx, Vector3 org)
		{
		}
	}
}