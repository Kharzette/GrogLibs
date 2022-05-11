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
	Matrix4x4	[]mBones;

	internal Matrix4x4	[]Bones
	{
		set	{	mBones	=value;	}
	}


	internal void Apply(ID3D11DeviceContext dc,
						CBKeeper cbk, StuffKeeper sk)
	{
		cbk.SetBones(mBones);
	}
}