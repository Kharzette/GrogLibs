//post process shaders
//ambient occlusion from flashie, an implementation of
//http://graphics.cs.williams.edu/papers/AlchemyHPG11/VV11AlchemyAO.pdf
//
//blur from some article on the interwebs I frogot
//
//edge detection from nvidia
//
//I think this file is set up to only build if the project is set
//to HiDef.  Reach profile should ignore it as it has v3 shaders

//post process stuff
float2		mInvViewPort;
float3		mFrustCorners[4];
float		mFarClip;
float3		mFrustRay;
float		mRandTexSize	=64;

//textures
Texture	mNormalTex;
Texture	mRandTex;
Texture	mColorTex;


#include "Types.fxh"
#include "CommonFunctions.fxh"

//bloom params
float	mBloomThreshold;
float	mBloomIntensity;
float	mBaseIntensity;
float	mBloomSaturation;
float	mBaseSaturation;

//outliner params
float	mTexelSteps;
float	mThreshold;
float2	mScreenSize;

//ambient occlusion params
#define		NUMSAMPLES		12
const int	SamplesPerPass	=4;
float2		mSamples[NUMSAMPLES];
float		mBias			=0.01;
float		mEpsilon		=0.00001;
float		mIntensityScale	=1;
float		mContrast		=1;
float		mRadius			=1;
float		mProjConst		=50.0;

//gaussianblur stuff
#define	RADIUS			30
#define	KERNEL_SIZE		(RADIUS * 2 + 1)
float	mWeightsX[KERNEL_SIZE], mWeightsY[KERNEL_SIZE];
float2	mOffsetsX[KERNEL_SIZE], mOffsetsY[KERNEL_SIZE];
texture	mBlurTargetTex;

//bilateral blur stuff
float	mBlurFallOff;
float	mSharpNess;
float	mBlurRadius	=7;
float	mOpacity;

sampler	NormalSampler	=sampler_state
{
	Texture		=(mNormalTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};

sampler	RandSampler	=sampler_state
{
	Texture		=(mRandTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler	ColorSampler	=sampler_state
{
	Texture		=(mColorTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};

sampler	BlurTargetSampler	=sampler_state
{
	Texture		=(mBlurTargetTex);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Clamp;
	AddressV	=Clamp;
};


//helper functions
float3 DecodeNormal(float4 enc)
{
	return	enc.xyz;	//my normals aren't compressed
//	half4	nn	=enc * half4(2, 2, 0, 0) + half4(-1, -1, 1, -1);
//	half	l	=dot(nn.xyz, -nn.xyw);
	
//	nn.z	=l;
//	nn.xy	*=sqrt(l);
	
//	return	nn.xyz * 2 + half3(0, 0, -1);
}

float3 PositionFromDepth(float2 texCoord, float3 ray)
{
	float	depth	=tex2D(NormalSampler, texCoord).w;
	float3	pos		=ray * depth;
	
	return	pos;
}
 
float3 GetFrustumRay(in float2 texCoord)
{
	float	index	=texCoord.x + (texCoord.y * 2);
	
	return	mFrustCorners[index];
}

float fetch_eye_z(float2 uv)
{
	float	z	=tex2D(NormalSampler, uv).w;
	return	z;
}

float BlurFunction(float2 uv, float r, float center_c,
					float center_d, inout float w_total)
{
	float	c	=tex2D(BlurTargetSampler, uv);
	float	d	=fetch_eye_z(uv);

	float	ddiff	=d - center_d;
	float	fucked	=-r * r * mBlurFallOff - ddiff * ddiff * mSharpNess;
	float	w		=exp(fucked);

	w_total	+=w;

	return	(w * c);
}

float GetGray(float4 c)
{
	return	(dot(c.rgb, ((0.33333).xxx)));
}

//Helper for modifying the saturation of a color. from bloom sample
float4 AdjustSaturation(float4 color, float saturation)
{
	//The constants 0.3, 0.59, and 0.11 are chosen because the
	//human eye is more sensitive to green light, and less to blue.
	float	grey	=dot(color, float3(0.3, 0.59, 0.11));
	
	return	lerp(grey, color, saturation);
}


VPosTex0Tex13	AOVS(VPosTex0 input)
{
	VPosTex0Tex13	output;

	output.Position.x	=input.Position.x - mInvViewPort.x * 0.5f;
	output.Position.y	=input.Position.y + mInvViewPort.y * 0.5f;
	output.Position.z	=input.Position.z;
	output.Position.w	=1.0f;

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=GetFrustumRay(input.TexCoord0);

	return	output;
}


VPosTex0	OutlineVS(VPosTex0 input)
{
	VPosTex0	output;

	output.Position.x	=input.Position.x - mInvViewPort.x * 0.5f;
	output.Position.y	=input.Position.y + mInvViewPort.y * 0.5f;
	output.Position.z	=input.Position.z;
	output.Position.w	=1.0f;

	output.TexCoord0	=input.TexCoord0;

	return	output;
}


float4	AOPS(VTex0Tex13VPos input) : COLOR0
{
	float2	ssc	=input.TexCoord0;

	float4	ndepth	=tex2D(NormalSampler, ssc);
	float	depth	=ndepth.w;
	float3	cPos	=input.TexCoord1 * depth;
	float3	cNormal	=normalize(DecodeNormal(ndepth));
	float	cBias	=mBias;

	float	invNumSamples	=1.0f / SamplesPerPass;

	float	ssr	=mProjConst * mRadius / (depth * mFarClip);

	float2	randomSpin	=tex2D(RandSampler, input.VPos * mInvViewPort * mRandTexSize).rg;

	float	amount	=0;
	for(int i=0;i < NUMSAMPLES;++i)
	{
		float2	unitOffset	=reflect(mSamples[i], randomSpin);
		float2	offset		=float2(unitOffset * ssr);
		float2	ssp			=offset + ssc;
		float	sDepth		=tex2D(NormalSampler, ssp).w;

		ssp		=(ssp - 0.5f) * 2;
		ssp.y	*=-1.0f;

		float3	ray	=mFrustRay;

		ray.xy	*=ssp.xy;

		float3	q	=ray * sDepth;
		float3	v	=q - cPos;

		float	vv	=dot(v, v);
		float	vn	=dot(v, cNormal);

		amount	+=max(0.0, vn + mBias * cPos.z) / (mEpsilon + vv);
	}

	amount	=2.0f * mIntensityScale * amount / NUMSAMPLES;
	amount	=max(0.001, 1.0f - amount);
	amount	=pow(amount, mContrast);

	return	float4(amount, 0, 0, 0);
}

float4	BloomExtractPS(VTex0Tex13VPos input) : COLOR0
{
	float4	ret	=tex2D(BlurTargetSampler, input.TexCoord0);

	return	saturate((ret - mBloomThreshold) / (1 - mBloomThreshold));
}

float4	BloomCombinePS(VTex0Tex13VPos input) : COLOR0
{
	//Look up the bloom and original base image colors.
	float4	bloom	=tex2D(BlurTargetSampler, input.TexCoord0);
	float4	base	=tex2D(ColorSampler, input.TexCoord0);
    
	//Adjust color saturation and intensity.
	bloom	=AdjustSaturation(bloom, mBloomSaturation) * mBloomIntensity;
	base	=AdjustSaturation(base, mBaseSaturation) * mBaseIntensity;
    
	//Darken down the base image in areas where there is a lot of bloom,
	//to prevent things looking excessively burned-out.
	base	*=(1 - saturate(bloom));
    
	//Combine the two images.
	return	base + bloom;
}

float4	GaussianBlurXPS(VTex0Tex13VPos input) : COLOR0
{
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		ret	+=tex2D(BlurTargetSampler, input.TexCoord0 + mOffsetsX[i]) * mWeightsX[i];
	}
	return	ret;
}

float4	GaussianBlurYPS(VTex0Tex13VPos input) : COLOR0
{
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		ret	+=tex2D(BlurTargetSampler, input.TexCoord0 + mOffsetsY[i]) * mWeightsY[i];
	}
	return	ret;
}

float4	BiLatBlurXPS(VTex0Tex13VPos input) : COLOR0
{
	float	b			=0;
	float	w_total		=0;
//	float2	screenCoord	=input.VPos.xy * mInvViewPort;
	float2	screenCoord	=input.TexCoord0;
	float	center_c	=tex2D(BlurTargetSampler, screenCoord);
	float	center_d	=fetch_eye_z(screenCoord);

	for(float r = -RADIUS;r <= RADIUS;++r)
	{
		float2	uv	=screenCoord + float2(r * mInvViewPort.x, 0);
		b			+=BlurFunction(uv, r, center_c, center_d, w_total);
	}

	return	b / w_total;
}

float4	BiLatBlurYPS(VTex0Tex13VPos input) : COLOR0
{
	float	b			=0;
	float	w_total		=0;
//	float2	screenCoord	=input.VPos.xy * mInvViewPort;
	float2	screenCoord	=input.TexCoord0;
	float	center_c	=tex2D(BlurTargetSampler, screenCoord);
	float	center_d	=fetch_eye_z(screenCoord);

	for(float r = -RADIUS;r <= RADIUS;++r)
	{
		float2	uv	=screenCoord + float2(0, r * mInvViewPort.y);
		b			+=BlurFunction(uv, r, center_c, center_d, w_total);
	}

	return	b / w_total * tex2D(ColorSampler, input.TexCoord0);
}

float4	OutlinePS(VTex0 input) : COLOR0
{
	float2	ox	=float2(mTexelSteps / mScreenSize.x, 0.0);
	float2	oy	=float2(0.0, mTexelSteps / mScreenSize.y);
	
	float2	uv	=input.TexCoord0;
	float2	PP	=uv - oy;
	
	float4	CC	=tex2D(ColorSampler, PP - ox);
	float	g00	=GetGray(CC);
	
	CC	=tex2D(ColorSampler, PP);
	float	g01	=GetGray(CC);
	
	CC	=tex2D(ColorSampler, PP + ox);
	float	g02	=GetGray(CC);
	
	PP	=uv;
	CC	=tex2D(ColorSampler, PP - ox);
	float	g10	=GetGray(CC);

	CC	=tex2D(ColorSampler, PP);
	float	g11	=GetGray(CC);
	
	CC	=tex2D(ColorSampler, PP + ox);
	float	g12	=GetGray(CC);
	
	PP	=uv + oy;
	CC	=tex2D(ColorSampler, PP - ox);
	float	g20	=GetGray(CC);
	
	CC	=tex2D(ColorSampler, PP);
	float	g21	=GetGray(CC);

	CC	=tex2D(ColorSampler, PP + ox);
	float	g22	=GetGray(CC);
	
	float	K00	=-1;
	float	K01	=-2;
	float	K02	=-1;
	float	K10	=0;
	float	K11	=0;
	float	K12	=0;
	float	K20	=1;
	float	K21	=2;
	float	K22	=1;
	float	sx	=0;
	float	sy	=0;

	sx	+=g00 * K00;
	sx	+=g01 * K01;
	sx	+=g02 * K02;
	sx	+=g10 * K10;
	sx	+=g11 * K11;
	sx	+=g12 * K12;
	sx	+=g20 * K20;
	sx	+=g21 * K21;
	sx	+=g22 * K22; 
	sy	+=g00 * K00;
	sy	+=g01 * K10;
	sy	+=g02 * K20;
	sy	+=g10 * K01;
	sy	+=g11 * K11;
	sy	+=g12 * K21;
	sy	+=g20 * K02;
	sy	+=g21 * K12;
	sy	+=g22 * K22;
	
	float	dist	=sqrt(sx * sx + sy * sy);

	float	result	=1;
	
	if(dist > mThreshold)
	{
		result	=0;
	}

    return	float4(result, result, result, 1);
}


float4	ModulatePS(VTex0 input) : COLOR0
{
	float4	color	=tex2D(ColorSampler, input.TexCoord0);
	float4	color2	=tex2D(BlurTargetSampler, input.TexCoord0);

	color	*=color2;

	return	float4(color.xyz, 1);
}


float4	BleachBypassPS(VTex0 input) : COLOR0
{
	float4	base		=tex2D(ColorSampler, input.TexCoord0);
	float3	lumCoeff	=float3(0.25, 0.65, 0.1);
	float	lum			=dot(lumCoeff, base.rgb);

	float3	blend		=lum.rrr;
	float	L			=min(1, max(0, 10 * (lum - 0.45)));

	float3	result1		=2.0f * base.rgb * blend;
	float3	result2		=1.0f - 2.0f * (1.0f - blend) * (1.0f - base.rgb);
	float3	newColor	=lerp(result1, result2, L);

	float	A2			=mOpacity * base.a;
	float3	mixRGB		=A2 * newColor.rgb;

	mixRGB	+=((1.0f - A2) * base.rgb);

	return	float4(mixRGB, base.a);	
}


technique AmbientOcclusion
{
	pass P0
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0 AOPS();
	}
}

technique GaussianBlurX
{
	pass P0
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0 GaussianBlurXPS();
	}
}

technique GaussianBlurY
{
	pass P0
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0 GaussianBlurYPS();
	}
}

technique BilateralBlur
{
	pass pX
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0	BiLatBlurXPS();
	}

	pass pY
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0	BiLatBlurYPS();
	}
}

technique Outline
{
	pass P0
	{
		VertexShader	=compile vs_3_0 OutlineVS();
		PixelShader		=compile ps_3_0 OutlinePS();
	}
}

technique Modulate
{
	pass P0
	{
		VertexShader	=compile vs_3_0 OutlineVS();
		PixelShader		=compile ps_3_0 ModulatePS();
	}
}

technique BleachBypass
{
	pass P0
	{
		VertexShader	=compile vs_3_0 OutlineVS();
		PixelShader		=compile ps_3_0 BleachBypassPS();
	}
}

technique BloomExtract
{
	pass P0
	{
		VertexShader	=compile vs_3_0 OutlineVS();
		PixelShader		=compile ps_3_0 BloomExtractPS();
	}
}

technique BloomCombine
{
	pass P0
	{
		VertexShader	=compile vs_3_0 OutlineVS();
		PixelShader		=compile ps_3_0 BloomCombinePS();
	}
}