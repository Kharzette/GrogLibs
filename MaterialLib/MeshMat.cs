using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;


namespace MaterialLib;

//Material stuff specific to static / character meshes
internal class MeshMat
{
	Vector4	mSolidColour, mSpecColor;
	Vector4	mLightColor0, mLightColor1, mLightColor2;
	Vector3	mLightDirection, mDanglyForce;
	float	mSpecPower;

	string	mTexture0, mTexture1;


	internal MeshMat()
	{
		mTexture0	=mTexture1	="";
	}

	internal Vector4	SolidColour
	{
		get	{	return	mSolidColour;	}
		set	{	mSolidColour	=value;	}
	}

	internal Vector4	SpecColor
	{
		get	{	return	mSpecColor;	}
		set	{	mSpecColor	=value;	}
	}

	internal Vector4	LightColor0
	{
		get	{	return	mLightColor0;	}
		set	{	mLightColor0	=value;	}
	}

	internal Vector4	LightColor1
	{
		get	{	return	mLightColor1;	}
		set	{	mLightColor1	=value;	}
	}

	internal Vector4	LightColor2
	{
		get	{	return	mLightColor2;	}
		set	{	mLightColor2	=value;	}
	}

	internal Vector3	LightDirection
	{
		get	{	return	mLightDirection;	}
		set	{	mLightDirection	=value;	}
	}

	internal Vector3	DanglyForce
	{
		get	{	return	mDanglyForce;	}
		set	{	mDanglyForce	=value;	}
	}

	internal float	SpecPower
	{
		get	{	return	mSpecPower;	}
		set	{	mSpecPower	=value;	}
	}

	internal string	Texture0
	{
		get	{	return	mTexture0;	}
		set	{	mTexture0	=value;	}
	}

	internal string	Texture1
	{
		get	{	return	mTexture1;	}
		set	{	mTexture1	=value;	}
	}


	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetTrilights(mLightColor0, mLightColor1, mLightColor2, mLightDirection);
		cbk.SetSpecular(mSpecColor, mSpecPower);
		cbk.SetSolidColour(mSolidColour);

		ID3D11ShaderResourceView	srv	=sk.GetSRV(mTexture0);
		if(srv != null)
		{
			dc.PSSetShaderResource(0, srv);
		}
		srv	=sk.GetSRV(mTexture1);
		if(srv != null)
		{
			dc.PSSetShaderResource(1, srv);
		}
	}
}