//commonly used types
#ifndef _TYPESFXH
#define _TYPESFXH

struct VPosTex0
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
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

struct VPosTex0Tex1Col0
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VPosCubeTex0
{
	float4	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VPosTex0SingleTex1
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

struct VTex0
{
	float2	TexCoord0	: TEXCOORD0;
};

struct VCubeTex0
{
	float3	TexCoord0	: TEXCOORD0;
};

struct VTex0Col0
{
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VTex0SingleTex1
{
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VTex0Tex1Col0
{
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
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
#endif	//_TYPESFXH