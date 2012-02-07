//commonly used types
#ifndef _TYPESFXH
#define _TYPESFXH


struct VPos
{
	float4	Position	: POSITION;
};

struct VPosNorm
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct VPosTex0
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosTex04
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
};

struct VPosCol0
{
	float4	Position	: POSITION;
	float4	Color		: COLOR0;	
};

struct VPosTex0Col0
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosTex0Col0Tan
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;
	float4	Tangent		: TANGENT0;
};

struct VPosTex0Single
{
	float4	Position	: POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VPosTex0SingleCol0
{
	float4	Position	: POSITION;
	float	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosTex0Tex1Col0
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VPosTex0Tex1Col0Col1
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color0		: COLOR0;
	float4	Color1		: COLOR1;
};

struct VPosCubeTex0
{
	float4	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VPosTex0Tex1
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosTex03Tex13
{
	float4	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VPosTex04Tex14
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VPosTex04Tex14Tex24
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VPosTex04Tex14Tex24Tex34
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
};

struct VPosTex0Tex1Cube
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VPosTex0Tex1Single
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VPosNormTex0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormTanBiTanTex0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float3	Tangent		: TANGENT0;
	float3	BiTangent	: BINORMAL0;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormTanTex0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Tangent		: TANGENT0;
	float2	TexCoord0	: TEXCOORD0;
};

struct VOutPosNormTanBiTanTex0
{
	float4	Position	: POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
};

struct VOutPosNormTanBiTanTex0Col0
{
	float4	Position	: POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float4	Color		: COLOR0;
};

struct VPosNormBone
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float4	Weight0		: BLENDWEIGHT0;
};

struct VPosNormTex0Col0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosNormTex0Tex1Single
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VPosNormCol0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Color		: COLOR0;	
};

struct VPosNormBoneTex0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float4	Weight0		: BLENDWEIGHT0;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormBoneTex0Tex1
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float4	Weight0		: BLENDWEIGHT0;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosNormBoneTex0Col0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float4	Weight0		: BLENDWEIGHT0;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1Col0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VPosNormBlendTex0Tex1Tex2Tex3Tex4
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float2	TexCoord2	: TEXCOORD2;
	float2	TexCoord3	: TEXCOORD3;
	float2	TexCoord4	: TEXCOORD4;
};

struct VPosTex04Tex14Tex24Tex34Tex44
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;	//tex0 & tex1
	float4	TexCoord1	: TEXCOORD1;	//tex2 & tex3
	float4	TexCoord2	: TEXCOORD2;	//worldPos.xyz & norm.x
	float4	TexCoord3	: TEXCOORD3;	//norm.yz & style1 & style2
	float4	TexCoord4	: TEXCOORD4;	//style3 & style4 & alpha
};

struct VPosTex0Tex1Tex2Tex3Tex4Col0Intensity
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float2	TexCoord2	: TEXCOORD2;
	float2	TexCoord3	: TEXCOORD3;
	float2	TexCoord4	: TEXCOORD4;
	float4	Color		: COLOR0;
	float4	Intensity	: TEXCOORD5;
};

struct VPosNormTex04
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	TexCoord0	: TEXCOORD0;	
};

struct VPosNormTex04Col0
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	TexCoord0	: TEXCOORD0;	
	float4	Color		: COLOR0;
};

struct VPosNormBlendTex04Tex14Tex24
{
	float4	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Blend0		: BLENDINDICES0;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VPosTex04Tex14Tex24Tex34Tex44Tex54
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
	float4	TexCoord5	: TEXCOORD5;
};

struct VTex0
{
	float2	TexCoord0	: TEXCOORD0;
};

struct VTex04
{
	float4	TexCoord0	: TEXCOORD0;
};

struct VTex0TanBiNorm
{
	float2	TexCoord0	: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiNormal	: TEXCOORD2;
};

struct VCubeTex0
{
	float3	TexCoord0	: TEXCOORD0;
};

struct VTex0Tex1Cube
{
	float2	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VTex0Col0
{
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VTex0Col0TanBiNorm
{
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
	float4	Tangent		: TANGENT0;
	float4	BiNormal	: BINORMAL0;
};

struct VTex0Tex1SingleCol0
{
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VTex0Single
{
	float	TexCoord0	: TEXCOORD0;
};

struct VTex0Tex1Single
{
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VTex0Tex1
{
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VTex03Tex13
{
	float3	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VNormTex0Tex1
{
	float3	Normal		: Normal;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VNormTanBiTanTex0
{
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
};

struct VNormTanBiTanTex0Tex1
{
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float2	TexCoord1	: TEXCOORD4;
};

struct VNormTanBiTanTex0Col0
{
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float4	Color		: COLOR0;
};

struct VTex0Tex1Col0
{
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VTex0Tex1Col0Col1
{
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color0		: COLOR0;
	float4	Color1		: COLOR0;
};

struct VTex0Tex1Tex2Tex3Tex4Col0Intensity
{
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float2	TexCoord2	: TEXCOORD2;
	float2	TexCoord3	: TEXCOORD3;
	float2	TexCoord4	: TEXCOORD4;
	float4	Color		: COLOR0;
	float4	Intensity	: TEXCOORD5;
};

struct VTex04Tex14
{
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VTex04Tex14Tex24
{
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VTex04Tex14Tex24Tex34
{
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
};

struct VTex04Tex14Tex24Tex34Tex44
{
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
};

struct VTex04Tex14Tex24Tex34Tex44Tex54
{
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
	float4	TexCoord5	: TEXCOORD5;
};
#endif	//_TYPESFXH