using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UtilityLib
{
	public class TSPool<T>
	{
		private readonly List<T> items = new List<T>();
		private readonly Queue<T> freeItems = new Queue<T>();
		
		private readonly Func<T>	createItemAction;
		private readonly Func<T, T> createItemAction2;
		
		public TSPool(Func<T> createItemAction)
		{
			this.createItemAction = createItemAction;
		}
		
		public TSPool(Func<T, T> createItemAction2)
		{
			this.createItemAction2 = createItemAction2;
		}
		
		public void FlagFreeItem(T item)
		{
			lock(freeItems)
			{
				freeItems.Enqueue(item);
			}
		}
		
		public T GetFreeItem()
		{
			bool	bFreeZero	=false;
			T		deQueued	=default(T);
			lock(freeItems)
			{
				bFreeZero	=(freeItems.Count == 0);
				if(!bFreeZero)
				{
					deQueued	=freeItems.Dequeue();
				}
			}

			if(bFreeZero)
			{
				T item = createItemAction();

				lock(items)
				{
					items.Add(item);
				}
				
				return item;
			}
			else
			{
				return	deQueued;
			}
		}

		public T GetFreeItem(T copy)
		{
			bool	bFreeZero	=false;
			T		deQueued	=default(T);
			lock(freeItems)
			{
				bFreeZero	=(freeItems.Count == 0);

				if(!bFreeZero)
				{
					deQueued	=freeItems.Dequeue();
				}
			}

			if(bFreeZero)
			{
				T item = createItemAction2(copy);

				lock(items)
				{
					items.Add(item);
				}
				
				return item;
			}
			else
			{
				return	deQueued;
			}
		}

//		public List<T> Items
//		{
//			get { return items; }
//		}
		
		public void Clear()
		{
			lock(items)
			{
				items.Clear();
			}
			lock(freeItems)
			{
				freeItems.Clear();
			}
		}
	}
}