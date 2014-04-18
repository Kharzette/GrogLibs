//commonly used types
#ifndef _TYPESFXH
#define _TYPESFXH


struct VPos
{
	float3	Position	: POSITION;
};

struct VPosNorm
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
};

struct VPosTex0
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosTex01
{
	float3	Position	: POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VPosTex03
{
	float3	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VPos4Tex04
{
	float4	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
};

struct VPosCol0
{
	float3	Position	: POSITION;
	float4	Color		: COLOR0;
};

struct VPosCol0Tex04Tex14Tex24
{
	float3	Position	: POSITION;
	float4	Color		: COLOR0;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
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
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosTex0Col0Tan
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;
	float4	Tangent		: TANGENT0;
};

struct VPosTex0Single
{
	float3	Position	: POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VPosTex0SingleCol0
{
	float3	Position	: POSITION;
	float	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosTex0Tex1Col0
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VPosTex0Tex1Col0Col1
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color0		: COLOR0;
	float4	Color1		: COLOR1;
};

struct VPosCubeTex0
{
	float3	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VPosTex0Tex1
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosTex03Tex13
{
	float3	Position	: POSITION;
	float3	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VPosTex04Tex14
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VPosTex04Tex14Tex24
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VPosTex04Tex14Tex24Tex31
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float	TexCoord3	: TEXCOORD3;
};

struct VPosTex04Tex14Tex24Tex34
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
};

struct VPosTex0Tex13
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VPosTex0Tex14
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VPosTex0Tex1Single
{
	float3	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VPosNormTanBiTanTex0
{
	float3	Position	: POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
};

struct VPosNormTex0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormTanTex0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Tangent		: TANGENT0;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormTanBiTanTex0Col0
{
	float3	Position	: POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float4	Color		: COLOR0;
};

struct VPosNormBone
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
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
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosNormTex0Tex1Single
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VPosNormCol0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	Color		: COLOR0;	
};

struct VPosNormBoneTex0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	float2	TexCoord0	: TEXCOORD0;
};

struct VPosNormBoneTex0Tex1
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VPosNormBoneCol0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	float4	Color		: COLOR0;	
};

struct VPosNormBoneTex0Col0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	half4	Weight0		: BLENDWEIGHTS0;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VPosNormTex0Tex1Col0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VPosNormBlendTex0Tex1Tex2Tex3Tex4
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float2	TexCoord2	: TEXCOORD2;
	float2	TexCoord3	: TEXCOORD3;
	float2	TexCoord4	: TEXCOORD4;
};

struct VPosTex04Tex14Tex24Tex34Tex44
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;	//tex0 & tex1
	float4	TexCoord1	: TEXCOORD1;	//tex2 & tex3
	float4	TexCoord2	: TEXCOORD2;	//worldPos.xyz & norm.x
	float4	TexCoord3	: TEXCOORD3;	//norm.yz & style1 & style2
	float4	TexCoord4	: TEXCOORD4;	//style3 & style4 & alpha
};

struct VPosTex0Tex1Tex2Tex3Tex4Col0Intensity
{
	float3	Position	: POSITION;
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
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	TexCoord0	: TEXCOORD0;	
};

struct VPosNormTex04Col0
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
	float4	TexCoord0	: TEXCOORD0;	
	float4	Color		: COLOR0;
};

struct VPosNormBlendTex04Tex14Tex24
{
	float3	Position	: POSITION;
	float3	Normal		: NORMAL;
#if defined(SM2)
	half4	Blend0		: BLENDINDICES0;
#else
	int4	Blend0		: BLENDINDICES0;
#endif

	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VPosTex04Tex14Tex24Tex34Tex44Tex54
{
	float3	Position	: POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
	float4	TexCoord5	: TEXCOORD5;
};

struct VVPosNorm
{
	float4	Position	: SV_POSITION;
	float3	Normal		: NORMAL;
};

struct VVPosCol0
{
	float4	Position	: SV_POSITION;
	float4	Color		: COLOR0;
};

struct VVPosTex0
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
};

struct VVPosTex01
{
	float4	Position	: SV_POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VVPosTex03
{
	float4	Position	: SV_POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VVPosTex04
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
};

struct VVPosTex0TanBiNorm
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiNormal	: TEXCOORD2;
};

struct VVPosCubeTex0
{
	float4	Position	: SV_POSITION;
	float3	TexCoord0	: TEXCOORD0;
};

struct VVPosTex0Tex13
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Tex14
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Tex13VPos
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
	float3	VPos		: VPOS0;
};

struct VVPosTex0Col0
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
};

struct VVPosTex0Col0TanBiNorm
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float4	Color		: COLOR0;	
	float4	Tangent		: TANGENT0;
	float4	BiNormal	: BINORMAL0;
};

struct VVPosTex0Tex1SingleCol0
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VVPosTex0Single
{
	float4	Position	: SV_POSITION;
	float	TexCoord0	: TEXCOORD0;
};

struct VVPosTex0Tex1Single
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float	TexCoord1	: TEXCOORD1;
};

struct VVPosTex0Tex1
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VVPosTex03Tex13
{
	float4	Position	: SV_POSITION;
	float3	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
};

struct VVPosTex03Tex13VPos
{
	float4	Position	: SV_POSITION;
	float3	TexCoord0	: TEXCOORD0;
	float3	TexCoord1	: TEXCOORD1;
	float3	VPos		: VPOS0;
};

struct VVPosNormTex0Tex1
{
	float4	Position	: SV_POSITION;
	float3	Normal		: Normal;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

struct VVPosNormTanBiTanTex0
{
	float4	Position	: SV_POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
};

struct VVPosNormTanBiTanTex0Tex1
{
	float4	Position	: SV_POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float2	TexCoord1	: TEXCOORD4;
};

struct VVPosNormTanBiTanTex0Col0
{
	float4	Position	: SV_POSITION;
	float3	Normal		: TEXCOORD0;
	float3	Tangent		: TEXCOORD1;
	float3	BiTangent	: TEXCOORD2;
	float2	TexCoord0	: TEXCOORD3;
	float4	Color		: COLOR0;
};

struct VVPosTex0Tex1Col0
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color		: COLOR0;	
};

struct VVPosTex0Tex1Col0Col1
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float4	Color0		: COLOR0;
	float4	Color1		: COLOR0;
};

struct VVPosTex0Tex1Tex2Tex3Tex4Col0Intensity
{
	float4	Position	: SV_POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
	float2	TexCoord2	: TEXCOORD2;
	float2	TexCoord3	: TEXCOORD3;
	float2	TexCoord4	: TEXCOORD4;
	float4	Color		: COLOR0;
	float4	Intensity	: TEXCOORD5;
};

struct VVPosTex04Tex14
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
};

struct VVPosTex04Tex14VPos
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float3	VPos		: VPOS0;
};

struct VVPosTex04Tex14Tex24
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
};

struct VVPosTex04Tex14Tex24Tex31
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float	TexCoord3	: TEXCOORD3;
};

struct VVPosTex04Tex14Tex24Tex34
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
};

struct VVPosTex04Tex14Tex24Tex34VPos
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float3	VPos		: VPOS0;
};

struct VVPosTex04Tex14Tex24Tex34Tex44
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
};

struct VVPosTex04Tex14Tex24Tex34Tex44Tex54
{
	float4	Position	: SV_POSITION;
	float4	TexCoord0	: TEXCOORD0;
	float4	TexCoord1	: TEXCOORD1;
	float4	TexCoord2	: TEXCOORD2;
	float4	TexCoord3	: TEXCOORD3;
	float4	TexCoord4	: TEXCOORD4;
	float4	TexCoord5	: TEXCOORD5;
};

#endif	//_TYPESFXH