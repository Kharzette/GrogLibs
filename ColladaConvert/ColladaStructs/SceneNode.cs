using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Character;

namespace ColladaConvert
{
	public class SceneNode
	{
		string	mName, mType, mSID;
		Matrix	mMat;

		Dictionary<string, NodeElement>	mElements	=new Dictionary<string, NodeElement>();
		Dictionary<string, SceneNode>	mChildren	=new Dictionary<string, SceneNode>();

		//skin instance stuff
		private string	mInstanceControllerURL;
		private string	mSkeleton;
		private string	mInstanceGeometryURL;

		private	List<InstanceMaterial>	mBindMaterials;


		public Dictionary<string, SceneNode>	GetChildren()
		{
			return	mChildren;
		}


		public SceneNode()
		{
			mBindMaterials	=new List<InstanceMaterial>();
			mMat			=Matrix.Identity;
		}


		public bool GetMatrixForBone(string boneName, out Matrix outMat)
		{
			if(mName == boneName)
			{
				outMat	=GetMatrix();
				return	true;
			}
			foreach(KeyValuePair<string, SceneNode> sn in mChildren)
			{
				if(sn.Value.GetMatrixForBone(boneName, out outMat))
				{
					//mul by parent
					outMat	*=GetMatrix();
					return	true;
				}
			}
			outMat	=Matrix.Identity;
			return	false;
		}


		//matrix returned here will not be multiplied by parents
		public bool GetMatrixForBoneNonRecursive(string boneName, out Matrix outMat)
		{
			if(mName == boneName)
			{
				outMat	=GetMatrix();
				return	true;
			}
			foreach(KeyValuePair<string, SceneNode> sn in mChildren)
			{
				if(sn.Value.GetMatrixForBoneNonRecursive(boneName, out outMat))
				{
					return	true;
				}
			}
			outMat	=Matrix.Identity;
			return	false;
		}


		public void AddToGameSkeleton(out GSNode gsn)
		{
			gsn	=new GSNode();
			gsn.SetName(mName);
			gsn.SetChannels(GetGameChannels());

			foreach(KeyValuePair<string, SceneNode> k in mChildren)
			{
				GSNode	kid	=new GSNode();
				k.Value.AddToGameSkeleton(out kid);

				gsn.AddChild(kid);
			}
		}


		public List<ChannelTarget> GetGameChannels()
		{
			List<ChannelTarget>	ret	=new List<ChannelTarget>();

			foreach(KeyValuePair<string, NodeElement> ne in mElements)
			{
				ChannelTarget	gc	=null;
				Vector4				val	=Vector4.Zero;
				if(ne.Value is Rotate)
				{
					Rotate	r	=(Rotate)ne.Value;
					gc	=new ChannelTarget(Character.Channel.ChannelType.ROTATE, ne.Key);

					val.X	=r.mValue.X;
					val.Y	=r.mValue.Y;
					val.Z	=r.mValue.Z;
					val.W	=r.mValue.W;
				}
				else if(ne.Value is Scale)
				{
					Scale	s	=(Scale)ne.Value;
					gc	=new ChannelTarget(Character.Channel.ChannelType.SCALE, ne.Key);

					val.X	=s.mValue.X;
					val.Y	=s.mValue.Y;
					val.Z	=s.mValue.Z;
				}
				else if(ne.Value is Translate)
				{
					Translate	t	=(Translate)ne.Value;
					gc	=new ChannelTarget(Character.Channel.ChannelType.TRANSLATE, ne.Key);

					val.X	=t.mValue.X;
					val.Y	=t.mValue.Y;
					val.Z	=t.mValue.Z;
				}
				gc.SetValue(val);
				ret.Add(gc);
			}
			return	ret;
		}


		public bool GetElement(string boneName, string elName, out NodeElement el)
		{
			if(mName == boneName)
			{
				if(mElements.ContainsKey(elName))
				{
					el	=mElements[elName];
					return	true;
				}
				el	=null;
				return	false;
			}
			foreach(KeyValuePair<string, SceneNode> sn in mChildren)
			{
				if(sn.Value.GetElement(boneName, elName, out el))
				{
					return	true;
				}
			}
			el	=null;
			return	false;
		}


		public bool ModifyMatrixForBone(string boneName, Matrix mat)
		{
			if(mName == boneName)
			{
				mMat	*=mat;
				return	true;
			}
			foreach(KeyValuePair<string, SceneNode> sn in mChildren)
			{
				if(sn.Value.ModifyMatrixForBone(boneName, mat))
				{
					return	true;
				}
			}
			return	false;
		}


		public void AdjustRootMatrixForMax()
		{
			//mMat	*=Matrix.CreateTranslation(50.0f, 80.0f, 150.0f);
			mMat	*=Matrix.CreateFromYawPitchRoll(0, MathHelper.ToRadians(-90), MathHelper.ToRadians(180));
		}


		public string GetInstanceControllerURL()
		{
			return	mInstanceControllerURL;
		}


		public Matrix GetMatrix()
		{
			//compose from elements
			mMat	=Matrix.Identity;

			//this should probably be cached
			foreach(KeyValuePair<string,NodeElement> el in mElements)
			{
				if(el.Value is Rotate)
				{
					mMat	*=el.Value.GetMatrix();
				}
				else
				{
					mMat	*=el.Value.GetMatrix();
				}
			}

			return	mMat;
		}


		public string GetName()
		{
			return	mName;
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

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}
				else if(r.NodeType != XmlNodeType.Element && r.Name == "node")
				{
					return;
				}
				else if(r.NodeType != XmlNodeType.Element)
				{
					continue;
				}

				if(r.Name == "translate")
				{
					Translate	t	=new Translate(r);

					//sids are optional
					string	sid	=t.GetSID();
					if(sid == null)
					{
						sid	="translate";
					}
					mElements.Add(sid, t);
				}
				else if(r.Name == "rotate")
				{
					Rotate	rot	=new Rotate(r);

					//sids are optional
					string	sid	=rot.GetSID();
					if(sid == null)
					{
						sid	="rotate";
					}
					mElements.Add(sid, rot);
				}
				else if(r.Name == "scale")
				{
					Scale	s	=new Scale(r);

					//sids are optional
					string	sid	=s.GetSID();
					if(sid == null)
					{
						sid	="scale";
					}
					mElements.Add(sid, s);
				}
				else if(r.Name == "skew")
				{
					Skew	s	=new Skew(r);
					//sids are optional
					string	sid	=s.GetSID();
					if(sid == null)
					{
						sid	="skew";
					}
					mElements.Add(sid, s);
				}
				else if(r.Name == "matrix")
				{
					MatrixL	mat	=new MatrixL(r);

					//sids are optional
					string	sid	=mat.GetSID();
					if(sid == null)
					{
						sid	="matrix";
					}
					mElements.Add(sid, mat);
				}
				else if(r.Name == "LookAt")
				{
					LookAt	la	=new LookAt(r);

					//sids are optional
					string	sid	=la.GetSID();
					if(sid == null)
					{
						sid	="lookat";
					}
					mElements.Add(sid, la);
				}
				else if(r.Name == "instance_geometry")
				{
					r.MoveToFirstAttribute();
					mInstanceGeometryURL	=r.Value;
				}
				else if(r.Name == "instance_material")
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
					mBindMaterials.Add(m);
				}
				else if(r.Name == "instance_controller")
				{
					r.MoveToFirstAttribute();
					mInstanceControllerURL	=r.Value;
				}
				else if(r.Name == "skeleton")
				{
					r.Read();
					mSkeleton	=r.Value;
				}
				else if(r.Name == "node")
				{
					r.MoveToFirstAttribute();
					string	id	=r.Value;

					SceneNode child	=new SceneNode();
					child.LoadNode(r);

					mChildren.Add(id, child);
				}
			}
		}
	}
}