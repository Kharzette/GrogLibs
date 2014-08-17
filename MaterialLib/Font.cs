using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;


namespace MaterialLib
{
	internal class Font
	{
		int		mWidth, mHeight;
		int		mCellWidth, mCellHeight;
		int		mStartChar;
		byte	[]mWidths;


		internal Font(string file)
		{
			FileStream	fs	=new FileStream(file, FileMode.Open, FileAccess.Read);
			if(fs == null)
			{
				return;
			}

			BinaryReader	br	=new BinaryReader(fs);

			mWidth		=br.ReadInt32();
			mHeight		=br.ReadInt32();
			mCellWidth	=br.ReadInt32();
			mCellHeight	=br.ReadInt32();
			mStartChar	=br.ReadByte();

			mWidths	=br.ReadBytes(255);

			br.Close();
			fs.Close();
		}


		internal Vector2 GetUV(char letter, int triangleIndex)
		{
			int	posOffset	=letter - mStartChar;
			if(posOffset < 0)
			{
				return	Vector2.Zero;
			}

			int	numColumns	=mWidth / mCellWidth;
			int	yOffset		=0;
			while(posOffset > numColumns)
			{
				yOffset++;
				posOffset	-=numColumns;
			}

			Vector2	ret;

			ret.X	=posOffset * mCellWidth;
			ret.Y	=yOffset * mCellHeight;

			int	charWidth	=GetCharacterWidth(letter);

			switch(triangleIndex)
			{
				case	0:
					break;
				case	1:
					ret.X	+=charWidth;
					break;
				case	2:
					ret.X	+=charWidth;
					ret.Y	+=mCellHeight;
					break;
				case	3:
					break;
				case	4:
					ret.X	+=charWidth;
					ret.Y	+=mCellHeight;
					break;
				case	5:
					ret.Y	+=mCellHeight;
					break;
				default:
					Debug.Assert(false);
					break;
			}

			ret.X	/=mWidth;
			ret.Y	/=mHeight;

			return	ret;
		}


		internal int GetCharacterWidth()
		{
			return	mCellWidth;
		}


		internal int GetCharacterWidth(char c)
		{
			return	mWidths[c];
		}


		internal int GetCharacterHeight()
		{
			return	mCellHeight;
		}
	}
}
