using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;
using ParticleLib;


namespace BSPZone
{
	//handles particle entities in the world
	public class ParticleHelper
	{
		Zone			mZone;
		ParticleBoss	mPB;


		public void Initialize(Zone zone, ParticleBoss pb, string texPrefix)
		{
			mZone	=zone;
			mPB		=pb;

			Array	shapeVals	=Enum.GetValues(typeof(Emitter.Shapes));

			//grab out all particle emitters
			List<ZoneEntity>	parts	=mZone.GetEntitiesStartsWith("misc_particle");
			foreach(ZoneEntity ze in parts)
			{
				Vector4	color;
				Vector3	col, pos;
				bool	bCell, bOn;
				int		shapeIdx, maxParticles;
				float	gravYaw, gravPitch, gravRoll, shapeSize;
				float	gravStr, startSize, startAlpha, emitMS;
				float	rotVelMin, rotVelMax, velMin, velMax;
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
				Mathery.TryParse(ze.GetValue("grav_roll"), out gravRoll);
				Mathery.TryParse(ze.GetValue("grav_strength"), out gravStr);
				Mathery.TryParse(ze.GetValue("start_size"), out startSize);
				Mathery.TryParse(ze.GetValue("start_alpha"), out startAlpha);
				Mathery.TryParse(ze.GetValue("emit_ms"), out emitMS);
				Mathery.TryParse(ze.GetValue("rot_velocity_min"), out rotVelMin);
				Mathery.TryParse(ze.GetValue("rot_velocity_max"), out rotVelMax);
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


				int	bVal;
				Mathery.TryParse(ze.GetValue("turned_on"), out bVal);
				bOn	=(bVal != 0);

				Mathery.TryParse(ze.GetValue("cell_shade"), out bVal);
				bCell	=(bVal != 0);

				Mathery.WrapAngleDegrees(ref gravYaw);
				Mathery.WrapAngleDegrees(ref gravPitch);
				Mathery.WrapAngleDegrees(ref gravRoll);

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

				mPB.CreateEmitter(
					texPrefix + ze.GetValue("tex_name"),
					color, bCell, shape, shapeSize, maxParticles,
					pos, (int)gravYaw, (int)gravPitch, (int)gravRoll, gravStr,
					startSize, startAlpha, emitMS, rotVelMin, rotVelMax,
					velMin, velMax, sizeVelMin, sizeVelMax,
					alphaVelMin, alphaVelMax, (int)lifeMin, (int)lifeMax);

			}
		}
	}
}
