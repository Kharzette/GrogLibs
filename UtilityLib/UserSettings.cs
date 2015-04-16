using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UtilityLib
{
	public class UserSettings
	{
		public const float	MouseTurnMultiplier		=0.8f;
		public const float	AnalogTurnMultiplier	=300f;
		public const float	KeyTurnMultiplier		=300f;

		public int	mTurnSensitivity	=5;
		public bool	mbXAxisInverted		=false;
		public bool	mbYAxisInverted		=false;
		public bool	mbMultiSampling		=false;
		public bool	mbFullScreen		=false;
		public bool	mbESDF				=false;


		public UserSettings()
		{
			ReadSettings();
		}


		public void SaveSettings()
		{
			FileStream		fs	=new FileStream("ControlSettings.sav", FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(mTurnSensitivity);
			bw.Write(mbXAxisInverted);
			bw.Write(mbYAxisInverted);
			bw.Write(mbMultiSampling);
			bw.Write(mbFullScreen);
			bw.Write(mbESDF);

			bw.Close();
			fs.Close();
		}


		void ReadSettings()
		{
			if(!File.Exists("ControlSettings.sav"))
			{
				return;
			}

			FileStream		fs	=new FileStream("ControlSettings.sav", FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			mTurnSensitivity	=br.ReadInt32();
			mbXAxisInverted		=br.ReadBoolean();
			mbYAxisInverted		=br.ReadBoolean();
			mbMultiSampling		=br.ReadBoolean();
			mbFullScreen		=br.ReadBoolean();
			mbESDF				=br.ReadBoolean();

			br.Close();
			fs.Close();
		}
	}
}
