using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UtilityLib
{
	public class Pool<T>
	{
		private readonly List<T> items = new List<T>();
		private readonly Queue<T> freeItems = new Queue<T>();
		
		private readonly Func<T>	createItemAction;
		private readonly Func<T, T> createItemAction2;
		
		public Pool(Func<T> createItemAction)
		{
			this.createItemAction = createItemAction;
		}
		
		public Pool(Func<T, T> createItemAction2)
		{
			this.createItemAction2 = createItemAction2;
		}
		
		public void FlagFreeItem(T item)
		{
			freeItems.Enqueue(item);
		}
		
		public T GetFreeItem()
		{
			if (freeItems.Count == 0)
			{
				T item = createItemAction();
				items.Add(item);
				
				return item;
			}
			
			return freeItems.Dequeue();
		}

		public T GetFreeItem(T copy)
		{
			if (freeItems.Count == 0)
			{
				T item = createItemAction2(copy);
				items.Add(item);
				
				return item;
			}
			
			return freeItems.Dequeue();
		}

		public List<T> Items
		{
			get { return items; }
		}
		
		public void Clear()
		{
			items.Clear();
			freeItems.Clear();
		}
	}
}