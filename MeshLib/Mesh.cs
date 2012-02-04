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
		protected BoundingBox		mBoxBound;
		protected BoundingSphere	mSphereBound;

		public string Name
		{
			get { return mName; }
			set { mName = UtilityLib.Misc.AssignValue(value); }
		}
		public string MaterialName
		{
			get { return mMaterialName; }
			set { mMaterialName = UtilityLib.Misc.AssignValue(value); }
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


		//borrowed from http://www.terathon.com/code/tangent.html
		public void GenTangents(GraphicsDevice gd, int texCoordSet)
		{
			UInt16	[]inds	=new UInt16[mNumTriangles * 3];

			mIndexs.GetData<UInt16>(inds);

			List<Vector3>	verts	=VertexTypes.GetPositions(mVerts, mNumVerts, mTypeIndex);			
			List<Vector2>	texs	=VertexTypes.GetTexCoord(mVerts, mNumVerts, mTypeIndex, texCoordSet);
			List<Vector3>	norms	=VertexTypes.GetNormals(mVerts, mNumVerts, mTypeIndex);

			if(texs.Count == 0)
			{
				return;
			}

			Vector3	[]stan	=new Vector3[mNumVerts];
			Vector3	[]ttan	=new Vector3[mNumVerts];
			Vector4	[]tang	=new Vector4[mNumVerts];

			for(int i=0;i < mNumTriangles;i++)
			{
				int	idx0	=inds[0 + (i * 3)];
				int	idx1	=inds[1 + (i * 3)];
				int	idx2	=inds[2 + (i * 3)];

				Vector3	v0	=verts[idx0];
				Vector3	v1	=verts[idx1];
				Vector3	v2	=verts[idx2];

				Vector2	w0	=texs[idx0];
				Vector2	w1	=texs[idx1];
				Vector2	w2	=texs[idx2];

				float	x0	=v1.X - v0.X;
				float	x1	=v2.X - v0.X;
				float	y0	=v1.Y - v0.Y;
				float	y1	=v2.Y - v0.Y;
				float	z0	=v1.Z - v0.Z;
				float	z1	=v2.Z - v0.Z;
				float	s0	=w1.X - w0.X;
				float	s1	=w2.X - w0.X;
				float	t0	=w1.Y - w0.Y;
				float	t1	=w2.Y - w0.Y;
				float	r	=1.0F / (s0 * t1 - s1 * t0);

				Vector3	sdir	=Vector3.Zero;
				Vector3	tdir	=Vector3.Zero;

				sdir.X	=(t1 * x0 - t0 * x1) * r;
				sdir.Y	=(t1 * y0 - t0 * y1) * r;
				sdir.Z	=(t1 * z0 - t0 * z1) * r;

				tdir.X	=(s0 * x1 - s1 * x0) * r;
				tdir.Y	=(s0 * y1 - s1 * y0) * r;
				tdir.Z	=(s0 * z1 - s1 * z0) * r;

				stan[idx0]	=sdir;
				stan[idx1]	=sdir;
				stan[idx2]	=sdir;

				ttan[idx0]	=tdir;
				ttan[idx1]	=tdir;
				ttan[idx2]	=tdir;
			}

			for(int i=0;i < mNumVerts;i++)
			{
				Vector3	norm	=norms[i];
				Vector3	t		=stan[i];

				float	dot	=Vector3.Dot(norm, t);

				Vector3	tan	=t - norm * dot;
				tan.Normalize();

				Vector3	norm2	=Vector3.Cross(norm, t);

				dot	=Vector3.Dot(norm2, ttan[i]);

				float	hand	=(dot < 0.0f)? -1.0f : 1.0f;

				tang[i].X	=tan.X;
				tang[i].Y	=tan.Y;
				tang[i].Z	=tan.Z;
				tang[i].W	=hand;
			}

			mVerts	=VertexTypes.AddTangents(gd, mVerts, mNumVerts, mTypeIndex, tang, out mTypeIndex);
		}


		public virtual void Write(BinaryWriter bw) { }
		public virtual void Read(BinaryReader br, GraphicsDevice gd, bool bEditor) { }
		public virtual void Draw(GraphicsDevice g, MaterialLib.MaterialLib matLib, Matrix world) { }


		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			if(bBox)
			{
				return	UtilityLib.Mathery.RayIntersectBox(start, end, mBoxBound);
			}
			else
			{
				return	UtilityLib.Mathery.RayIntersectSphere(start, end, mSphereBound);
			}
		}


		public virtual void Bound()
		{
			VertexTypes.GetVertBounds(mVerts, mNumVerts, mTypeIndex, out mBoxBound, out mSphereBound);
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