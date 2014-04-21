using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using UtilityLib;

using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace ParticleLib
{
	public class ParticleBoss
	{
		//graphics stuff
		Device	mGD;
		MatLib	mMats;

		//indexes
		int	mNextIndex;

		class EmitterData
		{
			internal Emitter			mEmitter;
			internal ParticleViewDynVB	mView;
			internal Vector4			mColor;
		}

		//data
		Dictionary<int, EmitterData>	mEmitters	=new Dictionary<int, EmitterData>();


		public ParticleBoss(Device gd, MatLib mats)
		{
			mGD		=gd;
			mMats	=mats;
		}


		public int CreateEmitter(string matName, Vector4 color,
			Emitter.Shapes shape, float shapeSize,
			int maxParticles, Vector3 pos,
			int gravYaw, int gravPitch, float gravStr,
			float startSize, float startAlpha, float emitMS,
			float rotVelMin, float rotVelMax, float velMin,
			float velMax, float sizeVelMin, float sizeVelMax,
			float alphaVelMin, float alphaVelMax,
			int lifeMin, int lifeMax)
		{
			Emitter	newEmitter	=new Emitter(
				maxParticles, shape, shapeSize, pos,
				gravYaw, gravPitch, gravStr,
				startSize, startAlpha, emitMS,
				rotVelMin, rotVelMax, velMin, velMax,
				sizeVelMin, sizeVelMax, alphaVelMin, alphaVelMax,
				lifeMin, lifeMax);

			newEmitter.Activate(true);
			
			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mMats, matName, maxParticles);

			EmitterData	ed	=new EmitterData();
			ed.mColor		=color;
			ed.mEmitter		=newEmitter;
			ed.mView		=pvd;

			mEmitters.Add(mNextIndex++, ed);

			return	mNextIndex - 1;
		}


		//returns true if emitter count changed
		public void Update(DeviceContext dc, int msDelta)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				int	numParticles	=0;
				Particle	[]parts	=em.Value.mEmitter.Update(msDelta, out numParticles);

				em.Value.mView.Update(dc, parts, numParticles);
			}
		}


		public void DrawDMN(Matrix view, Matrix proj, Vector3 eyePos)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
//				em.Value.mView.DrawDMN(em.Value.mColor, view, proj, eyePos);
			}
		}


		public void Draw(DeviceContext dc, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(dc, em.Value.mColor, view, proj);
			}
		}


/*		public void Draw(MaterialLib.AlphaPool ap, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(ap, em.Value.mEmitter.mPosition, em.Value.mColor, view, proj);
			}
		}*/


		public int GetEmitterCount()
		{
			return	mEmitters.Count;
		}


		public Emitter GetEmitterByIndex(int index)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return	null;
			}
			return	mEmitters[index].mEmitter;
		}


		public Vector4 GetColorByIndex(int index)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return	Vector4.One;
			}
			return	mEmitters[index].mColor;
		}


		public void SetColorByIndex(int index, Vector4 col)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return;
			}
			mEmitters[index].mColor	=col;
		}


		public void SetMaterialByIndex(int index, string mat)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return;
			}
			mEmitters[index].mView.SetMaterial(mat);
		}


		public string GetMaterialByIndex(int index)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return	"";
			}
			return	mEmitters[index].mView.GetMaterial();
		}


		public void NukeEmitter(int idx)
		{
			if(!mEmitters.ContainsKey(idx))
			{
				return;
			}
			mEmitters.Remove(idx);
		}


		public void NukeAll()
		{
			mEmitters.Clear();
		}


		internal static void AddField(ref string ent, string fieldName, string value)
		{
			ent	+="    " + fieldName + " = \"" + value + "\"\n";
		}

		
		//convert to a quark entity
		public string GetEmitterEntityString(int index)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return	"";
			}

			EmitterData	ed	=mEmitters[index];

			string	entity	="QQRKSRC1\n{\n  misc_particle_emitter:e =\n  {\n";

			entity	=ed.mEmitter.GetEntityFields(entity);
			entity	=ed.mView.GetEntityFields(entity);

			Vector4	col	=ed.mColor;

			AddField(ref entity, "color", Misc.FloatToString(col.X, 4)
				+ " " + Misc.FloatToString(col.Y, 4)
				+ " " + Misc.FloatToString(col.Z, 4));

			//color's w component goes in alpha
			//quark doesn't like 4 component stuff
			AddField(ref entity, "alpha", "" + Misc.FloatToString(col.W, 4));

			entity	+="  }\n}";

			return	entity;
		}
	}
}
