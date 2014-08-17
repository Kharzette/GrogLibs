using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using MaterialLib;
using SharpDX;
using SharpDX.Direct3D11;
using UtilityLib;

using Buffer	=SharpDX.Direct3D11.Buffer;
using Device	=SharpDX.Direct3D11.Device;


namespace MaterialLib
{
	internal struct TextVert
	{
		internal Vector2	Position;
		internal Half2		TexCoord0;
	}


	public class ScreenText
	{
		Device				mGD;
		Buffer				mVB;
		MaterialLib			mMatLib;
		VertexBufferBinding	mVBB;

		TextVert	[]mTextBuf;

		bool	mbDirty;
		int		mMaxCharacters;
		int		mNumVerts;
		string	mFontTexName;

		Dictionary<string, TextVert[]>	mStrings	=new Dictionary<string, TextVert[]>();


		public ScreenText(Device gd,
			MaterialLib matLib,
			string fontTexName,
			int maxCharacters)
		{
			mGD				=gd;
			mMaxCharacters	=maxCharacters;
			mMatLib			=matLib;
			mFontTexName	=fontTexName;

			mTextBuf	=new TextVert[mMaxCharacters];

			BufferDescription	bDesc	=new BufferDescription(
				mMaxCharacters * 6 * 12,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<TextVert>(gd, mTextBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 12, 0);
		}


		public void AddString(string fontName, string text, string id)
		{
			TextVert	[]textVerts	=new TextVert[text.Length * 6];

			Font	font	=mMatLib.GetFont(fontName);
			if(font == null)
			{
				return;
			}

			CopyLetters(textVerts, font, text);

			mStrings.Add(id, textVerts);

			mbDirty	=true;
		}


		public void ModifyString(string fontName, string text, string id)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}

			Font	font	=mMatLib.GetFont(fontName);
			if(font == null)
			{
				return;
			}

			mStrings[id]	=new TextVert[text.Length * 6];

			CopyLetters(mStrings[id], font, text);

			mbDirty	=true;
		}


		public void DeleteString(string id)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}

			mStrings.Remove(id);
		}


		void CopyLetters(TextVert []tv, Font font, string text)
		{
			for(int i=0;i < text.Length;i++)
			{
				Vector2	xCoord	=Vector2.UnitX * i * font.GetCharacterWidth();
				Vector2	xCoord2	=Vector2.UnitX * (i + 1) * font.GetCharacterWidth();
				Vector2	yCoord	=Vector2.Zero;
				Vector2	yCoord2	=Vector2.UnitY * font.GetCharacterHeight();

				tv[(i * 6)].Position	=xCoord;
				tv[(i * 6)].TexCoord0	=font.GetUV(text[i], 0);

				tv[(i * 6) + 1].Position	=xCoord2;
				tv[(i * 6) + 1].TexCoord0	=font.GetUV(text[i], 1);

				tv[(i * 6) + 2].Position	=xCoord2 + yCoord2;
				tv[(i * 6) + 2].TexCoord0	=font.GetUV(text[i], 2);

				tv[(i * 6) + 3].Position	=xCoord;
				tv[(i * 6) + 3].TexCoord0	=font.GetUV(text[i], 3);

				tv[(i * 6) + 4].Position	=yCoord2 + xCoord2;
				tv[(i * 6) + 4].TexCoord0	=font.GetUV(text[i], 4);

				tv[(i * 6) + 5].Position	=xCoord + yCoord2;
				tv[(i * 6) + 5].TexCoord0	=font.GetUV(text[i], 5);
			}
		}


		public void Update(DeviceContext dc)
		{
			if(mbDirty)
			{
				RebuildVB(dc);
				mbDirty	=false;
			}
		}


		void RebuildVB(DeviceContext dc)
		{
			mNumVerts	=0;
			foreach(KeyValuePair<string, TextVert[]> str in mStrings)
			{
				str.Value.CopyTo(mTextBuf, mNumVerts);
				
				mNumVerts	+=str.Value.Length;
			}

			DataStream	ds;
			dc.MapSubresource(mVB, MapMode.WriteDiscard, MapFlags.None, out ds);

			for(int i=0;i < mNumVerts;i++)
			{
				ds.Write<TextVert>(mTextBuf[i]);
			}

			dc.UnmapSubresource(mVB, 0);
		}


		public void Draw(DeviceContext dc, Matrix view, Matrix proj)
		{
			if(mNumVerts <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter("Text", "mView", view);
			mMatLib.SetMaterialParameter("Text", "mProjection", proj);
			mMatLib.SetMaterialFontTexture("Text", "mTexture", mFontTexName);

			mMatLib.ApplyMaterialPass("Text", dc, 0);

			dc.Draw(mNumVerts, 0);
		}
	}
}
