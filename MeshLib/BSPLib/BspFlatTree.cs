using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class BspFlatTree
	{
		BspFlatNode		mRoot, mOutsideNode;
		List<Portal>	mPortals	=new List<Portal>();

		//debug draw
		List<BspFlatNode>	mFlooded	=new List<BspFlatNode>();


		internal BspFlatTree(List<Brush> brushList)
		{
			mRoot	=new BspFlatNode();
			mRoot.BuildTree(brushList);
			mRoot.Bound();

			Map.Print("Flat tree build complete\n");

			mOutsideNode				=new BspFlatNode();
			mOutsideNode.mbLeaf			=true;

			//this might be a bad idea commenting this out
//			mOutsideNode.mBrushContents	=Brush.CONTENTS_SOLID;

			Map.Print("Portalizing...\n");
			Portalize();
			Map.Print("Portalization complete\n");
		}


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			mRoot.GetTriangles(verts, indexes, bCheckFlags);
		}


		internal void Portalize()
		{
			Bounds	rootBounds	=mRoot.mBounds;

			//create outside node portals
			List<Portal>	outPorts		=new List<Portal>();
			List<Portal>	outChoppedPorts	=new List<Portal>();
			for(int i=0;i < 6;i++)
			{
				Plane	p;
				p.mNormal	=UtilityLib.Mathery.AxialNormals[i];

				if(i < 3)
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMaxs, p.mNormal) + 128.0f;
				}
				else
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMins, p.mNormal) + 128.0f;
				}

				Face	f		=new Face(p, null);
				Portal	port	=new Portal();

				port.mFace			=f;
				port.mFront			=mOutsideNode;

				outPorts.Add(port);
			}

			//clip these against each other
			for(int i=0;i < 6;i++)
			{
				for(int j=0;j < 6;j++)
				{
					if(i==j)
					{
						continue;
					}
					outPorts[j].mFace.ClipByFace(outPorts[i].mFace, false, true);
				}
			}

			//grab all the non leaf nodes
			List<Plane>	planes	=new List<Plane>();

			mRoot.GatherNonLeafPlanes(planes);

			//slice up the outside portals by all the other planes
			foreach(Portal outPort in outPorts)
			{
				List<Portal>	pieces	=new List<Portal>();
				pieces.Add(outPort);
				for(int j=0;j < planes.Count;j++)
				{
					Plane	split	=planes[j];
					if(outPort.mFace.GetPlane().IsCoPlanar(split))
					{
						continue;
					}

					for(int i=0;i < pieces.Count;i++)
					{
						Portal	front, back;
						if(pieces[i].Split(split, out front, out back))
						{
							if(front != null)
							{
								pieces.Add(front);
							}
							if(back != null)
							{
								pieces.Add(back);
							}

							pieces.RemoveAt(i);
							i--;
						}
					}
				}
				outChoppedPorts.AddRange(pieces);
			}

			//create portals from non leaf planes
			foreach(Plane plane in planes)
			{
				Portal	p	=new Portal();
				p.mFace		=new Face(plane, null);

				//slice portal by the outside nodes
				for(int i=0;i < 6;i++)
				{
					p.mFace.ClipByFace(outPorts[i].mFace, false, true);
				}

				Debug.Assert(!p.mFace.IsTiny());

				List<Portal>	pieces	=new List<Portal>();
				pieces.Add(p);

				//slice up the portals by all the other planes
				for(int j=0;j < planes.Count;j++)
				{
					Plane	split	=planes[j];
					if(plane.IsCoPlanar(split))
					{
						continue;
					}

					for(int i=0;i < pieces.Count;i++)
					{
						Portal	front, back;
						if(pieces[i].Split(split, out front, out back))
						{
							if(front != null)
							{
								pieces.Add(front);
							}
							if(back != null)
							{
								pieces.Add(back);
							}

							pieces.RemoveAt(i);
							i--;
						}
					}
				}
				mPortals.AddRange(pieces);
			}

			//filter the portals into leaves
			foreach(Portal port in mPortals)
			{
				mRoot.FilterPortalFront(port);
				mRoot.FilterPortalBack(port);
				Debug.Assert(port.mFront != null);
				Debug.Assert(port.mBack != null);
			}

			//filter outer node portals
			foreach(Portal port in outChoppedPorts)
			{
				mRoot.FilterPortalBack(port);
				Debug.Assert(port.mFront != null);
				Debug.Assert(port.mBack != null);
			}

			mPortals.AddRange(outChoppedPorts);

			//add portals to nodes
			foreach(Portal port in mPortals)
			{
				port.mFront.AddPortal(port);
				port.mBack.AddPortal(port);
			}
		}


		internal void GetPortalLines(List<Vector3> verts, List<UInt32> indexes)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			foreach(Portal p in mPortals)
			{
				if((p.mBack.mBrushContents & Brush.CONTENTS_SOLID) == 0)
				{
//					continue;
				}
				if((p.mFront.mBrushContents & Brush.CONTENTS_SOLID) == 0)
				{
//					continue;
				}

				Vector3	frontCentroid	=p.mFace.GetCentroid();

				foreach(Face f in p.mBack.mFaces)
				{
					Vector3	backCentroid	=f.GetCentroid();

					verts.Add(frontCentroid);
					verts.Add(backCentroid);

					indexes.Add(offset++);
					indexes.Add(offset++);
				}
			}

			foreach(BspFlatNode node in mFlooded)
			{
				foreach(Face f in node.mFaces)
				{
					Vector3	centroid	=f.GetCentroid();

					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitX * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitY * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitZ * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitX * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitY * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitZ * 10.0f);

					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
				}
			}
		}


		internal void GetPortalTriangles(List<Vector3> verts, List<UInt32> indexes)
		{
			foreach(Portal p in mPortals)
			{
				if(!p.mBack.mbLeaf)
				{
//					continue;
				}
				if(p.mFront.mbLeaf)
				{
//					continue;
				}
				if((p.mBack.mBrushContents & Brush.CONTENTS_SOLID) == 0)
				{
//					continue;
				}
				if((p.mFront.mBrushContents & Brush.CONTENTS_SOLID) == 0)
				{
//					continue;
				}
				p.mFace.GetTriangles(verts, indexes);
			}
		}


		internal BspFlatNode GetNodeLandedIn(Vector3 org)
		{
			return	mRoot.GetNodeLandedIn(org);
		}


		internal bool CheckForLeak(Dictionary<BspFlatNode, List<Entity>> nodeEnts)
		{
			foreach(KeyValuePair<BspFlatNode, List<Entity>> nodeEnt in nodeEnts)
			{
				Debug.Assert(nodeEnt.Key.mBrushContents == 0);
				Debug.Assert(!nodeEnt.Key.mbLeaf);

				nodeEnt.Key.FloodFillEmpty(mFlooded);
				if(mFlooded.Contains(mOutsideNode))
				{
					Vector3	org	=Vector3.Zero;
					nodeEnt.Value[0].GetOrigin(out org);
					Map.Print("Leak found near: " + org + "!!!\n");
				}
				break;
			}
			Map.Print("No leaks found.\n");
			return	false;
		}
	}
}
