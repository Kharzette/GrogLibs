using System;


namespace UtilityLib;

public static partial class Mathery
{
	public static int NextPowerOfTwo(int val)
	{
		int	count	=0;
		while(val > 0)
		{
			val	=val >> 1;
			count++;
		}

		return	(1 << count);
	}


	public static int PreviousPowerOfTwo(int val)
	{
		int	count	=0;
		while(val > 1)
		{
			val	=val >> 1;
			count++;
		}

		return	(1 << count);
	}


	public static void SwapValues(ref int val1, ref int val2)
	{
		val1	^=val2;
		val2	^=val1;
		val1	^=val2;
	}


	public static void SwapValues(ref uint val1, ref uint val2)
	{
		val1	^=val2;
		val2	^=val1;
		val1	^=val2;
	}


	//xor would work but # doesn't let you at the bits
	public static void SwapValues(ref float val1, ref float val2)
	{
		float	temp	=val1;
		val1	=val2;
		val1	=temp;
	}


	public static bool TryParse(string str, out float val)
	{
#if XBOX
		try
		{
			val	=float.Parse(str);
			return	true;
		}
		catch
		{
			val	=float.NaN;
			return	false;
		}
#else
//			return	float.TryParse(str, out val);
		return	float.TryParse(str,
			System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out val);
#endif
	}


	public static bool TryParse(string str, out double val)
	{
#if XBOX
		try
		{
			val	=double.Parse(str);
			return	true;
		}
		catch
		{
			val	=double.NaN;
			return	false;
		}
#else
		return	double.TryParse(str,
			System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out val);
#endif
	}


	public static bool TryParse(string str, out int val)
	{
#if XBOX
		try
		{
			val	=int.Parse(str);
			return	true;
		}
		catch
		{
			val	=0;
			return	false;
		}
#else
		return	int.TryParse(str, out val);
#endif
	}


	public static bool TryParse(string str, out UInt32 val)
	{
#if XBOX
		try
		{
			val	=UInt32.Parse(str);
			return	true;
		}
		catch
		{
			val	=0;
			return	false;
		}
#else
		return	UInt32.TryParse(str, out val);
#endif
	}


	public static bool TryParse(string str, out bool val)
	{
#if XBOX
		try
		{
			val	=bool.Parse(str);
			return	true;
		}
		catch
		{
			val	=false;
			return	false;
		}
#else
		return	bool.TryParse(str, out val);
#endif
	}
}