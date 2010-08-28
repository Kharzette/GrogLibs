using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	//intermediate stage, points back to brush
	public class PortalFace
	{
		public Face		mFace;
		public Brush	mBrush;
	}


	public class Portal
	{
		PortalFace			mPortalFace;
		List<PortalLink>	mLinks	=new List<PortalLink>();


		public Portal(PortalFace me, List<PortalFace> portList)
		{
			mPortalFace	=me;

			List<Edge>	myEdges	=new List<Edge>();
			me.mFace.GetEdges(myEdges);

			//walk each edge
			foreach(Edge e in myEdges)
			{
				foreach(PortalFace pf in portList)
				{
					if(pf.mFace == me.mFace)
					{
						continue;
					}

					List<Edge>	fedges	=new List<Edge>();
					pf.mFace.GetEdges(fedges);

					foreach(Edge fe in fedges)
					{
						if(e.Overlaps(fe))
						{
							PortalLink	pl	=new PortalLink();
							pl.mEdge	=e;
							pl.mConnected.Add(pf);

							mLinks.Add(pl);
						}
					}
				}
			}
		}


		internal void BevelObtuse(float hullWidth)
		{
			foreach(PortalLink pl in mLinks)
			{
				foreach(PortalFace con in pl.mConnected)
				{
					Face	cf	=con.mFace;
					float	d	=cf.AngleBetween(mPortalFace.mFace);

					if(d < -Plane.EPSILON)
					{
						Plane	bev			=new Plane();
						Plane	myPlane		=mPortalFace.mFace.GetPlane();
						Plane	conPlane	=cf.GetPlane();
						
						bev.mNormal	=conPlane.mNormal + myPlane.mNormal;
						bev.mNormal.Normalize();

						bev.mDistance	=Vector3.Dot(bev.mNormal, cf.GetFirstSharedVert(mPortalFace.mFace));
						bev.mDistance	-=hullWidth;

						if(!mPortalFace.mBrush.ContainsPlane(bev))
						{
							mPortalFace.mBrush.AddPlane(bev);
							mPortalFace.mBrush.SealFaces();
						}
					}
				}
			}
		}
	}
}
