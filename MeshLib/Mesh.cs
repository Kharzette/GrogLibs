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

		protected VertexBufferBinding	mVBBinding;


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


		public void FreeAll()
		{
			mVerts.Dispose();
			mIndexs.Dispose();
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
			mVBBinding	=new VertexBufferBinding(mVerts, VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
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


		public virtual void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mNumVerts);
			bw.Write(mNumTriangles);
			bw.Write(mVertSize);
			bw.Write(mMaterialName);
			bw.Write(mTypeIndex);
			bw.Write(mbVisible);

			//transform
			FileUtil.WriteMatrix(bw, mTransform);

			//box bound
			FileUtil.WriteVector3(bw, mBoxBound.Minimum);
			FileUtil.WriteVector3(bw, mBoxBound.Maximum);

			//sphere bound
			FileUtil.WriteVector3(bw, mSphereBound.Center);
			bw.Write(mSphereBound.Radius);
		}


		public virtual void Read(BinaryReader br, Device gd, bool bEditor)
		{
			mName			=br.ReadString();
			mNumVerts		=br.ReadInt32();
			mNumTriangles	=br.ReadInt32();
			mVertSize		=br.ReadInt32();
			mMaterialName	=br.ReadString();
			mTypeIndex		=br.ReadInt32();
			mbVisible		=br.ReadBoolean();

			mTransform	=FileUtil.ReadMatrix(br);

			mBoxBound.Minimum	=FileUtil.ReadVector3(br);
			mBoxBound.Maximum	=FileUtil.ReadVector3(br);

			mSphereBound.Center	=FileUtil.ReadVector3(br);
			mSphereBound.Radius	=br.ReadSingle();

			if(!bEditor)
			{
				Array	vertArray;

				VertexTypes.ReadVerts(br, gd, out vertArray);

				int	indLen	=br.ReadInt32();

				UInt16	[]indArray	=new UInt16[indLen];

				for(int i=0;i < indLen;i++)
				{
					indArray[i]	=br.ReadUInt16();
				}

				mVerts	=VertexTypes.BuildABuffer(gd, vertArray, mTypeIndex);
				mIndexs	=VertexTypes.BuildAnIndexBuffer(gd, indArray);

				mVBBinding	=new VertexBufferBinding(mVerts,
					VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
			}
		}


		internal void DrawDMN(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			MaterialLib.IDKeeper idk,
			Matrix world)
		{
			if(!mbVisible)
			{
				return;
			}

			if(!matLib.MaterialExists("DMN"))
			{
				return;
			}

			int	id	=idk.GetID(mMaterialName);
			if(id == -1)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBBinding);
			dc.InputAssembler.SetIndexBuffer(mIndexs, Format.R16_UInt, 0);

			matLib.SetMaterialParameter("DMN", "mWorld", world);
			matLib.SetMaterialParameter("DMN", "mMaterialID", id);

			matLib.ApplyMaterialPass(mMaterialName, dc, 0);

			dc.DrawIndexed(mNumTriangles * 3, 0, 0);
		}


		internal void Draw(DeviceContext dc, MaterialLib.MaterialLib matLib, Matrix transform)
		{
			if(!matLib.MaterialExists(mMaterialName))
			{
				return;
			}

			matLib.SetMaterialParameter(mMaterialName, "mWorld", (mTransform * transform));

			dc.InputAssembler.SetVertexBuffers(0, mVBBinding);
			dc.InputAssembler.SetIndexBuffer(mIndexs, Format.R16_UInt, 0);

			matLib.ApplyMaterialPass(mMaterialName, dc, 0);

			dc.DrawIndexed(mNumTriangles * 3, 0, 0);
		}


		internal void Draw(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			Matrix transform, string altMat)
		{
			if(!matLib.MaterialExists(altMat))
			{
				return;
			}

			matLib.SetMaterialParameter(altMat, "mWorld", (mTransform * transform));

			dc.InputAssembler.SetVertexBuffers(0, mVBBinding);
			dc.InputAssembler.SetIndexBuffer(mIndexs, Format.R16_UInt, 0);

			matLib.ApplyMaterialPass(altMat, dc, 0);

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