using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;


namespace MaterialLib;

//Material stuff specific to static / character meshes
//This might get passed to GUI stuff so public
public partial class MeshMat
{
	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetTrilights(mLightColor0, mLightColor1, mLightColor2, mLightDirection);
		cbk.SetSpecular(mSpecColor, mSpecPower);
		cbk.SetSolidColour(mSolidColour);

		ID3D11ShaderResourceView	srv	=sk.GetSRV(mTexture0);
		if(srv != null)
		{
			dc.PSSetShaderResource(0, srv);
		}
		srv	=sk.GetSRV(mTexture1);
		if(srv != null)
		{
			dc.PSSetShaderResource(1, srv);
		}
	}
}