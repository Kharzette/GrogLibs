using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//an instance of a non animating boneless style mesh (or meshes)
public partial class StaticMesh
{
	public void SetPartMaterialName(int index, string matName,
									StuffKeeper sk)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}
		mPartMats[index].mMaterialName	=matName;
	}
	public void Draw(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm);
		}
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm, altMaterial);
		}
	}


	public void DrawDMN(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.DrawDMN(mlib, mTransform, mm);
		}
	}
}