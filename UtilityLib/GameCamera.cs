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
		Matrix	mMATWorld;
		Matrix	mMATView;
		Matrix	mMATViewTranspose;
		Matrix	mMATProjection;

		BoundingFrustum	mFrust	=new BoundingFrustum(Matrix.Identity);

		Vector3	mCamPos;
		float	mPitch, mYaw, mRoll;
		float	mAspect, mWidth, mHeight;

		KeyboardState	mLastKBS	=new KeyboardState();
		MouseState		mLastMS		=new MouseState();

		const float	CamSpeed			=0.1f;
		const float	MouseSensitivity	=0.01f;


		public GameCamera(float width, float height, float aspect)
		{
			mWidth	=width;
			mHeight	=height;
			mAspect	=aspect;

			//starting cam will drift a bit
			mYaw	=140.0f;
			mPitch	=-10.0f;
			mRoll	=0.0f;

			InitializeMats();
		}


		public Vector3 CamPos
		{
			get { return mCamPos; }
			set { mCamPos = value; }
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

		public Matrix Projection
		{
			get { return mMATProjection; }
			set { mMATProjection = value; }
		}


		public void Update(float msDelta, KeyboardState ks, MouseState ms)
		{
			Vector3 vup		=Vector3.Zero;
			Vector3 vleft	=Vector3.Zero;
			Vector3 vin		=Vector3.Zero;

			//grab view matrix in vector transpose
			vup.X   =mMATView.M12;
			vup.Y   =mMATView.M22;
			vup.Z   =mMATView.M32;
			vleft.X =mMATView.M11;
			vleft.Y =mMATView.M21;
			vleft.Z =mMATView.M31;
			vin.X   =mMATView.M13;
			vin.Y   =mMATView.M23;
			vin.Z   =mMATView.M33;

			float	speed	=0.0f;
			if(ks.IsKeyDown(Keys.RightShift) || ks.IsKeyDown(Keys.LeftShift))
			{
				speed	=CamSpeed * msDelta * 20.0f;
			}
			else
			{
				speed	=CamSpeed * msDelta;
			}
			
			if(ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A))
			{
				mCamPos	+=vleft * speed;
			}
			if(ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D))
			{
				mCamPos	-=vleft * speed;
			}
			if(ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W))
			{
				mCamPos	+=vin * speed;
			}
			if(ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S))
			{
				mCamPos	-=vin * speed;
			}

			if(ms.RightButton == ButtonState.Pressed)
			{
				mPitch	+=(ms.Y - mLastMS.Y) * msDelta * MouseSensitivity;
				mYaw	+=(ms.X - mLastMS.X) * msDelta * MouseSensitivity;
			}

			mLastKBS	=ks;
			mLastMS		=ms;

			UpdateMatrices();
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


		void UpdateMatrices()
		{
			mMATView	=Matrix.CreateTranslation(mCamPos) *
				Matrix.CreateRotationY(MathHelper.ToRadians(mYaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(mPitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(mRoll));
			
			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
				mAspect, 1, 50000);

			mFrust.Matrix	=mMATWorld * mMATView * mMATProjection;
			
			Matrix.Transpose(ref mMATView, out mMATViewTranspose);
		}


		void InitializeMats()
		{
			mMATWorld	=Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));
			mMATView	=Matrix.CreateTranslation(mCamPos);

			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				mWidth / mHeight, 1.0f, 100.0f);
		}
	}
}
