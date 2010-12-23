using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	internal class AlphaNodeComparer : IComparer<AlphaNode>
	{
		Vector3	mEye;


		public AlphaNodeComparer(Vector3 eyePoint)
		{
			mEye	=eyePoint;
		}


		public int Compare(AlphaNode x, AlphaNode y)
		{
			if(x.DistSquared(mEye) == y.DistSquared(mEye))
			{
				return	0;
			}
			if(x.DistSquared(mEye) < y.DistSquared(mEye))
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
		VertexDeclaration	mVD;
		Int32				mVertStride;

		//drawprim call numbers
		Int32	mBaseVertex;
		Int32	mMinVertexIndex;
		Int32	mNumVerts;
		Int32	mStartIndex;
		Int32	mPrimCount;


		internal AlphaNode(Vector3 sortPoint, Material matRef,
			VertexBuffer vb, IndexBuffer ib, VertexDeclaration vd,
			Int32 vbStride, Int32 baseVert, Int32 minVertIndex,
			Int32 numVerts, Int32 startIndex, Int32 primCount)
		{
			mSortPoint		=sortPoint;
			mMaterial		=matRef;
			mVB				=vb;
			mIB				=ib;
			mVD				=vd;
			mVertStride		=vbStride;
			mBaseVertex		=baseVert;
			mMinVertexIndex	=minVertIndex;
			mNumVerts		=numVerts;
			mStartIndex		=startIndex;
			mPrimCount		=primCount;
		}


		internal void Draw(GraphicsDevice g, MaterialLib mlib)
		{
			g.VertexDeclaration	=mVD;
			g.Vertices[0].SetSource(mVB, 0, mVertStride);
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

			//set renderstates from material
			g.RenderState.AlphaBlendEnable			=mMaterial.Alpha;
			g.RenderState.AlphaTestEnable			=mMaterial.AlphaTest;
			g.RenderState.BlendFunction				=mMaterial.BlendFunction;
			g.RenderState.SourceBlend				=mMaterial.SourceBlend;
			g.RenderState.DestinationBlend			=mMaterial.DestBlend;
			g.RenderState.DepthBufferWriteEnable	=mMaterial.DepthWrite;
			g.RenderState.CullMode					=mMaterial.CullMode;
			g.RenderState.DepthBufferFunction		=mMaterial.ZFunction;

			fx.CommitChanges();

			fx.Begin();
			foreach(EffectPass pass in fx.CurrentTechnique.Passes)
			{
				pass.Begin();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					mBaseVertex, mMinVertexIndex, mNumVerts,
					mStartIndex, mPrimCount);

				pass.End();
			}
			fx.End();
		}


		internal float DistSquared(Vector3 mEye)
		{
			return	Vector3.DistanceSquared(mSortPoint, mEye);
		}
	}
}
