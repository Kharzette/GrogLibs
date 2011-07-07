using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace PathLib
{
	public class PathNode
	{
		//raw stuff for the grid
		public Vector3				mPosition;
		public List<PathConnection>	mConnections	=new List<PathConnection>();


		public void ConnectIfPassable(PathNode pn, BSPZone.Zone tree, float radius)
		{
			if(pn == null || tree == null)
			{
				return;
			}

			Vector3				impacto		=Vector3.Zero;
			BSPZone.ZonePlane	hitPlane	=new BSPZone.ZonePlane();
			if(!tree.CapsuleCollide(mPosition, pn.mPosition, radius, ref impacto, ref hitPlane))
			{
				PathConnection	pc	=new PathConnection();
				pc.mConnectedTo	=pn;
				pc.mDistance	=(pn.mPosition - mPosition).Length();
				mConnections.Add(pc);
			}
		}


		public void Read(BinaryReader br)
		{
			mPosition.X	=br.ReadSingle();
			mPosition.Y	=br.ReadSingle();
			mPosition.Z	=br.ReadSingle();

			int	cnt	=br.ReadInt32();
			for(int i=0;i < cnt;i++)
			{
				Vector3	nodePos	=Vector3.Zero;
				nodePos.X	=br.ReadSingle();
				nodePos.Y	=br.ReadSingle();
				nodePos.Z	=br.ReadSingle();

				PathConnection	pc	=new PathConnection();
				pc.mDistance	=br.ReadSingle();

				//construct a temporary pathnode
				pc.mConnectedTo	=new PathNode();
				pc.mConnectedTo.mPosition	=nodePos;

				mConnections.Add(pc);
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mPosition.X);
			bw.Write(mPosition.Y);
			bw.Write(mPosition.Z);

			//connections contain a reference
			//and we can't really save that
			//so write out the positions of
			//the connections and read will
			//fix it up
			bw.Write(mConnections.Count);
			foreach(PathConnection pc in mConnections)
			{
				bw.Write(pc.mConnectedTo.mPosition.X);
				bw.Write(pc.mConnectedTo.mPosition.Y);
				bw.Write(pc.mConnectedTo.mPosition.Z);
				bw.Write(pc.mDistance);
			}
		}


		public Vector3 GetPosition()
		{
			return	mPosition;
		}


		public float GetDistance(PathNode pn)
		{
			return	(pn.mPosition - mPosition).Length();
		}
	}
}
