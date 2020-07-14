using System;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using BSPZone;
using UtilityLib;


namespace EntityLib
{
	public class QuakeTranslator
	{
		public void TranslateLights(EntityBoss eb, Zone z, Light.SwitchLight switchLight)
		{
			List<ZoneEntity>	lights	=z.GetEntitiesStartsWith("light");

			foreach(ZoneEntity ze in lights)
			{
				Vector3	lightPos;
				float	str;
				bool	bSun	=false;

				ze.GetOrigin(out lightPos);
				if(!ze.GetLightValue(out str))
				{
					if(ze.GetFloat("strength", out str))
					{
						bSun	=true;

						//stuff the direction in the position field
						Vector3	angles;
						if(!ze.GetVectorNoConversion("angles", out angles))
						{
							continue;	//something wrong with the entity
						}
						float	yaw		=angles.Y - 90;
						float	pitch	=angles.X;
						float	roll	=angles.Z;

						yaw		=MathUtil.DegreesToRadians(yaw);
						pitch	=MathUtil.DegreesToRadians(pitch);
						roll	=MathUtil.DegreesToRadians(roll);

						Matrix	rotMat	=Matrix.RotationYawPitchRoll(yaw, pitch, roll);

						lightPos	=rotMat.Backward;
					}
				}

				Vector3	color;
				ze.GetColor(out color);

				//check for switchable lights
				bool	bSwitchable, bOn	=true;
				int		switchNum			=-1;
				if(ze.GetInt("LightSwitchNum", out switchNum))
				{
					bSwitchable	=true;

					//quake spawnflags of 1 means start off, otherwise start on
					int	activated;
					if(ze.GetInt("spawnflags", out activated))
					{
						if(activated == 1)
						{
							bOn	=false;
						}
						else
						{
							bOn	=true;
						}
					}
					else
					{
						bOn	=true;
					}
				}
				else
				{
					bSwitchable	=false;
				}

				//check for styled lights
				int	style;
				ze.GetInt("style", out style);

				Entity	e	=new Entity(false, eb);

				Light	light	=new Light(e, str, style, lightPos, color,
									bOn, bSwitchable, bSun, switchNum, switchLight);

				e.AddComponent(light);

				eb.AddEntity(e);

				if(bSwitchable)
				{
					string	targName	=ze.GetTargetName();
					if(targName != "")
					{
						TargetName	tn	=new TargetName(targName, e);
						e.AddComponent(tn);
					}
					TriggerAble	ta	=new TriggerAble(e);

					e.AddComponent(ta);
				}
			}
		}


		public void TranslateTriggers(EntityBoss eb, Zone z)
		{
			List<ZoneEntity>	trigs	=z.GetEntitiesStartsWith("trigger");
			foreach(ZoneEntity ze in trigs)
			{
				if(ze.mData.ContainsKey("Model"))
				{
					string	modelNum	=ze.mData["Model"];

					int	modelNumi	=Convert.ToInt32(modelNum);

					double	delay	=0;

					if(ze.mData.ContainsKey("delay"))
					{
						if(Mathery.TryParse(ze.mData["delay"], out delay))
						{
							//bump to milliseconds
							delay	*=1000.0;
						}
					}

					string	targ	=ze.GetTarget();
					Entity	e		=new Entity(false, eb);					
					Trigger	t		=new Trigger(z, modelNumi,
						z.GetModelBounds(modelNumi),
						(ze.mData["classname"] == "trigger_once"),
						(ze.mData["classname"] == "trigger_stand_in"),
						targ, delay, e);

					e.AddComponent(t);
					eb.AddEntity(e);

					string	myTarg	=ze.GetTargetName();
					if(myTarg != "")
					{
						MakeEntityTargetName(e, myTarg);						
					}
				}
			}
		}


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

				Entity	xEnt	=new Entity(false, eb);

				string	className	=ze.GetValue("classname");
				if(className == "func_train")
				{
					ConvertTrain(z, ze, modelIdx, org, ref xEnt);
				}
				else if(className == "func_button")
				{
					ConvertButton(z, ze, modelIdx, org, ref xEnt);
				}
				else
				{
					ConvertDoor(z, ze, modelIdx, org, ref xEnt);
				}

				if(xEnt == null)
				{
					continue;
				}

				eb.AddEntity(xEnt);

				string	myTarg	=ze.GetTargetName();
				if(myTarg != "")
				{
					MakeEntityTargetName(xEnt, myTarg);
				}
			}
		}


		void ConvertTrain(Zone zone, ZoneEntity zeTrain, int modelIdx,
							Vector3 org, ref Entity outEnt)
		{
			string	nms	=zeTrain.GetTarget();
			if(nms == null || nms == "")
			{
				//no track to follow!
				outEnt	=null;
				return;
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
			BModelMover		bmm	=new BModelMover(modelIdx, bms, zone, outEnt);

			outEnt.AddComponent(bmm);

			string	myTarg	=zeTrain.GetTargetName();
			if(myTarg != "")
			{
				MakeEntityTargetName(outEnt, myTarg);
			}
		}


		void MakeEntityTargetName(Entity e, string targetName)
		{
			TargetName	tn	=new TargetName(targetName, e);

			e.AddComponent(tn);
		}


		void ConvertDoor(Zone zone, ZoneEntity zeDoor, int modelIdx,
						Vector3 org, ref Entity outEnt)
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

			BModelMover	bmm	=new BModelMover(modelIdx, bms, zone, outEnt);

			outEnt.AddComponent(bmm);
		}


		void ConvertButton(Zone zone, ZoneEntity zeButton, int modelIdx,
							Vector3 org, ref Entity outEnt)
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

			BModelMover	bmm	=new BModelMover(modelIdx, bms, zone, outEnt);

			outEnt.AddComponent(bmm);
		}
	}
}