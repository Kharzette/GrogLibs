using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;


namespace UtilityLib
{
	public class GameCamera
	{
		//mats
		protected Matrix	mView, mViewInverse;
		protected Matrix	mProjection;

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
			get { return mView; }
			set { mView = value; mViewInverse = Matrix.Invert(mView); }
		}

		public Matrix ViewInverse
		{
			get { return mViewInverse; }
		}

		public Matrix Projection
		{
			get { return mProjection; }
			set { mProjection = value; }
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
				return		-ret;	//changed from xna
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
				return	mViewInverse.TranslationVector;
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
			Matrix	yawMat	=Matrix.RotationY(MathUtil.DegreesToRadians(yaw));
			Vector3	yawVec	=Vector3.TransformNormal(Vector3.UnitX, yawMat);
			Vector3	aimVec	=yawVec + Vector3.Down;

			aimVec.Normalize();

			Vector3	camPos	=(focusPos + Vector3.Up * 32f) - aimVec * povDist;

			mView			=Matrix.LookAtLH(camPos, focusPos, Vector3.Up);
			mViewInverse	=Matrix.Invert(mView);			
			mFrust.Matrix	=mView * mProjection;
		}


		public void UpdateTopDown(Vector3 focusPos, float yaw, float povDist)
		{
			Matrix	yawMat		=Matrix.RotationY(MathUtil.DegreesToRadians(yaw));
			Vector3	yawVec		=Vector3.TransformNormal(Vector3.UnitX, yawMat);
			Vector3	aimVec		=yawVec + Vector3.Down;

			aimVec.Normalize();

			Vector3	camPos	=focusPos - aimVec * povDist;

			mView			=Matrix.LookAtLH(camPos, focusPos, Vector3.Up);
			mViewInverse	=Matrix.Invert(mView);			
			mFrust.Matrix	=mView * mProjection;
		}


		public void UpdateIntermission(Vector3 camPos, Vector3 camDir)
		{
			mView	=Matrix.LookAtLH(camPos, camPos + camDir, Vector3.Up);

			mViewInverse	=Matrix.Invert(mView);
			
			mFrust.Matrix	=mView * mProjection;
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
				Vector4	transformed	=Vector3.Transform(pnt, mFrust.Matrix);

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
			return	mFrust.Intersects(ref box);
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
			mView	=Matrix.Translation(camPos) *
				Matrix.RotationY(MathUtil.DegreesToRadians(yaw)) *
				Matrix.RotationX(MathUtil.DegreesToRadians(pitch)) *
				Matrix.RotationZ(MathUtil.DegreesToRadians(roll));

			mViewInverse	=Matrix.Invert(mView);
			
			mFrust.Matrix	=mView * mProjection;
		}


		void InitializeMats()
		{
			mView			=Matrix.Translation(Vector3.Zero);
			mViewInverse	=Matrix.Invert(mView);

			mProjection	=Matrix.PerspectiveFovLH(
				MathUtil.DegreesToRadians(45),
				mWidth / mHeight, mNearClip, mFarClip);
		}
	}
}