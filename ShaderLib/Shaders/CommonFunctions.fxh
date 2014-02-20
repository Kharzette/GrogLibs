//common functions used by most shaders
#ifndef _COMMONFUNCTIONSFXH
#define _COMMONFUNCTIONSFXH

//constants
#define	MAX_BONES		55
#define	PI_OVER_FOUR	0.7853981634f
#define	PI_OVER_TWO		1.5707963268f

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mLightViewProj;	//for shadowing
shared float3	mEyePos;

//for tinting or diffuse
shared float4	mSolidColour;

//outline / cel related
shared Texture	mCelTable;

//for shadowmaps
shared Texture	mShadowTexture;		//2D or cube
shared float3	mShadowLightPos;	//point light location
shared bool		mbDirectional;		//sunnish or point
shared float	mShadowAtten;		//shadow attenuation

//specular stuff
shared float4	mSpecColor;
shared float	mSpecPower;

//specular behaviour defines
//#define	CELALL			//for a goofy retro cga look
//#define	CELSPECULAR		//for quantized specular
#define	CELLIGHT		//for quantized light (the default)

#include "Types.fxh"


sampler CelSampler = sampler_state
{
	Texture		=mCelTable;

	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;

	AddressU	=Clamp;
	AddressV	=Clamp;
};

sampler	ShadowSampler2D	=sampler_state
{
	Texture		=mShadowTexture;
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};

sampler	ShadowSampler3D	=sampler_state
{
	Texture		=mShadowTexture;
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
	AddressW	=Clamp;
};

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


//look up the skin transform
float4x4 GetSkinXForm(int4 bnIdxs, float4 bnWeights, float4x4 bones[MAX_BONES])
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
	float	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

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

//	ret.x	=tex1D(CelSampler, fracColor.x) + colVal.x;
//	ret.y	=tex1D(CelSampler, fracColor.y) + colVal.y;
//	ret.z	=tex1D(CelSampler, fracColor.z) + colVal.z;

	float3	fracColor	=frac(colVal);

	if(colVal.x > 1)
	{
		ret.x	=tex1D(CelSampler, fracColor.x) + (colVal.x - fracColor.x);
	}
	else
	{
		ret.x	=tex1D(CelSampler, colVal.x);
	}

	if(colVal.y > 1)
	{
		ret.y	=tex1D(CelSampler, fracColor.y) + (colVal.y - fracColor.y);
	}
	else
	{
		ret.y	=tex1D(CelSampler, colVal.y);
	}

	if(colVal.z > 1)
	{
		ret.z	=tex1D(CelSampler, fracColor.z) + (colVal.z - fracColor.z);
	}
	else
	{
		ret.z	=tex1D(CelSampler, colVal.z);
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
		mapDepth	=tex2D(ShadowSampler2D, shadCoord.xy).r;
	}
	else
	{
		mapDepth	=texCUBE(ShadowSampler3D, shadDir).r;
	}

	return	ApplyShadow(mapDepth, pixDepth, color);
}
#endif	//_COMMONFUNCTIONSFXH