using Vortice.Direct3D11;


namespace MaterialLib;

//Material stuff specific to BSP meshes
public partial class BSPMat
{
	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetTextureEnabled(mbTextureEnabled);
		cbk.SetTexSize(mTexSize);

		ID3D11Texture2D	tex	=sk.GetTexture2D(mTexture);

		ID3D11ShaderResourceView	srv	=sk.GetSRV(mTexture);
		if(srv != null)
		{
			if(tex.Description.ArraySize > 1)
			{
				dc.PSSetShaderResource(3, srv);
			}
			else
			{
				dc.PSSetShaderResource(0, srv);
			}
		}

		//not really sure yet how I'll identify the correct lightmap
		//if multiple bsp maps are open
		ID3D11ShaderResourceView	lm	=sk.GetSRV("LightMapAtlas");
		if(lm != null)
		{
			dc.PSSetShaderResource(1, lm);
		}
		cbk.UpdateBSP(dc);
	}
}