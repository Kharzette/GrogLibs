using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
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


		public void Initialize(Zone zone, TriggerHelper th, ParticleBoss pb)
		{
			mZone	=zone;
			mPB		=pb;

			//clear old particles
			foreach(int idx in mIndexes)
			{
				mPB.NukeEmitter(idx);
			}

			mIndexes.Clear();

			//make sure to wire event once only
			if(!mbMiscListening)
			{
				th.eMisc		+=OnTriggerMisc;
				mbMiscListening	=true;
			}

			Array	shapeVals	=Enum.GetValues(typeof(Emitter.Shapes));

			//grab out all particle emitters
			List<ZoneEntity>	parts	=mZone.GetEntitiesStartsWith("misc_particle");
			foreach(ZoneEntity ze in parts)
			{
				Vector4	color;
				Vector3	col, pos;
				bool	bOn;
				int		shapeIdx, maxParticles, sortPri;
				float	gravYaw, gravPitch, shapeSize;
				float	gravStr, startSize, startAlpha, emitMS;
				float	velMin, velMax;
				float	sizeVelMin, sizeVelMax, spinVelMin, spinVelMax;
				float	alphaVelMin, alphaVelMax, lifeMin, lifeMax;

				Emitter.Shapes	shape;

				ze.GetOrigin(out pos);

				if(!ze.GetVectorNoConversion("color", out col))
				{
					color	=Vector4.One;
				}
				else
				{
					color.X	=col.X;
					color.Y	=col.Y;
					color.Z	=col.Z;
					Mathery.TryParse(ze.GetValue("alpha"), out color.W);				
				}
				Mathery.TryParse(ze.GetValue("max_particles"), out maxParticles);
				Mathery.TryParse(ze.GetValue("shape"), out shapeIdx);
				Mathery.TryParse(ze.GetValue("shape_size"), out shapeSize);
				Mathery.TryParse(ze.GetValue("grav_yaw"), out gravYaw);
				Mathery.TryParse(ze.GetValue("grav_pitch"), out gravPitch);
				Mathery.TryParse(ze.GetValue("grav_strength"), out gravStr);
				Mathery.TryParse(ze.GetValue("start_size"), out startSize);
				Mathery.TryParse(ze.GetValue("start_alpha"), out startAlpha);
				Mathery.TryParse(ze.GetValue("emit_ms"), out emitMS);
				Mathery.TryParse(ze.GetValue("velocity_min"), out velMin);
				Mathery.TryParse(ze.GetValue("velocity_max"), out velMax);
				Mathery.TryParse(ze.GetValue("size_velocity_min"), out sizeVelMin);
				Mathery.TryParse(ze.GetValue("size_velocity_max"), out sizeVelMax);
				Mathery.TryParse(ze.GetValue("spin_velocity_min"), out spinVelMin);
				Mathery.TryParse(ze.GetValue("spin_velocity_max"), out spinVelMax);
				Mathery.TryParse(ze.GetValue("alpha_velocity_min"), out alphaVelMin);
				Mathery.TryParse(ze.GetValue("alpha_velocity_max"), out alphaVelMax);
				Mathery.TryParse(ze.GetValue("lifetime_min"), out lifeMin);
				Mathery.TryParse(ze.GetValue("lifetime_max"), out lifeMax);
				Mathery.TryParse(ze.GetValue("sort_priority"), out sortPri);

				int	bVal;
				Mathery.TryParse(ze.GetValue("activated"), out bVal);
				bOn	=(bVal != 0);

				Mathery.WrapAngleDegrees(ref gravYaw);
				Mathery.WrapAngleDegrees(ref gravPitch);

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
				alphaVelMin	/=1000f;
				alphaVelMax	/=1000f;
				lifeMin		*=1000;
				lifeMax		*=1000;

				int	idx	=mPB.CreateEmitter(
					ze.GetValue("tex_name"),
					color, shape, shapeSize, maxParticles,
					pos, (int)gravYaw, (int)gravPitch, gravStr,
					startSize, startAlpha, emitMS, spinVelMin, spinVelMax,
					velMin, velMax, sizeVelMin, sizeVelMax,
					alphaVelMin, alphaVelMax,
					(int)lifeMin, (int)lifeMax, sortPri);

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
