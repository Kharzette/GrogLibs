//post process shaders
//ambient occlusion from flashie, an implementation of
//http://graphics.cs.williams.edu/papers/AlchemyHPG11/VV11AlchemyAO.pdf
//
//blur from some article on the interwebs

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
float	mWeights[KERNEL_SIZE];
float2	mOffsets[KERNEL_SIZE];
texture	mBlurTargetTex;

//bilateral blur stuff
float	mBlurFallOff;
float	mSharpNess;
float	mBlurRadius	=7;


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
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
//	AddressU	=Wrap;
//	AddressV	=Wrap;
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

float4	GaussianBlurPS(VTex0Tex13VPos input) : COLOR0
{
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		ret	+=tex2D(BlurTargetSampler, input.TexCoord0 + mOffsets[i]) * mWeights[i];
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


technique AmbientOcclusion
{
	pass P0
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0 AOPS();
	}
}

technique GaussianBlur
{
	pass P0
	{
		VertexShader	=compile vs_3_0 AOVS();
		PixelShader		=compile ps_3_0 GaussianBlurPS();
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