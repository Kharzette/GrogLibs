//post process shaders
//ambient occlusion from flashie, an implementation of
//http://graphics.cs.williams.edu/papers/AlchemyHPG11/VV11AlchemyAO.pdf
//
//blur from some article on the interwebs I frogot
//
//edge detection from nvidia
//
#include "types.hlsli"

//gaussianblur stuff
#if !defined(SM2)
#define	RADIUS			30
#else
#define	RADIUS			7
#endif
#define	KERNEL_SIZE		(RADIUS * 2 + 1)

//post process stuff
cbuffer Post : register(b0)
{
	float2		mInvViewPort;

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

	//bilateral blur stuff
	float	mBlurFallOff;
	float	mSharpNess;
	float	mOpacity;
}

//this will probably be a KERNEL_SIZE * 4 float array on the C# side
cbuffer PostBlur : register(b1)
{
	//gaussianblur stuff
	float	mWeightsX[KERNEL_SIZE], mWeightsY[KERNEL_SIZE];
	float	mOffsetsX[KERNEL_SIZE], mOffsetsY[KERNEL_SIZE];
}

//textures
Texture2D	mNormalTex;
Texture2D	mColorTex;
Texture2D	mBlurTargetTex;

#if !defined(SM2)
#define		OUTLINE_TEX_SIZE	1024	//should match MaxOutlineColours in PostProcess.cs
Texture1D	mOutlineTex;				//lookup table for outline colors per material id
#endif

SamplerState	PointClamp : register(s0);
SamplerState	LinearClamp : register(s1);

#define	NORM_LINE_THRESHOLD	0.6


//helper functions
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
	float	goblin	=-r * r * mBlurFallOff - ddiff * ddiff * mSharpNess;
	float	w		=exp(goblin);

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


VVPos	SimpleQuadVS(VPos input)
{
	VVPos	output;

	output.Position.xyz	=input.Position.xyz;
	output.Position.w	=1.0f;

	return	output;
}

VVPos93	SimpleQuad93VS(VPos input)
{
	VVPos93	output;

	output.Position.xyz	=input.Position.xyz;
	output.Position.w	=1.0f;

	//93 needs the half pixel offset
	output.VPos.x	=((input.Position.x * 0.5) + 0.5) + mInvViewPort.x * 0.5f;
	output.VPos.y	=((-input.Position.y * 0.5) + 0.5) + mInvViewPort.y * 0.5f;
	output.VPos.z	=input.Position.z;
	output.VPos.w	=1.0f;

	return	output;
}

float4	BloomExtractPS(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / (mScreenSize / 2);	//half size rendertarget
	float4	ret	=mBlurTargetTex.Sample(LinearClamp, uv);

	return	saturate((ret - mBloomThreshold) / (1 - mBloomThreshold));
}

float4	BloomExtract93PS(VVPos93 input) : SV_Target
{
	float2	uv	=input.VPos.xy * 2;	//half size
	float4	ret	=mBlurTargetTex.Sample(LinearClamp, uv);

	return	saturate((ret - mBloomThreshold) / (1 - mBloomThreshold));
}

float4	BloomCombinePS(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / mScreenSize;

	//Look up the bloom and original base image colors.
	float4	bloom	=mBlurTargetTex.Sample(LinearClamp, uv);
	float4	base	=mColorTex.Sample(PointClamp, uv);
    
	//Adjust color saturation and intensity.
	bloom	=AdjustSaturation(bloom, mBloomSaturation) * mBloomIntensity;
	base	=AdjustSaturation(base, mBaseSaturation) * mBaseIntensity;
    
	//Darken down the base image in areas where there is a lot of bloom,
	//to prevent things looking excessively burned-out.
	base	*=(1 - saturate(bloom));
    
	//Combine the two images.
	return	base + bloom;
}

float4	BloomCombine93PS(VVPos93 input) : SV_Target
{
	float2	uv	=input.VPos.xy;

	//Look up the bloom and original base image colors.
	float4	bloom	=mBlurTargetTex.Sample(LinearClamp, uv);
	float4	base	=mColorTex.Sample(PointClamp, uv);
    
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
	float2	uv	=input.Position.xy / mScreenSize;
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		float2	uvOfs	=uv;

		uvOfs.x	+=mOffsetsX[i];

		ret	+=mBlurTargetTex.Sample(LinearClamp, uv + uvOfs) * mWeightsX[i];
	}
	return	ret;
}

float4	GaussianBlurX93PS(VVPos93 input) : SV_Target
{
	float2	uv	=input.VPos.xy;
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		float2	uvOfs	=uv;

		uvOfs.x	+=mOffsetsX[i];

		ret	+=mBlurTargetTex.Sample(LinearClamp, uv + uvOfs) * mWeightsX[i];
	}
	return	ret;
}

float4	GaussianBlurYPS(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / mScreenSize;
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		float2	uvOfs	=uv;

		uvOfs.y	+=mOffsetsY[i];

		ret	+=mBlurTargetTex.Sample(LinearClamp, uv + uvOfs) * mWeightsY[i];
	}
	return	ret;
}

float4	GaussianBlurY93PS(VVPos93 input) : SV_Target
{
	float2	uv	=input.VPos.xy;
	float4	ret	=float4(0, 0, 0, 0);

	for(int i=0;i < KERNEL_SIZE;++i)
	{
		float2	uvOfs	=uv;

		uvOfs.y	+=mOffsetsY[i];

		ret	+=mBlurTargetTex.Sample(LinearClamp, uv + uvOfs) * mWeightsY[i];
	}
	return	ret;
}

float4	BiLatBlurXPS(VVPos input) : SV_Target
{
	float	b			=0;
	float	w_total		=0;
	float2	screenCoord	=input.Position.xy / mScreenSize;
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
	float2	screenCoord	=input.Position.xy / mScreenSize;
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
float4	DebugMatIDDraw(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / mScreenSize;
	half4	dmn	=mNormalTex.Sample(PointClamp, uv);

	float	matShade	=dmn.y * 0.01;

	return	float4(matShade, matShade, matShade, 1);
}

//draws the depth in shades for debuggery
float4	DebugDepthDraw(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / mScreenSize;
	half4	dmn	=mNormalTex.Sample(PointClamp, uv);

	dmn.x	/=1000.0;

	return	float4(dmn.x, dmn.x, dmn.x, 1);
}

//draws the normals for debuggery
/*
float4	DebugNormalDraw(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy / mScreenSize;
	half4	dmn	=mNormalTex.Sample(PointClamp, uv);

	half3	norm	=DecodeNormal(dmn.zw, mView._m02_m12_m22);

	return	float4(norm.x, norm.y, norm.z, 1);
}*/


//for > 9 feature levels
float4	OutlinePS(VVPos input) : SV_Target
{
	float2	ox	=float2(mTexelSteps / mScreenSize.x, 0.0);
	float2	oy	=float2(0.0, mTexelSteps / mScreenSize.y);
	
	float2	uv	=input.Position.xy / mScreenSize;

	//only do 5 samples for sm2, 4 extra for > SM2
	half4	center, up, left, right, down;
	half4	upLeft, upRight, downLeft, downRight;

	//read center
	center	=mNormalTex.Sample(PointClamp, uv);

	//grab material colour
	//need to ifdef this, SM2 errors even though
	//this function isn't used
#if !defined(SM2)
	float4	matColour	=mOutlineTex.Sample(PointClamp,
							(center.x / OUTLINE_TEX_SIZE));
#else
	float4	matColour	=float4(0, 0, 0, 1);
#endif

#if defined(LINE_OCCLUSION_TEST)
	//check for material ID 0, this is a hack for stuff like
	//particles that need to occlude a line
	if(center.x == 0)
	{
		return	float4(1, 1, 1, 1);
	}
#endif

	//one texel around center
	//format is x matid, yzw normal
	up			=mNormalTex.Sample(PointClamp, uv + oy);
	left		=mNormalTex.Sample(PointClamp, uv - ox);
	right		=mNormalTex.Sample(PointClamp, uv + ox);
	down		=mNormalTex.Sample(PointClamp, uv - oy);

	//corners are extra processing done for > SM2
	upLeft		=mNormalTex.Sample(PointClamp, uv - ox + oy);
	upRight		=mNormalTex.Sample(PointClamp, uv + ox + oy);
	downLeft	=mNormalTex.Sample(PointClamp, uv - ox - oy);
	downRight	=mNormalTex.Sample(PointClamp, uv + ox - oy);

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

	//normal stuff, too many instructions for sm2
	half3	centerNorm		=center.yzw;
	half3	upNorm			=up.yzw;
	half3	leftNorm		=left.yzw;
	half3	rightNorm		=right.yzw;
	half3	downNorm		=down.yzw;
	half3	upLeftNorm		=upLeft.yzw;
	half3	upRightNorm		=upRight.yzw;
	half3	downLeftNorm	=downLeft.yzw;
	half3	downRightNorm	=downRight.yzw;

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
		return	matColour;
	}

	float4	matDiff1;

	matDiff1.x	=center.x - up.x;
	matDiff1.y	=center.x - right.x;
	matDiff1.z	=center.x - left.x;
	matDiff1.w	=center.x - down.x;

	matDiff1	=abs(matDiff1);

	//extra corners for > SM2
	float4	matDiff2;

	matDiff2.x	=center.x - upLeft.x;
	matDiff2.y	=center.x - upRight.x;
	matDiff2.z	=center.x - downLeft.x;
	matDiff2.w	=center.x - downRight.x;

	matDiff1	+=abs(matDiff2);

	//if any material differences do black line
	if(any(matDiff1))
	{
		return	matColour;
	}

	//if the normals are fairly similar, and no mat boundary,
	//check if the samples all lie in the same plane
	half3	centerPos	=mColorTex.Sample(PointClamp, uv).xyz;
	float	centerDist	=dot(centerPos, centerNorm);

	half3	upPos		=mColorTex.Sample(PointClamp, uv + oy).xyz;
	half3	leftPos		=mColorTex.Sample(PointClamp, uv - ox).xyz;
	half3	rightPos	=mColorTex.Sample(PointClamp, uv + ox).xyz;
	half3	downPos		=mColorTex.Sample(PointClamp, uv - oy).xyz;

	half3	upLeftPos		=mColorTex.Sample(PointClamp, uv - ox + oy).xyz;
	half3	upRightPos		=mColorTex.Sample(PointClamp, uv + ox + oy).xyz;
	half3	downLeftPos		=mColorTex.Sample(PointClamp, uv - ox - oy).xyz;
	half3	downRightPos	=mColorTex.Sample(PointClamp, uv + ox - oy).xyz;

	float	planeDist	=dot(upPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(leftPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(rightPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(downPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(upLeftPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(upRightPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(downLeftPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	planeDist	=dot(downRightPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	matColour;
	}

	return	float4(1, 1, 1, 1);
}

//for 9_3 feature levels
float4	Outline93PS(VVPos93 input) : SV_Target
{
	float2	ox	=float2(mTexelSteps / mScreenSize.x, 0.0);
	float2	oy	=float2(0.0, mTexelSteps / mScreenSize.y);
	
	float2	uv	=input.VPos.xy;

	//only do 5 samples for sm2
	half4	center, up, left, right, down;

	//read center
	center	=mNormalTex.Sample(PointClamp, uv);

	//one texel around center
	//format is x depth, y matid, zw normal
	up			=mNormalTex.Sample(PointClamp, uv + oy);
	up			=mNormalTex.Sample(PointClamp, uv + oy);
	left		=mNormalTex.Sample(PointClamp, uv - ox);
	right		=mNormalTex.Sample(PointClamp, uv + ox);
	down		=mNormalTex.Sample(PointClamp, uv - oy);

	half3	centerNorm		=center.yzw;
	half3	upNorm			=up.yzw;
	half3	leftNorm		=left.yzw;
	half3	rightNorm		=right.yzw;
	half3	downNorm		=down.yzw;

	float4	normDots0;
	normDots0.x	=dot(centerNorm, upNorm);
	normDots0.y	=dot(centerNorm, rightNorm);
	normDots0.z	=dot(centerNorm, leftNorm);
	normDots0.w	=dot(centerNorm, downNorm);

	normDots0	=step(normDots0, NORM_LINE_THRESHOLD);

	//can early out with the normal test
	if(any(normDots0))
	{
		return	float4(0, 0, 0, 1);
	}

	float4	matDiff1;

	matDiff1.x	=center.x - up.x;
	matDiff1.y	=center.x - right.x;
	matDiff1.z	=center.x - left.x;
	matDiff1.w	=center.x - down.x;

	matDiff1	=abs(matDiff1);

	//if any material differences do black line
	if(any(matDiff1))
	{
		return	float4(0, 0, 0, 1);
	}

	//if the normals are fairly similar, and no mat boundary,
	//check if the samples all lie in the same plane
	half3	centerPos	=mColorTex.Sample(PointClamp, uv).xyz;
	float	centerDist	=dot(centerPos, centerNorm);

	half3	upPos		=mColorTex.Sample(PointClamp, uv + oy).xyz;
	half3	leftPos		=mColorTex.Sample(PointClamp, uv - ox).xyz;
	half3	rightPos	=mColorTex.Sample(PointClamp, uv + ox).xyz;
	half3	downPos		=mColorTex.Sample(PointClamp, uv - oy).xyz;
	float	planeDist	=dot(upPos, centerNorm) - centerDist;

	if(abs(planeDist) > mThreshold)
	{
		return	float4(0, 0, 0, 1);
	}

	planeDist	=dot(leftPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	float4(0, 0, 0, 1);
	}

	planeDist	=dot(rightPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	float4(0, 0, 0, 1);
	}

	planeDist	=dot(downPos, centerNorm) - centerDist;
	if(abs(planeDist) > mThreshold)
	{
		return	float4(0, 0, 0, 1);
	}

    return	float4(1, 1, 1, 1);
}


//for > 9_3
float4	ModulatePS(VVPos input) : SV_Target
{
	float2	uv	=input.Position.xy;

	uv	/=mScreenSize;

	float4	color	=mColorTex.Sample(PointClamp, uv);
	float4	color2	=mBlurTargetTex.Sample(LinearClamp, uv);

	color	*=color2;

	return	float4(color.xyz, 1);
}

float4	Modulate93PS(VVPos93 input) : SV_Target
{
	float2	uv	=input.VPos.xy;

	float4	color	=mColorTex.Sample(PointClamp, uv);
	float4	color2	=mBlurTargetTex.Sample(LinearClamp, uv);

	color	*=color2;

	return	float4(color.xyz, 1);
}


float4	BleachBypassPS(VVPos input) : SV_Target
{
	float2	uv			=input.Position.xy;

	uv	/=mScreenSize;

	float4	base		=saturate(mColorTex.Sample(PointClamp, uv));
	float3	lumCoeff	=float3(0.25, 0.65, 0.1);
	float	lum			=dot(lumCoeff, base.rgb);

	float3	blend		=lum.rrr;
	float	L			=min(1, max(0, 10 * (lum - 0.45)));

	float3	result1		=2.0f * base.rgb * blend;
	float3	result2		=1.0f - 2.0f * (1.0f - blend) * (1.0f - base.rgb);
	float3	newColor	=lerp(result1, result2, L);

//	float	A2			=mOpacity * base.a;
	float	A2			=mOpacity;
	float3	mixRGB		=A2 * newColor.rgb;

	mixRGB	+=((1.0f - A2) * base.rgb);

//	return	float4(mixRGB, base.a);	
	return	float4(mixRGB, 1);	
}

float4	BleachBypass93PS(VVPos93 input) : SV_Target
{
	float2	uv			=input.VPos.xy;
	float4	base		=mColorTex.Sample(PointClamp, uv);
	float3	lumCoeff	=float3(0.25, 0.65, 0.1);
	float	lum			=dot(lumCoeff, base.rgb);

	float3	blend		=lum.rrr;
	float	L			=min(1, max(0, 10 * (lum - 0.45)));

	float3	result1		=2.0f * base.rgb * blend;
	float3	result2		=1.0f - 2.0f * (1.0f - blend) * (1.0f - base.rgb);
	float3	newColor	=lerp(result1, result2, L);

//	float	A2			=mOpacity * base.a;
	float	A2			=mOpacity;
	float3	mixRGB		=A2 * newColor.rgb;

	mixRGB	+=((1.0f - A2) * base.rgb);

//	return	float4(mixRGB, base.a);	
	return	float4(mixRGB, 1);	
}


technique10 GaussianBlurX
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 GaussianBlurXPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 GaussianBlurXPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 GaussianBlurXPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 GaussianBlurX93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 GaussianBlurY
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 GaussianBlurYPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 GaussianBlurYPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 GaussianBlurYPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 GaussianBlurY93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

#if !defined(SM2)
technique10 BilateralBlur
{
	pass pX
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0	BiLatBlurXPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1	BiLatBlurXPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0	BiLatBlurXPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 BiLatBlurX93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}

	pass pY
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0	BiLatBlurYPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1	BiLatBlurYPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0	BiLatBlurYPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 BiLatBlurY93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}
#endif

technique10 BloomExtract
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 BloomExtractPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 BloomExtractPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 BloomExtractPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 BloomExtract93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 BloomCombine
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 BloomCombinePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 BloomCombinePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 BloomCombinePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 BloomCombine93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 Outline
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 OutlinePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 OutlinePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 OutlinePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 Outline93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 BleachBypass
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 BleachBypassPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 BleachBypassPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 BleachBypassPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 BleachBypass93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 Modulate
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SimpleQuadVS();
		PixelShader		=compile ps_5_0 ModulatePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SimpleQuadVS();
		PixelShader		=compile ps_4_1 ModulatePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SimpleQuadVS();
		PixelShader		=compile ps_4_0 ModulatePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SimpleQuad93VS();
		PixelShader		=compile ps_4_0_level_9_3 Modulate93PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}