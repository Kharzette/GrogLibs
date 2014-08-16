using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using UtilityLib;

using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	//an instance of a character
	public class Character
	{
		MeshPartStuff	mParts;

		//refs to anim lib
		AnimLib	mAnimLib;

		//bounds
		BoundingBox		mBoxBound;
		BoundingSphere	mSphereBound;

		//transform
		Matrix	mTransform;
		Matrix	mTransInverted;

		//raw bone transforms for shader
		Matrix	[]mBones;


		public Character(IArch ca, AnimLib al)
		{
			mParts		=new MeshPartStuff(ca);
			mAnimLib	=al;
			mTransform	=Matrix.Identity;
		}


		public void FreeAll()
		{
			mParts.FreeAll();

			mBones	=null;
			mParts	=null;
		}


		public bool IsEmpty()
		{
			return	mParts.IsEmpty();
		}


		public Matrix GetTransform()
		{
			return	mTransform;
		}


		public void SetTransform(Matrix mat)
		{
			mTransform		=mat;
			mTransInverted	=mTransform;

			mTransInverted.Invert();

			//set in the materials
			mParts.SetMatObjTransforms(mat);
		}


		public BoundingBox GetBoxBound()
		{
			BoundingBox	box	=mParts.GetBoxBound();

			box.Minimum	=Vector3.TransformCoordinate(box.Minimum, mTransform);
			box.Maximum	=Vector3.TransformCoordinate(box.Maximum, mTransform);

			return	box;
		}


		public BoundingSphere GetSphereBound()
		{
			BoundingSphere	ret	=mParts.GetSphereBound();

			return	Mathery.TransformSphere(mTransform, ret);
		}


		//these index the same as the mesh part list in the archetype
		public void AddPart(MatLib mats)
		{
			mParts.AddPart(mats, mTransform);
		}


		public void NukePart(int index)
		{
			mParts.NukePart(index);
		}


		public void NukeParts(List<int> indexes)
		{
			mParts.NukeParts(indexes);
		}


		public void SetPartMaterialName(int index, string matName)
		{
			mParts.SetPartMaterialName(index, matName);
		}


		public string GetPartMaterialName(int index)
		{
			return	mParts.GetPartMaterialName(index);
		}


		public void SetPartVisible(int index, bool bVisible)
		{
			mParts.SetPartVisible(index, bVisible);
		}


		public void SetTriLightValues(
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			mParts.SetTriLightValues(col0, col1, col2, lightDir);
		}


		//TODO: needs testing
		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			return	mParts.RayIntersect(backStart, backEnd, bBox, out partHit);
		}

	
		//copies bones into the shader
		//materials should be set up to ignore
		//the mBones parameter
		void UpdateShaderBones(MatLib matLib)
		{
			if(mBones != null)
			{
				matLib.SetEffectParameter("Character.fx", "mBones", mBones);
			}
		}


		void UpdateBones(Skeleton sk, Skin skn)
		{
			//no need for this if not skinned
			if(skn == null || sk == null)
			{
				return;
			}

			if(mBones == null)
			{
				mBones	=new Matrix[sk.GetNumIndexedBones()];
			}
			for(int i=0;i < mBones.Length;i++)
			{
				mBones[i]	=skn.GetBoneByIndex(i, sk);
			}
		}


		public void Blend(string anim1, float anim1Time,
			string anim2, float anim2Time, float percentage)
		{
			mAnimLib.Blend(anim1, anim1Time, anim2, anim2Time, percentage);

			UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());
		}


		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());
		}


		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			if(bBox)
			{
				return	Mathery.RayIntersectBox(backStart, backEnd, mBoxBound);
			}
			else
			{
				return	Mathery.RayIntersectSphere(backStart, backEnd, mSphereBound);
			}
		}


		public void UpdateBounds()
		{
			Skeleton		skel		=mAnimLib.GetSkeleton();

			if(skel == null)
			{
				return;	//no anim stuff loaded
			}

			List<Vector3>	points		=new List<Vector3>();
			int				numIndexed	=skel.GetNumIndexedBones();

			if(skel == null)
			{
				return;
			}

			for(int i=0;i < numIndexed;i++)
			{
				Matrix	mat	=Matrix.Identity;
				skel.GetMatrixForBone(i, out mat);

				mat	*=Matrix.RotationX((float)Math.PI / -2.0f);
				mat	*=Matrix.RotationY((float)Math.PI);

				Vector3	pnt	=Vector3.Zero;

				pnt	=Vector3.TransformCoordinate(pnt, mat);

				points.Add(pnt);
			}
			mBoxBound		=BoundingBox.FromPoints(points.ToArray());
			mSphereBound	=Mathery.SphereFromPoints(points);
		}


		public void Draw(DeviceContext dc, MatLib matLib)
		{
			UpdateShaderBones(matLib);

			mParts.Draw(dc);
		}


		public void Draw(DeviceContext dc, MatLib matLib, string altMaterial)
		{
			UpdateShaderBones(matLib);

			mParts.Draw(dc, altMaterial);
		}


		public void DrawDMN(DeviceContext dc, MatLib matLib)
		{
			UpdateShaderBones(matLib);

			mParts.DrawDMN(dc);
		}


		public Vector3 GetForwardVector()
		{
			return	mTransform.Forward;
		}
	}
}