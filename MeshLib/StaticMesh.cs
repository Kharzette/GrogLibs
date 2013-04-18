using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MeshLib
{
	public class StaticMesh : Mesh
	{
		public StaticMesh() : base()
		{
		}


		public StaticMesh(string name) : base(name)
		{
		}


		public override void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mNumVerts);
			bw.Write(mNumTriangles);
			bw.Write(mVertSize);
			bw.Write(mMaterialName);
			bw.Write(mTypeIndex);
			bw.Write(mbVisible);
			
			//transform
			UtilityLib.FileUtil.WriteMatrix(bw, mTransform);

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
			mbVisible		=br.ReadBoolean();

			mTransform	=UtilityLib.FileUtil.ReadMatrix(br);

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

			mVBinding[0]	=new VertexBufferBinding(mVerts);
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
			g.Indices			=mIndexs;

			if(altMaterial == "")
			{
				matLib.ApplyParameters(mMaterialName);
			}
			else
			{
				matLib.ApplyParameters(altMaterial);
			}

			mat.ApplyRenderStates(g);

			fx.Parameters["mWorld"].SetValue(mTransform * world);

			fx.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0,
				mNumVerts,
				0,
				mNumTriangles);
		}


		public override void SetSecondVertexBufferBinding(VertexBufferBinding v2)
		{
			mVBinding[1]	=v2;
		}


		public override void Draw(GraphicsDevice g,
			MaterialLib.MaterialLib matLib, int numInstances)
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

			g.SetVertexBuffers(mVBinding);

			g.Indices	=mIndexs;

			matLib.ApplyParameters(mMaterialName);
			mat.ApplyRenderStates(g);

			fx.CurrentTechnique.Passes[0].Apply();

			g.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
				mNumVerts, 0, mNumTriangles, numInstances);
		}
	}
}