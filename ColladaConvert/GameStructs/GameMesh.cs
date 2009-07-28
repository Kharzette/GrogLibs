using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GameMesh
	{
		public VertexBuffer			mVerts;
		public IndexBuffer			mIndexs;
		public VertexDeclaration	mVD;
		public Matrix				[]mBones;
		public Matrix				mBindShapeMatrix;
		public int					mNumVerts, mNumTriangles, mVertSize;
		public string				mMaterialName;
		public string				mGeometryID;	//for mapping geoms to skins
		public bool					mSkinned;		//true if using skinning


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

			//hard code material name for now
			mMaterialName	="desu.png";

			//grab material
			GameMaterial	gm	=matLib.GetMaterial(mMaterialName);
			if(gm == null)
			{
				return;	//no material?
			}

//			Effect	fx	=matLib.GetShader(gm.mShaderName);

			//hard code this for now
			Effect	fx	=matLib.GetShader("Shaders\\VPosNormBoneTex0Tex1Col0");

			for(int map=0;map < gm.mMaps.Count;map++)
			{
				string	tex	="mTexture" + map;
				fx.Parameters[tex].SetValue(gm.mMaps[map]);

				UpdateBones(fx);

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
}