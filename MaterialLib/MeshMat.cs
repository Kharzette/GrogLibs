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
//This might get passed to GUI stuff so public
public class MeshMat
{
	Vector4	mSolidColour;
	Vector3	mSpecColor;
	Vector3	mLightColor0, mLightColor1, mLightColor2;
	Vector3	mLightDirection, mDanglyForce;
	int		mMaterialID;
	float	mSpecPower;

	string	mTexture0, mTexture1;


	internal MeshMat()
	{
		mTexture0	=mTexture1	="";
		mSpecPower	=3f;
	}


	internal void Load(BinaryReader br)
	{
		mSolidColour	=FileUtil.ReadVector4(br);

		Vector4	tmp	=FileUtil.ReadVector4(br);
		mSpecColor	=tmp.XYZ();

		tmp	=FileUtil.ReadVector4(br);
		mLightColor0	=tmp.XYZ();

		tmp	=FileUtil.ReadVector4(br);
		mLightColor1	=tmp.XYZ();

		tmp	=FileUtil.ReadVector4(br);
		mLightColor2	=tmp.XYZ();

		mLightDirection	=FileUtil.ReadVector3(br);
		mDanglyForce	=FileUtil.ReadVector3(br);
		mSpecPower		=br.ReadSingle();
		mTexture0		=br.ReadString();
		mTexture1		=br.ReadString();
	}


	internal void Save(BinaryWriter bw)
	{
		FileUtil.WriteVector4(bw, SolidColour);
		FileUtil.WriteVector4(bw, SpecColor);
		FileUtil.WriteVector4(bw, LightColor0);
		FileUtil.WriteVector4(bw, LightColor1);
		FileUtil.WriteVector4(bw, LightColor2);
		FileUtil.WriteVector3(bw, LightDirection);
		FileUtil.WriteVector3(bw, DanglyForce);
		bw.Write(mSpecPower);
		bw.Write(mTexture0);
		bw.Write(mTexture1);
	}


	internal MeshMat Clone()
	{
		return	(MeshMat)MemberwiseClone();
	}

	public Vector4	SolidColour
	{
		get	{	return	mSolidColour;	}
		set	{	mSolidColour	=value;	}
	}

	public Vector3	SpecColor
	{
		get	{	return	mSpecColor;	}
		set	{	mSpecColor	=value;	}
	}

	public Vector3	LightColor0
	{
		get	{	return	mLightColor0;	}
		set	{	mLightColor0	=value;	}
	}

	public Vector3	LightColor1
	{
		get	{	return	mLightColor1;	}
		set	{	mLightColor1	=value;	}
	}

	public Vector3	LightColor2
	{
		get	{	return	mLightColor2;	}
		set	{	mLightColor2	=value;	}
	}

	public Vector3	LightDirection
	{
		get	{	return	mLightDirection;	}
		set	{	mLightDirection	=value;	}
	}

	public Vector3	DanglyForce
	{
		get	{	return	mDanglyForce;	}
		set	{	mDanglyForce	=value;	}
	}

	public float	SpecPower
	{
		get	{	return	mSpecPower;	}
		set	{	mSpecPower	=value;	}
	}

	public string	Texture0
	{
		get	{	return	mTexture0;	}
		set	{	mTexture0	=value;	}
	}

	public string	Texture1
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