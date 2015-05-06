//shaders using TomF's trilights for light
//see http://home.comcast.net/~tom_forsyth/blog.wiki.html#Trilights

//material id for borders etc
int	mMaterialID;

//texture layers used on the surface
shared Texture2D	mTexture0;
shared Texture2D	mTexture1;

//These are considered directional (no falloff)
float4	mLightColor0;		//trilights need 3 colors
float4	mLightColor1;		//trilights need 3 colors
float4	mLightColor2;		//trilights need 3 colors
float3	mLightDirection;

//terrain texturing variables
#define	MAX_TERRAIN_TEX		16

float4	mAtlasUVData[MAX_TERRAIN_TEX];
float	mAtlasTexScale[MAX_TERRAIN_TEX];

#include "RenderStates.fxh"

//shared pixel shaders
//just shows the shape for debugging
float4 FullBrightSkinPS(VVPosTex03 input) : SV_Target
{
	return	mSolidColour;
}

//writes distance into a float rendertarget
float4 ShadowPS(VVPosTex03 input) : SV_Target
{
	float	dist	=distance(mShadowLightPos, input.TexCoord0);

	return	float4(dist, 0, 0, 0);
}

struct	CubeTarg
{
	float4	mPosXFace;
	float4	mNegXFace;
	float4	mPosYFace;
	float4	mNegYFace;
	float4	mPosZFace;
	float4	mNegZFace;
};

CubeTarg ShadowCubePS(VVPosTex03Tex13 input) : SV_Target
{
	CubeTarg	ret;

	return	ret;
}

//writes depth
float4 DepthPS(VVPosTex01 input) : SV_Target0
{
	return	float4(input.TexCoord0, 0, 0, 0);
}

//writes material id
float4 MaterialPS(VVPosTex01 input) : SV_Target0
{
	return	float4(mMaterialID, 0, 0, 0);
}

struct TwoHalf4Targets
{
	half4	targ1, targ2;
};

TwoHalf4Targets DMNPS(VVPosTex03Tex13 input) : SV_Target0
{
	TwoHalf4Targets	ret;

	float3	normed	=normalize(input.TexCoord0);

	ret.targ1.x		=mMaterialID;
	ret.targ1.yzw	=normed;
	ret.targ2		=float4(input.TexCoord1, 0);

	return	ret;
}

//single texture, single color modulated
float4 Tex0Col0PS(VVPosTex0Col0 input) : SV_Target
{
	float4	texel	=mTexture0.Sample(LinearWrap, input.TexCoord0);
	
	float4	inColor		=input.Color;	
	float4	texLitColor	=inColor * texel;
	
	return	texLitColor;
}

//single texture, single color modulated, cel
float4 Tex0Col0CelPS(VVPosTex0Col0 input) : SV_Target
{
	float4	texel	=mTexture0.Sample(LinearWrap, input.TexCoord0);
	
	float4	inColor		=input.Color;

#if defined(CELLIGHT)
	inColor.xyz	=CalcCelColor(inColor.xyz);
#endif
	float4	texLitColor	=inColor * texel;
	
	return	texLitColor;
}

//normal mapped from tex1, with tex0 texturing
float4 NormalMapTriTex0Tex1PS(VVPosNormTanBiTanTex0Tex1 input) : SV_Target
{
	float4	norm	=mTexture1.Sample(LinearWrap, input.TexCoord1);
	float4	texel0	=mTexture0.Sample(LinearWrap, input.TexCoord0);

	float3	goodNorm	=ComputeNormalFromMap(
		norm, input.Tangent, input.BiTangent, input.Normal);
	
	float3	texLitColor	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	texLitColor	*=texel0.xyz;
	
	return	float4(texLitColor, texel0.w);
}

//Solid color, trilight
float4 TriSolidPS(VVPosTex03Tex13 input) : SV_Target
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	litSolid	=mSolidColour.xyz * triLight;

	return	float4(litSolid, mSolidColour.w);
}

//Solid color, vert color, trilight, specular
float4 TriSolidVColorSpecPS(VVPosTex03Tex13Tex23 input) : SV_Target
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;
	float3	vcolor	=input.TexCoord2;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
	float3	litSolid	=mSolidColour.xyz * triLight * vcolor;

	specular	=saturate(specular + litSolid);

	return	float4(specular, mSolidColour.w);
}

//Solid color, trilight, and specular
float4 TriSolidSpecPS(VVPosTex03Tex13 input) : SV_Target
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid);

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex0, with solid color & trilight
float4 NormalMapTriTex0SolidPS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=mTexture0.Sample(LinearWrap, tex);
	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	litSolid	=mSolidColour.xyz * triLight;

	return	float4(litSolid, mSolidColour.w);
}

//Texture 0, trilight, modulate solid, and specular
float4 TriTex0SpecPS(VVPosTex04Tex14 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	texColor	=mTexture0.Sample(LinearWrap, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	wpos	=input.TexCoord1.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#endif

	float3	litColor	=texColor.xyz * triLight;

	specular	=saturate((specular + litColor.xyz) * mSolidColour.xyz);

	return	float4(specular, texColor.w);
}

//Texture 0, trilight, modulate solid, cel, and specular
float4 TriCelTex0SpecPS(VVPosTex04Tex14 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	texColor	=mTexture0.Sample(LinearWrap, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	wpos	=input.TexCoord1.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	//this quantizes the light value
#if defined(CELLIGHT)
	triLight	=CalcCelColor(triLight);
#endif

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#endif

	//this will quantize the specularity as well
#if defined(CELSPECULAR)
	specular	=CalcCelColor(specular);
#endif

	float3	litColor	=texColor.xyz * triLight;

	specular	=saturate((specular + litColor.xyz) * mSolidColour.xyz);

	//for super retro goofy color action
#if defined(CELALL)
	specular	=CalcCelColor(specular);
#endif

	return	float4(specular, texColor.w);
}

//cel shading, solid color, trilight and expensive specular
float4 TriCelSolidSpecPS(VVPosTex03Tex13 input) : SV_Target
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	//this quantizes the light value
#if defined(CELLIGHT)
	triLight	=CalcCelColor(triLight);
#endif

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);

	//this will quantize the specularity as well
#if defined(CELSPECULAR)
	specular	=CalcCelColor(specular);
#endif

	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid);

	//for super retro goofy color action
#if defined(CELALL)
	specular	=CalcCelColor(specular);
#endif

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex0, with solid color, trilight and specular
float4 NormalMapTriTex0SolidSpecPS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=mTexture0.Sample(LinearWrap, tex);
	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex1, texture from tex0, with color tinting, trilight and specular
float4 NormalMapTriTex0SpecPS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=mTexture1.Sample(LinearWrap, tex);
	float4	texCol	=mTexture0.Sample(LinearWrap, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
#endif
	float3	litSolid	=texCol.xyz * triLight;

	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//cel, passed in color and specular
float4 TriCelColorSpecPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	//this quantizes the light value
#if defined(CELLIGHT)
	triLight	=CalcCelColor(triLight);
#endif

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);

	//this will quantize the specularity as well
#if defined(CELSPECULAR)
	specular	=CalcCelColor(specular);
#endif

	float3	litSolid	=input.TexCoord2.xyz * triLight;

	specular	=saturate(specular + litSolid);

	//for super retro goofy color action
#if defined(CELALL)
	specular	=CalcCelColor(specular);
#endif

	return	float4(specular, input.TexCoord2.w);
}

//two texture lookups, but one set of texcoords
//alphas tex0 over tex1
float4 Tex0Col0DecalPS(VVPosTex0Col0 input) : SV_Target
{
	float4	texel0, texel1;
	texel0	=mTexture0.Sample(LinearWrap, input.TexCoord0);
	texel1	=mTexture1.Sample(LinearWrap, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=(texel1.w * texel1) + ((1.0 - texel1.w) * texel0);

	texLitColor	*=inColor;

	texLitColor.w	=1.0f;
	
	return	texLitColor;
}

//two texture lookups, 2 texcoord, alpha tex0 over tex1
float4 Tex0Tex1Col0DecalPS(VVPosTex0Tex1Col0 input) : SV_Target
{
	float4	texel0, texel1;
	texel0	=mTexture0.Sample(LinearWrap, input.TexCoord0);
	texel1	=mTexture1.Sample(LinearWrap, input.TexCoord1);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=(texel1.w * texel1) + ((1.0 - texel1.w) * texel0);

	texLitColor	*=inColor;

	texLitColor.w	=1.0f;
	
	return	texLitColor;
}

//trilight, up to 4 texture lookups
float4	TriTexFact4PS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float4	texColor	=float4(0, 0, 0, 0);

	//texcoord1 has worldspace position
	float2	worldXZ	=input.TexCoord1.xz;

	//mSolidColour has texture scale factor
	//kind of a hack but it is a handy float4

	//texcoord2 has texture factor
	if(input.TexCoord2.x > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.x];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.x].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.x].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.x;
	}
	if(input.TexCoord2.y > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.y];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.y].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.y].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.y;
	}
	if(input.TexCoord2.z > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.z];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.z].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.z].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.z;
	}
	if(input.TexCoord2.w > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.w];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.w].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.w].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.w;
	}

//	float3	pnorm	=input.TexCoord0.xyz;

//	pnorm	=normalize(pnorm);

//	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
//							mLightColor0, mLightColor1, mLightColor2);

//	texColor.xyz	*=triLight;

	return	texColor;
}