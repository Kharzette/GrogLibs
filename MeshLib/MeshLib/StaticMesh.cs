using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MeshLib
{
	public class StaticMesh
	{
		string				mName;
		VertexBuffer		mVerts;
		IndexBuffer			mIndexs;
		VertexDeclaration	mVD;
		int					mNumVerts, mNumTriangles, mVertSize;
		string				mMaterialName;
		int					mTypeIndex;
		bool				mbVisible;
		Bounds				mMeshBounds	=new Bounds();


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


		public StaticMesh() { }
		public StaticMesh(string name)
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


		public Vector3 GetBoundsCenter()
		{
			return	(mMeshBounds.mMins + mMeshBounds.mMaxs) / 2.0f;
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mNumVerts);
			bw.Write(mNumTriangles);
			bw.Write(mVertSize);
			bw.Write(mMaterialName);
			bw.Write(mTypeIndex);
			bw.Write(mbVisible);

			mMeshBounds.Write(bw);

			VertexTypes.WriteVerts(bw, mVerts, mNumVerts, mTypeIndex);

			ushort	[]idxs	=new ushort[mNumTriangles * 3];

			mIndexs.GetData<ushort>(idxs);

			bw.Write(idxs.Length);

			for(int i=0;i < idxs.Length;i++)
			{
				bw.Write(idxs[i]);
			}

			VertexElement	[]elms	=mVD.GetVertexElements();

			bw.Write(elms.Length);
			foreach(VertexElement ve in elms)
			{
				bw.Write(ve.Stream);
				bw.Write(ve.Offset);
				bw.Write((UInt32)ve.VertexElementFormat);
				bw.Write((UInt32)ve.VertexElementMethod);
				bw.Write((UInt32)ve.VertexElementUsage);
				bw.Write(ve.UsageIndex);
			}
		}


		public void Read(BinaryReader br, GraphicsDevice gd, bool bEditor)
		{
			mName			=br.ReadString();
			mNumVerts		=br.ReadInt32();
			mNumTriangles	=br.ReadInt32();
			mVertSize		=br.ReadInt32();
			mMaterialName	=br.ReadString();
			mTypeIndex		=br.ReadInt32();
			mbVisible		=br.ReadBoolean();

			mMeshBounds.Read(br);

			VertexTypes.ReadVerts(br, gd, out mVerts, mNumVerts, mTypeIndex, bEditor);

			int		numIdx	=br.ReadInt32();
			ushort	[]idxs	=new ushort[numIdx];

			for(int i=0;i < numIdx;i++)
			{
				idxs[i]	=br.ReadUInt16();
			}

			if(bEditor)
			{
				mIndexs	=new IndexBuffer(gd, numIdx * 2, BufferUsage.None, IndexElementSize.SixteenBits);
			}
			else
			{
				mIndexs	=new IndexBuffer(gd, numIdx * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
			}
			mIndexs.SetData<ushort>(idxs);

			int	numElements	=br.ReadInt32();
			VertexElement	[]vels	=new VertexElement[numElements];
			for(int i=0;i < numElements;i++)
			{
				short	streamIdx	=br.ReadInt16();
				short	offset		=br.ReadInt16();

				VertexElementFormat	vef	=(VertexElementFormat)br.ReadUInt32();
				VertexElementMethod	vem	=(VertexElementMethod)br.ReadUInt32();
				VertexElementUsage	veu	=(VertexElementUsage)br.ReadUInt32();

				byte	usageIndex	=br.ReadByte();

				vels[i]	=new VertexElement(streamIdx, offset, vef, vem, veu, usageIndex);
			}
			mVD	=new VertexDeclaration(gd, vels);
		}


		//this could be used for pre or post steps that
		//need an external effect for shadows or whatever
		public void Draw(GraphicsDevice g, Effect fx)
		{
			if(!mbVisible)
			{
				return;
			}

			if(fx == null)
			{
				return;
			}

			g.Vertices[0].SetSource(mVerts, 0, mVertSize);
			g.Indices			=mIndexs;
			g.VertexDeclaration	=mVD;

			//assume all parameters are set up
			//and all renderstates are where they need to be
			fx.Begin();
			foreach(EffectPass pass in fx.CurrentTechnique.Passes)
			{
				pass.Begin();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0,
					mNumVerts,
					0,
					mNumTriangles);

				pass.End();
			}
			fx.End();
		}


		public void Draw(GraphicsDevice g, MaterialLib.MaterialLib matLib)
		{
			if(!mbVisible)
			{
				return;
			}

			MaterialLib.Material	mat	=matLib.GetMaterial(mMaterialName);
			if(mat == null)
			{
				return;
			}

			Effect		fx	=matLib.GetShader(mat.ShaderName);
			if(fx == null)
			{
				return;
			}

			g.Vertices[0].SetSource(mVerts, 0, mVertSize);
			g.Indices			=mIndexs;
			g.VertexDeclaration	=mVD;

			//this might get slow
			matLib.ApplyParameters(mMaterialName);

			//set renderstates from material
			//this could also get crushingly slow
			g.RenderState.AlphaBlendEnable			=mat.Alpha;
			g.RenderState.AlphaTestEnable			=mat.AlphaTest;
			g.RenderState.BlendFunction				=mat.BlendFunction;
			g.RenderState.SourceBlend				=mat.SourceBlend;
			g.RenderState.DestinationBlend			=mat.DestBlend;
			g.RenderState.DepthBufferWriteEnable	=mat.DepthWrite;
			g.RenderState.CullMode					=mat.CullMode;
			g.RenderState.DepthBufferFunction		=mat.ZFunction;

			fx.CommitChanges();

			fx.Begin();
			foreach(EffectPass pass in fx.CurrentTechnique.Passes)
			{
				pass.Begin();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0,
					mNumVerts,
					0,
					mNumTriangles);

				pass.End();
			}
			fx.End();
		}


		public void Bound()
		{
			mMeshBounds	=VertexTypes.GetVertBounds(mVerts, mNumVerts, mTypeIndex);
		}


		public Bounds GetBounds()
		{
			return	mMeshBounds;
		}


		internal bool RayIntersectBounds(Vector3 start, Vector3 end)
		{
			return	mMeshBounds.RayIntersect(start, end);
		}
	}
}