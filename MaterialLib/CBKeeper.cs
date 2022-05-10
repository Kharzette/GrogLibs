using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using UtilityLib;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;
using Vortice.Mathematics;
using Vortice.WIC;


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
		internal UInt32		mPadding0;
		internal UInt32		mPadding1;
		internal UInt32		mPadding2;	//pad to 16
	}

	//CommonFunctions.hlsli
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	struct ChangeLess
	{
		internal Matrix4x4	mProjection;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	internal struct TwoD
	{
		internal Vector2	mTextPosition, mSecondLayerOffset;
		internal Vector2	mTextScale;
		internal Vector2	mPadding;
		internal Vector4	mTextColor;
	}

	//gpu side
	ID3D11Buffer	mPerObjectBuf;
	ID3D11Buffer	mPerFrameBuf;
	ID3D11Buffer	mChangeLessBuf;
	ID3D11Buffer	mTwoDBuf;

	//cpu side
	PerObject	mPerObject;
	PerFrame	mPerFrame;
	ChangeLess	mChangeLess;
	TwoD		mTwoD;


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
	}


	void AllocCBuffers(ID3D11Device dev)
	{
		//create constant buffers
		mPerObjectBuf	=MakeConstantBuffer(dev, sizeof(PerObject));
		mPerFrameBuf	=MakeConstantBuffer(dev, sizeof(PerFrame));
		mChangeLessBuf	=MakeConstantBuffer(dev, sizeof(ChangeLess));
		mTwoDBuf		=MakeConstantBuffer(dev, sizeof(TwoD));

		//alloc C# side constant buffer data
		mPerObject	=new PerObject();
		mPerFrame	=new PerFrame();
		mChangeLess	=new ChangeLess();
		mTwoD		=new TwoD();
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
		//commonfunctions
		dc.VSSetConstantBuffer(4, mTwoDBuf);
		dc.PSSetConstantBuffer(4, mTwoDBuf);
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


	public void SetWorldMat(Matrix4x4 world)
	{
		mPerObject.mWorld	=world;
	}


	public void SetMaterialID(int matID)
	{
		mPerObject.mMaterialID	=matID;
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


	public void SetProjection(Matrix4x4 proj)
	{
		mChangeLess.mProjection	=proj;
	}
}