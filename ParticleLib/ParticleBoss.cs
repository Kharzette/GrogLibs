using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using UtilityLib;

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


		public int CreateEmitter(string texName, Vector4 startColor,
			Emitter.Shapes shape, float shapeSize,
			int maxParticles, Vector3 pos,
			Vector3 gravPos, float gravStr,
			float startSize, float emitMS,
			float rotVelMin, float rotVelMax,
			float velMin, float velMax, float velCap,
			float sizeVelMin, float sizeVelMax,
			Vector4 colorVelMin, Vector4 colorVelMax,
			int lifeMin, int lifeMax)
		{
			Emitter	newEmitter	=new Emitter(
				maxParticles, shape, shapeSize, pos,
				startColor,	gravPos, gravStr,
				startSize, emitMS,
				rotVelMin, rotVelMax,
				velMin, velMax, velCap,
				sizeVelMin, sizeVelMax,
				colorVelMin, colorVelMax,
				lifeMin, lifeMax);

			newEmitter.Activate(true);
			
			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mMats, texName, maxParticles);

			EmitterData	ed	=new EmitterData();
			ed.mEmitter		=newEmitter;
			ed.mView		=pvd;

			mEmitters.Add(mNextIndex++, ed);

			return	mNextIndex - 1;
		}


		//returns true if emitter count changed
		public void Update(DeviceContext dc, int msDelta)
		{
			Debug.Assert(msDelta > 0);

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
				em.Value.mView.DrawDMN(dc, view, proj, eyePos);
			}
		}


		public void Draw(DeviceContext dc, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(dc, view, proj);
			}
		}


		public void Draw(MaterialLib.AlphaPool ap, Matrix view, Matrix proj)
		{
			foreach(KeyValuePair<int, EmitterData> em in mEmitters)
			{
				em.Value.mView.Draw(mMats, ap, em.Value.mEmitter.mPosition, view, proj);
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

			int		maxPart, shape, shapeSize;
			float	gravStr, startSize, startAlpha, velCap;
			float	velMin, velMax, sizeMin, sizeMax, spinMin, spinMax;
			float	alphaMin, alphaMax, lifeMin, lifeMax, emitMS;
			string	texName	="";
			Vector4	colorVelMin	=Vector4.Zero;
			Vector4	colorVelMax	=Vector4.Zero;
			Vector4	startColor	=Vector4.Zero;
			Vector3	gravPos		=Vector3.Zero;

			//initialize, annoying
			maxPart		=1000;		shape		=(int)Emitter.Shapes.Point;
			shapeSize	=10;
			gravStr		=0.001f;	startSize	=4;
			startAlpha	=1f;		spinMin		=0;
			spinMax		=0;			velMin		=-0.1f;
			velMax		=.1f;		sizeMin		=-0.1f;
			sizeMax		=.1f;		alphaMin	=-0.1f;
			alphaMax	=.1f;		lifeMin		=4000;
			lifeMax		=8000;		emitMS		=0.04f;
			velCap		=0.04f;

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
				else if(trimmed.StartsWith("grav_loc"))
				{
					gravPos	=Misc.StringToVector3(GrabValue(trimmed));
				}
				else if(trimmed.StartsWith("grav_strength"))
				{
					Mathery.TryParse(GrabValue(trimmed), out gravStr);
				}
				else if(trimmed.StartsWith("start_size"))
				{
					Mathery.TryParse(GrabValue(trimmed), out startSize);
				}
				else if(trimmed.StartsWith("start_color"))
				{
					Vector3	col	=Misc.StringToVector3(GrabValue(trimmed));
					startColor	=new Vector4(col, 1f);
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
				else if(trimmed.StartsWith("velocity_cap"))
				{
					Mathery.TryParse(GrabValue(trimmed), out velCap);
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
					Mathery.TryParse(GrabValue(trimmed), out colorVelMin.W);
				}
				else if(trimmed.StartsWith("alpha_velocity_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out colorVelMax.W);
				}
				else if(trimmed.StartsWith("lifetime_min"))
				{
					Mathery.TryParse(GrabValue(trimmed), out lifeMin);
				}
				else if(trimmed.StartsWith("lifetime_max"))
				{
					Mathery.TryParse(GrabValue(trimmed), out lifeMax);
				}
				else if(trimmed.StartsWith("tex_name"))
				{
					texName	=GrabValue(trimmed);
				}
				else if(trimmed.StartsWith("color_velocity_min"))
				{
					Vector3	col	=Misc.StringToVector3(GrabValue(trimmed));
					colorVelMin	=new Vector4(col, colorVelMin.W);
				}
				else if(trimmed.StartsWith("color_velocity_max"))
				{
					Vector3	col	=Misc.StringToVector3(GrabValue(trimmed));
					colorVelMax	=new Vector4(col, colorVelMax.W);
				}
			}

			startColor.W	=startAlpha;

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
			colorVelMin	/=10000f;
			colorVelMax	/=10000f;

			return	CreateEmitter(texName, startColor,
				(Emitter.Shapes)shape, shapeSize,
				maxPart, Vector3.Zero, gravPos, gravStr,
				startSize, emitMS, spinMin, spinMax,
				velMin, velMax, velCap,
				sizeMin, sizeMax,
				colorVelMin, colorVelMax,
				(int)lifeMin, (int)lifeMax);
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

			entity	+="  }\n}";

			return	entity;
		}
	}
}
