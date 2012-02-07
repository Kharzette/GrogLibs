using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace TerrainLib
{
	public class CloudPlane
	{
		VertexBuffer	mVB;
		IndexBuffer		mIB;
		Effect			mCloudFX;

		Matrix	mWorld;

		const float PlaneSize	=132000.0f;
		const float	TexSpeed0	=0.03f;
		const float	TexSpeed1	=0.06f;
		const float	TexSpeed2	=0.13f;
		const float	TexSpeed3	=0.15f;

		float	mTexOffset0;
		float	mTexOffset1;
		float	mTexOffset2;
		float	mTexOffset3;


		public CloudPlane(GraphicsDevice gd, Effect cloudFX, int resX, int resY,
			Texture2D c1, Texture2D c2, Texture2D c3, Texture2D c4,
			int thickness, float thickDist, Matrix proj)
		{
			mCloudFX	=cloudFX;

			//create cloud planes
			VertexPositionColor	[]vpc	=new VertexPositionColor[4 * thickness];

			for(int i=0;i < thickness;i++)
			{
				vpc[0 + (i * 4)].Position	=Vector3.UnitX * PlaneSize + Vector3.UnitZ * PlaneSize;
				vpc[1 + (i * 4)].Position	=-Vector3.UnitX * PlaneSize + Vector3.UnitZ * PlaneSize;
				vpc[2 + (i * 4)].Position	=Vector3.UnitX * PlaneSize - Vector3.UnitZ * PlaneSize;
				vpc[3 + (i * 4)].Position	=-Vector3.UnitX * PlaneSize - Vector3.UnitZ * PlaneSize;

				//vertical
				vpc[0 + (i * 4)].Position	+=Vector3.UnitY * thickDist * i;
			}

			mVB	=new VertexBuffer(gd, typeof(VertexPositionColor), 4 * thickness, BufferUsage.WriteOnly);

			mVB.SetData<VertexPositionColor>(vpc);

			UInt16	[]indexes	=new UInt16[6 * thickness];

			for(int i=0;i < thickness;i++)
			{
				indexes[(i * 6) + 0]	=(UInt16)((i * 4) + 2);
				indexes[(i * 6) + 1]	=(UInt16)((i * 4) + 0);
				indexes[(i * 6) + 2]	=(UInt16)((i * 4) + 1);
				indexes[(i * 6) + 3]	=(UInt16)((i * 4) + 3);
				indexes[(i * 6) + 4]	=(UInt16)((i * 4) + 2);
				indexes[(i * 6) + 5]	=(UInt16)((i * 4) + 1);
			}

			mWorld	=Matrix.CreateTranslation(Vector3.UnitY * -thickDist * (thickness / 2));

			mIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits, 6 * thickness, BufferUsage.WriteOnly);

			mIB.SetData<UInt16>(indexes);

			mCloudFX.Parameters["mTexture0"].SetValue(c1);
			mCloudFX.Parameters["mTexture1"].SetValue(c2);
			mCloudFX.Parameters["mTexture2"].SetValue(c3);
			mCloudFX.Parameters["mTexture3"].SetValue(c4);

			mCloudFX.Parameters["mTexFactor0"].SetValue(500.0f);
			mCloudFX.Parameters["mTexFactor1"].SetValue(1000.0f);
			mCloudFX.Parameters["mTexFactor2"].SetValue(1500.0f);
			mCloudFX.Parameters["mTexFactor3"].SetValue(2000.0f);

			mCloudFX.Parameters["mDistThreshold"].SetValue(0.5f);
			mCloudFX.Parameters["mFallOff"].SetValue(5.5f);

			mCloudFX.Parameters["mProjection"].SetValue(proj);
			mCloudFX.Parameters["mCamRange"].SetValue(8000.0f);
			mCloudFX.Parameters["mInvViewPort"].SetValue(
				Vector2.One / (Vector2.UnitX * resX + Vector2.UnitY * resY));

			Vector4	cloudColour	=Color.White.ToVector4();
			cloudColour.W		=0.9f;

			mCloudFX.Parameters["mCloudColour"].SetValue(cloudColour);
		}


		public void Update(float msDelta, float height, float distThresh, float fallOff)
		{
			float	secDelta	=msDelta / 1000.0f;

			mTexOffset0	+=secDelta * TexSpeed0;
			mTexOffset1	+=secDelta * TexSpeed1;
			mTexOffset2	+=secDelta * TexSpeed2;
			mTexOffset3	+=secDelta * TexSpeed3;

			mWorld	=Matrix.CreateTranslation(Vector3.UnitY * height);

			mCloudFX.Parameters["mTexOffset0"].SetValue(mTexOffset0);
			mCloudFX.Parameters["mTexOffset1"].SetValue(mTexOffset1);
			mCloudFX.Parameters["mTexOffset2"].SetValue(mTexOffset2);
			mCloudFX.Parameters["mTexOffset3"].SetValue(mTexOffset3);

			mCloudFX.Parameters["mDistThreshold"].SetValue(distThresh);
			mCloudFX.Parameters["mFallOff"].SetValue(fallOff);
		}


		public void Draw(GraphicsDevice gd, Matrix cam, RenderTarget2D depthPass)
		{
			gd.SetVertexBuffer(mVB);
			gd.Indices	=mIB;

			mCloudFX.Parameters["mWorld"].SetValue(mWorld);
			mCloudFX.Parameters["mView"].SetValue(cam);
			mCloudFX.Parameters["mDepthTex"].SetValue(depthPass);

			mCloudFX.CurrentTechnique.Passes[0].Apply();

			gd.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0, mVB.VertexCount, 0, mVB.VertexCount / 2);
		}
	}
}
