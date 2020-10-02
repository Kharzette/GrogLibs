using System;
using System.Collections.Generic;
using UtilityLib;
using MeshLib;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using MatLib = MaterialLib.MaterialLib;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;


namespace SharedForms
{
	public class DebugDraw
	{
		Buffer				mVB, mIB;
		VertexBufferBinding	mVBBinding;
		int					mNumIndexes;
		Vector3				mLightDir;
		Random				mRand	=new Random();

		MatLib	mMatLib;


		public DebugDraw(GraphicsDevice gd, MaterialLib.StuffKeeper sk)
		{
			mMatLib	=new MatLib(gd, sk);

			mLightDir	=Mathery.RandomDirection(mRand);

			Vector4	lightColor2	=Vector4.One * 0.8f;
			Vector4	lightColor3	=Vector4.One * 0.6f;

			lightColor2.W	=lightColor3.W	=1f;

			mMatLib.CreateMaterial("LevelGeometry");
			mMatLib.SetMaterialEffect("LevelGeometry", "Static.fx");
			mMatLib.SetMaterialTechnique("LevelGeometry", "TriVColorSolidSpec");
			mMatLib.SetMaterialParameter("LevelGeometry", "mLightColor0", Vector4.One);
			mMatLib.SetMaterialParameter("LevelGeometry", "mLightColor1", lightColor2);
			mMatLib.SetMaterialParameter("LevelGeometry", "mLightColor2", lightColor3);
			mMatLib.SetMaterialParameter("LevelGeometry", "mSolidColour", Vector4.One);
			mMatLib.SetMaterialParameter("LevelGeometry", "mSpecPower", 1);
			mMatLib.SetMaterialParameter("LevelGeometry", "mSpecColor", Vector4.One);
			mMatLib.SetMaterialParameter("LevelGeometry", "mWorld", Matrix.Identity);
		}


		public void MakeDrawStuff(Device dev,
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
				vpnc[i].Position	=verts[i];
				vpnc[i].Color0		=colors[i];
				vpnc[i].Normal.X	=norms[i].X;
				vpnc[i].Normal.Y	=norms[i].Y;
				vpnc[i].Normal.Z	=norms[i].Z;
				vpnc[i].Normal.W	=1f;
			}

			mVB			=VertexTypes.BuildABuffer(dev, vpnc, vpnc[0].GetType());
			mIB			=VertexTypes.BuildAnIndexBuffer(dev, inds.ToArray());
			mVBBinding	=VertexTypes.BuildAVBB(VertexTypes.GetIndex(vpnc[0].GetType()), mVB);

			mNumIndexes	=inds.Count;
		}


		public void FreeAll()
		{
			mMatLib.FreeAll();
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

			mMatLib.SetParameterForAll("mLightDirection", -mLightDir);
			mMatLib.SetParameterForAll("mView", gd.GCam.View);
			mMatLib.SetParameterForAll("mEyePos", gd.GCam.Position);
			mMatLib.SetParameterForAll("mProjection", gd.GCam.Projection);

			mMatLib.ApplyMaterialPass("LevelGeometry", gd.DC, 0);

			gd.DC.InputAssembler.PrimitiveTopology
				=SharpDX.Direct3D.PrimitiveTopology.TriangleList;
				
			gd.DC.InputAssembler.SetVertexBuffers(0, mVBBinding);
			gd.DC.InputAssembler.SetIndexBuffer(mIB, Format.R16_UInt, 0);

			gd.DC.DrawIndexed(mNumIndexes, 0, 0);
		}
	}
}
