using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace UtilityLib
{
	class RenderThingy
	{
		//transform info
		internal Matrix	mWorld, mView;

		//verts
		internal VertexPositionColor	[]mVerts;
	}


	public class RenderHelper
	{
		Matrix			mProj;
		BasicEffect		mBFX;
		GraphicsDevice	mGD;
		Random			mRand	=new Random();

		List<RenderThingy>	mThingies	=new List<RenderThingy>();

		//every rect has the same index list
		int	[]mRectIndexData	=new int[8];


		public RenderHelper(GraphicsDevice gd, int viewWidth, int viewHeight)
		{
			mGD		=gd;
			mProj	=Matrix.CreateOrthographicOffCenter(0,
						-viewWidth,	viewHeight,	0, 0.1f, 100.0f);

			mBFX					=new BasicEffect(gd);
			mBFX.Projection			=mProj;
			mBFX.VertexColorEnabled	=true;

			//make index
			mRectIndexData[0]	=0;
			mRectIndexData[1]	=1;
			mRectIndexData[2]	=1;
			mRectIndexData[3]	=2;
			mRectIndexData[4]	=2;
			mRectIndexData[5]	=3;
			mRectIndexData[6]	=3;
			mRectIndexData[7]	=0;
		}


		public void DrawRect(Rectangle rect, Vector2 pos, Vector2 org, Vector2 scale, float rot)
		{
			RenderThingy	rend	=new RenderThingy();

			rend.mView	=Matrix.CreateLookAt(Vector3.UnitY * 30.0f,
								Vector3.Zero, Vector3.UnitZ);
			rend.mView	*=Matrix.CreateRotationZ(-rot);
			rend.mView	*=Matrix.CreateTranslation(-pos.X, pos.Y, 0.0f);

			rend.mWorld	=Matrix.Identity;
			rend.mWorld	*=Matrix.CreateTranslation(-org.X, 0.0f, -org.Y);
			rend.mWorld	*=Matrix.CreateScale(scale.X, 1.0f, scale.Y);

			rend.mVerts	=GetVerts(rect);

			mThingies.Add(rend);
		}


		public void DrawAll()
		{
			foreach(RenderThingy rend in mThingies)
			{
				mBFX.World	=rend.mWorld;
				mBFX.View	=rend.mView;

				mBFX.CurrentTechnique.Passes[0].Apply();

				mGD.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.LineList, rend.mVerts, 0, rend.mVerts.Length, mRectIndexData, 0, 4);
			}

			mThingies.Clear();
		}


		VertexPositionColor	[]GetVerts(Rectangle rect)
		{
			VertexPositionColor	[]ret	=new VertexPositionColor[4];

			//top left
			ret[0].Position.X	=rect.X;
			ret[0].Position.Z	=rect.Y;

			//top right
			ret[1].Position.X	=rect.X + rect.Width;
			ret[1].Position.Z	=rect.Y;

			//bottom right
			ret[2].Position.X	=rect.X + rect.Width;
			ret[2].Position.Z	=rect.Y + rect.Height;

			//bottom left
			ret[3].Position.X	=rect.X;
			ret[3].Position.Z	=rect.Y + rect.Height;

			ret[0].Color	=UtilityLib.Mathery.RandomColor(mRand);
			ret[1].Color	=UtilityLib.Mathery.RandomColor(mRand);
			ret[2].Color	=UtilityLib.Mathery.RandomColor(mRand);
			ret[3].Color	=UtilityLib.Mathery.RandomColor(mRand);

			return	ret;
		}
	}
}
