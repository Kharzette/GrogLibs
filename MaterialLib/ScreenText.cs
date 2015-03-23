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


	internal class StringData
	{
		internal TextVert	[]mVerts;
		internal Vector4	mColor;
		internal Vector2	mPosition;
		internal Vector2	mScale;
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

		Dictionary<string, StringData>	mStrings	=new Dictionary<string, StringData>();


		public ScreenText(Device gd,
			MaterialLib matLib,
			string fontTexName,
			int maxCharacters)
		{
			mGD				=gd;
			mMaxCharacters	=maxCharacters;
			mMatLib			=matLib;
			mFontTexName	=fontTexName;

			mTextBuf	=new TextVert[mMaxCharacters * 6];

			BufferDescription	bDesc	=new BufferDescription(
				mMaxCharacters * 6 * 12,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<TextVert>(gd, mTextBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 12, 0);
		}


		public void AddString(string fontName, string text, string id,
			Vector4 color, Vector2 position, Vector2 scale)
		{
			StringData	sd	=new StringData();

			sd.mVerts	=new TextVert[text.Length * 6];

			Font	font	=mMatLib.GetFont(fontName);
			if(font == null)
			{
				return;
			}

			CopyLetters(sd.mVerts, font, text);

			sd.mColor		=color;
			sd.mPosition	=position;
			sd.mScale		=scale;

			mStrings.Add(id, sd);

			mbDirty	=true;
		}


		public void FreeAll()
		{
			mStrings.Clear();
			mMatLib.FreeAll();
			mVB.Dispose();

			mGD		=null;
		}


		public void ModifyStringColor(string id, Vector4 color)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}
			mStrings[id].mColor	=color;
		}


		public void ModifyStringScale(string id, Vector2 scale)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}
			mStrings[id].mScale	=scale;
		}


		public void ModifyStringPosition(string id, Vector2 pos)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}
			mStrings[id].mPosition	=pos;
		}


		public void ModifyStringText(string fontName, string text, string id)
		{
			if(!mStrings.ContainsKey(id))
			{
				return;
			}

			StringData	sd	=mStrings[id];

			Font	font	=mMatLib.GetFont(fontName);
			if(font == null)
			{
				return;
			}

			sd.mVerts	=new TextVert[text.Length * 6];

			CopyLetters(sd.mVerts, font, text);

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
			int	curWidth	=0;
			for(int i=0;i < text.Length;i++)
			{
				int	nextWidth	=curWidth + font.GetCharacterWidth(text[i]);

				Vector2	xCoord	=Vector2.UnitX * curWidth;
				Vector2	xCoord2	=Vector2.UnitX * nextWidth;
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

				curWidth	=nextWidth;
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
			foreach(KeyValuePair<string, StringData> str in mStrings)
			{
				//make sure we donut blast beyond the end
				int	nextSize	=mNumVerts + str.Value.mVerts.Length;
				if(nextSize > mTextBuf.Length)
				{
					//TODO, warn
					continue;
				}
				str.Value.mVerts.CopyTo(mTextBuf, mNumVerts);
				
				mNumVerts	+=str.Value.mVerts.Length;
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
			//if this assert fires, make sure
			//all text modification stuff happens
			//before the call to update
			Debug.Assert(!mbDirty);

			if(mNumVerts <= 0)
			{
				return;
			}

			dc.InputAssembler.SetVertexBuffers(0, mVBB);

			mMatLib.SetMaterialParameter("Text", "mView", view);
			mMatLib.SetMaterialParameter("Text", "mProjection", proj);
			mMatLib.SetMaterialFontTexture("Text", "mTexture", mFontTexName);

			int	offset	=0;
			foreach(KeyValuePair<string, StringData> str in mStrings)
			{
				int	len	=str.Value.mVerts.Length;

				mMatLib.SetMaterialParameter("Text", "mTextPosition", str.Value.mPosition);
				mMatLib.SetMaterialParameter("Text", "mTextScale", str.Value.mScale);
				mMatLib.SetMaterialParameter("Text", "mTextColor", str.Value.mColor);

				mMatLib.ApplyMaterialPass("Text", dc, 0);

				dc.Draw(len, offset);

				offset	+=len;
			}
		}
	}
}
