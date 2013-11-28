using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;


namespace MeshLib
{
	public class Character
	{
		//parts
		List<SkinnedMesh>	mMeshParts	=new List<SkinnedMesh>();

		//skin info
		Skin	mSkin;

		//refs to anim and material libs
		MaterialLib.MaterialLib	mMatLib;
		AnimLib					mAnimLib;

		//bounds
		BoundingBox		mBoxBound;
		BoundingSphere	mSphereBound;

		//transform
		Matrix	mTransform;

		//raw bone transforms for shader
		Matrix	[]mBones;

		//ref to the effect that has bones
		Effect	mFX;


		public Character(MaterialLib.MaterialLib ml, AnimLib al)
		{
			mMatLib		=ml;
			mAnimLib	=al;
			mTransform	=Matrix.Identity;
		}


		//copies bones into the shader
		//materials should be set up to ignore
		//the mBones parameter
		public void UpdateShaderBones()
		{
			if(mBones != null)
			{
				if(mFX == null)
				{
					mFX	=mMatLib.GetShader("Shaders\\Trilight");
				}
				mFX.Parameters["mBones"].SetValue(mBones);
			}
		}


		public void UpdateBones(Skeleton sk, Skin skn)
		{
			//no need for this if not skinned
			if(skn == null || sk == null)
			{
				return;
			}

			if(mBones == null)
			{
				mBones	=new Matrix[skn.GetNumBones()];
			}
			for(int i=0;i < mBones.Length;i++)
			{
				mBones[i]	=skn.GetBoneByIndex(i, sk);
			}
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
			SkinnedMesh	sm	=m as SkinnedMesh;

			if(sm != null)
			{
				mMeshParts.Add(sm);
			}
		}


		public void NukeMesh(Mesh m)
		{
			SkinnedMesh	sm	=m as SkinnedMesh;

			if(sm != null)
			{
				if(mMeshParts.Contains(sm))
				{
					mMeshParts.Remove(sm);
				}
			}
		}


		public void SetSkin(Skin s)
		{
			mSkin	=s;
		}


		public void SetAppearance(List<string> meshParts, List<string> materials)
		{
			foreach(SkinnedMesh m in mMeshParts)
			{
				if(meshParts.Contains(m.Name))
				{
					m.Visible	=true;

					int	idx	=meshParts.IndexOf(m.Name);

					m.MaterialName	=materials[idx];
				}
				else
				{
					m.Visible	=false;
				}
			}
		}


		//for gui
		public List<SkinnedMesh> GetMeshPartList()
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
			foreach(SkinnedMesh m in mMeshParts)
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
		public bool ReadFromFile(string fileName, GraphicsDevice gd, bool bEditor)
		{
			Stream			file	=null;
			if(bEditor)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

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
				SkinnedMesh	m	=new SkinnedMesh();

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
			List<string>	boneNames	=mSkin.GetBoneNames();
			List<Vector3>	points		=new List<Vector3>();

			if(skel == null)
			{
				return;
			}

			foreach(string bone in boneNames)
			{
				Matrix	mat	=Matrix.Identity;
				skel.GetMatrixForBone(bone, out mat);

				mat	*=Matrix.CreateRotationX((float)Math.PI / -2.0f);
				mat	*=Matrix.CreateRotationY((float)Math.PI);

				Vector3	pnt	=Vector3.Zero;

				pnt	=Vector3.Transform(pnt, mat);

				points.Add(pnt);
			}
			mBoxBound		=BoundingBox.CreateFromPoints(points);
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


		public void Draw(GraphicsDevice gd)
		{
			UpdateShaderBones();

			foreach(SkinnedMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib, mTransform, "");
			}
		}


		public void Draw(GraphicsDevice gd, string altMaterial)
		{
			UpdateShaderBones();

			foreach(SkinnedMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib, mTransform, altMaterial);
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