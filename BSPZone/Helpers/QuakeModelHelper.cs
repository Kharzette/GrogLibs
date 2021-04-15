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
	/*
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


		void ConvertTrain(Zone zone, ZoneEntity zeTrain, int modelIdx, Vector3 org)
		{
			string	nms	=zeTrain.GetTarget();
			if(nms == null || nms == "")
			{
				return;
			}

			if(!mModelStages.ContainsKey(modelIdx))
			{
				ModelStages	stg	=new ModelStages();
				mModelStages.Add(modelIdx, stg);
			}

			//grab speed
			float	speed	=-1f;
			if(!zeTrain.GetFloat("speed", out speed))
			{
				speed	=100f;	//default
			}

			Dictionary<string, ModelMoveStage>	stages	=new Dictionary<string, ModelMoveStage>();

			//path corners are offset by model mins for some strange reason
			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			//gather stages, origins at the track corners
			while(nms != null && nms != "")
			{
				List<ZoneEntity>	ents	=zone.GetEntitiesByTargetName(nms);
				if(ents.Count == 0)
				{
					break;
				}

				//think this should always only be one
				Debug.Assert(ents.Count == 1);

				Vector3	trackOrg	=Vector3.Zero;
				if(!ents[0].GetOrigin(out trackOrg))
				{
					break;
				}

				ModelMoveStage	mms	=new ModelMoveStage();
				mms.mModelIndex		=modelIdx;
				mms.mOrigin			=trackOrg - bbox.Minimum;
				mms.mStageInterval	=1f;
				mms.mEaseIn			=0.2f;
				mms.mEaseOut		=0.2f;

				stages.Add(nms, mms);

				nms	=ents[0].GetTarget();

				//see if this is a loop
				if(stages.ContainsKey(nms))
				{
					mModelStages[modelIdx].mbLooping	=true;
					break;
				}
			}

			//copy to iterate easier
			int	stageCount	=stages.Count;
			ModelMoveStage	[]copy	=new ModelMoveStage[stageCount];
			stages.Values.CopyTo(copy, 0);

			//figure out move amount
			for(int i=0;i < stageCount;i++)
			{
				int	j	=(i + 1) % stageCount;

				Vector3	startPos	=copy[i].mOrigin;
				Vector3	endPos		=copy[j].mOrigin;

				Vector3	dir	=endPos - startPos;

				float	len	=dir.Length();

				copy[i].mMoveAmount		=len;
				copy[i].mMoveAxis		=dir / len;
				copy[i].mStageInterval	=len / speed;
			}

			mModelStages[modelIdx].mStages.AddRange(copy);
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
				Matrix.RotationY(-MathUtil.DegreesToRadians(angle), out rot);
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
			foreach(ZoneEntity zea in entsAttached)
			{
				string	cname	=zea.GetValue("classname");
			}
		}


		void ConvertButton(Zone zone, ZoneEntity zeButton, int modelIdx, Vector3 org)
		{
			int		wait, angle;

			zeButton.GetInt("wait", out wait);
			zeButton.GetInt("angle", out angle);

			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			if(!mModelStages.ContainsKey(modelIdx))
			{
				ModelStages	stages	=new ModelStages();
				mModelStages.Add(modelIdx, stages);
			}
			
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
				Matrix.RotationY(-MathUtil.DegreesToRadians(angle), out rot);
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

			mModelStages[modelIdx].mStages.Add(mms);

		}
	}*/
}