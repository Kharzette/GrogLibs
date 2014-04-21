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
		internal Vector4	mSizeRotAlpha;
	}


	internal class ParticleViewDynVB
	{
		Device				mGD;
		Buffer				mVB;
		VertexBufferBinding	mVBB;

		ParticleVert		[]mPartBuf;

		MatLib	mMatLib;
		string	mMatName;

		int		mNumParticles;
		int		mMaxParticles;


		internal ParticleViewDynVB(Device gd, MatLib mats, string matName, int maxParticles)
		{
			mGD				=gd;
			mMatLib			=mats;
			mMaxParticles	=maxParticles;
			mMatName		=matName;

			mPartBuf	=new ParticleVert[maxParticles * 6];

			BufferDescription	bDesc	=new BufferDescription(
				maxParticles * 6 * 32,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<ParticleVert>(gd, mPartBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 32, 0);
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
				mPartBuf[i * 6 + 1].mPosition.W	=1;
				mPartBuf[i * 6 + 2].mPosition.W	=1;
				mPartBuf[i * 6 + 4].mPosition.W	=1;

				//texcoordy
				mPartBuf[i * 6 + 2].mSizeRotAlpha.W	=1;
				mPartBuf[i * 6 + 4].mSizeRotAlpha.W	=1;
				mPartBuf[i * 6 + 5].mSizeRotAlpha.W	=1;

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


//		internal void Draw(AlphaPool ap, Vector3 pos, Vector4 color, Matrix view, Matrix proj)
//		{
//			ap.StoreParticleDraw(pos, mVB, mNumParticles * 2, mbCel, color, mFX, mTex, view, proj);
//		}


		internal void Draw(DeviceContext dc, Vector4 color, Matrix view, Matrix proj)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter(mMatName, "mSolidColour", color);
			mMatLib.SetMaterialParameter(mMatName, "mView", view);
			mMatLib.SetMaterialParameter(mMatName, "mProjection", proj);

			mMatLib.ApplyMaterialPass(mMatName, dc, 0);

			dc.Draw(mNumParticles * 6, 0);
		}


		//write into the depth/normal/material buffer
		/*
		internal void DrawDMN(Vector4 color, Matrix view, Matrix proj, Vector3 eyePos)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			mGD.SetVertexBuffer(mVB);

			mFX.CurrentTechnique	=mFX.Techniques["ParticleDMN"];

			mFX.Parameters["mTexture"].SetValue(mTex);
			mFX.Parameters["mView"].SetValue(view);
			mFX.Parameters["mProjection"].SetValue(proj);
			mFX.Parameters["mEyePos"].SetValue(eyePos);

			mFX.CurrentTechnique.Passes[0].Apply();

			mGD.DrawPrimitives(PrimitiveType.TriangleList, 0, mNumParticles * 2);

			mGD.SetVertexBuffer(null);
		}*/


		//adds on to the end
		internal string GetEntityFields(string ent)
		{
			ParticleBoss.AddField(ref ent, "tex_name", mMatName);

			return	ent;
		}


		internal void SetMaterial(string mat)
		{
			mMatName	=mat;
		}


		internal string GetMaterial()
		{
			return	mMatName;
		}
	}
}
