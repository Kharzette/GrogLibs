using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace UtilityLib
{
	public class GameCamera
	{
		//mats
		protected Matrix	mMATWorld;
		protected Matrix	mMATView;
		protected Matrix	mMATViewTranspose;
		protected Matrix	mMATProjection;

		BoundingFrustum	mFrust	=new BoundingFrustum(Matrix.Identity);

		protected float		mAspect, mWidth, mHeight;
		protected float		mNearClip, mFarClip;


		public GameCamera(float width, float height, float aspect, float near, float far)
		{
			mWidth		=width;
			mHeight		=height;
			mAspect		=aspect;
			mNearClip	=near;
			mFarClip	=far;

			InitializeMats();
		}


		public Matrix World
		{
			get { return mMATWorld; }
			set { mMATWorld = value; }
		}

		public Matrix View
		{
			get { return mMATView; }
			set { mMATView = value; }
		}

		public Matrix ViewTranspose
		{
			get { return mMATViewTranspose; }
			set { mMATViewTranspose = value; }
		}

		public Matrix Projection
		{
			get { return mMATProjection; }
			set { mMATProjection = value; }
		}


		public void Update(float msDelta, PlayerSteering ps)
		{
			UpdateMatrices(ps.Position, ps.Pitch, ps.Yaw, ps.Roll);
		}


		public Rectangle GetScreenCoverage(List<Vector3> points)
		{
			Rectangle	rect	=new Rectangle();

			rect.X		=5000;
			rect.Y		=5000;
			rect.Width	=-5000;
			rect.Height	=-5000;

			foreach(Vector3 pnt in points)
			{
				Vector3	transformed	=Vector3.Transform(pnt, mFrust.Matrix);

				if(transformed.X < rect.X)
				{
					rect.X	=(int)transformed.X;
				}
				if(transformed.X > rect.Width)
				{
					rect.Width	=(int)transformed.X;
				}
				if(transformed.Y < rect.Y)
				{
					rect.Y	=(int)transformed.Y;
				}
				if(transformed.Y > rect.Height)
				{
					rect.Height	=(int)transformed.Y;
				}
			}

			rect.Width	-=rect.X;
			rect.Height	-=rect.Y;

			return	rect;
		}


		public bool IsBoxOnScreen(BoundingBox box)
		{
			return	mFrust.Intersects(box);
		}


		public bool IsPointOnScreen(Vector3 point)
		{
			ContainmentType	ct	=mFrust.Contains(point);

			if(ct == ContainmentType.Contains)
			{
				return	true;
			}
			return	false;
		}


		public void UpdateMatrices(Vector3 camPos, float pitch, float yaw, float roll)
		{
			mMATView	=Matrix.CreateTranslation(camPos) *
				Matrix.CreateRotationY(MathHelper.ToRadians(yaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(pitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(roll));
			
			mFrust.Matrix	=mMATWorld * mMATView * mMATProjection;
			
			Matrix.Transpose(ref mMATView, out mMATViewTranspose);
		}


		void InitializeMats()
		{
			mMATWorld	=Matrix.CreateTranslation(Vector3.Zero);
			mMATView	=Matrix.CreateTranslation(Vector3.Zero);

			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				mWidth / mHeight, mNearClip, mFarClip);
		}
	}
}