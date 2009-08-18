using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Character;

namespace ColladaConvert
{
	public class MaterialGridModel : BindingList<Character.Material>
	{
		public MaterialGridModel(Dictionary<string, Character.Material> mats)
		{
			foreach(KeyValuePair<string, Character.Material> m in mats)
			{
				Add(m.Value);
			}
		}
	}

	public class TextureGridModel
	{
		string	mName;
		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
	}

	public class ShaderGridModel
	{
		string	mName;
		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
	}
}
