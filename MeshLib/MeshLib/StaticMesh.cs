using System;
using System.Collections.Generic;
using System.Xml;
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
			mBounds	=new AxialBounds() as IRayCastable;
		}


		public StaticMesh(string name) : base(name)
		{
			mBounds	=new AxialBounds() as IRayCastable;
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

			mBounds.Write(bw);

			VertexTypes.WriteVerts(bw, mVerts, mTypeIndex);

			ushort	[]idxs	=new ushort[mNumTriangles * 3];

			mIndexs.GetData<ushort>(idxs);

			bw.Write(idxs.Length);

			for(int i=0;i < idxs.Length;i++)
			{
				bw.Write(idxs[i]);
			}

			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mVD);
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
				mIndexs	=new IndexBuffer(gd, IndexElementSize.SixteenBits, numIdx, BufferUsage.None);
			}
			else
			{
				mIndexs	=new IndexBuffer(gd, IndexElementSize.SixteenBits, numIdx, BufferUsage.WriteOnly);
			}
			mIndexs.SetData<ushort>(idxs);

			UtilityLib.FileUtil.ReadVertexDeclaration(br, out mVD);
		}


		public override void Draw(GraphicsDevice g, MaterialLib.MaterialLib matLib, Matrix world)
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

			matLib.ApplyParameters(mMaterialName);
			mat.ApplyRenderStates(g);

			fx.Parameters["mWorld"].SetValue(world);

			fx.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0,
				mNumVerts,
				0,
				mNumTriangles);
		}
	}
}