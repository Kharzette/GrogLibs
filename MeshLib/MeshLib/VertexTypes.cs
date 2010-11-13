using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
#if !XBOX
using System.Reflection.Emit;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

		public static int GetSizeForType(Type t)
		{
			int			i			=0;
			int			sizeSoFar	=0;
			FieldInfo	fi;

			fi	=t.GetField("Position");
			if(fi != null)
			{
				sizeSoFar	+=12;
			}
			fi	=t.GetField("Normal");
			if(fi != null)
			{
				sizeSoFar	+=12;
			}
			fi	=t.GetField("BoneIndex");
			if(fi != null)
			{
				sizeSoFar	+=16;
			}
			fi	=t.GetField("BoneWeights");
			if(fi != null)
			{
				sizeSoFar	+=16;
			}
			while(true)
			{
				fi	=t.GetField("TexCoord" + i);
				if(fi == null)
				{
					break;
				}
				i++;
				sizeSoFar	+=8;
			}
			i	=0;
			while(true)
			{
				fi	=t.GetField("Color" + i);
				if(fi == null)
				{
					break;
				}
				i++;
				sizeSoFar	+=16;
			}

			return	sizeSoFar;
		}


		//don't think this is in use
		public static VertexDeclaration GetVertexDeclarationForType(GraphicsDevice g, Type t)
		{
			List<VertexElement>	ves			=new List<VertexElement>();
			int					i			=0;
			short				sizeSoFar	=0;
			FieldInfo			fi;

			while(true)
			{
				fi	=t.GetField("Position" + i);
				if(fi == null)
				{
					break;
				}
				VertexElement ve	=new VertexElement(0,
					sizeSoFar, VertexElementFormat.Vector3,
					VertexElementMethod.Default,
					VertexElementUsage.Position,
					(byte)i);
				ves.Add(ve);
				i++;
				sizeSoFar	+=12;
			}
			i	=0;
			while(true)
			{
				fi	=t.GetField("Normal" + i);
				if(fi == null)
				{
					break;
				}
				VertexElement ve	=new VertexElement(0,
					sizeSoFar, VertexElementFormat.Vector3,
					VertexElementMethod.Default,
					VertexElementUsage.Normal,
					(byte)i);
				ves.Add(ve);
				i++;
				sizeSoFar	+=12;
			}
			i	=0;
			while(true)
			{
				fi	=t.GetField("TexCoord" + i);
				if(fi == null)
				{
					break;
				}
				VertexElement ve	=new VertexElement(0,
					sizeSoFar, VertexElementFormat.Vector2,
					VertexElementMethod.Default,
					VertexElementUsage.TextureCoordinate,
					(byte)i);
				ves.Add(ve);
				i++;
				sizeSoFar	+=8;
			}
			i	=0;
			while(true)
			{
				fi	=t.GetField("Color" + i);
				if(fi == null)
				{
					break;
				}
				VertexElement ve	=new VertexElement(0,
					sizeSoFar, VertexElementFormat.Vector4,
					VertexElementMethod.Default,
					VertexElementUsage.Color,
					(byte)i);
				ves.Add(ve);
				i++;
				sizeSoFar	+=16;
			}

			VertexElement	[]vel	=new VertexElement[ves.Count];

			for(i=0;i < ves.Count;i++)
			{
				vel[i]	=ves[i];
			}
			
			return	new VertexDeclaration(g, vel);
		}


		//match up vertex characteristics to one of the
		//structure types in VertexStructures
		public static Type GetMatch(bool bPos, bool bNorm, bool bBone, int numTex, int numColor)
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
				if(bBone)
				{
					FieldInfo	fi	=t.GetField("BoneIndex");
					if(fi == null)
					{
						continue;
					}
					fi	=t.GetField("BoneWeights");
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


		public static void GetVertBounds(VertexBuffer vb, int numVerts, int typeIdx, IRayCastable bound)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=Array.CreateInstance(vtype, numVerts);

			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "GetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			AxialBounds	bnd	=new AxialBounds();

			List<Vector3>	points	=new List<Vector3>();

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
					//this might not be positional data!
					if(fi.Name == "Position")
					{
						Vector3	vec	=(Vector3)GetArrayField(verts, i, fi.Name);
						points.Add(vec);
					}
				}
			}

			bound.AddPointListToBounds(points);
		}


		public static void WriteVerts(BinaryWriter bw, VertexBuffer vb, int numVerts, int typeIdx)
		{
			Type	vtype	=mTypes[typeIdx];
			Array	verts	=Array.CreateInstance(vtype, numVerts);

			MethodInfo genericMethod =
				typeof (VertexBuffer).GetMethods().Where(
					x => x.Name == "GetData" && x.IsGenericMethod && x.GetParameters().Length == 1).Single();
            
			var typedMethod = genericMethod.MakeGenericMethod(new Type[] {vtype});

			typedMethod.Invoke(vb, new object[] {verts});

			FieldInfo	[]finfo	=vtype.GetFields();
			for(int i=0;i < numVerts;i++)
			{
				foreach(FieldInfo fi in finfo)
				{
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
				vb	=new VertexBuffer(gd, numVerts * GetSizeForType(vtype), BufferUsage.None);
			}
			else
			{
				vb	=new VertexBuffer(gd, numVerts * GetSizeForType(vtype), BufferUsage.WriteOnly);
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
