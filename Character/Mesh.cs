using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
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

			WriteVerts(bw);

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


		public void Read(BinaryReader br, GraphicsDevice gd)
		{
			mName			=br.ReadString();
			mNumVerts		=br.ReadInt32();
			mNumTriangles	=br.ReadInt32();
			mVertSize		=br.ReadInt32();
			mMaterialName	=br.ReadString();
			mTypeIndex		=br.ReadInt32();
			mSkinIndex		=br.ReadInt32();

			ReadVerts(br, gd);

			int		numIdx	=br.ReadInt32();
			ushort	[]idxs	=new ushort[numIdx];

			for(int i=0;i < numIdx;i++)
			{
				idxs[i]	=br.ReadUInt16();
			}

			mIndexs	=new IndexBuffer(gd, numIdx * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
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
			if(mSkin == null)
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
			g.Vertices[0].SetSource(mVerts, 0, mVertSize);
			g.Indices			=mIndexs;
			g.VertexDeclaration	=mVD;

			Effect	fx	=matLib.GetMaterialShader(mMaterialName);

			if(fx == null)
			{
				return;
			}

			UpdateShaderBones(fx);

			//this might get slow
			matLib.ApplyParameters(mMaterialName);

			if(fx.Parameters["mBindPose"] != null)
			{
				fx.Parameters["mBindPose"].SetValue(mSkin.GetBindShapeMatrix());
			}

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


		private void WriteVerts(BinaryWriter bw)
		{
			switch(mTypeIndex)
			{
				case	0:
					VertexTypes.WriteVPos(bw, mVerts, mNumVerts);
					break;
				case	1:
					VertexTypes.WriteVPosNorm(bw, mVerts, mNumVerts);
					break;
				case	2:
					VertexTypes.WriteVPosBone(bw, mVerts, mNumVerts);
					break;
				case	3:
					VertexTypes.WriteVPosTex0(bw, mVerts, mNumVerts);
					break;
				case	4:
					VertexTypes.WriteVPosTex0Tex1(bw, mVerts, mNumVerts);
					break;
				case	5:
					VertexTypes.WriteVPosTex0Tex1Tex2(bw, mVerts, mNumVerts);
					break;
				case	6:
					VertexTypes.WriteVPosTex0Tex1Tex2Tex3(bw, mVerts, mNumVerts);
					break;
				case	7:
					VertexTypes.WriteVPosCol0(bw, mVerts, mNumVerts);
					break;
				case	8:
					VertexTypes.WriteVPosCol0Col1(bw, mVerts, mNumVerts);
					break;
				case	9:
					VertexTypes.WriteVPosCol0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	10:
					VertexTypes.WriteVPosCol0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	11:
					VertexTypes.WriteVPosTex0Col0(bw, mVerts, mNumVerts);
					break;
				case	12:
					VertexTypes.WriteVPosTex0Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	13:
					VertexTypes.WriteVPosTex0Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	14:
					VertexTypes.WriteVPosTex0Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	15:
					VertexTypes.WriteVPosTex0Tex1Col0(bw, mVerts, mNumVerts);
					break;
				case	16:
					VertexTypes.WriteVPosTex0Tex1Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	17:
					VertexTypes.WriteVPosTex0Tex1Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	18:
					VertexTypes.WriteVPosTex0Tex1Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	19:
					VertexTypes.WriteVPosTex0Tex1Tex2Col0(bw, mVerts, mNumVerts);
					break;
				case	20:
					VertexTypes.WriteVPosTex0Tex1Tex2Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	21:
					VertexTypes.WriteVPosTex0Tex1Tex2Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	22:
					VertexTypes.WriteVPosTex0Tex1Tex2Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	23:
					VertexTypes.WriteVPosTex0Tex1Tex2Tex3Col0(bw, mVerts, mNumVerts);
					break;
				case	24:
					VertexTypes.WriteVPosTex0Tex1Tex2Tex3Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	25:
					VertexTypes.WriteVPosTex0Tex1Tex2Tex3Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	26:
					VertexTypes.WriteVPosTex0Tex1Tex2Tex3Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	27:
					VertexTypes.WriteVPosBoneTex0(bw, mVerts, mNumVerts);
					break;
				case	28:
					VertexTypes.WriteVPosBoneTex0Tex1(bw, mVerts, mNumVerts);
					break;
				case	29:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2(bw, mVerts, mNumVerts);
					break;
				case	30:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Tex3(bw, mVerts, mNumVerts);
					break;
				case	31:
					VertexTypes.WriteVPosBoneCol0(bw, mVerts, mNumVerts);
					break;
				case	32:
					VertexTypes.WriteVPosBoneCol0Col1(bw, mVerts, mNumVerts);
					break;
				case	33:
					VertexTypes.WriteVPosBoneCol0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	34:
					VertexTypes.WriteVPosBoneCol0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	35:
					VertexTypes.WriteVPosBoneTex0Col0(bw, mVerts, mNumVerts);
					break;
				case	36:
					VertexTypes.WriteVPosBoneTex0Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	37:
					VertexTypes.WriteVPosBoneTex0Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	38:
					VertexTypes.WriteVPosBoneTex0Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	39:
					VertexTypes.WriteVPosBoneTex0Tex1Col0(bw, mVerts, mNumVerts);
					break;
				case	40:
					VertexTypes.WriteVPosBoneTex0Tex1Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	41:
					VertexTypes.WriteVPosBoneTex0Tex1Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	42:
					VertexTypes.WriteVPosBoneTex0Tex1Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	43:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Col0(bw, mVerts, mNumVerts);
					break;
				case	44:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	45:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	46:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	47:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Tex3Col0(bw, mVerts, mNumVerts);
					break;
				case	48:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	49:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2(bw, mVerts, mNumVerts);					
					break;
				case	50:
					VertexTypes.WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	51:
					VertexTypes.WriteVPosNormTex0(bw, mVerts, mNumVerts);
					break;
				case	52:
					VertexTypes.WriteVPosNormTex0Tex1(bw, mVerts, mNumVerts);
					break;
				case	53:
					VertexTypes.WriteVPosNormTex0Tex1Tex2(bw, mVerts, mNumVerts);
					break;
				case	54:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Tex3(bw, mVerts, mNumVerts);
					break;
				case	55:
					VertexTypes.WriteVPosNormCol0(bw, mVerts, mNumVerts);
					break;
				case	56:
					VertexTypes.WriteVPosNormCol0Col1(bw, mVerts, mNumVerts);
					break;
				case	57:
					VertexTypes.WriteVPosNormCol0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	58:
					VertexTypes.WriteVPosNormCol0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	59:
					VertexTypes.WriteVPosNormTex0Col0(bw, mVerts, mNumVerts);
					break;
				case	60:
					VertexTypes.WriteVPosNormTex0Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	61:
					VertexTypes.WriteVPosNormTex0Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	62:
					VertexTypes.WriteVPosNormTex0Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	63:
					VertexTypes.WriteVPosNormTex0Tex1Col0(bw, mVerts, mNumVerts);
					break;
				case	64:
					VertexTypes.WriteVPosNormTex0Tex1Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	65:
					VertexTypes.WriteVPosNormTex0Tex1Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	66:
					VertexTypes.WriteVPosNormTex0Tex1Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	67:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Col0(bw, mVerts, mNumVerts);
					break;
				case	68:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	69:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	70:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	71:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Tex3Col0(bw, mVerts, mNumVerts);
					break;
				case	72:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Tex3Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	73:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Tex3Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	74:
					VertexTypes.WriteVPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	75:
					VertexTypes.WriteVPosNormBoneTex0(bw, mVerts, mNumVerts);
					break;
				case	76:
					VertexTypes.WriteVPosNormBoneTex0Tex1(bw, mVerts, mNumVerts);
					break;
				case	77:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2(bw, mVerts, mNumVerts);
					break;
				case	78:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Tex3(bw, mVerts, mNumVerts);
					break;
				case	79:
					VertexTypes.WriteVPosNormBoneCol0(bw, mVerts, mNumVerts);
					break;
				case	80:
					VertexTypes.WriteVPosNormBoneCol0Col1(bw, mVerts, mNumVerts);
					break;
				case	81:
					VertexTypes.WriteVPosNormBoneCol0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	82:
					VertexTypes.WriteVPosNormBoneCol0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	83:
					VertexTypes.WriteVPosNormBoneTex0Col0(bw, mVerts, mNumVerts);
					break;
				case	84:
					VertexTypes.WriteVPosNormBoneTex0Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	85:
					VertexTypes.WriteVPosNormBoneTex0Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	86:
					VertexTypes.WriteVPosNormBoneTex0Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	87:
					VertexTypes.WriteVPosNormBoneTex0Tex1Col0(bw, mVerts, mNumVerts);
					break;
				case	88:
					VertexTypes.WriteVPosNormBoneTex0Tex1Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	89:
					VertexTypes.WriteVPosNormBoneTex0Tex1Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	90:
					VertexTypes.WriteVPosNormBoneTex0Tex1Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	91:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Col0(bw, mVerts, mNumVerts);
					break;
				case	92:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	93:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	94:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	95:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Tex3Col0(bw, mVerts, mNumVerts);
					break;
				case	96:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1(bw, mVerts, mNumVerts);
					break;
				case	97:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2(bw, mVerts, mNumVerts);
					break;
				case	98:
					VertexTypes.WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(bw, mVerts, mNumVerts);
					break;
				case	99:
					VertexTypes.WriteVPosNormBone(bw, mVerts, mNumVerts);
					break;
			}
		}


		private void ReadVerts(BinaryReader br, GraphicsDevice gd)
		{
			switch(mTypeIndex)
			{
				case	0:
					VertexTypes.ReadVPos(gd, br, out mVerts, out mNumVerts);
					break;
				case	1:
					VertexTypes.ReadVPosNorm(gd, br, out mVerts, out mNumVerts);
					break;
				case	2:
					VertexTypes.ReadVPosBone(gd, br, out mVerts, out mNumVerts);
					break;
				case	3:
					VertexTypes.ReadVPosTex0(gd, br, out mVerts, out mNumVerts);
					break;
				case	4:
					VertexTypes.ReadVPosTex0Tex1(gd, br, out mVerts, out mNumVerts);
					break;
				case	5:
					VertexTypes.ReadVPosTex0Tex1Tex2(gd, br, out mVerts, out mNumVerts);
					break;
				case	6:
					VertexTypes.ReadVPosTex0Tex1Tex2Tex3(gd, br, out mVerts, out mNumVerts);
					break;
				case	7:
					VertexTypes.ReadVPosCol0(gd, br, out mVerts, out mNumVerts);
					break;
				case	8:
					VertexTypes.ReadVPosCol0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	9:
					VertexTypes.ReadVPosCol0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	10:
					VertexTypes.ReadVPosCol0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	11:
					VertexTypes.ReadVPosTex0Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	12:
					VertexTypes.ReadVPosTex0Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	13:
					VertexTypes.ReadVPosTex0Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	14:
					VertexTypes.ReadVPosTex0Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	15:
					VertexTypes.ReadVPosTex0Tex1Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	16:
					VertexTypes.ReadVPosTex0Tex1Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	17:
					VertexTypes.ReadVPosTex0Tex1Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	18:
					VertexTypes.ReadVPosTex0Tex1Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	19:
					VertexTypes.ReadVPosTex0Tex1Tex2Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	20:
					VertexTypes.ReadVPosTex0Tex1Tex2Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	21:
					VertexTypes.ReadVPosTex0Tex1Tex2Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	22:
					VertexTypes.ReadVPosTex0Tex1Tex2Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	23:
					VertexTypes.ReadVPosTex0Tex1Tex2Tex3Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	24:
					VertexTypes.ReadVPosTex0Tex1Tex2Tex3Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	25:
					VertexTypes.ReadVPosTex0Tex1Tex2Tex3Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	26:
					VertexTypes.ReadVPosTex0Tex1Tex2Tex3Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	27:
					VertexTypes.ReadVPosBoneTex0(gd, br, out mVerts, out mNumVerts);
					break;
				case	28:
					VertexTypes.ReadVPosBoneTex0Tex1(gd, br, out mVerts, out mNumVerts);
					break;
				case	29:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2(gd, br, out mVerts, out mNumVerts);
					break;
				case	30:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Tex3(gd, br, out mVerts, out mNumVerts);
					break;
				case	31:
					VertexTypes.ReadVPosBoneCol0(gd, br, out mVerts, out mNumVerts);
					break;
				case	32:
					VertexTypes.ReadVPosBoneCol0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	33:
					VertexTypes.ReadVPosBoneCol0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	34:
					VertexTypes.ReadVPosBoneCol0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	35:
					VertexTypes.ReadVPosBoneTex0Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	36:
					VertexTypes.ReadVPosBoneTex0Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	37:
					VertexTypes.ReadVPosBoneTex0Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	38:
					VertexTypes.ReadVPosBoneTex0Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	39:
					VertexTypes.ReadVPosBoneTex0Tex1Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	40:
					VertexTypes.ReadVPosBoneTex0Tex1Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	41:
					VertexTypes.ReadVPosBoneTex0Tex1Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	42:
					VertexTypes.ReadVPosBoneTex0Tex1Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	43:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	44:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	45:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	46:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	47:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Tex3Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	48:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	49:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2(gd, br, out mVerts, out mNumVerts);					
					break;
				case	50:
					VertexTypes.ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	51:
					VertexTypes.ReadVPosNormTex0(gd, br, out mVerts, out mNumVerts);
					break;
				case	52:
					VertexTypes.ReadVPosNormTex0Tex1(gd, br, out mVerts, out mNumVerts);
					break;
				case	53:
					VertexTypes.ReadVPosNormTex0Tex1Tex2(gd, br, out mVerts, out mNumVerts);
					break;
				case	54:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Tex3(gd, br, out mVerts, out mNumVerts);
					break;
				case	55:
					VertexTypes.ReadVPosNormCol0(gd, br, out mVerts, out mNumVerts);
					break;
				case	56:
					VertexTypes.ReadVPosNormCol0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	57:
					VertexTypes.ReadVPosNormCol0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	58:
					VertexTypes.ReadVPosNormCol0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	59:
					VertexTypes.ReadVPosNormTex0Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	60:
					VertexTypes.ReadVPosNormTex0Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	61:
					VertexTypes.ReadVPosNormTex0Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	62:
					VertexTypes.ReadVPosNormTex0Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	63:
					VertexTypes.ReadVPosNormTex0Tex1Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	64:
					VertexTypes.ReadVPosNormTex0Tex1Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	65:
					VertexTypes.ReadVPosNormTex0Tex1Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	66:
					VertexTypes.ReadVPosNormTex0Tex1Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	67:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	68:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	69:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	70:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	71:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Tex3Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	72:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Tex3Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	73:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Tex3Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	74:
					VertexTypes.ReadVPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	75:
					VertexTypes.ReadVPosNormBoneTex0(gd, br, out mVerts, out mNumVerts);
					break;
				case	76:
					VertexTypes.ReadVPosNormBoneTex0Tex1(gd, br, out mVerts, out mNumVerts);
					break;
				case	77:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2(gd, br, out mVerts, out mNumVerts);
					break;
				case	78:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Tex3(gd, br, out mVerts, out mNumVerts);
					break;
				case	79:
					VertexTypes.ReadVPosNormBoneCol0(gd, br, out mVerts, out mNumVerts);
					break;
				case	80:
					VertexTypes.ReadVPosNormBoneCol0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	81:
					VertexTypes.ReadVPosNormBoneCol0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	82:
					VertexTypes.ReadVPosNormBoneCol0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	83:
					VertexTypes.ReadVPosNormBoneTex0Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	84:
					VertexTypes.ReadVPosNormBoneTex0Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	85:
					VertexTypes.ReadVPosNormBoneTex0Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	86:
					VertexTypes.ReadVPosNormBoneTex0Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	87:
					VertexTypes.ReadVPosNormBoneTex0Tex1Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	88:
					VertexTypes.ReadVPosNormBoneTex0Tex1Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	89:
					VertexTypes.ReadVPosNormBoneTex0Tex1Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	90:
					VertexTypes.ReadVPosNormBoneTex0Tex1Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	91:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	92:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	93:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	94:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	95:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Tex3Col0(gd, br, out mVerts, out mNumVerts);
					break;
				case	96:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1(gd, br, out mVerts, out mNumVerts);
					break;
				case	97:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2(gd, br, out mVerts, out mNumVerts);
					break;
				case	98:
					VertexTypes.ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(gd, br, out mVerts, out mNumVerts);
					break;
				case	99:
					VertexTypes.ReadVPosNormBone(gd, br, out mVerts, out mNumVerts);
					break;
			}
		}
	}
}