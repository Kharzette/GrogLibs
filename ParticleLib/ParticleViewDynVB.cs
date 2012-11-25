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

		int	mNumParticles;
		int	mMaxParticles;


		internal ParticleViewDynVB(GraphicsDevice gd, Effect fx, Texture2D tex, int maxParticles)
		{
			mGD		=gd;
			mFX		=fx;
			mTex	=tex;

			mMaxParticles	=maxParticles;

			VertexElement	[]els	=new VertexElement[2];

			els[0]	=new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0);
			els[1]	=new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0);

			mVD	=new VertexDeclaration(els);

			mVB	=new DynamicVertexBuffer(gd, mVD, maxParticles * 6, BufferUsage.WriteOnly);
		}


		internal void Update(Particle []parts, int count)
		{
			Debug.Assert(count < mMaxParticles);

			ParticleVert	[]pverts	=new ParticleVert[count * 6];

			for(int i=0;i < count;i++)
			{
				Vector4	pos	=Vector4.Zero;

				pos.X	=parts[i].mPosition.X;
				pos.Y	=parts[i].mPosition.Y;
				pos.Z	=parts[i].mPosition.Z;

				pverts[i * 6].mPosition		=pos;
				pverts[i * 6 + 1].mPosition	=pos;
				pverts[i * 6 + 2].mPosition	=pos;
				pverts[i * 6 + 3].mPosition	=pos;
				pverts[i * 6 + 4].mPosition	=pos;
				pverts[i * 6 + 5].mPosition	=pos;

				//texcoordx
				pverts[i * 6 + 1].mPosition.W	=1;
				pverts[i * 6 + 2].mPosition.W	=1;
				pverts[i * 6 + 4].mPosition.W	=1;

				//texcoordy
				pverts[i * 6 + 2].mSizeRotAlpha.W	=1;
				pverts[i * 6 + 4].mSizeRotAlpha.W	=1;
				pverts[i * 6 + 5].mSizeRotAlpha.W	=1;

				pverts[i * 6].mSizeRotAlpha.X		=parts[i].mSize;
				pverts[i * 6 + 1].mSizeRotAlpha.X	=parts[i].mSize;
				pverts[i * 6 + 2].mSizeRotAlpha.X	=parts[i].mSize;
				pverts[i * 6 + 3].mSizeRotAlpha.X	=parts[i].mSize;
				pverts[i * 6 + 4].mSizeRotAlpha.X	=parts[i].mSize;
				pverts[i * 6 + 5].mSizeRotAlpha.X	=parts[i].mSize;

				pverts[i * 6].mSizeRotAlpha.Y		=parts[i].mRotation;
				pverts[i * 6 + 1].mSizeRotAlpha.Y	=parts[i].mRotation;
				pverts[i * 6 + 2].mSizeRotAlpha.Y	=parts[i].mRotation;
				pverts[i * 6 + 3].mSizeRotAlpha.Y	=parts[i].mRotation;
				pverts[i * 6 + 4].mSizeRotAlpha.Y	=parts[i].mRotation;
				pverts[i * 6 + 5].mSizeRotAlpha.Y	=parts[i].mRotation;

				pverts[i * 6].mSizeRotAlpha.Z		=parts[i].mAlpha;
				pverts[i * 6 + 1].mSizeRotAlpha.Z	=parts[i].mAlpha;
				pverts[i * 6 + 2].mSizeRotAlpha.Z	=parts[i].mAlpha;
				pverts[i * 6 + 3].mSizeRotAlpha.Z	=parts[i].mAlpha;
				pverts[i * 6 + 4].mSizeRotAlpha.Z	=parts[i].mAlpha;
				pverts[i * 6 + 5].mSizeRotAlpha.Z	=parts[i].mAlpha;
			}

			mNumParticles	=count;

			mVB.SetData<ParticleVert>(pverts);
		}


		internal void Draw(Vector4 color, Matrix view, Matrix proj)
		{
			mGD.SetVertexBuffer(mVB);

			mGD.DepthStencilState	=DepthStencilState.DepthRead;
			mGD.BlendState			=BlendState.Additive;
			mGD.RasterizerState		=RasterizerState.CullNone;

			mFX.CurrentTechnique	=mFX.Techniques["Particle"];

			mFX.Parameters["mSolidColour"].SetValue(color);
			mFX.Parameters["mTexture"].SetValue(mTex);
			mFX.Parameters["mView"].SetValue(view);
			mFX.Parameters["mProjection"].SetValue(proj);

			mFX.CurrentTechnique.Passes[0].Apply();

			mGD.DrawPrimitives(PrimitiveType.TriangleList, 0, mNumParticles * 2);
		}
	}
}
