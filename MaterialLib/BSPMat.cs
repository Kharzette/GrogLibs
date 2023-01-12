using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;


namespace MaterialLib;

//Material stuff specific to BSP meshes
public class BSPMat
{
	bool	mbTextureEnabled;
	Vector2	mTexSize;

	string	mTexture;


	internal void Load(BinaryReader br)
	{
		mbTextureEnabled	=br.ReadBoolean();
		mTexSize			=FileUtil.ReadVector2(br);
		mTexture			=br.ReadString();
	}


	internal void Save(BinaryWriter bw)
	{
		bw.Write(mbTextureEnabled);
		FileUtil.WriteVector2(bw, mTexSize);

		//don't bother with the animated light array

		bw.Write(mTexture);
	}


	internal BSPMat()
	{
		mbTextureEnabled	=false;
		mTexSize			=Vector2.One * 64f;
		mTexture			="";		
	}

	internal BSPMat Clone()
	{
		BSPMat	ret	=new BSPMat();

		ret.mbTextureEnabled	=mbTextureEnabled;
		ret.mTexSize			=mTexSize;

		return	ret;
	}

	public bool	TextureEnabled
	{
		get	{	return	mbTextureEnabled;	}
		set	{	mbTextureEnabled	=value;	}
	}

	public Vector2	TextureSize
	{
		get	{	return	mTexSize;	}
		set	{	mTexSize	=value;	}
	}

	public string	Texture
	{
		get	{	return	mTexture;	}
		set	{	mTexture	=value;	}
	}


	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetTextureEnabled(mbTextureEnabled);
		cbk.SetTexSize(mTexSize);

		ID3D11Texture2D	tex	=sk.GetTexture2D(mTexture);

		ID3D11ShaderResourceView	srv	=sk.GetSRV(mTexture);
		if(srv != null)
		{
			if(tex.Description.ArraySize > 1)
			{
				dc.PSSetShaderResource(3, srv);
			}
			else
			{
				dc.PSSetShaderResource(0, srv);
			}
		}

		//not really sure yet how I'll identify the correct lightmap
		//if multiple bsp maps are open
		ID3D11ShaderResourceView	lm	=sk.GetSRV("LightMapAtlas");
		if(lm != null)
		{
			dc.PSSetShaderResource(1, lm);
		}
		cbk.UpdateBSP(dc);
	}
}