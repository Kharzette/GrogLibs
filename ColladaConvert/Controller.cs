using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Controller
	{
		private Skin	mSkin;
		private Matrix	[]mBones;

		private Dictionary<string, Int32>	mBIdxMap	=new Dictionary<string,int>();


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


		public Skin	GetSkin()
		{
			return	mSkin;
		}

		
		public void BuildBones(GraphicsDevice g)
		{
			int	idx	=0;

			//grab the source key for the joints array
			string	keyName	="";
			for(int i=0;i < mSkin.mJoints.mInputs.Count;i++)
			{
				if(mSkin.mJoints.mInputs[i].mSemantic == "JOINT")
				{
					keyName	=mSkin.mJoints.mInputs[i].mSource;
				}
			}

			//strip off the # in the key
			keyName	=keyName.Substring(1);

			//find the bone name list via the keyName
			Source	src	=mSkin.mSources[keyName];

			foreach(string name in src.mNames)
			{
				mBIdxMap.Add(name, idx++);
			}

			//alloc bones
			mBones	=new Matrix[idx];

			//grab the matrix key
			for(int i=0;i < mSkin.mJoints.mInputs.Count;i++)
			{
				if(mSkin.mJoints.mInputs[i].mSemantic == "INV_BIND_MATRIX")
				{
					keyName	=mSkin.mJoints.mInputs[i].mSource;
				}
			}

			//strip off the # in the key
			keyName	=keyName.Substring(1);

			//find the bone list via the keyName
			src	=mSkin.mSources[keyName];

			Matrix	mat;
			string	arr	=keyName + "-array";
			for(int i=0;i < idx;i++)
			{
				int	fidx	=i * 16;
				mat.M11	=src.mFloats[0 + fidx];
				mat.M12	=src.mFloats[1 + fidx];
				mat.M13	=src.mFloats[2 + fidx];
				mat.M14	=src.mFloats[3 + fidx];
				mat.M21	=src.mFloats[4 + fidx];
				mat.M22	=src.mFloats[5 + fidx];
				mat.M23	=src.mFloats[6 + fidx];
				mat.M24	=src.mFloats[7 + fidx];
				mat.M31	=src.mFloats[8 + fidx];
				mat.M32	=src.mFloats[9 + fidx];
				mat.M33	=src.mFloats[10 + fidx];
				mat.M34	=src.mFloats[11 + fidx];
				mat.M41	=src.mFloats[12 + fidx];
				mat.M42	=src.mFloats[13 + fidx];
				mat.M43	=src.mFloats[14 + fidx];
				mat.M44	=src.mFloats[15 + fidx];

				//copy matrix to bone list
				mBones[i]	=mat;
			}
		}
	}
}