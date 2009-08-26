﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using Character;

namespace ColladaConvert
{
	public partial class TextureForm : Form
	{
		MaterialLib	mMatLib;		

		BindingList<TextureGridModel>	mTextures	=new BindingList<TextureGridModel>();

		public event EventHandler	eOk;
		public event EventHandler	eCancel;


		public TextureForm(MaterialLib matlib)
		{
			InitializeComponent();

			mMatLib	=matlib;

			Dictionary<string, Texture2D>	textures
				=mMatLib.GetTextures();

			foreach(KeyValuePair<string, Texture2D> tex in textures)
			{
				TextureGridModel	tgm	=new TextureGridModel();
				tgm.Name	=tex.Key;
				mTextures.Add(tgm);
			}
			TextureGrid.DataSource	=mTextures;
		}


		private void OnSelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection
				texSel	=TextureGrid.SelectedRows;

			if(texSel.Count > 0)
			{
				Ok.Enabled	=true;
			}
			else
			{
				Ok.Enabled	=false;
			}
		}


		private void OnOk(object sender, EventArgs e)
		{
			Debug.Assert(TextureGrid.CurrentRow != null);

			eOk((string)TextureGrid.CurrentRow.Cells[0].FormattedValue, null);

			Close();
		}


		private void OnCancel(object sender, EventArgs e)
		{
			eCancel(null, null);

			Close();
		}
	}
}