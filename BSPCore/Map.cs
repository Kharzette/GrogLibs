using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Diagnostics;


namespace BSPCore
{
	public partial class Map
	{
		//planes
		PlanePool	mPlanePool	=new PlanePool();

		//gfx data
		GFXModel		[]mGFXModels;
		GFXNode			[]mGFXNodes;
		GFXLeaf			[]mGFXLeafs;
		GFXCluster		[]mGFXClusters;
		GFXArea			[]mGFXAreas;
		GFXAreaPortal	[]mGFXAreaPortals;
		GFXPlane		[]mGFXPlanes;
		GFXFace			[]mGFXFaces;
		Int32			[]mGFXLeafFaces;
		GFXLeafSide		[]mGFXLeafSides;
		Vector3			[]mGFXVerts;
		Int32			[]mGFXVertIndexes;
		Vector3			[]mGFXRGBVerts;
		GFXTexInfo		[]mGFXTexInfos;
		MapEntity		[]mGFXEntities;
		byte			[]mGFXLightData;
		byte			[]mGFXVisData;
		byte			[]mGFXMaterialVisData;
		int				mLightMapGridSize;


		public Map() { }


		internal void UpdateNumPortals(int numPortals)
		{
			CoreEvents.FireNumPortalsChangedEvent(numPortals, null);
		}


		static internal void Print(string str)
		{
			CoreEvents.Print(str);
		}
	}
}
