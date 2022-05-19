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
internal class BSPMat
{
	bool	mbTextureEnabled;
	Vector2	mTexSize;

	//intensity levels for the animated / switchable light styles
	Half	[]mAniIntensities;

	string	mTexture;


	internal BSPMat()
	{
		mbTextureEnabled	=false;
		mTexSize			=Vector2.One * 64f;
		mTexture			="";		
	}

	internal bool	TextureEnabled
	{
		get	{	return	mbTextureEnabled;	}
		set	{	mbTextureEnabled	=value;	}
	}

	internal Vector2	TextureSize
	{
		get	{	return	mTexSize;	}
		set	{	mTexSize	=value;	}
	}

	internal Half	[]AniIntensities
	{
		set	{	mAniIntensities	=value;	}
	}

	internal string	Texture
	{
		get	{	return	mTexture;	}
		set	{	mTexture	=value;	}
	}


	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetTextureEnabled(mbTextureEnabled);
		cbk.SetTexSize(mTexSize);
		cbk.SetAniIntensities(mAniIntensities);

		ID3D11ShaderResourceView	srv	=sk.GetSRV(mTexture);
		if(srv != null)
		{
			dc.PSSetShaderResource(0, srv);
		}

		//not really sure yet how I'll identify the correct lightmap
		//if multiple bsp maps are open
		ID3D11ShaderResourceView	lm	=sk.GetSRV("LightMapAtlas");
		if(lm != null)
		{
			dc.PSSetShaderResource(1, lm);
		}
	}
}