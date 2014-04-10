using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;


namespace MeshLib
{
	public enum WearLocations
	{
		NONE					=0,
		BOOT					=1,
		TORSO					=2,
		GLOVE					=4,
		HAT						=8,
		SHOULDERS				=16,
		FACE					=32,
		LEFT_HAND				=64,
		RIGHT_HAND				=128,
		HAIR					=256,
		BACK					=512,
		BRACERS					=1024,
		RING_LEFT				=2048,
		RING_RIGHT				=4096,
		EARRING_LEFT			=8192,
		EARRING_RIGHT			=16384,
		BELT					=32768,	//is this gonna overflow?
		HAT_FACE				=24,	//orred together values
		HAT_HAIR				=40,	//for the editor
		FACE_HAIR				=48,
		HAT_EARRINGS			=24584,
		HAT_HAIR_EARRINGS		=24840,
		HAT_FACE_HAIR_EARRINGS	=24872,
		GLOVE_RINGS				=6148
	}

	public class Mesh
	{
		protected string			mName;
		protected Buffer			mVerts;
		protected Buffer			mIndexs;
		protected int				mNumVerts, mNumTriangles, mVertSize;
		protected string			mMaterialName;
		protected int				mTypeIndex;
		protected bool				mbVisible;
		protected BoundingBox		mBoxBound;
		protected BoundingSphere	mSphereBound;
		protected Matrix			mTransform;

		protected VertexBufferBinding	mVBinding;


		public string Name
		{
			get { return mName; }
			set { mName = ((value == null)? "" : value); }
		}
		public string MaterialName
		{
			get { return mMaterialName; }
			set { mMaterialName = ((value == null)? "" : value); }
		}
		public Type VertexType
		{
			get { return VertexTypes.GetTypeForIndex(mTypeIndex); }
			private set { mTypeIndex = VertexTypes.GetIndex(value); }
		}
		public bool Visible
		{
			get { return mbVisible; }
			set { mbVisible = value; }
		}
		

		public Mesh() { }
		public Mesh(string name)
		{
			Name			=name;
			mMaterialName	="";
		}


		public Matrix GetTransform()
		{
			return	mTransform;
		}


		public void SetVertSize(int size)
		{
			mVertSize	=size;
		}


		public void SetNumVerts(int nv)
		{
			mNumVerts	=nv;
		}


		public void SetNumTriangles(int numTri)
		{
			mNumTriangles	=numTri;
		}


		public void SetVertexBuffer(Buffer vb)
		{
			mVerts		=vb;
			mVBinding	=new VertexBufferBinding(mVerts, VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
		}


		public void SetIndexBuffer(Buffer indbuf)
		{
			mIndexs	=indbuf;
		}


		public void SetTypeIndex(int idx)
		{
			mTypeIndex	=idx;
		}


		public void SetTransform(Matrix mat)
		{
			mTransform	=mat;
		}


		public virtual void Write(BinaryWriter bw) { }
		public virtual void Read(BinaryReader br, Device gd, bool bEditor) { }
//		public virtual void Draw(Device g, MaterialLib.MaterialLib matLib, Matrix world, string altMaterial) { }
//		public virtual void DrawDMN(Device g, MaterialLib.MaterialLib matLib, MaterialLib.IDKeeper idk, Matrix world) { }
//		public virtual void Draw(Device g, MaterialLib.MaterialLib matLib, int numInstances) { }
		public virtual void SetSecondVertexBufferBinding(VertexBufferBinding v2) { }

		internal void TempDraw(DeviceContext dc, MaterialLib.MaterialLib matLib, Matrix transform)
		{
			matLib.SetMaterialParameter(mMaterialName, "mWorld", (transform * mTransform));

			dc.InputAssembler.SetVertexBuffers(0, mVBinding);
			dc.InputAssembler.SetIndexBuffer(mIndexs, Format.R16_UInt, 0);

			matLib.ApplyMaterialPass(mMaterialName, dc, 0);

			dc.DrawIndexed(mNumTriangles * 3, 0, 0);
		}


		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			if(bBox)
			{
				return	Mathery.RayIntersectBox(start, end, mBoxBound);
			}
			else
			{
				return	Mathery.RayIntersectSphere(start, end, mSphereBound);
			}
		}


		public virtual void Bound()
		{
			throw new NotImplementedException();
		}


		public BoundingBox GetBoxBounds()
		{
			return	mBoxBound;
		}


		public BoundingSphere GetSphereBounds()
		{
			return	mSphereBound;
		}
	}
}