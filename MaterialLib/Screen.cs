using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using Buffer		=SharpDX.Direct3D11.Buffer;
using RenderForm	=SharpDX.Windows.RenderForm;


namespace MaterialLib
{
	public class Screen
	{
		internal struct VertexPosition
		{
			internal Vector3	Position;
		}

		//for a fullscreen quad
		Buffer				mQuadVB;
		Buffer				mQuadIB;
		VertexBufferBinding	mQuadBinding;

		//effect file
		Effect	mPostFX;

		//screen text contents
		Texture1D						mScreenContents;
		ShaderResourceView				mScreenContentsSRV;
		EffectShaderResourceVariable	mESRV;
		ShaderResourceView				mFontSRV;
		MaterialLib						mMats;

		//cached techniques and passes
		Dictionary<EffectTechnique, EffectPass>	mEPasses	=new Dictionary<EffectTechnique, EffectPass>();
		Dictionary<string, EffectTechnique>		mETechs		=new Dictionary<string, EffectTechnique>();

		//stuff
		int		mResX, mResY;
		Color	mClearColor;

		//constants
		const UInt32	NumColumns		=16;
		const UInt32	CharWidth		=8;
		const UInt32	CharHeight		=8;
		const UInt32	ScreenWidth		=40;	//in characters
		const UInt32	ScreenHeight	=25;	//in characters
		const UInt32	StartChar		=0;


		internal Screen(GraphicsDevice gd, Effect fx)
		{
			Init(gd, fx);
		}


		public Screen(GraphicsDevice gd, StuffKeeper sk, MaterialLib mats)
		{
			mFontSRV	=sk.GetFontSRV("CGA");
			mMats		=mats;

			Init(gd, sk.EffectForName("TextMode.fx"));
		}


		void Init(GraphicsDevice gd, Effect fx)
		{
			if(fx == null)
			{
				return;
			}

			mPostFX	=fx;

			mResX		=gd.RendForm.ClientRectangle.Width;
			mResY		=gd.RendForm.ClientRectangle.Height;
			mClearColor	=Color.CornflowerBlue;

			MakeQuad(gd);

			InitScreenParams();

			//make the screen contents 1d tex
			DataStream	ds	=new DataStream((int)(ScreenWidth * ScreenHeight), false, true);

			Texture1DDescription	texDesc	=new Texture1DDescription();
			texDesc.ArraySize		=1;
			texDesc.BindFlags		=BindFlags.ShaderResource;
			texDesc.CpuAccessFlags	=CpuAccessFlags.Write;
			texDesc.MipLevels		=1;
			texDesc.OptionFlags		=ResourceOptionFlags.None;
			texDesc.Usage			=ResourceUsage.Dynamic;
			texDesc.Width			=(int)(ScreenWidth * ScreenHeight);
			texDesc.Format			=Format.R8_UInt;

			mScreenContents		=new Texture1D(gd.GD, texDesc, ds);
			mScreenContentsSRV	=new ShaderResourceView(gd.GD, mScreenContents);

			EffectVariable	esv	=mPostFX.GetVariableByName("mScreenContents");

			mESRV	=esv.AsShaderResource();

			esv.Dispose();

			EffectVariable	evf	=mPostFX.GetVariableByName("mFont");

			EffectShaderResourceVariable	fsrv	=evf.AsShaderResource();

			fsrv.SetResource(mFontSRV);

			fsrv.Dispose();
			evf.Dispose();
		}


		//set up parameters with known values
		void InitScreenParams()
		{
			mMats.SetMaterialParameter("TextMode", "mWidth", 1280);
			mMats.SetMaterialParameter("TextMode", "mHeight", 720);
			mMats.SetMaterialParameter("TextMode", "mCWidth", 40);
			mMats.SetMaterialParameter("TextMode", "mCHeight", 25);
			mMats.SetMaterialParameter("TextMode", "mStartChar", StartChar);
			mMats.SetMaterialParameter("TextMode", "mNumColumns", NumColumns);
			mMats.SetMaterialParameter("TextMode", "mCharWidth", CharWidth);
			mMats.SetMaterialParameter("TextMode", "mCharHeight", CharHeight);
		}


		void MakeQuad(GraphicsDevice gd)
		{
			VertexPosition	[]verts	=new VertexPosition[4];

			verts[0].Position = new Vector3(-1, 1, 1);
			verts[1].Position = new Vector3(1, 1, 1);
			verts[2].Position = new Vector3(-1, -1, 1);
			verts[3].Position = new Vector3(1, -1, 1);
			
			UInt16	[]inds	=new UInt16[6];
			inds[0]	=0;
			inds[1]	=1;
			inds[2]	=2;
			inds[3]	=1;
			inds[4]	=3;
			inds[5]	=2;

			BufferDescription	bd	=new BufferDescription(
				12 * verts.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			mQuadVB	=Buffer.Create(gd.GD, verts, bd);

			BufferDescription	id	=new BufferDescription(inds.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			mQuadIB	=Buffer.Create<UInt16>(gd.GD, inds, id);

			mQuadBinding	=new VertexBufferBinding(mQuadVB, 12, 0);
		}


		public void UpdateWVP(GameCamera cam)
		{
			mMats.UpdateWVP(Matrix.Identity, cam.View,
							cam.Projection, cam.Position);
		}


		public void SetScreenContents(GraphicsDevice gd, byte []stuff)
		{
			DataStream	ds;

			gd.DC.MapSubresource(mScreenContents, 0, MapMode.WriteDiscard,
				SharpDX.Direct3D11.MapFlags.None, out ds);

			ds.Write(stuff, 0, stuff.Length);

			gd.DC.UnmapSubresource(mScreenContents, 0);

		}


		public void DrawStage(GraphicsDevice gd, string technique)
		{
			gd.DC.InputAssembler.PrimitiveTopology
				=SharpDX.Direct3D.PrimitiveTopology.TriangleList;

			gd.DC.InputAssembler.SetVertexBuffers(0, mQuadBinding);
			gd.DC.InputAssembler.SetIndexBuffer(mQuadIB, Format.R16_UInt, 0);

			mMats.SetMaterialParameter("TextMode", "mScreenContents", mScreenContentsSRV);

			mMats.ApplyMaterialPass("TextMode", gd.DC, 0);

			gd.DC.DrawIndexed(6, 0, 0);
		}


		public void FreeAll(GraphicsDevice gd)
		{
			mESRV.Dispose();
			mFontSRV.Dispose();
			mScreenContentsSRV.Dispose();
			mScreenContents.Dispose();

			//dispose all passes
			foreach(KeyValuePair<EffectTechnique, EffectPass> pass in mEPasses)
			{
				pass.Value.Dispose();
			}

			//techniques
			foreach(KeyValuePair<string, EffectTechnique> tech in mETechs)
			{
				tech.Value.Dispose();
			}

			//buffers
			mQuadIB.Dispose();
			mQuadVB.Dispose();

			mPostFX.Dispose();

			mMats.FreeAll();
		}
	}
}