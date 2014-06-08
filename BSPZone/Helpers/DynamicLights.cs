using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using UtilityLib;

//ambiguous stuff
using Device	=SharpDX.Direct3D11.Device;
using Resource	=SharpDX.Direct3D11.Resource;


namespace BSPZone
{
	public class DynamicLights
	{
		//simple vector4 tex for passing dyn lights to shaders
		Texture1D			mDynLights;
		ShaderResourceView	mDynSRV;

		//effect to set the dynamic light parameter on
		//TODO: might need multiple
		Effect	mFX;

		//active list of dynamic lights
		Dictionary<int, Zone.ZoneLight>	mDynZoneLights	=new Dictionary<int, Zone.ZoneLight>();

		Vector4	[]mDynArray;
		bool	[]mInUse;

		const int	MaxLights	=16;


		public DynamicLights(GraphicsDevice gd, Effect fx)
		{
			mDynArray	=new Vector4[MaxLights * 2];
			mInUse		=new bool[MaxLights];

			mFX	=fx;

			SampleDescription	sampDesc	=new SampleDescription();
			sampDesc.Count		=1;
			sampDesc.Quality	=0;

			Resource	res	=null;
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
			mDynSRV		=new ShaderResourceView(gd.GD, res);
		}


		public void FreeAll()
		{
			mDynSRV.Dispose();
			mDynLights.Dispose();
			mFX.Dispose();
		}


		public Dictionary<int, Zone.ZoneLight> GetZoneLights()
		{
			return	mDynZoneLights;
		}


		public bool CreateDynamicLight(Vector3 pos, Vector3 color, float strength, out int id)
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

				Zone.ZoneLight	zl	=new Zone.ZoneLight();
				zl.mStrength		=strength;
				zl.mPosition		=pos;
				zl.mColor			=color;
				zl.mbOn				=true;

				mDynZoneLights.Add(id, zl);

				return	true;
			}
			id		=-1;
			return	false;
		}


		public void Update(int msDelta, GraphicsDevice gd)
		{
			DataStream	ds;

			gd.DC.MapSubresource(mDynLights, 0, MapMode.WriteDiscard,
				SharpDX.Direct3D11.MapFlags.None, out ds);

			//clear device textures, annoying
//			for(int i=0;i < 16;i++)
//			{
//				gd.Textures[i]	=null;
//			}

			for(int i=0;i < (MaxLights * 2);i++)
			{
				ds.Write<Vector4>(mDynArray[i]);
			}

			gd.DC.UnmapSubresource(mDynLights, 0);
		}


		public void SetParameter()
		{
			mFX.GetVariableByName("mDynLights").AsShaderResource().SetResource(mDynSRV);
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
	}
}
