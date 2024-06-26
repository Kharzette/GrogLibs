﻿using System;


namespace UtilityLib;

public class ObjEventArgs : EventArgs
{
	public	object	mObj;

	public ObjEventArgs(object obj) : base()
	{
		mObj	=obj;
	}
}


public static unsafe partial class Misc
{
	public static void SafeInvoke(this EventHandler eh, object sender)
	{
		if(eh != null)
		{
			eh(sender, EventArgs.Empty);
		}
	}


	public static void SafeInvoke(this EventHandler eh, object sender, EventArgs ea)
	{
		if(eh != null)
		{
			eh(sender, ea);
		}
	}


	public static void SafeInvoke<T>(this EventHandler<T> eh, object sender, T ea) where T : EventArgs
	{
		if(eh != null)
		{
			eh(sender, ea);
		}
	}


	static string AddCountStuffToString(int num, string stuff)
	{
		string	ret	="";

		for(int i=0;i < num;i++)
		{
			ret	+=stuff;
		}
		return	ret;
	}


	public static string FloatToString(float f, int numDecimalPlaces)
	{
		//this I think prevents scientific notation on small numbers
		decimal	d	=Convert.ToDecimal(f);

		return	string.Format("{0:0." + AddCountStuffToString(numDecimalPlaces, "#") + "}",
			d.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}


	public static bool bFlagSet(UInt32 val, UInt32 flag)
	{
		return	((val & flag) != 0);
	}


	public static bool bFlagSet(Int32 val, Int32 flag)
	{
		return	((val & flag) != 0);
	}


	public static void ClearFlag(ref Int32 val, Int32 flag)
	{
		val	&=(~flag);
	}


	public static void ClearFlag(ref UInt32 val, UInt32 flag)
	{
		val	&=(~flag);
	}


	public static void MemCpy(float []src, float []dest, int byteSize)
	{
		fixed(float *pSrc = src)
		{
			fixed(float *pDst = dest)
			{
				Buffer.MemoryCopy(pSrc, pDst, byteSize, byteSize);
			}
		}
	}


	public static void MemCpy(float []src, float *pDest, int byteSize)
	{
		fixed(float *pSrc = src)
		{
			Buffer.MemoryCopy(pSrc, pDest, byteSize, byteSize);
		}
	}
}