//common functions used by most shaders
#ifndef _COMMONFUNCTIONSFXH
#define _COMMONFUNCTIONSFXH

//constants
#define	MAX_BONES				55
#define	PI_OVER_FOUR			0.7853981634f
#define	PI_OVER_TWO				1.5707963268f
#define MAX_HALF				65504
#define	OUTLINE_ALPHA_THRESHOLD	0.05

//matrii
float4x4	mWorld;
float4x4	mView;
float4x4	mProjection;
float4x4	mLightViewProj;	//for shadowing
float3		mEyePos;

//for tinting or diffuse
float4	mSolidColour;

//outline / cel related
//1D textures are not supported in 9_3 feature levels
#if defined(SM2)
shared Texture2D	mCelTable;
#else
shared Texture1D	mCelTable;
#endif

//for shadowmaps
shared Texture2D	mShadowTexture;		//2D
shared Texture3D	mShadowCube;		//cube
float3				mShadowLightPos;	//point light location
bool				mbDirectional;		//sunnish or point
float				mShadowAtten;		//shadow attenuation

//specular stuff
float4	mSpecColor;
float	mSpecPower;

//specular behaviour defines
//#define	CELALL			//for a goofy retro cga look
//#define	CELSPECULAR		//for quantized specular
#define	CELLIGHT		//for quantized light (the default)

#include "Types.fxh"
#include "RenderStates.fxh"


//chops a half3 down to a half2
half2 EncodeNormal(half3 norm)
{
	return	(norm.xy * 0.5) + 0.5;
}


//half2 to half3
half3 DecodeNormal(half2 encoded)
{
	half3	ret;

	const half	theSign	=(encoded.x < 0)? -1.0f : 1.0f;
	
	ret.x	=abs(encoded.x) * 2.0f - 1.0f;
	ret.y	=encoded.y;
	ret.z	=sqrt(1.0f - dot(ret.xy, ret.xy)) * theSign;
	
	return	ret;
}


//half2 to half3
//tries to save some instructions
half3 DecodeNormalSM2(half2 encoded)
{
	half3	ret;

	ret.x	=encoded.x * 2.0f - 1.0f;
	ret.y	=encoded.y;
	ret.z	=sqrt(1.0f - dot(ret.xy, ret.xy));
	
	return	ret;
}


//does the math to get a normal from a sampled
//normal map to a proper normal useful for lighting
float3 ComputeNormalFromMap(float4 sampleNorm, float3 tan, float3 biTan, float3 surfNorm)
{
	//convert normal from 0 to 1 to -1 to 1
	sampleNorm	=2.0 * sampleNorm - float4(1.0, 1.0, 1.0, 1.0);

	float3x3	tbn	=float3x3(
					normalize(tan),
					normalize(biTan),
					normalize(surfNorm));
	
	//I borrowed a bunch of my math from GL samples thus
	//this is needed to get things back into XNA Land
	tbn	=transpose(tbn);

	//rotate normal into worldspace
	sampleNorm.xyz	=mul(tbn, sampleNorm.xyz);
	sampleNorm.xyz	=normalize(sampleNorm.xyz);

	return	sampleNorm.xyz;
}


//ints for > sm2
float4x4 GetSkinXForm(int4 bnIdxs, half4 bnWeights, float4x4 bones[MAX_BONES])
{
	float4x4 skinTransform	=bones[bnIdxs.x] * bnWeights.x;

	skinTransform	+=bones[bnIdxs.y] * bnWeights.y;
	skinTransform	+=bones[bnIdxs.z] * bnWeights.z;
	skinTransform	+=bones[bnIdxs.w] * bnWeights.w;
	
	return	skinTransform;
}


//look up the skin transform for sm2
float4x4 GetSkinXForm(half4 bnIdxs, half4 bnWeights, float4x4 bones[MAX_BONES])
{
	float4x4 skinTransform	=bones[bnIdxs.x] * bnWeights.x;

	skinTransform	+=bones[bnIdxs.y] * bnWeights.y;
	skinTransform	+=bones[bnIdxs.z] * bnWeights.z;
	skinTransform	+=bones[bnIdxs.w] * bnWeights.w;
	
	return	skinTransform;
}


//compute the 3 light effects on the vert
//see http://home.comcast.net/~tom_forsyth/blog.wiki.html
float3 ComputeTrilight(float3 normal, float3 lightDir, float3 c0, float3 c1, float3 c2)
{
    float3	totalLight;
	float	LdotN	=dot(normal, lightDir);
	
	//trilight
	totalLight	=(c0 * max(0, LdotN))
		+ (c1 * (1 - abs(LdotN)))
		+ (c2 * max(0, -LdotN));
		
	return	totalLight;
}


float3 ComputeGoodSpecular(float3 wpos, float3 lightDir, float3 pnorm, float3 lightVal, float4 fillLight)
{
	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float3	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(lightVal * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * lightVal * fresTerm * visTerm * fillLight;

	return	specular;
}

float3 ComputeCheapSpecular(float3 wpos, float3 lightDir, float3 pnorm, float3 lightVal, float4 fillLight)
{
	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	float3	specular	=specTerm * lightVal * fillLight;

	return	specular;
}

//snaps a color to a celish range
//makes a sort of EGA/CGA style look
float3 CalcCelColor(float3 colVal)
{
	float3	ret;

//	float3	fracColor	=modf(colVal, colVal);

//	ret.x	=mCelTable.Sample(PointClamp1D, fracColor.x) + colVal.x;
//	ret.y	=mCelTable.Sample(PointClamp1D, fracColor.y) + colVal.y;
//	ret.z	=mCelTable.Sample(PointClamp1D, fracColor.z) + colVal.z;

	float3	fracColor	=frac(colVal);

//	ret	=mCelTable.Sample(PointClampCube, colVal);

	if(colVal.x > 1)
	{
#if defined(SM2)
		ret.x	=mCelTable.Sample(PointClamp, fracColor.x) + (colVal.x - fracColor.x);
#else
		ret.x	=mCelTable.Sample(PointClamp1D, fracColor.x) + (colVal.x - fracColor.x);
#endif
	}
	else
	{
#if defined(SM2)
		ret.x	=mCelTable.Sample(PointClamp, float2(colVal.x, colVal.x));
#else
		ret.x	=mCelTable.Sample(PointClamp1D, colVal.x);
#endif
	}

	if(colVal.y > 1)
	{
#if defined(SM2)
		ret.y	=mCelTable.Sample(PointClamp, fracColor.y) + (colVal.y - fracColor.y);
#else
		ret.y	=mCelTable.Sample(PointClamp1D, fracColor.y) + (colVal.y - fracColor.y);
#endif
	}
	else
	{
#if defined(SM2)
		ret.y	=mCelTable.Sample(PointClamp, float2(colVal.y, colVal.y));
#else
		ret.y	=mCelTable.Sample(PointClamp1D, colVal.y);
#endif
	}

	if(colVal.z > 1)
	{
#if defined(SM2)
		ret.z	=mCelTable.Sample(PointClamp, fracColor.z) + (colVal.z - fracColor.z);
#else
		ret.z	=mCelTable.Sample(PointClamp1D, fracColor.z) + (colVal.z - fracColor.z);
#endif
	}
	else
	{
#if defined(SM2)
		ret.z	=mCelTable.Sample(PointClamp, float2(colVal.z, colVal.z));
#else
		ret.z	=mCelTable.Sample(PointClamp1D, colVal.z);
#endif
	}

	return	ret;
}

float3 CalcSkyColorGradient(float3 worldPos, float3 skyGrad0, float3 skyGrad1)
{
	float3	upVec	=float3(0.0f, 1.0f, 0.0f);

	//texcoord has world pos
	float3	skyVec	=(worldPos - mEyePos);

	skyVec	=normalize(skyVec);

	float	skyDot	=abs(dot(skyVec, upVec));

	return	lerp(skyGrad0, skyGrad1, skyDot);
}

float3 ComputeShadowCoord(float4 worldPos)
{
	float3	shadCoord;

	//powerup near shadow calculation
	float4	lightPos	=mul(worldPos, mLightViewProj);

	//texCoord xy, world depth in z
	shadCoord.xy	=0.5f * lightPos.xy / lightPos.w + float2(0.5f, 0.5f);
	shadCoord.z		=distance(worldPos.xyz, mShadowLightPos);

	//flip y
	shadCoord.y	=1.0f - shadCoord.y;

	return	shadCoord;
}

float4 ApplyShadow(float mapDepth, float pixDepth, float4 color)
{
	if(mapDepth < 2)
	{
		return	color;
	}

	if(mapDepth < pixDepth)
	{
		//match atten, jontology convinced me
		color.xyz	+=color.w * color.w *
			min(0.2, ((mShadowAtten - pixDepth) / mShadowAtten));
	}
	return	color;
}

float4	ShadowColor(bool bDirectional, float4 worldPos, float3 worldNorm, float4 color)
{
	float3	shadDir;

	if(bDirectional)
	{
		//pull direction vector from light matrix
		shadDir.x	=-mLightViewProj._m02;
		shadDir.y	=-mLightViewProj._m12;
		shadDir.z	=-mLightViewProj._m22;
	}
	else
	{
		shadDir	=worldPos.xyz - mShadowLightPos;
	}

	//alphas shadow on both sides
	if(color.w > .95)
	{
		float	facing	=dot(shadDir, worldNorm);
		if(facing >= 0)
		{
			return	color;
		}
	}

	float	pixDepth;
	float3	shadCoord;

	if(bDirectional)
	{
		shadCoord	=ComputeShadowCoord(worldPos);
		pixDepth	=shadCoord.z;

		//check direction of distance
		float	worldDot	=dot(shadDir, worldPos.xyz);
		float	posDot		=dot(shadDir, mShadowLightPos);
		if(worldDot < posDot)
		{
			return	color;
		}
	}
	else
	{
		pixDepth	=length(shadDir);
		shadDir		/=pixDepth;
	}

	if(pixDepth > mShadowAtten)
	{
		return	color;
	}

	float	mapDepth;
	if(bDirectional)
	{
		mapDepth	=mShadowTexture.Sample(PointClamp, shadCoord.xy).r;
	}
	else
	{
		mapDepth	=mShadowTexture.Sample(PointClampCube, shadDir).r;
	}

	return	ApplyShadow(mapDepth, pixDepth, color);
}
#endif	//_COMMONFUNCTIONSFXH