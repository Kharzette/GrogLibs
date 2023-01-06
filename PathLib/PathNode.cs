using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UtilityLib;


namespace PathLib
{
	internal class PathNode
	{
		//raw stuff for grid or navmesh
		internal ConvexPoly	mPoly;	//ref, multiple nodes might point to it
		internal Vector3	mPoint;
		internal float		mHScorePenalty;	//use to discourage use

		internal List<PathConnection>	mConnections	=new List<PathConnection>();


		internal PathNode(ConvexPoly cp, Vector3 point)
		{
			mPoly	=cp;
			mPoint	=point;
		}


		internal PathNode(BinaryReader br)
		{
			mPoint			=FileUtil.ReadVector3(br);
			mHScorePenalty	=br.ReadSingle();

			mPoly	=new ConvexPoly(br);
		}


		internal float DistanceBetweenNodes(PathNode pn2)
		{
			return	Vector3.Distance(mPoint, pn2.mPoint);
		}


		internal void ReadConnections(BinaryReader br, List<PathNode> nodery)
		{
			int	numCon	=br.ReadInt32();
			for(int i=0;i < numCon;i++)
			{
				PathConnection	pc	=new PathConnection();

				int	nIdx	=br.ReadInt32();

				pc.mConnectedTo		=nodery[nIdx];
				pc.mDistanceBetween	=br.ReadSingle();
				pc.mbUseEdge		=br.ReadBoolean();
				pc.mbPassable		=br.ReadBoolean();
				pc.mEdgeBetween		=FileUtil.ReadVector3(br);

				mConnections.Add(pc);
			}
		}


		internal void WriteConnections(BinaryWriter bw, List<PathNode> nodery)
		{
			bw.Write(mConnections.Count);
			for(int i=0;i < mConnections.Count;i++)
			{
				PathConnection	pc	=mConnections[i];

				bw.Write(nodery.IndexOf(pc.mConnectedTo));
				bw.Write(pc.mDistanceBetween);
				bw.Write(pc.mbUseEdge);
				bw.Write(pc.mbPassable);
				FileUtil.WriteVector3(bw, pc.mEdgeBetween);
			}
		}


		internal void Write(BinaryWriter bw)
		{
			FileUtil.WriteVector3(bw, mPoint);
			bw.Write(mHScorePenalty);

			mPoly.Write(bw);
		}
	}
}
