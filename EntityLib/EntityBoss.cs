using System;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class EntityBoss
	{
		Dictionary<int, Entity>	mEnts	=new Dictionary<int, Entity>();


		public void AddEntity(Entity e)
		{
			int	hash	=e.GetHashCode();

			mEnts.Add(hash, e);
		}


		public void Update(UpdateTimer ut)
		{
			foreach(KeyValuePair<int, Entity> ent in mEnts)
			{
				ent.Value.Update(ut);
			}
		}


		public List<Component> GetEntityComponents(Type t)
		{
			List<Component>	ret	=new List<Component>();

			foreach(KeyValuePair<int, Entity> ents in mEnts)
			{
				Component	c	=ents.Value.GetComponent(t);

				if(c != null)
				{
					ret.Add(c);
				}
			}
			return	ret;
		}
	}
}
