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
	PrimObject	mBoxBound, mSphereBound;

	Dictionary<int, PrimObject>	mBoxes		=new Dictionary<int, PrimObject>();
	Dictionary<int, PrimObject>	mSpheres	=new Dictionary<int, PrimObject>();
	Dictionary<int, PrimObject>	mCapsules	=new Dictionary<int, PrimObject>();

	Vector3	mLightDir	=-Vector3.UnitY;

	const float	AxisSize	=50f;


	public CommonPrims(ID3D11Device gd, StuffKeeper sk)
	{
		//extra material lib for prim stuff
		mSK	=sk;

		//shaders
		mVS	=sk.GetVertexShader("WNormWPosTexVS");
		mPS	=sk.GetPixelShader("TriSolidSpecPS");

		//axis boxes
		BoundingBox	xBox	=Misc.MakeBox(AxisSize, 1f, 1f);
		BoundingBox	yBox	=Misc.MakeBox(1f, AxisSize, 1f);
		BoundingBox	zBox	=Misc.MakeBox(1f, 1f, AxisSize);

		byte	[]code	=sk.GetVSCompiledCode("WNormWPosTexVS");

		mXAxis	=PrimFactory.CreateCube(gd, code, xBox);
		mYAxis	=PrimFactory.CreateCube(gd, code, yBox);
		mZAxis	=PrimFactory.CreateCube(gd, code, zBox);

		mXAxis.World	=Matrix4x4.Identity;
		mYAxis.World	=Matrix4x4.Identity;
		mZAxis.World	=Matrix4x4.Identity;
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


	public void Update(GameCamera gcam, Vector3 lightDir)
	{
		mSK.GetCBKeeper().SetView(gcam.ViewTransposed, gcam.Position);

		mLightDir	=lightDir;
	}


	public void DrawAxis()
	{
		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		dc.VSSetShader(mVS);
		dc.PSSetShader(mPS);

		cbk.SetWorldMat(Matrix4x4.Transpose(Matrix4x4.Identity));
//		cbk.SetWorldMat(Matrix4x4.Identity);

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
	}


	public void DrawCapsule(int index, Matrix4x4 transform)
	{
		if(!mCapsules.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		cbk.SetSolidColour(Vector4.One * 0.5f);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);

		mCapsules[index].Draw(dc);
	}


	public void DrawBox(int index, Matrix4x4 transform)
	{
		if(!mBoxes.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		lightColor2.W	=lightColor3.W	=1f;

		cbk.SetSolidColour(Vector4.One * 0.5f);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mBoxes[index].Draw(dc);
	}


	public void DrawBox(int index, Matrix4x4 transform, Vector4 color)
	{
		if(!mBoxes.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(color);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mBoxes[index].Draw(dc);
	}


	public void DrawBox(Matrix4x4 transform)
	{
		if(mBoxBound == null)
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(Vector4.One * 0.5f);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mBoxBound.Draw(dc);
	}


	public void DrawSphere(int index, Matrix4x4 transform)
	{
		if(!mSpheres.ContainsKey(index))
		{
			return;
		}

		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(Vector4.One * 0.5f);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mSpheres[index].Draw(dc);
	}


	public void DrawSphere(Matrix4x4 transform)
	{
		if(mSphereBound == null)
		{
			return;
		}
		CBKeeper			cbk	=mSK.GetCBKeeper();		
		ID3D11DeviceContext	dc	=mVS.Device.ImmediateContext;

		Vector4	lightColor2	=Vector4.One * 0.8f;
		Vector4	lightColor3	=Vector4.One * 0.6f;

		cbk.SetSolidColour(Vector4.One * 0.5f);
		cbk.SetTrilights(Vector4.One, lightColor2, lightColor3, mLightDir);
		cbk.SetSpecular(Vector4.One, 1);
		cbk.SetWorldMat(transform);
		cbk.UpdateObject(dc);
		mSphereBound.Draw(dc);
	}


	public void AddBox(int index, BoundingBox box)
	{
		if(mBoxes.ContainsKey(index))
		{
			return;	//already a box here
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		PrimObject	po	=PrimFactory.CreateCube(mVS.Device, code, box);

		mBoxes.Add(index, po);
	}


	public void AddSphere(int index, BoundingSphere sphere)
	{
		if(mSpheres.ContainsKey(index))
		{
			return;	//already a sphere here
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
			return;	//already here
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		PrimObject	po	=PrimFactory.CreateCapsule(mVS.Device, code, cap.mRadius, cap.mLength);

		mCapsules.Add(index, po);
	}


	public void ReBuildBoundsDrawData(object mesh)
	{
		BoundingBox		box		=new BoundingBox(Vector3.Zero, Vector3.Zero);
		BoundingSphere	sphere	=new BoundingSphere(Vector3.Zero, 0f);

		if(mesh is Character)
		{
			Character	chr	=mesh as Character;

			//bone bound stuff
//			chr.ComputeBoneBounds(new List<string>());

			box		=chr.GetBoxBound();
			sphere	=chr.GetSphereBound();
		}
		else
		{
			StaticMesh	sm	=mesh as StaticMesh;

			box		=sm.GetBoxBound();
			sphere	=sm.GetSphereBound();
		}

		byte	[]code	=mSK.GetVSCompiledCode("WNormWPosTexVS");

		mBoxBound		=PrimFactory.CreateCube(mVS.Device, code, box);
		mSphereBound	=PrimFactory.CreateSphere(mVS.Device,
							code, sphere.Center, sphere.Radius);
	}
}
