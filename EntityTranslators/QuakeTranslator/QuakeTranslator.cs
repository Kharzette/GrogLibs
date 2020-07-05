using System;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using BSPZone;


namespace EntityLib
{
	public class QuakeTranslator
	{
		public void TranslateModels(EntityBoss eb, Zone z)
		{
			//grab doors and lifts and such
			List<ZoneEntity>	funcs	=z.GetEntitiesStartsWith("func_");

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

				Entity	xEnt;

				string	className	=ze.GetValue("classname");
				if(className == "func_train")
				{
					xEnt	=ConvertTrain(z, ze, modelIdx, org);
				}
				else if(className == "func_button")
				{
					xEnt	=ConvertButton(z, ze, modelIdx, org);
				}
				else
				{
					xEnt	=ConvertDoor(z, ze, modelIdx, org);
				}

				if(xEnt != null)
				{
					eb.AddEntity(xEnt);
				}
			}
		}


		Entity ConvertTrain(Zone zone, ZoneEntity zeTrain, int modelIdx, Vector3 org)
		{
			string	nms	=zeTrain.GetTarget();
			if(nms == null || nms == "")
			{
				return	null;
			}

			//grab speed
			float	speed	=-1f;
			if(!zeTrain.GetFloat("speed", out speed))
			{
				speed	=100f;	//default
			}

			Dictionary<string, BModelMoveStage>	stages	=new Dictionary<string, BModelMoveStage>();

			//path corners are offset by model mins for some strange reason
			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			bool	bLooping	=false;

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

				BModelMoveStage	mms	=new BModelMoveStage(modelIdx,
					trackOrg - bbox.Minimum, 1f, 0.2f, 0.2f);

				stages.Add(nms, mms);

				nms	=ents[0].GetTarget();

				//see if this is a loop
				if(stages.ContainsKey(nms))
				{
					bLooping	=true;
					break;
				}
			}

			//copy to iterate easier
			int	stageCount	=stages.Count;
			BModelMoveStage	[]copy	=new BModelMoveStage[stageCount];
			stages.Values.CopyTo(copy, 0);

			//figure out move amount
			for(int i=0;i < stageCount;i++)
			{
				int	j	=(i + 1) % stageCount;

				Vector3	startPos	=copy[i].GetOrigin();
				Vector3	endPos		=copy[j].GetOrigin();

				Vector3	dir	=endPos - startPos;

				float	len	=dir.Length();

				copy[i].SetMovement(len, dir / len, len / speed);
			}

			BModelStages	bms	=new BModelStages(bLooping, copy);

			Entity	ret	=new Entity();

			BModelMover	bmm	=new BModelMover(modelIdx, bms, zone);

			ret.AddComponent(bmm);

			return	ret;
		}


		Entity ConvertDoor(Zone zone, ZoneEntity zeDoor, int modelIdx, Vector3 org)
		{
			int		wait, speed, angle, lip;

			zeDoor.GetInt("wait", out wait);
			zeDoor.GetInt("speed", out speed);
			zeDoor.GetInt("angle", out angle);
			zeDoor.GetInt("lip", out lip);

			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			BModelMoveStage	mms	=new BModelMoveStage(modelIdx, org, 1f, 0.2f, 0.2f);
//			mms.mAudio			=aud;
//			mms.mEmitter		=Audio.MakeEmitter(org);

			Vector3	moveAxis	=Vector3.Zero;
			if(angle >= 0)
			{
				Matrix	rot;
				Matrix.RotationY(-MathUtil.DegreesToRadians(angle), out rot);
				moveAxis	=Vector3.TransformNormal(Vector3.Right, rot);
			}
			else if(angle == -1)
			{
				//up
				moveAxis	=Vector3.Up;
			}
			else if(angle == -2)
			{
				//down
				moveAxis	=Vector3.Down;
			}

			//find size of model along move vec
			float	minDot	=Vector3.Dot(moveAxis, bbox.Minimum);
			float	maxDot	=Vector3.Dot(moveAxis, bbox.Maximum);

			float	moveAmount	=Math.Abs(minDot) + Math.Abs(maxDot);

			moveAmount	-=lip;

			mms.SetMovement(moveAmount, moveAxis, 1f);

			BModelStages	bms	=new BModelStages(false, mms);

			string	targetName	=zeDoor.GetTargetName();
			List<ZoneEntity>	entsAttached	=zone.GetEntitiesByTarget(targetName);
			foreach(ZoneEntity zea in entsAttached)
			{
				string	cname	=zea.GetValue("classname");
			}

			Entity	ret	=new Entity();

			BModelMover	bmm	=new BModelMover(modelIdx, bms, zone);

			ret.AddComponent(bmm);

			return	ret;
		}


		Entity ConvertButton(Zone zone, ZoneEntity zeButton, int modelIdx, Vector3 org)
		{
			int		wait, angle;

			zeButton.GetInt("wait", out wait);
			zeButton.GetInt("angle", out angle);

			BoundingBox	bbox	=zone.GetModelBounds(modelIdx);

			BModelMoveStage	mms	=new BModelMoveStage(modelIdx, org, 1f, 0.2f, 0.2f);

			Vector3	moveAxis	=Vector3.Zero;
			if(angle >= 0)
			{
				Matrix	rot;
				Matrix.RotationY(-MathUtil.DegreesToRadians(angle), out rot);
				moveAxis	=Vector3.TransformNormal(Vector3.Right, rot);
			}
			else if(angle == -1)
			{
				//up
				moveAxis	=Vector3.Up;
			}
			else if(angle == -2)
			{
				//down
				moveAxis	=Vector3.Down;
			}

			//find size of model along move vec
			float	minDot	=Vector3.Dot(moveAxis, bbox.Minimum);
			float	maxDot	=Vector3.Dot(moveAxis, bbox.Maximum);

			float	moveAmount	=Math.Abs(minDot) + Math.Abs(maxDot);

			mms.SetMovement(moveAmount, moveAxis, 1f);

			BModelStages	bms	=new BModelStages(false, mms);

			Entity	ret	=new Entity();
			
			BModelMover	bmm	=new BModelMover(modelIdx, bms, zone);

			ret.AddComponent(bmm);

			return	ret;
		}
	}
}