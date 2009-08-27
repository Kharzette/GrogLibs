using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if !XBOX
using System.Reflection;
using System.Reflection.Emit;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

namespace Character
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


		public static int GetIndex(Type t)
		{
			return	mTypes.IndexOf(t);
		}

#if !XBOX
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
#endif













		public static void WriteVPos(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPos	[]verts	=new VPos[numVerts];
			vb.GetData<VPos>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNorm(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNorm	[]verts	=new VPosNorm[numVerts];
			vb.GetData<VPosNorm>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBone(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBone	[]verts	=new VPosBone[numVerts];
			vb.GetData<VPosBone>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0	[]verts	=new VPosTex0[numVerts];
			vb.GetData<VPosTex0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1	[]verts	=new VPosTex0Tex1[numVerts];
			vb.GetData<VPosTex0Tex1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2	[]verts	=new VPosTex0Tex1Tex2[numVerts];
			vb.GetData<VPosTex0Tex1Tex2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Tex3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Tex3	[]verts	=new VPosTex0Tex1Tex2Tex3[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Tex3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosCol0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosCol0	[]verts	=new VPosCol0[numVerts];
			vb.GetData<VPosCol0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosCol0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosCol0Col1	[]verts	=new VPosCol0Col1[numVerts];
			vb.GetData<VPosCol0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosCol0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosCol0Col1Col2	[]verts	=new VPosCol0Col1Col2[numVerts];
			vb.GetData<VPosCol0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosCol0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosCol0Col1Col2Col3	[]verts	=new VPosCol0Col1Col2Col3[numVerts];
			vb.GetData<VPosCol0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Col0	[]verts	=new VPosTex0Col0[numVerts];
			vb.GetData<VPosTex0Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Col0Col1	[]verts	=new VPosTex0Col0Col1[numVerts];
			vb.GetData<VPosTex0Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Col0Col1Col2	[]verts	=new VPosTex0Col0Col1Col2[numVerts];
			vb.GetData<VPosTex0Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Col0Col1Col2Col3	[]verts	=new VPosTex0Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosTex0Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Col0	[]verts	=new VPosTex0Tex1Col0[numVerts];
			vb.GetData<VPosTex0Tex1Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Col0Col1	[]verts	=new VPosTex0Tex1Col0Col1[numVerts];
			vb.GetData<VPosTex0Tex1Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Col0Col1Col2	[]verts	=new VPosTex0Tex1Col0Col1Col2[numVerts];
			vb.GetData<VPosTex0Tex1Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosTex0Tex1Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Col0	[]verts	=new VPosTex0Tex1Tex2Col0[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Col0Col1	[]verts	=new VPosTex0Tex1Tex2Col0Col1[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosTex0Tex1Tex2Col0Col1Col2[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Tex3Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Tex3Col0	[]verts	=new VPosTex0Tex1Tex2Tex3Col0[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Tex3Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Tex3Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Tex3Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Tex3Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosTex0Tex1Tex2Tex3Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0	[]verts	=new VPosBoneTex0[numVerts];
			vb.GetData<VPosBoneTex0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1	[]verts	=new VPosBoneTex0Tex1[numVerts];
			vb.GetData<VPosBoneTex0Tex1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2	[]verts	=new VPosBoneTex0Tex1Tex2[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Tex3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Tex3	[]verts	=new VPosBoneTex0Tex1Tex2Tex3[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Tex3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneCol0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneCol0	[]verts	=new VPosBoneCol0[numVerts];
			vb.GetData<VPosBoneCol0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneCol0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneCol0Col1	[]verts	=new VPosBoneCol0Col1[numVerts];
			vb.GetData<VPosBoneCol0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneCol0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneCol0Col1Col2	[]verts	=new VPosBoneCol0Col1Col2[numVerts];
			vb.GetData<VPosBoneCol0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneCol0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneCol0Col1Col2Col3	[]verts	=new VPosBoneCol0Col1Col2Col3[numVerts];
			vb.GetData<VPosBoneCol0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Col0	[]verts	=new VPosBoneTex0Col0[numVerts];
			vb.GetData<VPosBoneTex0Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Col0Col1	[]verts	=new VPosBoneTex0Col0Col1[numVerts];
			vb.GetData<VPosBoneTex0Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Col0Col1Col2	[]verts	=new VPosBoneTex0Col0Col1Col2[numVerts];
			vb.GetData<VPosBoneTex0Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosBoneTex0Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Col0	[]verts	=new VPosBoneTex0Tex1Col0[numVerts];
			vb.GetData<VPosBoneTex0Tex1Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Col0Col1	[]verts	=new VPosBoneTex0Tex1Col0Col1[numVerts];
			vb.GetData<VPosBoneTex0Tex1Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Col0Col1Col2[numVerts];
			vb.GetData<VPosBoneTex0Tex1Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosBoneTex0Tex1Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Col0	[]verts	=new VPosBoneTex0Tex1Tex2Col0[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Col0Col1	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1Col2[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Tex3Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Tex3Col0	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Tex3Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0	[]verts	=new VPosNormTex0[numVerts];
			vb.GetData<VPosNormTex0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1	[]verts	=new VPosNormTex0Tex1[numVerts];
			vb.GetData<VPosNormTex0Tex1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2	[]verts	=new VPosNormTex0Tex1Tex2[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Tex3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Tex3	[]verts	=new VPosNormTex0Tex1Tex2Tex3[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Tex3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormCol0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormCol0	[]verts	=new VPosNormCol0[numVerts];
			vb.GetData<VPosNormCol0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormCol0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormCol0Col1	[]verts	=new VPosNormCol0Col1[numVerts];
			vb.GetData<VPosNormCol0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormCol0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormCol0Col1Col2	[]verts	=new VPosNormCol0Col1Col2[numVerts];
			vb.GetData<VPosNormCol0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormCol0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormCol0Col1Col2Col3	[]verts	=new VPosNormCol0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormCol0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Col0	[]verts	=new VPosNormTex0Col0[numVerts];
			vb.GetData<VPosNormTex0Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Col0Col1	[]verts	=new VPosNormTex0Col0Col1[numVerts];
			vb.GetData<VPosNormTex0Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Col0Col1Col2	[]verts	=new VPosNormTex0Col0Col1Col2[numVerts];
			vb.GetData<VPosNormTex0Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Col0Col1Col2Col3	[]verts	=new VPosNormTex0Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormTex0Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Col0	[]verts	=new VPosNormTex0Tex1Col0[numVerts];
			vb.GetData<VPosNormTex0Tex1Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Col0Col1	[]verts	=new VPosNormTex0Tex1Col0Col1[numVerts];
			vb.GetData<VPosNormTex0Tex1Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Col0Col1Col2[numVerts];
			vb.GetData<VPosNormTex0Tex1Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormTex0Tex1Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Col0	[]verts	=new VPosNormTex0Tex1Tex2Col0[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Col0Col1	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1Col2[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Tex3Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Tex3Col0	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Tex3Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Tex3Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Tex3Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Tex3Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0	[]verts	=new VPosNormBoneTex0[numVerts];
			vb.GetData<VPosNormBoneTex0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
				bw.Write(verts[i].Normal.X);
				bw.Write(verts[i].Normal.Y);
				bw.Write(verts[i].Normal.Z);
				bw.Write(verts[i].BoneIndex.X);
				bw.Write(verts[i].BoneIndex.Y);
				bw.Write(verts[i].BoneIndex.Z);
				bw.Write(verts[i].BoneIndex.W);
				bw.Write(verts[i].BoneWeights.X);
				bw.Write(verts[i].BoneWeights.Y);
				bw.Write(verts[i].BoneWeights.Z);
				bw.Write(verts[i].BoneWeights.W);
				bw.Write(verts[i].TexCoord0.X);
				bw.Write(verts[i].TexCoord0.Y);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1	[]verts	=new VPosNormBoneTex0Tex1[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2	[]verts	=new VPosNormBoneTex0Tex1Tex2[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Tex3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Tex3	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Tex3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneCol0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneCol0	[]verts	=new VPosNormBoneCol0[numVerts];
			vb.GetData<VPosNormBoneCol0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneCol0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneCol0Col1	[]verts	=new VPosNormBoneCol0Col1[numVerts];
			vb.GetData<VPosNormBoneCol0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneCol0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneCol0Col1Col2	[]verts	=new VPosNormBoneCol0Col1Col2[numVerts];
			vb.GetData<VPosNormBoneCol0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneCol0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneCol0Col1Col2Col3	[]verts	=new VPosNormBoneCol0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormBoneCol0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Col0	[]verts	=new VPosNormBoneTex0Col0[numVerts];
			vb.GetData<VPosNormBoneTex0Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Col0Col1	[]verts	=new VPosNormBoneTex0Col0Col1[numVerts];
			vb.GetData<VPosNormBoneTex0Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Col0Col1Col2	[]verts	=new VPosNormBoneTex0Col0Col1Col2[numVerts];
			vb.GetData<VPosNormBoneTex0Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormBoneTex0Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Col0	[]verts	=new VPosNormBoneTex0Tex1Col0[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
				bw.Write(verts[i].Normal.X);
				bw.Write(verts[i].Normal.Y);
				bw.Write(verts[i].Normal.Z);
				bw.Write(verts[i].BoneIndex.X);
				bw.Write(verts[i].BoneIndex.Y);
				bw.Write(verts[i].BoneIndex.Z);
				bw.Write(verts[i].BoneIndex.W);
				bw.Write(verts[i].BoneWeights.X);
				bw.Write(verts[i].BoneWeights.Y);
				bw.Write(verts[i].BoneWeights.Z);
				bw.Write(verts[i].BoneWeights.W);
				bw.Write(verts[i].TexCoord0.X);
				bw.Write(verts[i].TexCoord0.Y);
				bw.Write(verts[i].TexCoord1.X);
				bw.Write(verts[i].TexCoord1.Y);
				bw.Write(verts[i].Color0.X);
				bw.Write(verts[i].Color0.Y);
				bw.Write(verts[i].Color0.Z);
				bw.Write(verts[i].Color0.W);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Col0Col1[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Col0Col1Col2[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Col0	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1Col2[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Tex3Col0(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Tex3Col0	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Tex3Col0>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			vb.GetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}


		public static void WriteVPosNormBone(BinaryWriter bw, VertexBuffer vb, int numVerts)
		{
			VPos	[]verts	=new VPos[numVerts];
			vb.GetData<VPos>(verts);
			bw.Write(numVerts);
			for(int i=0;i < numVerts;i++)
			{
				bw.Write(verts[i].Position.X);
				bw.Write(verts[i].Position.Y);
				bw.Write(verts[i].Position.Z);
			}
		}
















		public static void ReadVPos(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPos	[]verts	=new VPos[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPos>(verts);
		}


		public static void ReadVPosNorm(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNorm	[]verts	=new VPosNorm[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNorm>(verts);
		}


		public static void ReadVPosBone(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBone	[]verts	=new VPosBone[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBone>(verts);
		}


		public static void ReadVPosTex0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0	[]verts	=new VPosTex0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0>(verts);
		}


		public static void ReadVPosTex0Tex1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1	[]verts	=new VPosTex0Tex1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2	[]verts	=new VPosTex0Tex1Tex2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Tex3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Tex3	[]verts	=new VPosTex0Tex1Tex2Tex3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3>(verts);
		}


		public static void ReadVPosCol0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosCol0	[]verts	=new VPosCol0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosCol0>(verts);
		}


		public static void ReadVPosCol0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosCol0Col1	[]verts	=new VPosCol0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosCol0Col1>(verts);
		}


		public static void ReadVPosCol0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosCol0Col1Col2	[]verts	=new VPosCol0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosCol0Col1Col2>(verts);
		}


		public static void ReadVPosCol0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosCol0Col1Col2Col3	[]verts	=new VPosCol0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosCol0Col1Col2Col3>(verts);
		}


		public static void ReadVPosTex0Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Col0	[]verts	=new VPosTex0Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Col0>(verts);
		}


		public static void ReadVPosTex0Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Col0Col1	[]verts	=new VPosTex0Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Col0Col1>(verts);
		}


		public static void ReadVPosTex0Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Col0Col1Col2	[]verts	=new VPosTex0Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Col0Col1Col2>(verts);
		}


		public static void ReadVPosTex0Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Col0Col1Col2Col3	[]verts	=new VPosTex0Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosTex0Tex1Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Col0	[]verts	=new VPosTex0Tex1Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Col0>(verts);
		}


		public static void ReadVPosTex0Tex1Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Col0Col1	[]verts	=new VPosTex0Tex1Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Col0Col1>(verts);
		}


		public static void ReadVPosTex0Tex1Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Col0Col1Col2	[]verts	=new VPosTex0Tex1Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Col0Col1Col2>(verts);
		}


		public static void ReadVPosTex0Tex1Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Col0	[]verts	=new VPosTex0Tex1Tex2Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Col0>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Col0Col1	[]verts	=new VPosTex0Tex1Tex2Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Col0Col1>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosTex0Tex1Tex2Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Col0Col1Col2>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Tex3Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Tex3Col0	[]verts	=new VPosTex0Tex1Tex2Tex3Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3Col0>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Tex3Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3Col0Col1>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Tex3Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
		}


		public static void ReadVPosTex0Tex1Tex2Tex3Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosBoneTex0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0	[]verts	=new VPosBoneTex0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0>(verts);
		}


		public static void ReadVPosBoneTex0Tex1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1	[]verts	=new VPosBoneTex0Tex1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2	[]verts	=new VPosBoneTex0Tex1Tex2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Tex3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Tex3	[]verts	=new VPosBoneTex0Tex1Tex2Tex3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Tex3>(verts);
		}


		public static void ReadVPosBoneCol0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneCol0	[]verts	=new VPosBoneCol0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneCol0>(verts);
		}


		public static void ReadVPosBoneCol0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneCol0Col1	[]verts	=new VPosBoneCol0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneCol0Col1>(verts);
		}


		public static void ReadVPosBoneCol0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneCol0Col1Col2	[]verts	=new VPosBoneCol0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneCol0Col1Col2>(verts);
		}


		public static void ReadVPosBoneCol0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneCol0Col1Col2Col3	[]verts	=new VPosBoneCol0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneCol0Col1Col2Col3>(verts);
		}


		public static void ReadVPosBoneTex0Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Col0	[]verts	=new VPosBoneTex0Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Col0>(verts);
		}


		public static void ReadVPosBoneTex0Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Col0Col1	[]verts	=new VPosBoneTex0Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Col0Col1>(verts);
		}


		public static void ReadVPosBoneTex0Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Col0Col1Col2	[]verts	=new VPosBoneTex0Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Col0Col1Col2>(verts);
		}


		public static void ReadVPosBoneTex0Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Col0	[]verts	=new VPosBoneTex0Tex1Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Col0>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Col0Col1	[]verts	=new VPosBoneTex0Tex1Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Col0Col1>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Col0Col1Col2>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Col0	[]verts	=new VPosBoneTex0Tex1Tex2Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Col0>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Col0Col1	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Col0Col1>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Col0Col1Col2>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Tex3Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Tex3Col0	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Tex3Col0>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
		}


		public static void ReadVPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormTex0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0	[]verts	=new VPosNormTex0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0>(verts);
		}


		public static void ReadVPosNormTex0Tex1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1	[]verts	=new VPosNormTex0Tex1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2	[]verts	=new VPosNormTex0Tex1Tex2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Tex3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Tex3	[]verts	=new VPosNormTex0Tex1Tex2Tex3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Tex3>(verts);
		}


		public static void ReadVPosNormCol0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormCol0	[]verts	=new VPosNormCol0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormCol0>(verts);
		}


		public static void ReadVPosNormCol0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormCol0Col1	[]verts	=new VPosNormCol0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormCol0Col1>(verts);
		}


		public static void ReadVPosNormCol0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormCol0Col1Col2	[]verts	=new VPosNormCol0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormCol0Col1Col2>(verts);
		}


		public static void ReadVPosNormCol0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormCol0Col1Col2Col3	[]verts	=new VPosNormCol0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormCol0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormTex0Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Col0	[]verts	=new VPosNormTex0Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Col0>(verts);
		}


		public static void ReadVPosNormTex0Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Col0Col1	[]verts	=new VPosNormTex0Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Col0Col1>(verts);
		}


		public static void ReadVPosNormTex0Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Col0Col1Col2	[]verts	=new VPosNormTex0Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormTex0Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Col0Col1Col2Col3	[]verts	=new VPosNormTex0Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormTex0Tex1Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Col0	[]verts	=new VPosNormTex0Tex1Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Col0>(verts);
		}


		public static void ReadVPosNormTex0Tex1Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Col0Col1	[]verts	=new VPosNormTex0Tex1Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Col0Col1>(verts);
		}


		public static void ReadVPosNormTex0Tex1Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormTex0Tex1Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Col0	[]verts	=new VPosNormTex0Tex1Tex2Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Col0>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Col0Col1	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Col0Col1>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Tex3Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Tex3Col0	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Tex3Col0>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Tex3Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Tex3Col0Col1>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Tex3Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBoneTex0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0	[]verts	=new VPosNormBoneTex0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X		=br.ReadSingle();
				verts[i].Position.Y		=br.ReadSingle();
				verts[i].Position.Z		=br.ReadSingle();
				verts[i].Normal.X		=br.ReadSingle();
				verts[i].Normal.Y		=br.ReadSingle();
				verts[i].Normal.Z		=br.ReadSingle();
				verts[i].BoneIndex.X	=br.ReadSingle();
				verts[i].BoneIndex.Y	=br.ReadSingle();
				verts[i].BoneIndex.Z	=br.ReadSingle();
				verts[i].BoneIndex.W	=br.ReadSingle();
				verts[i].BoneWeights.X	=br.ReadSingle();
				verts[i].BoneWeights.Y	=br.ReadSingle();
				verts[i].BoneWeights.Z	=br.ReadSingle();
				verts[i].BoneWeights.W	=br.ReadSingle();
				verts[i].TexCoord0.X	=br.ReadSingle();
				verts[i].TexCoord0.Y	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 64, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1	[]verts	=new VPosNormBoneTex0Tex1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2	[]verts	=new VPosNormBoneTex0Tex1Tex2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Tex3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Tex3	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Tex3>(verts);
		}


		public static void ReadVPosNormBoneCol0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneCol0	[]verts	=new VPosNormBoneCol0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneCol0>(verts);
		}


		public static void ReadVPosNormBoneCol0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneCol0Col1	[]verts	=new VPosNormBoneCol0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneCol0Col1>(verts);
		}


		public static void ReadVPosNormBoneCol0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneCol0Col1Col2	[]verts	=new VPosNormBoneCol0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneCol0Col1Col2>(verts);
		}


		public static void ReadVPosNormBoneCol0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneCol0Col1Col2Col3	[]verts	=new VPosNormBoneCol0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneCol0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBoneTex0Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Col0	[]verts	=new VPosNormBoneTex0Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Col0>(verts);
		}


		public static void ReadVPosNormBoneTex0Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Col0Col1	[]verts	=new VPosNormBoneTex0Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Col0Col1>(verts);
		}


		public static void ReadVPosNormBoneTex0Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Col0Col1Col2	[]verts	=new VPosNormBoneTex0Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormBoneTex0Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Col0	[]verts	=new VPosNormBoneTex0Tex1Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X		=br.ReadSingle();
				verts[i].Position.Y		=br.ReadSingle();
				verts[i].Position.Z		=br.ReadSingle();
				verts[i].Normal.X		=br.ReadSingle();
				verts[i].Normal.Y		=br.ReadSingle();
				verts[i].Normal.Z		=br.ReadSingle();
				verts[i].BoneIndex.X	=br.ReadSingle();
				verts[i].BoneIndex.Y	=br.ReadSingle();
				verts[i].BoneIndex.Z	=br.ReadSingle();
				verts[i].BoneIndex.W	=br.ReadSingle();
				verts[i].BoneWeights.X	=br.ReadSingle();
				verts[i].BoneWeights.Y	=br.ReadSingle();
				verts[i].BoneWeights.Z	=br.ReadSingle();
				verts[i].BoneWeights.W	=br.ReadSingle();
				verts[i].TexCoord0.X	=br.ReadSingle();
				verts[i].TexCoord0.Y	=br.ReadSingle();
				verts[i].TexCoord1.X	=br.ReadSingle();
				verts[i].TexCoord1.Y	=br.ReadSingle();
				verts[i].Color0.X		=br.ReadSingle();
				verts[i].Color0.Y		=br.ReadSingle();
				verts[i].Color0.Z		=br.ReadSingle();
				verts[i].Color0.W		=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 88, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Col0>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Col0Col1>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Col0	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Col0>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Col0Col1>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Tex3Col0(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Tex3Col0	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Tex3Col0>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2>(verts);
		}


		public static void ReadVPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3	[]verts	=new VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3>(verts);
		}


		public static void ReadVPosNormBone(GraphicsDevice gd, BinaryReader br, out VertexBuffer vb, out int numVerts)
		{
			numVerts	=br.ReadInt32();

			VPosNormBone	[]verts	=new VPosNormBone[numVerts];
			for(int i=0;i < numVerts;i++)
			{
				verts[i].Position.X	=br.ReadSingle();
				verts[i].Position.Y	=br.ReadSingle();
				verts[i].Position.Z	=br.ReadSingle();
			}

			vb	=new VertexBuffer(gd, numVerts * 12, BufferUsage.WriteOnly);
			vb.SetData<VPosNormBone>(verts);
		}
	}
}
