using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpDX;


namespace MeshLib
{
	public class DrawCall
	{
		public int		mCount;			//vert or index count depending on draw
		public int		mStartIndex;	//offsets
		public int		mMaterialID;
		public Vector3	mSortPoint;


		void Write(BinaryWriter bw)
		{
			bw.Write(mCount);
			bw.Write(mStartIndex);

			bw.Write(mSortPoint.X);
			bw.Write(mSortPoint.Y);
			bw.Write(mSortPoint.Z);
		}


		void Read(BinaryReader br)
		{
			mCount			=br.ReadInt32();
			mStartIndex		=br.ReadInt32();

			mSortPoint.X	=br.ReadSingle();
			mSortPoint.Y	=br.ReadSingle();
			mSortPoint.Z	=br.ReadSingle();
		}


		internal static void WriteDrawCallArray(BinaryWriter bw, DrawCall []dcs)
		{
			if(dcs == null)
			{
				bw.Write(0);
				return;
			}

			bw.Write(dcs.Length);
			foreach(DrawCall dc in dcs)
			{
				dc.Write(bw);
			}
		}


		internal static void WriteDrawCallAlphaDict(BinaryWriter bw,
			Dictionary<int, List<List<DrawCall>>> dict)
		{
			if(dict == null)
			{
				bw.Write(0);
				return;
			}

			bw.Write(dict.Count);
			foreach(KeyValuePair<int, List<List<DrawCall>>> modelDraws in dict)
			{
				bw.Write(modelDraws.Key);	//model index

				bw.Write(modelDraws.Value.Count);	//num materials for this model

				foreach(List<DrawCall> matDraws in modelDraws.Value)
				{
					bw.Write(matDraws.Count);	//per plane count

					foreach(DrawCall dc in matDraws)
					{
						dc.Write(bw);
					}
				}
			}
		}


		internal static void WriteDrawCallListArray(BinaryWriter bw, List<DrawCall> []dcs)
		{
			if(dcs == null)
			{
				bw.Write(0);
				return;
			}

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


		internal static void WriteDrawCallDict(BinaryWriter bw, Dictionary<int, List<DrawCall>> dcs)
		{
			if(dcs == null)
			{
				bw.Write(0);
				return;
			}

			bw.Write(dcs.Count);

			foreach(KeyValuePair<int, List<DrawCall>> modelDrawList in dcs)
			{
				bw.Write(modelDrawList.Key);

				bw.Write(modelDrawList.Value.Count);
				foreach(DrawCall dc in modelDrawList.Value)
				{
					dc.Write(bw);
				}
			}
		}


		internal static Dictionary<int, List<DrawCall>> ReadDrawCallDict(BinaryReader br)
		{
			int	dclen	=br.ReadInt32();
			if(dclen <= 0)
			{
				return	null;	//empty
			}

			Dictionary<int, List<DrawCall>>	ret	=new Dictionary<int, List<DrawCall>>();

			for(int i=0;i < dclen;i++)
			{
				int	modelNum	=br.ReadInt32();
				int	numDraws	=br.ReadInt32();

				List<DrawCall>	materialDraws	=new List<DrawCall>();
				for(int j=0;j < numDraws;j++)
				{
					DrawCall	dc	=new DrawCall();

					dc.Read(br);

					materialDraws.Add(dc);
				}

				ret.Add(modelNum, materialDraws);
			}
			return	ret;
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


		internal static Dictionary<int, List<List<MeshLib.DrawCall>>> ReadDrawCallAlphaDict(BinaryReader br)
		{
			int	numModels	=br.ReadInt32();
			if(numModels <= 0)
			{
				return	null;
			}

			Dictionary<int, List<List<MeshLib.DrawCall>>>	ret	=new Dictionary<int, List<List<DrawCall>>>();

			for(int i=0;i < numModels;i++)
			{
				int	modelIdx	=br.ReadInt32();

				ret.Add(modelIdx, new List<List<DrawCall>>());

				int	matCount	=br.ReadInt32();

				for(int j=0;j < matCount;j++)
				{
					ret[i].Add(new List<DrawCall>());

					int	perPlane	=br.ReadInt32();

					for(int k=0;k < perPlane;k++)
					{
						DrawCall	dc	=new DrawCall();
						dc.Read(br);

						ret[i][j].Add(dc);
					}
				}
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
