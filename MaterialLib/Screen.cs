using System;
using System.Numerics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.DXGI;
using UtilityLib;

using Color		=Vortice.Mathematics.Color;

namespace MaterialLib;

public class Screen
{
	internal struct VertexPosition
	{
		internal Vector3	Position;
	}

	//for a fullscreen quad
	ID3D11Buffer	mQuadVB;
	ID3D11Buffer	mQuadIB;

	//screen text contents
	ID3D11Texture1D				mScreenContents;
	ID3D11ShaderResourceView	mScreenContentsSRV;
	ID3D11ShaderResourceView	mFontSRV;

	//stuff
	StuffKeeper	mSK;
	int			mResX, mResY;
	Color		mClearColor;

	//constants
	const UInt32	NumColumns		=16;
	const UInt32	CharWidth		=8;
	const UInt32	CharHeight		=8;
	const UInt32	ScreenWidth		=40;	//in characters
	const UInt32	ScreenHeight	=25;	//in characters
	const UInt32	StartChar		=0;


	public Screen(GraphicsDevice gd, StuffKeeper sk)
	{
		mSK			=sk;
		mFontSRV	=sk.GetFontSRV("CGA");

		Init(gd);
	}


	void Init(GraphicsDevice gd)
	{
		mResX		=gd.RendForm.ClientRectangle.Width;
		mResY		=gd.RendForm.ClientRectangle.Height;
		mClearColor	=Misc.SystemColorToDXColor(System.Drawing.Color.CornflowerBlue);

		MakeQuad(gd);

		InitScreenParams();

		//make the screen contents 1d tex
		Texture1DDescription	texDesc	=new Texture1DDescription();
		texDesc.ArraySize		=1;
		texDesc.BindFlags		=BindFlags.ShaderResource;
		texDesc.CPUAccessFlags	=CpuAccessFlags.Write;
		texDesc.MipLevels		=1;
		texDesc.MiscFlags		=ResourceOptionFlags.None;
		texDesc.Usage			=ResourceUsage.Dynamic;
		texDesc.Width			=(int)(ScreenWidth * ScreenHeight);
		texDesc.Format			=Format.R8_UInt;

		mScreenContents		=gd.GD.CreateTexture1D(texDesc);
		mScreenContentsSRV	=gd.GD.CreateShaderResourceView(mScreenContents);
	}


	//set up parameters with known values
	void InitScreenParams()
	{
		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.SetTextModeScreenSize(1280, 720, ScreenWidth, ScreenHeight);
		cbk.SetTextModeFontInfo(StartChar, NumColumns, CharWidth, CharHeight);
	}


	void MakeQuad(GraphicsDevice gd)
	{
		VertexPosition	[]verts	=new VertexPosition[4];

		verts[0].Position = new Vector3(0, 720, -0.5f);
		verts[1].Position = new Vector3(1280, 720, -0.5f);
		verts[2].Position = new Vector3(0, 0, -0.5f);
		verts[3].Position = new Vector3(1280, 0, -0.5f);
		
		UInt16	[]inds	=new UInt16[6];
		inds[0]	=0;
		inds[1]	=2;
		inds[2]	=1;
		inds[3]	=1;
		inds[4]	=2;
		inds[5]	=3;

		BufferDescription	bd	=new BufferDescription(
			12 * verts.Length, BindFlags.VertexBuffer);

		mQuadVB	=gd.GD.CreateBuffer<VertexPosition>(verts, bd);

		BufferDescription	id	=new BufferDescription(inds.Length * 2,
			BindFlags.IndexBuffer, ResourceUsage.Immutable,
			CpuAccessFlags.None, ResourceOptionFlags.None, 0);

		mQuadIB	=gd.GD.CreateBuffer<UInt16>(inds, id);
	}


	public unsafe void SetScreenContents(GraphicsDevice gd, byte []stuff)
	{
		Span<byte>	blort	=gd.DC.Map<byte>(mScreenContents, 0, 0, MapMode.WriteDiscard);
		stuff.CopyTo(blort);
		gd.DC.Unmap(mScreenContents, 0, 0);		
	}


	public void DrawStage(GraphicsDevice gd)
	{
		gd.DC.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

		gd.DC.IASetVertexBuffer(0, mQuadVB, 12);
		gd.DC.IASetIndexBuffer(mQuadIB, Format.R16_UInt, 0);

		ID3D11InputLayout	lay	=mSK.GetOrCreateLayout("SimpleVS");

		gd.DC.VSSetShader(mSK.GetVertexShader("SimpleVS"));
		gd.DC.PSSetShader(mSK.GetPixelShader("TextModePS"));
		gd.DC.IASetInputLayout(lay);

		gd.DC.PSSetShaderResource(0, mScreenContentsSRV);
		gd.DC.PSSetShaderResource(1, mFontSRV);

		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.UpdateTextMode(gd.DC);
		cbk.SetTextModeToShaders(gd.DC);

		gd.DC.DrawIndexed(6, 0, 0);
	}


	public void FreeAll(GraphicsDevice gd)
	{
		mFontSRV.Dispose();
		mScreenContentsSRV.Dispose();
		mScreenContents.Dispose();

		//buffers
		mQuadIB.Dispose();
		mQuadVB.Dispose();
	}
}