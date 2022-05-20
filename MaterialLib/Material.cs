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

		//defaults
		mDSS			="EnableDepth";
		mBlendState		="NoBlending";
		mSamplerState0	="PointClamp";
		mSamplerState1	="PointClamp";
	}


	public string Name
	{
		get { return mName; }
		set { mName = value; }
	}


	public string VSName
	{
		get { return mVSName; }
		set { mVSName = value; }
	}


	public string PSName
	{
		get { return mPSName; }
		set { mPSName = value; }
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


	//often in tools the material doesn't know what sort of
	//geometry it will be attached to (at construct time)
	internal void ChangeType(bool bBSP, bool bCharacter)
	{
		if(bBSP)
		{
			if(mBSPVars == null)
			{
				mBSPVars	=new BSPMat();
			}
		}
		else if(mBSPVars != null)
		{
			mBSPVars	=null;
		}

		if(bCharacter)
		{
			if(mCharVars == null)
			{
				mCharVars	=new CharacterMat();
			}
		}
		else if(mCharVars != null)
		{
			mCharVars	=null;
		}
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