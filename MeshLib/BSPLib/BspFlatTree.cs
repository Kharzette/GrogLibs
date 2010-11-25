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
		List<Brush>			mEmptySpace	=new List<Brush>();


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
//			Portalize();
			Map.Print("Portalization complete\n");
		}


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			mRoot.GetTriangles(verts, indexes, bCheckFlags);
		}


		internal void Portalize()
		{
			Bounds	rootBounds	=mRoot.mBounds;

			//create outside node brush volume
			Brush	outsideBrush	=new Brush();

			List<Face>	obFaces	=new List<Face>();
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
				obFaces.Add(f);
			}

			//clip brush faces against each other
			for(int i=0;i < 6;i++)
			{
				for(int j=0;j < 6;j++)
				{
					if(i==j)
					{
						continue;
					}
					obFaces[j].ClipByFace(obFaces[i], false, true);
				}
			}

			outsideBrush.AddFaces(obFaces);

			List<Brush>	inList	=new List<Brush>();

			inList.Add(outsideBrush);

			//merge the outside volume into the empty
			//space of the tree
			mRoot.MergeBrush(inList, mEmptySpace);

			//grab brush faces
			List<Face>	emptySpaceFaces	=new List<Face>();
			foreach(Brush b in mEmptySpace)
			{
				List<Face>	brushFaces	=b.GetFaces();

				//clone
				foreach(Face f in brushFaces)
				{
					emptySpaceFaces.Add(new Face(f));
				}
			}

			foreach(Face f in emptySpaceFaces)
			{
				Portal	port	=new Portal();

				port.mFace	=f;

				mRoot.FilterPortalBack(port);
				mRoot.FilterPortalFront(port);

				port.mFront.AddPortal(port);
				port.mBack.AddPortal(port);

				mPortals.Add(port);
			}

//			mRoot.GatherPortals(mPortals);
		}


		internal void GetPortalLines(List<Vector3> verts, List<UInt32> indexes)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;
			/*
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
			}*/

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
			/*
			foreach(Brush b in mEmptySpace)
			{
				b.GetTriangles(verts, indexes, false);
			}
			*/
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
