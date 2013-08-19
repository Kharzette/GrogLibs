using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPZone
{
	public class DynamicLights
	{
		//simple vector4 tex for passing dyn lights to shaders
		Texture2D	mDynLights;

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
			//dynamic lights stuffed into a rendertarget
			mDynLights	=new Texture2D(gd, MaxLights * 2, 1, false, SurfaceFormat.Vector4);

			mDynArray	=new Vector4[MaxLights * 2];
			mInUse		=new bool[MaxLights];

			mFX	=fx;
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
			//clear device textures, annoying
			for(int i=0;i < 16;i++)
			{
				gd.Textures[i]	=null;
			}
			mDynLights.SetData<Vector4>(mDynArray);
		}


		public void SetParameter()
		{
			mFX.Parameters["mDynLights"].SetValue(mDynLights);
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
			mDynArray[id * 2].W	=0f;
			mInUse[id]			=false;

			mDynZoneLights.Remove(id);
		}
	}
}
