using System;
using System.Collections.Generic;
using System.Xml;
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
		bool				mSkinned;		//true if using skinning
		Skin				mSkin;


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


		public Mesh(string name)
		{
			mName			=name;
			mMaterialName	="";
		}


		public void SetSkin(Skin sk)
		{
			mSkin	=sk;
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
	}
}