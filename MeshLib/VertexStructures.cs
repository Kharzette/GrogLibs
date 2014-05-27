using System;
using System.Diagnostics;
using SharpDX;

namespace MeshLib
{
	//a hopefully exhaustive list of possible data
	//structures for anything you could export.
	//I'm sure there's stuff I didn't think of.
	public struct VPos
	{
		public Vector3	Position;
	}

	public struct VPosNorm
	{
		public Vector3	Position;
		public Vector3	Normal;
	}

	public struct VPosBone
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
	}

	public struct VPosTex0
	{
		public Vector3	Position;
		public Half2	TexCoord0;
	}

	public struct VPosTex0Tex1
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
	}

	public struct VPosTex0Tex1Tex2
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
	}

	public struct VPosTex0Tex1Tex2Tex3
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
	}

	public struct VPosCol0
	{
		public Vector3	Position;
		public Color	Color0;
	}

	public struct VPosCol0Col1
	{
		public Vector3	Position;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosCol0Col1Col2
	{
		public Vector3	Position;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosCol0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosTex0Col0
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Color	Color0;
	}

	public struct VPosTex0Col0Col1
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosTex0Col0Col1Col2
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosTex0Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosTex0Tex1Col0
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
	}

	public struct VPosTex0Tex1Col0Col1
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosTex0Tex1Col0Col1Col2
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosTex0Tex1Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosTex0Tex1Tex2Col0
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
	}

	public struct VPosTex0Tex1Tex2Col0Col1
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosTex0Tex1Tex2Col0Col1Col2
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosTex0Tex1Tex2Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosTex0Tex1Tex2Tex3Col0
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
	}

	public struct VPosTex0Tex1Tex2Tex3Col0Col1
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosTex0Tex1Tex2Tex3Col0Col1Col2
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}
	
	public struct VPosBoneTex0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
	}

	public struct VPosBoneTex0Tex1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
	}

	public struct VPosBoneTex0Tex1Tex2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
	}

	public struct VPosBoneTex0Tex1Tex2Tex3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
	}

	public struct VPosBoneCol0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
	}

	public struct VPosBoneCol0Col1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosBoneCol0Col1Col2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosBoneCol0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosBoneTex0Col0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
	}

	public struct VPosBoneTex0Col0Col1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosBoneTex0Col0Col1Col2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosBoneTex0Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosBoneTex0Tex1Col0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
	}

	public struct VPosBoneTex0Tex1Col0Col1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosBoneTex0Tex1Col0Col1Col2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosBoneTex0Tex1Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosBoneTex0Tex1Tex2Col0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
	}

	public struct VPosBoneTex0Tex1Tex2Col0Col1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosBoneTex0Tex1Tex2Col0Col1Col2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosBoneTex0Tex1Tex2Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosBoneTex0Tex1Tex2Tex3Col0
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
	}

	public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormTex0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
	}

	public struct VPosNormTanTex0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half4	Tangent;
		public Half2	TexCoord0;
	}

	public struct VPosNormTanBiTanTex0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half4	Tangent;
		public Half4	BiTangent;
		public Half2	TexCoord0;
	}

	public struct VPosNormTex0Tex1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
	}

	public struct VPosNormTex0Tex1Tex2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
	}

	public struct VPosNormTex0Tex1Tex2Tex3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
	}

	public struct VPosNormCol0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	Color0;
	}

	public struct VPosNormCol0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormCol0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormCol0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormTex0Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Color	Color0;
	}

	public struct VPosNormTex0Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormTex0Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormTex0Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormTex0Tex1Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
	}

	public struct VPosNormTex0Tex1Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormTex0Tex1Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormTex0Tex1Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormTex0Tex1Tex2Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
	}

	public struct VPosNormTex0Tex1Tex2Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormTex0Tex1Tex2Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormTex0Tex1Tex2Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormTex0Tex1Tex2Tex3Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
	}

	public struct VPosNormTex0Tex1Tex2Tex3Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormTex0Tex1Tex2Tex3Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBoneTex0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
	}

	public struct VPosNormBoneTex0Tex1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
	}

	public struct VPosNormBoneTex0Tex1Tex2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
	}

	public struct VPosNormBoneTex0Tex1Tex2Tex3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
	}

	public struct VPosNormBoneCol0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
	}

	public struct VPosNormBoneCol0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormBoneCol0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormBoneCol0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBoneTex0Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
	}

	public struct VPosNormBoneTex0Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormBoneTex0Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormBoneTex0Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBoneTex0Tex1Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
	}

	public struct VPosNormBoneTex0Tex1Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormBoneTex0Tex1Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormBoneTex0Tex1Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBoneTex0Tex1Tex2Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
	}

	public struct VPosNormBoneTex0Tex1Tex2Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormBoneTex0Tex1Tex2Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBoneTex0Tex1Tex2Tex3Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
	}

	public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
	}

	public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
	}

	public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Color	Color0;
		public Color	Color1;
		public Color	Color2;
		public Color	Color3;
	}

	public struct VPosNormBone
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half4	BoneWeights;
	}

	public struct VPosNormBlendTex0Tex1Tex2Tex3Tex4
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Color	BoneIndex;
		public Half2	TexCoord0;
		public Half2	TexCoord1;
		public Half2	TexCoord2;
		public Half2	TexCoord3;
		public Half2	TexCoord4;
	}

	public struct VPosNormTex04
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half4	TexCoord0;
	}

	public struct VPosNormTex04Col0
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half4	TexCoord0;
		public Color	Color0;
	}

	public struct VPosNormTex04Tex14Tex24Tex34
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Half4	TexCoord0;
		public Half4	TexCoord1;
		public Half4	TexCoord2;
		public Color	TexCoord3;
	}
}