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

			//create particle materials
			CreateParticleMats();
		}


		public void FreeAll()
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.FreeAll();
			}
		}


		//if tied into a matlib that does io or clears,
		//this will recreate the needed materials
		public void CreateParticleMats()
		{
			mMats.CreateMaterial("Particle");
			mMats.SetMaterialEffect("Particle", "2D.fx");
			mMats.SetMaterialTechnique("Particle", "Particle");

			mMats.CreateMaterial("ParticleDMN");
			mMats.SetMaterialEffect("ParticleDMN", "2D.fx");
			mMats.SetMaterialTechnique("ParticleDMN", "ParticleDMN");
		}


		public int CreateEmitter(string texName, Vector4 color,
			Emitter.Shapes shape, float shapeSize,
			int maxParticles, Vector3 pos,
			int gravYaw, int gravPitch, float gravStr,
			float startSize, float startAlpha, float emitMS,
			float rotVelMin, float rotVelMax, float velMin,
			float velMax, float sizeVelMin, float sizeVelMax,
			float alphaVelMin, float alphaVelMax,
			int lifeMin, int lifeMax, int sortPri)
		{
			Emitter	newEmitter	=new Emitter(
				maxParticles, shape, shapeSize, pos,
				gravYaw, gravPitch, gravStr,
				startSize, startAlpha, emitMS,
				rotVelMin, rotVelMax, velMin, velMax,
				sizeVelMin, sizeVelMax, alphaVelMin, alphaVelMax,
				lifeMin, lifeMax, sortPri);

			newEmitter.Activate(true);
			
			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mMats, texName, maxParticles);

			EmitterData	ed	=new EmitterData();
			ed.mColor		=color;
			ed.mEmitter		=newEmitter;
			ed.mView		=pvd;

			mEmitters.Add(mNextIndex++, ed);

			return	mNextIndex - 1;
		}


		//returns true if emitter count changed
		public void Update(DeviceContext dc, float msDelta)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				int	numParticles	=0;
				Particle	[]parts	=em.Value.mEmitter.Update(msDelta, out numParticles);

				em.Value.mView.Update(dc, parts, numParticles);
			}
		}


		public void DrawDMN(DeviceContext dc, Matrix view, Matrix proj, Vector3 eyePos)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.DrawDMN(dc, em.Value.mColor, view, proj, eyePos);
			}
		}


		public void Draw(DeviceContext dc, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(dc, em.Value.mColor, view, proj);
			}
		}


		public void Draw(MaterialLib.AlphaPool ap, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(mMats, ap, em.Value.mEmitter.mPosition, em.Value.mColor, view, proj);
			}
		}


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


		public void SetTextureByIndex(int index, string tex)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return;
			}
			mEmitters[index].mView.SetTexture(tex);
		}


		public string GetTextureByIndex(int index)
		{
			if(!mEmitters.ContainsKey(index))
			{
				return	"";
			}
			return	mEmitters[index].mView.GetTexture();
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


		string GrabValue(string line)
		{
			int	quoteIndex	=line.IndexOf('\"');
			if(quoteIndex < 0)
			{
				return	"";
			}

			string	fromQuote	=line.Substring(quoteIndex + 1);

			quoteIndex	=fromQuote.IndexOf('\"');
			if(quoteIndex < 0)
			{
				return	"";
			}

			return	fromQuote.Substring(0, quoteIndex);
		}


		public int CreateEmitterFromQuArK(string entityText)
		{
			if(!entityText.StartsWith("QQRKSRC1"))
			{
				//doesn't look like this came from quarkland
				return	-1;
			}

			int		maxPart, shape, shapeSize, gravYaw, gravPitch, sortPri;
			float	gravStr, startSize, startAlpha;
			float	velMin, velMax, sizeMin, sizeMax, spinMin, spinMax;
			float	alphaMin, alphaMax, lifeMin, lifeMax, emitMS;
			string	texName	="";
			Vector4	color	=Vector4.Zero;

			//initialize, annoying
			maxPart		=1000;		shape		=(int)Emitter.Shapes.Point;
			shapeSize	=10;		gravYaw		=-90;
			gravStr		=0.001f;	startSize	=4;
			startAlpha	=1f;		spinMin		=0;
			spinMax		=0;			velMin		=-0.1f;
			velMax		=.1f;		sizeMin		=-0.1f;
			sizeMax		=.1f;		alphaMin	=-0.1f;
			alphaMax	=.1f;		lifeMin		=4000;
			lifeMax		=8000;		emitMS		=0.04f;
			gravPitch	=0;			sortPri		=0;

			string	[]lines	=entityText.Split('\n');
			foreach(string line in lines)
			{
				string	trimmed	=line.TrimStart();

				if(trimmed.StartsWith("max_particles"))
				{
					Mathery.TryParse(GrabValue(trimmed), out maxPart);
				}
				else if(trimmed.StartsWith("shape_size"))
				{
					Mathery.TryParse(GrabValue(trimmed), out shapeSize);
				}
				else if(trimmed.StartsWith("shape"))
				{
					Mathery.TryParse(GrabValue(trimmed), out shape);
				}
				else if(trimmed.StartsWith("grav_yaw"))
				{
					Mathery.TryParse(GrabValue(trimmed), out gravYaw);
				}
				else if(trimmed.StartsWith("grav_pitch"))
				{
					Mathery.TryParse(GrabValue(trimmed), out gravPitch);
				}
				else if(trimmed.StartsWith("grav_strength"))
				{
					Mathery.TryParse(GrabValue(trimmed), out gravStr);
				}
				else if(trimmed.StartsWith("start_size"))
				{
					Mathery.TryParse(GrabValue(trimmed), out startSize);
				}
				else if(trimmed.StartsWith("start_alpha"))
				{
					Mathery.TryParse(GrabValue(trimmed), out startAlpha);
				}
				else if(trimmed.StartsWith("emit_ms"))
				{
					Mathery.TryParse(GrabValue(trimmed), out emitMS);
				}
				else if(trimmed.StartsWith("velocity_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out velMin);
				}
				else if(trimmed.StartsWith("velocity_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out velMax);
				}
				else if(trimmed.StartsWith("size_velocity_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out sizeMin);
				}
				else if(trimmed.StartsWith("size_velocity_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out sizeMax);
				}
				else if(trimmed.StartsWith("spin_velocity_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out spinMin);
				}
				else if(trimmed.StartsWith("spin_velocity_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out spinMax);
				}
				else if(trimmed.StartsWith("alpha_velocity_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out alphaMin);
				}
				else if(trimmed.StartsWith("alpha_velocity_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out alphaMax);
				}
				else if(trimmed.StartsWith("lifetime_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out lifeMin);
				}
				else if(trimmed.StartsWith("lifetime_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out lifeMax);
				}
				else if(trimmed.StartsWith("sort_priority"))
				{
					Mathery.TryParse(GrabValue(trimmed), out sortPri);
				}
				else if(trimmed.StartsWith("tex_name"))
				{
					texName	=GrabValue(trimmed);
				}
				else if(trimmed.StartsWith("color"))
				{
					Vector3	col;
					col	=Misc.StringToVector3(GrabValue(trimmed));
					color.X	=col.X;
					color.Y	=col.Y;
					color.Z	=col.Z;
				}
				else if(trimmed.StartsWith("alpha"))
				{
					Mathery.TryParse(GrabValue(trimmed), out color.W);
				}
			}

			//scale some to millisecond values
			spinMin		/=1000f;
			spinMax		/=1000f;
			velMin		/=1000f;
			velMax		/=1000f;
			sizeMin		/=1000f;
			sizeMax		/=1000f;
			alphaMin	/=1000f;
			alphaMax	/=1000f;
			lifeMin		*=1000;
			lifeMax		*=1000;

			return	CreateEmitter(texName, color, (Emitter.Shapes)shape, shapeSize,
				maxPart, Vector3.Zero, gravYaw, gravPitch, gravStr,
				startSize, startAlpha, emitMS, spinMin, spinMax, velMin,
				velMax, sizeMin, sizeMax, alphaMin, alphaMax,
				(int)lifeMin, (int)lifeMax, sortPri);
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
