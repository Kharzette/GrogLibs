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
			Vector3 pos, Vector3 gravity,
			float startSize, float startAlpha,
			int durationMS, float emitMS,
			float rotVelMin, float rotVelMax, float velMin,
			float velMax, float sizeVelMin, float sizeVelMax,
			float alphaVelMin, float alphaVelMax,
			int lifeMin, int lifeMax)
		{
			if(!mTextures.ContainsKey(texName))
			{
				return;
			}

			Emitter	newEmitter	=new Emitter(maxParticles,
				pos, gravity,
				startSize, startAlpha,	durationMS,
				emitMS, rotVelMin, rotVelMax, velMin, velMax,
				sizeVelMin, sizeVelMax, alphaVelMin, alphaVelMax, lifeMin, lifeMax);

			newEmitter.Activate(true);

			ParticleViewDynVB	pvd	=new ParticleViewDynVB(mGD, mFX, mTextures[texName], maxParticles);

			mEmitters.Add(newEmitter);
			mViews.Add(pvd);
		}


		public void Update(int msDelta)
		{
			Debug.Assert(mEmitters.Count == mViews.Count);

			List<Emitter>	nuke	=new List<Emitter>();

			for(int i=0;i < mEmitters.Count;i++)
			{
				int	numParticles	=0;
				Particle	[]parts	=mEmitters[i].Update(msDelta, out numParticles);

				if(parts == null)
				{
					nuke.Add(mEmitters[i]);
					continue;
				}

				mViews[i].Update(parts, numParticles);
			}

			foreach(Emitter em in nuke)
			{
				int	idx	=mEmitters.IndexOf(em);

				mEmitters.RemoveAt(idx);
				mViews.RemoveAt(idx);
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
