using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

namespace Collada
{
	public struct VPosNormTexTexColor
	{
		public Vector3	Position0;
		public Vector3	Normal0;
		public Vector2	TexCoord0;
		public Vector2	TexCoord1;
		public Vector2	TexCoord2;
		public Color	Color0;
		//public Vector4	BoneIndices;
		//public Vector4	BoneWeights;
	}

	public struct VPosNormTexTexColorAnim
	{
		public Vector3	Position0;
		public Vector3	Normal0;
		public Vector2	TexCoord0;
		public Vector2	TexCoord1;
		public Color	Color0;
		public Vector4	BoneIndices;
		public Vector4	BoneWeights;
	}

	public static class VertexTypes
	{
		static List<Type>	mTypes	=new List<Type>();

		public static void AddType(Type t)
		{
			mTypes.Add(t);
		}

		public static int GetSizeForType(Type t)
		{
			int			i			=0;
			int			sizeSoFar	=0;
			FieldInfo	fi;

			while(true)
			{
				fi	=t.GetField("Position" + i);
				if(fi == null)
				{
					break;
				}
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
				sizeSoFar	+=4;
			}

			return	sizeSoFar;
		}

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

		public static Type GetMatch(int numPos, int numNorm, int numTex, int numColor)
		{
			foreach(Type t in mTypes)
			{
				for(int i=0;i < numPos;i++)
				{
					FieldInfo	fi	=t.GetField("Position" + i);
					if(fi == null)
					{
						continue;
					}
				}
				for(int i=0;i < numNorm;i++)
				{
					FieldInfo	fi	=t.GetField("Normal" + i);
					if(fi == null)
					{
						continue;
					}
				}
				for(int i=0;i < numTex;i++)
				{
					FieldInfo	fi	=t.GetField("TexCoord" + i);
					if(fi == null)
					{
						continue;
					}
				}
				for(int i=0;i < numColor;i++)
				{
					FieldInfo	fi	=t.GetField("Color" + i);
					if(fi == null)
					{
						continue;
					}
				}
				return	t;
			}
			return	typeof(object);
		}
	}
}
