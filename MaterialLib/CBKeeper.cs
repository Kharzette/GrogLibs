using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;


namespace MaterialLib;

//hold / handle constant buffer related stuffs
public unsafe class CBKeeper
{
	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct PerFrame
	{
		internal Matrix4x4	mView;
		internal Matrix4x4	mLightViewProj;	//for shadows
		internal Vector3	mEyePos;
		internal UInt32		mPadding;		//pad to 16 boundary
	}

	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct PerObject
	{
		internal Matrix4x4	mWorld;
		internal Vector4	mSolidColour;
		internal Vector4	mSpecColor;

		//These are considered directional (no falloff)
		internal Vector4	mLightColor0;		//trilights need 3 colors
		internal Vector4	mLightColor1;		//trilights need 3 colors
		internal Vector4	mLightColor2;		//trilights need 3 colors

		internal Vector3	mLightDirection;
		internal float		mSpecPower;

		//material id for borders etc
		internal int		mMaterialID;
		internal Vector3	mDanglyForce;
	}

	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct ChangeLess
	{
		internal Matrix4x4	mProjection;
	}

	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct PerShadow
	{
		internal Vector3	mShadowLightPos;
		internal bool		mbDirectional;
		internal float		mShadowAtten;
		internal Vector3	mShadPadding;
	}

	//2D.hlsl
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct TwoD
	{
		internal Vector2	mTextPosition, mSecondLayerOffset;
		internal Vector2	mTextScale;
		internal Vector2	mPadding;
		internal Vector4	mTextColor;
	}

	//BSP.hlsl
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct BSP
	{
		internal bool		mbTextureEnabled;
		internal Vector2	mTexSize;
		internal uint		mPadding;
	}

	//post.hlsl
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct Post
	{
		internal Vector2	mInvViewPort;

		//bloom stuff
		internal float		mBloomThreshold;
		internal float		mBloomIntensity;
		internal float		mBaseIntensity;
		internal float		mBloomSaturation;
		internal float		mBaseSaturation;

		//outliner stuff
		internal float		mTexelSteps;
		internal float		mThreshold;
		internal Vector2	mScreenSize;

		//bilateral blur stuff
		internal float		mBlurFallOff;
		internal float		mSharpNess;
		internal float		mOpacity;

		//padding
		internal uint	mPad0, mPad1;
	}

	//Character.hlsl bone array
	Matrix4x4	[]mBones;

	//BSP.hlsl light style array
	Half	[]mAniIntensities;

	//post.hlsl weights & offsets array
	float	[]mWeightsOffsetsXY;

	//gpu side
	ID3D11Buffer	mPerObjectBuf;
	ID3D11Buffer	mPerFrameBuf;
	ID3D11Buffer	mChangeLessBuf;
	ID3D11Buffer	mTwoDBuf;
	ID3D11Buffer	mCharacterBuf;
	ID3D11Buffer	mBSPBuf, mBSPStylesBuf;
	ID3D11Buffer	mPostBuf, mPostWOXYBuf;
	ID3D11Buffer	mPerShadowBuf;

	//cpu side
	PerObject	mPerObject;
	PerFrame	mPerFrame;
	ChangeLess	mChangeLess;
	TwoD		mTwoD;
	BSP			mBSP;
	Post		mPost;
	PerShadow	mPerShadow;


	//ensure matches Character.hlsl
	const int	MaxBones	=55;
	const int	NumStyles	=44;	//match bsp.hlsl


	//stuffkeeper constructs this
	internal CBKeeper(ID3D11Device dev)
	{
		AllocCBuffers(dev);
	}


	internal void FreeAll()
	{
		mPerObjectBuf.Dispose();
		mPerFrameBuf.Dispose();
		mChangeLessBuf.Dispose();
		mTwoDBuf.Dispose();
		mCharacterBuf.Dispose();
		mBSPBuf.Dispose();
		mBSPStylesBuf.Dispose();
		mPerShadowBuf.Dispose();
	}


	void AllocCBuffers(ID3D11Device dev)
	{
		//create constant buffers
		mPerObjectBuf	=MakeConstantBuffer(dev, sizeof(PerObject));
		mPerFrameBuf	=MakeConstantBuffer(dev, sizeof(PerFrame));
		mChangeLessBuf	=MakeConstantBuffer(dev, sizeof(ChangeLess));
		mTwoDBuf		=MakeConstantBuffer(dev, sizeof(TwoD));
		mBSPBuf			=MakeConstantBuffer(dev, sizeof(BSP));
		mCharacterBuf	=MakeConstantBuffer(dev, sizeof(Matrix4x4) * MaxBones);
		mBSPStylesBuf	=MakeConstantBuffer(dev, sizeof(Half) * NumStyles + 8);
		mPostBuf		=MakeConstantBuffer(dev, sizeof(Post));
		mPerShadowBuf	=MakeConstantBuffer(dev, sizeof(PerShadow));

		//alloc C# side constant buffer data
		mPerObject		=new PerObject();
		mPerFrame		=new PerFrame();
		mChangeLess		=new ChangeLess();
		mTwoD			=new TwoD();
		mBones			=new Matrix4x4[MaxBones];
		mAniIntensities	=new Half[NumStyles];
		mBSP			=new BSP();
		mPost			=new Post();
		mPerShadow		=new PerShadow();

		int	woxySize	=61 * 4;

		if(dev.FeatureLevel == FeatureLevel.Level_9_3)
		{
			woxySize	=15 * 4;
		}
		mWeightsOffsetsXY	=new float[woxySize];
		mPostWOXYBuf		=MakeConstantBuffer(dev, sizeof(float) * woxySize);
	}


	static ID3D11Buffer	MakeConstantBuffer(ID3D11Device dev, int size)
	{
		BufferDescription	cbDesc	=new BufferDescription();

		//these are kind of odd, but change any one and it breaks		
		cbDesc.BindFlags			=BindFlags.ConstantBuffer;
		cbDesc.ByteWidth			=size;
		cbDesc.CPUAccessFlags		=CpuAccessFlags.None;	//you'd think write, but nope
		cbDesc.MiscFlags			=ResourceOptionFlags.None;
		cbDesc.Usage				=ResourceUsage.Default;	//you'd think dynamic here but nope
		cbDesc.StructureByteStride	=0;

		//alloc
		return	dev.CreateBuffer(cbDesc);
	}


	public void SetCommonCBToShaders(ID3D11DeviceContext dc)
	{
		//commonfunctions
		dc.VSSetConstantBuffer(0, mPerObjectBuf);
		dc.PSSetConstantBuffer(0, mPerObjectBuf);
		dc.VSSetConstantBuffer(1, mPerFrameBuf);
		dc.PSSetConstantBuffer(1, mPerFrameBuf);
		dc.VSSetConstantBuffer(2, mChangeLessBuf);
		dc.PSSetConstantBuffer(2, mChangeLessBuf);
	}


	public void Set2DCBToShaders(ID3D11DeviceContext dc)
	{
		//2d
		dc.VSSetConstantBuffer(4, mTwoDBuf);
		dc.PSSetConstantBuffer(4, mTwoDBuf);
	}


	public void SetCharacterToShaders(ID3D11DeviceContext dc)
	{
		//character
		dc.VSSetConstantBuffer(4, mCharacterBuf);
		dc.PSSetConstantBuffer(4, mCharacterBuf);
	}


	public void SetBSPToShaders(ID3D11DeviceContext dc)
	{
		//bsp
		dc.VSSetConstantBuffer(4, mBSPBuf);
		dc.PSSetConstantBuffer(4, mBSPBuf);
		dc.VSSetConstantBuffer(5, mBSPStylesBuf);
		dc.PSSetConstantBuffer(5, mBSPStylesBuf);
	}


	public void SetPostToShaders(ID3D11DeviceContext dc)
	{
		dc.VSSetConstantBuffer(0, mPostBuf);
		dc.PSSetConstantBuffer(0, mPostBuf);
		dc.VSSetConstantBuffer(1, mPostWOXYBuf);
		dc.PSSetConstantBuffer(1, mPostWOXYBuf);
	}


	public void SetPerShadowToShaders(ID3D11DeviceContext dc)
	{
		dc.VSSetConstantBuffer(3, mPerShadowBuf);
		dc.PSSetConstantBuffer(3, mPerShadowBuf);
	}


	public void UpdateFrame(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerFrame>(mPerFrame, mPerFrameBuf);
	}


	public void UpdateObject(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerObject>(mPerObject, mPerObjectBuf);
	}


	public void UpdateChangeLess(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<ChangeLess>(mChangeLess, mChangeLessBuf);
	}


	public void UpdateTwoD(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<TwoD>(mTwoD, mTwoDBuf);
	}


	public void UpdateCharacter(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource(mBones, mCharacterBuf);
	}


	public void UpdateBSP(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<BSP>(mBSP, mBSPBuf);
		dc.UpdateSubresource(mAniIntensities, mBSPStylesBuf);
	}


	public void UpdatePost(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<Post>(mPost, mPostBuf);
		dc.UpdateSubresource(mWeightsOffsetsXY, mPostWOXYBuf);
	}


	public void	UpdatePerShadow(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerShadow>(mPerShadow, mPerShadowBuf);
	}


#region PerFrame
	public void SetView(Matrix4x4 view, Vector3 eyePos)
	{
		mPerFrame.mView		=view;
		mPerFrame.mEyePos	=eyePos;
	}


	public void SetLightViewProj(Matrix4x4 lvp)
	{
		mPerFrame.mLightViewProj	=lvp;
	}
#endregion


#region	PerObject
	public void SetTrilights(Vector4 L0, Vector4 L1, Vector4 L2, Vector3 lightDir)
	{
		mPerObject.mLightColor0		=L0;
		mPerObject.mLightColor1		=L1;
		mPerObject.mLightColor2		=L2;
		mPerObject.mLightDirection	=lightDir;
	}


	public void SetTrilights(Vector3 L0, Vector3 L1, Vector3 L2, Vector3 lightDir)
	{
		mPerObject.mLightColor0.X		=L0.X;
		mPerObject.mLightColor0.Y		=L0.Y;
		mPerObject.mLightColor0.Z		=L0.Z;

		mPerObject.mLightColor1.X		=L1.X;
		mPerObject.mLightColor1.Y		=L1.Y;
		mPerObject.mLightColor1.Z		=L1.Z;

		mPerObject.mLightColor2.X		=L2.X;
		mPerObject.mLightColor2.Y		=L2.Y;
		mPerObject.mLightColor2.Z		=L2.Z;

		mPerObject.mLightDirection	=lightDir;

		mPerObject.mLightColor0.W	=mPerObject.mLightColor1.W
			=mPerObject.mLightColor2.W	=1f;
	}


	public void SetSolidColour(Vector4 sc)
	{
		mPerObject.mSolidColour	=sc;
	}


	public void SetSpecular(Vector4 specColour, float specPow)
	{
		mPerObject.mSpecColor	=specColour;
		mPerObject.mSpecPower	=specPow;
	}


	internal void SetSpecularPower(float specPow)
	{
		mPerObject.mSpecPower	=specPow;
	}


	public void SetWorldMat(Matrix4x4 world)
	{
		mPerObject.mWorld	=world;
	}


	public void SetMaterialID(int matID)
	{
		mPerObject.mMaterialID	=matID;
	}


	public void SetDanglyForce(Vector3 force)
	{
		mPerObject.mDanglyForce	=force;
	}
#endregion


#region	TwoD
	public void SetTextTransform(Vector2 textPos, Vector2 textScale)
	{
		mTwoD.mTextPosition	=textPos;
		mTwoD.mTextScale	=textScale;
	}


	public void SetSecondLayerOffset(Vector2 ofs)
	{
		mTwoD.mSecondLayerOffset	=ofs;
	}


	public void SetTextColor(Vector4 col)
	{
		mTwoD.mTextColor	=col;
	}
#endregion


	public void SetTextureEnabled(bool bOn)
	{
		mBSP.mbTextureEnabled	=bOn;
	}


	public void SetTexSize(Vector2 size)
	{
		mBSP.mTexSize	=size;
	}


	public void SetAniIntensities(Half	[]ani)
	{
		Array.Copy(ani, mAniIntensities, NumStyles);
	}


	public void SetBones(Matrix4x4 []bones)
	{
		bones.CopyTo(mBones, 0);
	}


	public void SetProjection(Matrix4x4 proj)
	{
		mChangeLess.mProjection	=proj;
	}


#region	PostProcess
	public void SetInvViewPort(Vector2 port)
	{
		mPost.mInvViewPort	=port;
	}


	public void SetOutlinerVars(Vector2 size, float texelSteps, float threshold)
	{
		mPost.mScreenSize	=size;
		mPost.mTexelSteps	=texelSteps;
		mPost.mThreshold	=threshold;
	}


	public void SetBilateralBlurVars(float fallOff, float sharpness, float opacity)
	{
		mPost.mBlurFallOff	=fallOff;
		mPost.mSharpNess	=sharpness;
		mPost.mOpacity		=opacity;
	}


	public void SetBloomVars(float thresh, float intensity,
		float sat, float baseIntensity, float baseSat)
	{
		mPost.mBloomThreshold	=thresh;
		mPost.mBloomIntensity	=intensity;
		mPost.mBloomSaturation	=sat;
		mPost.mBaseIntensity	=baseIntensity;
		mPost.mBaseSaturation	=baseSat;
	}


	public void SetWeightsOffsets(float []wx, float []wy, float []offx, float []offy)
	{
		wx.CopyTo(mWeightsOffsetsXY, 0);
		wy.CopyTo(mWeightsOffsetsXY, wx.Length);
		offx.CopyTo(mWeightsOffsetsXY, wx.Length * 2);
		offy.CopyTo(mWeightsOffsetsXY, wx.Length * 3);
	}
#endregion


	public void SetPerShadow(Vector3 shadowLightPos, bool bDirectional, float shadowAtten)
	{
		mPerShadow.mShadowLightPos	=shadowLightPos;
		mPerShadow.mbDirectional	=bDirectional;
		mPerShadow.mShadowAtten		=shadowAtten;
	}


	public void SetPerShadowDirectional(bool bDirectional)
	{
		mPerShadow.mbDirectional	=bDirectional;
	}


	public void SetPerShadowLightPos(Vector3 pos)
	{
		mPerShadow.mShadowLightPos	=pos;
	}
}