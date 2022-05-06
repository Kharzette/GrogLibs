//commonly used types
#ifndef _TYPESFXH
#define _TYPESFXH


//vertex shader inputs
//these all generally have a POSITION and
//maybe some other stuff, and are the same
//on all feature levels except where ifdefd
struct VPos
{
	float3	Position	: POSITION;
};

struct VPosNorm
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
};

struct VPosTex0
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
};

struct VPosTex01
{
	float3	Position	: POSITION;
	half	TexCoord0	: TEXCOORD0;
};

struct VPos2Tex02
{
	float2	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
};

struct VPos2Tex04
{
	float2	Position	: POSITION;
	half4	TexCoord04	: TEXCOORD0;
};

struct VPos4Tex04Tex14
{
	float4	Position	: POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
};

struct VPosCol0
{
	float3	Position	: POSITION;
	half4	Color		: COLOR0;
};

struct VPosCol0Tex04Tex14Tex24
{
	float3	Position	: POSITION;
	half4	Color		: COLOR0;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
};

struct VPosBone
{
	float3	Position	: POSITION;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif
	half4	Weight0		: BLENDWEIGHTS0;
};

struct VPosTex0Col0
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;	
};

struct VPosTex0Col0Tan
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;
	half4	Tangent		: TANGENT0;
};

struct VPosTex0Tex1Col0
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half4	Color		: COLOR0;	
};

struct VPosTex0Tex1Col0Col1
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half4	Color0		: COLOR0;
	half4	Color1		: COLOR1;
};

struct VPosTex0Tex1
{
	float3	Position	: POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
};

struct VPosNormTex0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
};

struct VPosNormTanTex0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	Tangent		: TANGENT0;
	half2	TexCoord0	: TEXCOORD0;
};

struct VPosNormBone
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
};

struct VPosNormTex0Col0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
};

struct VPosNormCol0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	Color		: COLOR0;	
};

struct VPosNormBoneTex0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	half2	TexCoord0	: TEXCOORD0;
};

struct VPosNormBoneTex0Tex1
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
};

struct VPosNormBoneCol0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	half4	Color		: COLOR0;	
};

struct VPosNormBoneTex0Col0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1Col0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half4	Color		: COLOR0;	
};

struct VPosNormTex04
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	TexCoord0	: TEXCOORD0;	
};

struct VPosNormTex04Col0
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	TexCoord0	: TEXCOORD0;	
	half4	Color		: COLOR0;
};

struct VPosNormTex04Tex14Tex24
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	TexCoord0	: TEXCOORD0;	
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;	
};

struct VPosNormTex04Tex14Tex24Col04
{
	float3	Position	: POSITION;
	half4	Normal		: NORMAL;
	half4	TexCoord0	: TEXCOORD0;	
	half4	TexCoord1	: TEXCOORD1;	
	half4	TexCoord2	: TEXCOORD2;	
	half4	Color		: COLOR0;
};


//pixel shader input stuff for > 9_3 feature
//levels, uses SV_POSITION and the pixel shader
//is free to read from it.
struct VVPosNorm
{
	float4	Position	: SV_POSITION;
	half3	Normal		: NORMAL;
};

struct VVPos
{
	float4	Position	: SV_POSITION;
};

struct VVPosCol0
{
	float4	Position	: SV_POSITION;
	half4	Color		: COLOR0;
};

struct VVPosTex0
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
};

struct VVPosTex01
{
	float4	Position	: SV_POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VVPosTex03
{
	float4	Position	: SV_POSITION;
	half3	TexCoord0	: TEXCOORD0;
};

struct VVPosTex04
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
};

struct VVPosTex04RTAI
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	uint	CubeFace	: SV_RenderTargetArrayIndex;
};

struct VVPosTex0TanBiNorm
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half3	Tangent		: TEXCOORD1;
	half3	BiNormal	: TEXCOORD2;
};

struct VVPosCubeTex0
{
	float4	Position	: SV_POSITION;
	half3	TexCoord0	: TEXCOORD0;
};

struct VVPosTex0Tex13
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half3	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Tex14
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Col0
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;	
};

struct VVPosTex0Col0TanBiNorm
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half4	Color		: COLOR0;	
	half4	Tangent		: TANGENT0;
	half4	BiNormal	: BINORMAL0;
};

struct VVPosTex0Tex1SingleCol0
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
	half4	Color		: COLOR0;	
};

struct VVPosTex0Single
{
	float4	Position	: SV_POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VVPosTex0Tex1Single
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Tex1
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
};

struct VVPosTex03Tex13
{
	float4	Position	: SV_POSITION;
	half3	TexCoord0	: TEXCOORD0;
	half3	TexCoord1	: TEXCOORD1;
};

struct VVPosTex03Tex13Tex23
{
	float4	Position	: SV_POSITION;
	half3	TexCoord0	: TEXCOORD0;
	half3	TexCoord1	: TEXCOORD1;
	half3	TexCoord2	: TexCoord2;
};

struct VVPosNormTex0Tex1
{
	float4	Position	: SV_POSITION;
	half3	Normal		: NORMAL;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
};

struct VVPosNormTanBiTanTex0
{
	float4	Position	: SV_POSITION;
	half3	Normal		: TEXCOORD0;
	half3	Tangent		: TEXCOORD1;
	half3	BiTangent	: TEXCOORD2;
	half2	TexCoord0	: TEXCOORD3;
};

struct VVPosNormTanBiTanTex0Tex1
{
	float4	Position	: SV_POSITION;
	half3	Normal		: TEXCOORD0;
	half3	Tangent		: TEXCOORD1;
	half3	BiTangent	: TEXCOORD2;
	half2	TexCoord0	: TEXCOORD3;
	half2	TexCoord1	: TEXCOORD4;
};

struct VVPosNormTanBiTanTex0Col0
{
	float4	Position	: SV_POSITION;
	half3	Normal		: TEXCOORD0;
	half3	Tangent		: TEXCOORD1;
	half3	BiTangent	: TEXCOORD2;
	half2	TexCoord0	: TEXCOORD3;
	half4	Color		: COLOR0;
};

struct VVPosTex0Tex1Col0
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half4	Color		: COLOR0;	
};

struct VVPosTex0Tex1Col0Col1
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half4	Color0		: COLOR0;
	half4	Color1		: COLOR0;
};

struct VVPosTex0Tex1Tex2Tex3Tex4Col0Intensity
{
	float4	Position	: SV_POSITION;
	half2	TexCoord0	: TEXCOORD0;
	half2	TexCoord1	: TEXCOORD1;
	half2	TexCoord2	: TEXCOORD2;
	half2	TexCoord3	: TEXCOORD3;
	half2	TexCoord4	: TEXCOORD4;
	half4	Color		: COLOR0;
	half4	Intensity	: TEXCOORD5;
};

struct VVPosTex04Tex14
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
};

struct VVPosTex04Tex14Tex24
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
};

struct VVPosTex04Tex14Tex24Tex31
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
	float	TexCoord3	: TEXCOORD3;
};

struct VVPosTex04Tex14Tex24Tex34
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
	half4	TexCoord3	: TEXCOORD3;
};

struct VVPosTex04Tex14Tex24Tex34Tex44
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
	half4	TexCoord3	: TEXCOORD3;
	half4	TexCoord4	: TEXCOORD4;
};

struct VVPosTex04Tex14Tex24Tex34Tex44Tex54
{
	float4	Position	: SV_POSITION;
	half4	TexCoord0	: TEXCOORD0;
	half4	TexCoord1	: TEXCOORD1;
	half4	TexCoord2	: TEXCOORD2;
	half4	TexCoord3	: TEXCOORD3;
	half4	TexCoord4	: TEXCOORD4;
	half4	TexCoord5	: TEXCOORD5;
};


//9_3 specific pixel shader inputs, these
//use VPOS instead of SV_POSITION for
//screen stuff, since SV_POSITION can't be
//read from 9_3
struct VVPos93
{
	float4	Position	: SV_POSITION;
	float4	VPos		: VPOS;
};

#endif	//_TYPESFXH