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
			mMeshBounds	=new AxialBounds() as IRayCastable;
		}


		public StaticMesh(string name) : base(name)
		{
			mMeshBounds	=new AxialBounds() as IRayCastable;
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


		public override void Read(BinaryReader br, GraphicsDevice gd, bool bEditor)
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
	}
}