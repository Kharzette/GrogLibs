using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;
using UtilityLib;


namespace MaterialLib;

/*
public class DynamicLights
{
	public class DynLight
	{
		public float	mStrength;
		public Vector3	mPosition;
		public Vector3	mColor;
		public bool		mbOn;			//on by default
	}

	//simple vector4 tex for passing dyn lights to shaders
	Texture1D			mDynLights;
	ShaderResourceView	mDynSRV;

	EffectShaderResourceVariable	mESRV;

	//effect to set the dynamic light parameter on
	//TODO: might need multiple
	Effect	mFX;

	//active list of dynamic lights
	Dictionary<int, DynLight>	mDynZoneLights	=new Dictionary<int, DynLight>();

	Vector4	[]mDynArray;
	bool	[]mInUse;

	const int	MaxLights	=16;


	public DynamicLights(GraphicsDevice gd, Effect fx)
	{
		Init(gd, fx);
	}


	public DynamicLights(GraphicsDevice gd, MaterialLib matLib, string fxName)
	{
		if(gd == null || matLib == null)
		{
			return;
		}

		Effect	fx	=matLib.GetEffect(fxName);
		if(fx == null)
		{
			return;
		}

		Init(gd, fx);
	}


	void Init(GraphicsDevice gd, Effect fx)
	{
		mDynArray	=new Vector4[MaxLights * 2];
		mInUse		=new bool[MaxLights];

		mFX	=fx;

		SampleDescription	sampDesc	=new SampleDescription();
		sampDesc.Count		=1;
		sampDesc.Quality	=0;

		DataStream	ds	=new DataStream(MaxLights * 2 * 16, false, true);
		for(int x=0;x < MaxLights;x++)
		{
			ds.Write(Vector4.Zero);
			ds.Write(Vector4.Zero);
		}

		Texture1DDescription	texDesc	=new Texture1DDescription();
		texDesc.ArraySize		=1;
		texDesc.BindFlags		=BindFlags.ShaderResource;
		texDesc.CpuAccessFlags	=CpuAccessFlags.Write;
		texDesc.MipLevels		=1;
		texDesc.OptionFlags		=ResourceOptionFlags.None;
		texDesc.Usage			=ResourceUsage.Dynamic;
		texDesc.Width			=MaxLights * 2;
		texDesc.Format			=Format.R32G32B32A32_Float;

		mDynLights	=new Texture1D(gd.GD, texDesc, ds);
		mDynSRV		=new ShaderResourceView(gd.GD, mDynLights);

		EffectVariable	esv	=mFX.GetVariableByName("mDynLights");

		mESRV	=esv.AsShaderResource();

		esv.Dispose();
	}


	public void FreeAll()
	{
		mESRV.Dispose();
		mDynSRV.Dispose();
		mDynLights.Dispose();
		mFX.Dispose();

		mDynZoneLights.Clear();

		mDynArray	=null;
		mInUse		=null;
	}


	public Dictionary<int, DynLight> GetDynLights()
	{
		return	mDynZoneLights;
	}


	public bool CreateDynamicLight(Vector3 pos,
		Vector3 color, float strength, out int id)
	{
		for(int i=0;i < MaxLights;i++)
		{
			if(mInUse[i])
			{
				continue;
			}

			mInUse[i]	=true;
			id			=i;

			mDynArray[i * 2].X	=pos.X;
			mDynArray[i * 2].Y	=pos.Y;
			mDynArray[i * 2].Z	=pos.Z;

			mDynArray[i * 2].W	=strength;

			mDynArray[(i * 2) + 1].X	=color.X;
			mDynArray[(i * 2) + 1].Y	=color.Y;
			mDynArray[(i * 2) + 1].Z	=color.Z;

			mDynArray[(i * 2) + 1].W	=1f;

			DynLight	dl	=new DynLight();
			dl.mStrength	=strength;
			dl.mPosition	=pos;
			dl.mColor		=color;
			dl.mbOn			=true;

			mDynZoneLights.Add(id, dl);

			return	true;
		}
		id		=-1;
		return	false;
	}


	public void Update(GraphicsDevice gd)
	{
		DataStream	ds;

		gd.DC.MapSubresource(mDynLights, 0, MapMode.WriteDiscard,
			SharpDX.Direct3D11.MapFlags.None, out ds);

		for(int i=0;i < (MaxLights * 2);i++)
		{
			ds.Write<Vector4>(mDynArray[i]);
		}

		gd.DC.UnmapSubresource(mDynLights, 0);
	}


	public void SetParameter()
	{
		mESRV.SetResource(mDynSRV);
	}


	public void SetStrength(int id, float strength)
	{
		if(!mInUse[id])
		{
			return;
		}

		mDynArray[id * 2].W	=strength;

		mDynZoneLights[id].mStrength	=strength;
	}


	public void SetPos(int id, Vector3 pos)
	{
		if(!mInUse[id])
		{
			return;
		}

		mDynArray[id * 2].X	=pos.X;
		mDynArray[id * 2].Y	=pos.Y;
		mDynArray[id * 2].Z	=pos.Z;

		mDynZoneLights[id].mPosition	=pos;
	}


	public void SetColor(int id, Vector3 color)
	{
		if(!mInUse[id])
		{
			return;
		}

		mDynArray[(id * 2) + 1].X	=color.X;
		mDynArray[(id * 2) + 1].Y	=color.Y;
		mDynArray[(id * 2) + 1].Z	=color.Z;

		mDynZoneLights[id].mColor	=color;
	}


	void Decay(int id, int decayMS)
	{
	}


	public void Destroy(int id)
	{
		if(id < 0 || id >= MaxLights)
		{
			return;
		}

		mDynArray[id * 2].W	=0f;
		mInUse[id]			=false;

		mDynZoneLights.Remove(id);
	}
}*/

