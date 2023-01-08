using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;


namespace MeshLib;

//a hopefully exhaustive list of possible data
//structures for anything you could export.
//I'm sure there's stuff I didn't think of.
[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPos
{
	[FieldOffset(0)]
	public Vector3	Position;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNorm
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBone
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Tex3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Half2	TexCoord3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosCol0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosCol0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	Color0;
	[FieldOffset(16)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosCol0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	Color0;
	[FieldOffset(16)]
	public Color	Color1;
	[FieldOffset(20)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosCol0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	Color0;
	[FieldOffset(16)]
	public Color	Color1;
	[FieldOffset(20)]
	public Color	Color2;
	[FieldOffset(24)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Color	Color0;
	[FieldOffset(20)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Color	Color0;
	[FieldOffset(20)]
	public Color	Color1;
	[FieldOffset(24)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Color	Color0;
	[FieldOffset(20)]
	public Color	Color1;
	[FieldOffset(24)]
	public Color	Color2;
	[FieldOffset(28)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
	[FieldOffset(28)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
	[FieldOffset(28)]
	public Color	Color2;
	[FieldOffset(32)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
	[FieldOffset(36)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Tex3Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Half2	TexCoord3;
	[FieldOffset(28)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Tex3Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Half2	TexCoord3;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Tex3Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Half2	TexCoord3;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosTex0Tex1Tex2Tex3Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half2	TexCoord0;
	[FieldOffset(16)]
	public Half2	TexCoord1;
	[FieldOffset(20)]
	public Half2	TexCoord2;
	[FieldOffset(24)]
	public Half2	TexCoord3;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
	[FieldOffset(40)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Tex3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneCol0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneCol0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneCol0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneCol0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
	[FieldOffset(36)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
	[FieldOffset(40)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
	[FieldOffset(44)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
	[FieldOffset(48)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Tex3Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
	[FieldOffset(40)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
	[FieldOffset(48)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Color	BoneIndex;
	[FieldOffset(16)]
	public Half4	BoneWeights;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
	[FieldOffset(48)]
	public Color	Color2;
	[FieldOffset(52)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack =0)]
public struct VPosNormTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTanTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half4	Tangent;
	[FieldOffset(28)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTanBiTanTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half4	Tangent;
	[FieldOffset(28)]
	public Half4	BiTangent;
	[FieldOffset(36)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Tex3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Half2	TexCoord3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormCol0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormCol0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormCol0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
	[FieldOffset(28)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormCol0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	Color0;
	[FieldOffset(24)]
	public Color	Color1;
	[FieldOffset(28)]
	public Color	Color2;
	[FieldOffset(32)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Color	Color0;
	[FieldOffset(28)]
	public Color	Color1;
	[FieldOffset(32)]
	public Color	Color2;
	[FieldOffset(36)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Color	Color0;
	[FieldOffset(32)]
	public Color	Color1;
	[FieldOffset(36)]
	public Color	Color2;
	[FieldOffset(40)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
	[FieldOffset(44)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Tex3Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Half2	TexCoord3;
	[FieldOffset(36)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Tex3Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Half2	TexCoord3;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Tex3Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Half2	TexCoord3;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Tex1Tex2Tex3Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half2	TexCoord0;
	[FieldOffset(24)]
	public Half2	TexCoord1;
	[FieldOffset(28)]
	public Half2	TexCoord2;
	[FieldOffset(32)]
	public Half2	TexCoord3;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
	[FieldOffset(48)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack =0)]
public struct VPosNormBoneTex0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Tex3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Half2	TexCoord3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneCol0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneCol0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneCol0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneCol0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Color	Color0;
	[FieldOffset(36)]
	public Color	Color1;
	[FieldOffset(40)]
	public Color	Color2;
	[FieldOffset(44)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Color	Color0;
	[FieldOffset(40)]
	public Color	Color1;
	[FieldOffset(44)]
	public Color	Color2;
	[FieldOffset(48)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
	[FieldOffset(48)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Color	Color0;
	[FieldOffset(44)]
	public Color	Color1;
	[FieldOffset(48)]
	public Color	Color2;
	[FieldOffset(52)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Color	Color0;
	[FieldOffset(48)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Color	Color0;
	[FieldOffset(48)]
	public Color	Color1;
	[FieldOffset(52)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Color	Color0;
	[FieldOffset(48)]
	public Color	Color1;
	[FieldOffset(52)]
	public Color	Color2;
	[FieldOffset(56)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Tex3Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Half2	TexCoord3;
	[FieldOffset(48)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Half2	TexCoord3;
	[FieldOffset(48)]
	public Color	Color0;
	[FieldOffset(52)]
	public Color	Color1;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Half2	TexCoord3;
	[FieldOffset(48)]
	public Color	Color0;
	[FieldOffset(52)]
	public Color	Color1;
	[FieldOffset(56)]
	public Color	Color2;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTex0Tex1Tex2Tex3Col0Col1Col2Col3
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half2	TexCoord0;
	[FieldOffset(36)]
	public Half2	TexCoord1;
	[FieldOffset(40)]
	public Half2	TexCoord2;
	[FieldOffset(44)]
	public Half2	TexCoord3;
	[FieldOffset(48)]
	public Color	Color0;
	[FieldOffset(52)]
	public Color	Color1;
	[FieldOffset(56)]
	public Color	Color2;
	[FieldOffset(60)]
	public Color	Color3;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBone
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBoneTanTex0Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half4	BoneWeights;
	[FieldOffset(32)]
	public Half4	Tangent;
	[FieldOffset(40)]
	public Half2	TexCoord0;
	[FieldOffset(44)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormBlendTex0Tex1Tex2Tex3Tex4
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Color	BoneIndex;
	[FieldOffset(24)]
	public Half2	TexCoord0;
	[FieldOffset(28)]
	public Half2	TexCoord1;
	[FieldOffset(32)]
	public Half2	TexCoord2;
	[FieldOffset(36)]
	public Half2	TexCoord3;
	[FieldOffset(40)]
	public Half2	TexCoord4;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex04
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half4	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex04Col0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half4	TexCoord0;
	[FieldOffset(28)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex04Tex14Tex24Color0
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Half4	Normal;
	[FieldOffset(20)]
	public Half4	TexCoord0;
	[FieldOffset(28)]
	public Half4	TexCoord1;
	[FieldOffset(36)]
	public Half4	TexCoord2;
	[FieldOffset(44)]
	public Color	TexCoord3;
}

//higher precision types
[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex04F
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Vector4	Normal;
	[FieldOffset(28)]
	public Vector4	TexCoord0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex0Col0F
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Vector4	Normal;
	[FieldOffset(28)]
	public Vector2	TexCoord0;
	[FieldOffset(36)]
	public Color	Color0;
}

[StructLayout(LayoutKind.Explicit, Pack = 0)]
public struct VPosNormTex04Tex14Tex24Color0F
{
	[FieldOffset(0)]
	public Vector3	Position;
	[FieldOffset(12)]
	public Vector4	Normal;
	[FieldOffset(28)]
	public Vector4	TexCoord0;
	[FieldOffset(44)]
	public Vector4	TexCoord1;
	[FieldOffset(60)]
	public Vector4	TexCoord2;
	[FieldOffset(76)]
	public Color	Color0;
}