using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Direct3D11;
using UtilityLib;


namespace MaterialLib;

//The idea behind this class is to store stuff for a draw call.
//Such as shaders, shader variable values like colors and specularity,
//and textures and such
internal class Material
{
	string	mName;			//name of the material

	//stuffkeeper holds these
	string	mVSName;		//vertex shader
	string	mPSName;		//pixel shader
	string	mDSS;			//depth stencil state
	string	mBlendState;	//blend
	string	mSamplerState0;	//sampling
	string	mSamplerState1;	//sampling

	//variable stuffs
	internal Matrix4x4	mWorld;			//world matrix
	internal int		mMaterialID;	//ID for doing outlines and such


	//variables for specific meshtypes
	internal BSPMat			mBSPVars;		//bsp specific, might be null
	internal MeshMat		mMeshVars;		//mesh specific, should be non null
	internal CharacterMat	mCharVars;		//character specific, might be null


	internal Material(string name, bool bBSP, bool bCharacter)
	{
		mName	=name;

		mMeshVars	=new MeshMat();

		if(bBSP)
		{
			mBSPVars	=new BSPMat();
		}
		if(bCharacter)
		{
			mCharVars	=new CharacterMat();
		}
	}


	public string Name
	{
		get { return mName; }
		set { mName = value; }
	}


	internal void Apply(ID3D11DeviceContext dc, StuffKeeper sk)
	{
		CBKeeper	cbk	=sk.GetCBKeeper();
		
		cbk.SetCommonCBToShaders(dc);
		if(mBSPVars != null)
		{
			cbk.SetBSPToShaders(dc);
		}
		if(mCharVars != null)
		{
			cbk.SetCharacterToShaders(dc);
		}


		//shaders
		dc.VSSetShader(sk.GetVertexShader(mVSName));
		dc.PSSetShader(sk.GetPixelShader(mPSName));

		//renderstates
		dc.OMSetBlendState(sk.GetBlendState(mBlendState));
		dc.OMSetDepthStencilState(sk.GetDepthStencilState(mDSS));

		//sampling
		dc.PSSetSampler(0, sk.GetSamplerState(mSamplerState0));
		dc.PSSetSampler(1, sk.GetSamplerState(mSamplerState1));

		cbk.SetMaterialID(mMaterialID);

		mMeshVars.Apply(dc, cbk, sk);
		mBSPVars?.Apply(dc, cbk, sk);
		mCharVars?.Apply(dc, cbk, sk);
	}


	internal void SetTrilightValues(Vector4 col0, Vector4 col1,
									Vector4 col2, Vector3 lightDir)
	{
		mMeshVars.LightColor0		=col0;
		mMeshVars.LightColor1		=col1;
		mMeshVars.LightColor2		=col2;
		mMeshVars.LightDirection	=lightDir;
	}


	internal void SetMaterialID(int id)
	{
		mMaterialID	=id;
	}


	internal void SetWorld(Matrix4x4 mat)
	{
		mWorld	=mat;
	}
}