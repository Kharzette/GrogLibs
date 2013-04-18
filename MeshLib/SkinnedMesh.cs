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
		}


		public SkinnedMesh(string name) : base(name)
		{
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

			//box bound
			UtilityLib.FileUtil.WriteVector3(bw, mBoxBound.Min);
			UtilityLib.FileUtil.WriteVector3(bw, mBoxBound.Max);

			//sphere bound
			UtilityLib.FileUtil.WriteVector3(bw, mSphereBound.Center);
			bw.Write(mSphereBound.Radius);

			VertexTypes.WriteVerts(bw, mVerts, mTypeIndex);

			ushort	[]idxs	=new ushort[mNumTriangles * 3];

			mIndexs.GetData<ushort>(idxs);

			bw.Write(idxs.Length);

			for(int i=0;i < idxs.Length;i++)
			{
				bw.Write(idxs[i]);
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

			mBoxBound.Min		=UtilityLib.FileUtil.ReadVector3(br);
			mBoxBound.Max		=UtilityLib.FileUtil.ReadVector3(br);
			mSphereBound.Center	=UtilityLib.FileUtil.ReadVector3(br);
			mSphereBound.Radius	=br.ReadSingle();

			VertexTypes.ReadVerts(br, gd, out mVerts, mNumVerts, mTypeIndex, bEditor);

			int		numIdx	=br.ReadInt32();
			ushort	[]idxs	=new ushort[numIdx];

			for(int i=0;i < numIdx;i++)
			{
				idxs[i]	=br.ReadUInt16();
			}

			if(bEditor)
			{
				mIndexs	=new IndexBuffer(gd, IndexElementSize.SixteenBits, numIdx, BufferUsage.None);
			}
			else
			{
				mIndexs	=new IndexBuffer(gd, IndexElementSize.SixteenBits, numIdx, BufferUsage.WriteOnly);
			}
			mIndexs.SetData<ushort>(idxs);
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


		public override void Draw(GraphicsDevice g,
			MaterialLib.MaterialLib matLib, Matrix world, string altMaterial)
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

			g.SetVertexBuffer(mVerts);
			g.Indices	=mIndexs;

			UpdateShaderBones(fx);

			if(altMaterial == "")
			{
				matLib.ApplyParameters(mMaterialName);
			}
			else
			{
				matLib.ApplyParameters(altMaterial);
			}

			fx.Parameters["mWorld"].SetValue(world);

			if(fx.Parameters["mBindPose"] != null)
			{
				if(mSkin != null)
				{
					fx.Parameters["mBindPose"].SetValue(mSkin.GetBindShapeMatrix());
				}
			}

			mat.ApplyRenderStates(g);

			fx.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0,
				mNumVerts,
				0,
				mNumTriangles);
		}
	}
}
