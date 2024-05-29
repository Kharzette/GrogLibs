using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics.PackedVector;
using UtilityLib;
using System.Runtime.CompilerServices;


namespace MaterialLib;

//hold / handle constant buffer related stuffs
public unsafe class CBKeeper
{
	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct PerFrame
	{
		internal Matrix4x4	mView;
		internal Matrix4x4	mProjection;
		internal Matrix4x4	mLightViewProj;	//for shadows
		internal Vector4	mEyePos;		//w unused
		internal Vector4	mFog;			//start, end, enabled, unused
		internal Vector4	mSkyGradient0;
		internal Vector4	mSkyGradient1;
	}

	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct PerObject
	{
		internal Matrix4x4	mWorld;
		internal Vector4	mSolidColour;
		internal Vector4	mSpecColorPow;	//power in w

		//These are considered directional (no falloff)
		//light direction in w component
		internal Vector4	mLightColor0;		//trilights need 3 colors
		internal Vector4	mLightColor1;		//trilights need 3 colors
		internal Vector4	mLightColor2;		//trilights need 3 colors

		//a force vector for doing physicsy stuff
		//integer material id in w
		internal Vector4	mDanglyForce;
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
		//light style array, make sure size matches HLSL
		//Should be float16 TODO
		internal fixed float	mAniIntensities[NumAniIntensities];
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
		//gaussianblur stuff, make sure size matches Post.hlsl
		internal fixed float	mWeightsX[KernelSize], mWeightsY[KernelSize];
		internal fixed float	mOffsetsX[KernelSize], mOffsetsY[KernelSize];
}

	//TextMode.hlsl
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct	TextMode
	{
		internal uint	mWidth, mHeight;	//dimensions of screen in pixels
		internal uint	mCWidth, mCHeight;	//dimensions of screen in character blocks

		//font texture info
		internal uint	mStartChar;		//first letter of the font bitmap
		internal uint	mNumColumns;	//number of font columns in the font texture
		internal uint	mCharWidth;		//width of characters in texels in the font texture (fixed)
		internal uint	mCharHeight;	//height of characters in texels in the font texture (fixed)
	}

	//Cel.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct	CelStuff
	{
		internal Vector4	mValMin;
		internal Vector4	mValMax;
		internal Vector4	mSnapTo;

		internal int		mNumSteps;
		internal Vector3	mPad;
	}

	//Character.hlsl bone array
	Matrix4x4	[]mBones;

	//BSP.hlsl dynamic lights
	Vector4	[]mDynPos;
	Vector4	[]mDynColor;

	//gpu side
	ID3D11Buffer	mPerObjectBuf;
	ID3D11Buffer	mPerFrameBuf;
	ID3D11Buffer	mTwoDBuf;
	ID3D11Buffer	mCharacterBuf;
	ID3D11Buffer	mBSPBuf;
	ID3D11Buffer	mDynPosBuf, mDynColBuf;
	ID3D11Buffer	mPostBuf;
	ID3D11Buffer	mPerShadowBuf;
	ID3D11Buffer	mTextModeBuf;
	ID3D11Buffer	mCelBuf;

	//cpu side
	PerObject	mPerObject;
	PerFrame	mPerFrame;
	TwoD		mTwoD;
	BSP			mBSP;
	Post		mPost;
	PerShadow	mPerShadow;
	TextMode	mTextMode;
	CelStuff	mCelStuff;


	//ensure matches Character.hlsl
	const int	MaxBones			=55;	//match CommonFunctions.hlsli
	const int	NumStyles			=44;	//match bsp.hlsl
	const int	MaxDynLights		=16;	//match bsp.hlsl
	const int	NumAniIntensities	=44;
	const int	Radius				=30;
	const int	KernelSize			=(Radius * 2 + 1);


	//stuffkeeper constructs this
	internal CBKeeper(ID3D11Device dev)
	{
		AllocCBuffers(dev);
	}


	internal void FreeAll()
	{
		mPerObjectBuf.Dispose();
		mPerFrameBuf.Dispose();
		mTwoDBuf.Dispose();
		mCharacterBuf.Dispose();
		mBSPBuf.Dispose();
		mPerShadowBuf.Dispose();
		mTextModeBuf.Dispose();
		mCelBuf.Dispose();
		mDynColBuf.Dispose();
		mDynPosBuf.Dispose();
	}


	void AllocCBuffers(ID3D11Device dev)
	{
		//create constant buffers
		mPerObjectBuf	=MakeConstantBuffer(dev, sizeof(PerObject));
		mPerFrameBuf	=MakeConstantBuffer(dev, sizeof(PerFrame));
		mTwoDBuf		=MakeConstantBuffer(dev, sizeof(TwoD));
		mBSPBuf			=MakeConstantBuffer(dev, sizeof(BSP));
		mPostBuf		=MakeConstantBuffer(dev, sizeof(Post));
		mPerShadowBuf	=MakeConstantBuffer(dev, sizeof(PerShadow));
		mTextModeBuf	=MakeConstantBuffer(dev, sizeof(TextMode));
		mCelBuf			=MakeConstantBuffer(dev, sizeof(CelStuff));

		//array buffers, C# is a pain sometimes
		mDynColBuf		=MakeConstantBuffer(dev, MaxDynLights * sizeof(Vector4));
		mDynPosBuf		=MakeConstantBuffer(dev, MaxDynLights * sizeof(Vector4));
		mCharacterBuf	=MakeConstantBuffer(dev, sizeof(Matrix4x4) * MaxBones);

		//alloc C# side constant buffer data
		mPerObject		=new PerObject();
		mPerFrame		=new PerFrame();
		mTwoD			=new TwoD();
		mBones			=new Matrix4x4[MaxBones];
		mBSP			=new BSP();
		mPost			=new Post();
		mPerShadow		=new PerShadow();
		mTextMode		=new TextMode();
		mCelStuff		=new CelStuff();
		mDynColor		=new Vector4[MaxDynLights];
		mDynPos			=new Vector4[MaxDynLights];
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
		dc.VSSetConstantBuffer(10, mCelBuf);
		dc.PSSetConstantBuffer(10, mCelBuf);
	}

	public void Set2DCBToShaders(ID3D11DeviceContext dc)
	{
		//2d
		dc.VSSetConstantBuffer(7, mTwoDBuf);
		dc.PSSetConstantBuffer(7, mTwoDBuf);
	}

	public void SetCharacterToShaders(ID3D11DeviceContext dc)
	{
		//character
		dc.VSSetConstantBuffer(3, mCharacterBuf);
		dc.PSSetConstantBuffer(3, mCharacterBuf);
	}

	public void SetBSPToShaders(ID3D11DeviceContext dc)
	{
		//bsp
		dc.VSSetConstantBuffer(4, mBSPBuf);
		dc.PSSetConstantBuffer(4, mBSPBuf);
		dc.VSSetConstantBuffer(8, mDynPosBuf);
		dc.PSSetConstantBuffer(8, mDynPosBuf);
		dc.VSSetConstantBuffer(9, mDynColBuf);
		dc.PSSetConstantBuffer(9, mDynColBuf);
	}

	public void SetPostToShaders(ID3D11DeviceContext dc)
	{
		dc.VSSetConstantBuffer(5, mPostBuf);
		dc.PSSetConstantBuffer(5, mPostBuf);
	}

	public void SetPerShadowToShaders(ID3D11DeviceContext dc)
	{
		dc.VSSetConstantBuffer(2, mPerShadowBuf);
		dc.PSSetConstantBuffer(2, mPerShadowBuf);
	}

	public void SetTextModeToShaders(ID3D11DeviceContext dc)
	{
		dc.PSSetConstantBuffer(6, mTextModeBuf);
	}


	public void UpdateFrame(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerFrame>(mPerFrame, mPerFrameBuf);
	}

	public void UpdateObject(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerObject>(mPerObject, mPerObjectBuf);
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
	}

	public void UpdateBSPArrays(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource(mDynColor, mDynColBuf);
		dc.UpdateSubresource(mDynPos, mDynPosBuf);
	}

	public void UpdatePost(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<Post>(mPost, mPostBuf);
	}

	public void	UpdatePerShadow(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<PerShadow>(mPerShadow, mPerShadowBuf);
	}

	public void UpdateTextMode(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<TextMode>(mTextMode, mTextModeBuf);
	}

	public void UpdateCelStuff(ID3D11DeviceContext dc)
	{
		dc.UpdateSubresource<CelStuff>(mCelStuff, mCelBuf);
	}


#region PerFrame
	public void SetView(Matrix4x4 view, Vector3 eyePos)
	{
		mPerFrame.mView		=Matrix4x4.Transpose(view);
		mPerFrame.mEyePos	=eyePos.ToV4(1.0f);
	}


	public void SetTransposedView(Matrix4x4 view, Vector3 eyePos)
	{
		mPerFrame.mView		=view;
		mPerFrame.mEyePos	=eyePos.ToV4(1.0f);
	}


	public void SetTransposedLightViewProj(Matrix4x4 lvp)
	{
		mPerFrame.mLightViewProj	=lvp;
	}


	public void SetLightViewProj(Matrix4x4 lvp)
	{
		mPerFrame.mLightViewProj	=Matrix4x4.Transpose(lvp);
	}


	public void SetTransposedProjection(Matrix4x4 proj)
	{
		mPerFrame.mProjection	=proj;
	}

	
	public void SetProjection(Matrix4x4 proj)
	{
		mPerFrame.mProjection	=Matrix4x4.Transpose(proj);
	}
#endregion


#region	PerObject
	public void SetTrilights(Vector3 L0, Vector3 L1, Vector3 L2, Vector3 lightDir)
	{		
		mPerObject.mLightColor0		=L0.ToV4(lightDir.X);
		mPerObject.mLightColor1		=L1.ToV4(lightDir.Y);
		mPerObject.mLightColor2		=L2.ToV4(lightDir.Z);
	}


	public void SetSolidColour(Vector4 sc)
	{
		mPerObject.mSolidColour	=sc;
	}


	public void SetSpecular(Vector3 specColour, float specPow)
	{
		mPerObject.mSpecColorPow	=specColour.ToV4(specPow);
	}


	internal void SetSpecularPower(float specPow)
	{
		mPerObject.mSpecColorPow.W	=specPow;
	}


	public void SetWorldMat(Matrix4x4 world)
	{
		mPerObject.mWorld	=Matrix4x4.Transpose(world);
	}


	public void SetTransposedWorldMat(Matrix4x4 world)
	{
		mPerObject.mWorld	=world;
	}


	public void SetMaterialID(int matID)
	{
		//TODO: this might differ from C a bit
		mPerObject.mDanglyForce.W	=matID;
	}


	public void SetDanglyForce(Vector3 force)
	{
		mPerObject.mDanglyForce	=force.ToV4(mPerObject.mDanglyForce.W);
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


	public void SetAniIntensities(float []ani)
	{
		if(ani == null)
		{
			return;
		}

		fixed(float	*pAni = ani)
		{
			fixed(float *pBspAnis = mBSP.mAniIntensities)
			{
				Buffer.MemoryCopy(pAni, pBspAnis, NumAniIntensities * 4, NumStyles * 4);
			}
		}
	}


	public void SetBones(Matrix4x4 []bones)
	{
		bones?.CopyTo(mBones, 0);
	}


	public void SetBonesWithTranspose(Matrix4x4 []bones)
	{
		if(bones == null)
		{
			return;
		}

		Debug.Assert(bones.Length <= MaxBones);

		Parallel.For(0, bones.Length, (b) =>
		{
			mBones[b]	=Matrix4x4.Transpose(bones[b]);
		});
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
		fixed(float *pPost = mPost.mWeightsX)
		{
			Misc.MemCpy(wx, pPost, KernelSize * 4);
		}
		fixed(float *pPost = mPost.mOffsetsX)
		{
			Misc.MemCpy(offx, pPost, KernelSize * 4);
		}
		fixed(float *pPost = mPost.mWeightsY)
		{
			Misc.MemCpy(wy, pPost, KernelSize * 4);
		}
		fixed(float *pPost = mPost.mOffsetsY)
		{
			Misc.MemCpy(offy, pPost, KernelSize * 4);
		}
	}
#endregion


#region PerShadow
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
#endregion


#region	TextMode
	public void SetTextModeScreenSize(uint width, uint height, uint cwidth, uint cheight)
	{
		mTextMode.mWidth	=width;
		mTextMode.mHeight	=height;
		mTextMode.mCWidth	=cwidth;
		mTextMode.mCHeight	=cheight;
	}


	public void SetTextModeFontInfo(uint startChar, uint numColumns, uint charWidth, uint charHeight)
	{
		mTextMode.mStartChar	=startChar;
		mTextMode.mNumColumns	=numColumns;
		mTextMode.mCharWidth	=charWidth;
		mTextMode.mCharHeight	=charHeight;
	}
#endregion


#region Cel
	public void	SetCelSteps(Vector4 mins, Vector4 maxs, Vector4 steps, int numSteps)
	{
		mCelStuff.mValMin	=mins;
		mCelStuff.mValMax	=maxs;
		mCelStuff.mSnapTo	=steps;
		mCelStuff.mNumSteps	=numSteps;
	}
#endregion
}