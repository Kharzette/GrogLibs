﻿using System;
using System.Text;
using System.Collections.Generic;


namespace BSPCore;

internal class NodeCounter
{
	internal int	mNumGFXFaces;
	internal int	mNumSolidLeafs;
	internal int	mNumGFXLeafs;
	internal int	mNumGFXNodes;
	internal int	mNumGFXLeafFaces;
	internal int	mNumLeafClusters;

	List<Int32>	mVertIndexes	=new List<Int32>();


	internal int VertIndexListCount
	{
		get { return mVertIndexes.Count; }
	}


	internal void AddIndex(Int32 ind)
	{
		mVertIndexes.Add(ind);
	}


	internal Int32[] GetIndexArray()
	{
		return	mVertIndexes.ToArray();
	}
}