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


		public ParticleBoss(GraphicsDevice gd, Effect fx, Dictionary<string, Texture2D> texs)
		{
			mGD			=gd;
			mFX			=fx;
			mTextures	=texs;
		}


		public void CreateEmitter(string texName, int maxParticles,
			Vector3 pos, float startSize,
			int durationMS, int emitPerSecond,
			int rotVelMin, int rotVelMax, int velMin,
			int velMax, int sizeVelMin, int sizeVelMax,
			int alphaVelMin, int alphaVelMax,
			int lifeMin, int lifeMax)
		{
			if(!mTextures.ContainsKey(texName))
			{
				return;
			}

			Emitter	newEmitter	=new Emitter(maxParticles, pos, startSize, durationMS,
				emitPerSecond, rotVelMin, rotVelMax, velMin, velMax,
				sizeVelMin, sizeVelMax, alphaVelMin, alphaVelMax, lifeMin, lifeMax);

			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mFX, mTextures[texName], maxParticles);

			mEmitters.Add(newEmitter);
			mViews.Add(pvd);
		}


		public void Update(int msDelta)
		{
			Debug.Assert(mEmitters.Count == mViews.Count);

			for(int i=0;i < mEmitters.Count;i++)
			{
				Particle	[]parts	=mEmitters[i].Update(msDelta);

				if(parts != null)
				{
					if(parts.Length > 0)
					{
						mViews[i].Update(parts, parts.Length);
					}
				}
			}
		}


		public void Draw(Matrix view, Matrix proj)
		{
			foreach(ParticleViewDynVB pvd in mViews)
			{
				pvd.Draw(Vector4.One, view, proj);
			}
		}
	}
}
