using System;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class EntityBoss
	{
		List<Entity>	mEnts	=new List<Entity>();


		public void AddEntity(Entity e)
		{
			mEnts.Add(e);
		}


		public void Update(UpdateTimer ut)
		{
			foreach(Entity ent in mEnts)
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
	}
}
