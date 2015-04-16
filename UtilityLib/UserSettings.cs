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
		public const float	AnalogTurnMultiplier	=0.5f;
		public const float	KeyTurnMultiplier		=300f;

		public int	mTurnSensitivity	=5;
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
			mbYAxisInverted		=br.ReadBoolean();
			mbMultiSampling		=br.ReadBoolean();
			mbFullScreen		=br.ReadBoolean();
			mbESDF				=br.ReadBoolean();

			br.Close();
			fs.Close();
		}
	}
}
