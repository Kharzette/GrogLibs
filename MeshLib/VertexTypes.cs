using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
#if !XBOX
using System.Reflection.Emit;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Storage;

namespace MeshLib
{
	public static class VertexTypes
	{
		static List<Type>	mTypes	=new List<Type>();

		static VertexTypes()
		{
			VertexTypes.AddType(typeof(VPos));
			VertexTypes.AddType(typeof(VPosNorm));
			VertexTypes.AddType(typeof(VPosBone));
			VertexTypes.AddType(typeof(VPosTex0));
			VertexTypes.AddType(typeof(VPosTex0Tex1));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Tex3));
			VertexTypes.AddType(typeof(VPosCol0));
			VertexTypes.AddType(typeof(VPosCol0Col1));
			VertexTypes.AddType(typeof(VPosCol0Col1Col2));
			VertexTypes.AddType(typeof(VPosCol0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosTex0Col0));
			VertexTypes.AddType(typeof(VPosTex0Col0Col1));
			VertexTypes.AddType(typeof(VPosTex0Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosTex0Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosTex0Tex1Col0));
			VertexTypes.AddType(typeof(VPosTex0Tex1Col0Col1));
			VertexTypes.AddType(typeof(VPosTex0Tex1Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosTex0Tex1Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Col0));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Col0Col1));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Tex3Col0));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Tex3Col0Col1));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Tex3Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosBoneTex0));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Tex3));
			VertexTypes.AddType(typeof(VPosBoneCol0));
			VertexTypes.AddType(typeof(VPosBoneCol0Col1));
			VertexTypes.AddType(typeof(VPosBoneCol0Col1Col2));
			VertexTypes.AddType(typeof(VPosBoneCol0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosBoneTex0Col0));
			VertexTypes.AddType(typeof(VPosBoneTex0Col0Col1));
			VertexTypes.AddType(typeof(VPosBoneTex0Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosBoneTex0Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Col0));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Col0Col1));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Col0));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Col0Col1));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Tex3Col0));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Tex3Col0Col1));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormTex0));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Tex3));
			VertexTypes.AddType(typeof(VPosNormCol0));
			VertexTypes.AddType(typeof(VPosNormCol0Col1));
			VertexTypes.AddType(typeof(VPosNormCol0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormCol0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormTex0Col0));
			VertexTypes.AddType(typeof(VPosNormTex0Col0Col1));
			VertexTypes.AddType(typeof(VPosNormTex0Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormTex0Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Col0));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Col0Col1));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Col0));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Col0Col1));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Tex3Col0));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Tex3Col0Col1));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Tex3Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBoneTex0));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Tex3));
			VertexTypes.AddType(typeof(VPosNormBoneCol0));
			VertexTypes.AddType(typeof(VPosNormBoneCol0Col1));
			VertexTypes.AddType(typeof(VPosNormBoneCol0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormBoneCol0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Col0));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Col0Col1));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Col0));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Col0Col1));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Col0));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Col0Col1));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Tex3Col0));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Tex3Col0Col1));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2));
			VertexTypes.AddType(typeof(VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3));
			VertexTypes.AddType(typeof(VPosNormBone));
			VertexTypes.AddType(typeof(VPosNormBlendTex0Tex1Tex2Tex3Tex4));
			VertexTypes.AddType(typeof(VPosNormTanTex0));
			VertexTypes.AddType(typeof(VPosNormTanBiTanTex0));
			VertexTypes.AddType(typeof(VPosNormTex04));
			VertexTypes.AddType(typeof(VPosNormTex04Col0));
			VertexTypes.AddType(typeof(VPosNormBlendTex04Tex14Tex24));
		}

		public static void AddType(Type t)
		{
			mTypes.Add(t);
		}


		public static Type GetTypeForIndex(int idx)
		{
			return	mTypes[idx];
		}


		public static int GetIndex(Type t)
		{
			return	mTypes.IndexOf(t);
		}


		public static int GetIndexForVertexDeclaration(VertexDeclaration vd)
		{
			Type	t	=GetTypeForVertexDeclaration(vd);

			return	GetIndex(t);
		}


		static Type VEFType(VertexElementFormat vef)
		{
			switch(vef)
			{
				case VertexElementFormat.Byte4:
					return	typeof(Byte4);
				case VertexElementFormat.Color:
					return	typeof(Color);
				case VertexElementFormat.HalfVector2:
					return	typeof(HalfVector2);
				case VertexElementFormat.HalfVector4:
					return	typeof(HalfVector4);
				case VertexElementFormat.NormalizedShort2:
					return	typeof(NormalizedShort2);
				case VertexElementFormat.NormalizedShort4:
					return	typeof(NormalizedShort4);
				case VertexElementFormat.Short2:
					return	typeof(Short2);
				case VertexElementFormat.Short4:
					return	typeof(Short4);
				case VertexElementFormat.Single:
					return	typeof(Single);
				case VertexElementFormat.Vector2:
					return	typeof(Vector2);
				case VertexElementFormat.Vector3:
					return	typeof(Vector3);
				case VertexElementFormat.Vector4:
					return	typeof(Vector4);
			}
			return	typeof(object);
		}


		static VertexElementFormat GetElementFormat(Type t)
		{
			if(t == typeof(Byte4))
			{
				return	VertexElementFormat.Byte4;
			}
			else if(t == typeof(Color))
			{
				return	VertexElementFormat.Color;
			}
			else if(t == typeof(HalfVector2))
			{
				return	VertexElementFormat.HalfVector2;
			}
			else if(t == typeof(HalfVector4))
			{
				return	VertexElementFormat.HalfVector4;
			}
			else if(t == typeof(NormalizedShort2))
			{
				return	VertexElementFormat.NormalizedShort2;
			}
			else if(t == typeof(NormalizedShort4))
			{
				return	VertexElementFormat.NormalizedShort4;
			}
			else if(t == typeof(Short2))
			{
				return	VertexElementFormat.Short2;
			}
			else if(t == typeof(Short4))
			{
				return	VertexElementFormat.Short4;
			}
			else if(t == typeof(Single))
			{
				return	VertexElementFormat.Single;
			}
			else if(t == typeof(Vector2))
			{
				return	VertexElementFormat.Vector2;
			}
			else if(t == typeof(Vector3))
			{
				return	VertexElementFormat.Vector3;
			}
			else if(t == typeof(Vector4))
			{
				return	VertexElementFormat.Vector4;
			}
			else
			{
				Debug.Assert(false);
				return	VertexElementFormat.HalfVector4;
			}
		}


		static bool HasFormatAndUsage(Type t, VertexElement el, int numColor, int numTex)
		{
			VertexElementFormat	fmt	=el.VertexElementFormat;

			if(el.VertexElementUsage == VertexElementUsage.Binormal)
			{
				return	HasElement(t, VEFType(fmt), "BiTangent");
			}
			else if(el.VertexElementUsage == VertexElementUsage.BlendIndices)
			{
				return	HasElement(t, VEFType(fmt), "BoneIndex");
			}
			else if(el.VertexElementUsage == VertexElementUsage.BlendWeight)
			{
				return	HasElement(t, VEFType(fmt), "BoneWeights");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Color)
			{
				return	HasElement(t, VEFType(fmt), "Color" + numColor);
			}
			else if(el.VertexElementUsage == VertexElementUsage.Depth)
			{
				return	HasElement(t, VEFType(fmt), "Depth");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Fog)
			{
				return	HasElement(t, VEFType(fmt), "Fog");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Normal)
			{
				return	HasElement(t, VEFType(fmt), "Normal");
			}
			else if(el.VertexElementUsage == VertexElementUsage.PointSize)
			{
				return	HasElement(t, VEFType(fmt), "PointSize");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Position)
			{
				return	HasElement(t, VEFType(fmt), "Position");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Sample)
			{
				return	HasElement(t, VEFType(fmt), "Sample");
			}
			else if(el.VertexElementUsage == VertexElementUsage.Tangent)
			{
				return	HasElement(t, VEFType(fmt), "Tangent");
			}
			else if(el.VertexElementUsage == VertexElementUsage.TessellateFactor)
			{
				return	HasElement(t, VEFType(fmt), "TessFactor");
			}
			else if(el.VertexElementUsage == VertexElementUsage.TextureCoordinate)
			{
				return	HasElement(t, VEFType(fmt), "TexCoord" + numTex);
			}
			return	false;
		}


		public static Type GetTypeForVertexDeclaration(VertexDeclaration vd)
		{
			VertexElement	[]elems	=vd.GetVertexElements();

			int		numTex, numColor;
			foreach(Type t in mTypes)
			{
				numTex	=numColor	=0;
				bool	bFound	=true;
				foreach(VertexElement el in elems)
				{
					if(!HasFormatAndUsage(t, el, numColor, numTex))
					{
						bFound	=false;
						break;
					}
					else
					{
						//found
						if(el.VertexElementUsage == VertexElementUsage.TextureCoordinate)
						{
							numTex++;
						}
						else if(el.VertexElementUsage == VertexElementUsage.Color)
						{
							numColor++;
						}
					}
				}

				if(bFound)
				{
					return	t;
				}			
			}

			Debug.WriteLine("Warning!  Type not found for vertex declaration!");

			Debug.Assert(false);

			return	typeof(object);
		}


		public static bool HasElement(Type t, Type subType, string eleName)
		{
			FieldInfo	fi	=t.GetField(eleName);
			if(fi == null)
			{
				return	false;
			}
			return	(fi.FieldType == subType);
		}


		public static int GetSizeForType(Type t)
		{
			FieldInfo	[]fields	=t.GetFields();

			int	size	=0;
			foreach(FieldInfo fi in fields)
			{
				if(fi.FieldType == typeof(Single))
				{
					size	+=4;
				}
				else if(fi.FieldType == typeof(Vector2))
				{
					size	+=8;
				}
				else if(fi.FieldType == typeof(Vector3))
				{
					size	+=12;
				}
				else if(fi.FieldType == typeof(Vector4))
				{
					size	+=16;
				}
				else
				{
					Debug.Assert(false);	//unknown
				}
			}
			return	size;
		}


		static int CountTypes(List<VertexElement> ves, Type t, VertexElementUsage veu)
		{
			int	ret	=0;
			foreach(VertexElement ve in ves)
			{
				if(ve.VertexElementFormat == GetElementFormat(t))
				{
					if(ve.VertexElementUsage == veu)
					{
						ret++;
					}
				}
			}
			return	ret;
		}


		public static VertexDeclaration GetVertexDeclarationForType(Type t)
		{
			FieldInfo	[]fields	=t.GetFields();

			List<VertexElement>	ves			=new List<VertexElement>();

			int	sizeSoFar	=0;
			foreach(FieldInfo fi in fields)
			{
				VertexElementUsage	veu;
				if(fi.Name.StartsWith("BiTangent"))
				{
					veu	=VertexElementUsage.Binormal;
				}
				else if(fi.Name.StartsWith("BoneIndex"))
				{
					veu	=VertexElementUsage.BlendIndices;
				}
				else if(fi.Name.StartsWith("BoneWeights"))
				{
					veu	=VertexElementUsage.BlendWeight;
				}
				else if(fi.Name.StartsWith("Color"))
				{
					veu	=VertexElementUsage.Color;
				}
				else if(fi.Name.StartsWith("Depth"))
				{
					veu	=VertexElementUsage.Depth;
				}
				else if(fi.Name.StartsWith("Fog"))
				{
					veu	=VertexElementUsage.Fog;
				}
				else if(fi.Name.StartsWith("Normal"))
				{
					veu	=VertexElementUsage.Normal;
				}
				else if(fi.Name.StartsWith("PointSize"))
				{
					veu	=VertexElementUsage.PointSize;
				}
				else if(fi.Name.StartsWith("Position"))
				{
					veu	=VertexElementUsage.Position;
				}
				else if(fi.Name.StartsWith("Sample"))
				{
					veu	=VertexElementUsage.Sample;
				}
				else if(fi.Name.StartsWith("Tangent"))
				{
					veu	=VertexElementUsage.Tangent;
				}
				else if(fi.Name.StartsWith("TessFactor"))
				{
					veu	=VertexElementUsage.TessellateFactor;
				}
				else if(fi.Name.StartsWith("TexCoord"))
				{
					veu	=VertexElementUsage.TextureCoordinate;
				}
				else
				{
					Debug.Assert(false);
					veu	=VertexElementUsage.Position;
				}

				ves.Add(new VertexElement(sizeSoFar,
					GetElementFormat(fi.FieldType), veu,
					CountTypes(ves, fi.FieldType, veu)));

				sizeSoFar	+=GetSizeForType(fi.FieldType);
			}
			
			return	new VertexDeclaration(ves.ToArray());
		}


		//match up vertex characteristics to one of the
		//structure types in VertexStructures
		public static Type GetMatch(bool bPos, bool bNorm, bool bBoneIdx,
			bool bBoneWeight, bool bTan, bool bBiTan, int numTex, int numColor)
		{
			foreach(Type t in mTypes)
			{
				//only support 1 position
				if(bPos)
				{
					FieldInfo	fi	=t.GetField("Position");
					if(fi == null)
					{
						continue;
					}
				}
				//only support 1 normal set
				if(bNorm)
				{
					FieldInfo	fi	=t.GetField("Normal");
					if(fi == null)
					{
						continue;
					}
				}
				//only support 1 tangent set
				if(bTan)
				{
					FieldInfo	fi	=t.GetField("Tangent");
					if(fi == null)
					{
						continue;
					}
				}
				//only support 1 bitangent set
				if(bBiTan)
				{
					FieldInfo	fi	=t.GetField("BiTangent");
					if(fi == null)
					{
						continue;
					}
				}
				if(bBoneIdx)
				{
					FieldInfo	fi	=t.GetField("BoneIndex");
					if(fi == null)
					{
						continue;
					}
				}
				if(bBoneWeight)
				{
					FieldInfo	fi	=t.GetField("BoneWeights");
					if(fi == null)
					{
						continue;
					}
				}
				bool	bFound	=true;
				for(int i=0;i < numTex;i++)
				{
					FieldInfo	fi	=t.GetField("TexCoord" + i);
					if(fi == null)
					{
						bFound	=false;
						break;
					}
				}
				if(!bFound)
				{
					continue;
				}
				for(int i=0;i < numColor;i++)
				{
					FieldInfo	fi	=t.GetField("Color" + i);
					if(fi == null)
					{
						bFound	=false;
						break;
					}
				}
				if(!bFound)
				{
					continue;
				}
				return	t;
			}
			return	typeof(object);
		}


		public static void SetArrayField(Array a, int idx, string fieldName, object value)
		{
			//grab the struct out of the array
			object	val	=a.GetValue(idx);

			//find the field in the struct
			FieldInfo	fi	=val.GetType().GetField(fieldName);
			if(fi == null)
			{
				return;
			}

			//set the field's value
			fi.SetValue(val, value);

			//put the struct back into the array modified
			a.SetValue(val, idx);
		}


		public static object GetArrayField(Array a, int idx, string fieldName)
		{
			//grab the struct out of the array
			object	val	=a.GetValue(idx);

			//find the field in the struct
			FieldInfo	fi	=val.GetType().GetField(fieldName);
			if(fi == null)
			{
				return	null;
			}

			return	fi.GetValue(val);
		}


		static Array GetVertArray(VertexBuffer vb, int numVerts, int typeIdx)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=Array.CreateInstance(vtype, numVerts);

			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "GetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			return	verts;
		}


		public static List<Vector3> GetPositions(VertexBuffer vb, int numVerts, int typeIdx)
		{
			List<Vector3>	vecs	=new List<Vector3>();

			Type	vtype	=mTypes[typeIdx];
			Array	verts	=GetVertArray(vb, numVerts, typeIdx);

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					//this might not be positional data!
					if(fi.Name == "Position")
					{
						Vector3	vec	=(Vector3)GetArrayField(verts, i, fi.Name);
						vecs.Add(vec);
					}
				}
			}

			return	vecs;
		}


		public static List<Vector3> GetNormals(VertexBuffer vb, int numVerts, int typeIdx)
		{
			List<Vector3>	norms	=new List<Vector3>();

			Type	vtype	=mTypes[typeIdx];
			Array	verts	=GetVertArray(vb, numVerts, typeIdx);

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					//this might not be positional data!
					if(fi.Name == "Normal")
					{
						Vector3	vec	=(Vector3)GetArrayField(verts, i, fi.Name);
						norms.Add(vec);
					}
				}
			}

			return	norms;
		}


		public static List<Vector2> GetTexCoord(VertexBuffer vb, int numVerts, int typeIdx, int set)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=GetVertArray(vb, numVerts, typeIdx);

			List<Vector2>	texs	=new List<Vector2>();

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					//this might not be positional data!
					if(fi.Name == "TexCoord" + set)
					{
						Vector2	vec	=(Vector2)GetArrayField(verts, i, fi.Name);
						texs.Add(vec);
					}
				}
			}

			return	texs;
		}


		//create a new vertexbuffer with tangents added
		public static VertexBuffer AddTangents(GraphicsDevice gd, VertexBuffer vb, int numVerts, int typeIdx, Vector4 []tans, out int typeIndex)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=GetVertArray(vb, numVerts, typeIdx);

			//count texcoords
			int	texCnt	=0;
			while(HasElement(vtype, typeof(Vector2), "TexCoord" + texCnt))
			{
				texCnt++;
			}

			//count colors
			int	colCnt	=0;
			while(HasElement(vtype, typeof(Vector4), "Color" + colCnt))
			{
				colCnt++;
			}

			bool	bPos		=HasElement(vtype, typeof(Vector3), "Position");
			bool	bNorm		=HasElement(vtype, typeof(Vector3), "Normal");
			bool	bBoneIdx	=HasElement(vtype, typeof(Vector4), "BoneIndex");
			bool	bBoneWeight	=HasElement(vtype, typeof(Vector4), "BoneWeights");

			//build the new type
			Type	vtypeNew	=GetMatch(
				bPos,
				bNorm,
				bBoneIdx,
				bBoneWeight,
				true,
				false,
				texCnt,
				colCnt);

			Array	newVerts	=Array.CreateInstance(vtypeNew, numVerts);

			for(int i=0;i < numVerts;i++)
			{
				if(bPos)
				{
					SetArrayField(newVerts, i, "Position", GetArrayField(verts, i, "Position"));
				}
				if(bNorm)
				{
					SetArrayField(newVerts, i, "Normal", GetArrayField(verts, i, "Normal"));
				}
				if(bBoneIdx)
				{
					SetArrayField(newVerts, i, "BoneIndex", GetArrayField(verts, i, "BoneIndex"));
				}
				if(bBoneWeight)
				{
					SetArrayField(newVerts, i, "BoneWeights", GetArrayField(verts, i, "BoneWeights"));
				}
				for(int j=0;j < texCnt;j++)
				{
					SetArrayField(newVerts, i, "TexCoord" + j, GetArrayField(verts, i, "TexCoord" + j));
				}
				for(int j=0;j < colCnt;j++)
				{
					SetArrayField(newVerts, i, "Color" + j, GetArrayField(verts, i, "Color" + j));
				}

				//tangents
				SetArrayField(newVerts, i, "Tangent", tans[i]);
			}

			typeIndex	=GetIndex(vtypeNew);

			VertexDeclaration	dec	=GetVertexDeclarationForType(vtypeNew);

			VertexBuffer vb2	=new VertexBuffer(gd, dec, numVerts, BufferUsage.None);
			
			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "SetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtypeNew});

			typedMethod.Invoke(vb2, new object[] {newVerts});

			return	vb2;
		}


		//create a new vertexbuffer with tangents and bitangents added
		public static VertexBuffer AddTangents(GraphicsDevice gd, VertexBuffer vb, int numVerts,
			int typeIdx, Vector3 []tans, Vector3 []bitans, out int typeIndex)
		{
			typeIndex	=69;
			return	null;
			/*
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=GetVertArray(vb, numVerts, typeIdx);

			//count texcoords
			int	texCnt	=0;
			while(HasElement(vtype, "TexCoord" + texCnt))
			{
				texCnt++;
			}

			//count colors
			int	colCnt	=0;
			while(HasElement(vtype, "Color" + colCnt))
			{
				colCnt++;
			}

			bool	bPos		=HasElement(vtype, "Position");
			bool	bNorm		=HasElement(vtype, "Normal");
			bool	bBoneIdx	=HasElement(vtype, "BoneIndex");
			bool	bBoneWeight	=HasElement(vtype, "BoneWeights");

			//build the new type
			Type	vtypeNew	=GetMatch(
				bPos,
				bNorm,
				bBoneIdx,
				bBoneWeight,
				true,
				true,
				texCnt,
				colCnt);

			Array	newVerts	=Array.CreateInstance(vtypeNew, numVerts);

			for(int i=0;i < numVerts;i++)
			{
				if(bPos)
				{
					SetArrayField(newVerts, i, "Position", GetArrayField(verts, i, "Position"));
				}
				if(bNorm)
				{
					SetArrayField(newVerts, i, "Normal", GetArrayField(verts, i, "Normal"));
				}
				if(bBoneIdx)
				{
					SetArrayField(newVerts, i, "BoneIndex", GetArrayField(verts, i, "BoneIndex"));
				}
				if(bBoneWeight)
				{
					SetArrayField(newVerts, i, "BoneWeights", GetArrayField(verts, i, "BoneWeights"));
				}
				for(int j=0;j < texCnt;j++)
				{
					SetArrayField(newVerts, i, "TexCoord" + j, GetArrayField(verts, i, "TexCoord" + j));
				}
				for(int j=0;j < colCnt;j++)
				{
					SetArrayField(newVerts, i, "Color" + j, GetArrayField(verts, i, "Color" + j));
				}

				//tangents
				SetArrayField(newVerts, i, "Tangent", tans[i]);

				//bitangents
				SetArrayField(newVerts, i, "BiTangent", bitans[i]);
			}

			typeIndex	=GetIndex(vtypeNew);

			VertexDeclaration	dec	=GetVertexDeclarationForType(vtypeNew);

			VertexBuffer vb2	=new VertexBuffer(gd, dec, numVerts, BufferUsage.None);
			
			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "SetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtypeNew});

			typedMethod.Invoke(vb2, new object[] {newVerts});

			return	vb2;*/
		}


		public static void GetVertBounds(VertexBuffer vb, int numVerts,
			int typeIdx, out BoundingBox box, out BoundingSphere sphere)
		{
			List<Vector3>	points	=GetPositions(vb, numVerts, typeIdx);

			box		=BoundingBox.CreateFromPoints(points);
			sphere	=UtilityLib.Mathery.SphereFromPoints(points);
		}


		public static List<Vector3> GetNormals(VertexBuffer vb, int typeIdx)
		{
			List<Vector3>	ret		=new List<Vector3>();
			Type			vtype	=mTypes[typeIdx];
			Array			verts	=Array.CreateInstance(vtype, vb.VertexCount);

			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "GetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < vb.VertexCount;i++)
			{
				Vector3	pos	=Vector3.Zero;
				foreach(FieldInfo fi in finfo)
				{
					if(fi.Name == "Position")
					{
						pos	=(Vector3)GetArrayField(verts, i, fi.Name);
						ret.Add(pos);
					}
					else if(fi.Name == "Normal")
					{
						Vector3	vec	=(Vector3)GetArrayField(verts, i, fi.Name);
						ret.Add(pos + (vec * 5));
					}
				}
			}

			return	ret;
		}


		public static void WriteVerts(BinaryWriter bw, VertexBuffer vb, int typeIdx)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=Array.CreateInstance(vtype, vb.VertexCount);

			//save vertex declaration first
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, vb.VertexDeclaration);

			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "GetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < vb.VertexCount;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					if(fi.FieldType.Name == "Single")
					{
						Single	vec	=(Single)GetArrayField(verts, i, fi.Name);
						bw.Write(vec);
					}
					if(fi.FieldType.Name == "Vector2")
					{
						Vector2	vec	=(Vector2)GetArrayField(verts, i, fi.Name);
						bw.Write(vec.X);
						bw.Write(vec.Y);
					}
					else if(fi.FieldType.Name == "Vector3")
					{
						Vector3	vec	=(Vector3)GetArrayField(verts, i, fi.Name);
						bw.Write(vec.X);
						bw.Write(vec.Y);
						bw.Write(vec.Z);
					}
					else if(fi.FieldType.Name == "Vector4")
					{
						Vector4	vec	=(Vector4)GetArrayField(verts, i, fi.Name);
						bw.Write(vec.X);
						bw.Write(vec.Y);
						bw.Write(vec.Z);
						bw.Write(vec.W);
					}
				}
			}
		}


		public static void ReadVerts(BinaryReader		br,
									GraphicsDevice		gd,
									out VertexBuffer	vb,
									int					numVerts,
									int					typeIdx,
									bool				bEditor)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=Array.CreateInstance(vtype, numVerts);

			MethodInfo	[]meths	=typeof (VertexBuffer).GetMethods();
			MethodInfo	genericMethod	=null;
			
			//read the vertex declaration
			VertexDeclaration	vd;
			UtilityLib.FileUtil.ReadVertexDeclaration(br, out vd);

			foreach(MethodInfo mi in meths)
			{
				if(mi.Name == "SetData" && mi.IsGenericMethod)
				{
					genericMethod	=mi;

					//get parameters is not supported on generic methods
					//so make a typed method to test
					var testMethod	=genericMethod.MakeGenericMethod(new Type[] {vtype});
					ParameterInfo	[]pi	=testMethod.GetParameters();
					if(pi.Length == 1)
					{
						break;
					}
				}
			}
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			if(bEditor)
			{
				vb	=new VertexBuffer(gd, vd, numVerts, BufferUsage.None);
			}
			else
			{
				vb	=new VertexBuffer(gd, vd, numVerts, BufferUsage.WriteOnly);
			}

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					if(fi.FieldType.Name == "Vector2")
					{
						Vector2	vec	=Vector2.Zero;

						vec.X	=br.ReadSingle();
						vec.Y	=br.ReadSingle();

						SetArrayField(verts, i, fi.Name, vec);
					}
					else if(fi.FieldType.Name == "Vector3")
					{
						Vector3	vec	=Vector3.Zero;

						vec.X	=br.ReadSingle();
						vec.Y	=br.ReadSingle();
						vec.Z	=br.ReadSingle();

						SetArrayField(verts, i, fi.Name, vec);
					}
					else if(fi.FieldType.Name == "Vector4")
					{
						Vector4	vec	=Vector4.Zero;

						vec.X	=br.ReadSingle();
						vec.Y	=br.ReadSingle();
						vec.Z	=br.ReadSingle();
						vec.W	=br.ReadSingle();

						SetArrayField(verts, i, fi.Name, vec);
					}
				}
			}

			typedMethod.Invoke(vb, new object[] {verts});
		}
	}
}
