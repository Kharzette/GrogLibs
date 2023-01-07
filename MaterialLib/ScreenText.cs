using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;
using Vortice.Direct3D11;
using Vortice.DXGI;
using UtilityLib;


namespace MaterialLib;

internal struct TextVert
{
	internal Vector2	Position;
	internal Half2		TexCoord0;
}

internal class StringData
{
	internal Memory<TextVert>	mVerts;
	internal Vector4			mColor;
	internal Vector2			mPosition;
	internal Vector2			mScale;
}


public class ScreenText
{
	GraphicsDevice	mGD;
	StuffKeeper		mSK;
	CBKeeper		mCBK;
	Font			mFont;

	//d3d stuff
	ID3D11Buffer				mVB;
	ID3D11InputLayout			mLayout;
	ID3D11ShaderResourceView	mFontSRV;
	ID3D11VertexShader			mVS;
	ID3D11PixelShader			mPS;
	ID3D11DepthStencilState		mDSS;
	ID3D11BlendState			mBS;

	Memory<TextVert>	mTextBuf;

	bool	mbDirty;
	int		mMaxCharacters;
	int		mNumVerts;
	string	mFontTexName;

	Dictionary<string, StringData>	mStrings	=new Dictionary<string, StringData>();


	public unsafe ScreenText(GraphicsDevice gd, StuffKeeper sk,
		string	fontName,
		string	fontTexName,
		int		maxCharacters)
	{
		mGD				=gd;
		mSK				=sk;
		mCBK			=sk.GetCBKeeper();
		mMaxCharacters	=maxCharacters;
		mFontTexName	=fontTexName;
		mFontSRV		=sk.GetFontSRV(fontName);

		mFont	=sk.GetFont(fontName);

		mTextBuf	=new TextVert[mMaxCharacters * 6];

		BufferDescription	bDesc	=new BufferDescription(
			mMaxCharacters * 6 * 12, BindFlags.VertexBuffer,
			ResourceUsage.Dynamic, CpuAccessFlags.Write,
			ResourceOptionFlags.None, 0);

		mVB		=gd.GD.CreateBuffer<TextVert>(mTextBuf.Span, bDesc);

		InputElementDescription	[]ied	=new[]
		{
			new	InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
			new InputElementDescription("TEXCOORD", 0, Format.R16G16_Float, 8, 0)
		};

		mLayout	=sk.MakeLayout(gd.GD, "TextVS", ied);
		mVS		=sk.GetVertexShader("TextVS");
		mPS		=sk.GetPixelShader("TextPS");
		mDSS	=sk.GetDepthStencilState("DisableDepth");
		mBS		=sk.GetBlendState("AlphaBlending");
	}


	public void AddString(string text, string id,
		Vector4 color, Vector2 position, Vector2 scale)
	{
		StringData	sd	=new StringData();

		sd.mVerts	=new TextVert[text.Length * 6];

		CopyLetters(sd.mVerts.Span, text);

		sd.mColor		=color;
		sd.mPosition	=position;
		sd.mScale		=scale;

		mStrings.Add(id, sd);

		mbDirty	=true;
	}


	public void FreeAll()
	{
		mStrings.Clear();
		mVB.Dispose();

		mGD		=null;
	}


	//no linefeed/cr!
	public Vector2 MeasureString(string fontName, string toMeasure)
	{
		Vector2	ret	=Vector2.Zero;

		if(mFont == null)
		{
			return	ret;
		}

		int	maxHeight	=0;
		for(int i=0;i < toMeasure.Length;i++)
		{
			ret.X	+=mFont.GetCharacterWidth(toMeasure[i]);

			int	height	=mFont.GetCharacterHeight();
			if(height > maxHeight)
			{
				maxHeight	=height;
				ret.Y		=height;
			}
		}
		return	ret;
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


	public void ModifyStringText(string text, string id)
	{
		if(!mStrings.ContainsKey(id))
		{
			return;
		}

		StringData	sd	=mStrings[id];

		sd.mVerts	=new TextVert[text.Length * 6];

		CopyLetters(sd.mVerts.Span, text);

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


	void CopyLetters(Span<TextVert> tv, string text)
	{
		int	curWidth	=0;
		for(int i=0;i < text.Length;i++)
		{
			int	nextWidth	=curWidth + mFont.GetCharacterWidth(text[i]);

			Vector2	xCoord	=Vector2.UnitX * curWidth;
			Vector2	xCoord2	=Vector2.UnitX * nextWidth;
			Vector2	yCoord	=Vector2.Zero;
			Vector2	yCoord2	=Vector2.UnitY * mFont.GetCharacterHeight();

			tv[(i * 6)].Position	=xCoord;
			tv[(i * 6)].TexCoord0	=mFont.GetUV(text[i], 0);

			tv[(i * 6) + 1].Position	=xCoord2;
			tv[(i * 6) + 1].TexCoord0	=mFont.GetUV(text[i], 1);

			tv[(i * 6) + 2].Position	=xCoord2 + yCoord2;
			tv[(i * 6) + 2].TexCoord0	=mFont.GetUV(text[i], 2);

			tv[(i * 6) + 3].Position	=xCoord;
			tv[(i * 6) + 3].TexCoord0	=mFont.GetUV(text[i], 3);

			tv[(i * 6) + 4].Position	=yCoord2 + xCoord2;
			tv[(i * 6) + 4].TexCoord0	=mFont.GetUV(text[i], 4);

			tv[(i * 6) + 5].Position	=xCoord + yCoord2;
			tv[(i * 6) + 5].TexCoord0	=mFont.GetUV(text[i], 5);

			curWidth	=nextWidth;
		}
	}


	public void Update()
	{
		if(mbDirty)
		{
			RebuildVB();
			mbDirty	=false;
		}
	}


	void RebuildVB()
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

			Memory<TextVert>	stringChunk	=mTextBuf.Slice(mNumVerts);

			str.Value.mVerts.CopyTo(stringChunk);

			mNumVerts	+=str.Value.mVerts.Length;
		}

		MappedSubresource	msr	=mGD.DC.Map(mVB, MapMode.WriteDiscard);

		//AsSpan needs size in bytes, but that might change
		Span<TextVert>	verts	=msr.AsSpan<TextVert>(mNumVerts * 12);

		Memory<TextVert>	allStrings	=mTextBuf.Slice(0, mNumVerts);

		allStrings.Span.CopyTo(verts);

		mGD.DC.Unmap(mVB);
	}


	public void Draw()
	{
		//if this assert fires, make sure
		//all text modification stuff happens
		//before the call to update
		Debug.Assert(!mbDirty);

		if(mNumVerts <= 0)
		{
			return;
		}

		mGD.DC.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

		mGD.DC.IASetVertexBuffer(0, mVB, 12);
		mGD.DC.IASetIndexBuffer(null, Format.Unknown, 0);
		mGD.DC.IASetInputLayout(mLayout);

		mGD.DC.VSSetShader(mVS);
		mGD.DC.PSSetShader(mPS);
		mGD.DC.PSSetShaderResource(0, mFontSRV);

		mGD.DC.OMSetDepthStencilState(mDSS);
		mGD.DC.OMSetBlendState(mBS);

		mCBK.Set2DCBToShaders(mGD.DC);

		int	offset	=0;
		foreach(KeyValuePair<string, StringData> str in mStrings)
		{
			int	len	=str.Value.mVerts.Length;

			mCBK.SetTextTransform(str.Value.mPosition, str.Value.mScale);
			mCBK.SetTextColor(str.Value.mColor);

			mCBK.UpdateTwoD(mGD.DC);

			mGD.DC.Draw(len, offset);

			offset	+=len;
		}
	}
}