using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using UtilityLib;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

//ambiguous stuff
using Device	=SharpDX.Direct3D11.Device;
using Resource	=SharpDX.Direct3D11.Resource;


namespace MaterialLib
{
	public partial class MaterialLib
	{
		//cel shading lookup textures
		//allows for many different types of shading
		ShaderResourceView	[]mCelResources;
		Texture2D			[]mCelTex2Ds;
		Texture1D			[]mCelTex1Ds;

		//constants for a world preset
		//looks good with bsp lightmapped levels
		const int	WorldLookupSize	=256;
		const float	WorldThreshold0	=0.7f;
		const float	WorldThreshold1	=0.3f;
		const float	WorldLevel0		=1.0f;
		const float	WorldLevel1		=0.5f;
		const float	WorldLevel2		=0.08f;

		//constants for a character preset
		//looks good for anime style characters
		const int	CharacterLookupSize	=256;
		const float	CharacterThreshold0	=0.6f;
		const float	CharacterThreshold1	=0.35f;
		const float	CharacterThreshold2	=0.1f;
		const float	CharacterLevel0		=1.0f;
		const float	CharacterLevel1		=0.6f;
		const float	CharacterLevel2		=0.3f;
		const float	CharacterLevel3		=0.1f;


		public void InitCelShading(int numShadingVariations)
		{
			mCelResources	=new ShaderResourceView[numShadingVariations];
			mCelTex1Ds		=new Texture1D[numShadingVariations];
			mCelTex2Ds		=new Texture2D[numShadingVariations];
		}


		public void GetDefaultValues(out float []thresh, out float []level, out int size)
		{
			thresh	=new float[2];
			level	=new float[3];

			thresh[0]	=WorldThreshold0;
			thresh[1]	=WorldThreshold1;
			level[0]	=WorldLevel0;
			level[1]	=WorldLevel1;
			level[2]	=WorldLevel2;

			size	=WorldLookupSize;
		}


		public void SetCelTexture(int index)
		{			
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter("mCelTable", mCelResources[index]);
			}
		}


		//the nine three thing is if the feature level is 9.3
		public void GenerateCelTexturePreset(Device gd, bool bNineThree, bool bCharacter, int index)
		{
			float	[]thresholds;
			float	[]levels;
			int		size;

			if(bCharacter)
			{
				thresholds	=new float[3];
				levels		=new float[4];

				thresholds[0]	=CharacterThreshold0;
				thresholds[1]	=CharacterThreshold1;
				thresholds[2]	=CharacterThreshold2;
				levels[0]		=CharacterLevel0;
				levels[1]		=CharacterLevel1;
				levels[2]		=CharacterLevel2;
				levels[3]		=CharacterLevel3;
				size			=CharacterLookupSize;
			}
			else
			{
				//worldy preset
				thresholds	=new float[2];
				levels		=new float[3];

				thresholds[0]	=WorldThreshold0;
				thresholds[1]	=WorldThreshold1;
				levels[0]		=WorldLevel0;
				levels[1]		=WorldLevel1;
				levels[2]		=WorldLevel2;
				size			=WorldLookupSize;
			}

			GenerateCelTexture(gd, bNineThree, index, size, thresholds, levels);
		}


		//generate a lookup texture for cel shading
		//this allows a game to specify exactly instead of using a preset
		public void GenerateCelTexture(Device gd, bool bNineThree,
			int index, int size, float []thresholds, float []levels)
		{
			if(mCelResources == null)
			{
				return;	//need to init with a size first
			}

			SampleDescription	sampDesc	=new SampleDescription();
			sampDesc.Count		=1;
			sampDesc.Quality	=0;

			Resource	res	=null;
			if(bNineThree)
			{
				Texture2DDescription	texDesc	=new Texture2DDescription();
				texDesc.ArraySize			=1;
				texDesc.BindFlags			=BindFlags.ShaderResource;
				texDesc.CpuAccessFlags		=CpuAccessFlags.None;
				texDesc.MipLevels			=1;
				texDesc.OptionFlags			=ResourceOptionFlags.None;
				texDesc.Usage				=ResourceUsage.Default;
				texDesc.Width				=size;
				texDesc.Height				=size;
				texDesc.Format				=Format.R16_UNorm;
				texDesc.SampleDescription	=sampDesc;

				DataStream	ds	=new DataStream(
					texDesc.Width * texDesc.Height *
					(int)FormatHelper.SizeOfInBytes(texDesc.Format),
					true, true);

				float	csize	=size;
				for(int y=0;y < size;y++)
				{
					for(int x=0;x < size;x++)
					{
						float	xPercent	=(float)x / csize;

						float	val	=CelMe(xPercent, thresholds, levels);

						UInt16	val2	=(UInt16)(val * 65535f);

						ds.Write(val2);
					}
				}

				DataBox	[]dbs	=new DataBox[1];

				dbs[0]	=new DataBox(ds.DataPointer,
					texDesc.Width *
					(int)FormatHelper.SizeOfInBytes(texDesc.Format),
					texDesc.Width * texDesc.Height *
					(int)FormatHelper.SizeOfInBytes(texDesc.Format));

				res	=mCelTex2Ds[index]	=new Texture2D(gd, texDesc, dbs);
			}
			else
			{
				DataStream	ds	=new DataStream(size * 2, false, true);
				float	csize	=size;
				for(int x=0;x < size;x++)
				{
					float	xPercent	=(float)x / csize;

					Half	val	=CelMe(xPercent, thresholds, levels);

					ds.Write(val);
				}
				Texture1DDescription	texDesc	=new Texture1DDescription();
				texDesc.ArraySize		=1;
				texDesc.BindFlags		=BindFlags.ShaderResource;
				texDesc.CpuAccessFlags	=CpuAccessFlags.None;
				texDesc.MipLevels		=1;
				texDesc.OptionFlags		=ResourceOptionFlags.None;
				texDesc.Usage			=ResourceUsage.Immutable;
				texDesc.Width			=size;
				texDesc.Format			=Format.R16_Float;

				res	=mCelTex1Ds[index]	=new Texture1D(gd, texDesc, ds);
			}

			mCelResources[index]	=new ShaderResourceView(gd, res);
		}


		float	CelMe(float val, float []thresholds, float []levels)
		{
			float	ret	=-69f;

			Debug.Assert(thresholds.Length == (levels.Length - 1));

			for(int i=0;i < thresholds.Length;i++)
			{
				if(val > thresholds[i])
				{
					ret	=levels[i];
					break;
				}
			}

			if(ret < -68f)
			{
				ret	=levels[levels.Length - 1];
			}
			return	ret;
		}
	}
}
