using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using UtilityLib;

using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	public class MeshMaterial
	{
		public enum WearLocations
		{
			NONE					=0,
			BOOT					=1,
			TORSO					=2,
			GLOVE					=4,
			HAT						=8,
			SHOULDERS				=16,
			FACE					=32,
			LEFT_HAND				=64,
			RIGHT_HAND				=128,
			HAIR					=256,
			BACK					=512,
			BRACERS					=1024,
			RING_LEFT				=2048,
			RING_RIGHT				=4096,
			EARRING_LEFT			=8192,
			EARRING_RIGHT			=16384,
			BELT					=32768,	//is this gonna overflow?
			HAT_FACE				=24,	//orred together values
			HAT_HAIR				=40,	//for the editor
			FACE_HAIR				=48,
			HAT_EARRINGS			=24584,
			HAT_HAIR_EARRINGS		=24840,
			HAT_FACE_HAIR_EARRINGS	=24872,
			GLOVE_RINGS				=6148
		}

		//per instance material stuff
		public string	mMaterialName;
		public int		mMaterialID;
		public bool		mbVisible;
		public Matrix	mObjectTransform;

		//reference to material lib
		public MatLib	mMatLib;


		internal void Read(BinaryReader br)
		{
			mMaterialName		=br.ReadString();
			mMaterialID			=br.ReadInt32();
			mbVisible			=br.ReadBoolean();
			mObjectTransform	=FileUtil.ReadMatrix(br);
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mMaterialName);
			bw.Write(mMaterialID);
			bw.Write(mbVisible);
			FileUtil.WriteMatrix(bw, mObjectTransform);
		}
	}
}
