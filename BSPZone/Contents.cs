using System;

namespace BSPZone
{
	public class Contents
	{
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
		
		//These contents are all solid types
		public const UInt32 BSP_CONTENTS_SOLID_CLIP		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_CLIP2);
	}
}
