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

		protected Vector3	mCamPos;
		protected float		mPitch, mYaw, mRoll;
		protected float		mAspect, mWidth, mHeight;
		protected float		mNearClip, mFarClip;
		protected float		mCamSpeed	=0.1f;

		KeyboardState	mLastKBS	=new KeyboardState();
		MouseState		mLastMS		=new MouseState();
		GamePadState	mLastGS		=new GamePadState();
		MouseState		mOriginalMS;

		const float	MouseSensitivity	=0.01f;
		const float GamePadSensitivity	=0.25f;


		public GameCamera(float width, float height, float aspect, float near, float far)
		{
			mWidth		=width;
			mHeight		=height;
			mAspect		=aspect;
			mNearClip	=near;
			mFarClip	=far;

			//starting cam will drift a bit
			mYaw	=140.0f;
			mPitch	=-10.0f;
			mRoll	=0.0f;

			//set up mouselook on right button
			Mouse.SetPosition((int)(width / 2), (int)(height / 2));
			mOriginalMS	=Mouse.GetState();

			InitializeMats();
		}


		public float CamSpeed
		{
			get { return mCamSpeed; }
			set { mCamSpeed = value; }
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


		public void Update(float msDelta, KeyboardState ks, MouseState ms, GamePadState gs)
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
				speed	=CamSpeed * msDelta * 2.0f;
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
				Vector2	delta	=Vector2.Zero;
				delta.X	=mOriginalMS.X - ms.X;
				delta.Y	=mOriginalMS.Y - ms.Y;

				Mouse.SetPosition(mOriginalMS.X, mOriginalMS.Y);

				mPitch	-=(delta.Y) * msDelta * MouseSensitivity;
				mYaw	-=(delta.X) * msDelta * MouseSensitivity;
			}

			mPitch	+=gs.ThumbSticks.Right.Y * msDelta * 0.25f;
			mYaw	+=gs.ThumbSticks.Right.X * msDelta * 0.25f;

			mCamPos	-=vleft * (gs.ThumbSticks.Left.X * msDelta * 0.25f);
			mCamPos	+=vin * (gs.ThumbSticks.Left.Y * msDelta * 0.25f);

			mLastKBS	=ks;
			mLastMS		=ms;
			mLastGS		=gs;

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


		public void UpdateMatrices()
		{
			mMATView	=Matrix.CreateTranslation(mCamPos) *
				Matrix.CreateRotationY(MathHelper.ToRadians(mYaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(mPitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(mRoll));
			
			mFrust.Matrix	=mMATWorld * mMATView * mMATProjection;
			
			Matrix.Transpose(ref mMATView, out mMATViewTranspose);
		}


		void InitializeMats()
		{
			mMATWorld	=Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));
			mMATView	=Matrix.CreateTranslation(mCamPos);

			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				mWidth / mHeight, mNearClip, mFarClip);
		}
	}
}
