using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace SharedForms
{
	public class MaterialGridModel : BindingList<MaterialLib.GUIStates>
	{
		public MaterialGridModel(Dictionary<string, MaterialLib.Material> mats)
		{
			foreach(KeyValuePair<string, MaterialLib.Material> m in mats)
			{
				Add(m.Value.GetGUIStates());
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
		string					mName;

		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
	}
}
