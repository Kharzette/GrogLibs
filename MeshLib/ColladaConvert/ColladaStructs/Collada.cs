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
		Dictionary<string, Material>		mMaterials		=new Dictionary<string, Material>();
		Dictionary<string, LibImage>		mImages			=new Dictionary<string, LibImage>();
		Dictionary<string, SceneNode>		mRootNodes		=new Dictionary<string, SceneNode>();
		Dictionary<string, Controller>		mControllers	=new Dictionary<string, Controller>();
		Dictionary<string, Geometry>		mGeometries		=new Dictionary<string, Geometry>();
		Dictionary<string, Animation>		mAnimations		=new Dictionary<string, Animation>();
		Dictionary<string, ColladaEffect>	mColladaEffects	=new Dictionary<string, ColladaEffect>();

		//midstep converters
		List<MeshConverter>	mChunks	=new List<MeshConverter>();
		MeshLib.Skeleton	mGameSkeleton;

		//actual useful data for the game
		MaterialLib.MaterialLib		mMatLib;
		MeshLib.AnimLib				mAnimLib;
		MeshLib.Character			mCharacter;
		MeshLib.StaticMeshObject	mStaticMesh;

		Dictionary<string, Texture2D>	mTextures	=new Dictionary<string,Texture2D>();


		public Collada(COLLADA			colMesh,
			GraphicsDevice				g,
			ContentManager				cm,
			MaterialLib.MaterialLib		mlib,
			MeshLib.StaticMeshObject	stat)
		{
			mMatLib		=mlib;
			mStaticMesh	=stat;

			foreach(object item in colMesh.Items)
			{
				library_geometries	geoms	=item as library_geometries;
				if(geoms == null)
				{
					continue;
				}
				foreach(object geomItem in geoms.geometry)
				{
					mesh	msh	=geomItem as mesh;
					if(msh == null)
					{
						continue;
					}

					int	numParts	=msh.source.Length;

					for(int j=0;j < numParts;j++)
					{
						float_array	verts	=msh.source[j].Item as float_array;
						if(verts == null)
						{
							continue;
						}
						
						MeshConverter	cnk	=new MeshConverter("blah");

						cnk.CreateBaseVerts(verts);

						cnk.mPartIndex	=j;
						cnk.SetGeometryID(geoms.id);
						
						mChunks.Add(cnk);
					}
				}
			}

			//bake scene node modifiers into geometry?
			foreach(MeshConverter mc in mChunks)
			{
				string	gid	=mc.mGeometryID;
				if(gid == null || gid == "")
				{
					continue;
				}

				foreach(KeyValuePair<string, SceneNode> roots in mRootNodes)
				{
					if(roots.Value == null)
					{
						continue;
					}

					Matrix	nodeMat	=Matrix.Identity;
					if(roots.Value.GetMatrixForGeometryID(gid, out nodeMat))
					{
						mc.BakeTransformIntoVerts(nodeMat);
					}
				}
			}

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				//find the matching drawchunk
				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == geo.Key)
					{
						List<int>	posIdxs		=geo.Value.GetPositionIndexs(cnk.mPartIndex);
						List<float>	norms		=geo.Value.GetNormals(cnk.mPartIndex);
						List<int>	normIdxs	=geo.Value.GetNormalIndexs(cnk.mPartIndex);
						List<float>	texCoords0	=geo.Value.GetTexCoords(cnk.mPartIndex, 0);
						List<float>	texCoords1	=geo.Value.GetTexCoords(cnk.mPartIndex, 1);
						List<float>	texCoords2	=geo.Value.GetTexCoords(cnk.mPartIndex, 2);
						List<float>	texCoords3	=geo.Value.GetTexCoords(cnk.mPartIndex, 3);
						List<int>	texIdxs0	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 0);
						List<int>	texIdxs1	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 1);
						List<int>	texIdxs2	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 2);
						List<int>	texIdxs3	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 3);
						List<float>	colors0		=geo.Value.GetColors(cnk.mPartIndex, 0);
						List<float>	colors1		=geo.Value.GetColors(cnk.mPartIndex, 1);
						List<float>	colors2		=geo.Value.GetColors(cnk.mPartIndex, 2);
						List<float>	colors3		=geo.Value.GetColors(cnk.mPartIndex, 3);
						List<int>	colIdxs0	=geo.Value.GetColorIndexs(cnk.mPartIndex, 0);
						List<int>	colIdxs1	=geo.Value.GetColorIndexs(cnk.mPartIndex, 1);
						List<int>	colIdxs2	=geo.Value.GetColorIndexs(cnk.mPartIndex, 2);
						List<int>	colIdxs3	=geo.Value.GetColorIndexs(cnk.mPartIndex, 3);
						List<int>	vertCounts	=geo.Value.GetVertCounts(cnk.mPartIndex);

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

						//normals, if available, need to be transformed
						//by any scene nodes that affect them
						if(bNorm)
						{
							foreach(KeyValuePair<string, SceneNode> roots in mRootNodes)
							{
								if(roots.Value == null)
								{
									continue;
								}

								Matrix	nodeMat	=Matrix.Identity;
								if(roots.Value.GetMatrixForGeometryID(geo.Key, out nodeMat))
								{
									cnk.BakeTransformIntoNormals(nodeMat);
								}
							}

							//flip the normals
							cnk.FlipNormals();
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

			foreach(MeshConverter mc in mChunks)
			{
				mStaticMesh.AddMeshPart(mc.GetStaticMesh());
			}
		}


		public Collada(string			meshFileName,
			GraphicsDevice				g,
			ContentManager				cm,
			MaterialLib.MaterialLib		mlib,
			MeshLib.StaticMeshObject	stat)
		{
			Load(meshFileName);

			mMatLib		=mlib;
			mStaticMesh	=stat;

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				int	numParts	=geo.Value.GetNumMeshParts();

				for(int j=0;j < numParts;j++)
				{
					List<float> verts	=geo.Value.GetBaseVerts(j);
					
					MeshConverter	cnk	=new MeshConverter(geo.Value.GetMeshName());

					cnk.CreateBaseVerts(verts);

					cnk.mPartIndex	=j;
					cnk.SetGeometryID(geo.Key);
					
					mChunks.Add(cnk);
				}
			}

			//bake scene node modifiers into geometry?
			foreach(MeshConverter mc in mChunks)
			{
				string	gid	=mc.mGeometryID;
				if(gid == null || gid == "")
				{
					continue;
				}

				foreach(KeyValuePair<string, SceneNode> roots in mRootNodes)
				{
					if(roots.Value == null)
					{
						continue;
					}

					Matrix	nodeMat	=Matrix.Identity;
					if(roots.Value.GetMatrixForGeometryID(gid, out nodeMat))
					{
						mc.BakeTransformIntoVerts(nodeMat);
					}
				}
			}

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				//find the matching drawchunk
				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == geo.Key)
					{
						List<int>	posIdxs		=geo.Value.GetPositionIndexs(cnk.mPartIndex);
						List<float>	norms		=geo.Value.GetNormals(cnk.mPartIndex);
						List<int>	normIdxs	=geo.Value.GetNormalIndexs(cnk.mPartIndex);
						List<float>	texCoords0	=geo.Value.GetTexCoords(cnk.mPartIndex, 0);
						List<float>	texCoords1	=geo.Value.GetTexCoords(cnk.mPartIndex, 1);
						List<float>	texCoords2	=geo.Value.GetTexCoords(cnk.mPartIndex, 2);
						List<float>	texCoords3	=geo.Value.GetTexCoords(cnk.mPartIndex, 3);
						List<int>	texIdxs0	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 0);
						List<int>	texIdxs1	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 1);
						List<int>	texIdxs2	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 2);
						List<int>	texIdxs3	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 3);
						List<float>	colors0		=geo.Value.GetColors(cnk.mPartIndex, 0);
						List<float>	colors1		=geo.Value.GetColors(cnk.mPartIndex, 1);
						List<float>	colors2		=geo.Value.GetColors(cnk.mPartIndex, 2);
						List<float>	colors3		=geo.Value.GetColors(cnk.mPartIndex, 3);
						List<int>	colIdxs0	=geo.Value.GetColorIndexs(cnk.mPartIndex, 0);
						List<int>	colIdxs1	=geo.Value.GetColorIndexs(cnk.mPartIndex, 1);
						List<int>	colIdxs2	=geo.Value.GetColorIndexs(cnk.mPartIndex, 2);
						List<int>	colIdxs3	=geo.Value.GetColorIndexs(cnk.mPartIndex, 3);
						List<int>	vertCounts	=geo.Value.GetVertCounts(cnk.mPartIndex);

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

						//normals, if available, need to be transformed
						//by any scene nodes that affect them
						if(bNorm)
						{
							foreach(KeyValuePair<string, SceneNode> roots in mRootNodes)
							{
								if(roots.Value == null)
								{
									continue;
								}

								Matrix	nodeMat	=Matrix.Identity;
								if(roots.Value.GetMatrixForGeometryID(geo.Key, out nodeMat))
								{
									cnk.BakeTransformIntoNormals(nodeMat);
								}
							}

							//flip the normals
							cnk.FlipNormals();
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

			foreach(MeshConverter mc in mChunks)
			{
				mStaticMesh.AddMeshPart(mc.GetStaticMesh());
			}
		}


		public Collada(string meshFileName,
			GraphicsDevice g,
			ContentManager cm,
			MaterialLib.MaterialLib mlib,
			MeshLib.AnimLib alib,
			MeshLib.Character chr)
		{
			Load(meshFileName);

			mMatLib		=mlib;
			mAnimLib	=alib;
			mCharacter	=chr;

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				int	numParts	=geo.Value.GetNumMeshParts();

				for(int j=0;j < numParts;j++)
				{
					List<float> verts	=geo.Value.GetBaseVerts(j);
					
					MeshConverter	cnk	=new MeshConverter(geo.Value.GetMeshName());

					cnk.CreateBaseVerts(verts);

					cnk.mPartIndex	=j;
					cnk.SetGeometryID(geo.Key);
					
					mChunks.Add(cnk);
				}
			}

			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == sk.GetGeometryID())
					{
						cnk.AddWeightsToBaseVerts(sk);
					}
				}
			}


			//bake scene node modifiers into controllers
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				if(sk == null)
				{
					continue;
				}

				string	gid	=sk.GetGeometryID();
				if(gid == null || gid == "")
				{
					continue;
				}

				foreach(KeyValuePair<string, SceneNode> roots in mRootNodes)
				{
					SceneNode	node	=roots.Value.GetNodeForInstanceController(cont.Key);

					if(node == null)
					{
						continue;
					}

					Matrix	nodeMat	=Matrix.Identity;
					node.GetMatrixForBone(node.GetName(), out nodeMat);
					
					foreach(MeshConverter mc in mChunks)
					{
						if(mc.mGeometryID == gid)
						{
							mc.BakeTransformIntoVerts(nodeMat);
						}
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
						List<int>	posIdxs		=geo.Value.GetPositionIndexs(cnk.mPartIndex);
						List<float>	norms		=geo.Value.GetNormals(cnk.mPartIndex);
						List<int>	normIdxs	=geo.Value.GetNormalIndexs(cnk.mPartIndex);
						List<float>	texCoords0	=geo.Value.GetTexCoords(cnk.mPartIndex, 0);
						List<float>	texCoords1	=geo.Value.GetTexCoords(cnk.mPartIndex, 1);
						List<float>	texCoords2	=geo.Value.GetTexCoords(cnk.mPartIndex, 2);
						List<float>	texCoords3	=geo.Value.GetTexCoords(cnk.mPartIndex, 3);
						List<int>	texIdxs0	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 0);
						List<int>	texIdxs1	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 1);
						List<int>	texIdxs2	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 2);
						List<int>	texIdxs3	=geo.Value.GetTexCoordIndexs(cnk.mPartIndex, 3);
						List<float>	colors0		=geo.Value.GetColors(cnk.mPartIndex, 0);
						List<float>	colors1		=geo.Value.GetColors(cnk.mPartIndex, 1);
						List<float>	colors2		=geo.Value.GetColors(cnk.mPartIndex, 2);
						List<float>	colors3		=geo.Value.GetColors(cnk.mPartIndex, 3);
						List<int>	colIdxs0	=geo.Value.GetColorIndexs(cnk.mPartIndex, 0);
						List<int>	colIdxs1	=geo.Value.GetColorIndexs(cnk.mPartIndex, 1);
						List<int>	colIdxs2	=geo.Value.GetColorIndexs(cnk.mPartIndex, 2);
						List<int>	colIdxs3	=geo.Value.GetColorIndexs(cnk.mPartIndex, 3);
						List<int>	vertCounts	=geo.Value.GetVertCounts(cnk.mPartIndex);

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
			List<MeshLib.SubAnim>	subs	=new List<MeshLib.SubAnim>();
			foreach(KeyValuePair<string, Animation> anim in mAnimations)
			{
				MeshLib.SubAnim	sa	=anim.Value.GetAnims(mGameSkeleton);
				if(sa != null)
				{
					subs.Add(sa);
				}
			}
			MeshLib.Anim	anm	=new MeshLib.Anim(subs);

			anm.SetBoneRefs(mGameSkeleton);

			//chop at content if content is in the path
			if(meshFileName.IndexOf("Content") != -1)
			{
				anm.Name	=meshFileName.Substring(meshFileName.IndexOf("Content"));
			}
			else
			{
				if(meshFileName.IndexOf("\\") != -1)
				{
					anm.Name	="Content" + meshFileName.Substring(meshFileName.LastIndexOf("\\"));
				}
				else
				{
					anm.Name	="Content\\" + meshFileName;
				}
			}
			int	i		=0;
			
			//create anims we can save
			List<MeshLib.Skin>	skinList	=new List<MeshLib.Skin>();
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				List<string>		boneNames	=sk.GetJointNameArray();
				i++;

				MeshLib.Skin	skin	=new MeshLib.Skin();
				skin.SetBindShapeMatrix(sk.GetBindShapeMatrix());
				skin.SetBoneNames(sk.GetJointNameArray());
				skin.SetInverseBindPoses(sk.GetInverseBindPoses());

				mCharacter.AddSkin(skin);
				skinList.Add(skin);
			}

			mAnimLib.AddAnim(anm);


			Dictionary<string, MeshLib.Mesh>	idlist	=new Dictionary<string,MeshLib.Mesh>();

			foreach(MeshConverter mc in mChunks)
			{
				mCharacter.AddMeshPart(mc.GetCharMesh());
				idlist.Add(mc.mGeometryID + Convert.ToString(mc.mPartIndex), mc.GetCharMesh());
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
						idlist[cnk.mGeometryID + Convert.ToString(cnk.mPartIndex)].SetSkin(skinList[i]);
						idlist[cnk.mGeometryID + Convert.ToString(cnk.mPartIndex)].SetSkinIndex(i);
					}
				}
				i++;
			}
		}


		public Collada(COLLADA colMesh,
			GraphicsDevice g,
			ContentManager cm,
			MaterialLib.MaterialLib mlib,
			MeshLib.AnimLib alib,
			MeshLib.Character chr)
		{
			mMatLib		=mlib;
			mAnimLib	=alib;
			mCharacter	=chr;

			foreach(object item in colMesh.Items)
			{
				library_geometries	geoms	=item as library_geometries;
				if(geoms == null)
				{
					continue;
				}
				foreach(object geomItem in geoms.geometry)
				{
					geometry	geom	=geomItem as geometry;
					if(geom == null || geom.Item == null)
					{
						continue;
					}

					mesh	msh	=geom.Item as mesh;
					if(msh == null || msh.Items == null)
					{
						continue;
					}

					foreach(object polyObj in msh.Items)
					{
						polygons	polys	=polyObj as polygons;
						if(polys == null)
						{
							continue;
						}

						float_array	verts	=GetGeometryFloatArrayBySemantic(geom, "VERTEX", 0, polys.material);
						if(verts == null)
						{
							continue;
						}
						
						MeshConverter	cnk	=new MeshConverter(polys.material);

						cnk.CreateBaseVerts(verts);

						cnk.mPartIndex	=-1;
						cnk.SetGeometryID(geom.id);
						
						mChunks.Add(cnk);
					}
				}
			}

			foreach(object item in colMesh.Items)
			{
				library_controllers	conts	=item as library_controllers;
				if(conts == null)
				{
					continue;
				}
				foreach(controller cont in conts.controller)
				{
					skin	sk	=cont.Item as skin;
					if(sk == null)
					{
						continue;
					}

					string	skinSource	=sk.source1.Substring(1);

					foreach(MeshConverter cnk in mChunks)
					{
						if(cnk.mGeometryID == skinSource)
						{
							cnk.AddWeightsToBaseVerts(sk);
						}
					}
				}
			}

			//build skeleton
			mGameSkeleton	=BuildSkeleton(colMesh);

			//bake scene node modifiers into controllers
			foreach(object item in colMesh.Items)
			{
				library_controllers	conts	=item as library_controllers;
				if(conts == null)
				{
					continue;
				}
				foreach(controller cont in conts.controller)
				{
					skin	sk	=cont.Item as skin;
					if(sk == null)
					{
						continue;
					}
					string	skinSource	=sk.source1.Substring(1);
					if(skinSource == null || skinSource == "")
					{
						continue;
					}					

					foreach(object item2 in colMesh.Items)
					{
						library_visual_scenes	lvs	=item2 as library_visual_scenes;
						if(lvs == null)
						{
							continue;
						}
						foreach(visual_scene vs in lvs.visual_scene)
						{
							foreach(node n in vs.node)
							{
								string	nname	=GetNodeNameForInstanceController(n, cont.id);
								if(nname == "")
								{
									continue;
								}
								Matrix	mat	=Matrix.Identity;
								if(!mGameSkeleton.GetMatrixForBone(nname, out mat))
								{
									continue;
								}

								foreach(MeshConverter mc in mChunks)
								{
									if(mc.mGeometryID == skinSource)
									{
										mc.BakeTransformIntoVerts(mat);
									}
								}
							}
						}
					}
				}
			}

			mAnimLib.SetSkeleton(mGameSkeleton);

			foreach(object item in colMesh.Items)
			{
				library_geometries	geoms	=item as library_geometries;
				if(geoms == null)
				{
					continue;
				}
				foreach(object geomItem in geoms.geometry)
				{
					geometry	geom	=geomItem as geometry;
					if(geom == null)
					{
						continue;
					}
					foreach(MeshConverter cnk in mChunks)
					{
						string	name	=cnk.GetName();
						if(cnk.mGeometryID == geom.id)
						{
							List<int>	posIdxs		=GetGeometryIndexesBySemantic(geom, "VERTEX", 0, name);
							float_array	norms		=GetGeometryFloatArrayBySemantic(geom, "NORMAL", 0, name);
							List<int>	normIdxs	=GetGeometryIndexesBySemantic(geom, "NORMAL", 0, name);
							float_array	texCoords0	=GetGeometryFloatArrayBySemantic(geom, "TEXCOORD", 0, name);
							float_array	texCoords1	=GetGeometryFloatArrayBySemantic(geom, "TEXCOORD", 1, name);
							float_array	texCoords2	=GetGeometryFloatArrayBySemantic(geom, "TEXCOORD", 2, name);
							float_array	texCoords3	=GetGeometryFloatArrayBySemantic(geom, "TEXCOORD", 3, name);
							List<int>	texIdxs0	=GetGeometryIndexesBySemantic(geom, "TEXCOORD", 0, name);
							List<int>	texIdxs1	=GetGeometryIndexesBySemantic(geom, "TEXCOORD", 1, name);
							List<int>	texIdxs2	=GetGeometryIndexesBySemantic(geom, "TEXCOORD", 2, name);
							List<int>	texIdxs3	=GetGeometryIndexesBySemantic(geom, "TEXCOORD", 3, name);
							float_array	colors0		=GetGeometryFloatArrayBySemantic(geom, "COLOR", 0, name);
							float_array	colors1		=GetGeometryFloatArrayBySemantic(geom, "COLOR", 1, name);
							float_array	colors2		=GetGeometryFloatArrayBySemantic(geom, "COLOR", 2, name);
							float_array	colors3		=GetGeometryFloatArrayBySemantic(geom, "COLOR", 3, name);
							List<int>	colIdxs0	=GetGeometryIndexesBySemantic(geom, "COLOR", 0, name);
							List<int>	colIdxs1	=GetGeometryIndexesBySemantic(geom, "COLOR", 1, name);
							List<int>	colIdxs2	=GetGeometryIndexesBySemantic(geom, "COLOR", 2, name);
							List<int>	colIdxs3	=GetGeometryIndexesBySemantic(geom, "COLOR", 3, name);
							List<int>	vertCounts	=GetGeometryVertCount(geom, name);

							cnk.AddNormTexByPoly(posIdxs, norms, normIdxs,
								texCoords0, texIdxs0, texCoords1, texIdxs1,
								texCoords2, texIdxs2, texCoords3, texIdxs3,
								colors0, colIdxs0, colors1, colIdxs1,
								colors2, colIdxs2, colors3, colIdxs3,
								vertCounts);

							bool	bPos	=(posIdxs != null && posIdxs.Count > 0);
							bool	bNorm	=(norms != null && norms.count > 0);
							bool	bTex0	=(texCoords0 != null && texCoords0.count > 0);
							bool	bTex1	=(texCoords1 != null && texCoords1.count > 0);
							bool	bTex2	=(texCoords2 != null && texCoords2.count > 0);
							bool	bTex3	=(texCoords3 != null && texCoords3.count > 0);
							bool	bCol0	=(colors0 != null && colors0.count > 0);
							bool	bCol1	=(colors1 != null && colors1.count > 0);
							bool	bCol2	=(colors2 != null && colors2.count > 0);
							bool	bCol3	=(colors3 != null && colors3.count > 0);
							bool	bBone	=false;

							//see if any skins reference this geometry
							foreach(object itm in colMesh.Items)
							{
								library_controllers	conts	=itm as library_controllers;
								if(conts == null)
								{
									continue;
								}
								foreach(controller cont in conts.controller)
								{
									skin	sk	=cont.Item as skin;
									if(sk == null)
									{
										continue;
									}
									string	skinSource	=sk.source1.Substring(1);
									if(skinSource == null || skinSource == "")
									{
										continue;
									}
									if(skinSource == geom.id)
									{
										bBone	=true;
										break;
									}
								}
							}

							cnk.BuildBuffers(g, bPos, bNorm, bBone,
								bBone, bTex0, bTex1, bTex2, bTex3,
								bCol0, bCol1, bCol2, bCol3);
						}
					}
				}
			}

			//create useful anims
			List<MeshLib.SubAnim>	subs	=new List<MeshLib.SubAnim>();
			foreach(object item in colMesh.Items)
			{
				library_animations	anims	=item as library_animations;
				if(anims == null)
				{
					continue;
				}

				foreach(animation anim in anims.animation)
				{
					Animation	an	=new Animation(anim);

					MeshLib.SubAnim	sa	=an.GetAnims(mGameSkeleton);
					if(sa != null)
					{
						subs.Add(sa);
					}
				}
			}
			MeshLib.Anim	anm	=new MeshLib.Anim(subs);

			anm.SetBoneRefs(mGameSkeleton);
			anm.Name	="RenameThis";

			//chop at content if content is in the path
			/*
			if(meshFileName.IndexOf("Content") != -1)
			{
				anm.Name	=meshFileName.Substring(meshFileName.IndexOf("Content"));
			}
			else
			{
				if(meshFileName.IndexOf("\\") != -1)
				{
					anm.Name	="Content" + meshFileName.Substring(meshFileName.LastIndexOf("\\"));
				}
				else
				{
					anm.Name	="Content\\" + meshFileName;
				}
			}*/
			int	i		=0;
			
			//create anims we can save
			List<MeshLib.Skin>	skinList	=new List<MeshLib.Skin>();
			foreach(object item in colMesh.Items)
			{
				library_controllers	conts	=item as library_controllers;
				if(conts == null)
				{
					continue;
				}
				foreach(controller cont in conts.controller)
				{
					skin	sk	=cont.Item as skin;
					if(sk == null)
					{
						continue;
					}
					string	skinSource	=sk.source1.Substring(1);
					if(skinSource == null || skinSource == "")
					{
						continue;
					}
					MeshLib.Skin	skin	=new MeshLib.Skin();

					Matrix	mat	=Matrix.Identity;

					GetMatrixFromString(sk.bind_shape_matrix, out mat);

					skin.SetBindShapeMatrix(mat);

					string	jointSrc	="";
					string	invSrc		="";
					foreach(InputLocal inp in sk.joints.input)
					{
						if(inp.semantic == "JOINT")
						{
							jointSrc	=inp.source.Substring(1);
						}
						else if(inp.semantic == "INV_BIND_MATRIX")
						{
							invSrc	=inp.source.Substring(1);
						}
					}

					foreach(source src in sk.source)
					{
						if(src.id == jointSrc)
						{
							Name_array	na	=src.Item as Name_array;

							skin.SetBoneNames(na.Values);
						}
						else if(src.id == invSrc)
						{
							float_array	ma	=src.Item as float_array;

							List<Matrix>	mats	=GetMatrixListFromFA(ma);

							skin.SetInverseBindPoses(mats);
						}
					}
					mCharacter.AddSkin(skin);
					skinList.Add(skin);

					//set mesh pointers
					foreach(MeshConverter mc in mChunks)
					{
						if(mc.mGeometryID == sk.source1.Substring(1))
						{
							MeshLib.Mesh	msh	=mc.GetCharMesh();
							msh.SetSkin(skin);
							msh.SetSkinIndex(skinList.IndexOf(skin));
						}
					}
				}
			}

			mAnimLib.AddAnim(anm);

			Dictionary<string, MeshLib.Mesh>	idlist	=new Dictionary<string,MeshLib.Mesh>();

			foreach(MeshConverter mc in mChunks)
			{
				mCharacter.AddMeshPart(mc.GetCharMesh());
				idlist.Add(mc.mGeometryID + mc.GetName(), mc.GetCharMesh());
			}

			//set skin pointers in meshes

			//needs to be indexed for file IO
			/*
			i	=0;
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				foreach(MeshConverter cnk in mChunks)
				{
					if(cnk.mGeometryID == sk.GetGeometryID())
					{
						idlist[cnk.mGeometryID + Convert.ToString(cnk.mPartIndex)].SetSkin(skinList[i]);
						idlist[cnk.mGeometryID + Convert.ToString(cnk.mPartIndex)].SetSkinIndex(i);
					}
				}
				i++;
			}*/
		}


		string GetNodeNameForInstanceController(node n, string ic)
		{
			if(n.instance_controller != null)
			{
				foreach(instance_controller inst in n.instance_controller)
				{
					if(inst.url.Substring(1) == ic)
					{
						return	n.name;
					}
				}
			}

			if(n.node1 == null)
			{
				return	"";
			}

			//check kids
			foreach(node kid in n.node1)
			{
				string	ret	=GetNodeNameForInstanceController(kid, ic);
				if(ret != "")
				{
					return	ret;
				}
			}
			return	"";
		}


		public List<int> GetGeometryIndexesBySemantic(geometry geom, string sem, int set, string material)
		{
			List<int>	ret	=new List<int>();

			mesh	msh	=geom.Item as mesh;
			if(msh == null || msh.Items == null)
			{
				return	null;
			}

			string	key		="";
			int		idx		=-1;
			int		ofs		=-1;
			foreach(object polObj in msh.Items)
			{
				polygons	polys	=polObj as polygons;
				if(polys == null || polys.Items == null || polys.material != material)
				{
					continue;
				}

				for(int i=0;i < polys.input.Length;i++)
				{
					InputLocalOffset	inp	=polys.input[i];
					if(inp.semantic == sem && set == (int)inp.set)
					{
						//strip #
						key		=inp.source.Substring(1);
						idx		=i;
						ofs		=(int)inp.offset;
						break;
					}
				}

				if(key == "")
				{
					continue;
				}

				foreach(object polyObj in polys.Items)
				{
					string	pols	=polyObj as string;
					Debug.Assert(pols != null);

					int	numSem	=polys.input.Length;
					int	curIdx	=0;

					string	[]tokens	=pols.Split(' ', '\n');
					foreach(string tok in tokens)
					{
						if(curIdx == ofs)
						{
							int	val	=0;
							if(int.TryParse(tok, out val))
							{
								ret.Add(val);
							}
						}
						curIdx++;
						if(curIdx >= numSem)
						{
							curIdx	=0;
						}
					}
				}
			}
			return	ret;
		}


		public List<int> GetGeometryVertCount(geometry geom, string material)
		{
			List<int>	ret	=new List<int>();

			mesh	msh	=geom.Item as mesh;
			if(msh == null || msh.Items == null)
			{
				return	null;
			}
			foreach(object polObj in msh.Items)
			{
				polygons	polys	=polObj as polygons;
				if(polys == null || polys.Items == null || polys.material != material)
				{
					continue;
				}

				foreach(object polyObj in polys.Items)
				{
					string	pols	=polyObj as string;
					Debug.Assert(pols != null);

					int	numSem	=polys.input.Length;

					string	[]tokens	=pols.Split(' ', '\n');
					ret.Add(tokens.Length / numSem);
				}
			}
			return	ret;
		}


		public float_array GetGeometryFloatArrayBySemantic(geometry geom, string sem, int set, string material)
		{
			mesh	msh	=geom.Item as mesh;
			if(msh == null)
			{
				return	null;
			}

			string	key		="";
			int		idx		=-1;
			int		ofs		=-1;
			foreach(object polObj in msh.Items)
			{
				polygons	polys	=polObj as polygons;
				if(polys == null || polys.Items == null || polys.material != material)
				{
					continue;
				}

				for(int i=0;i < polys.input.Length;i++)
				{
					InputLocalOffset	inp	=polys.input[i];
					if(inp.semantic == sem && set == (int)inp.set)
					{
						//strip #
						key		=inp.source.Substring(1);
						idx		=i;
						ofs		=(int)inp.offset;
						break;
					}
				}
			}

			if(key == "")
			{
				return	null;
			}

			//check vertices
			if(msh.vertices != null && msh.vertices.id == key)
			{
				key	=msh.vertices.input[0].source.Substring(1);
			}

			for(int j=0;j < msh.source.Length;j++)
			{
				float_array	verts	=msh.source[j].Item as float_array;
				if(verts == null || msh.source[j].id != key)
				{
					continue;
				}
				return	verts;
			}

			return	null;
		}


		public void LoadAnim(string path)
		{
			FileStream	file	=OpenTitleFile(path,
				FileMode.Open, FileAccess.Read);
			
			file.Seek(0, SeekOrigin.Begin);
			XmlReader	r	=XmlReader.Create(file);
			mAnimations.Clear();
//			LoadAnimations(r);

			//create useful anims
			List<MeshLib.SubAnim>	subs	=new List<MeshLib.SubAnim>();
			foreach(KeyValuePair<string, Animation> anim in mAnimations)
			{
				MeshLib.SubAnim	sa	=anim.Value.GetAnims(mGameSkeleton);
				if(sa != null)
				{
					subs.Add(sa);
				}
			}
			MeshLib.Anim	anm	=new MeshLib.Anim(subs);	

			anm.SetBoneRefs(mGameSkeleton);
			
			//chop at content if content is in the path
			if(path.IndexOf("Content") != -1)
			{
				anm.Name	=path.Substring(path.IndexOf("Content"));
			}
			else
			{
				if(path.IndexOf("\\") != -1)
				{
					anm.Name	="Content" + path.Substring(path.LastIndexOf("\\"));
				}
				else
				{
					anm.Name	="Content\\" + path;
				}
			}
			mAnimLib.AddAnim(anm);
			file.Close();
		}


		MeshLib.Skeleton BuildSkeleton(COLLADA colMesh)
		{
			MeshLib.Skeleton	ret	=new MeshLib.Skeleton();

			foreach(object item2 in colMesh.Items)
			{
				library_visual_scenes	lvs	=item2 as library_visual_scenes;
				if(lvs == null)
				{
					continue;
				}
				foreach(visual_scene vs in lvs.visual_scene)
				{
					foreach(node n in vs.node)
					{
						MeshLib.GSNode	gsnRoot	=new MeshLib.GSNode();

						BuildSkeleton(n, out gsnRoot);

						ret.AddRoot(gsnRoot);
					}
				}
			}
			return	ret;
		}


		public MeshLib.KeyFrame GetKeyFromCNode(node n)
		{
			MeshLib.KeyFrame	key	=new MeshLib.KeyFrame();

			if(n.Items == null)
			{
				return	key;
			}

			Matrix	mat	=Matrix.Identity;
			for(int i=0;i < n.Items.Length;i++)
			{
				if(n.ItemsElementName[i] == ItemsChoiceType2.rotate)
				{
					rotate	rot	=n.Items[i] as rotate;

					Debug.Assert(rot != null);

					Vector3	axis	=Vector3.Zero;
					axis.X	=(float)rot.Values[0];
					axis.Y	=(float)rot.Values[1];
					axis.Z	=(float)rot.Values[2];

					mat	=Matrix.CreateFromAxisAngle(axis, (float)rot.Values[3])
						* mat;
				}
				else if(n.ItemsElementName[i] == ItemsChoiceType2.translate)
				{
					TargetableFloat3	trans	=n.Items[i] as TargetableFloat3;

					Vector3	t	=Vector3.Zero;
					t.X	=(float)trans.Values[0];
					t.Y	=(float)trans.Values[1];
					t.Z	=(float)trans.Values[2];

					mat	=Matrix.CreateTranslation(t)
						* mat;
				}
				else if(n.ItemsElementName[i] == ItemsChoiceType2.scale)
				{
					TargetableFloat3	scl	=n.Items[i] as TargetableFloat3;

					Vector3	t	=Vector3.Zero;
					t.X	=(float)scl.Values[0];
					t.Y	=(float)scl.Values[1];
					t.Z	=(float)scl.Values[2];

					mat	=Matrix.CreateScale(t)
						* mat;
				}
			}

			mat.Decompose(out key.mScale, out key.mRotation, out key.mPosition);

			return	key;
		}


		void BuildSkeleton(node n, out MeshLib.GSNode gsn)
		{
			gsn	=new MeshLib.GSNode();

			gsn.SetName(n.name);
			gsn.SetKey(GetKeyFromCNode(n));

			if(n.node1 == null)
			{
				return;
			}

			foreach(node child in n.node1)
			{
				MeshLib.GSNode	kid	=new MeshLib.GSNode();

				BuildSkeleton(child, out kid);

				gsn.AddChild(kid);
			}
		}


		public MeshLib.Skeleton	BuildGameSkeleton()
		{
			MeshLib.Skeleton	ret	=new MeshLib.Skeleton();

			foreach(KeyValuePair<string, SceneNode> sn in mRootNodes)
			{
				MeshLib.GSNode	n;

				sn.Value.AddToGameSkeleton(out n);

				ret.AddRoot(n);
			}
			return	ret;
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
//			LoadAnimations(r);

			//find effects
			file.Seek(0, SeekOrigin.Begin);
			r	=XmlReader.Create(file);
			LoadColladaEffects(r);

			file.Close();
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


		public static List<Matrix> GetMatrixListFromFA(float_array fa)
		{
			List<Matrix>	ret	=new List<Matrix>();

			Debug.Assert(fa.count % 16 == 0);

			for(int i=0;i < (int)fa.count;i+=16)
			{
				Matrix	mat	=new Matrix();

				mat.M11	=(float)fa.Values[i + 0];
				mat.M21	=(float)fa.Values[i + 1];
				mat.M31	=(float)fa.Values[i + 2];
				mat.M41	=(float)fa.Values[i + 3];
				mat.M12	=(float)fa.Values[i + 4];
				mat.M22	=(float)fa.Values[i + 5];
				mat.M32	=(float)fa.Values[i + 6];
				mat.M42	=(float)fa.Values[i + 7];
				mat.M13	=(float)fa.Values[i + 8];
				mat.M23	=(float)fa.Values[i + 9];
				mat.M33	=(float)fa.Values[i + 10];
				mat.M43	=(float)fa.Values[i + 11];
				mat.M14	=(float)fa.Values[i + 12];
				mat.M24	=(float)fa.Values[i + 13];
				mat.M34	=(float)fa.Values[i + 14];
				mat.M44	=(float)fa.Values[i + 15];

				ret.Add(mat);
			}

			return	ret;
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
		/*
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
		}*/


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