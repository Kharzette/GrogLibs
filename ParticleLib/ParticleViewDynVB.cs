using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using MaterialLib;
using SharpDX;
using SharpDX.Direct3D11;
using UtilityLib;

using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace ParticleLib
{
	internal struct ParticleVert
	{
		internal Vector4	mPosition;
		internal Half4		mSizeRotAlpha;
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
				maxParticles * 6 * 24,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<ParticleVert>(gd, mPartBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 24, 0);
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

				mPartBuf[i * 6].mPosition		=pos;
				mPartBuf[i * 6 + 1].mPosition	=pos;
				mPartBuf[i * 6 + 2].mPosition	=pos;
				mPartBuf[i * 6 + 3].mPosition	=pos;
				mPartBuf[i * 6 + 4].mPosition	=pos;
				mPartBuf[i * 6 + 5].mPosition	=pos;

				//texcoordx
				mPartBuf[i * 6 + 2].mPosition.W	=1;
				mPartBuf[i * 6 + 4].mPosition.W	=1;
				mPartBuf[i * 6 + 5].mPosition.W	=1;
//				mPartBuf[i * 6 + 1].mPosition.W	=1;
//				mPartBuf[i * 6 + 2].mPosition.W	=1;
//				mPartBuf[i * 6 + 4].mPosition.W	=1;

				//texcoordy
				mPartBuf[i * 6 + 1].mSizeRotAlpha.W	=1;
				mPartBuf[i * 6 + 2].mSizeRotAlpha.W	=1;
				mPartBuf[i * 6 + 4].mSizeRotAlpha.W	=1;
//				mPartBuf[i * 6 + 2].mSizeRotAlpha.W	=1;
//				mPartBuf[i * 6 + 4].mSizeRotAlpha.W	=1;
//				mPartBuf[i * 6 + 5].mSizeRotAlpha.W	=1;

				mPartBuf[i * 6].mSizeRotAlpha.X		=parts[i].mSize;
				mPartBuf[i * 6 + 1].mSizeRotAlpha.X	=parts[i].mSize;
				mPartBuf[i * 6 + 2].mSizeRotAlpha.X	=parts[i].mSize;
				mPartBuf[i * 6 + 3].mSizeRotAlpha.X	=parts[i].mSize;
				mPartBuf[i * 6 + 4].mSizeRotAlpha.X	=parts[i].mSize;
				mPartBuf[i * 6 + 5].mSizeRotAlpha.X	=parts[i].mSize;

				mPartBuf[i * 6].mSizeRotAlpha.Y		=parts[i].mRotation;
				mPartBuf[i * 6 + 1].mSizeRotAlpha.Y	=parts[i].mRotation;
				mPartBuf[i * 6 + 2].mSizeRotAlpha.Y	=parts[i].mRotation;
				mPartBuf[i * 6 + 3].mSizeRotAlpha.Y	=parts[i].mRotation;
				mPartBuf[i * 6 + 4].mSizeRotAlpha.Y	=parts[i].mRotation;
				mPartBuf[i * 6 + 5].mSizeRotAlpha.Y	=parts[i].mRotation;

				mPartBuf[i * 6].mSizeRotAlpha.Z		=parts[i].mAlpha;
				mPartBuf[i * 6 + 1].mSizeRotAlpha.Z	=parts[i].mAlpha;
				mPartBuf[i * 6 + 2].mSizeRotAlpha.Z	=parts[i].mAlpha;
				mPartBuf[i * 6 + 3].mSizeRotAlpha.Z	=parts[i].mAlpha;
				mPartBuf[i * 6 + 4].mSizeRotAlpha.Z	=parts[i].mAlpha;
				mPartBuf[i * 6 + 5].mSizeRotAlpha.Z	=parts[i].mAlpha;
			}

			//really annoying that I have to do this 3 times
//			mGD.SetVertexBuffer(null);
			DataStream	ds;
			dc.MapSubresource(mVB, MapMode.WriteDiscard, MapFlags.None, out ds);

			for(int i=0;i < (count * 6);i++)
			{
				ds.Write<ParticleVert>(mPartBuf[i]);
			}

			dc.UnmapSubresource(mVB, 0);
		}


		internal void Draw(MatLib mlib, AlphaPool ap,
			Vector3 pos, Vector4 color,
			Matrix view, Matrix proj)
		{
			ap.StoreParticleDraw(mlib, pos, mVBB, mNumParticles * 6, color, mTexName, view, proj);
		}


		internal void Draw(DeviceContext dc, Vector4 color,
			Matrix view, Matrix proj)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter("Particle", "mSolidColour", color);
			mMatLib.SetMaterialParameter("Particle", "mView", view);
			mMatLib.SetMaterialParameter("Particle", "mProjection", proj);
			mMatLib.SetMaterialTexture("Particle", "mTexture", mTexName);

			mMatLib.ApplyMaterialPass("Particle", dc, 0);

			dc.Draw(mNumParticles * 6, 0);
		}


		//write into the depth/normal/material buffer
		internal void DrawDMN(DeviceContext dc, Vector4 color,
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
