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
	internal class UIData
	{
		internal TextVert	[]mVerts;
		internal Vector4	mColor;
		internal Vector2	mPosition;
		internal Vector2	mScale;
		internal string		mTexture;
	}


	public class ScreenUI
	{
		Device				mGD;
		Buffer				mVB;
		MaterialLib			mMatLib;
		VertexBufferBinding	mVBB;

		TextVert	[]mGumpBuf;

		bool	mbDirty;
		int		mMaxGumps;
		int		mNumVerts;

		Dictionary<string, UIData>	mGumps	=new Dictionary<string, UIData>();


		public ScreenUI(Device gd,
			MaterialLib matLib,
			int maxGumps)
		{
			mGD				=gd;
			mMaxGumps		=maxGumps;
			mMatLib			=matLib;

			mGumpBuf	=new TextVert[mMaxGumps];

			BufferDescription	bDesc	=new BufferDescription(
				mMaxGumps * 6 * 12,
				ResourceUsage.Dynamic, BindFlags.VertexBuffer,
				CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

			mVB		=Buffer.Create<TextVert>(gd, mGumpBuf, bDesc);
			mVBB	=new VertexBufferBinding(mVB, 12, 0);
		}


		public void FreeAll()
		{
			mVB.Dispose();

			mGumpBuf	=null;

			mGumps.Clear();
		}


		public void AddGump(string texName, string id,
			Vector4 color, Vector2 pos, Vector2 scale)
		{
			UIData	uid	=new UIData();

			uid.mColor		=color;
			uid.mPosition	=pos;
			uid.mScale		=scale;
			uid.mTexture	=texName;
			uid.mVerts		=new TextVert[6];

			Texture2D	tex	=mMatLib.GetTexture2D(texName);
			if(tex == null)
			{
				return;
			}

			RectangleF	rect	=new RectangleF();

			rect.X		=rect.Y	=0f;
			rect.Width	=tex.Description.Width;
			rect.Height	=tex.Description.Height;

			MakeQuad(uid.mVerts, rect);

			mGumps.Add(id, uid);

			mbDirty	=true;
		}


		public void ModifyGumpColor(string id, Vector4 color)
		{
			if(!mGumps.ContainsKey(id))
			{
				return;
			}
			mGumps[id].mColor	=color;
		}


		public void ModifyGumpScale(string id, Vector2 scale)
		{
			if(!mGumps.ContainsKey(id))
			{
				return;
			}
			mGumps[id].mScale	=scale;
		}


		public void ModifyGumpPosition(string id, Vector2 pos)
		{
			if(!mGumps.ContainsKey(id))
			{
				return;
			}
			mGumps[id].mPosition	=pos;
		}


		void MakeQuad(TextVert []tv, RectangleF rect)
		{
			tv[0].Position	=rect.TopLeft;
			tv[0].TexCoord0	=Vector2.Zero;

			tv[1].Position	=rect.TopRight;
			tv[1].TexCoord0	=Vector2.UnitX;

			tv[2].Position	=rect.BottomRight;
			tv[2].TexCoord0	=Vector2.One;

			tv[3].Position	=rect.TopLeft;
			tv[3].TexCoord0	=Vector2.Zero;

			tv[4].Position	=rect.BottomRight;
			tv[4].TexCoord0	=Vector2.One;

			tv[5].Position	=rect.BottomLeft;
			tv[5].TexCoord0	=Vector2.UnitY;
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
			foreach(KeyValuePair<string, UIData> uid in mGumps)
			{
				uid.Value.mVerts.CopyTo(mGumpBuf, mNumVerts);
				
				mNumVerts	+=uid.Value.mVerts.Length;
			}

			DataStream	ds;
			dc.MapSubresource(mVB, MapMode.WriteDiscard, MapFlags.None, out ds);

			for(int i=0;i < mNumVerts;i++)
			{
				ds.Write<TextVert>(mGumpBuf[i]);
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

			int	offset	=0;
			foreach(KeyValuePair<string, UIData> str in mGumps)
			{
				int	len	=str.Value.mVerts.Length;

				mMatLib.SetMaterialParameter("Text", "mTextPosition", str.Value.mPosition);
				mMatLib.SetMaterialParameter("Text", "mTextScale", str.Value.mScale);
				mMatLib.SetMaterialParameter("Text", "mTextColor", str.Value.mColor);
				mMatLib.SetMaterialTexture("Text", "mTexture", str.Value.mTexture);

				mMatLib.ApplyMaterialPass("Text", dc, 0);

				dc.Draw(len, offset);

				offset	+=len;
			}
		}
	}
}
