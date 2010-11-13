using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


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
		protected VertexBuffer		mVerts;
		protected IndexBuffer		mIndexs;
		protected VertexDeclaration	mVD;
		protected int				mNumVerts, mNumTriangles, mVertSize;
		protected string			mMaterialName;
		protected int				mTypeIndex;
		protected bool				mbVisible;
		protected IRayCastable		mMeshBounds;

		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public string MaterialName
		{
			get { return mMaterialName; }
			set { mMaterialName = value; }
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
			mName			=name;
			mMaterialName	="";
		}


		public void SetVertexDeclaration(VertexDeclaration vd)
		{
			mVD	=vd;
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


		public void SetVertexBuffer(VertexBuffer vb)
		{
			mVerts	=vb;
		}


		public void SetIndexBuffer(IndexBuffer indbuf)
		{
			mIndexs	=indbuf;
		}


		public void SetTypeIndex(int idx)
		{
			mTypeIndex	=idx;
		}


		public virtual void Write(BinaryWriter bw) { }
		public virtual void Read(BinaryReader br, GraphicsDevice gd, bool bEditor) { }
		public virtual void Draw(GraphicsDevice g, MaterialLib.MaterialLib matLib) { }


		public float? RayIntersect(Vector3 start, Vector3 end)
		{
			return	mMeshBounds.RayIntersect(start, end);
		}


		public void Bound()
		{
			VertexTypes.GetVertBounds(mVerts, mNumVerts, mTypeIndex, mMeshBounds);
		}


		public IRayCastable GetBounds()
		{
			return	mMeshBounds;
		}
	}
}