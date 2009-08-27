using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;

namespace ColladaConvert
{
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
	}
}
