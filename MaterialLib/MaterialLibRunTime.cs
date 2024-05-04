using System.Numerics;
using System.Collections.Generic;
using Vortice.Direct3D11;


namespace MaterialLib;

public partial class MaterialLib
{
	StuffKeeper	mSKeeper;

	public MaterialLib(StuffKeeper sk)
	{
		mSKeeper	=sk;
	}


	public void ApplyMaterial(string matName, ID3D11DeviceContext dc)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}
		Material	mat	=mMats[matName];

		mat.Apply(dc, mSKeeper);
	}


	public void CheckMaterialType(string name)
	{
		if(!mMats.ContainsKey(name))
		{
			return;
		}

		Material	mat	=mMats[name];

		bool	bBsp	=false;
		bool	bChar	=false;

		string	hlslFile	=mSKeeper.GetHLSLName(mat.VSName);
		if(hlslFile == "BSP")
		{
			bBsp	=true;
		}
		else if(hlslFile == "Character")
		{
			bChar	=true;
		}

		mat.ChangeType(bBsp, bChar);
	}

	
	internal ID3D11Texture2D GetTexture2D(string texName)
	{
		return	mSKeeper.GetTexture2D(texName);
	}


	internal Font GetFont(string fontName)
	{
		return	mSKeeper.GetFont(fontName);
	}


	//grab device context from some loaded resource
	public ID3D11DeviceContext GetDC()
	{
		return	mSKeeper.GetDC();
	}


	public CBKeeper	GetCBKeeper()
	{
		return	mSKeeper.GetCBKeeper();
	}


	public void SetMaterialShadersAndLayout(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	m	=mMats[matName];

		ID3D11VertexShader	vs	=mSKeeper.GetVertexShader(m.VSName);
		ID3D11PixelShader	ps	=mSKeeper.GetPixelShader(m.PSName);

		if(vs == null || ps == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=vs.Device.ImmediateContext;

		dc.VSSetShader(vs);
		dc.PSSetShader(ps);

		dc.IASetInputLayout(mSKeeper.GetOrCreateLayout(m.VSName));
	}


	public void SetMaterialStates(string matName, string blendState, string depthState)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	m	=mMats[matName];

		m.SetStates(blendState, depthState);
	}


	public void GuessTextures()
	{
		List<string>	textures	=mSKeeper.GetTexture2DList(true);

		foreach(KeyValuePair<string, Material> mat in mMats)
		{
			//only applies to bsp materials
			if(mat.Value.mBSPVars == null)
			{
				continue;
			}

			string	rawMatName	=mat.Key;
			if(rawMatName.Contains("*"))
			{
				rawMatName	=rawMatName.Substring(0, rawMatName.IndexOf('*'));
			}

			foreach(string tex in textures)
			{
				if(tex.Contains(rawMatName)
					|| tex.Contains(rawMatName.ToLower()))
				{
					ID3D11Texture2D				tex2D	=mSKeeper.GetTexture2D(tex);
					ID3D11ShaderResourceView	srv		=mSKeeper.GetSRV(tex);

					if(tex2D == null || srv == null)
					{
						continue;
					}

					Vector2	texSize	=Vector2.Zero;

					texSize.X	=tex2D.Description.Width;
					texSize.Y	=tex2D.Description.Height;

					mat.Value.mBSPVars.Texture			=tex;
					mat.Value.mBSPVars.TextureEnabled	=true;
					mat.Value.mBSPVars.TextureSize		=texSize;
					break;
				}
			}
		}
	}
}