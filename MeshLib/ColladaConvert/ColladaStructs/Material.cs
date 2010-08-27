using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Material
	{
		public string	mName, mInstanceEffect;
	}


	public class LibImage
	{
		public string	mName, mPath;
	}


	public class MeshMaterials
	{
		private List<uint> mPolyPositionIndices;
		private List<uint> mPolyNormalIndices;
		private List<uint> mPolyUVIndices;

		public MeshMaterials()
		{
			mPolyPositionIndices	=new List<uint>();
			mPolyNormalIndices		=new List<uint>();
			mPolyUVIndices			=new List<uint>();
		}
	}


	public class InstanceMaterial
	{
		public	string	mSymbol, mTarget, mBindSemantic, mBindTarget;
	}
}