using Vortice.Direct3D11;


namespace MaterialLib;

//The idea behind this class is to store stuff for a draw call.
//Such as shaders, shader variable values like colors and specularity,
//and textures and such
internal partial class Material
{
	internal void Apply(ID3D11DeviceContext dc, StuffKeeper sk)
	{
		CBKeeper	cbk	=sk.GetCBKeeper();
		
		//shaders first
		dc.VSSetShader(sk.GetVertexShader(mVSName));
		dc.PSSetShader(sk.GetPixelShader(mPSName));

		cbk.SetCommonCBToShaders(dc);

		if(mBSPVars != null)
		{
			cbk.SetBSPToShaders(dc);
		}

		//layout
		dc.IASetInputLayout(sk.GetOrCreateLayout(mVSName));

		//renderstates
		dc.OMSetBlendState(sk.GetBlendState(mBlendState));
		dc.OMSetDepthStencilState(sk.GetDepthStencilState(mDSS));

		//sampling
		dc.PSSetSampler(0, sk.GetSamplerState(mSamplerState0));
		dc.PSSetSampler(1, sk.GetSamplerState(mSamplerState1));

		cbk.SetMaterialID(mMaterialID);

		mMeshVars.Apply(dc, cbk, sk);
		mBSPVars?.Apply(dc, cbk, sk);

		//hopefully the world matrix is set by now
		cbk.UpdateObject(dc);
	}
}