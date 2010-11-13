using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using MeshLib;


namespace ColladaConvert
{
	internal class ColladaFileUtils
	{
		internal static COLLADA DeSerializeCOLLADA(string path)
		{
			FileStream		fs	=new FileStream(path, FileMode.Open, FileAccess.Read);
			XmlSerializer	xs	=new XmlSerializer(typeof(COLLADA));

			COLLADA	ret	=xs.Deserialize(fs) as COLLADA;

			return	ret;
		}


		//loads an animation into an existing anim lib
		internal static bool LoadAnim(string path, AnimLib alib)
		{
			COLLADA	colladaFile	=DeSerializeCOLLADA(path);
			Skeleton	skel	=BuildSkeleton(colladaFile);

			if(alib.CheckSkeleton(skel))
			{
				List<SubAnim>	subs	=GetSubAnimList(colladaFile, alib.GetSkeleton());
				Anim	anm	=new Anim(subs);

				anm.SetBoneRefs(alib.GetSkeleton());

				anm.Name	=NameFromPath(path);
				alib.AddAnim(anm);

				return	true;
			}
			return	false;
		}


		internal static Character LoadCharacter(string					path,
												GraphicsDevice			gd,
												MaterialLib.MaterialLib	matLib,
												AnimLib					alib)
		{
			COLLADA	colladaFile	=DeSerializeCOLLADA(path);

			Character	chr	=new Character(matLib, alib);

			List<MeshConverter>	chunks	=GetMeshChunks(colladaFile, true);

			AddVertexWeightsToChunks(colladaFile, chunks);

			//build skeleton
			Skeleton	skel	=BuildSkeleton(colladaFile);

			//bake scene node modifiers into controllers
			BakeSceneNodesIntoVerts(colladaFile, skel, chunks);

			alib.SetSkeleton(skel);

			BuildFinalVerts(colladaFile, gd, chunks);

			//create useful anims
			List<SubAnim>	subs	=GetSubAnimList(colladaFile, skel);
			Anim	anm	=new Anim(subs);

			anm.SetBoneRefs(skel);
			anm.Name	=NameFromPath(path);
			alib.AddAnim(anm);

			CreateSkins(colladaFile, chr, chunks);

			foreach(MeshConverter mc in chunks)
			{
				chr.AddMeshPart(mc.GetConvertedMesh());
			}

			return	chr;
		}


		internal static StaticMeshObject LoadStatic(string					path,
													GraphicsDevice			gd,
													MaterialLib.MaterialLib	matLib)
		{
			COLLADA	colladaFile	=DeSerializeCOLLADA(path);

			StaticMeshObject	smo		=new StaticMeshObject(matLib);
			List<MeshConverter>	chunks	=GetMeshChunks(colladaFile, false);

			BuildFinalVerts(colladaFile, gd, chunks);

			foreach(MeshConverter mc in chunks)
			{
				smo.AddMeshPart(mc.GetConvertedMesh());
			}

			return	smo;
		}


		static void CreateSkins(COLLADA				colladaFile,
								Character			chr,
								List<MeshConverter>	chunks)
		{
			List<Skin>	skinList	=new List<Skin>();
			foreach(object item in colladaFile.Items)
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
					Skin	skin	=new Skin();

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
					chr.AddSkin(skin);
					skinList.Add(skin);

					//set mesh pointers
					foreach(MeshConverter mc in chunks)
					{
						if(mc.mGeometryID == sk.source1.Substring(1))
						{
							SkinnedMesh	msh	=mc.GetConvertedMesh() as SkinnedMesh;
							msh.SetSkin(skin);
							msh.SetSkinIndex(skinList.IndexOf(skin));
						}
					}
				}
			}
		}


		static List<SubAnim> GetSubAnimList(COLLADA colladaFile, Skeleton skel)
		{
			//create useful anims
			List<SubAnim>	subs	=new List<SubAnim>();
			foreach(object item in colladaFile.Items)
			{
				library_animations	anims	=item as library_animations;
				if(anims == null)
				{
					continue;
				}

				foreach(animation anim in anims.animation)
				{
					Animation	an	=new Animation(anim);

					SubAnim	sa	=an.GetAnims(skel);
					if(sa != null)
					{
						subs.Add(sa);
					}
				}
			}
			return	subs;
		}


		static void BuildFinalVerts(COLLADA				colladaFile,
									GraphicsDevice		gd,
									List<MeshConverter>	chunks)
		{
			foreach(object item in colladaFile.Items)
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
					foreach(MeshConverter cnk in chunks)
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
							foreach(object itm in colladaFile.Items)
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

							cnk.BuildBuffers(gd, bPos, bNorm, bBone,
								bBone, bTex0, bTex1, bTex2, bTex3,
								bCol0, bCol1, bCol2, bCol3);
						}
					}
				}
			}
		}


		static void BakeSceneNodesIntoVerts(COLLADA				colladaFile,
											Skeleton			skel,
											List<MeshConverter>	chunks)
		{
			//bake scene node modifiers into controllers
			foreach(object item in colladaFile.Items)
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

					foreach(object item2 in colladaFile.Items)
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
								if(!skel.GetMatrixForBone(nname, out mat))
								{
									continue;
								}

								foreach(MeshConverter mc in chunks)
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
		}


		static void AddVertexWeightsToChunks(COLLADA colladaFile, List<MeshConverter> chunks)
		{
			foreach(object item in colladaFile.Items)
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

					foreach(MeshConverter cnk in chunks)
					{
						if(cnk.mGeometryID == skinSource)
						{
							cnk.AddWeightsToBaseVerts(sk);
						}
					}
				}
			}
		}


		static List<MeshConverter> GetMeshChunks(COLLADA colladaFile, bool bSkinned)
		{
			List<MeshConverter>	chunks	=new List<MeshConverter>();

			foreach(object item in colladaFile.Items)
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

						cnk.CreateBaseVerts(verts, bSkinned);

						cnk.mPartIndex	=-1;
						cnk.SetGeometryID(geom.id);
						
						chunks.Add(cnk);
					}
				}
			}
			return	chunks;
		}


		static string GetNodeNameForInstanceController(node n, string ic)
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


		static List<int> GetGeometryIndexesBySemantic(geometry geom, string sem, int set, string material)
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


		static List<int> GetGeometryVertCount(geometry geom, string material)
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


		static float_array GetGeometryFloatArrayBySemantic(geometry geom, string sem, int set, string material)
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


		static Skeleton BuildSkeleton(COLLADA colMesh)
		{
			Skeleton	ret	=new Skeleton();

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
						GSNode	gsnRoot	=new GSNode();

						BuildSkeleton(n, out gsnRoot);

						ret.AddRoot(gsnRoot);
					}
				}
			}
			return	ret;
		}


		static KeyFrame GetKeyFromCNode(node n)
		{
			KeyFrame	key	=new KeyFrame();

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
					axis.X	=rot.Values[0];
					axis.Y	=rot.Values[1];
					axis.Z	=rot.Values[2];

					mat	=Matrix.CreateFromAxisAngle(axis, rot.Values[3])
						* mat;
				}
				else if(n.ItemsElementName[i] == ItemsChoiceType2.translate)
				{
					TargetableFloat3	trans	=n.Items[i] as TargetableFloat3;

					Vector3	t	=Vector3.Zero;
					t.X	=trans.Values[0];
					t.Y	=trans.Values[1];
					t.Z	=trans.Values[2];

					mat	=Matrix.CreateTranslation(t)
						* mat;
				}
				else if(n.ItemsElementName[i] == ItemsChoiceType2.scale)
				{
					TargetableFloat3	scl	=n.Items[i] as TargetableFloat3;

					Vector3	t	=Vector3.Zero;
					t.X	=scl.Values[0];
					t.Y	=scl.Values[1];
					t.Z	=scl.Values[2];

					mat	=Matrix.CreateScale(t)
						* mat;
				}
			}

			mat.Decompose(out key.mScale, out key.mRotation, out key.mPosition);

			return	key;
		}


		static void BuildSkeleton(node n, out GSNode gsn)
		{
			gsn	=new GSNode();

			gsn.SetName(n.name);
			gsn.SetKey(GetKeyFromCNode(n));

			if(n.node1 == null)
			{
				return;
			}

			foreach(node child in n.node1)
			{
				GSNode	kid	=new GSNode();

				BuildSkeleton(child, out kid);

				gsn.AddChild(kid);
			}
		}


		internal static List<Matrix> GetMatrixListFromFA(float_array fa)
		{
			List<Matrix>	ret	=new List<Matrix>();

			Debug.Assert(fa.count % 16 == 0);

			for(int i=0;i < (int)fa.count;i+=16)
			{
				Matrix	mat	=new Matrix();

				mat.M11	=fa.Values[i + 0];
				mat.M21	=fa.Values[i + 1];
				mat.M31	=fa.Values[i + 2];
				mat.M41	=fa.Values[i + 3];
				mat.M12	=fa.Values[i + 4];
				mat.M22	=fa.Values[i + 5];
				mat.M32	=fa.Values[i + 6];
				mat.M42	=fa.Values[i + 7];
				mat.M13	=fa.Values[i + 8];
				mat.M23	=fa.Values[i + 9];
				mat.M33	=fa.Values[i + 10];
				mat.M43	=fa.Values[i + 11];
				mat.M14	=fa.Values[i + 12];
				mat.M24	=fa.Values[i + 13];
				mat.M34	=fa.Values[i + 14];
				mat.M44	=fa.Values[i + 15];

				ret.Add(mat);
			}

			return	ret;
		}


		internal static void GetMatrixFromString(string str, out Matrix mat)
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


		static string NameFromPath(string path)
		{
			int	lastSlash	=path.LastIndexOf('\\');
			if(lastSlash == -1)
			{
				lastSlash	=0;
			}
			else
			{
				lastSlash++;
			}

			int	extension	=path.LastIndexOf('.');

			string	name	=path.Substring(lastSlash, extension - lastSlash);

			return	name;
		}
	}
}