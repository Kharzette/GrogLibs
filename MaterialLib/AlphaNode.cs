using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


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

			if(x.Distance(mEye) == y.Distance(mEye))
			{
				return	0;
			}
			if(x.Distance(mEye) > y.Distance(mEye))
			{
				return	-1;
			}
			return	1;
		}
	}


	internal class AlphaNode
	{
		//sorting
		Vector3	mSortPoint;
		Vector3	mSortPlaneNormal;
		float	mSortPlaneDistance;
		bool	mbSortPlanar;

		string	mMaterialName;
		MatLib	mMatLib;

		//drawprim setup stuff
		VertexBufferBinding	mVBB;
		Buffer				mIB;
		Matrix				mWorldMat;

		//drawprim index or vertex count
		Int32	mCount;
		Int32	mStartIndex;

		bool	mbParticle;	//particle draw?
		string	mTex;
		Matrix	mView, mProj;


		internal AlphaNode(MatLib matLib,
			Vector3 sortPoint, string matName,
			VertexBufferBinding vbb, Buffer ib,
			Matrix worldMat, Int32 startIndex, Int32 indexCount)
		{
			mMatLib			=matLib;
			mSortPoint		=sortPoint;
			mMaterialName	=matName;
			mVBB			=vbb;
			mIB				=ib;
			mWorldMat		=worldMat;
			mCount			=indexCount;
			mStartIndex		=startIndex;
		}


		internal AlphaNode(MatLib matLib,
			Vector3 sortPoint, Vector3 sortPlaneNormal,
			float sortPlaneDistance, string matName,
			VertexBufferBinding vbb, Buffer ib,
			Matrix worldMat, Int32 startIndex, Int32 indexCount)
		{
			mMatLib				=matLib;
			mSortPoint			=sortPoint;
			mbSortPlanar		=true;
			mSortPlaneNormal	=sortPlaneNormal;
			mSortPlaneDistance	=sortPlaneDistance;
			mMaterialName		=matName;
			mVBB				=vbb;
			mIB					=ib;
			mWorldMat			=worldMat;
			mCount				=indexCount;
			mStartIndex			=startIndex;
		}


		internal AlphaNode(MatLib matLib,
			Vector3 sortPoint,
			VertexBufferBinding vbb,
			Int32 vertCount, string tex,
			Matrix view, Matrix proj)
		{
			mMatLib			=matLib;
			mSortPoint		=sortPoint;
			mVBB			=vbb;
			mCount			=vertCount;
			mTex			=tex;
			mView			=view;
			mProj			=proj;
			mMaterialName	="Particle";
			mWorldMat		=Matrix.Identity;

			mbParticle	=true;
		}


		internal void Draw(GraphicsDevice gd)
		{
			if(mbParticle)
			{
				DrawParticle(gd);
			}
			else
			{
				DrawRegular(gd);
			}
		}


		void DrawParticle(GraphicsDevice gd)
		{
			if(mCount <= 0)
			{
				return;
			}

			gd.DC.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialTexture(mMaterialName, "mTexture", mTex);
			mMatLib.SetMaterialParameter(mMaterialName, "mView", mView);
			mMatLib.SetMaterialParameter(mMaterialName, "mProjection", mProj);

			mMatLib.ApplyMaterialPass(mMaterialName, gd.DC, 0);

			gd.DC.Draw(mCount, 0);
		}


		void DrawRegular(GraphicsDevice gd)
		{
			if(mCount <= 0)
			{
				return;
			}

			gd.DC.InputAssembler.SetVertexBuffers(0, mVBB);
			gd.DC.InputAssembler.SetIndexBuffer(mIB, Format.R16_UInt, 0);

			mMatLib.SetMaterialParameter(mMaterialName, "mWorld", mWorldMat);

			mMatLib.ApplyMaterialPass(mMaterialName, gd.DC, 0);

			gd.DC.DrawIndexed(mCount, mStartIndex, 0);
		}


		internal bool IsPlanar()
		{
			return	mbSortPlanar;
		}


		internal float PlaneDistance(Vector3 pos)
		{
			float	dist	=Vector3.Dot(mSortPlaneNormal, pos);
			dist			-=mSortPlaneDistance;

			return	dist;
		}


		internal float PlaneDistance(AlphaNode otherNode)
		{
			return	PlaneDistance(otherNode.mSortPoint);
		}


		internal float Distance(Vector3 mEye)
		{
			Vector3	transformedSort	=Vector3.TransformCoordinate(mSortPoint, mWorldMat);

			return	Vector3.Distance(transformedSort, mEye);
		}
	}
}
