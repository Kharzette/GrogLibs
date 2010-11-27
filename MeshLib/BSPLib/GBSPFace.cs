using System;
using System.Collections.Generic;
using System.Text;

namespace BSPLib
{
	public class GBSPFace
	{
		public GBSPFace		Next;
		public GBSPFace		Original;
		public GBSPPoly		Poly;
		public Int32		[]Contents	=new Int32[2];
		public Int32		TexInfo;
		public Int32		PlaneNum;
		public Int32		PlaneSide;

		public Int32		Entity;					// Originating entity

		public byte			Visible;			

		//For GFX file saving
		public Int32		OutputNum;	

		public Int32		IndexVerts;
		public Int32		FirstIndexVert;
		public Int32		NumIndexVerts;

		public GBSPPortal	Portal;
		public GBSPFace		[]Split	=new GBSPFace[2];
		public GBSPFace		Merged;
	}
}
