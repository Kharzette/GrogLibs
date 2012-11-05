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


		internal void Draw(GraphicsDevice g, MaterialLib mlib)
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
		}


		internal float DistSquared(Vector3 mEye)
		{
			Vector3	transformedSort	=Vector3.Transform(mSortPoint, mWorldMat);

			return	Vector3.DistanceSquared(transformedSort, mEye);
		}
	}
}
