using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MeshLib
{
	public class SkinnedMesh : Mesh
	{
		Matrix			[]mBones;
		Skin			mSkin;
		int				mSkinIndex;
		WearLocations	mSlot;


		public WearLocations Slot
		{
			get { return mSlot; }
			set { mSlot = value; }
		}


		public SkinnedMesh() : base()
		{
			mBounds	=new SphereBounds() as IRayCastable;
		}


		public SkinnedMesh(string name) : base(name)
		{
			mBounds	=new SphereBounds() as IRayCastable;
		}


		public void SetSkin(Skin sk)
		{
			mSkin	=sk;
		}


		public void SetSkinIndex(int idx)
		{
			mSkinIndex	=idx;
		}


		public int GetSkinIndex()
		{
			return	mSkinIndex;
		}


		public override void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mNumVerts);
			bw.Write(mNumTriangles);
			bw.Write(mVertSize);
			bw.Write(mMaterialName);
			bw.Write(mTypeIndex);
			bw.Write(mSkinIndex);
			bw.Write((UInt32)mSlot);
			bw.Write(mbVisible);

			mBounds.Write(bw);

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


		public override void Read(BinaryReader br, GraphicsDevice gd, bool bEditor)
		{
			mName			=br.ReadString();
			mNumVerts		=br.ReadInt32();
			mNumTriangles	=br.ReadInt32();
			mVertSize		=br.ReadInt32();
			mMaterialName	=br.ReadString();
			mTypeIndex		=br.ReadInt32();
			mSkinIndex		=br.ReadInt32();
			mSlot			=(WearLocations)br.ReadUInt32();
			mbVisible		=br.ReadBoolean();

			mBounds.Read(br);

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


		//just using the character bounds for now
		public override void Bound()
		{
		}


		public override void Draw(GraphicsDevice g, MaterialLib.MaterialLib matLib)
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
	}
}
