using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace MaterialLib
{
	public class PostProcess
	{
		//targets for doing postery
		Dictionary<string, RenderTarget2D>	mPostTargets	=new Dictionary<string, RenderTarget2D>();

		//for a fullscreen quad
		VertexBuffer	mQuadVB;
		IndexBuffer		mQuadIB;

		//effect file that has most of the post stuff in it
		Effect	mPostFX;

		//stuff
		int	mResX, mResY;


		public PostProcess(GraphicsDevice gd, ContentManager slib, int resx, int resy)
		{
			mPostFX	=slib.Load<Effect>("Shaders/Post");

			mResX	=resx;
			mResY	=resy;

			MakeQuad(gd);

			InitPostParams();
		}


		public void MakePostTarget(GraphicsDevice gd, string name,
			int resx, int resy, SurfaceFormat surf, DepthFormat depth)
		{
			RenderTarget2D	rend	=new RenderTarget2D(gd, resx, resy, false, surf, depth);

			mPostTargets.Add(name, rend);
		}


		//set up parameters with known values
		void InitPostParams()
		{
			mPostFX.Parameters["mTexelSteps"].SetValue(1.0f);
			mPostFX.Parameters["mThreshold"].SetValue(0.2f);
			mPostFX.Parameters["mScreenSize"].SetValue(new Vector2(mResX, mResY));
			mPostFX.Parameters["mOpacity"].SetValue(0.75f);
		}


		void MakeQuad(GraphicsDevice gd)
		{
			VertexPositionTexture	[]verts	=new VertexPositionTexture[4];

			verts[0].Position = new Vector3(-1, 1, 1);
			verts[1].Position = new Vector3(1, 1, 1);
			verts[2].Position = new Vector3(-1, -1, 1);
			verts[3].Position = new Vector3(1, -1, 1);
			
			verts[0].TextureCoordinate = new Vector2(0, 0);
			verts[1].TextureCoordinate = new Vector2(1, 0);
			verts[2].TextureCoordinate = new Vector2(0, 1);
			verts[3].TextureCoordinate = new Vector2(1, 1);

			mQuadVB	=new VertexBuffer(gd, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
			mQuadVB.SetData(verts);

			mQuadIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);

			UInt16	[]inds	=new UInt16[6];
			inds[0]	=0;
			inds[1]	=1;
			inds[2]	=2;
			inds[3]	=1;
			inds[4]	=3;
			inds[5]	=2;

			mQuadIB.SetData(inds);
		}


		public void SetTarget(GraphicsDevice gd, string targName, bool bClear)
		{
			if(targName == "null")
			{
				gd.SetRenderTarget(null);
			}
			else if(!mPostTargets.ContainsKey(targName))
			{
				return;
			}
			else
			{
				gd.SetRenderTarget(mPostTargets[targName]);
			}

			if(bClear)
			{
				gd.Clear(Color.CornflowerBlue);
			}
		}


		public void SetParameter(string paramName, string targName)
		{
			mPostFX.Parameters[paramName].SetValue(mPostTargets[targName]);
		}


		public void DrawStage(GraphicsDevice gd, string technique)
		{
			gd.SetVertexBuffer(mQuadVB);
			gd.Indices	=mQuadIB;

			mPostFX.CurrentTechnique	=mPostFX.Techniques[technique];

			mPostFX.CurrentTechnique.Passes[0].Apply();

			gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
		}
	}
}
