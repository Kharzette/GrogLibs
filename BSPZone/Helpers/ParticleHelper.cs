using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using ParticleLib;


namespace BSPZone
{
	//handles particle entities in the world
	public class ParticleHelper
	{
		Zone			mZone;
		ParticleBoss	mPB;

		List<int>	mIndexes	=new List<int>();

		bool	mbMiscListening;


		public void Initialize(Zone zone, ParticleBoss pb)
		{
			mZone	=zone;
			mPB		=pb;

			//clear old particles
			foreach(int idx in mIndexes)
			{
				mPB.NukeEmitter(idx);
			}

			mIndexes.Clear();

			Array	shapeVals	=Enum.GetValues(typeof(Emitter.Shapes));

			Dictionary<string, Vector3>	gravTargs	=new Dictionary<string, Vector3>();

			//grab any gravity entities
			List<ZoneEntity>	gravs	=mZone.GetEntitiesStartsWith("misc_particle_grav");
			foreach(ZoneEntity grav in gravs)
			{
				string	targ	=grav.GetTargetName();
				if(targ == null)
				{
					continue;
				}
				if(targ == "")
				{
					continue;
				}

				Vector3	org;
				if(!grav.GetOrigin(out org))
				{
					continue;
				}

				gravTargs.Add(targ, org);
			}

			//grab out all particle emitters
			List<ZoneEntity>	parts	=mZone.GetEntitiesStartsWith("misc_particle_emitter");
			foreach(ZoneEntity ze in parts)
			{
				Vector4	color;
				Vector3	col, pos, gravPos;
				Vector3	colVelMin, colVelMax;
				Vector4	colorVelMin, colorVelMax;
				bool	bOn;
				int		shapeIdx, maxParticles;
				float	gravStr, startSize, emitMS, shapeSize;
				float	velMin, velMax, velCap, alphaVelMin, alphaVelMax;
				float	sizeVelMin, sizeVelMax, spinVelMin, spinVelMax;
				float	lifeMin, lifeMax;

				Emitter.Shapes	shape;

				ze.GetOrigin(out pos);

				if(!ze.GetVectorNoConversion("start_color", out col))
				{
					color	=Vector4.One;
				}
				else
				{
					color.X	=col.X;
					color.Y	=col.Y;
					color.Z	=col.Z;
					Mathery.TryParse(ze.GetValue("start_alpha"), out color.W);				
				}
				Mathery.TryParse(ze.GetValue("max_particles"), out maxParticles);
				Mathery.TryParse(ze.GetValue("shape"), out shapeIdx);
				Mathery.TryParse(ze.GetValue("shape_size"), out shapeSize);
				Mathery.TryParse(ze.GetValue("grav_strength"), out gravStr);
				Mathery.TryParse(ze.GetValue("start_size"), out startSize);
				Mathery.TryParse(ze.GetValue("emit_ms"), out emitMS);
				Mathery.TryParse(ze.GetValue("velocity_min"), out velMin);
				Mathery.TryParse(ze.GetValue("velocity_max"), out velMax);
				Mathery.TryParse(ze.GetValue("velocity_cap"), out velCap);
				Mathery.TryParse(ze.GetValue("size_velocity_min"), out sizeVelMin);
				Mathery.TryParse(ze.GetValue("size_velocity_max"), out sizeVelMax);
				Mathery.TryParse(ze.GetValue("spin_velocity_min"), out spinVelMin);
				Mathery.TryParse(ze.GetValue("spin_velocity_max"), out spinVelMax);
				Mathery.TryParse(ze.GetValue("alpha_velocity_min"), out alphaVelMin);
				Mathery.TryParse(ze.GetValue("alpha_velocity_max"), out alphaVelMax);
				Mathery.TryParse(ze.GetValue("lifetime_min"), out lifeMin);
				Mathery.TryParse(ze.GetValue("lifetime_max"), out lifeMax);

				ze.GetVectorNoConversion("grav_loc", out gravPos);
				ze.GetVectorNoConversion("color_velocity_min", out colVelMin);
				ze.GetVectorNoConversion("color_velocity_max", out colVelMax);

				colorVelMin	=new Vector4(colVelMin.X, colVelMin.Y, colVelMin.Z, alphaVelMin);
				colorVelMax	=new Vector4(colVelMax.X, colVelMax.Y, colVelMax.Z, alphaVelMax);

				int	bVal;
				Mathery.TryParse(ze.GetValue("activated"), out bVal);
				bOn	=(bVal != 0);

				if(shapeIdx >= 0 && shapeIdx < shapeVals.Length)
				{
					shape	=(Emitter.Shapes)shapeVals.GetValue(shapeIdx);
				}
				else
				{
					shape	=Emitter.Shapes.Point;
				}

				//scale some to millisecond values
				spinVelMin	/=1000f;
				spinVelMax	/=1000f;
				velMin		/=1000f;
				velMax		/=1000f;
				sizeVelMin	/=1000f;
				sizeVelMax	/=1000f;
				lifeMin		*=1000;
				lifeMax		*=1000;

				//color scaled even smaller
				colorVelMin	/=10000f;
				colorVelMax	/=10000f;

				//set up the gravity pos if any
				string	targ			=ze.GetTarget();
				Vector3	gravityPosition	=gravPos;
				if(gravTargs.ContainsKey(targ))
				{
					//aimed at an entity
					gravityPosition	=gravTargs[targ];
				}
				else
				{
					//relative to emitter position
					gravityPosition	+=pos;
				}

				int	idx	=mPB.CreateEmitter(
					ze.GetValue("tex_name"),
					color, shape, shapeSize, maxParticles,
					pos, gravityPosition, gravStr,
					startSize, emitMS, spinVelMin, spinVelMax,
					velMin, velMax, velCap,
					sizeVelMin, sizeVelMax,
					colorVelMin, colorVelMax,
					(int)lifeMin, (int)lifeMax);

				if(idx == -1)
				{
					continue;	//creation failed, probably missing texture
				}

				Matrix	orient;
				if(ze.GetMatrixFromAngles("angles", out orient))
				{
					mPB.GetEmitterByIndex(idx).mLineAxis	=
						Vector3.TransformNormal(Vector3.UnitZ, orient);
				}

				ze.SetInt("EmitterIndex", idx);

				mPB.GetEmitterByIndex(idx).mbOn	=bOn;

				mIndexes.Add(idx);
			}
		}


		public void ActivateEmitter(int index, bool bOn)
		{
			if(index < 0 || mIndexes.Count <= index)
			{
				return;
			}
			
			mPB.GetEmitterByIndex(mIndexes[index]).mbOn	=bOn;
		}


		void OnTriggerMisc(object sender, EventArgs ea)
		{
			ZoneEntity	ze	=sender as ZoneEntity;
			if(ze == null)
			{
				return;
			}

			string	className	=ze.GetValue("classname");
			if(!className.StartsWith("misc_particle"))
			{
				return;
			}

			int	index	=0;
			if(!ze.GetInt("EmitterIndex", out index))
			{
				return;
			}

			Emitter	em	=mPB.GetEmitterByIndex(index);
			if(em == null)
			{
				return;
			}

			bool	bOn	=ze.ToggleEntityActivated();

			em.mbOn	=bOn;
		}
	}
}
