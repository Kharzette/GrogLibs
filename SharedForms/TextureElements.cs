using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpriteMapLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace SharedForms
{
	public partial class TextureElements : Form
	{
		BindingList<TextureElement>			mTextures	=new BindingList<TextureElement>();
		Dictionary<string, TextureElement>	mTexDict	=new Dictionary<string, TextureElement>();

		//file dialogs
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		//ref to content manager
		ContentManager	mCM;

		//sprite that points to the texelemetns
		Sprite	mSprite;

		//current selection index
		int	mCurTex;

		//events
		public event EventHandler	eTexChanged;
		public event EventHandler	eTexDictChanged;


		public TextureElements(ContentManager cm)
		{
			InitializeComponent();

			TextureGrid.DataSource	=mTextures;

			mCM	=cm;
			mSprite	=new Sprite();
		}


		public Dictionary<string, TextureElement> GetTextures()
		{
			return	mTexDict;
		}


		public void SetCurrentTexElement(string assPath)
		{
			if(mTexDict.ContainsKey(assPath))
			{
				TextureGrid.ClearSelection();

				TextureElement	te	=mTexDict[assPath];

				int	index	=mTextures.IndexOf(te);

				TextureGrid.Rows[index].Selected	=true;
			}
		}


		private void OnLoad(object sender, EventArgs e)
		{
			mOFD.DefaultExt		="*.TexLib";
			mOFD.Filter			="Texture library files (*.TexLib)|*.TexLib|All files (*.*)|*.*";
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			FileStream		fs	=new FileStream(mOFD.FileName, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			mTextures.Clear();

			int	cnt	=br.ReadInt32();
			for(int i=0;i < cnt;i++)
			{
				TextureElement	te	=new TextureElement();
				te.Read(br);
				te.LoadAsset(mCM, null);

				mTextures.Add(te);
			}

			br.Close();
			fs.Close();

			UpdateTexDict();

			TextureGrid.DataSource	=mTextures;

			TextureGrid.Columns[0].AutoSizeMode	=DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
		}


		private void OnSelectionChanged(object sender, EventArgs e)
		{
			//this should change the active sprite
			//to whatever is selected
			DataGridViewSelectedRowCollection	row	=TextureGrid.SelectedRows;

			Debug.Assert(row.Count < 2);	//no multiselect

			if(row.Count == 1)
			{
				mCurTex	=row[0].Index;
				eTexChanged(mTextures[mCurTex], null);
			}
		}


		private void UpdateTexDict()
		{
			mTexDict.Clear();
			foreach(TextureElement te in mTextures)
			{
				mTexDict.Add(te.Asset_Path, te);
			}

			if(eTexDictChanged != null)
			{
				eTexDictChanged(mTexDict, null);
			}
		}
	}
}
