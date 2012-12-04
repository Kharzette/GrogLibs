using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace ParticleLib
{
	public class ParticleBoss
	{
		//graphics stuff
		GraphicsDevice	mGD;
		Effect			mFX;

		//texture library
		Dictionary<string, Texture2D>	mTextures;

		//data
		List<Emitter>			mEmitters	=new List<Emitter>();
		List<ParticleViewDynVB>	mViews		=new List<ParticleViewDynVB>();
		List<Vector4>			mColors		=new List<Vector4>();


		public ParticleBoss(GraphicsDevice gd, Effect fx, Dictionary<string, Texture2D> texs)
		{
			mGD			=gd;
			mFX			=fx;
			mTextures	=texs;
		}


		public void CreateEmitter(string texName, Vector4 color, bool bCell,
			Emitter.Shapes shape, float shapeSize,
			int maxParticles, Vector3 pos,
			int gravYaw, int gravPitch, int gravRoll, float gravStr,
			float startSize, float startAlpha, float emitMS,
			float rotVelMin, float rotVelMax, float velMin,
			float velMax, float sizeVelMin, float sizeVelMax,
			float alphaVelMin, float alphaVelMax,
			int lifeMin, int lifeMax)
		{
			if(!mTextures.ContainsKey(texName))
			{
				return;
			}

			Emitter	newEmitter	=new Emitter(
				maxParticles, shape, shapeSize, pos,
				gravYaw, gravPitch, gravRoll, gravStr,
				startSize, startAlpha, emitMS,
				rotVelMin, rotVelMax, velMin, velMax,
				sizeVelMin, sizeVelMax, alphaVelMin, alphaVelMax,
				lifeMin, lifeMax);

			newEmitter.Activate(true);
			
			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mFX, mTextures[texName], maxParticles);

			mEmitters.Add(newEmitter);
			mViews.Add(pvd);
			mColors.Add(color);

			pvd.SetCell(bCell);
		}


		//returns true if emitter count changed
		public void Update(int msDelta)
		{
			Debug.Assert(mEmitters.Count == mViews.Count);

			List<Emitter>	nuke	=new List<Emitter>();

			for(int i=0;i < mEmitters.Count;i++)
			{
				int	numParticles	=0;
				Particle	[]parts	=mEmitters[i].Update(msDelta, out numParticles);

				mViews[i].Update(parts, numParticles);
			}
		}


		public void Draw(Matrix view, Matrix proj)
		{
			for(int i=0;i < mViews.Count;i++)
			{
				mViews[i].Draw(mColors[i], view, proj);
			}
		}


		public int GetEmitterCount()
		{
			return	mEmitters.Count;
		}


		public Emitter GetEmitterByIndex(int index)
		{
			if(mEmitters.Count <= index || index < 0)
			{
				return	null;
			}
			return	mEmitters[index];
		}


		public Vector4 GetColorByIndex(int index)
		{
			if(mColors.Count <= index)
			{
				return	Vector4.One;
			}
			return	mColors[index];
		}


		public void SetColorByIndex(int index, Vector4 col)
		{
			if(mColors.Count <= index)
			{
				return;
			}
			mColors[index]	=col;
		}


		public void SetTextureByIndex(int index, Texture2D tex)
		{
			if(mViews.Count <= index)
			{
				return;
			}
			mViews[index].SetTexture(tex);
		}


		public void SetCellByIndex(int index, bool bOn)
		{
			if(mViews.Count <= index)
			{
				return;
			}
			mViews[index].SetCell(bOn);
		}


		public bool GetCellByIndex(int index)
		{
			if(mViews.Count <= index)
			{
				return	false;
			}
			return	mViews[index].GetCell();
		}


		public string GetTexturePathByIndex(int index)
		{
			if(mViews.Count <= index)
			{
				return	"";
			}
			return	mViews[index].GetTexturePath();
		}


		public void NukeEmitter(int idx)
		{
			mEmitters.RemoveAt(idx);
			mViews.RemoveAt(idx);
			mColors.RemoveAt(idx);
		}
	}
}
