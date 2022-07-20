//This hlsl is for the auto generated materials from the bsp compiler
Texture1D	mDynLights : register(t3);

#include "Types.hlsli"
#include "CommonFunctions.hlsli"

cbuffer BSP : register(b5)
{
	bool		mbTextureEnabled;
	float2		mTexSize;
	uint		mPad;		//16 boundary
}

//putting these in their own cbuffer
//because of how painful arrays are in C#
cbuffer BSPStyles : register(b6)
{
	//intensity levels for the animated / switchable light styles
	half	mAniIntensities[44];
}


VVPosTex04Tex14Tex24 LightMapVS(VPosNormTex04 input)
{
	VVPosTex04Tex14Tex24	output;

	float4	worldPosition	=mul(float4(input.Position, 1), mWorld);

	output.TexCoord0.xy	=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw	=input.TexCoord0.zw;
	output.TexCoord1	=worldPosition;
	output.TexCoord2	=input.Normal;
	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord1.w	=input.Normal.w;	//alpha stored in normal w
	
	return	output;
}


VVPosTex04Tex14Tex24Tex31 VertexLitVS(VPosNormTex0Col0 input)
{
	VVPosTex04Tex14Tex24Tex31	output;

	float4	worldPosition	=mul(float4(input.Position, 1), mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0.x	=input.TexCoord0.x / mTexSize.x;
	output.TexCoord0.y	=input.TexCoord0.y / mTexSize.y;
	output.TexCoord0.z	=worldPosition.x;
	output.TexCoord0.w	=worldPosition.y;
	output.TexCoord1.x	=worldPosition.z;
	output.TexCoord1.y	=worldPosition.w;
	output.TexCoord1.z	=input.Normal.x;
	output.TexCoord1.w	=input.Normal.y;
	output.TexCoord2.x	=input.Normal.z;
	output.TexCoord2.y	=input.Color.x;
	output.TexCoord2.z	=input.Color.y;
	output.TexCoord2.w	=input.Color.z;
	output.TexCoord3.x	=input.Color.w;
	
	return	output;
}


VVPosTex04Tex14Tex24Tex31 FullBrightVS(VPosNormTex0 input)
{
	VVPosTex04Tex14Tex24Tex31	output;

	float4	worldPosition	=mul(float4(input.Position, 1), mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0.x	=input.TexCoord0.x / mTexSize.x;
	output.TexCoord0.y	=input.TexCoord0.y / mTexSize.y;
	output.TexCoord0.z	=worldPosition.x;
	output.TexCoord0.w	=worldPosition.y;
	output.TexCoord1.x	=worldPosition.z;
	output.TexCoord1.y	=worldPosition.w;
	output.TexCoord1.z	=input.Normal.x;
	output.TexCoord1.w	=input.Normal.y;
	output.TexCoord2.x	=input.Normal.z;
	output.TexCoord2.y	=1;
	output.TexCoord2.z	=1;
	output.TexCoord2.w	=1;
	output.TexCoord3.x	=1;
	
	return	output;
}


VVPosCubeTex0 SkyVS(VPosTex0 input)
{
	VVPosCubeTex0	output;

	float4	worldPosition	=mul(float4(input.Position, 1), mWorld);

	output.TexCoord0	=worldPosition.xyz;
	output.Position		=mul(mul(worldPosition, mView), mProjection);

	return	output;
}


VVPosTex04Tex14Tex24Tex34Tex44Tex54 LightMapAnimVS(VPosNormTex04Tex14Tex24Col04 input)
{
	VVPosTex04Tex14Tex24Tex34Tex44Tex54	output;

	float4	worldPosition	=mul(float4(input.Position, 1), mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0.xy	=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw	=input.TexCoord0.zw;
	output.TexCoord1	=input.TexCoord1;
	output.TexCoord2	=input.TexCoord2;
	output.TexCoord3	=input.Normal;
	output.TexCoord4	=worldPosition;
	output.TexCoord5	=float4(-1, -1, -1, -1);
	
	//really silly that I have to do this
	//initially tried using Format.R8G8B8A8_UInt to make
	//the input values as a byte4 0-255...
	//this worked, but the compiler interpreted it as a float4
	//which caused a float to int conversion on the already int
	//values.  I suppose this way it will work on 9.3 anyway
	half4	sidx	=input.Color * 255;
	
	//look up style intensities
	if(sidx.x < 44)
	{
		output.TexCoord5.x	=mAniIntensities[sidx.x];
	}
	
	//next anim style if any
	if(sidx.y < 44)
	{
		output.TexCoord5.y	=mAniIntensities[sidx.y];
	}
	
	if(sidx.z < 44)
	{
		output.TexCoord5.z	=mAniIntensities[sidx.z];
	}

	if(sidx.w < 44)
	{
		output.TexCoord5.w	=mAniIntensities[sidx.w];
	}
	
	return	output;
}


//Sm2 not happy with this
#if !defined(SM2)
float3	GetDynLight(float3 pixelPos, float3 normal)
{
	float3	nl	=0;

	for(int i=0;i < 16;i++)
	{
		float4	lCol	=mDynLights.Sample(CelSampler, float((i * 2) + 1) / 32);

		if(!any(lCol))
		{
			continue;
		}

		float4	lPos1	=mDynLights.Sample(CelSampler, float(i * 2) / 32);
		float3	lDir	=lPos1.xyz - pixelPos;
		float	atten	=saturate(1 - dot(lDir / lPos1.w, lDir / lPos1.w));

		lDir	=normalize(lDir);

		float	ang	=(dot(normal, lDir) * atten);

		ang	=max(ang, 0);
		nl	+=lCol * ang;
	}
	return	nl;
}
#endif


//regular color output pixel shaders
float4 LightMapPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	float3		color;
	
	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, input.TexCoord0.xy);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float3	lm	=mTexture1.Sample(Tex1Sampler, input.TexCoord0.zw);

#if !defined(SM2)
	lm	+=GetDynLight(input.TexCoord1, input.TexCoord2.xyz);
#endif

	color	*=lm;

	return	float4(color, input.TexCoord1.w);
}

float4 LightMapCelPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	float3		color;
	
	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, input.TexCoord0.xy);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=mTexture1.Sample(Tex1Sampler, input.TexCoord0.zw);

#if !defined(SM2)
	lm	+=GetDynLight(input.TexCoord1, input.TexCoord2.xyz);
#endif

#if defined(CELLIGHT)
	lm	=CalcCelColor(lm);
#endif

	color	*=lm;

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	return	float4(color, input.TexCoord1.w);
}

float4 VertexLitPS(VVPosTex04Tex14Tex24Tex31 input) : SV_Target
{
	float3		color;
	float2		tex0;

	tex0.x	=input.TexCoord0.x;
	tex0.y	=input.TexCoord0.y;
	
	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, tex0);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float4	worldPos;
	worldPos.x	=input.TexCoord0.z;
	worldPos.y	=input.TexCoord0.w;
	worldPos.z	=input.TexCoord1.x;
	worldPos.w	=input.TexCoord1.y;

	float3	norm;
	norm.x		=input.TexCoord1.z;
	norm.y		=input.TexCoord1.w;
	norm.z		=input.TexCoord2.x;

	float3	inColor;
	inColor.x	=input.TexCoord2.y;
	inColor.y	=input.TexCoord2.z;
	inColor.z	=input.TexCoord2.w;

	//dynamic lights	
#if !defined(SM2)
	inColor.xyz	+=GetDynLight(worldPos, norm);
#endif

	color	*=inColor;

	return	float4(color, input.TexCoord3.x);
}

float4 VertexLitCelPS(VVPosTex04Tex14Tex24Tex31 input) : SV_Target
{
	float3		color;
	float2		tex0;

	tex0.x	=input.TexCoord0.x;
	tex0.y	=input.TexCoord0.y;
	
	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, tex0);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float4	worldPos;
	worldPos.x	=input.TexCoord0.z;
	worldPos.y	=input.TexCoord0.w;
	worldPos.z	=input.TexCoord1.x;
	worldPos.w	=input.TexCoord1.y;

	float3	norm;
	norm.x		=input.TexCoord1.z;
	norm.y		=input.TexCoord1.w;
	norm.z		=input.TexCoord2.x;

	float3	light;
	light.x	=input.TexCoord2.y;
	light.y	=input.TexCoord2.z;
	light.z	=input.TexCoord2.w;
	
	//dynamic lights
#if !defined(SM2)
	float3	dynLight	=GetDynLight(worldPos, norm);
	light	+=dynLight;
#endif

	//celshade the vertex light + dyn
#if defined(CELLIGHT)
	light.xyz	=CalcCelColor(light.xyz);
#endif

	color.rgb	*=light;

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	return	float4(color, input.TexCoord3.x);
}

float4 FullBrightPixelShader(VVPosTex0 input) : SV_Target
{
	if(mbTextureEnabled)
	{
		return	mTexture0.Sample(Tex0Sampler, input.TexCoord0);
	}
	return	float4(1, 1, 1, 1);
}

float4 LightMapAnimPS(VVPosTex04Tex14Tex24Tex34Tex44Tex54 input) : SV_Target
{
	float3		color;

	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, input.TexCoord0.xy);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float3	lm			=float3(0, 0, 0);
	float3	norm		=input.TexCoord3.xyz;
	float3	worldPos	=input.TexCoord4.xyz;

	//grab style intensity
	if(input.TexCoord5.x > 0)
	{
		lm	+=(input.TexCoord5.x * mTexture1.Sample(Tex1Sampler, input.TexCoord0.zw));
	}
	if(input.TexCoord5.y > 0)
	{
		lm	+=(input.TexCoord5.y * mTexture1.Sample(Tex1Sampler, input.TexCoord1.xy));
	}
	if(input.TexCoord5.z > 0)
	{
		lm	+=(input.TexCoord5.z * mTexture1.Sample(Tex1Sampler, input.TexCoord1.zw));
	}
	if(input.TexCoord5.w > 0)
	{
		lm	+=(input.TexCoord5.w * mTexture1.Sample(Tex1Sampler, input.TexCoord2.xy));
	}

	//dyn lights
#if !defined(SM2)
	lm	+=GetDynLight(worldPos, norm);
#endif

	//Apply lighting.
	color	*=lm;
	color	=saturate(color);

	return	float4(color, input.TexCoord2.z);
}

float4 LightMapAnimCelPS(VVPosTex04Tex14Tex24Tex34Tex44Tex54 input) : SV_Target
{
	float3		color;

	if(mbTextureEnabled)
	{
		color	=mTexture0.Sample(Tex0Sampler, input.TexCoord0.xy);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float3	lm			=float3(0, 0, 0);
	float3	norm		=input.TexCoord3.xyz;
	float3	worldPos	=input.TexCoord4.xyz;

	//grab style intensity
	if(input.TexCoord5.x > 0)
	{
		lm	+=(input.TexCoord5.x * mTexture1.Sample(Tex1Sampler, input.TexCoord0.zw));
	}
	if(input.TexCoord5.y > 0)
	{
		lm	+=(input.TexCoord5.y * mTexture1.Sample(Tex1Sampler, input.TexCoord1.xy));
	}
	if(input.TexCoord5.z > 0)
	{
		lm	+=(input.TexCoord5.z * mTexture1.Sample(Tex1Sampler, input.TexCoord1.zw));
	}
	if(input.TexCoord5.w > 0)
	{
		lm	+=(input.TexCoord5.w * mTexture1.Sample(Tex1Sampler, input.TexCoord2.xy));
	}

	//dynamic
#if !defined(SM2)
	lm	+=GetDynLight(worldPos, norm);
#endif

	//cel
#if defined(CELLIGHT)
	lm	=CalcCelColor(lm);
#endif

	color.rgb	*=lm;

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	return	float4(color, input.TexCoord2.z);
}

float4 SkyPS(VVPosCubeTex0 input) : SV_Target
{
	float4	color;

	if(mbTextureEnabled)
	{
		float3	worldPosition	=input.TexCoord0;

		//calculate vector from eye to pos
		float3	eyeVec	=worldPosition - mEyePos;
	
		eyeVec	=normalize(eyeVec);

		color	=mTexture0.Sample(Tex0Sampler, eyeVec);
	}
	else
	{
		color	=float4(1, 1, 1, 1);
	}

	return	color;
}


struct TwoHalf4Targets
{
	half4	targ1, targ2;
};

//depth normal material pixel shaders
//these all spit out depth in x, material id in y
//and encoded normal in zw
TwoHalf4Targets LightMapDMNPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	TwoHalf4Targets	ret;

	ret.targ1.x		=mMaterialID;
	ret.targ1.yzw	=normalize(input.TexCoord2.xyz);
	ret.targ2		=input.TexCoord1;

	return	ret;
}

TwoHalf4Targets VertexLitDMNPS(VVPosTex04Tex14Tex24Tex31 input) : SV_Target
{
	TwoHalf4Targets	ret;

	float3	worldPos;
	worldPos.x	=input.TexCoord0.z;
	worldPos.y	=input.TexCoord0.w;
	worldPos.z	=input.TexCoord1.x;

	float3	norm;
	norm.x		=input.TexCoord1.z;
	norm.y		=input.TexCoord1.w;
	norm.z		=input.TexCoord2.x;

	ret.targ1.x		=mMaterialID;
	ret.targ1.yzw	=normalize(norm);
	ret.targ2		=float4(worldPos, 0);

	return	ret;
}

TwoHalf4Targets LightMapAnimDMNPS(VVPosTex04Tex14Tex24Tex34Tex44Tex54 input) : SV_Target
{
	float3	norm		=input.TexCoord3.xyz;
	float3	worldPos	=input.TexCoord4.xyz;

	TwoHalf4Targets	ret;

	ret.targ1.x		=mMaterialID;
	ret.targ1.yzw	=normalize(norm);
	ret.targ2		=float4(worldPos, 0);

	return	ret;
}

TwoHalf4Targets SkyDMNPS(VVPosCubeTex0 input) : SV_Target
{
	TwoHalf4Targets	ret;

	ret.targ1.x		=mMaterialID;
	ret.targ1.yzw	=float3(0, -1, 0);
	ret.targ2		=float4(0, 0, 0, 0);

	return	ret;
}


//second pass shadow draws, uses alpha
float4 LightMapShadowPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	float4	color	=float4(0.0, 0.0, 0.0, input.TexCoord1.w);

	return	ShadowColor(mbDirectional, input.TexCoord1, input.TexCoord2.xyz, color);
}

float4 VertexLitShadowPS(VVPosTex04Tex14Tex24Tex31 input) : SV_Target
{
	float4	color	=float4(0.0, 0.0, 0.0, input.TexCoord3.x);

	float4	worldPos;
	worldPos.x	=input.TexCoord0.z;
	worldPos.y	=input.TexCoord0.w;
	worldPos.z	=input.TexCoord1.x;
	worldPos.w	=input.TexCoord1.y;

	float3	norm;
	norm.x		=input.TexCoord1.z;
	norm.y		=input.TexCoord1.w;
	norm.z		=input.TexCoord2.x;

	//shadow map
	return	ShadowColor(mbDirectional, worldPos, norm, color);
}

float4 LightMapAnimShadowPS(VVPosTex04Tex14Tex24Tex34Tex44Tex54 input) : SV_Target
{
	float4	color	=float4(0.0, 0.0, 0.0, input.TexCoord2.z);

	float3	norm	=input.TexCoord3.xyz;

	//shadow map
	return	ShadowColor(mbDirectional, input.TexCoord4, norm, color);
}
/*

//techniques
technique10 LightMap
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapCelPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 VertexLightingAlpha
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 VertexLightingAlphaCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitCelPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 VertexLighting
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 VertexLightingCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitCelPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 VertexLitVS();
		PixelShader		=compile ps_5_0 VertexLitDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 VertexLitVS();
		PixelShader		=compile ps_4_1 VertexLitDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 VertexLitVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAlpha
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAlphaCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapCelPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapVS();
		PixelShader		=compile ps_5_0 LightMapDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapVS();
		PixelShader		=compile ps_4_1 LightMapDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 FullBright
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 FullBrightVS();
		PixelShader		=compile ps_5_0 VertexLitCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 FullBrightVS();
		PixelShader		=compile ps_4_1 VertexLitCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 FullBrightVS();
		PixelShader		=compile ps_4_0 VertexLitCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 FullBrightVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitCelPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 FullBrightVS();
		PixelShader		=compile ps_5_0 VertexLitShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 FullBrightVS();
		PixelShader		=compile ps_4_1 VertexLitShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 FullBrightVS();
		PixelShader		=compile ps_4_0 VertexLitShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 FullBrightVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 FullBrightVS();
		PixelShader		=compile ps_5_0 VertexLitDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 FullBrightVS();
		PixelShader		=compile ps_4_1 VertexLitDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 FullBrightVS();
		PixelShader		=compile ps_4_0 VertexLitDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 FullBrightVS();
		PixelShader		=compile ps_4_0_level_9_3 VertexLitDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAnim
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAnimAlpha
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAnimAlphaCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimCelPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 LightMapAnimCel
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimCelPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimCelPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimCelPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimCelPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass Shadow
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimShadowPS();
#endif
		SetBlendState(ShadowBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(ShadowDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 LightMapAnimVS();
		PixelShader		=compile ps_5_0 LightMapAnimDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 LightMapAnimVS();
		PixelShader		=compile ps_4_1 LightMapAnimDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 LightMapAnimVS();
		PixelShader		=compile ps_4_0_level_9_3 LightMapAnimDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 Sky
{
	pass Base
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkyVS();
		PixelShader		=compile ps_5_0 SkyPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkyVS();
		PixelShader		=compile ps_4_1 SkyPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkyVS();
		PixelShader		=compile ps_4_0 SkyPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkyVS();
		PixelShader		=compile ps_4_0_level_9_3 SkyPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
	pass DMN
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkyVS();
		PixelShader		=compile ps_5_0 SkyDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkyVS();
		PixelShader		=compile ps_4_1 SkyDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkyVS();
		PixelShader		=compile ps_4_0 SkyDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkyVS();
		PixelShader		=compile ps_4_0_level_9_3 SkyDMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}*/