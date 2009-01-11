using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

namespace Collada
{
	public class Material
	{
		public string	mName, mInstanceEffect;
	}


	public class LibImage
	{
		public string	mName, mPath;
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


	public class Vertices
	{
		public List<Input>	mInputs	=new List<Input>();

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "vertices")
				{
					return;
				}
			}
		}
	}


	public class Polygons
	{
		public string		mMaterial;
		public int			mCount;

		public List<Input>					mInputs	=new List<Input>();
		public Dictionary<int, List<int>>	mIndexs	=new Dictionary<int,List<int>>();

		public void BuildBuffers(GraphicsDevice g, Mesh m, List<DrawChunk> dcl)
		{
			DrawChunk	dc	=new DrawChunk();

			dc.mVD	=GetVertexDeclaration(g);

			Type	t	=GetVertexType();

			//calc size of index buffer
			//this is tricky because we have to triangulate
			int	size		=0;
			int numPoints	=0;	//number of polygon points
			foreach(KeyValuePair<int, List<int>> idx in mIndexs)
			{
				int polyPoints		=idx.Value.Count / mInputs.Count;
				numPoints			+=polyPoints;
				dc.mNumTriangles	=(numPoints - 3) + 1;
				size	+=dc.mNumTriangles * 3;
			}

			dc.mIndexs	=new IndexBuffer(g, 2 * size, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

			ushort	[]indices	=new ushort[size];

			Type	vertType	=GetVertexType();
			Array	vertArray	=Array.CreateInstance(vertType, numPoints);

			dc.mNumVerts		=numPoints;
			dc.mVertSize		=VertexTypes.GetSizeForType(t);

			//there's a big difference in the way the data is stored
			//from Collada, and what XNA needs.  Collada feeds us
			//per polygon indices that reach into seperate lists of
			//positions, normals, texture coordinates etc...  Because
			//these lists are per polygon, the same vertex can have
			//a different set of UV coordinates or normals based on
			//however the artist set it up.  So we have to draw from
			//the per polygon data, and combine each polygon vertex
			//into a single long list, then check it for duplicates
			//and remap the indices and vertex weights.  It's a mess.

			//get the max offset
			int	maxOffset	=0;
			foreach(Input inp in mInputs)
			{
				if(inp.mOffset > maxOffset)
				{
					maxOffset	=inp.mOffset;
				}
			}

			//loop through the inputs, yankin out the values
			int	curPoly	=0;	//current polygon index
			int	curIdx	=0;	//current vertex index in the polygon
			int	curVI	=0;	//current vertex index in the master list
			int	ind		=0;	//current index index.... get it?
			foreach(Input inp in mInputs)
			{
				curPoly	=0;
				curVI	=0;
				ind		=0;
				while(curPoly < mCount)
				{
					curIdx			=inp.mOffset;
					int	startVert	=curVI;	//a zero offset for triangulation
					int startIdx	=ind;	//a zero offset for reversing winding
					while(mIndexs[curPoly].Count > curIdx)
					{
						//triangulation
						if(ind > 2)
						{
							indices[ind++]	=(ushort)(startVert);
							indices[ind++]	=(ushort)(curVI - 1);
						}
						//find the input source
						string	sourceKey	=inp.mSource;
						if(sourceKey.StartsWith("#"))
						{
							sourceKey	=sourceKey.Remove(0, 1);
						}
						if(m.mSources.ContainsKey(sourceKey))
						{
							Source src	=m.mSources[sourceKey];

							//get accessor information
							if(src.mAccessor.mSource.Contains(sourceKey))
							{
								if(src.mAccessor.mStride == 3)
								{
									if(src.mAccessor.mParams[0].mType == "float")
									{
										string	arr	=sourceKey + "-array";
										//going to assume these are vector3
										FloatArray fa	=src.mFloats[arr];

										//get the value
										Vector3	vec;
										int		flIdx	=mIndexs[curPoly][curIdx];

										vec.X	=fa.mFloats[flIdx];
										vec.Y	=fa.mFloats[flIdx + 1];
										vec.Z	=fa.mFloats[flIdx + 2];

										if(inp.mSemantic == "NORMAL")
										{
											Collada.SetValue(vertArray, curVI, "Normal" + inp.mSet, vec);
										}
										else if(inp.mSemantic == "TEXCOORD")
										{
											Collada.SetValue(vertArray, curVI, "TexCoord" + inp.mSet, vec);
										}
										else if(inp.mSemantic == "COLOR")
										{
											Collada.SetValue(vertArray, curVI, "Color" + inp.mSet, vec);
										}
										curIdx	+=(maxOffset + 1);
										indices[ind++]	=(ushort)curVI++;
									}
								}
								else if(src.mAccessor.mStride == 2)
								{
									if(src.mAccessor.mParams[0].mType == "float")
									{
										string	arr	=sourceKey + "-array";
										FloatArray fa	=src.mFloats[arr];

										//get the value
										Vector2	vec;
										int		flIdx	=mIndexs[curPoly][curIdx];

										vec.X	=fa.mFloats[flIdx];
										vec.Y	=fa.mFloats[flIdx + 1];

										if(inp.mSemantic == "TEXCOORD")
										{
											Collada.SetValue(vertArray, curVI, "TexCoord" + inp.mSet, vec);
										}
										curIdx	+=(maxOffset + 1);
										indices[ind++]	=(ushort)curVI++;
									}
								}
								else if(src.mAccessor.mStride == 4)
								{
									if(src.mAccessor.mParams[0].mType == "double")
									{
										string	arr	=sourceKey + "-array";
										FloatArray fa	=src.mFloats[arr];

										//get the value
										Vector4	vec;
										int		flIdx	=mIndexs[curPoly][curIdx];

										vec.X	=fa.mFloats[flIdx];
										vec.Y	=fa.mFloats[flIdx + 1];
										vec.Z	=fa.mFloats[flIdx + 2];
										vec.W	=fa.mFloats[flIdx + 3];

										Color	col	=new Color(vec);

										if(inp.mSemantic == "COLOR")
										{
											Collada.SetValue(vertArray, curVI, "Color" + inp.mSet, col);
										}
										curIdx	+=(maxOffset + 1);
										indices[ind++]	=(ushort)curVI++;
									}
								}
							}
						}
						else if(m.mVerts.ContainsKey(sourceKey))
						{
							//verts are just a redirection
							//I'm going to assume here that there
							//is only one input
							string redirKey	=m.mVerts[sourceKey].mInputs[0].mSource;
							if(redirKey.StartsWith("#"))
							{
								redirKey	=redirKey.Remove(0, 1);
							}
							//now search in sources
							if(m.mSources.ContainsKey(redirKey))
							{
								Source src	=m.mSources[redirKey];

								//get accessor information
								if(src.mAccessor.mSource.Contains(redirKey))
								{
									if(src.mAccessor.mStride == 3)
									{
										if(src.mAccessor.mParams[0].mType == "float")
										{
											string	arr	=redirKey + "-array";
											//going to assume these are vector3
											FloatArray fa	=src.mFloats[arr];

											//get the value
											Vector3	vec;
											int		flIdx	=mIndexs[curPoly][curIdx] * 3;

											vec.X	=fa.mFloats[flIdx];
											vec.Y	=fa.mFloats[flIdx + 1];
											vec.Z	=fa.mFloats[flIdx + 2];

											if(inp.mSemantic == "VERTEX")
											{
												Collada.SetValue(vertArray, curVI, "Position" + inp.mSet, vec);
											}
											else if(inp.mSemantic == "NORMAL")
											{
												Collada.SetValue(vertArray, curVI, "Normal" + inp.mSet, vec);
											}
											else if(inp.mSemantic == "TEXCOORD")
											{
												Collada.SetValue(vertArray, curVI, "TexCoord" + inp.mSet, vec);
											}
											else if(inp.mSemantic == "COLOR")
											{
												Collada.SetValue(vertArray, curVI, "Color" + inp.mSet, vec);
											}
											curIdx	+=(maxOffset + 1);
											indices[ind++]	=(ushort)curVI++;
										}
									}
								}
							}
						}
					}
					//flip winding order
					List<ushort>	reverse	=new List<ushort>();
					for(int j=startIdx;j < ind;j++)
					{
						reverse.Add(indices[j]);
					}
					int k=0;
					for(int j=ind - 1;j >= startIdx;j--,k++)
					{
						indices[j]	=reverse[k];
					}
					curPoly++;
				}
			}
			dc.mVerts	=new VertexBuffer(g,
				numPoints * VertexTypes.GetSizeForType(t),
				BufferUsage.WriteOnly);

			//take the built array and feed it into
			//a vertex buffer and pray
			//dc.mVerts.SetData<t>(vertArray);
			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "SetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {t});

			typedMethod.Invoke(dc.mVerts, new object[] {vertArray});

			dc.mIndexs.SetData<ushort>(indices);

			dcl.Add(dc);
		}


		public int GetNumVertices(Mesh m)
		{
			//find the verts
			foreach(Input inp in mInputs)
			{
				if(inp.mSemantic == "VERTEX")
				{
					string	key	=inp.mSource;
					if(key.StartsWith("#"))
					{
						key	=key.Remove(0, 1);
					}
					if(m.mVerts.ContainsKey(key))
					{
						//assuming one input
						string	redirKey	=m.mVerts[key].mInputs[0].mSource;
						if(redirKey.StartsWith("#"))
						{
							redirKey	=redirKey.Remove(0, 1);
						}
						string arrKey	=redirKey + "-array";
						if(m.mSources.ContainsKey(redirKey))
						{
							if(m.mSources[redirKey].mFloats.ContainsKey(arrKey))
							{
								return	m.mSources[redirKey].mFloats[arrKey].mCount / 3;
							}
						}
					}
				}
			}
			return	0;
		}

		public Type GetVertexType()
		{
			//count up the number of different elements
			int	pos		=0;
			int	norm	=0;
			int	tex		=0;
			int	color	=0;

			foreach(Input inp in mInputs)
			{
				switch(inp.mSemantic)
				{
					case "VERTEX":
						pos++;
						break;
					case "NORMAL":
						norm++;
						break;
					case "TEXCOORD":
						tex++;
						break;
					case "COLOR":
						color++;
						break;
					default:
						Debug.WriteLine("Warning! unknown input semantic!");
						break;
				}
			}
			return	VertexTypes.GetMatch(pos, norm, tex, color);

		}
		public VertexDeclaration GetVertexDeclaration(GraphicsDevice g)
		{
			Type	t	=GetVertexType();
			return	VertexTypes.GetVertexDeclarationForType(g, t);
			/*
			VertexElement	[]ve	=new VertexElement[mInputs.Count];

			short	sizeSoFar	=0;
			for(int i=0;i < mInputs.Count;i++)
			{
				//keep track of the number of times
				//the various channels are used
				byte	vertNum	=0;
				byte	normNum	=0;
				byte	texNum	=0;
				byte	colNum	=0;

				switch(mInputs[i].mSemantic)
				{
					case "VERTEX":
						ve[i]	=new VertexElement(0, sizeSoFar,
							VertexElementFormat.Vector3,
							VertexElementMethod.Default,
							VertexElementUsage.Position, vertNum);
						vertNum++;
						sizeSoFar	+=12;
						break;
					case "NORMAL":
						ve[i]	=new VertexElement(0, sizeSoFar,
							VertexElementFormat.Vector3,
							VertexElementMethod.Default,
							VertexElementUsage.Normal, vertNum);
						normNum++;
						sizeSoFar	+=12;
						break;
					case "TEXCOORD":
						ve[i]	=new VertexElement(0, sizeSoFar,
							VertexElementFormat.Vector2,
							VertexElementMethod.Default,
							VertexElementUsage.TextureCoordinate, vertNum);
						texNum++;
						sizeSoFar	+=8;
						break;
					case "COLOR":
						ve[i]	=new VertexElement(0, sizeSoFar,
							VertexElementFormat.Vector4,
							VertexElementMethod.Default,
							VertexElementUsage.Color, vertNum);
						colNum++;
						sizeSoFar	+=16;
						break;
					default:
						Debug.WriteLine("Warning! unknown input semantic!");
						break;
				}
			}
			VertexDeclaration	vd	=new VertexDeclaration(g, ve);

			return	vd;*/
		}

		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;
			if(attCnt > 0)
			{
				r.MoveToFirstAttribute();
				while(attCnt > 0)
				{
					if(r.Name == "material")
					{
						mMaterial	=r.Value;
					}
					else if(r.Name == "count")
					{
						int.TryParse(r.Value, out mCount);
					}
					r.MoveToNextAttribute();
					attCnt--;
				}

				int	curPoly	=0;

				while(r.Read())
				{
					if(r.NodeType == XmlNodeType.Whitespace)
					{
						continue;	//skip whitey
					}
					if(r.Name == "input")
					{
						Input	inp	=new Input();
						inp.Load(r);
						mInputs.Add(inp);
					}
					else if(r.Name == "p")
					{
						if(r.NodeType == XmlNodeType.EndElement)
						{
							continue;
						}
						List<int>	ind	=new List<int>();

						//go to values
						r.Read();

						string	[]tokens	=r.Value.Split(' ', '\n');
						foreach(string tok in tokens)
						{
							int	i;

							if(int.TryParse(tok, out i))
							{
								ind.Add(i);
							}
						}
						mIndexs.Add(curPoly, ind);
						curPoly++;
					}
					else if(r.Name == "polygons")
					{
						return;
					}
				}
			}
		}
	}


	public class Mesh
	{
		public Dictionary<string, Source>	mSources	=new Dictionary<string,Source>();
		public Dictionary<string, Vertices>	mVerts		=new Dictionary<string,Vertices>();
		public List<Polygons>				mPolys		=new List<Polygons>();

		public Mesh()	{}

		public void BuildBuffers(GraphicsDevice g, List<DrawChunk> dc)
		{
			foreach(Polygons p in mPolys)
			{
				p.BuildBuffers(g, this, dc);
			}
		}
		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "source")
				{
					r.MoveToFirstAttribute();

					string	srcID	=r.Value;

					Source	src	=new Source();
					src.Load(r);

					mSources.Add(srcID, src);
				}
				else if(r.Name == "mesh")
				{
					return;
				}
				else if(r.Name == "vertices")
				{
					r.MoveToFirstAttribute();
					string	vertID	=r.Value;

					Vertices	vert	=new Vertices();
					vert.Load(r);
					mVerts.Add(vertID, vert);					
				}
				else if(r.Name == "polygons")
				{
					Polygons	pol	=new Polygons();
					pol.Load(r);

					mPolys.Add(pol);
				}
			}
		}
	}


	public class Geometry
	{
		public string	mName;

		public	List<Mesh>	mMeshes	=new List<Mesh>();

		public Geometry()	{}

		public void BuildBuffers(GraphicsDevice g, List<DrawChunk> dc)
		{
			foreach(Mesh m in mMeshes)
			{
				m.BuildBuffers(g, dc);
			}
		}
		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "mesh")
				{
					Mesh	m	=new Mesh();
					m.Load(r);
					mMeshes.Add(m);
				}
				else if(r.Name == "geometry")
				{
					return;
				}
			}
		}
	}


	public class NameArray
	{
		public List<string>	mNames	=new List<string>();

		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			int nCnt;
			int.TryParse(r.Value,out nCnt);

			//skip to the guts
			r.Read();

			string[] tokens	=r.Value.Split(' ', '\n');

			//copynames
			foreach(string tok in tokens)
			{
				if(tok == "")
				{
					continue;	//skip empties
				}
				mNames.Add(tok);
			}
			r.Read();
		}
	}


	public class FloatArray
	{
		public List<float>	mFloats	=new List<float>();
		public int			mCount;

		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			int.TryParse(r.Value,out mCount);

			//skip to the guts
			r.Read();

			string[] tokens	=r.Value.Split(' ', '\n');

			//dump into floats
			foreach(string flt in tokens)
			{
				float	f;
				
				if(Single.TryParse(flt, out f))
				{
					mFloats.Add(f);
				}
			}
			r.Read();
		}
	}


	public class Param
	{
		public string	mType, mName;

		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;
			if(attCnt > 0)
			{
				r.MoveToFirstAttribute();
				while(attCnt > 0)
				{
					if(r.Name == "name")
					{
						mName	=r.Value;
					}
					else if(r.Name == "type")
					{
						mType	=r.Value;
					}
					r.MoveToNextAttribute();
					attCnt--;
				}
			}
		}
	}


	public class Accessor
	{
		public string		mSource;
		public int			mCount;
		public int			mStride;
		public List<Param>	mParams	=new List<Param>();

		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;
			if(attCnt > 0)
			{
				r.MoveToFirstAttribute();
				while(attCnt > 0)
				{
					if(r.Name == "source")
					{
						mSource	=r.Value;
					}
					else if(r.Name == "count")
					{
						int.TryParse(r.Value, out mCount);
					}
					else if(r.Name == "stride")
					{
						int.TryParse(r.Value, out mStride);
					}
					r.MoveToNextAttribute();
					attCnt--;
				}
			}
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "accessor")
				{
					return;
				}
				else if(r.Name == "param")
				{
					Param	p	=new Param();
					p.Load(r);
					mParams.Add(p);
				}
			}
		}
	}


	public class Source
	{
		public Dictionary<string, NameArray>	mNames		=new Dictionary<string,NameArray>();
		public Dictionary<string, FloatArray>	mFloats		=new Dictionary<string,FloatArray>();
		public Accessor							mAccessor;

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "source")
				{
					return;
				}
				else if(r.Name == "Name_array")
				{
					if(r.AttributeCount > 0)
					{
						NameArray	na	=new NameArray();
						r.MoveToFirstAttribute();
						string naID	=r.Value;
						na.Load(r);
						mNames.Add(naID, na);
					}
				}
				else if(r.Name == "float_array")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						string faID	=r.Value;
						FloatArray	fa	=new FloatArray();
						fa.Load(r);
						mFloats.Add(faID, fa);
					}
				}
				else if(r.Name == "accessor")
				{
					mAccessor	=new Accessor();
					mAccessor.Load(r);
				}
			}
		}
	}


	public class Input
	{
		public string	mSemantic, mSource;
		public int		mOffset, mSet;

		public Type GetTypeForSemantic()
		{
			switch(mSemantic)
			{
				case "VERTEX":
					return	typeof(Vector3);
				case "NORMAL":
					return	typeof(Vector3);
				case "TEXCOORD":
					return	typeof(Vector2);
				case "COLOR":
					return	typeof(Vector4);
				default:
					Debug.WriteLine("Warning! unknown input semantic!");
					break;
			}
			return	typeof(object);	//wild guess?
		}
		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;

			r.MoveToFirstAttribute();
			while(attCnt > 0)
			{
				if(r.Name == "semantic")
				{
					mSemantic	=r.Value;
				}
				else if(r.Name == "offset")
				{
					int.TryParse(r.Value, out mOffset);
				}
				else if(r.Name == "source")
				{
					mSource	=r.Value;
				}
				else if(r.Name == "set")	//by Set!
				{
					int.TryParse(r.Value, out mSet);
				}
				r.MoveToNextAttribute();
				attCnt--;
			}
		}
	}

	public class Joints
	{
		List<Input>	mInputs	=new List<Input>();

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "joints")
				{
					return;
				}
			}
		}
	}


	public class VertexWeights
	{
		public List<Input>	mInputs				=new List<Input>();
		public List<int>	mVertWeightCount	=new List<int>();	//number of bone influences per vertex
		public List<int>	mVertWeightBones	=new List<int>();	//indexes of bones that correspond to the weight

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "vcount")
				{
					if(r.NodeType != XmlNodeType.EndElement)
					{
						r.Read();

						string[] tokens	=r.Value.Split(' ');

						//copy vertex weight counts
						foreach(string tok in tokens)
						{
							int numInfluences;

							if(int.TryParse(tok, out numInfluences))
							{
								mVertWeightCount.Add(numInfluences);
							}
						}
					}
				}
				else if(r.Name == "v")
				{
					if(r.NodeType != XmlNodeType.EndElement)
					{
						r.Read();

						string	[]tokens	=r.Value.Split(' ');

						//copy vertex weight bones
						foreach(string tok in tokens)
						{
							int boneIndex;

							if(int.TryParse(tok, out boneIndex))
							{
								mVertWeightBones.Add(boneIndex);
							}
						}
					}
				}
				else if(r.Name == "vertex_weights")
				{
					return;
				}
			}
		}
	}


	public class Skin
	{
		public string						mSource;
		public Matrix						mBindShapeMatrix;
		public Dictionary<string, Source>	mSources	=new Dictionary<string,Source>();
		public Joints						mJoints;
		public VertexWeights				mVertWeights;

		public void Load(XmlReader r)
		{
			if(r.AttributeCount > 0)
			{
				r.MoveToFirstAttribute();

				mSource	=r.Value;
			}

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "skin")
				{
					return;
				}
				else if(r.Name == "bind_shape_matrix")
				{
					if(r.NodeType == XmlNodeType.Element)
					{
						//skip to the guts
						r.Read();

						Collada.GetMatrixFromString(r.Value, out mBindShapeMatrix);
					}
				}
				else if(r.Name == "joints")
				{
					mJoints	=new Joints();
					mJoints.Load(r);
				}
				else if(r.Name == "source")
				{
					Source	src	=new Source();
					r.MoveToFirstAttribute();
					string	srcID	=r.Value;
					src.Load(r);
					mSources.Add(srcID, src);
				}
				else if(r.Name == "vertex_weights")
				{
					mVertWeights	=new VertexWeights();
					mVertWeights.Load(r);
				}
			}
		}
	}


	public class Controller
	{
		public Skin	mSkin;

		public void Load(XmlReader r)
		{
			//read controller stuff
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "controller")
				{
					return;
				}
				else if(r.Name == "skin")
				{
					mSkin	=new Skin();
					mSkin.Load(r);
				}
			}
		}
	}


	public class InstanceMaterial
	{
		public	string	mSymbol, mTarget, mBindSemantic, mBindTarget;
	}


	public class SceneNode
	{
		public	string			mName, mSID, mType;
		public	Vector3			mTranslation, mScale;
		public	Vector4			mRotX, mRotY, mRotZ;

		public	Dictionary<string, SceneNode>	mChildren	=new Dictionary<string, SceneNode>();

		//skin instance stuff
		public string	mInstanceControllerURL;
		public string	mSkeleton;
		public string	mInstanceGeometryURL;

		public	List<InstanceMaterial>	mBindMaterials;

		public SceneNode()
		{
			mBindMaterials	=new List<InstanceMaterial>();
		}
	}

	public class Channel
	{
		public string	mSource;
		public string	mTarget;
	}

	public class Sampler
	{
		List<Input>		mInputs	=new List<Input>();

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "sampler")
				{
					return;
				}
			}
		}
	}

	//animations on individual controllers
	public class SubAnimation
	{
		public Dictionary<string, Source>	mSources	=new Dictionary<string,Source>();
		public Dictionary<string, Sampler>	mSamplers	=new Dictionary<string,Sampler>();
		public List<Channel>				mChannels	=new List<Channel>();

		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "source")
				{
					Source	src	=new Source();
					r.MoveToFirstAttribute();
					string	srcID	=r.Value;
					src.Load(r);
					mSources.Add(srcID, src);
				}
				else if(r.Name == "sampler")
				{
					Sampler	samp	=new Sampler();
					r.MoveToFirstAttribute();
					string	sampID	=r.Value;
					samp.Load(r);
					mSamplers.Add(sampID, samp);
				}
				else if(r.Name == "channel")
				{
					Channel	chan	=new Channel();

					int	attCnt	=r.AttributeCount;
					r.MoveToFirstAttribute();
					while(attCnt > 0)
					{
						if(r.Name == "source")
						{
							chan.mSource	=r.Value;
						}
						else if(r.Name == "target")
						{
							chan.mTarget	=r.Value;
						}
						r.MoveToNextAttribute();
						attCnt--;
					}
					mChannels.Add(chan);
				}
				else if(r.Name == "animation")
				{
					return;
				}
			}
		}
	}

	public class Animation
	{
		public string	mName;

		public List<SubAnimation>	mSubAnims	=new List<SubAnimation>();

		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			mName	=r.Value;
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "animation")
				{
					if(r.NodeType == XmlNodeType.EndElement)
					{
						return;
					}

					SubAnimation	sub	=new SubAnimation();
					sub.Load(r);
					mSubAnims.Add(sub);
				}
			}
		}
	}

	//debug draw data
	public class DrawChunk
	{
		public VertexBuffer			mVerts;
		public IndexBuffer			mIndexs;
		public VertexDeclaration	mVD;
		public int					mNumVerts, mNumTriangles, mVertSize;
	}

	class Collada
	{
		//data from collada
		private	Dictionary<string, Material>	mMaterials		=new Dictionary<string,Material>();
		private Dictionary<string, LibImage>	mImages			=new Dictionary<string,LibImage>();
		private	Dictionary<string, SceneNode>	mRootNodes		=new Dictionary<string,SceneNode>();
		private	Dictionary<string, Controller>	mControllers	=new Dictionary<string,Controller>();
		private	Dictionary<string, Geometry>	mGeometries		=new Dictionary<string,Geometry>();
		private Dictionary<string, Animation>	mAnimations		=new Dictionary<string,Animation>();

		private List<DrawChunk>	mChunks	=new List<DrawChunk>();


		public Collada(string meshFileName, GraphicsDevice g)
		{
			Load(meshFileName);

			BuildBuffers(g);

			FileStream	fs	=new FileStream("gack.mesh", FileMode.OpenOrCreate);

			BinaryFormatter	bf	=new BinaryFormatter();

			bf.Serialize(fs, mChunks);

			fs.Close();
		}
		
		//Getting the FieldInfo's is relatively expensive, you should come up
		//with a way to cache them (by root object type and name) if speed matters.
		public static void SetValue(Array a, int pos, string fieldName, object value)
		{
			var element = a.GetValue(pos);
			var elementType = element.GetType();
			FieldInfo fi = elementType.GetField(fieldName);
			fi.SetValue(element, value);
			a.SetValue(element, pos);
		}
		
		private	void	BuildBuffers(GraphicsDevice g)
		{
			foreach(KeyValuePair<string, Geometry> geom in mGeometries)
			{
				geom.Value.BuildBuffers(g, mChunks);
			}
		}


		public void Draw(GraphicsDevice g, Effect fx)
		{
			foreach(DrawChunk dc in mChunks)
			{
				g.VertexDeclaration	=dc.mVD;
				g.Indices			=dc.mIndexs;
				g.Vertices[0].SetSource(dc.mVerts, 0, dc.mVertSize);

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
					r.MoveToFirstAttribute();

					Material m			=new Material();
					m.mName				=matName;
					m.mInstanceEffect	=r.Value;

					mMaterials.Add(matID, m);
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
						LoadNode(r, root);

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


		public void LoadNode(XmlReader r, SceneNode n)
		{
			r.MoveToFirstAttribute();

			int attcnt	=r.AttributeCount;

			while(attcnt > 0)
			{
				//look for valid attributes for nodes
				if(r.Name == "name")
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

			InstanceMaterial	m		=null;
			bool				bEmpty	=false;
			
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

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
				else if(r.Name == "instance_geometry")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						n.mInstanceGeometryURL	=r.Value;
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
						bEmpty	=r.IsEmptyElement;

						r.MoveToFirstAttribute();

						m	=new InstanceMaterial();

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
						if(bEmpty)
						{
							n.mBindMaterials.Add(m);
						}
					}
					else
					{
						if(!bEmpty)
						{
							n.mBindMaterials.Add(m);
						}
					}
				}
				else if(r.Name == "bind")
				{
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
						r.MoveToFirstAttribute();
						string	id	=r.Value;

						SceneNode child	=new SceneNode();
						LoadNode(r, child);

						n.mChildren.Add(id, child);
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
Console.WriteLine("Attribute Count:" + r.Value.ToString());
						//create a dynamic type based on the collada
						//input semantics
						AssemblyName	an	=new AssemblyName("Assgoblins");
						AssemblyBuilder	ab	=AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
						ModuleBuilder	mb	=ab.DefineDynamicModule("Assgoblins");
						TypeBuilder		tb	=mb.DefineType("CustomVertex",
												TypeAttributes.Public |
												TypeAttributes.SequentialLayout |
												TypeAttributes.Serializable,
												typeof(ValueType));

						//define the custom struct
						foreach(Input inp in p.mInputs)
						{
							tb.DefineField(inp.mSemantic, inp.GetTypeForSemantic(), FieldAttributes.Public);
						}
						var	CustomVertex	=tb.CreateType();
						var	CVArray	=Array.CreateInstance(CustomVertex.GetType(), size);
						//dynArray.SetValue(dynamicType1Instance1, 0);
*/