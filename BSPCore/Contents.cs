using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;


namespace BSPCore
{
	public class Contents
	{
		//
		//	Quake and I think Hammer content flags
		//
		public const UInt32	CONTENTS_EMPTY			=0;
		public const UInt32	CONTENTS_SOLID			=1;		//an eye is never valid in a solid
		public const UInt32	CONTENTS_WINDOW			=2;		//translucent, but not watery
		public const UInt32	CONTENTS_AUX			=4;
		public const UInt32	CONTENTS_LAVA			=8;
		public const UInt32	CONTENTS_SLIME			=16;
		public const UInt32	CONTENTS_WATER			=32;
		public const UInt32	CONTENTS_MIST			=64;
		public const UInt32	LAST_VISIBLE_CONTENTS	=64;

		//remaining contents are non-visible, and don't eat brushes
		public const UInt32	CONTENTS_AREAPORTAL		=0x8000;
		public const UInt32	CONTENTS_PLAYERCLIP		=0x10000;
		public const UInt32	CONTENTS_MONSTERCLIP	=0x20000;

		//currents can be added to any other contents, and may be mixed
		public const UInt32	CONTENTS_CURRENT_0		=0x40000;
		public const UInt32	CONTENTS_CURRENT_90		=0x80000;
		public const UInt32	CONTENTS_CURRENT_180	=0x100000;
		public const UInt32	CONTENTS_CURRENT_270	=0x200000;
		public const UInt32	CONTENTS_CURRENT_UP		=0x400000;
		public const UInt32	CONTENTS_CURRENT_DOWN	=0x800000;

		public const UInt32	CONTENTS_ORIGIN			=0x1000000;		//removed before bsping an entity

		public const UInt32	CONTENTS_MONSTER		=0x2000000;		//should never be on a brush, only in game
		public const UInt32	CONTENTS_DEADMONSTER	=0x4000000;
		public const UInt32	CONTENTS_DETAIL			=0x8000000;		//brushes to be added after vis leafs
		public const UInt32	CONTENTS_TRANSLUCENT	=0x10000000;	//auto set if any surface has trans
		public const UInt32	CONTENTS_LADDER			=0x20000000;
		public const UInt32	CONTENTS_STRUCTURAL		=0x10000000;	//brushes used for the bsp
		public const UInt32	CONTENTS_TRIGGER		=0x40000000;
		public const UInt32	CONTENTS_NODROP			=0x80000000;	//don't leave bodies or items (death fog, lava)

		//
		//	Genesis contents, the above stuff is converted to these below
		//
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
		public const UInt32 RESERVED4					=(1<<12);
		public const UInt32 RESERVED5					=(1<<13);
		public const UInt32 RESERVED6					=(1<<14);
		public const UInt32 RESERVED7					=(1<<15);

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


		//convert hammer contents to genesis style contents
		static public UInt32 FixHammerContents(UInt32 hammerContents)
		{
			UInt32	ret	=0;

			if(hammerContents == 0)
			{
				//set to solid by default?
				ret	|=BSP_CONTENTS_SOLID2;
			}

			if((hammerContents & CONTENTS_LAVA) != 0)
			{
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
				ret	|=BSP_CONTENTS_USER1;
			}
			if((hammerContents & CONTENTS_SLIME) != 0)
			{
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
				ret	|=BSP_CONTENTS_WAVY2;
				ret	|=BSP_CONTENTS_USER2;
			}
			if((hammerContents & CONTENTS_WATER) != 0)
			{
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
				ret	|=BSP_CONTENTS_WAVY2;
				ret	|=BSP_CONTENTS_USER3;
			}
			if((hammerContents & CONTENTS_MIST) != 0)
			{
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
				ret	|=BSP_CONTENTS_USER4;
			}
			if((hammerContents & CONTENTS_AREAPORTAL) != 0)
			{
				ret	|=BSP_CONTENTS_AREA2;
			}
			if((hammerContents & CONTENTS_PLAYERCLIP) != 0)
			{
				ret	|=BSP_CONTENTS_CLIP2;
			}
			if((hammerContents & CONTENTS_MONSTERCLIP) != 0)
			{
				ret	|=BSP_CONTENTS_CLIP2;
			}
			if((hammerContents & CONTENTS_CURRENT_0) != 0)
			{
				ret	|=BSP_CONTENTS_USER5;
			}
			if((hammerContents & CONTENTS_CURRENT_90) != 0)
			{
				ret	|=BSP_CONTENTS_USER6;
			}
			if((hammerContents & CONTENTS_CURRENT_180) != 0)
			{
				ret	|=BSP_CONTENTS_USER7;
			}
			if((hammerContents & CONTENTS_CURRENT_270) != 0)
			{
				ret	|=BSP_CONTENTS_USER8;
			}
			if((hammerContents & CONTENTS_CURRENT_UP) != 0)
			{
				ret	|=BSP_CONTENTS_USER9;
			}
			if((hammerContents & CONTENTS_CURRENT_DOWN) != 0)
			{
				ret	|=BSP_CONTENTS_USER10;
			}
			if((hammerContents & CONTENTS_DETAIL) != 0)
			{
				ret	|=BSP_CONTENTS_DETAIL2;
			}
			if((hammerContents & CONTENTS_TRANSLUCENT) != 0)
			{
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
			}
			if((hammerContents & CONTENTS_LADDER) != 0)
			{
				ret	|=BSP_CONTENTS_USER11;
			}
			if((hammerContents & CONTENTS_STRUCTURAL) != 0)
			{
				ret	|=BSP_CONTENTS_SOLID2;
			}
			if((hammerContents & CONTENTS_TRIGGER) != 0)
			{
				ret	|=BSP_CONTENTS_TRIGGER;
				ret	|=BSP_CONTENTS_EMPTY2;
			}
			if((hammerContents & CONTENTS_NODROP) != 0)
			{
				ret	|=BSP_CONTENTS_USER13;
			}

			//HACK!  Convert solid sheets to clip...
			if(((ret & BSP_CONTENTS_SHEET) !=0)
				&& ((ret & BSP_CONTENTS_SOLID2) !=0))
			{
				ret	&=~BSP_CONTENTS_SOLID2;
				ret |= BSP_CONTENTS_CLIP2;
			}

			if((ret & BSP_CONTENTS_WINDOW2) != 0)
			{
				ret	|=BSP_CONTENTS_WINDOW2;
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
			}

			return	ret;			
		}


		//convert quake contents to genesis style contents
		static public UInt32 FixQuakeContents(UInt32 quakeContents)
		{
			UInt32	ret	=0;

			if((quakeContents & CONTENTS_SOLID) != 0)
			{
				ret	|=BSP_CONTENTS_SOLID2;
			}

			if((quakeContents & CONTENTS_DETAIL) != 0)
			{
				ret	|=BSP_CONTENTS_DETAIL2;
			}

			if((quakeContents & CONTENTS_AUX) != 0)
			{
				ret	|=BSP_CONTENTS_SOLID2;
				ret	|=BSP_CONTENTS_USER14;	//teleport
				ret	|=BSP_CONTENTS_WAVY2;
				ret	|=BSP_CONTENTS_DETAIL2;
			}
			else if((quakeContents & CONTENTS_WINDOW) != 0)
			{
				ret	|=BSP_CONTENTS_WINDOW2;
				ret	|=BSP_CONTENTS_TRANSLUCENT2;
			}
			else if((quakeContents & CONTENTS_LAVA) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_TRANSLUCENT2;
				ret	|=Contents.BSP_CONTENTS_EMPTY2;
				ret	|=Contents.BSP_CONTENTS_USER1;
				ret	|=Contents.BSP_CONTENTS_WAVY2;
			}
			else if((quakeContents & CONTENTS_SLIME) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_TRANSLUCENT2;
				ret	|=Contents.BSP_CONTENTS_EMPTY2;
				ret	|=Contents.BSP_CONTENTS_WAVY2;
				ret	|=Contents.BSP_CONTENTS_USER2;
			}
			else if((quakeContents & CONTENTS_WATER) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_TRANSLUCENT2;
				ret	|=Contents.BSP_CONTENTS_EMPTY2;
				ret	|=Contents.BSP_CONTENTS_WAVY2;
				ret	|=Contents.BSP_CONTENTS_USER3;
			}
			else if((quakeContents & CONTENTS_MIST) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_TRANSLUCENT2;
				ret	|=Contents.BSP_CONTENTS_EMPTY2;
				ret	|=Contents.BSP_CONTENTS_USER4;
			}
			else if((quakeContents & CONTENTS_TRANSLUCENT) != 0)
			{
				CoreEvents.Print("Some sort of nonstandard translucent on a face\n");
			}

			if((quakeContents & CONTENTS_AREAPORTAL) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_AREA2;
			}

			if((quakeContents & CONTENTS_PLAYERCLIP) != 0
				|| (quakeContents & CONTENTS_MONSTERCLIP) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_CLIP2;
			}

			if((quakeContents & CONTENTS_CURRENT_0) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER5;
			}
			if((quakeContents & CONTENTS_CURRENT_90) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER6;
			}
			if((quakeContents & CONTENTS_CURRENT_180) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER7;
			}
			if((quakeContents & CONTENTS_CURRENT_270) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER8;
			}
			if((quakeContents & CONTENTS_CURRENT_UP) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER9;
			}
			if((quakeContents & CONTENTS_CURRENT_DOWN) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER10;
			}
			if((quakeContents & CONTENTS_LADDER) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER11;
			}
			if((quakeContents & CONTENTS_TRIGGER) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_TRIGGER;
			}
			if((quakeContents & CONTENTS_NODROP) != 0)
			{
				ret	|=Contents.BSP_CONTENTS_USER13;
			}

			return	ret;			
		}


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
}
