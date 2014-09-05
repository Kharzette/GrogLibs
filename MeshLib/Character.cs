using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

		//inverted bones for raycasting
		Matrix			[]mInvertedBones;
		bool			mbInvertedReady;	//thread done?
		int				mThreadMisses;		//times had to invert on the spot
		Action<object>	mInvertBones;
		float			mTimeSinceLastInvert;
		bool			mbAutoInvert;
		float			mInvertInterval;
		Task			mRunningTask;


		public Character(IArch ca, AnimLib al)
		{
			mParts		=new MeshPartStuff(ca);
			mAnimLib	=al;
			mTransform	=Matrix.Identity;

			//task to invert the bones
			mInvertBones	=(object c) =>
			{
				Character	chr	=c as Character;
				for(int i=0;i < chr.mInvertedBones.Length;i++)
				{
					chr.mInvertedBones[i]	=Matrix.Invert(chr.mBones[i]);
				}
				chr.mbInvertedReady	=true;
			};
		}


		public void FreeAll()
		{
			mParts.FreeAll();

			mBones			=null;
			mParts			=null;
			mInvertedBones	=null;
		}


		public void AutoInvert(bool bAuto, float interval)
		{
			mbAutoInvert	=bAuto;
			mInvertInterval	=interval;
		}


		public int GetThreadMisses()
		{
			return	mThreadMisses;
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


		public void SetMatLib(MatLib mats)
		{
			mParts.SetMatLibs(mats);
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


		//if the character needs accurate box collision
		public void UpdateInvertedBones(bool bImmediate)
		{
			if(bImmediate)
			{
				if(!mbInvertedReady)
				{
					mRunningTask.Wait();
				}
				else
				{
					mbInvertedReady	=false;
					mRunningTask	=Task.Factory.StartNew(mInvertBones, this);
					mRunningTask.Wait();
				}
			}
			else if(mbInvertedReady)
			{
				//update inverted on a thread
				mbInvertedReady	=false;
				mRunningTask	=Task.Factory.StartNew(mInvertBones, this);
			}
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


		public float? RayIntersectBones(Vector3 start, Vector3 end, bool bAccurate, out int boneHit)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			boneHit	=-1;

			Skin	sk	=mParts.GetSkin();

			float	bestDist	=float.MaxValue;
			for(int i=0;i < mBones.Length;i++)
			{
				BoundingBox	box	=sk.GetBoneBoundBox(i);

				if(!mbInvertedReady)
				{
					mThreadMisses++;
				}

				Matrix	verted;
				if(!mbInvertedReady && bAccurate)
				{
					verted	=Matrix.Invert(mBones[i]);
				}
				else
				{
					verted	=mInvertedBones[i];
				}

				Vector3	boneStart	=Vector3.TransformCoordinate(backStart, verted);
				Vector3	boneEnd		=Vector3.TransformCoordinate(backEnd, verted);

				float?	hit	=Mathery.RayIntersectBox(boneStart, boneEnd, box);
				if(hit == null)
				{
					continue;
				}

				if(hit.Value < bestDist)
				{
					bestDist	=hit.Value;
					boneHit		=i;
				}
			}

			if(boneHit == -1)
			{
				return	null;
			}
			return	bestDist;
		}


		//TODO: needs testing
		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			return	mParts.RayIntersect(backStart, backEnd, bBox, out partHit);
		}


		public void ComputeBoneBounds(List<string> skipMaterials)
		{
			mParts.ComputeBoneBounds(skipMaterials, mAnimLib.GetSkeleton());
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
				mBones			=new Matrix[sk.GetNumIndexedBones()];
				mInvertedBones	=new Matrix[mBones.Length];
				mbInvertedReady	=true;
			}
			for(int i=0;i < mBones.Length;i++)
			{
				mBones[i]	=skn.GetBoneByIndex(i, sk);
			}

			if(mbAutoInvert)
			{
				if(mbInvertedReady && mTimeSinceLastInvert > mInvertInterval)
				{
					mTimeSinceLastInvert	=0f;
					UpdateInvertedBones(false);
				}
			}
		}


		public void Blend(string anim1, float anim1Time,
			string anim2, float anim2Time,
			float percentage)
		{
			mAnimLib.Blend(anim1, anim1Time, anim2, anim2Time, percentage);

			UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());
		}


		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());

			mTimeSinceLastInvert	+=time;
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
			List<Vector3>	points		=new List<Vector3>();

			Vector3	[]corners	=new Vector3[8];

			Skin	sk	=mParts.GetSkin();

			for(int i=0;i < mBones.Length;i++)
			{
				BoundingBox	box	=sk.GetBoneBoundBox(i);

				box.GetCorners(corners);

				for(int j=0;j < 8;j++)
				{
					Vector3	transd	=Vector3.TransformCoordinate(corners[j], mBones[i]);

					points.Add(transd);
				}
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


		//this if for the DMN renderererererer
		public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
		{
			mParts.AssignMaterialIDs(idk);
		}


		public Vector3 GetForwardVector()
		{
			return	mTransform.Forward;
		}


		public void SaveToFile(string fileName)
		{
			FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw		=new BinaryWriter(file);

			//write a magic number identifying characters
			UInt32	magic	=0xCA1EC7BE;

			bw.Write(magic);

			//save mesh parts
			mParts.Write(bw);

			bw.Close();
			file.Close();
		}


		public bool ReadFromFile(string fileName)
		{
			if(!File.Exists(fileName))
			{
				return	false;
			}

			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			UInt32	magic	=br.ReadUInt32();
			if(magic != 0xCA1EC7BE)
			{
				br.Close();
				file.Close();
				return	false;
			}

			mParts.Read(br);

			br.Close();
			file.Close();

			return	true;
		}
	}
}