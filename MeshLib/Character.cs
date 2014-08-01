using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Device			=SharpDX.Direct3D11.Device;
using MaterialLibrary	=MaterialLib.MaterialLib;

namespace MeshLib
{
	//an instance of a character
	public class Character
	{
		//parts
		List<Mesh>	mMeshParts	=new List<Mesh>();

		//skin info
		Skin	mSkin;

		//refs to anim lib
		AnimLib	mAnimLib;

		//bounds
		BoundingBox		mBoxBound;
		BoundingSphere	mSphereBound;

		//transform
		Matrix	mTransform;

		//raw bone transforms for shader
		Matrix	[]mBones;


		public Character(AnimLib al)
		{
			mAnimLib	=al;
			mTransform	=Matrix.Identity;
		}


		public void FreeAll()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.FreeAll();
			}
			mBones	=null;
		}


		public bool RenameMesh(string oldName, string newName)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(m.Name == oldName)
				{
					m.Name	=newName;
					return	true;
				}
			}
			return	false;
		}


		public void AssignMaterialIDs(MaterialLib.IDKeeper keeper)
		{
			foreach(Mesh m in mMeshParts)
			{
//				m.AssignMaterialIDs(keeper);
			}
		}


		//copies bones into the shader
		//materials should be set up to ignore
		//the mBones parameter
		void UpdateShaderBones(MaterialLibrary matLib)
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


		public Skin GetSkin()
		{
			return	mSkin;
		}


		public void SetTransform(Matrix mat)
		{
			mTransform	=mat;
		}


		public Matrix GetTransform()
		{
			return	mTransform;
		}


		public void AddMeshPart(Mesh m)
		{
			mMeshParts.Add(m);
		}


		public void NukeMesh(Mesh m)
		{
			if(mMeshParts.Contains(m))
			{
				mMeshParts.Remove(m);
			}
		}


		public bool HasSkin()
		{
			return	mSkin != null;
		}


		public void SetSkin(Skin s)
		{
			mSkin	=s;
		}


		//for gui
		public List<Mesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		public void SaveToFile(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying characters
			UInt32	magic	=0xCA1EC7BE;

			bw.Write(magic);

			//save mesh parts
			bw.Write(mMeshParts.Count);
			foreach(Mesh m in mMeshParts)
			{
				m.Write(bw);
			}

			//save skin
			mSkin.Write(bw);

			bw.Close();
			file.Close();
		}


		//set bEditor if you want the buffers set to readable
		//so they can be resaved if need be
		public bool ReadFromFile(string fileName, Device gd, bool bEditor)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mMeshParts.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xCA1EC7BE)
			{
				br.Close();
				file.Close();
				return	false;
			}

			int	numMesh	=br.ReadInt32();
			for(int i=0;i < numMesh;i++)
			{
				Mesh	m;

				if(bEditor)
				{
					m	=new EditorMesh("temp");
				}
				else
				{
					m	=new Mesh();
				}

				m.Read(br, gd, bEditor);
				mMeshParts.Add(m);
			}

			mSkin	=new Skin();
			mSkin.Read(br);

			br.Close();
			file.Close();

			mTransform	=Matrix.Identity;

			return	true;
		}


		public void Blend(string anim1, float anim1Time,
			string anim2, float anim2Time, float percentage)
		{
			mAnimLib.Blend(anim1, anim1Time, anim2, anim2Time, percentage);

			UpdateBones(mAnimLib.GetSkeleton(), mSkin);
		}


		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			UpdateBones(mAnimLib.GetSkeleton(), mSkin);
		}


		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			if(bBox)
			{
				return	UtilityLib.Mathery.RayIntersectBox(start, end, mBoxBound);
			}
			else
			{
				return	UtilityLib.Mathery.RayIntersectSphere(start, end, mSphereBound);
			}
		}


		public void UpdateBounds()
		{
			if(mSkin == null)
			{
				return;
			}

			Skeleton		skel		=mAnimLib.GetSkeleton();
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
			mSphereBound	=UtilityLib.Mathery.SphereFromPoints(points);
		}


		public BoundingBox GetBoxBound()
		{
			return	mBoxBound;
		}


		public BoundingSphere GetSphereBound()
		{
			return	mSphereBound;
		}

		
		public void Draw(DeviceContext dc, MaterialLib.MaterialLib matLib)
		{
			UpdateShaderBones(matLib);

			foreach(Mesh m in mMeshParts)
			{
				m.Draw(dc, null);
			}
		}


		public void DrawDMN(DeviceContext dc,
			MaterialLib.MaterialLib matLib)
		{
			UpdateShaderBones(matLib);

			foreach(Mesh m in mMeshParts)
			{
				m.DrawDMN(dc, null);
			}
		}


		public void Draw(DeviceContext dc, MaterialLib.MaterialLib matLib, string altMaterial)
		{
			UpdateShaderBones(matLib);

			foreach(Mesh m in mMeshParts)
			{
				m.Draw(dc, null);
			}
		}


		public Vector3 GetForwardVector()
		{
			return	mTransform.Forward;
		}


		public Matrix GetBoneMatrix(string boneName)
		{
			return	mSkin.GetBoneByNameNoBind(boneName, mAnimLib.GetSkeleton());
		}
	}
}