using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;
using Vortice.Mathematics;

//using DynLight	=MaterialLib.DynamicLights.DynLight;


namespace BSPZone;

public partial class Zone
{
	//just a list of which model indexes are triggers
	List<int>	mNonCollidingModels	=new List<int>();

	//face flag constants from core texinfos
	public const UInt32	MIRROR		=(1<<0);
	public const UInt32	FULLBRIGHT	=(1<<1);
	public const UInt32	SKY			=(1<<2);
	public const UInt32	EMITLIGHT	=(1<<3);
	public const UInt32	TRANSPARENT	=(1<<4);
	public const UInt32	GOURAUD		=(1<<5);
	public const UInt32	FLAT		=(1<<6);
	public const UInt32	CELSHADE	=(1<<7);
	public const UInt32	NO_LIGHTMAP	=(1<<15);


	public Vector3 GetPlayerStartPos(out float angle)
	{
		angle	=0f;

		foreach(ZoneEntity e in mEntities)
		{
			if(e.mData.ContainsKey("classname"))
			{
				if(e.mData["classname"] != "info_player_start")
				{
					continue;
				}
			}
			else
			{
				continue;
			}

			Vector3	ret	=Vector3.Zero;
			if(e.GetOrigin(out ret))
			{
				e.GetFloat("angle", out angle);
				return	ret;
			}
		}
		return	Vector3.Zero;
	}


	public List<ZoneEntity> GetEntitiesByTargetName(string targName)
	{
		//targetnames can have multiple entries
		string	[]targs	=targName.Split(' ');

		List<ZoneEntity>	ret	=new List<ZoneEntity>();
		foreach(ZoneEntity ze in mEntities)
		{
			if(ze.mData.ContainsKey("targetname"))
			{
				string	checkName	=ze.mData["targetname"];

				foreach(string targ in targs)					
				{
					if(targ == checkName)
					{
						ret.Add(ze);
					}
				}
			}
		}
		return	ret;
	}


	public List<ZoneEntity> GetEntitiesByTarget(string targ)
	{
		//targets can have multiple entries
		string	[]targs	=targ.Split(' ');

		List<ZoneEntity>	ret	=new List<ZoneEntity>();
		foreach(ZoneEntity ze in mEntities)
		{
			if(ze.mData.ContainsKey("target"))
			{
				string	checkName	=ze.mData["target"];

				foreach(string trg in targs)					
				{
					if(targ == checkName)
					{
						ret.Add(ze);
					}
				}
			}
		}
		return	ret;
	}


	public List<ZoneEntity> GetEntitiesTargetNameStartsWith(string targName)
	{
		List<ZoneEntity>	ret	=new List<ZoneEntity>();
		foreach(ZoneEntity ze in mEntities)
		{
			string	tName	=ze.GetTargetName();
			if(tName.StartsWith(targName))
			{
				ret.Add(ze);
			}
		}
		return	ret;
	}


	public List<ZoneEntity> GetEntities(string className)
	{
		List<ZoneEntity>	ret	=new List<ZoneEntity>();
		foreach(ZoneEntity ze in mEntities)
		{
			if(ze.mData.ContainsKey("classname"))
			{
				if(ze.mData["classname"] == className)
				{
					ret.Add(ze);
				}
			}
		}
		return	ret;
	}


	internal void AddEntity(ZoneEntity newEnt)
	{
		mEntities.Add(newEnt);
	}


	public List<ZoneEntity> GetEntitiesStartsWith(string startText)
	{
		List<ZoneEntity>	ret	=new List<ZoneEntity>();
		foreach(ZoneEntity ze in mEntities)
		{
			if(ze.mData.ContainsKey("classname"))
			{
				if(ze.mData["classname"].StartsWith(startText))
				{
					ret.Add(ze);
				}
			}
		}
		return	ret;
	}


	public bool GetRandomPointInsideModelEntity(ZoneEntity ent, Random rand, out Vector3 point)
	{
		string	idxStr	=ent.GetValue("Model");

		int	modIdx;
		if(!Int32.TryParse(idxStr, out modIdx))
		{
			point	=Vector3.Zero;
			return	false;
		}

		ZoneModel	zm	=mZoneModels[modIdx];
		BoundingBox	bb	=zm.mBounds;

		Matrix4x4	modXForm	=zm.mTransform;

		Vector3	min, max;
		Mathery.TransformCoordinate(bb.Max, ref modXForm, out max);
		Mathery.TransformCoordinate(bb.Min, ref modXForm, out min);

		bb	=new BoundingBox(min, max);

		int	iterations	=0;
		point			=Vector3.Zero;
		for(;iterations < 50;iterations++)
		{
			int	x	=rand.Next((int)bb.Min.X, (int)bb.Max.X);
			int	y	=rand.Next((int)bb.Min.Y, (int)bb.Max.Y);
			int	z	=rand.Next((int)bb.Min.Z, (int)bb.Max.Z);

			point	=new Vector3(x, y, z);

			Mathery.TransformCoordinate(point, ref zm.mInvertedTransform, out point);

			RayTrace	rt	=new RayTrace(point, point);

			rt.mCollision.mModelHit	=modIdx;

			if(TraceNodeTrigger(rt, point, point, zm.mRootNode))
			{
				Mathery.TransformCoordinate(point, ref zm.mTransform, out point);
				break;
			}
		}
		return	(iterations < 50);
	}


	//check these positions for LOS
	//fills the index list with indexes to positions in
	//line of sight
	public void	GetInLOS(Vector3 eyePos, List<Vector3> positions,
						ref List<int> losIndexes)
	{
		losIndexes.Clear();

		for(int i=0;i < positions.Count;i++)
		{
			Vector3	pos	=positions[i];

			if(IsVisibleFrom(eyePos, pos))
			{
				Collision	col;
				if(!TraceAll(null, null, eyePos, pos, out col))
				{
					losIndexes.Add(i);
				}
			}
		}
	}


	void BuildNonCollidingModelsList()
	{
		List<ZoneEntity>	trigs	=GetEntitiesStartsWith("trigger_");
		List<ZoneEntity>	regs	=GetEntitiesStartsWith("func_region");

		//combine
		trigs.AddRange(regs);

		foreach(ZoneEntity ze in trigs)
		{
			string	mod	=ze.GetValue("Model");
			if(mod == null || mod == "")
			{
				continue;
			}

			int	modIdx	=Convert.ToInt32(mod);

			mNonCollidingModels.Add(modIdx);
		}
	}
}