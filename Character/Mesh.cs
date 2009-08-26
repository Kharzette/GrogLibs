﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class Mesh
	{
		string						mName;
		public VertexBuffer			mVerts;
		public IndexBuffer			mIndexs;
		public VertexDeclaration	mVD;
		public Matrix				[]mBones;
		public Matrix				mBindShapeMatrix;
		public int					mNumVerts, mNumTriangles, mVertSize;
		public string				mMaterialName;
		public string				mGeometryID;	//for mapping geoms to skins
		public bool					mSkinned;		//true if using skinning


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


		public void SetGeometryID(string id)
		{
			mGeometryID	=id;
		}


		//copies bones into the shader
		public void UpdateBones(Effect fx)
		{
			//some chunks are never really drawn
			if(mBones != null)
			{
				fx.Parameters["mBones"].SetValue(mBones);
			}
		}


		public void Draw(GraphicsDevice g, MaterialLib matLib)
		{
			g.Vertices[0].SetSource(mVerts, 0, mVertSize);
			g.Indices			=mIndexs;
			g.VertexDeclaration	=mVD;

			Matrix	loc	=Matrix.Identity;
			/*
			Texture2D	tex	=matLib.GetMaterialTexture(mMaterialName);
			if(tex == null)
			{
				return;	//no material?
			}*/

			Effect	fx	=matLib.GetMaterialShader(mMaterialName);

			if(fx == null)
			{
				return;
			}
			//fx.Parameters["mTexture0"].SetValue(tex);

			UpdateBones(fx);

			//this might get slow
			matLib.ApplyParameters(mMaterialName);

			if(fx.Parameters["mBindPose"] != null)
			{
				fx.Parameters["mBindPose"].SetValue(mBindShapeMatrix);
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