using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

namespace Collada
{
	public class Material
	{
		public	string	mID, mInstanceEffect;
	}


	public class MeshMaterials
	{
		public List<uint> mPolyPositionIndices;
		public List<uint> mPolyNormalIndices;
		public List<uint> mPolyUVIndices;

		public MeshMaterials()
		{
			mPolyPositionIndices	=new List<uint>();
			mPolyNormalIndices		=new List<uint>();
			mPolyUVIndices			=new List<uint>();
		}
	}


	public class Mesh
	{
		public	List<Vector3>		mPositions;
		public	List<Vector3>		mNormals;
		public	List<Vector2>		mUVs;
		public	List<MeshMaterials>	mMeshMaterials;

		public Mesh()
		{
			mPositions		=new List<Vector3>();
			mNormals		=new List<Vector3>();
			mUVs			=new List<Vector2>();
			mMeshMaterials	=new List<MeshMaterials>();
		}
	}


	public class Geometry
	{
		public	string	mID, mName;

		public	List<Mesh>	mMeshes;

		public Geometry()
		{
			mMeshes	=new List<Mesh>();
		}
	}


	public class Controller
	{
		public	string	mID, mSkinSource, mSourceID;
		public	Matrix	mBindShapeMatrix;

		public	List<string>	mBoneNames;
		public	List<float>		mSkinWeightsArray;
		public	List<Matrix>	mSkinMatricesArray;
		public	List<int>		mVertWeightCount;	//number of bone influences per vertex
		public	List<int>		mVertWeightBones;	//indexes of bones that correspond to the weight

		public Controller()
		{
			mBoneNames			=new List<string>();
			mSkinWeightsArray	=new List<float>();
			mSkinMatricesArray	=new List<Matrix>();
			mVertWeightCount	=new List<int>();
			mVertWeightBones	=new List<int>();
		}
	}


	public class InstanceMaterial
	{
		public	string	mSymbol, mTarget, mBindSemantic, mBindTarget;
	}


	public class SceneNode
	{
		public	string			mID, mName, mSID, mType;
		public	Vector3			mTranslation, mScale;
		public	Vector4			mRotX, mRotY, mRotZ;

		public	List<SceneNode>	mChildren;

		//skin instance stuff
		public	string	mInstanceControllerURL;
		public	string	mSkeleton;

		public	List<InstanceMaterial>	mBindMaterials;

		public SceneNode()
		{
			mChildren		=new List<SceneNode>();
			mBindMaterials	=null;
		}
	}

	class Collada
	{
		//data from collada
		private	List<Material>		mMaterials;
		private	List<SceneNode>		mRootNodes;
		private	List<Controller>	mControllers;
		private	List<Geometry>		mGeometries;

		//debug draw data
		VertexBuffer			mVerts;
		List<IndexBuffer>		mIndexs;

		public Collada(string meshFileName, GraphicsDevice g)
		{
			mMaterials		=new List<Material>();
			mRootNodes		=new List<SceneNode>();
			mControllers	=new List<Controller>();
			mGeometries		=new List<Geometry>();
			mIndexs			=new List<IndexBuffer>();

			Load(meshFileName);

			ConvertData(g);
		}


		private	void	ConvertData(GraphicsDevice g)
		{
			mVerts	=new VertexBuffer(g, 16 * mGeometries[0].mMeshes[0].mPositions.Count, BufferUsage.WriteOnly);

			VertexPositionColor[] vpc	=new VertexPositionColor[mGeometries[0].mMeshes[0].mPositions.Count];

			//copy in the goodies
			for(int i=0;i < mGeometries[0].mMeshes[0].mPositions.Count;i++)
			{
				vpc[i].Position	=mGeometries[0].mMeshes[0].mPositions[i];
				vpc[i].Color	=Color.Beige;
			}

			mVerts.SetData<VertexPositionColor>(vpc);

			//load the various index buffs
			for(int i=0;i < mGeometries[0].mMeshes[0].mMeshMaterials.Count;i++)
			{
				UInt32[] indexL	=null;
				indexL	=new UInt32[mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices.Count];

				//copy in indexs
				for(int j=0;j < mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices.Count;j++)
				{
					indexL[j]	=mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices[j];
				}

				IndexBuffer	idx	=new IndexBuffer(g, mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices.Count * 4, BufferUsage.WriteOnly, IndexElementSize.ThirtyTwoBits);
				idx.SetData<UInt32>(indexL, 0, mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices.Count);

				mIndexs.Add(idx);
			}
		}


		public void Draw(GraphicsDevice g, Effect fx)
		{
			g.Vertices[0].SetSource(mVerts, 0, 16);

			for(int i=0;i < mIndexs.Count;i++)
			{
				g.Indices	=mIndexs[i];
				
				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0,
						mGeometries[0].mMeshes[0].mPositions.Count,
						0,
						mGeometries[0].mMeshes[0].mMeshMaterials[i].mPolyPositionIndices.Count / 3);
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

			string matID	="";

			//read materials
			while(r.Read())
			{
				Console.WriteLine("Name:" + r.Name);

				if(r.Name == "library_materials")
				{
					break;
				}

				if(r.Name == "material")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						matID	=r.Value;
					}
				}

				if(r.Name == "instance_effect")
				{
					r.MoveToFirstAttribute();

					Material m			=new Material();
					m.mID				=matID;
					m.mInstanceEffect	=r.Value;

					mMaterials.Add(m);
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
				if(r.Name == "library_visual_scenes")
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
						SceneNode	root	=new SceneNode();

						LoadNode(r, root);

						mRootNodes.Add(root);
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

			Controller c	=null;

			//read controller stuff
			while(r.Read())
			{
				if(r.Name == "library_controllers")
				{
					break;
				}
				else if(r.Name == "controller")
				{
					if(r.AttributeCount > 0)
					{
						c	=new Controller();

						r.MoveToFirstAttribute();
						c.mID	=r.Value;
					}
					else
					{
						mControllers.Add(c);
					}
				}
				else if(r.Name == "skin")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();

						c.mSkinSource	=r.Value;
					}
				}
				else if(r.Name == "bind_shape_matrix")
				{
					//skip to the guts
					r.Read();

					GetMatrixFromString(r.Value,out c.mBindShapeMatrix);
				}
				else if(r.Name == "source")
				{
					if(r.AttributeCount > 0)
					{
						if(r.Name == "id")
						{
							c.mSourceID	=r.Value;
						}
					}
				}
				else if(r.Name == "vertex_weights")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						int cnt;
						int.TryParse(r.Value,out cnt);

						//skip junk
						while(r.Name != "vcount")
						{
							r.Read();
						}

						r.Read();

						string[] tokens	=r.Value.Split(' ');

						//copy vertex weight counts
						for(int i=0;i < cnt;i++)
						{
							int numInfluences;

							int.TryParse(tokens[i],out numInfluences);

							c.mVertWeightCount.Add(numInfluences);
						}

						//skip junk
						while(r.Name != "v")
						{
							r.Read();
						}

						r.Read();

						tokens	=null;
						tokens	=r.Value.Split(' ');

						//copy vertex weight bones
						for(int i=0;i < tokens.Length;i++)
						{
							int boneIndex;

							int.TryParse(tokens[i],out boneIndex);

							c.mVertWeightBones.Add(boneIndex);
						}
					}
				}
				else if(r.Name == "IDREF_array")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						r.MoveToNextAttribute();
						int nCnt;
						int.TryParse(r.Value,out nCnt);

						//skip to the guts
						r.Read();

						string[] tokens	=r.Value.Split(' ');

						//copy bonenames
						for(int i=0;i < nCnt;i++)
						{
							c.mBoneNames.Add(tokens[i]);
						}
					}
				}
				else if(r.Name == "float_array")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						r.MoveToNextAttribute();
						int nCnt;
						int.TryParse(r.Value,out nCnt);
						//skip to the guts
						r.Read();

						string[] tokens	=r.Value.Split(' ');

						//figure out where this needs to go
						while(r.Name != "param")
						{
							r.Read();
						}

						r.MoveToFirstAttribute();
						if(r.Name == "type")
						{
							if(r.Value == "float")
							{
								//guessing this is skin weights
								for(int i=0;i < nCnt;i++)
								{
									float weight;

									Single.TryParse(tokens[i],out weight);
									c.mSkinWeightsArray.Add(weight);
								}
							}
							else if(r.Value == "float4x4")
							{
								string matString	="";

								//guessing this is skin matrices
								for(int i=0;i < nCnt;i++)
								{
									matString	+=tokens[i];
									matString	+=" ";

									//make a mat every 16
									if((i & 0xf) == 0xf)
									{
										Matrix m	=new Matrix();

										GetMatrixFromString(matString,out m);
										c.mSkinMatricesArray.Add(m);

										matString	="";
									}
								}
							}
						}
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

			LoadMaterials(r);
			LoadVisualScenes(r);
			LoadControllers(r);

			//find geometry
			while(r.Read())
			{
				if(r.Name == "library_geometries")
				{
					break;
				}
			}

			Geometry	g	=null;
			Mesh		m	=null;

			//read controller stuff
			while(r.Read())
			{
				if(r.Name == "library_geometries")
				{
					break;
				}
				else if(r.Name == "geometry")
				{
					if(r.AttributeCount > 0)
					{
						//alloc a new geom
						g	=new Geometry();
					}
					else
					{
						mGeometries.Add(g);
					}
				}
				else if(r.Name == "mesh" && r.NodeType == XmlNodeType.Element)
				{
					m	=new Mesh();

					//read positions
					LoadFloatArray(r, m.mPositions);

					//read normals
					LoadFloatArray(r, m.mNormals);

					//read uvs
					LoadFloatArray(r, m.mUVs);

					//read up the poly lists
					LoadMeshMaterials(r, m);

					g.mMeshes.Add(m);
				}
			}
		}


		//this is lazy and ignores the channel info and
		//offsets, probably will only work with daz
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


		public void LoadNode(XmlReader r,SceneNode n)
		{
			r.MoveToFirstAttribute();

			int attcnt	=r.AttributeCount;

			while(attcnt > 0)
			{
				//look for valid attributes for nodes
				if(r.Name == "id")
				{
					n.mID	=r.Value;
				}
				else if(r.Name == "name")
				{
					n.mName	=r.Value;
				}
				else if(r.Name == "sid")
				{
					n.mSID	=r.Value;
				}
				else if(r.Name == "type")
				{
					n.mType	=r.Value;
				}

				attcnt--;
				r.MoveToNextAttribute();
			}
			
			while(r.Read())
			{
				if(r.Name == "translate")
				{
					int attcnt2	=r.AttributeCount;

					if(attcnt2 > 0)
					{
						//skip to the next element, the actual value
						r.Read();

						GetVectorFromString(r.Value,out n.mTranslation);
					}
				}
				else if(r.Name == "scale")
				{
					int attcnt2	=r.AttributeCount;

					if(attcnt2 > 0)
					{
						//skip to the next element, the actual value
						r.Read();

						GetVectorFromString(r.Value, out n.mScale);
					}
				}
				else if(r.Name == "instance_material")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();

						InstanceMaterial	m	=new InstanceMaterial();

						if(r.Name == "symbol")
						{
							m.mSymbol	=r.Value;
						}
						else if(r.Name == "target")
						{
							m.mTarget	=r.Value;
						}

						r.MoveToNextAttribute();

						if(r.Name == "symbol")
						{
							m.mSymbol	=r.Value;
						}
						else if(r.Name == "target")
						{
							m.mTarget	=r.Value;
						}

						//skip to bind
						r.Read();
						r.Read();

						r.MoveToFirstAttribute();

						if(r.Name == "semantic")
						{
							m.mBindSemantic	=r.Value;
						}
						else if(r.Name == "target")
						{
							m.mBindTarget	=r.Value;
						}

						r.MoveToNextAttribute();

						if(r.Name == "semantic")
						{
							m.mBindSemantic	=r.Value;
						}
						else if(r.Name == "target")
						{
							m.mBindTarget	=r.Value;
						}

						n.mBindMaterials.Add(m);
					}
				}
				else if(r.Name == "instance_controller")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();

						n.mInstanceControllerURL	=r.Value;
					}
				}
				else if(r.Name == "skeleton")
				{
					r.Read();
					n.mSkeleton	=r.Value;

					//probably a good time to allocate the bind materials
					n.mBindMaterials	=new List<InstanceMaterial>();
				}
				else if(r.Name == "rotate")
				{
					int attcnt2	=r.AttributeCount;

					if(attcnt2 > 0)
					{
						r.MoveToFirstAttribute();

						string	axis	=r.Value;

						r.Read();	//skip to vec4 value

						//check the sid for which axis
						if(axis == "rotateX")
						{
							GetVectorFromString(r.Value, out n.mRotX);
						}
						else if(axis == "rotateY")
						{
							GetVectorFromString(r.Value, out n.mRotY);
						}
						else if(axis == "rotateZ")
						{
							GetVectorFromString(r.Value, out n.mRotZ);
						}
					}
				}
				else if(r.Name == "node")
				{
					int attcnt2	=r.AttributeCount;

					if(attcnt2 > 0)
					{
						SceneNode child	=new SceneNode();

						LoadNode(r, child);

						n.mChildren.Add(child);
					}
					else
					{
						return;
					}
				}
			}
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



/*
//Move to fist element
r.MoveToElement();
				
Console.WriteLine("XmlReader Properties Test");
Console.WriteLine("===================");
				
//Read this element's properties and display them on console
Console.WriteLine("Name:" + r.Name);
Console.WriteLine("Base URI:" + r.BaseURI);
Console.WriteLine("Local Name:" + r.LocalName);
Console.WriteLine("Attribute Count:" + r.AttributeCount.ToString());
Console.WriteLine("Depth:" + r.Depth.ToString());
//Console.WriteLine("Line Number:" + r.LineNumber.ToString());
Console.WriteLine("Node Type:" + r.NodeType.ToString());
Console.WriteLine("Attribute Count:" + r.Value.ToString());*/
