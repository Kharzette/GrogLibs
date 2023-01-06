using System;
using System.Numerics;
using System.Collections.Generic;
using UtilityLib;


namespace EntityLib
{
	public class EntityBoss
	{
		List<Entity>	mEnts		=new List<Entity>();
		List<Entity>	mUpdateEnts	=new List<Entity>();


		public void AddEntity(Entity e)
		{
			mEnts.Add(e);

			if(e.mbWantUpdate)
			{
				mUpdateEnts.Add(e);
			}
		}


		public void Update(UpdateTimer ut)
		{
			foreach(Entity ent in mUpdateEnts)
			{
				ent.Update(ut);
			}
		}


		public List<Component> GetEntityComponents(Type t)
		{
			List<Component>	ret	=new List<Component>();

			foreach(Entity ents in mEnts)
			{
				Component	c	=ents.GetComponent(t);

				if(c != null)
				{
					ret.Add(c);
				}
			}
			return	ret;
		}


		public void FreeAll()
		{
			mUpdateEnts.Clear();
			mEnts.Clear();
		}
	}
}
