using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class SceneNode
	{
		private	string			mName, mSID, mType;
		private	Vector3			mTranslation, mScale;
		private	Vector4			mRotX, mRotY, mRotZ;

		private	Dictionary<string, SceneNode>	mChildren	=new Dictionary<string, SceneNode>();

		//skin instance stuff
		private string	mInstanceControllerURL;
		private string	mSkeleton;
		private string	mInstanceGeometryURL;

		private	List<InstanceMaterial>	mBindMaterials;

		public SceneNode()
		{
			mBindMaterials	=new List<InstanceMaterial>();
		}


		public Matrix GetMatrix()
		{
			Matrix	mat	=Matrix.Identity;
			mat.Translation	=mTranslation;
			mat.M11			=mRotX.X;
			mat.M12			=mRotX.Y;
			mat.M13			=mRotX.Z;
			mat.M21			=mRotY.X;
			mat.M22			=mRotY.Y;
			mat.M23			=mRotY.Z;
			mat.M31			=mRotZ.X;
			mat.M32			=mRotZ.Y;
			mat.M33			=mRotZ.Z;

			return	mat;
		}


		public void LoadNode(XmlReader r)
		{
			r.MoveToFirstAttribute();

			int attcnt	=r.AttributeCount;

			while(attcnt > 0)
			{
				//look for valid attributes for nodes
				if(r.Name == "name")
				{
					mName	=r.Value;
				}
				else if(r.Name == "sid")
				{
					mSID	=r.Value;
				}
				else if(r.Name == "type")
				{
					mType	=r.Value;
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

						Collada.GetVectorFromString(r.Value,out mTranslation);
					}
				}
				else if(r.Name == "instance_geometry")
				{
					if(r.AttributeCount > 0)
					{
						r.MoveToFirstAttribute();
						mInstanceGeometryURL	=r.Value;
					}
				}
				else if(r.Name == "scale")
				{
					int attcnt2	=r.AttributeCount;

					if(attcnt2 > 0)
					{
						//skip to the next element, the actual value
						r.Read();

						Collada.GetVectorFromString(r.Value, out mScale);
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
							mBindMaterials.Add(m);
						}
					}
					else
					{
						if(!bEmpty)
						{
							mBindMaterials.Add(m);
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

						mInstanceControllerURL	=r.Value;
					}
				}
				else if(r.Name == "skeleton")
				{
					r.Read();
					mSkeleton	=r.Value;
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
							Collada.GetVectorFromString(r.Value, out mRotX);
						}
						else if(axis == "rotateY")
						{
							Collada.GetVectorFromString(r.Value, out mRotY);
						}
						else if(axis == "rotateZ")
						{
							Collada.GetVectorFromString(r.Value, out mRotZ);
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
						child.LoadNode(r);

						mChildren.Add(id, child);
					}
					else
					{
						return;
					}
				}
			}
		}
	}
}