using System.IO;
using System.Numerics;


namespace MaterialLib;

//The idea behind this class is to store stuff for a draw call.
//Such as shaders, shader variable values like colors and specularity,
//and textures and such
internal partial class Material
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
	internal int	mMaterialID;	//ID for doing outlines and such


	//variables for specific meshtypes
	internal BSPMat			mBSPVars;		//bsp specific, might be null
	internal MeshMat		mMeshVars;		//mesh specific, should be non null
	internal CharacterMat	mCharVars;		//character specific, might be null


	internal void Save(BinaryWriter bw)
	{
		bw.Write(mName);
		bw.Write(mVSName);
		bw.Write(mPSName);
		bw.Write(mDSS);
		bw.Write(mBlendState);
		bw.Write(mSamplerState0);
		bw.Write(mSamplerState1);
		
		bw.Write(mMaterialID);

		bw.Write(mBSPVars != null);
		mBSPVars?.Save(bw);

		bw.Write(mMeshVars != null);
		mMeshVars?.Save(bw);

		//nothing in character stuff yet
	}


	internal void Load(BinaryReader br)
	{
		mName			=br.ReadString();
		mVSName			=br.ReadString();
		mPSName			=br.ReadString();
		mDSS			=br.ReadString();
		mBlendState		=br.ReadString();
		mSamplerState0	=br.ReadString();
		mSamplerState1	=br.ReadString();

		mMaterialID	=br.ReadInt32();

		bool	bBSP	=br.ReadBoolean();
		if(bBSP)
		{
			mBSPVars	=new BSPMat();
			mBSPVars.Load(br);
		}

		bool	bMesh	=br.ReadBoolean();
		if(bMesh)
		{
			mMeshVars	=new MeshMat();
			mMeshVars.Load(br);
		}
	}


	internal Material Clone(string newName)
	{
		Material	ret	=new Material(newName, mBSPVars != null, mCharVars != null);

		ret.mVSName			=mVSName;
		ret.mPSName			=mPSName;
		ret.mDSS			=mDSS;
		ret.mBlendState		=mBlendState;
		ret.mSamplerState0	=mSamplerState0;
		ret.mSamplerState1	=mSamplerState1;
		ret.mMaterialID		=mMaterialID;

		if(mBSPVars != null)
		{
			ret.mBSPVars =mBSPVars.Clone();
		}

		if(mMeshVars != null)
		{
			ret.mMeshVars	=mMeshVars.Clone();
		}

		if(mCharVars != null)
		{
			ret.mCharVars	=mCharVars.Clone();
		}

		return	ret;
	}


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


	internal void SetStates(string blendState, string depthState)
	{
		mBlendState	=blendState;
		mDSS		=depthState;
	}


	internal void SetTrilightValues(Vector4 col0, Vector4 col1,
									Vector4 col2, Vector3 lightDir)
	{
		mMeshVars.LightColor0		=col0;
		mMeshVars.LightColor1		=col1;
		mMeshVars.LightColor2		=col2;
		mMeshVars.LightDirection	=lightDir;
	}


	internal void SetlightDirection(Vector3 lightDir)
	{
		mMeshVars.LightDirection	=lightDir;
	}


	internal void SetMaterialID(int id)
	{
		mMaterialID	=id;
	}


	internal bool HasTexture()
	{
		if(mPSName == null || mPSName == "")
		{
			return	false;
		}

		//a bit of an assumption here
		return	mPSName.Contains("Tex");
	}
}