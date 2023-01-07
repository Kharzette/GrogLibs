using System;
using UtilityLib;


namespace BSPCore;

public class GrogContents
{
	//genesis style contents
	public const UInt32 BSP_CONTENTS_SOLID2			=(1<<0);	//Solid (Visible)
	public const UInt32 BSP_CONTENTS_WINDOW2		=(1<<1);	//Window (Visible)
	public const UInt32 BSP_CONTENTS_EMPTY2			=(1<<2);	//Empty but Visible (water, lava, etc...)

	public const UInt32 BSP_CONTENTS_TRANSLUCENT2	=(1<<3);	//Vis will see through it
	public const UInt32 BSP_CONTENTS_WAVY2			=(1<<4);	//Wavy (Visible)
	public const UInt32 BSP_CONTENTS_DETAIL2		=(1<<5);	//Won't be included in vis oclusion

	public const UInt32 BSP_CONTENTS_CLIP2			=(1<<6);	//Structural but not visible
	public const UInt32 BSP_CONTENTS_HINT2			=(1<<7);	//Primary splitter (Non-Visible)
	public const UInt32 BSP_CONTENTS_AREA2			=(1<<8);	//Area seperator leaf (Non-Visible)

	public const UInt32 BSP_CONTENTS_FLOCKING		=(1<<9);	//flocking flag.  Not really a contents type
	public const UInt32 BSP_CONTENTS_SHEET			=(1<<10);
	public const UInt32 BSP_CONTENTS_TRIGGER		=(1<<11);
	public const UInt32 BSP_CONTENTS_ORIGIN			=(1<<12);	//for setting origin on bmodels
	public const UInt32 RESERVED1					=(1<<13);
	public const UInt32 RESERVED2					=(1<<14);
	public const UInt32 RESERVED3					=(1<<15);

	//16-31 reserved for user contents
	public const UInt32 BSP_CONTENTS_USER1			=(1<<16);
	public const UInt32 BSP_CONTENTS_USER2			=(1<<17);
	public const UInt32 BSP_CONTENTS_USER3			=(1<<18);
	public const UInt32 BSP_CONTENTS_USER4			=(1<<19);
	public const UInt32 BSP_CONTENTS_USER5			=(1<<20);
	public const UInt32 BSP_CONTENTS_USER6			=(1<<21);
	public const UInt32 BSP_CONTENTS_USER7			=(1<<22);
	public const UInt32 BSP_CONTENTS_USER8			=(1<<23);
	public const UInt32 BSP_CONTENTS_USER9			=(1<<24);
	public const UInt32 BSP_CONTENTS_USER10			=(1<<25);
	public const UInt32 BSP_CONTENTS_USER11			=(1<<26);
	public const UInt32 BSP_CONTENTS_USER12			=(1<<27);
	public const UInt32 BSP_CONTENTS_USER13			=(1<<28);
	public const UInt32 BSP_CONTENTS_USER14			=(1<<29);
	public const UInt32 BSP_CONTENTS_USER15			=(1<<30);
	public const UInt32 BSP_CONTENTS_USER16			=(0x80000000);
	
	//These contents are all solid types
	public const UInt32 BSP_CONTENTS_SOLID_CLIP		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_CLIP2);
	
	//These contents are all visible types
	public const UInt32 BSP_VISIBLE_CONTENTS		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_EMPTY2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_SHEET | BSP_CONTENTS_WAVY2);
	
	//These contents define where faces are NOT allowed to merge across
	public const UInt32 BSP_MERGE_SEP_CONTENTS		=(BSP_CONTENTS_WAVY2 | BSP_CONTENTS_HINT2 | BSP_CONTENTS_AREA2);


	//make sure contents are valid
	static public UInt32 FixContents(UInt32 mapContents)
	{
		UInt32	ret	=mapContents;

		//completely empty flags should be solid
		if(ret == 0)
		{
			ret	|=BSP_CONTENTS_SOLID2;
		}

		//triggers should be invisible
		if(Misc.bFlagSet(ret, BSP_CONTENTS_TRIGGER))
		{
		}

		return	ret;			
	}


	static public bool VisSeeThru(UInt32 contents)
	{
		if((contents & BSP_CONTENTS_DETAIL2) != 0)
		{
			//detail is see thru for vis
			return	true;
		}

		if((contents & BSP_CONTENTS_SOLID2) != 0)
		{
			//solid and not detail
			return	false;
		}
		return	true;	//everything else is seethru
	}
}