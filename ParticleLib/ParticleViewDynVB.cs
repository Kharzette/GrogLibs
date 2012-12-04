using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ParticleLib
{
	internal struct ParticleVert
	{
		internal Vector4	mPosition;
		internal Vector4	mSizeRotAlpha;
	}


	internal class ParticleViewDynVB
	{
		GraphicsDevice		mGD;
		Effect				mFX;
		Texture2D			mTex;
		VertexDeclaration	mVD;
		DynamicVertexBuffer	mVB;
		ParticleVert		[]mPartBuf;

		int		mNumParticles;
		int		mMaxParticles;
		bool	mbCell;


		internal ParticleViewDynVB(GraphicsDevice gd, Effect fx, Texture2D tex, int maxParticles)
		{
			mGD		=gd;
			mFX		=fx;
			mTex	=tex;

			mMaxParticles	=maxParticles;

			VertexElement	[]els	=new VertexElement[2];

			els[0]	=new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0);
			els[1]	=new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0);

			mVD			=new VertexDeclaration(els);
			mVB			=new DynamicVertexBuffer(gd, mVD, maxParticles * 6, BufferUsage.WriteOnly);
			mPartBuf	=new ParticleVert[maxParticles * 6];
		}


		internal void Update(Particle []parts, int count)
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

			mVB.SetData<ParticleVert>(mPartBuf, 0, count * 6);
		}


		internal void Draw(Vector4 color, Matrix view, Matrix proj)
		{
			if(mNumParticles <= 0)
			{
				return;
			}

			mGD.SetVertexBuffer(mVB);

			mGD.DepthStencilState	=DepthStencilState.DepthRead;
			mGD.BlendState			=BlendState.Additive;
			mGD.RasterizerState		=RasterizerState.CullCounterClockwise;

			if(mbCell)
			{
				mFX.CurrentTechnique	=mFX.Techniques["ParticleCell"];
			}
			else
			{
				mFX.CurrentTechnique	=mFX.Techniques["Particle"];
			}

			mFX.Parameters["mSolidColour"].SetValue(color);
			mFX.Parameters["mTexture"].SetValue(mTex);
			mFX.Parameters["mView"].SetValue(view);
			mFX.Parameters["mProjection"].SetValue(proj);

			mFX.CurrentTechnique.Passes[0].Apply();

			mGD.DrawPrimitives(PrimitiveType.TriangleList, 0, mNumParticles * 2);

			mGD.SetVertexBuffer(null);
		}


		internal bool GetCell()
		{
			return	mbCell;
		}


		internal void SetCell(bool bOn)
		{
			mbCell	=bOn;
		}


		internal void SetTexture(Texture2D tex)
		{
			mTex	=tex;
		}


		internal string GetTexturePath()
		{
			return	mTex.Name;
		}
	}
}
