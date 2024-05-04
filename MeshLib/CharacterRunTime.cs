using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib;

//an instance of a character
public partial class Character
{
	public void BuildDebugBoundDrawData(CommonPrims cprims)
	{
		mSkin?.BuildDebugBoundDrawData(cprims);
	}

	public void BuildDebugBoundDrawData(int index, CommonPrims cprims)
	{
		mSkin?.BuildDebugBoundDrawData(index, cprims);
	}

	//copies bones into the shader
	//materials should be set up to ignore
	//the mBones parameter
	void UpdateShaderBones(ID3D11DeviceContext dc, CBKeeper cbk)
	{
		if(mBones != null)
		{
			if(mBones.Length <= MAX_BONES)
			{
				cbk.SetBonesWithTranspose(mBones);
				cbk.UpdateCharacter(dc);
				cbk.SetCharacterToShaders(dc);
			}
			else
			{
				//Too many bones will stomp gpu memory!
				Debug.Assert(false);
			}
		}
	}

	
	public void Draw(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm);
		}
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm, altMaterial);
		}
	}


	public void DrawDMN(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.DrawDMN(mlib, mTransform, mm);
		}
	}


	//this if for the DMN renderererererer
	public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		foreach(MeshMaterial mm in mPartMats)
		{
			mm.mMaterialID	=idk.GetID(mm.mMaterialName);
		}
	}
}