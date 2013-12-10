using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	internal class AlphaNodeComparer : Comparer<AlphaNode>
	{
		Vector3	mEye;


		public AlphaNodeComparer(Vector3 eyePoint)
		{
			mEye	=eyePoint;
		}


		public override int Compare(AlphaNode x, AlphaNode y)
		{
			//if you don't have this here (even though it is covered below),
			//you get a nice many hours to find release only crash that will
			//only happen outside the debugger.  Thanks for that.
			if(x == y)
			{
				return	0;
			}

			if(x.DistSquared(mEye) == y.DistSquared(mEye))
			{
				return	0;
			}
			if(x.DistSquared(mEye) > y.DistSquared(mEye))
			{
				return	-1;
			}
			return	1;
		}
	}


	internal class AlphaNode
	{
		Vector3		mSortPoint;
		Material	mMaterial;

		//drawprim setup stuff
		VertexBuffer		mVB;
		IndexBuffer			mIB;
		Matrix				mWorldMat;

		//drawprim call numbers
		Int32	mBaseVertex;
		Int32	mMinVertexIndex;
		Int32	mNumVerts;
		Int32	mStartIndex;
		Int32	mPrimCount;

		bool		mbParticle;	//particle draw?
		bool		mbCel;
		Vector4		mColor;
		Effect		mFX;
		Texture2D	mTex;
		Matrix		mView;
		Matrix		mProj;


		internal AlphaNode(Vector3 sortPoint, Material matRef,
			VertexBuffer vb, IndexBuffer ib, Matrix worldMat,
			Int32 baseVert, Int32 minVertIndex,
			Int32 numVerts, Int32 startIndex, Int32 primCount)
		{
			mSortPoint		=sortPoint;
			mMaterial		=matRef;
			mVB				=vb;
			mIB				=ib;
			mWorldMat		=worldMat;
			mBaseVertex		=baseVert;
			mMinVertexIndex	=minVertIndex;
			mNumVerts		=numVerts;
			mStartIndex		=startIndex;
			mPrimCount		=primCount;
		}


		internal AlphaNode(Vector3 sortPoint,
			VertexBuffer vb, Int32 primCount,
			bool bCel, Vector4 color,
			Effect fx, Texture2D tex,
			Matrix view, Matrix proj)
		{
			mSortPoint	=sortPoint;
			mVB			=vb;
			mPrimCount	=primCount;
			mbCel		=bCel;
			mColor		=color;
			mFX			=fx;
			mTex		=tex;
			mView		=view;
			mProj		=proj;

			mbParticle	=true;
		}


		internal void Draw(GraphicsDevice g, MaterialLib mlib,
			int numShadows, AlphaPool.RenderShadows renderShadows)
		{
			if(mbParticle)
			{
				DrawParticle(g);
			}
			else
			{
				DrawRegular(g, mlib, numShadows, renderShadows);
			}
		}


		void DrawParticle(GraphicsDevice g)
		{
            g.SetVertexBuffer(mVB, 0);

			if(mPrimCount == 0)
			{
				return;
			}

			g.DepthStencilState	=DepthStencilState.DepthRead;
			g.BlendState		=BlendState.Additive;
			g.RasterizerState	=RasterizerState.CullCounterClockwise;

			if(mbCel)
			{
				mFX.CurrentTechnique	=mFX.Techniques["ParticleCel"];
			}
			else
			{
				mFX.CurrentTechnique	=mFX.Techniques["Particle"];
			}

			mFX.Parameters["mSolidColour"].SetValue(mColor);
			mFX.Parameters["mTexture"].SetValue(mTex);
			mFX.Parameters["mView"].SetValue(mView);
			mFX.Parameters["mProjection"].SetValue(mProj);

			mFX.CurrentTechnique.Passes[0].Apply();

			g.DrawPrimitives(PrimitiveType.TriangleList, 0, mPrimCount);

			g.SetVertexBuffer(null);
		}


		void DrawRegular(GraphicsDevice g, MaterialLib mlib,
			int numShadows, AlphaPool.RenderShadows renderShadows)
		{
            g.SetVertexBuffer(mVB, 0);
			g.Indices	=mIB;

			if(mNumVerts == 0 || mPrimCount == 0)
			{
				return;
			}

			Effect	fx	=mlib.GetShader(mMaterial.ShaderName);
			if(fx == null)
			{
				return;
			}

			mlib.ApplyParameters(mMaterial.Name);

			mMaterial.ApplyRenderStates(g);

			fx.Parameters["mWorld"].SetValue(mWorldMat);

			fx.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				mBaseVertex, mMinVertexIndex, mNumVerts,
				mStartIndex, mPrimCount);

			//draw shadows
			for(int i=0;i < numShadows;i++)
			{
				renderShadows(i);

				g.SetVertexBuffer(mVB, 0);
				g.Indices	=mIB;

				mlib.ApplyParameters(mMaterial.Name);

				fx.Parameters["mWorld"].SetValue(mWorldMat);

				fx.CurrentTechnique.Passes[1].Apply();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					mBaseVertex, mMinVertexIndex, mNumVerts,
					mStartIndex, mPrimCount);
			}
		}


		internal float DistSquared(Vector3 mEye)
		{
			Vector3	transformedSort	=Vector3.Transform(mSortPoint, mWorldMat);

			return	Vector3.DistanceSquared(transformedSort, mEye);
		}
	}
}
