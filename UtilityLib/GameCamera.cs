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
		protected Matrix	mMATView, mMatViewInverse;
		protected Matrix	mMATProjection;

		BoundingFrustum	mFrust	=new BoundingFrustum(Matrix.Identity);

		//camera settings
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


		public Matrix View
		{
			get { return mMATView; }
			set { mMATView = value; mMatViewInverse = Matrix.Invert(mMATView); }
		}

		public Matrix ViewInverse
		{
			get { return mMatViewInverse; }
		}

		public Matrix Projection
		{
			get { return mMATProjection; }
			set { mMATProjection = value; }
		}

		//returns transposed, useful for worldspace stuff
		public Vector3 Forward
		{
			get
			{
				Vector3	ret	=Vector3.Zero;
				ret.X		=View.M13;
				ret.Y		=View.M23;
				ret.Z		=View.M33;
				return		ret;
			}
		}

		public Vector3 Up
		{
			get
			{
				Vector3	ret	=Vector3.Zero;
				ret.X		=View.M12;
				ret.Y		=View.M22;
				ret.Z		=View.M32;
				return		ret;
			}
		}

		public Vector3 Left
		{
			get
			{
				Vector3	ret	=Vector3.Zero;
				ret.X		=View.M11;
				ret.Y		=View.M21;
				ret.Z		=View.M31;
				return		ret;
			}
		}

		public Vector3 Position
		{
			get
			{
				return	mMatViewInverse.Translation;
			}
		}

		public Vector2 InvViewPort
		{
			get
			{
				return	Vector2.One / (Vector2.UnitX * mWidth + Vector2.UnitY * mHeight);
			}
		}

		public float NearClip
		{
			get
			{
				return	mNearClip;
			}
		}

		public float FarClip
		{
			get
			{
				return	mFarClip;
			}
		}


		public void UpdateTwinStick(Vector3 focusPos, float yaw, float povDist)
		{
			Matrix	yawMat	=Matrix.CreateRotationY(MathHelper.ToRadians(yaw));
			Vector3	yawVec	=Vector3.TransformNormal(Vector3.UnitX, yawMat);
			Vector3	aimVec	=yawVec + Vector3.Down;

			aimVec.Normalize();

			Vector3	camPos	=(focusPos + Vector3.Up * 32f) - aimVec * povDist;

			mMATView		=Matrix.CreateLookAt(camPos, focusPos, Vector3.Up);
			mMatViewInverse	=Matrix.Invert(mMATView);			
			mFrust.Matrix	=mMATView * mMATProjection;
		}


		public void Update(Vector3 camPos, float pitch, float yaw, float roll)
		{
			UpdateMatrices(camPos, pitch, yaw, roll);
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

			mMatViewInverse	=Matrix.Invert(mMATView);
			
			mFrust.Matrix	=mMATView * mMATProjection;
		}


		void InitializeMats()
		{
			mMATView		=Matrix.CreateTranslation(Vector3.Zero);
			mMatViewInverse	=Matrix.Invert(mMATView);

			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				mWidth / mHeight, mNearClip, mFarClip);
		}
	}
}