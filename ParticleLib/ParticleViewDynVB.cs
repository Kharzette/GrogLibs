using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using MaterialLib;
using UtilityLib;


namespace ParticleLib
{
	internal struct ParticleVert
	{
		internal Vector4	mPositionTex;
		internal Half4		mTexSizeRotBlank;
		internal Half4		mColor;
	}


	internal class ParticleViewDynVB
	{
		Device				mGD;
		Buffer				mVB;
		VertexBufferBinding	mVBB;

		ParticleVert		[]mPartBuf;

		MatLib	mMatLib;
		string	mTexName;

		int		mNumParticles;
		int		mMaxParticles;


		internal ParticleViewDynVB(Device gd, MatLib mats, string texName, int maxParticles)
		{
			mGD				=gd;
			mMatLib			=mats;
			mMaxParticles	=maxParticles;
			mTexName		=texName;

			mPartBuf	=new ParticleVert[maxParticles * 6];

			BufferDescription	bDesc	=new BufferDescription(
				maxParticles * 6 * 32,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<ParticleVert>(gd, mPartBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 32, 0);
		}


		internal void FreeAll()
		{
			mVB.Dispose();
		}


		internal void Update(DeviceContext dc, Particle []parts, int count)
		{
			mNumParticles	=count;

			if(count == 0)
			{
				return;
			}

			Debug.Assert(count <= mMaxParticles);

			for(int i=0;i < count;i++)
			{
				Vector4	pos	=Vector4.Zero;

				pos.X	=parts[i].mPosition.X;
				pos.Y	=parts[i].mPosition.Y;
				pos.Z	=parts[i].mPosition.Z;

				mPartBuf[i * 6].mPositionTex		=pos;
				mPartBuf[i * 6 + 1].mPositionTex	=pos;
				mPartBuf[i * 6 + 2].mPositionTex	=pos;
				mPartBuf[i * 6 + 3].mPositionTex	=pos;
				mPartBuf[i * 6 + 4].mPositionTex	=pos;
				mPartBuf[i * 6 + 5].mPositionTex	=pos;

				//texcoordx
				mPartBuf[i * 6 + 2].mPositionTex.W	=1;
				mPartBuf[i * 6 + 4].mPositionTex.W	=1;
				mPartBuf[i * 6 + 5].mPositionTex.W	=1;

				//texcoordy
				mPartBuf[i * 6 + 1].mTexSizeRotBlank.X	=1;
				mPartBuf[i * 6 + 2].mTexSizeRotBlank.X	=1;
				mPartBuf[i * 6 + 4].mTexSizeRotBlank.X	=1;

				mPartBuf[i * 6].mTexSizeRotBlank.Y		=parts[i].mSize;
				mPartBuf[i * 6 + 1].mTexSizeRotBlank.Y	=parts[i].mSize;
				mPartBuf[i * 6 + 2].mTexSizeRotBlank.Y	=parts[i].mSize;
				mPartBuf[i * 6 + 3].mTexSizeRotBlank.Y	=parts[i].mSize;
				mPartBuf[i * 6 + 4].mTexSizeRotBlank.Y	=parts[i].mSize;
				mPartBuf[i * 6 + 5].mTexSizeRotBlank.Y	=parts[i].mSize;

				mPartBuf[i * 6].mTexSizeRotBlank.Z		=parts[i].mRotation;
				mPartBuf[i * 6 + 1].mTexSizeRotBlank.Z	=parts[i].mRotation;
				mPartBuf[i * 6 + 2].mTexSizeRotBlank.Z	=parts[i].mRotation;
				mPartBuf[i * 6 + 3].mTexSizeRotBlank.Z	=parts[i].mRotation;
				mPartBuf[i * 6 + 4].mTexSizeRotBlank.Z	=parts[i].mRotation;
				mPartBuf[i * 6 + 5].mTexSizeRotBlank.Z	=parts[i].mRotation;

				mPartBuf[i * 6].mColor		=parts[i].mColor;
				mPartBuf[i * 6 + 1].mColor	=parts[i].mColor;
				mPartBuf[i * 6 + 2].mColor	=parts[i].mColor;
				mPartBuf[i * 6 + 3].mColor	=parts[i].mColor;
				mPartBuf[i * 6 + 4].mColor	=parts[i].mColor;
				mPartBuf[i * 6 + 5].mColor	=parts[i].mColor;
			}

			DataStream	ds;
			dc.MapSubresource(mVB, MapMode.WriteDiscard, MapFlags.None, out ds);

			for(int i=0;i < (count * 6);i++)
			{
				ds.Write<ParticleVert>(mPartBuf[i]);
			}

			dc.UnmapSubresource(mVB, 0);
		}


		internal void Draw(MatLib mlib, AlphaPool ap,
			Vector3 pos, Matrix view, Matrix proj)
		{
			ap.StoreParticleDraw(mlib, pos, mVBB, mNumParticles * 6, mTexName, view, proj);
		}


		internal void Draw(DeviceContext dc, Matrix view, Matrix proj)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter("Particle", "mView", view);
			mMatLib.SetMaterialParameter("Particle", "mProjection", proj);
			mMatLib.SetMaterialTexture("Particle", "mTexture", mTexName);

			mMatLib.ApplyMaterialPass("Particle", dc, 0);

			dc.Draw(mNumParticles * 6, 0);
		}


		//write into the depth/normal/material buffer
		internal void DrawDMN(DeviceContext dc,
			Matrix view, Matrix proj, Vector3 eyePos)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter("ParticleDMN", "mView", view);
			mMatLib.SetMaterialParameter("ParticleDMN", "mProjection", proj);
			mMatLib.SetMaterialTexture("ParticleDMN", "mTexture", mTexName);

			mMatLib.ApplyMaterialPass("ParticleDMN", dc, 0);

			dc.Draw(mNumParticles * 6, 0);
		}


		//adds on to the end
		internal string GetEntityFields(string ent)
		{
			ParticleBoss.AddField(ref ent, "tex_name", mTexName);

			return	ent;
		}


		internal void SetTexture(string tex)
		{
			mTexName	=tex;
		}


		internal string GetTexture()
		{
			return	mTexName;
		}
	}
}
