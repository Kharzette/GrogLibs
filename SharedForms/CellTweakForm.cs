using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace SharedForms
{
	public partial class CellTweakForm : Form
	{
		internal class CellThreshLevel
		{
			float	mThreshold;
			float	mLevel;

			public float Threshold
			{
				get { return mThreshold; }
				set { mThreshold = value; }
			}

			public float Level
			{
				get { return mLevel; }
				set { mLevel = value; }
			}
		}

		BindingList<CellThreshLevel>	mCellValues	=new BindingList<CellThreshLevel>();

		MaterialLib.MaterialLib	mMats;
		GraphicsDevice			mGD;

		int	mLastTexSize	=16;


		public CellTweakForm(GraphicsDevice gd, MaterialLib.MaterialLib mats)
		{
			InitializeComponent();

			mGD		=gd;
			mMats	=mats;

			CellTweakGrid.DataSource	=mCellValues;
		}


		void OnApplyShading(object sender, EventArgs e)
		{
			int	numLevels	=mCellValues.Count;

			float	[]thresholds	=new float[numLevels - 1];
			float	[]levels		=new float[numLevels];

			for(int i=0;i < (numLevels - 1);i++)
			{
				thresholds[i]	=mCellValues[i].Threshold;
				levels[i]		=mCellValues[i].Level;
			}
			levels[numLevels - 1]	=mCellValues[numLevels - 1].Level;

			mMats.GenerateCellTexture(mGD, 0, (int)TextureSize.Value, thresholds, levels);
			mMats.SetCellTexture(0);
		}


		void OnTextureSizeChanged(object sender, EventArgs e)
		{
			int	val	=(int)TextureSize.Value;

			if(val > mLastTexSize)
			{
				val				=Mathery.NextPowerOfTwo(val);
				mLastTexSize	=val;
				
				TextureSize.Value	=val;
			}
			else if(val < mLastTexSize)
			{
				val				=Mathery.PreviousPowerOfTwo(val);
				mLastTexSize	=val;
				
				TextureSize.Value	=val;
			}
		}
	}
}
