using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Character
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
		string				mName;
		VertexBuffer		mVerts;
		IndexBuffer			mIndexs;
		VertexDeclaration	mVD;
		Matrix				[]mBones;
		int					mNumVerts, mNumTriangles, mVertSize;
		string				mMaterialName;
		Skin				mSkin;
		int					mSkinIndex, mTypeIndex;
		bool				mbVisible;
		WearLocations		mSlot;
		Bounds				mMeshBounds;

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
		public WearLocations Slot
		{
			get { return mSlot; }
			set { mSlot = value; }
		}


		public Mesh() { }
		public Mesh(string name)
		{
			mName			=name;
			mMaterialName	="";
		}


		public void SetSkin(Skin sk)
		{
			mSkin	=sk;
		}


		public void SetSkinIndex(int idx)
		{
			mSkinIndex	=idx;
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


		public int GetSkinIndex()
		{
			return	mSkinIndex;
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mNumVerts);
			bw.Write(mNumTriangles);
			bw.Write(mVertSize);
			bw.Write(mMaterialName);
			bw.Write(mTypeIndex);
			bw.Write(mSkinIndex);
			bw.Write((UInt32)mSlot);

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
			mSkinIndex		=br.ReadInt32();
			mSlot			=(WearLocations)br.ReadUInt32();

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


		//copies bones into the shader
		public void UpdateShaderBones(Effect fx)
		{
			//some chunks are never really drawn
			if(mBones != null)
			{
				fx.Parameters["mBones"].SetValue(mBones);
			}
		}


		public void UpdateBones(Skeleton sk)
		{
			//no need for this if not skinned
			if(mSkin == null || sk == null)
			{
				return;
			}

			if(mBones == null)
			{
				mBones	=new Matrix[mSkin.GetNumBones()];
			}
			for(int i=0;i < mBones.Length;i++)
			{
				mBones[i]	=mSkin.GetBoneByIndex(i, sk);
			}
		}


		public void Draw(GraphicsDevice g, MaterialLib matLib)
		{
			if(!mbVisible)
			{
				return;
			}

			Material	mat	=matLib.GetMaterial(mMaterialName);
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

			UpdateShaderBones(fx);

			//this might get slow
			matLib.ApplyParameters(mMaterialName);

			if(fx.Parameters["mBindPose"] != null)
			{
				if(mSkin != null)
				{
					fx.Parameters["mBindPose"].SetValue(mSkin.GetBindShapeMatrix());
				}
			}

			//set renderstates from material
			//this could also get crushingly slow
			g.RenderState.AlphaBlendEnable			=mat.Alpha;
			g.RenderState.AlphaFunction				=CompareFunction.Less;
			g.RenderState.AlphaTestEnable			=mat.AlphaTest;
			g.RenderState.BlendFunction				=mat.BlendFunction;
			g.RenderState.SourceBlend				=mat.SourceBlend;
			g.RenderState.DestinationBlend			=mat.DestBlend;
			g.RenderState.DepthBufferWriteEnable	=mat.DepthWrite;
			g.RenderState.CullMode					=mat.CullMode;
			g.RenderState.DepthBufferFunction		=mat.ZFunction;

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
			if(mMeshBounds == null)
			{
				return	false;
			}
			return	mMeshBounds.RayIntersect(start, end);
		}
	}
}