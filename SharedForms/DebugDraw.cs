using System;
using System.Numerics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;
using UtilityLib;
using MeshLib;
using MaterialLib;


namespace SharedForms;

public class DebugDraw
{
	StuffKeeper			mSK;
	ID3D11Buffer		mVB, mIB;
	int					mNumIndexes;
	Vector3				mLightDir;
	Random				mRand	=new Random();


	public DebugDraw(GraphicsDevice gd, StuffKeeper sk)
	{
		mSK	=sk;

		mLightDir	=Mathery.RandomDirection(mRand);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		byte	[]code	=sk.GetVSCompiledCode("WNormWPosVColorVS");

	}


	public void MakeDrawStuff(ID3D11Device dev,
		List<Vector3> verts,
		List<Vector3> norms,
		List<Color> colors,
		List<UInt16> inds)
	{
		if(verts.Count <= 0)
		{
			return;
		}

		VPosNormCol0	[]vpnc	=new VPosNormCol0[verts.Count];

		for(int i=0;i < vpnc.Length;i++)
		{
			Vector3	norm	=norms[i];

			vpnc[i].Position	=verts[i];
			vpnc[i].Color0		=colors[i];
			vpnc[i].Normal		=new Half4(norm.X, norm.Y, norm.Z, 1f);
		}

		mVB	=VertexTypes.BuildABuffer(dev, vpnc, vpnc[0].GetType());
		mIB	=VertexTypes.BuildAnIndexBuffer(dev, inds.ToArray());

		mNumIndexes	=inds.Count;
	}


	public void FreeAll()
	{
		if(mVB != null)
		{
			mVB.Dispose();
		}
		if(mIB != null)
		{
			mIB.Dispose();
		}
	}


	public void Draw(GraphicsDevice gd)
	{
		if(gd.DC == null)
		{
			return;
		}
		if(mVB == null)
		{
			return;
		}

		CBKeeper	cbk	=mSK.GetCBKeeper();

		gd.DC.IASetVertexBuffer(0, mVB, 24);
		gd.DC.IASetIndexBuffer(mIB, Format.R16_UInt, 0);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		cbk.SetView(gd.GCam.ViewTransposed, gd.GCam.Position);
		cbk.SetWorldMat(Matrix4x4.Identity);

		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1f);
		cbk.UpdateObject(gd.DC);

		gd.DC.DrawIndexed(mNumIndexes, 0, 0);
	}
}