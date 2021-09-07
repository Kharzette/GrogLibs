using System;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class Entity
	{
		List<Component>	mComponents	=new List<Component>();

		public bool			mbWantUpdate;
		public EntityBoss	mBoss;


		public Entity(bool bWantUpdate, EntityBoss eb)
		{
			mbWantUpdate	=bWantUpdate;
			mBoss			=eb;
		}


		public void AddComponent(Component c)
		{
			Debug.Assert(!mComponents.Contains(c));

			mComponents.Add(c);
		}


		public List<Component> GetComponents()
		{
			return	mComponents;
		}


		public List<Component> GetComponents(Type t)
		{
			List<Component>	comps	=new List<Component>();
			foreach(Component c in mComponents)
			{
				if(c.GetType() == t)
				{
					comps.Add(c);
				}
			}
			return	comps;
		}


		public Component GetComponent(Type t)
		{
			foreach(Component c in mComponents)
			{
				if(c.GetType() == t)
				{
					return	c;
				}
			}
			return	null;
		}


		public void Update(UpdateTimer ut)
		{
			if(!mbWantUpdate)
			{
				return;
			}

			foreach(Component c in mComponents)
			{
				c.Update(ut);
			}
		}
	}
}
