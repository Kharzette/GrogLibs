using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPCore
{
	public class ClipPools
	{
		public Pool<Vector3 []>	mClipVecs;
		public Pool<float []>	mClipFloats;
		public Pool<Int32 []>	mClipInts;

		//verts for portal windings
		public Pool<Vector3 []>	mWindings3;
		public Pool<Vector3 []>	mWindings4;
		public Pool<Vector3 []>	mWindings5;
		public Pool<Vector3 []>	mWindings6;
		public Pool<Vector3 []>	mWindings7;
		public Pool<Vector3 []>	mWindings8;
		public Pool<Vector3 []>	mWindings9;
		public Pool<Vector3 []>	mWindings10;
		public Pool<Vector3 []>	mWindings11;
		public Pool<Vector3 []>	mWindings12;
		public Pool<Vector3 []>	mWindings13;
		public Pool<Vector3 []>	mWindings14;
		public Pool<Vector3 []>	mWindings15;
		public Pool<Vector3 []>	mWindings16;
		public Pool<Vector3 []>	mWindings17;
		public Pool<Vector3 []>	mWindings18;
		public Pool<Vector3 []>	mWindings19;
		public Pool<Vector3 []>	mWindings20;
		public Pool<Vector3 []>	mWindings21;
		public Pool<Vector3 []>	mWindings22;
		public Pool<Vector3 []>	mWindings23;
		public Pool<Vector3 []>	mWindings24;
		public Pool<Vector3 []>	mWindings25;
		public Pool<Vector3 []>	mWindings26;
		public Pool<Vector3 []>	mWindings27;
		public Pool<Vector3 []>	mWindings28;
		public Pool<Vector3 []>	mWindings29;
		public Pool<Vector3 []>	mWindings30;
		public Pool<Vector3 []>	mWindings31;
		public Pool<Vector3 []>	mWindings32;

		internal const Int32	ClipArraySize	=32;


		public ClipPools()
		{
			mClipFloats	=new Pool<float []>(() => new float[ClipArraySize]);
			mClipInts	=new Pool<Int32 []>(() => new Int32[ClipArraySize]);
			mClipVecs	=new Pool<Vector3[]>(() => new Vector3[ClipArraySize]);

			mWindings3	=new Pool<Vector3[]>(() => new Vector3[3]);
			mWindings4	=new Pool<Vector3[]>(() => new Vector3[4]);
			mWindings5	=new Pool<Vector3[]>(() => new Vector3[5]);
			mWindings6	=new Pool<Vector3[]>(() => new Vector3[6]);
			mWindings7	=new Pool<Vector3[]>(() => new Vector3[7]);
			mWindings8	=new Pool<Vector3[]>(() => new Vector3[8]);
			mWindings9	=new Pool<Vector3[]>(() => new Vector3[9]);
			mWindings10	=new Pool<Vector3[]>(() => new Vector3[10]);
			mWindings11	=new Pool<Vector3[]>(() => new Vector3[11]);
			mWindings12	=new Pool<Vector3[]>(() => new Vector3[12]);
			mWindings13	=new Pool<Vector3[]>(() => new Vector3[13]);
			mWindings14	=new Pool<Vector3[]>(() => new Vector3[14]);
			mWindings15	=new Pool<Vector3[]>(() => new Vector3[15]);
			mWindings16	=new Pool<Vector3[]>(() => new Vector3[16]);
			mWindings17	=new Pool<Vector3[]>(() => new Vector3[17]);
			mWindings18	=new Pool<Vector3[]>(() => new Vector3[18]);
			mWindings19	=new Pool<Vector3[]>(() => new Vector3[19]);
			mWindings20	=new Pool<Vector3[]>(() => new Vector3[20]);
			mWindings21	=new Pool<Vector3[]>(() => new Vector3[21]);
			mWindings22	=new Pool<Vector3[]>(() => new Vector3[22]);
			mWindings23	=new Pool<Vector3[]>(() => new Vector3[23]);
			mWindings24	=new Pool<Vector3[]>(() => new Vector3[24]);
			mWindings25	=new Pool<Vector3[]>(() => new Vector3[25]);
			mWindings26	=new Pool<Vector3[]>(() => new Vector3[26]);
			mWindings27	=new Pool<Vector3[]>(() => new Vector3[27]);
			mWindings28	=new Pool<Vector3[]>(() => new Vector3[28]);
			mWindings29	=new Pool<Vector3[]>(() => new Vector3[29]);
			mWindings30	=new Pool<Vector3[]>(() => new Vector3[30]);
			mWindings31	=new Pool<Vector3[]>(() => new Vector3[31]);
			mWindings32	=new Pool<Vector3[]>(() => new Vector3[32]);
		}


		public void FreeVerts(Vector3 []verts)
		{
			if(verts == null)
			{
				return;
			}

			switch(verts.Length)
			{
				case	3:
					mWindings3.FlagFreeItem(verts);
					break;
				case	4:
					mWindings4.FlagFreeItem(verts);
					break;
				case	5:
					mWindings5.FlagFreeItem(verts);
					break;
				case	6:
					mWindings6.FlagFreeItem(verts);
					break;
				case	7:
					mWindings7.FlagFreeItem(verts);
					break;
				case	8:
					mWindings8.FlagFreeItem(verts);
					break;
				case	9:
					mWindings9.FlagFreeItem(verts);
					break;
				case	10:
					mWindings10.FlagFreeItem(verts);
					break;
				case	11:
					mWindings11.FlagFreeItem(verts);
					break;
				case	12:
					mWindings12.FlagFreeItem(verts);
					break;
				case	13:
					mWindings13.FlagFreeItem(verts);
					break;
				case	14:
					mWindings14.FlagFreeItem(verts);
					break;
				case	15:
					mWindings15.FlagFreeItem(verts);
					break;
				case	16:
					mWindings16.FlagFreeItem(verts);
					break;
				case	17:
					mWindings17.FlagFreeItem(verts);
					break;
				case	18:
					mWindings18.FlagFreeItem(verts);
					break;
				case	19:
					mWindings19.FlagFreeItem(verts);
					break;
				case	20:
					mWindings20.FlagFreeItem(verts);
					break;
				case	21:
					mWindings21.FlagFreeItem(verts);
					break;
				case	22:
					mWindings22.FlagFreeItem(verts);
					break;
				case	23:
					mWindings23.FlagFreeItem(verts);
					break;
				case	24:
					mWindings24.FlagFreeItem(verts);
					break;
				case	25:
					mWindings25.FlagFreeItem(verts);
					break;
				case	26:
					mWindings26.FlagFreeItem(verts);
					break;
				case	27:
					mWindings27.FlagFreeItem(verts);
					break;
				case	28:
					mWindings28.FlagFreeItem(verts);
					break;
				case	29:
					mWindings29.FlagFreeItem(verts);
					break;
				case	30:
					mWindings30.FlagFreeItem(verts);
					break;
				case	31:
					mWindings31.FlagFreeItem(verts);
					break;
				case	32:
					mWindings32.FlagFreeItem(verts);
					break;
			}
		}


		public Vector3 []DupeVerts(Vector3 []verts)
		{
			return	DupeVerts(verts, verts.Length);
		}


		public Vector3 []DupeVerts(Vector3 []verts, int size)
		{
			Vector3	[]ret	=null;

			switch(size)
			{
				case	3:
					ret	=mWindings3.GetFreeItem();
					break;
				case	4:
					ret	=mWindings4.GetFreeItem();
					break;
				case	5:
					ret	=mWindings5.GetFreeItem();
					break;
				case	6:
					ret	=mWindings6.GetFreeItem();
					break;
				case	7:
					ret	=mWindings7.GetFreeItem();
					break;
				case	8:
					ret	=mWindings8.GetFreeItem();
					break;
				case	9:
					ret	=mWindings9.GetFreeItem();
					break;
				case	10:
					ret	=mWindings10.GetFreeItem();
					break;
				case	11:
					ret	=mWindings11.GetFreeItem();
					break;
				case	12:
					ret	=mWindings12.GetFreeItem();
					break;
				case	13:
					ret	=mWindings13.GetFreeItem();
					break;
				case	14:
					ret	=mWindings14.GetFreeItem();
					break;
				case	15:
					ret	=mWindings15.GetFreeItem();
					break;
				case	16:
					ret	=mWindings16.GetFreeItem();
					break;
				case	17:
					ret	=mWindings17.GetFreeItem();
					break;
				case	18:
					ret	=mWindings18.GetFreeItem();
					break;
				case	19:
					ret	=mWindings19.GetFreeItem();
					break;
				case	20:
					ret	=mWindings20.GetFreeItem();
					break;
				case	21:
					ret	=mWindings21.GetFreeItem();
					break;
				case	22:
					ret	=mWindings22.GetFreeItem();
					break;
				case	23:
					ret	=mWindings23.GetFreeItem();
					break;
				case	24:
					ret	=mWindings24.GetFreeItem();
					break;
				case	25:
					ret	=mWindings25.GetFreeItem();
					break;
				case	26:
					ret	=mWindings26.GetFreeItem();
					break;
				case	27:
					ret	=mWindings27.GetFreeItem();
					break;
				case	28:
					ret	=mWindings28.GetFreeItem();
					break;
				case	29:
					ret	=mWindings29.GetFreeItem();
					break;
				case	30:
					ret	=mWindings30.GetFreeItem();
					break;
				case	31:
					ret	=mWindings31.GetFreeItem();
					break;
				case	32:
					ret	=mWindings32.GetFreeItem();
					break;
			}

			for(int i=0;i < size;i++)
			{
				ret[i]	=verts[i];
			}
			return	ret;
		}
	}
}
