using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace MeshLib
{
	public class DrawCall
	{
		public int		mNumVerts;
		public int		mStartIndex;	//offsets
		public int		mPrimCount;		//num prims per call
		public Vector3	mSortPoint;


		void Write(BinaryWriter bw)
		{
			bw.Write(mNumVerts);
			bw.Write(mStartIndex);
			bw.Write(mPrimCount);

			bw.Write(mSortPoint.X);
			bw.Write(mSortPoint.Y);
			bw.Write(mSortPoint.Z);
		}


		void Read(BinaryReader br)
		{
			mNumVerts	=br.ReadInt32();
			mStartIndex	=br.ReadInt32();
			mPrimCount	=br.ReadInt32();

			mSortPoint.X	=br.ReadSingle();
			mSortPoint.Y	=br.ReadSingle();
			mSortPoint.Z	=br.ReadSingle();
		}


		internal static void WriteDrawCallArray(BinaryWriter bw, DrawCall []dcs)
		{
			bw.Write(dcs.Length);
			foreach(DrawCall dc in dcs)
			{
				dc.Write(bw);
			}
		}


		internal static void WriteDrawCallListArray(BinaryWriter bw, List<DrawCall> []dcs)
		{
			bw.Write(dcs.Length);
			foreach(List<DrawCall> dcl in dcs)
			{
				bw.Write(dcl.Count);
				foreach(DrawCall dc in dcl)
				{
					dc.Write(bw);
				}
			}
		}


		internal static DrawCall []ReadDrawCallArray(BinaryReader br)
		{
			int	dclen	=br.ReadInt32();
			if(dclen <= 0)
			{
				return	null;
			}

			DrawCall	[]ret	=new DrawCall[dclen];
			for(int i=0;i < dclen;i++)
			{
				ret[i]	=new DrawCall();
				ret[i].Read(br);
			}
			return	ret;
		}


		internal static List<DrawCall> []ReadDrawCallListArray(BinaryReader br)
		{
			int	dclen	=br.ReadInt32();
			if(dclen <= 0)
			{
				return	null;
			}

			List<DrawCall>	[]ret	=new List<DrawCall>[dclen];
			for(int i=0;i < dclen;i++)
			{
				int	listCount	=br.ReadInt32();

				ret[i]	=new List<DrawCall>();
				for(int j=0;j < listCount;j++)
				{
					DrawCall	dc	=new DrawCall();
					dc.Read(br);

					ret[i].Add(dc);
				}
			}
			return	ret;
		}
	}
}
