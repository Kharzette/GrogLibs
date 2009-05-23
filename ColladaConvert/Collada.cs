using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	class Collada
	{
		//data from collada
		private	Dictionary<string, Material>		mMaterials		=new Dictionary<string,Material>();
		private Dictionary<string, LibImage>		mImages			=new Dictionary<string,LibImage>();
		private	Dictionary<string, SceneNode>		mRootNodes		=new Dictionary<string,SceneNode>();
		private	Dictionary<string, Controller>		mControllers	=new Dictionary<string,Controller>();
		private	Dictionary<string, Geometry>		mGeometries		=new Dictionary<string,Geometry>();
		private Dictionary<string, Animation>		mAnimations		=new Dictionary<string,Animation>();
		private Dictionary<string, ColladaEffect>	mColladaEffects	=new Dictionary<string,ColladaEffect>();

		//actual useful data for the game
		private List<DrawChunk>	mChunks	=new List<DrawChunk>();

		private Dictionary<string, Texture2D>	mTextures	=new Dictionary<string,Texture2D>();


		public Collada(string meshFileName, GraphicsDevice g)
		{
			Load(meshFileName);

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				List<float> verts	=geo.Value.GetBaseVerts();

				DrawChunk	cnk	=new DrawChunk();

				cnk.CreateBaseVerts(verts);

				cnk.SetGeometryID(geo.Key);

				mChunks.Add(cnk);
			}

			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				foreach(DrawChunk cnk in mChunks)
				{
					if(cnk.mGeometryID == sk.GetGeometryID())
					{
						cnk.AddWeightsToBaseVerts(sk);
					}
				}
			}

			foreach(KeyValuePair<string, Geometry> geo in mGeometries)
			{
				//find the matching drawchunk
				foreach(DrawChunk cnk in mChunks)
				{
					if(cnk.mGeometryID == geo.Key)
					{
						List<int>	posIdxs		=geo.Value.GetPositionIndexs();
						List<float>	norms		=geo.Value.GetNormals();
						List<int>	normIdxs	=geo.Value.GetNormalIndexs();
						List<float>	texCoords	=geo.Value.GetTexCoords();
						List<int>	texIdxs		=geo.Value.GetTexCoordIndexs();
						List<int>	vertCounts	=geo.Value.GetVertCounts();

						cnk.AddNormTexByPoly(posIdxs, norms, normIdxs,
							texCoords, texIdxs, vertCounts);

						cnk.BuildBuffers(g);
					}
				}
			}

//			BuildBuffers(g);

//			BuildBones(g);

//			BuildWeights(g);

			foreach(KeyValuePair<string, LibImage> li in mImages)
			{
				string	path	=li.Value.mPath;
				if(path.StartsWith("file://"))
				{
					path	=path.Remove(0, 7);
				}
				Texture2D	tex	=Texture2D.FromFile(g, path);
				mTextures.Add(li.Value.mName, tex);
			}
		}
		
		//put all the wieghts and indices into the vbuffer
		/*
		public void BuildWeights(GraphicsDevice g)
		{
			foreach(DrawChunk dc in mChunks)
			{
				//get the geom id
				string	key	=dc.mGeometryID;

				//add a # on the front
				key	=key.Insert(0, "#");

				//match this to the right controller
				Controller	cnt	=null;
				foreach(KeyValuePair<string, Controller> cont in mControllers)
				{
					if(cont.Value.mSkin.mSource == key)
					{
						//found it
						cnt	=cont.Value;
						break;
					}
				}

				//grab the source key for the joints array
				string	keyName	="";
				for(int i=0;i < cnt.mSkin.mJoints.mInputs.Count;i++)
				{
					if(cnt.mSkin.mJoints.mInputs[i].mSemantic == "JOINT")
					{
						keyName	=cnt.mSkin.mJoints.mInputs[i].mSource;
					}
				}

				//strip off the # in the key
				keyName	=keyName.Substring(1);

				//replace -joints with -Weights, kinda lame
				keyName	=keyName.Replace("-Joints", "-Weights");

				//find the weights
				Source	src	=cnt.mSkin.mSources[keyName];
			}
		}*/

		
		private void	BuildBones(GraphicsDevice g)
		{
			//build bones for each controller
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				cont.Value.BuildBones(g, mRootNodes);
			}

			//copy bones into drawchunks
			foreach(DrawChunk dc in mChunks)
			{
				string	key	=dc.mGeometryID;

				//add a # on the front
				key	=key.Insert(0, "#");

				//match this to the right controller
				foreach(KeyValuePair<string, Controller> cont in mControllers)
				{
					Skin	sk	=cont.Value.GetSkin();
					if(sk.GetGeometryID() == key)
					{
						cont.Value.CopyBonesTo(dc.mBones);
						break;
					}
				}
			}
		}
		
		/*
		private	void	BuildBuffers(GraphicsDevice g)
		{
			foreach(KeyValuePair<string, Geometry> geom in mGeometries)
			{
				//pass down the geometry id for the chunkage
				geom.Value.BuildBuffers(g, geom.Key, mChunks);
			}

			//fix up materials
			foreach(DrawChunk dc in mChunks)
			{
				if(dc.mTexName != null)
				{
					//find in fx
					string	fxname	=dc.mTexName + "-fx";

					if(mColladaEffects.ContainsKey(fxname))
					{
						dc.mTexName		=mColladaEffects[fxname].mTextureID;
						dc.mTexChannel	=mColladaEffects[fxname].mChannel;

						//chop off -image on the end
						if(dc.mTexName != null && dc.mTexName.EndsWith("-image"))
						{
							dc.mTexName	=dc.mTexName.Remove(dc.mTexName.IndexOf("-image"));
						}
					}
				}
			}
		}*/


		//copies bones into the shader
		public void UpdateBones(GraphicsDevice g, Effect fx)
		{
			foreach(DrawChunk dc in mChunks)
			{
				//some chunks are never really drawn
				if(dc.mBones != null)
				{
					fx.Parameters["mBones"].SetValue(dc.mBones);
				}
			}
		}


		public void Draw(GraphicsDevice g, Effect fx)
		{
			for(int i=0;i < mChunks.Count;i++)
//			foreach(DrawChunk dc in mChunks)
			{
				DrawChunk	dc	=mChunks[i];
				g.Vertices[0].SetSource(dc.mVerts, 0, dc.mVertSize);
				g.Indices			=dc.mIndexs;
				g.VertexDeclaration	=dc.mVD;

				Matrix	loc	=Matrix.Identity;

				if(dc.mTexName != null)
				{
					if(mTextures.ContainsKey(dc.mTexName))
					{
						string	tex	="mTexture" + dc.mTexChannel;
						fx.Parameters[tex].SetValue(mTextures[dc.mTexName]);
					}
				}
				/*
				if(i==0)
				{
					loc	=Matrix.CreateTranslation(new Vector3(30.0f, 0.0f, 0.0f));
				}
				else if(i==1)
				{
					loc	=Matrix.CreateTranslation(new Vector3(0.0f, 30.0f, 0.0f));
				}
				else if(i == 2)
				{
					loc	=Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 30.0f));
				}*/

				fx.Parameters["mLocal"].SetValue(loc);

				fx.Begin();
				foreach(EffectPass pass in fx.CurrentTechnique.Passes)
				{
					pass.Begin();

					g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0,
						dc.mNumVerts,
						0,
						dc.mNumTriangles);

					pass.End();
				}
				fx.End();
			}
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

						Geometry	g	=new Geometry();
						g.Load(r);

						mGeometries.Add(geomID, g);
					}
				}
			}
		}


		//this is lazy and ignores the channel info and
		//offsets, probably will only work with daz
		/*
		public void LoadMeshMaterials(XmlReader r, Mesh m)
		{
			MeshMaterials	mm	=null;

			//triangulation buffers
			uint[] vq	=new uint[10];
			uint[] nq	=new uint[10];
			uint[] uq	=new uint[10];

			while(r.Read())
			{
				if(r.Name == "mesh" && r.NodeType == XmlNodeType.EndElement)
				{
					break;
				}
				else if(r.Name == "polygons")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();

						int nCnt;
						int.TryParse(r.Value, out nCnt);

						mm	=new MeshMaterials();
					}
					else
					{
						m.mMeshMaterials.Add(mm);
					}
				}
				else if(r.Name == "p" && r.NodeType == XmlNodeType.Element)
				{
					//skip to guts
					r.Read();

					string[] tokens	=r.Value.Split(' ');

					Debug.Assert(tokens.Length == 12);

					for(int j=0, i=0;i < tokens.Length;i+=3, j++)
					{
						uint	vert, norm, uv;

						uint.TryParse(tokens[i], out vert);
						uint.TryParse(tokens[i+1], out norm);
						uint.TryParse(tokens[i+2], out uv);

						vq[j]	=vert;
						nq[j]	=norm;
						uq[j]	=uv;
					}

					//comes out in quads sometimes, triangulate
					for(int i=1;i < ((tokens.Length / 3) - 1);i++)
					{
						mm.mPolyPositionIndices.Add(vq[0]);
						mm.mPolyNormalIndices.Add(nq[0]);
						mm.mPolyUVIndices.Add(uq[0]);
						mm.mPolyPositionIndices.Add(vq[((i + 1) % (tokens.Length / 3))]);
						mm.mPolyNormalIndices.Add(nq[((i + 1) % (tokens.Length / 3))]);
						mm.mPolyUVIndices.Add(uq[((i + 1) % (tokens.Length / 3))]);
						mm.mPolyPositionIndices.Add(vq[i]);
						mm.mPolyNormalIndices.Add(nq[i]);
						mm.mPolyUVIndices.Add(uq[i]);
					}
				}
			}
		}*/


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

			Single.TryParse(tokens[0], out vec.X);
			Single.TryParse(tokens[1], out vec.Y);
			Single.TryParse(tokens[2], out vec.Z);
		}


		public static void GetVectorFromString(string str, out Vector4 vec)
		{
			string[] tokens	=str.Split(' ');

			Single.TryParse(tokens[0],out vec.X);
			Single.TryParse(tokens[1],out vec.Y);
			Single.TryParse(tokens[2],out vec.Z);
			Single.TryParse(tokens[3],out vec.W);
		}


		public static void GetMatrixFromString(string str, out Matrix mat)
		{
			string[] tokens	=str.Split(' ');

			Single.TryParse(tokens[0],out mat.M11);
			Single.TryParse(tokens[1],out mat.M12);
			Single.TryParse(tokens[2],out mat.M13);
			Single.TryParse(tokens[3],out mat.M14);
			Single.TryParse(tokens[4],out mat.M21);
			Single.TryParse(tokens[5],out mat.M22);
			Single.TryParse(tokens[6],out mat.M23);
			Single.TryParse(tokens[7],out mat.M24);
			Single.TryParse(tokens[8],out mat.M31);
			Single.TryParse(tokens[9],out mat.M32);
			Single.TryParse(tokens[10],out mat.M33);
			Single.TryParse(tokens[11],out mat.M34);
			Single.TryParse(tokens[12],out mat.M41);
			Single.TryParse(tokens[13],out mat.M42);
			Single.TryParse(tokens[14],out mat.M43);
			Single.TryParse(tokens[15],out mat.M44);
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