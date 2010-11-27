using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSPLib
{
	public class TexInfoPool
	{
		List<TexInfo>	mTexInfos	=new List<TexInfo>();


		internal int Add(TexInfo ti)
		{
			foreach(TexInfo tex in mTexInfos)
			{
				if(tex.Compare(ti))
				{
					return	mTexInfos.IndexOf(tex);
				}
			}

			mTexInfos.Add(ti);

			return	mTexInfos.IndexOf(ti);
		}
	}
}
