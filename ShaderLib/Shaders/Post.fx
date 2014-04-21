//post process shaders
//ambient occlusion from flashie, an implementation of
//http://graphics.cs.williams.edu/papers/AlchemyHPG11/VV11AlchemyAO.pdf
//
//blur from some article on the interwebs I frogot
//
//edge detection from nvidia
//

//post process stuff
float2		mInvViewPort;
float3		mFrustCorners[4];
float		mFarClip;
float3		mFrustRay;
float		mRandTexSize	=64;

//textures
Texture2D	mNormalTex;
Texture2D	mRandTex;
Texture2D	mColorTex;
Texture2D	mBlurTargetTex;

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "RenderStates.fxh"

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
#define	NORM_LINE_THRESHOLD	0.8

//ambient occlusion params
#define		NUMSAMPLES		12
#define		SamplesPerPass	4
float		mBias			=0.01;
float		mEpsilon		=0.00001;
float		mIntensityScale	=1;
float		mContrast		=1;
float		mRadius			=1;
float		mProjConst		=50.0;

float2		mSamples[NUMSAMPLES];

//gaussianblur stuff
#define	RADIUS			30
#define	KERNEL_SIZE		(RADIUS * 2 + 1)
float	mWeightsX[KERNEL_SIZE], mWeightsY[KERNEL_SIZE];
float2	mOffsetsX[KERNEL_SIZE], mOffsetsY[KERNEL_SIZE];

//bilateral blur stuff
float	mBlurFallOff;
float	mSharpNess;
float	mBlurRadius	=7;
float	mOpacity;


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
	float	depth	=mNormalTex.Sample(PointClamp, texCoord).w;
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
	return	mNormalTex.Sample(PointClamp, uv).w;
}

float BlurFunction(float2 uv, float r, float center_c,
					float center_d, inout float w_total)
{
	float	c	=mBlurTargetTex.Sample(LinearClamp, uv);
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


VVPosTex0Tex13	AOVS(VPosTex0 input)
{
	VVPosTex0Tex13	output;

	output.Position.x	=input.Position.x - mInvViewPort.x * 0.5f;
	output.Position.y	=input.Position.y + mInvViewPort.y * 0.5f;
	output.Position.z	=input.Position.z;
	output.Position.w	=1.0f;

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=GetFrustumRay(input.TexCoord0);

	return	output;
}


VVPosTex0	OutlineVS(VPosTex0 input)
{
	VVPosTex0	output;

	output.Position.x	=input.Position.x - mInvViewPort.x * 0.5f;
	output.Position.y	=input.Position.y + mInvViewPort.y * 0.5f;
	output.Position.z	=input.Position.z;
	output.Position.w	=1.0f;

	output.TexCoord0	=input.TexCoord0;

	return	output;
}


float4	AOPS(VVPosTex0Tex13 input) : SV_Target
{
	float2	ssc	=input.TexCoord0;

	float4	ndepth	=mNormalTex.Sample(PointClamp, ssc);
	float	depth	=ndepth.w;
	float3	cPos	=input.TexCoord1 * depth;
	float3	cNormal	=normalize(DecodeNormal(ndepth));
	float	cBias	=mBias;

	float	invNumSamples	=1.0f / SamplesPerPass;

	float	ssr	=mProjConst * mRadius / (depth * mFarClip);

	float2	randomSpin	=mRandTex.Sample(PointWrap, input.Position.xy * mInvViewPort * mRandTexSize).rg;

	float	amount	=0;
	for(int i=0;i < NUMSAMPLES;++i)
	{
		float2	unitOffset	=reflect(mSamples[i], randomSpin);
		float2	offset		=float2(unitOffset * ssr);
		float2	ssp			=offset + ssc;
		float	sDepth		=mNormalTex.Sample(PointClamp, ssp).w;

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

float4	BloomExtractPS(VVPos input) : SV_Target
{
	float4	ret	=mBlurTargetTex.Sample(LinearClamp, input.Position.xy);

	return	saturate((ret - mBloomThreshold) / (1 - mBloomThreshold));
}

float4	BloomCombinePS(VVPos input) : SV_Target
{
	//Look up the bloom and original base image colors.
	float4	bloom	=mBlurTargetTex.Sample(LinearClamp, input.Position.xy);
	float4	base	=mColorTex.Sample(PointClamp, input.Position.xy);
    
	//Adjust color saturation and intensity.
	bloom	=AdjustSaturation(bloom, mBloomSaturation) * mBloomIntensity;
	base	=AdjustSaturation(base, mBaseSaturation) * mBaseIntensity;
    
	//Darken down the base image in areas where there is a lot of bloom,
	//to prevent things looking excessively burned-out.
	base	*=(1 - saturate(bloom));
    
	//Combine the two images.
	return	base + bloom;
}

float4	GaussianBlurXPS(VVPos input) : SV_Target
{
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		ret	+=mBlurTargetTex.Sample(LinearClamp, input.Position.xy + mOffsetsX[i]) * mWeightsX[i];
	}
	return	ret;
}

float4	GaussianBlurYPS(VVPos input) : SV_Target
{
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		ret	+=mBlurTargetTex.Sample(LinearClamp, input.Position.xy + mOffsetsY[i]) * mWeightsY[i];
	}
	return	ret;
}

float4	BiLatBlurXPS(VVPos input) : SV_Target
{
	float	b			=0;
	float	w_total		=0;
	float2	screenCoord	=input.Position.xy;
	float	center_c	=mBlurTargetTex.Sample(LinearClamp, screenCoord);
	float	center_d	=fetch_eye_z(screenCoord);

	for(float r = -RADIUS;r <= RADIUS;++r)
	{
		float2	uv	=screenCoord + float2(r * mInvViewPort.x, 0);
		b			+=BlurFunction(uv, r, center_c, center_d, w_total);
	}

	return	b / w_total;
}

float4	BiLatBlurYPS(VVPos input) : SV_Target
{
	float	b			=0;
	float	w_total		=0;
	float2	screenCoord	=input.Position.xy;
	float	center_c	=mBlurTargetTex.Sample(LinearClamp, screenCoord);
	float	center_d	=fetch_eye_z(screenCoord);

	for(float r = -RADIUS;r <= RADIUS;++r)
	{
		float2	uv	=screenCoord + float2(0, r * mInvViewPort.y);
		b			+=BlurFunction(uv, r, center_c, center_d, w_total);
	}

	return	b / w_total * mColorTex.Sample(PointClamp, screenCoord);
}

//draws the material id in shades for debuggery
float4	DebugMatIDDraw(VVPosTex0 input) : SV_Target
{
	half4	dmn	=mNormalTex.Sample(PointClamp, input.TexCoord0);

	float	matShade	=dmn.y * 0.01;

	return	float4(matShade, matShade, matShade, 1);
}

//draws the depth in shades for debuggery
float4	DebugDepthDraw(VVPosTex0 input) : SV_Target
{
	half4	dmn	=mNormalTex.Sample(PointClamp, input.TexCoord0);

	dmn.x	/=1000.0;

	return	float4(dmn.x, dmn.x, dmn.x, 1);
}

//draws the normals for debuggery
float4	DebugNormalDraw(VVPosTex0 input) : SV_Target
{
	half4	dmn	=mNormalTex.Sample(PointClamp, input.TexCoord0);

	half3	norm	=DecodeNormal(dmn.zw);

	return	float4(norm.x, norm.y, norm.z, 1);
}

float4	OutlinePS(VVPosTex0 input) : SV_Target
{
	float2	ox	=float2(mTexelSteps / mScreenSize.x, 0.0);
	float2	oy	=float2(0.0, mTexelSteps / mScreenSize.y);
	
	float2	uv	=input.Position.xy;

	//only do 5 samples for sm2
	half4	center, up, left, right, down;
#if !defined(SM2)
	half4	upLeft, upRight, downLeft, downRight;
#endif

	//read center
	center	=mNormalTex.Sample(PointClamp, uv);

#if defined(LINE_OCCLUSION_TEST)
	//check for material ID 0, this is a hack for stuff like
	//particles that need to occlude a line
	if(center.y == 0)
	{
		return	float4(1, 1, 1, 1);
	}
#endif

	//one texel around center
	//format is x depth, y matid, zw normal
	up			=mNormalTex.Sample(PointClamp, uv + oy);
	up			=mNormalTex.Sample(PointClamp, uv + oy);
	left		=mNormalTex.Sample(PointClamp, uv - ox);
	right		=mNormalTex.Sample(PointClamp, uv + ox);
	down		=mNormalTex.Sample(PointClamp, uv - oy);
#if !defined(SM2)
	upLeft		=mNormalTex.Sample(PointClamp, uv - ox + oy);
	upRight		=mNormalTex.Sample(PointClamp, uv + ox + oy);
	downLeft	=mNormalTex.Sample(PointClamp, uv - ox - oy);
	downRight	=mNormalTex.Sample(PointClamp, uv + ox - oy);
#endif

#if defined(LINE_OCCLUSION_TEST)
	//check for material ID 0, this is a hack for stuff like
	//particles that need to occlude a line
	half4	zeroTest1, zeroTest2;

	zeroTest1.x	=upLeft.y;
	zeroTest1.y	=up.y;
	zeroTest1.z	=upRight.y;
	zeroTest1.w	=left.y;
	zeroTest2.x	=right.y;
	zeroTest2.y	=downLeft.y;
	zeroTest2.z	=down.y;
	zeroTest2.w	=downRight.y;

	if(!all(zeroTest1))
	{
		return	float4(1, 1, 1, 1);
	}
	if(!all(zeroTest2))
	{
		return	float4(1, 1, 1, 1);
	}
#endif

	//normal stuff is too many instructions for sm2
#if !defined(SM2)
	half3	centerNorm		=DecodeNormal(center.zw);
	half3	upNorm			=DecodeNormal(up.zw);
	half3	leftNorm		=DecodeNormal(left.zw);
	half3	rightNorm		=DecodeNormal(right.zw);
	half3	downNorm		=DecodeNormal(down.zw);
	half3	upLeftNorm		=DecodeNormal(upLeft.zw);
	half3	upRightNorm		=DecodeNormal(upRight.zw);
	half3	downLeftNorm	=DecodeNormal(downLeft.zw);
	half3	downRightNorm	=DecodeNormal(downRight.zw);

	float4	normDots0;
	float4	normDots1;

	normDots0.x	=dot(centerNorm, upNorm);
	normDots0.y	=dot(centerNorm, rightNorm);
	normDots0.z	=dot(centerNorm, leftNorm);
	normDots0.w	=dot(centerNorm, downNorm);
	normDots1.x	=dot(centerNorm, upLeftNorm);
	normDots1.y	=dot(centerNorm, upRightNorm);
	normDots1.z	=dot(centerNorm, downLeftNorm);
	normDots1.w	=dot(centerNorm, downRightNorm);

	normDots0	=step(normDots0, NORM_LINE_THRESHOLD);
	normDots0	+=step(normDots1, NORM_LINE_THRESHOLD);

	//can early out with the normal test
	if(any(normDots0))
	{
		return	float4(0, 0, 0, 1);
	}
#endif


	float4	matDiff1;

	matDiff1.x	=center.y - up.y;
	matDiff1.y	=center.y - right.y;
	matDiff1.z	=center.y - left.y;
	matDiff1.w	=center.y - down.y;

	matDiff1	=abs(matDiff1);

#if !defined(SM2)
	float4	matDiff2;

	matDiff2.x	=center.y - upLeft.y;
	matDiff2.y	=center.y - upRight.y;
	matDiff2.z	=center.y - downLeft.y;
	matDiff2.w	=center.y - downRight.y;

	matDiff1	+=abs(matDiff2);
#endif

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

	sx	+=down.x * K01;
	sx	+=up.x * K21;
	sy	+=left.x * K01;
	sy	+=right.x * K21;

	//these are all optimized out
//	sy	+=down.x * K10;
//	sx	+=left.x * K10;
//	sx	+=center.x * K11;
//	sy	+=center.x * K11;
//	sx	+=right.x * K12;
//	sy	+=up.x * K12;

#if !defined(SM2)
	sx	+=downLeft.x * K00;
	sy	+=downLeft.x * K00;
	sx	+=downRight.x * K02;
	sy	+=downRight.x * K20;
	sx	+=upLeft.x * K20;
	sy	+=upLeft.x * K02;
	sx	+=upRight.x * K22; 
	sy	+=upRight.x * K22;
#endif

	float	dist	=sqrt(sx * sx + sy * sy);

	//if there's no material boundary, bias
	//heavily toward no outline, this helps prevent
	//steeply oblique to screen polys keep from going
	//super black from the outliner freaking out
	if(!any(matDiff1))
	{
		dist	-=50;
	}
	float	result	=1;
	
	if(dist > mThreshold)
	{
		result	=0;
	}

    return	float4(result, result, result, 1);
}


float4	ModulatePS(VVPosTex0 input) : SV_Target
{
	float4	color	=mColorTex.Sample(PointClamp, input.Position.xy);
	float4	color2	=mBlurTargetTex.Sample(LinearClamp, input.Position.xy);

	color	*=color2;

	return	float4(color.xyz, 1);
}


float4	BleachBypassPS(VVPosTex0 input) : SV_Target
{
	float4	base		=mColorTex.Sample(PointClamp, input.Position.xy);
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


//alot of these are too beefy for SM2
#if !defined(SM2)
technique10 AmbientOcclusion
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 AOVS();
		PixelShader		=compile ps_5_0 AOPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 AOVS();
		PixelShader		=compile ps_4_1 AOPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 AOVS();
		PixelShader		=compile ps_4_0 AOPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 AOVS();
		PixelShader		=compile ps_4_0_level_9_3 AOPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 GaussianBlurX
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 AOVS();
		PixelShader		=compile ps_5_0 GaussianBlurXPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 AOVS();
		PixelShader		=compile ps_4_1 GaussianBlurXPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 AOVS();
		PixelShader		=compile ps_4_0 GaussianBlurXPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 AOVS();
		PixelShader		=compile ps_4_0_level_9_3 GaussianBlurXPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 GaussianBlurY
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 AOVS();
		PixelShader		=compile ps_5_0 GaussianBlurYPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 AOVS();
		PixelShader		=compile ps_4_1 GaussianBlurYPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 AOVS();
		PixelShader		=compile ps_4_0 GaussianBlurYPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 AOVS();
		PixelShader		=compile ps_4_0_level_9_3 GaussianBlurYPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 BilateralBlur
{
	pass pX
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 AOVS();
		PixelShader		=compile ps_5_0	BiLatBlurXPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 AOVS();
		PixelShader		=compile ps_4_1	BiLatBlurXPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 AOVS();
		PixelShader		=compile ps_4_0	BiLatBlurXPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 AOVS();
		PixelShader		=compile ps_4_0_level_9_3 BiLatBlurXPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}

	pass pY
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 AOVS();
		PixelShader		=compile ps_5_0	BiLatBlurYPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 AOVS();
		PixelShader		=compile ps_4_1	BiLatBlurYPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 AOVS();
		PixelShader		=compile ps_4_0	BiLatBlurYPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 AOVS();
		PixelShader		=compile ps_4_0_level_9_3 BiLatBlurYPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 BloomExtract
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 OutlineVS();
		PixelShader		=compile ps_5_0 BloomExtractPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 OutlineVS();
		PixelShader		=compile ps_4_1 BloomExtractPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 OutlineVS();
		PixelShader		=compile ps_4_0 BloomExtractPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 OutlineVS();
		PixelShader		=compile ps_4_0_level_9_3 BloomExtractPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 BloomCombine
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 OutlineVS();
		PixelShader		=compile ps_5_0 BloomCombinePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 OutlineVS();
		PixelShader		=compile ps_4_1 BloomCombinePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 OutlineVS();
		PixelShader		=compile ps_4_0 BloomCombinePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 OutlineVS();
		PixelShader		=compile ps_4_0_level_9_3 BloomCombinePS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}
#endif

technique10 Outline
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 OutlineVS();
		PixelShader		=compile ps_5_0 OutlinePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 OutlineVS();
		PixelShader		=compile ps_4_1 OutlinePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 OutlineVS();
		PixelShader		=compile ps_4_0 OutlinePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 OutlineVS();
		PixelShader		=compile ps_4_0_level_9_3 OutlinePS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 BleachBypass
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 OutlineVS();
		PixelShader		=compile ps_5_0 BleachBypassPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 OutlineVS();
		PixelShader		=compile ps_4_1 BleachBypassPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 OutlineVS();
		PixelShader		=compile ps_4_0 BleachBypassPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 OutlineVS();
		PixelShader		=compile ps_4_0_level_9_3 BleachBypassPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 Modulate
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 OutlineVS();
		PixelShader		=compile ps_5_0 ModulatePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 OutlineVS();
		PixelShader		=compile ps_4_1 ModulatePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 OutlineVS();
		PixelShader		=compile ps_4_0 ModulatePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 OutlineVS();
		PixelShader		=compile ps_4_0_level_9_3 ModulatePS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}