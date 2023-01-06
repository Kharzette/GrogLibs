using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;


namespace MaterialLib;

//Material stuff specific to characters
internal class CharacterMat
{
	//bones are directly set in meshlib
	//so nothing here right now


	internal CharacterMat Clone()
	{
		CharacterMat	ret	=new CharacterMat();

		return	ret;
	}


	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
	}
}