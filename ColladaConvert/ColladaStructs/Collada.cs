using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace ColladaConvert
{
	class Collada
	{
		//data from collada
		private	Dictionary<string, Material>		mMaterials		=new Dictionary<string, Material>();
		private Dictionary<string, LibImage>		mImages			=new Dictionary<string, LibImage>();
		private	Dictionary<string, SceneNode>		mRootNodes		=new Dictionary<string, SceneNode>();
		private	Dictionary<string, Controller>		mControllers	=new Dictionary<string, Controller>();
		private	Dictionary<string, Geometry>		mGeometries		=new Dictionary<string, Geometry>();
		private Dictionary<string, Animation>		mAnimations		=new Dictionary<string, Animation>();
		private Dictionary<string, ColladaEffect>	mColladaEffects	=new Dictionary<string, ColladaEffect>();

		//midstep converters
		private List<MeshConverter>	mChunks	=new List<MeshConverter>();
		private	Character.Skeleton	mGameSkeleton;
		public Animator				mAnimator;

		//actual useful data for the game
		private	Character.MaterialLib	mMatLib;
		private Character.AnimLib		mAnimLib;
		private Character.Character		mCharacter;

		private Dictionary<string, Texture2D>	mTextures	=new Dictionary<string,Texture2D>();


		public Collada(string meshFileName,
			GraphicsDevice g,
			ContentManager cm,
			Character.MaterialLib mlib,
			Character.AnimLib alib,
			Character.Character chr)
		{
			Load(meshFileName);

			mMatLib		=mlib;
			mAnimLib	=alib;
			mCharacter	=chr;

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				List<float> verts	=geo.Value.GetBaseVerts();

				MeshConverter	cnk	=new MeshConverter(geo.Value.GetMeshName());

				cnk.CreateBaseVerts(verts);

				cnk.SetGeometryID(geo.Key);

				mChunks.Add(cnk);
			}

			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				cont.Value.ChangeCoordinateSystemMAX();

				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == sk.GetGeometryID())
					{
						cnk.AddWeightsToBaseVerts(sk);
					}
				}
			}

			mGameSkeleton	=BuildGameSkeleton();

			mAnimLib.SetSkeleton(mGameSkeleton);

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				//find the matching drawchunk
				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == geo.Key)
					{
						List<int>	posIdxs		=geo.Value.GetPositionIndexs();
						List<float>	norms		=geo.Value.GetNormals();
						List<int>	normIdxs	=geo.Value.GetNormalIndexs();
						List<float>	texCoords0	=geo.Value.GetTexCoords(0);
						List<float>	texCoords1	=geo.Value.GetTexCoords(1);
						List<float>	texCoords2	=geo.Value.GetTexCoords(2);
						List<float>	texCoords3	=geo.Value.GetTexCoords(3);
						List<int>	texIdxs0	=geo.Value.GetTexCoordIndexs(0);
						List<int>	texIdxs1	=geo.Value.GetTexCoordIndexs(1);
						List<int>	texIdxs2	=geo.Value.GetTexCoordIndexs(2);
						List<int>	texIdxs3	=geo.Value.GetTexCoordIndexs(3);
						List<float>	colors0		=geo.Value.GetColors(0);
						List<float>	colors1		=geo.Value.GetColors(1);
						List<float>	colors2		=geo.Value.GetColors(2);
						List<float>	colors3		=geo.Value.GetColors(3);
						List<int>	colIdxs0	=geo.Value.GetColorIndexs(0);
						List<int>	colIdxs1	=geo.Value.GetColorIndexs(1);
						List<int>	colIdxs2	=geo.Value.GetColorIndexs(2);
						List<int>	colIdxs3	=geo.Value.GetColorIndexs(3);
						List<int>	vertCounts	=geo.Value.GetVertCounts();

						cnk.AddNormTexByPoly(posIdxs, norms, normIdxs,
							texCoords0, texIdxs0, texCoords1, texIdxs1,
							texCoords2, texIdxs2, texCoords3, texIdxs3,
							colors0, colIdxs0, colors1, colIdxs1,
							colors2, colIdxs2, colors3, colIdxs3,
							vertCounts);

						bool	bPos	=(posIdxs != null && posIdxs.Count > 0);
						bool	bNorm	=(norms != null && norms.Count > 0);
						bool	bTex0	=(texCoords0 != null && texCoords0.Count > 0);
						bool	bTex1	=(texCoords1 != null && texCoords1.Count > 0);
						bool	bTex2	=(texCoords2 != null && texCoords2.Count > 0);
						bool	bTex3	=(texCoords3 != null && texCoords3.Count > 0);
						bool	bCol0	=(colors0 != null && colors0.Count > 0);
						bool	bCol1	=(colors1 != null && colors1.Count > 0);
						bool	bCol2	=(colors2 != null && colors2.Count > 0);
						bool	bCol3	=(colors3 != null && colors3.Count > 0);
						bool	bBone	=false;

						//see if any skins reference this geometry
						foreach(KeyValuePair<string, Controller> cont in mControllers)
						{
							Skin	sk	=cont.Value.GetSkin();
							if(sk.GetGeometryID() == geo.Key)
							{
								bBone	=true;
								break;
							}
						}

						cnk.BuildBuffers(g, bPos, bNorm, bBone,
							bBone, bTex0, bTex1, bTex2, bTex3,
							bCol0, bCol1, bCol2, bCol3);
					}
				}
			}

			foreach(KeyValuePair<string, LibImage> li in mImages)
			{
				string	path	=li.Value.mPath;
				if(path.StartsWith("file://"))
				{
					path	=path.Remove(0, 7);
				}
//				Texture2D	tex	=Texture2D.FromFile(g, path);
//				mTextures.Add(li.Value.mName, tex);
			}

			//create useful anims
			mAnimator	=new Animator(mAnimations, mRootNodes);

			Character.Anim	anm	=new Character.Anim(mControllers.Count);
			anm.Name	=meshFileName.Substring(meshFileName.IndexOf("Content"));
			int	i		=0;
			
			//create anims we can save
			List<Character.Skin>	skinList	=new List<Character.Skin>();
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				List<string>		boneNames	=sk.GetJointNameArray();
				List<Character.SubAnim>	anims	=mAnimator.BuildGameAnims(mGameSkeleton);

				anm.AddControllerSubAnims(i, anims);
				i++;

				Character.Skin	skin	=new Character.Skin();
				skin.SetBindShapeMatrix(sk.GetBindShapeMatrix());
				skin.SetBoneNames(sk.GetJointNameArray());
				skin.SetInverseBindPoses(sk.GetInverseBindPoses());

				mCharacter.AddSkin(skin);
				skinList.Add(skin);
			}
			mAnimLib.AddAnim(anm);


			Dictionary<string, Character.Mesh>	idlist	=new Dictionary<string,Character.Mesh>();

			foreach(MeshConverter mc in mChunks)
			{
				mCharacter.AddMeshPart(mc.mConverted);
				idlist.Add(mc.mGeometryID, mc.mConverted);
			}

			//set skin pointers in meshes
			i	=0;
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == sk.GetGeometryID())
					{
						idlist[cnk.mGeometryID].SetSkin(skinList[i]);
						idlist[cnk.mGeometryID].SetSkinIndex(i);
					}
				}
				i++;
			}
		}


		public void LoadAnim(string path)
		{
			FileStream	file	=OpenTitleFile(path,
				FileMode.Open, FileAccess.Read);
			
			file.Seek(0, SeekOrigin.Begin);
			XmlReader	r	=XmlReader.Create(file);
			mAnimations.Clear();
			LoadAnimations(r);

			//create useful anims
			mAnimator	=new Animator(mAnimations, mRootNodes);

			Character.Anim	anm	=new Character.Anim(mControllers.Count);
			anm.Name	=path.Substring(path.IndexOf("Content"));
			int	i		=0;
			
			//create anims we can save
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				List<string>		boneNames	=sk.GetJointNameArray();
				List<Character.SubAnim>	anims	=mAnimator.BuildGameAnims(mGameSkeleton);

				anm.AddControllerSubAnims(i, anims);
				i++;
			}
			mAnimLib.AddAnim(anm);
			file.Close();
		}


		public Character.Skeleton	BuildGameSkeleton()
		{
			Character.Skeleton	ret	=new Character.Skeleton();

			foreach(KeyValuePair<string, SceneNode> sn in mRootNodes)
			{
				Character.GSNode	n;//	=new GSNode();

				sn.Value.AddToGameSkeleton(out n);

				ret.AddRoot(n);
			}
			return	ret;
		}


		public void Draw(GraphicsDevice g)
		{
			mCharacter.Draw(g);
		}
		

		private void LoadColladaEffects(XmlReader r)
		{
			//find effects
			while(r.Read())
			{
				if(r.Name == "library_effects")
				{
					break;
				}
			}

			//read effects
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitespace
				}

				if(r.Name == "effect")
				{
					r.MoveToFirstAttribute();
					string	fxid	=r.Value;

					ColladaEffect	ce	=new ColladaEffect();
					ce.Load(r);
					mColladaEffects.Add(fxid, ce);
				}
			}
		}


		//fill up our mMaterials
		private void LoadMaterials(XmlReader r)
		{
			//find materials
			while(r.Read())
			{
				if(r.Name == "library_materials")
				{
					break;
				}
			}

			string	matID	="";
			string	matName	="";

			//read materials
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitespace
				}

				Console.WriteLine("Name:" + r.Name);

				if(r.Name == "library_materials" && r.NodeType == XmlNodeType.EndElement)
				{
					break;
				}

				if(r.Name == "material")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						matID	=r.Value;
						r.MoveToNextAttribute();
						matName	=r.Value;
					}
				}

				if(r.Name == "instance_effect")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();

						Material m			=new Material();
						m.mName				=matName;
						m.mInstanceEffect	=r.Value;

						mMaterials.Add(matID, m);
					}
				}
			}
		}


		//fill up our mImages
		private void LoadImages(XmlReader r)
		{
			//find images
			while(r.Read())
			{
				if(r.Name == "library_images")
				{
					break;
				}
			}

			string	imageID		="";
			string	imageName	="";

			//read materials
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitespace
				}

				Console.WriteLine("Name:" + r.Name);

				if(r.Name == "library_images" && r.NodeType == XmlNodeType.EndElement)
				{
					break;
				}

				if(r.Name == "image")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						imageID	=r.Value;
						r.MoveToNextAttribute();
						imageName	=r.Value;
					}
				}

				if(r.Name == "init_from" && r.NodeType == XmlNodeType.Element)
				{
					r.Read();	//skip to path

					LibImage	li	=new LibImage();
					li.mName		=imageName;
					li.mPath		=r.Value;

					mImages.Add(imageID, li);
				}
			}
		}


		public void	LoadVisualScenes(XmlReader r)
		{
			//find visual scenes
			while(r.Read())
			{
				if(r.Name == "library_visual_scenes")
				{
					break;
				}
			}

			string temp		="";

			//read visual scene stuff
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}
				if(r.Name == "library_visual_scenes" && r.NodeType == XmlNodeType.EndElement)
				{
					break;
				}

				if(r.Name == "visual_scene")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						temp	=r.Value;
					}
				}

				if(r.Name == "node")
				{
					int attcnt	=r.AttributeCount;

					if(attcnt > 0)
					{
						r.MoveToFirstAttribute();
						string	id	=r.Value;

						SceneNode	root	=new SceneNode();
						root.LoadNode(r);

						mRootNodes.Add(id, root);
					}
				}
			}
		}

		
		public void LoadControllers(XmlReader r)
		{
			//find controllers
			while(r.Read())
			{
				if(r.Name == "library_controllers")
				{
					break;
				}
			}

			//read controller stuff
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "library_controllers")
				{
					break;
				}
				else if(r.Name == "controller")
				{
					if(r.AttributeCount > 0)
					{
						Controller	c	=new Controller();

						r.MoveToFirstAttribute();
						string	contID	=r.Value;

						c.Load(r);
						mControllers.Add(contID, c);
					}
				}
			}
		}

		
		public void Load(string fileName)
		{
			FileStream	file	=OpenTitleFile(fileName,
				FileMode.Open, FileAccess.Read);
			
			XmlReader	r	=XmlReader.Create(file);

			//todo: read up_axis and unit meter

			LoadImages(r);

			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadMaterials(r);

			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadVisualScenes(r);

			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadControllers(r);

			//find geometry
			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadGeometries(r);

			//find animations
			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadAnimations(r);

			//find effects
			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadColladaEffects(r);

			file.Close();
		}


		private void LoadAnimations(XmlReader r)
		{
			while(r.Read())
			{
				if(r.Name == "library_animations")
				{
					break;
				}
			}

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}
				if(r.Name == "library_animations")
				{
					break;
				}
				else if(r.Name == "animation")
				{
					r.MoveToFirstAttribute();
					string	animID	=r.Value;

					Animation	anim	=new Animation();
					anim.Load(r);

					mAnimations.Add(animID, anim);
				}
			}
		}


		private void LoadGeometries(XmlReader r)
		{
			while(r.Read())
			{
				if(r.Name == "library_geometries")
				{
					break;
				}
			}

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}
				if(r.Name == "library_geometries")
				{
					break;
				}
				else if(r.Name == "geometry")
				{
					if(r.AttributeCount > 0)
					{
						//alloc a new geom
						r.MoveToFirstAttribute();
						string	geomID	=r.Value;
						r.MoveToNextAttribute();
						string	geoName	=r.Value;

						Geometry	g	=new Geometry(geoName);
						g.Load(r);

						mGeometries.Add(geomID, g);
					}
				}
			}
		}


		public void LoadFloatArray(XmlReader r, List<Vector3> fa)
		{
			//find float_array
			while(r.Name != "float_array")
			{
				r.Read();
			}

			r.MoveToFirstAttribute();
			r.MoveToNextAttribute();

			int cnt;
			int.TryParse(r.Value,out cnt);

			//skip to the goodies
			r.Read();

			string[] tokens	=r.Value.Split(' ');

			for(int i=0;i < tokens.Length;i+=3)
			{
				Vector3 temp;

				float.TryParse(tokens[i],out temp.X);
				float.TryParse(tokens[i+1],out temp.Y);
				float.TryParse(tokens[i+2],out temp.Z);

				fa.Add(temp);
			}

			//find closing float_array
			while(r.Name != "float_array")
			{
				r.Read();
			}
			r.Read();
		}


		public void LoadFloatArray(XmlReader r, List<Vector2> fa)
		{
			//find float_array
			while(r.Name != "float_array")
			{
				r.Read();
			}

			r.MoveToFirstAttribute();
			r.MoveToNextAttribute();

			int cnt;
			int.TryParse(r.Value,out cnt);

			//skip to the goodies
			r.Read();

			string[] tokens	=r.Value.Split(' ');

			for(int i=0;i < tokens.Length;i+=2)
			{
				Vector2 temp;

				float.TryParse(tokens[i],out temp.X);
				float.TryParse(tokens[i+1],out temp.Y);

				fa.Add(temp);
			}

			//find closing float_array
			while(r.Name != "float_array")
			{
				r.Read();
			}
			r.Read();
		}


		public static void GetVectorFromString(string str, out Vector3 vec)
		{
			string[] tokens	=str.Split(' ');

			//todo:  This is very fragile
			Single.TryParse(tokens[0], out vec.X);
			Single.TryParse(tokens[1], out vec.Y);
			Single.TryParse(tokens[2], out vec.Z);
		}


		public static void GetVectorFromString(string str, out Vector4 vec)
		{
			string[] tokens	=str.Split(' ');

			//todo:  This is very fragile
			Single.TryParse(tokens[0],out vec.X);
			Single.TryParse(tokens[1],out vec.Y);
			Single.TryParse(tokens[2],out vec.Z);
			Single.TryParse(tokens[3],out vec.W);
		}


		public static void GetQuaternionFromString(string str, out Quaternion q)
		{
			string[] tokens	=str.Split(' ');

			//todo:  This is very fragile
			Single.TryParse(tokens[0],out q.X);
			Single.TryParse(tokens[1],out q.Y);
			Single.TryParse(tokens[2],out q.Z);
			Single.TryParse(tokens[3],out q.W);

			q.W	=MathHelper.ToRadians(q.W);
		}


		public static void GetSkewFromString(string str, out float ang, out Vector3 axRot, out Vector3 axTrans)
		{
			string[] tokens	=str.Split(' ');

			//todo:  This is very fragile
			Single.TryParse(tokens[0], out ang);
			Single.TryParse(tokens[1], out axRot.X);
			Single.TryParse(tokens[2], out axRot.Y);
			Single.TryParse(tokens[3], out axRot.Z);
			Single.TryParse(tokens[4], out axTrans.X);
			Single.TryParse(tokens[5], out axTrans.Y);
			Single.TryParse(tokens[6], out axTrans.Z);
		}


		public static void GetLookAtFromString(string str, out Vector3 eyePos, out Vector3 interestPos, out Vector3 upVec)
		{
			string[] tokens	=str.Split(' ');

			//todo:  This is very fragile
			Single.TryParse(tokens[0], out eyePos.X);
			Single.TryParse(tokens[1], out eyePos.Y);
			Single.TryParse(tokens[2], out eyePos.Z);
			Single.TryParse(tokens[3], out interestPos.X);
			Single.TryParse(tokens[4], out interestPos.Y);
			Single.TryParse(tokens[5], out interestPos.Z);
			Single.TryParse(tokens[6], out upVec.X);
			Single.TryParse(tokens[7], out upVec.Y);
			Single.TryParse(tokens[8], out upVec.Z);
		}


		public static void GetMatrixFromString(string str, out Matrix mat)
		{
			string[] tokens	=str.Split(' ', '\n', '\t');

			int	tokIdx	=0;

			//transpose as we load
			//this looks very unsafe / dangerous
			while(!Single.TryParse(tokens[tokIdx++],out mat.M11));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M21));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M31));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M41));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M12));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M22));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M32));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M42));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M13));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M23));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M33));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M43));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M14));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M24));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M34));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M44));
		}


		//change a floatarray of vector3's from max's
		//coordinate system to ours
		//not used, but nice to have for debugging
		private void ConvertCoordinateSystemMAX(List<float> verts)
		{
			Debug.Assert(verts.Count % 3 == 0);
			for(int i=0;i < verts.Count / 3;i++)
			{
				float	temp	=verts[i * 3 + 1];

				//negate x, and swap y and z
				verts[i * 3]		=-verts[i * 3];
				verts[i * 3 + 1]	=verts[i * 3 + 2];
				verts[i * 3 + 2]	=temp;
			}
		}


		//decomposition seems the only reliable way
		//to convert between handedness
		public static Matrix ConvertMatrixCoordinateSystemMAX(Matrix inMat)
		{
			Vector3		scaleVec, trans;
			Quaternion	rot;

			//this could fail
			bool	ret	=inMat.Decompose(out scaleVec, out rot, out trans);

			Debug.Assert(ret);

			//wild guess at proper order
			Matrix	outMat;
			outMat	=Matrix.CreateScale(scaleVec);
			outMat	*=Matrix.CreateFromQuaternion(rot);
			outMat	*=Matrix.CreateTranslation(trans);

			return	outMat;
		}


		public static Matrix ConvertMatrixCoordinateSystemSceneNode(Matrix inMat)
		{
			Matrix	outMat	=inMat;

			return	outMat;
		}


		//debug routine for messing with bones
		public void DebugBoneModify(Matrix mat)
		{
			Controller		cont	=mControllers["Box01Controller"];
			Skin			sk		=cont.GetSkin();
			Matrix			bind	=sk.GetBindShapeMatrix();
			List<Matrix>	ibps	=sk.GetInverseBindPoses();
			Matrix			bone;

			mRootNodes["Bone01"].GetMatrixForBone("Bone01", out bone);

			//mod the shader bones directly
//			mChunks[0].mConverted.mBones[0]	=ibps[0] * bone * mat;
		}


		public static FileStream OpenTitleFile(string fileName,
			FileMode mode, FileAccess access)
		{
#if XBOX
			string fullPath	=Path.Combine(
				StorageContainer.TitleLocation, fileName);
#else
			string fullPath = fileName;	//on windows just use the path
#endif
			if(!File.Exists(fullPath) &&
				(access == FileAccess.Write || access == FileAccess.ReadWrite))
			{
				return	File.Create(fullPath);
			}
			else
			{
				return File.Open(fullPath, mode, access);
			}
		}
	}
}