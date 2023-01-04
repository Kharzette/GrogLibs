using System;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice;
using System.Numerics;


namespace UtilityLib
{
	public class GameCamera
	{
		//mats
		protected Matrix4x4	mView;
		protected Matrix4x4	mProjection, mViewProj;

		BoundingFrustum	mFrust	=new BoundingFrustum(Matrix4x4.Identity);

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


		public Matrix4x4 View
		{
			get { return mView; }
			set { mView = value; }
		}

		public Matrix4x4 ViewTransposed
		{
			get { return Matrix4x4.Transpose(mView); }
		}

		public Matrix4x4 Projection
		{
			get { return mProjection; }
			set { mProjection = value; }
		}

		public Matrix4x4 ProjectionTransposed
		{
			get { return Matrix4x4.Transpose(mProjection); }
		}

		//returns invert equiv for a worldspace matrix style
		public Vector3 Forward
		{
			get
			{
				Vector3	ret	=Vector3.Zero;
				ret.X		=View.M13;
				ret.Y		=View.M23;
				ret.Z		=View.M33;
				return		-ret;
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
				return		-ret;
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
				return		-ret;
			}
		}

		public Vector3 Position
		{
			get
			{
				return	-mView.Translation;
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
			Matrix4x4	yawMat	=Matrix4x4.CreateRotationY(MathHelper.ToRadians(yaw));
			Vector3		yawVec	=Vector3.TransformNormal(Vector3.UnitX, yawMat);
			Vector3		aimVec	=yawVec - Vector3.UnitY;

			aimVec	=Vector3.Normalize(aimVec);

			Vector3	camPos	=(focusPos + Vector3.UnitY * 32f) - aimVec * povDist;

			mView	=Matrix4x4.CreateLookAt(camPos, focusPos, Vector3.UnitY);

			mViewProj	=mView * mProjection;
		}


		public void UpdateTopDown(Vector3 focusPos, float yaw, float povDist)
		{
			Matrix4x4	yawMat		=Matrix4x4.CreateRotationY(MathHelper.ToRadians(yaw));
			Vector3		yawVec		=Vector3.TransformNormal(Vector3.UnitX, yawMat);
			Vector3		aimVec		=yawVec - Vector3.UnitY;

			aimVec	=Vector3.Normalize(aimVec);

			Vector3	camPos	=focusPos - aimVec * povDist;

			mView		=Matrix4x4.CreateLookAt(camPos, focusPos, Vector3.UnitY);
			mViewProj	=mView * mProjection;
		}


		public void UpdateIntermission(Vector3 camPos, Vector3 camDir)
		{
			mView	=Matrix4x4.CreateLookAt(camPos, camPos + camDir, Vector3.UnitY);

			mViewProj	=mView * mProjection;
		}


		public void Update(Vector3 camPos, float pitch, float yaw, float roll)
		{
			UpdateMatrices(camPos, pitch, yaw, roll);
		}


		public RawRect GetScreenCoverage(List<Vector3> points)
		{
			int	X		=5000;
			int	Y		=5000;
			int	Width	=-5000;
			int	Height	=-5000;

			foreach(Vector3 pnt in points)
			{
				Vector3	transformed	=Vector3.Transform(pnt, mViewProj);

				if(transformed.X < X)
				{
					X	=(int)transformed.X;
				}
				if(transformed.X > Width)
				{
					Width	=(int)transformed.X;
				}
				if(transformed.Y < Y)
				{
					Y	=(int)transformed.Y;
				}
				if(transformed.Y > Height)
				{
					Height	=(int)transformed.Y;
				}
			}

			Width	-=X;
			Height	-=Y;

			return	new RawRect(X, Y, Width, Height);
		}


		public bool IsBoxOnScreen(BoundingBox box)
		{
			return	mFrust.Intersects(box);
		}


		public bool IsPointOnScreen(Vector3 point)
		{
			foreach(Plane p in mFrust.Planes)
			{
				float	side	=p.Normal.dot(point) - p.D;
				if(side > 0f)
				{
					return	false;
				}
			}
			return	true;
		}


		public void UpdateMatrices(Vector3 camPos, float pitch, float yaw, float roll)
		{
			//view mats are inverted world mats, so negate translation
			mView	=Matrix4x4.CreateTranslation(-camPos) *
				Matrix4x4.CreateRotationZ(MathHelper.ToRadians(roll)) *
				Matrix4x4.CreateRotationY(MathHelper.ToRadians(yaw)) *
				Matrix4x4.CreateRotationX(MathHelper.ToRadians(pitch));

			mFrust	=new BoundingFrustum(mView * mProjection);
		}


		void InitializeMats()
		{
			mView		=Matrix4x4.Identity;
			mProjection	=Matrix4x4.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				mWidth / mHeight, mNearClip, mFarClip);
		}
	}
}