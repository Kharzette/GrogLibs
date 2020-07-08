using System;
using System.Collections.Generic;
using UtilityLib;
using BSPZone;
using SharpDX;


namespace EntityLib
{
	public class TargetName : Component
	{
		public readonly	string	mTargetName;


		public TargetName(string name, Entity owner) : base(owner)
		{
			mTargetName	=name;
		}
	}
}