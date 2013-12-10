using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace SharedForms
{
	public partial class CelTweakForm : Form
	{
		internal class CelThreshLevel
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

		BindingList<CelThreshLevel>	mCelValues	=new BindingList<CelThreshLevel>();

		MaterialLib.MaterialLib	mMats;
		GraphicsDevice			mGD;

		int	mLastTexSize	=16;


		public CelTweakForm(GraphicsDevice gd, MaterialLib.MaterialLib mats)
		{
			InitializeComponent();

			mGD		=gd;
			mMats	=mats;

			float	[]thresh;
			float	[]level;

			mMats.GetDefaultValues(out thresh, out level, out mLastTexSize);

			CelThreshLevel	ct	=new CelThreshLevel();
			ct.Level			=level[0];
			ct.Threshold		=thresh[0];

			mCelValues.Add(ct);

			ct				=new CelThreshLevel();
			ct.Level		=level[1];
			ct.Threshold	=thresh[1];
			mCelValues.Add(ct);

			ct				=new CelThreshLevel();
			ct.Level		=level[2];
			ct.Threshold	=thresh[1];
			mCelValues.Add(ct);

			TextureSize.Value	=mLastTexSize;

			CelTweakGrid.DataSource	=mCelValues;
		}


		void OnApplyShading(object sender, EventArgs e)
		{
			int	numLevels	=mCelValues.Count;

			float	[]thresholds	=new float[numLevels - 1];
			float	[]levels		=new float[numLevels];

			for(int i=0;i < (numLevels - 1);i++)
			{
				thresholds[i]	=mCelValues[i].Threshold;
				levels[i]		=mCelValues[i].Level;
			}
			levels[numLevels - 1]	=mCelValues[numLevels - 1].Level;

			mMats.GenerateCelTexture(mGD, 0, (int)TextureSize.Value, thresholds, levels);
			mMats.SetCelTexture(0);
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
