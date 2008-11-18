using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Terrain
{
	public class Flyer
	{
		//the graphical model
		private	Model	mModel;

		//vital statistics
		private	Vector3	mPosition;
		private	Vector3	mVelocity;
		private	float	mPitch;
		private	float	mYaw;
		private	float	mRoll;
		private	Matrix	mMat, mScale;
		private	int		mHealth;

		//effect for this flyer
		//should just be a "pointer"
		private	Effect	mFX;


		public Flyer(string assName, ContentManager cm, Effect fx)
		{
			mModel	=cm.Load<Model>(assName);
			mFX		=fx;

			//init to some defaults
			mPosition	=Vector3.One * 100.0f;
			mVelocity	=Vector3.Zero;
			mPitch		=0.0f;
			mYaw		=0.0f;
			mRoll		=0.0f;
			mMat		=Matrix.Identity;
			mScale		=Matrix.CreateScale(0.01f);
			mHealth		=100;
		}


		public void Draw(GraphicsDevice gd)
		{
			Matrix	[]transforms	=new Matrix[mModel.Bones.Count];
			
			mModel.CopyAbsoluteBoneTransformsTo(transforms);
			
			foreach(ModelMesh mesh in mModel.Meshes)
			{
				foreach(BasicEffect effect in mesh.Effects)
				{
					effect.EnableDefaultLighting();
					effect.World	*=transforms[mesh.ParentBone.Index]
						* mMat * mScale;
				}
				mesh.Draw();
			}
		}


		public void Update(GameTime gameTime, KeyboardState kbs)
		{
			/*
			float	time	=(float)gameTime.ElapsedGameTime.TotalMilliseconds;
			
			Vector3	vup;
			Vector3	vleft;
			Vector3	vin;

			//grab ship vectors from matrix transpose
			Matrix	matTrans;
			Matrix.Transpose(ref mMat, out matTrans);

			vup.X	=matTrans.M12;
			vup.Y	=matTrans.M22;
			vup.Z	=matTrans.M32;
			vleft.X	=matTrans.M11;
			vleft.Y	=matTrans.M21;
			vleft.Z	=matTrans.M31;
			vin.X	=matTrans.M13;
			vin.Y	=matTrans.M23;
			vin.Z	=matTrans.M33;


			if(kbs.IsKeyDown(Keys.T))
			{
				mPosition	+=vin * (time * 100.1f);
			}

			if(kbs.IsKeyDown(Keys.G))
			{
				mPosition	-=vin * (time * 100.1f);
			}

			if(kbs.IsKeyDown(Keys.F))
			{
				mPosition	+=vleft * (time * 100.1f);
			}

			if(kbs.IsKeyDown(Keys.H))
			{
				mPosition	-=vleft * (time * 100.1f);
			}

			if(kbs.IsKeyDown(Keys.R))
			{
				mYaw	-=time*0.1f;
			}

			if(kbs.IsKeyDown(Keys.Y))
			{
				mYaw	+=time*0.1f;
			}

			if(kbs.IsKeyDown(Keys.V))
			{
				mPitch	-=time*0.1f;
			}

			if(kbs.IsKeyDown(Keys.N))
			{
				mPitch	+=time*0.1f;
			}

			UpdateMatrix();*/
		}


		//matrices, matrii, matrixs?
		public void UpdateMatrices(Matrix world, Matrix view, Matrix proj)
		{
			foreach(ModelMesh mesh in mModel.Meshes)
			{
				foreach(BasicEffect effect in mesh.Effects)
				{
					effect.World		=world;
					effect.View			=view;
					effect.Projection	=proj;
				}
			}

			//haven't figured out how to override
			//the built in shaders
			/*
			mFX.Parameters["mWorld"].SetValue(world);
			mFX.Parameters["mView"].SetValue(view);
			mFX.Parameters["mProjection"].SetValue(proj);

			mFX.CommitChanges();
			*/
		}


		public void UpdatePosition(Vector3 pos)
		{
			mPosition	=pos;

			UpdateMatrix();
		}


		public void UpdateLightColor(Vector4 lightColor)
		{
			mFX.Parameters["mLightColor"].SetValue(lightColor);
			mFX.CommitChanges();
		}


		public void UpdateLightDirection(Vector3 lightDir)
		{
			mFX.Parameters["mLightDirection"].SetValue(lightDir);
			mFX.CommitChanges();
		}


		public void UpdateAmbientColor(Vector4 ambient)
		{
			mFX.Parameters["mAmbientColor"].SetValue(ambient);
			mFX.CommitChanges();
		}


		private void UpdateMatrix()
		{
			mMat	=Matrix.CreateTranslation(mPosition) *
				Matrix.CreateRotationY(MathHelper.ToRadians(mYaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(mPitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(mRoll));

//			mMat	*=Matrix.CreateScale(0.01f);
		}
	}
}
