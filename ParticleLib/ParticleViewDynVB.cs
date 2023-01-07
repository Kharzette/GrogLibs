using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using MaterialLib;
using UtilityLib;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;

using MatLib	=MaterialLib.MaterialLib;

namespace ParticleLib;

internal struct ParticleVert
{
	internal Vector4	mPositionTex;
	internal Half4		mTexSizeRotBlank;
	internal Half4		mColor;
}


internal class ParticleViewDynVB
{
	GraphicsDevice		mGD;
	ID3D11Buffer		mVB;
	StuffKeeper			mSK;

	ParticleVert		[]mPartBuf;

	string	mTexName;

	int		mNumParticles;
	int		mMaxParticles;

	const int	Stride	=32;


	internal ParticleViewDynVB(GraphicsDevice gd, StuffKeeper sk, string texName, int maxParticles)
	{
		mGD				=gd;
		mSK				=sk;
		mMaxParticles	=maxParticles;
		mTexName		=texName;

		mPartBuf	=new ParticleVert[maxParticles * 6];

		BufferDescription	bDesc	=new BufferDescription(
			maxParticles * 6 * Stride, BindFlags.VertexBuffer,
			ResourceUsage.Dynamic, CpuAccessFlags.Write,
			ResourceOptionFlags.None, 0);

		mVB		=gd.GD.CreateBuffer<ParticleVert>(mPartBuf, bDesc);
	}


	internal void FreeAll()
	{
		mVB.Dispose();
	}


	internal void Update(ID3D11DeviceContext dc, Particle []parts, int count)
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
			mPartBuf[i * 6].mTexSizeRotBlank		=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);
			mPartBuf[i * 6 + 1].mTexSizeRotBlank	=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);
			mPartBuf[i * 6 + 2].mTexSizeRotBlank	=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);
			mPartBuf[i * 6 + 3].mTexSizeRotBlank	=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);
			mPartBuf[i * 6 + 4].mTexSizeRotBlank	=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);
			mPartBuf[i * 6 + 5].mTexSizeRotBlank	=new Half4(1, parts[i].mSize, parts[i].mRotation, 1);

			mPartBuf[i * 6].mColor		=parts[i].mColor;
			mPartBuf[i * 6 + 1].mColor	=parts[i].mColor;
			mPartBuf[i * 6 + 2].mColor	=parts[i].mColor;
			mPartBuf[i * 6 + 3].mColor	=parts[i].mColor;
			mPartBuf[i * 6 + 4].mColor	=parts[i].mColor;
			mPartBuf[i * 6 + 5].mColor	=parts[i].mColor;
		}

		dc.UpdateSubresource<ParticleVert>(mPartBuf, mVB);
	}


	internal void Draw(ID3D11DeviceContext dc, Matrix4x4 view, Matrix4x4 proj, Vector3 eyePos)
	{
		if(mNumParticles <= 0)
		{
			return;
		}

		dc.IASetVertexBuffer(0, mVB, Stride);

		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.SetView(view, eyePos);
		cbk.SetProjection(proj);

		dc.VSSetShader(mSK.GetVertexShader("ParticleVS"));
		dc.PSSetShader(mSK.GetPixelShader("ParticlePS"));

		dc.PSSetShaderResource(0, mSK.GetSRV(mTexName));

		cbk.UpdateFrame(dc);

		cbk.SetCommonCBToShaders(dc);

		dc.Draw(mNumParticles * 6, 0);
	}


	//write into the depth/normal/material buffer
	internal void DrawDMN(ID3D11DeviceContext dc,
		Matrix4x4 view, Matrix4x4 proj, Vector3 eyePos)
	{
		if(mNumParticles <= 0)
		{
			return;
		}

		dc.IASetVertexBuffer(0, mVB, Stride);

		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.SetView(view, eyePos);
		cbk.SetProjection(proj);

		dc.VSSetShader(mSK.GetVertexShader("ParticleDMNVS"));
		dc.PSSetShader(mSK.GetPixelShader("ParticleDMNPS"));

		dc.PSSetShaderResource(0, mSK.GetSRV(mTexName));

		cbk.UpdateFrame(dc);

		cbk.SetCommonCBToShaders(dc);

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