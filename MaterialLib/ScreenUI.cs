using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Vortice.Mathematics.PackedVector;
using UtilityLib;


namespace MaterialLib;

internal class UIData
{
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
	GraphicsDevice	mGD;
	StuffKeeper		mSK;
	ID3D11Buffer	mVB;

	GumpVert	[]mGumpBuf;

	bool	mbDirty;
	int		mMaxGumps;
	int		mNumVerts;

	Dictionary<string, UIData>	mGumps	=new Dictionary<string, UIData>();


	public ScreenUI(GraphicsDevice gd, StuffKeeper sk, int maxGumps)
	{
		mGD			=gd;
		mSK			=sk;
		mMaxGumps	=maxGumps;

		mGumpBuf	=new GumpVert[mMaxGumps];

		BufferDescription	bDesc	=new BufferDescription(
			mMaxGumps * 6 * 16, BindFlags.VertexBuffer, ResourceUsage.Dynamic,
			CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

		mVB		=gd.GD.CreateBuffer<GumpVert>(mGumpBuf, bDesc);
	}


	public void FreeAll()
	{
		mVB.Dispose();

		mGumpBuf	=null;

		mGumps.Clear();
	}


	public void AddGump(string texName, string texName2,
		string id, Vector4 color, Vector2 pos, Vector2 scale)
	{
		UIData	uid	=new UIData();

		uid.mColor				=color;			
		uid.mPosition			=pos;
		uid.mSecondLayerOffset	=Vector2.Zero;
		uid.mScale				=scale;
		uid.mTexture			=texName;
		uid.mTexture2			=texName2;
		uid.mVerts				=new GumpVert[6];

		ID3D11Texture2D	tex	=mSK.GetTexture2D(texName);

		RawRectF	rect	=new RawRectF(0f, 0f,
			tex.Description.Width, tex.Description.Height);

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


	void MakeQuad(GumpVert []tv, RawRectF rect)
	{
		tv[0].Position		=Vector2.UnitX * rect.Left + Vector2.UnitY * rect.Top;
		tv[0].TexCoord04	=Vector4.Zero;

		tv[1].Position		=Vector2.UnitX * rect.Right + Vector2.UnitY * rect.Top;
		tv[1].TexCoord04	=Vector4.UnitX + Vector4.UnitZ;

		tv[2].Position		=Vector2.UnitX * rect.Right + Vector2.UnitY * rect.Bottom;
		tv[2].TexCoord04	=Vector4.One;

		tv[3].Position		=Vector2.UnitX * rect.Left + Vector2.UnitY * rect.Top;
		tv[3].TexCoord04	=Vector4.Zero;

		tv[4].Position		=Vector2.UnitX * rect.Right + Vector2.UnitY * rect.Bottom;
		tv[4].TexCoord04	=Vector4.One;

		tv[5].Position		=Vector2.UnitX * rect.Left + Vector2.UnitY * rect.Bottom;
		tv[5].TexCoord04	=Vector4.UnitY + Vector4.UnitW;
	}


	public void Update(ID3D11DeviceContext dc)
	{
		if(mbDirty)
		{
			RebuildVB(dc);
			mbDirty	=false;
		}
	}


	void RebuildVB(ID3D11DeviceContext dc)
	{
		mNumVerts	=0;
		foreach(KeyValuePair<string, UIData> uid in mGumps)
		{
			uid.Value.mVerts.CopyTo(mGumpBuf, mNumVerts);
			
			mNumVerts	+=uid.Value.mVerts.Length;
		}

		MappedSubresource	msr	=dc.Map(mVB, MapMode.WriteDiscard);

		Span<GumpVert>	verts	=msr.AsSpan<GumpVert>(mNumVerts * 16);

		for(int i=0;i < mNumVerts;i++)
		{
			verts[i]	=mGumpBuf[i];
		}

		dc.Unmap(mVB);
	}


	public void Draw(Matrix4x4 view, Matrix4x4 proj)
	{
		//if this assert fires, make sure
		//all text modification stuff happens
		//before the call to update
		Debug.Assert(!mbDirty);

		if(mNumVerts <= 0)
		{
			return;
		}

		ID3D11DeviceContext	dc	=mSK.GetDC();

		dc.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

		dc.IASetVertexBuffer(0, mVB, 16);
		dc.IASetIndexBuffer(null, Format.Unknown, 0);

		ID3D11InputLayout	lay	=mSK.GetOrCreateLayout("KeyedGumpVS");

		dc.VSSetShader(mSK.GetVertexShader("KeyedGumpVS"));
		dc.PSSetShader(mSK.GetPixelShader("GumpPS"));
		dc.IASetInputLayout(lay);

		dc.OMSetBlendState(mSK.GetBlendState("AlphaBlending"));
		dc.OMSetDepthStencilState(mSK.GetDepthStencilState("DisableDepth"));		

		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.SetView(view, Vector3.Zero);
		cbk.SetProjection(proj);
		cbk.UpdateFrame(dc);

		int	offset	=0;
		foreach(KeyValuePair<string, UIData> str in mGumps)
		{
			int	len	=str.Value.mVerts.Length;

			dc.PSSetShaderResource(0, mSK.GetSRV(str.Value.mTexture));
			dc.PSSetShaderResource(1, mSK.GetSRV(str.Value.mTexture2));

			cbk.SetTextColor(str.Value.mColor);
			cbk.SetTextTransform(str.Value.mPosition, str.Value.mScale);
			cbk.SetSecondLayerOffset(str.Value.mSecondLayerOffset);

			cbk.UpdateTwoD(dc);

			dc.Draw(len, offset);

			offset	+=len;
		}
	}
}

