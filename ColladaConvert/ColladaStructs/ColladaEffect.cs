using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public struct VPosNormTexAnim
	{
		public Vector3	Position0;
		public Vector3	Normal0;
		public Vector4	BoneIndex0;
		public Vector4	BoneWeights;
		public Vector2	TexCoord0;
	}

	class ColladaEffect
	{
		private string	mName;
		private string	mTextureID;
		private int		mChannel;

		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			mName	=r.Value;

			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "texture")
				{
					if(r.NodeType == XmlNodeType.Element)
					{
						r.MoveToFirstAttribute();
						mTextureID	=r.Value;

						r.MoveToNextAttribute();
						int.TryParse(r.Value, out mChannel);
					}
				}
				else if(r.Name == "effect")
				{
					return;
				}
			}
		}
	}
}