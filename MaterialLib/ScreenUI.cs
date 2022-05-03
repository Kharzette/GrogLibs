using System.Collections.Generic;


namespace MaterialLib;
/*
internal class UIData
{
	internal string		mMatName;
	internal GumpVert	[]mVerts;
	internal Vector4	mColor;
	internal Vector2	mPosition, mSecondLayerOffset;
	internal Vector2	mScale;
	internal string		mTexture, mTexture2;
}

internal struct GumpVert
{
	internal Vector2	Position;
	internal Half4		TexCoord04;
}


public class ScreenUI
{
	Device				mGD;
	Buffer				mVB;
	MaterialLib			mMatLib;
	VertexBufferBinding	mVBB;

	GumpVert	[]mGumpBuf;

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

		mGumpBuf	=new GumpVert[mMaxGumps];

		BufferDescription	bDesc	=new BufferDescription(
			mMaxGumps * 6 * 16,
			ResourceUsage.Dynamic, BindFlags.VertexBuffer,
			CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

		mVB		=Buffer.Create<GumpVert>(gd, mGumpBuf, bDesc);
		mVBB	=new VertexBufferBinding(mVB, 16, 0);
	}


	public void FreeAll()
	{
		mVB.Dispose();

		mGumpBuf	=null;

		mGumps.Clear();
	}


	public void AddGump(string matName, string texName, string texName2,
		string id, Vector4 color, Vector2 pos, Vector2 scale)
	{
		UIData	uid	=new UIData();

		uid.mMatName			=matName;
		uid.mColor				=color;			
		uid.mPosition			=pos;
		uid.mSecondLayerOffset	=Vector2.Zero;
		uid.mScale				=scale;
		uid.mTexture			=texName;
		uid.mTexture2			=texName2;
		uid.mVerts				=new GumpVert[6];

		Texture2D	tex	=mMatLib.GetTexture2D(texName);
		if(tex == null)
		{
			return;
		}

		//ok if this one is null
		Texture2D	tex2	=mMatLib.GetTexture2D(texName2);

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


	public void ModifyGumpSecondLayerOffset(string id, Vector2 pos)
	{
		if(!mGumps.ContainsKey(id))
		{
			return;
		}
		mGumps[id].mSecondLayerOffset	=pos;
	}


	void MakeQuad(GumpVert []tv, RectangleF rect)
	{
		tv[0].Position		=rect.TopLeft;
		tv[0].TexCoord04	=Vector4.Zero;

		tv[1].Position		=rect.TopRight;
		tv[1].TexCoord04	=Vector4.UnitX + Vector4.UnitZ;

		tv[2].Position		=rect.BottomRight;
		tv[2].TexCoord04	=Vector4.One;

		tv[3].Position		=rect.TopLeft;
		tv[3].TexCoord04	=Vector4.Zero;

		tv[4].Position		=rect.BottomRight;
		tv[4].TexCoord04	=Vector4.One;

		tv[5].Position		=rect.BottomLeft;
		tv[5].TexCoord04	=Vector4.UnitY + Vector4.UnitW;
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
			ds.Write<GumpVert>(mGumpBuf[i]);
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

		int	offset	=0;
		foreach(KeyValuePair<string, UIData> str in mGumps)
		{
			int		len		=str.Value.mVerts.Length;
			string	matName	=str.Value.mMatName;

			mMatLib.SetMaterialParameter(matName, "mView", view);
			mMatLib.SetMaterialParameter(matName, "mProjection", proj);
			mMatLib.SetMaterialParameter(matName, "mTextPosition", str.Value.mPosition);
			mMatLib.SetMaterialParameter(matName, "mSecondLayerOffset", str.Value.mSecondLayerOffset);
			mMatLib.SetMaterialParameter(matName, "mTextScale", str.Value.mScale);
			mMatLib.SetMaterialParameter(matName, "mTextColor", str.Value.mColor);
			mMatLib.SetMaterialTexture(matName, "mTexture", str.Value.mTexture);
			mMatLib.SetMaterialTexture(matName, "mTexture2", str.Value.mTexture2);

			mMatLib.ApplyMaterialPass(matName, dc, 0);

			dc.Draw(len, offset);

			offset	+=len;
		}
	}
}*/

