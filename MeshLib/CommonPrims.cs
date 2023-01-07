using System.Numerics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;
using MaterialLib;


namespace MeshLib;

public class CommonPrims
{
	StuffKeeper	mSK;

	ID3D11VertexShader	mVS;
	ID3D11PixelShader	mPS;

	PrimObject	mXAxis, mYAxis, mZAxis;
	PrimObject	mLightAxis, mLightPointyEnd;

	Dictionary<int, PrimObject>	mBoxes		=new Dictionary<int, PrimObject>();
	Dictionary<int, PrimObject>	mSpheres	=new Dictionary<int, PrimObject>();
	Dictionary<int, PrimObject>	mCapsules	=new Dictionary<int, PrimObject>();

	Vector3	mLightDir	=-Vector3.UnitY;

	Matrix4x4	mAxisScale			=Matrix4x4.Identity;
	Matrix4x4	mArrowHeadOffset	=Matrix4x4.Identity;

	const float	AxisSize	=5f;			//meters
	const float	AxisWidth	=0.0254000443f;	//meters


	public CommonPrims(ID3D11Device gd, StuffKeeper sk)
	{
		//extra material lib for prim stuff
		mSK	=sk;

		//shaders
		mVS	=sk.GetVertexShader("WNormWPosTexVS");
		mPS	=sk.GetPixelShader("TriSolidSpecPS");

		//axis boxes
		BoundingBox	xBox		=Misc.MakeBox(AxisSize, AxisWidth, AxisWidth);
		BoundingBox	yBox		=Misc.MakeBox(AxisWidth, AxisSize, AxisWidth);
		BoundingBox	zBox		=Misc.MakeBox(AxisWidth, AxisWidth, AxisSize);
		BoundingBox	lightBox	=Misc.MakeBaseOrgBox(AxisWidth, AxisWidth, 1f);

		byte	[]code	=sk.GetVSCompiledCode("WNormWPosTexVS");

		mXAxis		=PrimFactory.CreateCube(gd, code, xBox);
		mYAxis		=PrimFactory.CreateCube(gd, code, yBox);
		mZAxis		=PrimFactory.CreateCube(gd, code, zBox);
		mLightAxis	=PrimFactory.CreateCube(gd, code, lightBox);

		mLightPointyEnd	=PrimFactory.CreateHalfPrism(gd, code, AxisWidth * 2f);

		mXAxis.World			=Matrix4x4.Identity;
		mYAxis.World			=Matrix4x4.Identity;
		mZAxis.World			=Matrix4x4.Identity;
		mLightAxis.World		=Matrix4x4.Identity;
		mLightPointyEnd.World	=Matrix4x4.Identity;

		mArrowHeadOffset	=Matrix4x4.CreateRotationX(-MathHelper.PiOver2)
							*Matrix4x4.CreateTranslation(Vector3.UnitZ * (1f + (AxisWidth * 8)));
	}


	public void FreeAll()
	{
		foreach(KeyValuePair<int, PrimObject> box in mBoxes)
		{
			box.Value.Free();
		}

		foreach(KeyValuePair<int, PrimObject> sphere in mSpheres)
		{
			sphere.Value.Free();
		}

		foreach(KeyValuePair<int, PrimObject> capsule in mCapsules)
		{
			capsule.Value.Free();
		}

		mVS.Dispose();
		mPS.Dispose();
	}


	public void SetAxisScale(float scale)
	{
		mAxisScale	=Matrix4x4.CreateScale(scale);
	}


	public void Update(GameCamera gcam, Vector3 lightDir)
	{
		mSK.GetCBKeeper().SetTransposedView(gcam.ViewTransposed, gcam.Position);

		mLightDir	=lightDir;
	}


	public void DrawLightArrow(Matrix4x4 transform, Vector4 color)
	{
		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		cbk.SetSolidColour(color);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(mAxisScale * transform);
		cbk.UpdateObject(dc);

		mLightAxis.Draw(dc);

		cbk.SetWorldMat(mArrowHeadOffset * mAxisScale * transform);
		cbk.UpdateObject(dc);

		mLightPointyEnd.Draw(dc);
	}


	public void DrawAxis()
	{
		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		cbk.SetWorldMat(mAxisScale);

		Vector4	redColor	=Vector4.One;
		Vector4	greenColor	=Vector4.One;
		Vector4	blueColor	=Vector4.One;
		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		redColor.Y	=redColor.Z	=greenColor.X	=greenColor.Z	=blueColor.X	=blueColor.Y	=0f;

		//X axis red
		cbk.SetSolidColour(redColor);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.UpdateObject(dc);
		mXAxis.Draw(dc);

		//Y axis green
		cbk.SetSolidColour(greenColor);
		cbk.UpdateObject(dc);
		mYAxis.Draw(dc);

		//Z axis blue
		cbk.SetSolidColour(blueColor);
		cbk.UpdateObject(dc);
		mZAxis.Draw(dc);

		//transform for pointy end to aim at positive axiseseeses
		Vector3	axisOfs	=Vector3.UnitY	* ((AxisSize * 0.5f) + (AxisWidth * 8));
		axisOfs	=Vector3.Transform(axisOfs, mAxisScale);

		Matrix4x4	rot		=Matrix4x4.CreateRotationX(MathHelper.Pi);
		Matrix4x4	trans	=Matrix4x4.CreateTranslation(axisOfs);

		//arrow pointy end for positive Y
		cbk.SetWorldMat(mAxisScale * rot * trans);
		cbk.SetSolidColour(greenColor);
		cbk.UpdateObject(dc);
		mLightPointyEnd.Draw(dc);

		//adjust for Z
		axisOfs	=Vector3.UnitZ	* ((AxisSize * 0.5f) + (AxisWidth * 8));
		axisOfs	=Vector3.Transform(axisOfs, mAxisScale);
		
		rot		=Matrix4x4.CreateRotationX(-MathHelper.PiOver2);
		trans	=Matrix4x4.CreateTranslation(axisOfs);

		//for Z
		cbk.SetWorldMat(mAxisScale * rot * trans);
		cbk.SetSolidColour(blueColor);
		cbk.UpdateObject(dc);
		mLightPointyEnd.Draw(dc);

		//adjust for X
		axisOfs	=Vector3.UnitX	* ((AxisSize * 0.5f) + (AxisWidth * 8));
		axisOfs	=Vector3.Transform(axisOfs, mAxisScale);
		
		rot		=Matrix4x4.CreateRotationZ(MathHelper.PiOver2);
		trans	=Matrix4x4.CreateTranslation(axisOfs);

		//for Z
		cbk.SetWorldMat(mAxisScale * rot * trans);
		cbk.SetSolidColour(redColor);
		cbk.UpdateObject(dc);
		mLightPointyEnd.Draw(dc);
	}


	public void DrawCapsule(int index, Matrix4x4 transform, Vector4 color)
	{
		if(!mCapsules.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		cbk.SetSolidColour(color);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);

		mCapsules[index].Draw(dc);
	}


	public void DrawBox(int index, Matrix4x4 transform, Vector4 color)
	{
		if(!mBoxes.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(color);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mBoxes[index].Draw(dc);
	}


	public void DrawSphere(int index, Matrix4x4 transform, Vector4 color)
	{
		if(!mSpheres.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(color);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mSpheres[index].Draw(dc);
	}


	public void AddBox(int index, BoundingBox box)
	{
		if(mBoxes.ContainsKey(index))
		{
			//blast old
			mBoxes[index].Free();
			mBoxes.Remove(index);
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		PrimObject	po	=PrimFactory.CreateCube(mVS.Device, code, box);

		mBoxes.Add(index, po);
	}


	public void AddSphere(int index, BoundingSphere sphere)
	{
		if(mSpheres.ContainsKey(index))
		{
			//blast old
			mSpheres[index].Free();
			mSpheres.Remove(index);
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		PrimObject	po	=PrimFactory.CreateSphere(mVS.Device,
								code, sphere.Center, sphere.Radius);

		mSpheres.Add(index, po);
	}


	public void AddCapsule(int index, BoundingCapsule cap)
	{
		if(mCapsules.ContainsKey(index))
		{
			//blast old
			mCapsules[index].Free();
			mCapsules.Remove(index);
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		PrimObject	po	=PrimFactory.CreateCapsule(mVS.Device, code, cap.mRadius, cap.mLength);

		mCapsules.Add(index, po);
	}
}
