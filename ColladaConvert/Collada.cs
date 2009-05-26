using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

				//convert coordinate system to ours
				ConvertCoordinateSystemMAX(verts);

				cnk.CreateBaseVerts(verts);

				cnk.SetGeometryID(geo.Key);

				mChunks.Add(cnk);
			}

			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				Skin	sk	=cont.Value.GetSkin();

				cont.Value.ChangeCoordinateSystemMAX(mRootNodes);

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
						List<float>	texCoords	=geo.Value.GetTexCoords(0);
						List<int>	texIdxs		=geo.Value.GetTexCoordIndexs(0);
						List<int>	vertCounts	=geo.Value.GetVertCounts();

						cnk.AddNormTexByPoly(posIdxs, norms, normIdxs,
							texCoords, texIdxs, vertCounts);

						cnk.BuildBuffers(g);
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

			BuildBones(g);
		}

		
		private void	BuildBones(GraphicsDevice g)
		{
			foreach(KeyValuePair<string, Controller> cont in mControllers)
			{
				cont.Value.BuildBones(g, mRootNodes);
			}

			//copy bones into drawchunks
			foreach(DrawChunk dc in mChunks)
			{
				string	key	=dc.mGeometryID;

				//match this to the right controller
				foreach(KeyValuePair<string, Controller> cont in mControllers)
				{
					Skin	sk	=cont.Value.GetSkin();
					Matrix	bsm	=sk.GetBindShapeMatrix();
					if(sk.GetGeometryID() == key)
					{
						cont.Value.CopyBonesTo(ref dc.mBones);
						dc.mBindShapeMatrix	=bsm;
						break;
					}
				}
			}
		}
		

		public void Draw(GraphicsDevice g, Effect fx)
		{
			for(int i=0;i < mChunks.Count;i++)
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

				dc.UpdateBones(fx);

//				fx.Parameters["mLocal"].SetValue(dc.mBindShapeMatrix);
//				fx.Parameters["mLocal"].SetValue(mRootNodes["Box01"].GetMatrix());
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
			string[] tokens	=str.Split(' ', '\n', '\t');

			int	tokIdx	=0;
			
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
			/*
			while(!Single.TryParse(tokens[tokIdx++],out mat.M11));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M12));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M13));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M14));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M21));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M22));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M23));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M24));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M31));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M32));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M33));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M34));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M41));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M42));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M43));
			while(!Single.TryParse(tokens[tokIdx++],out mat.M44));
			*/
		}


		//change a floatarray of vector3's from max's
		//coordinate system to ours
		private void ConvertCoordinateSystemMAX(List<float> verts)
		{
			Debug.Assert(verts.Count % 3 == 0);
			return;

			for(int i=0;i < verts.Count / 3;i++)
			{
				float	temp	=verts[i * 3 + 1];

				//negate x, and swap y and z
				verts[i * 3]		=-verts[i * 3];
				verts[i * 3 + 1]	=verts[i * 3 + 2];
				verts[i * 3 + 2]	=temp;
			}
		}


		public static Matrix ConvertMatrixCoordinateSystemMAX(Matrix inMat)
		{
			Matrix	convertMat	=Matrix.Identity;
			convertMat.M11	=-1;
			convertMat.M22	=0;
			convertMat.M23	=1;
			convertMat.M32	=1;
			convertMat.M33	=0;

			Matrix	outMat	=inMat * convertMat;
//			Matrix	outMat	=convertMat * inMat;
//			Matrix	outMat	=inMat;

//			Matrix	outMat;

			return	outMat;

			outMat.M11	=inMat.M11;
			outMat.M12	=inMat.M12;
			outMat.M13	=inMat.M13;
			outMat.M14	=inMat.M14;
			outMat.M21	=inMat.M21;
			outMat.M22	=inMat.M22;
			outMat.M23	=inMat.M23;
			outMat.M24	=inMat.M24;
			outMat.M31	=inMat.M31;
			outMat.M32	=inMat.M32;
			outMat.M33	=inMat.M33;
			outMat.M34	=inMat.M34;
			outMat.M41	=-inMat.M41;
			outMat.M42	=inMat.M43;
			outMat.M43	=inMat.M42;
			outMat.M44	=inMat.M44;

			return	outMat;
		}


		public static Matrix ConvertMatrixCoordinateSystemSceneNode(Matrix inMat)
		{
			Matrix	convertMat	=Matrix.Identity;
			convertMat.M11	=1;
			convertMat.M22	=0;
			convertMat.M23	=1;
			convertMat.M32	=-1;
			convertMat.M33	=0;

//			Matrix	outMat	=inMat * convertMat;
//			Matrix	outMat	=convertMat * inMat;
			Matrix	outMat	=inMat;

			return	outMat;
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