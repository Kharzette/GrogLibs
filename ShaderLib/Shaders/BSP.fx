//This fx is for the auto generated materials from the bsp compiler
texture2D	mTexture;
texture2D	mLightMap;
texture1D	mDynLights;
bool		mbTextureEnabled;
float2		mTexSize;

//mat id
int	mMaterialID;

//intensity levels for the animated / switchable light styles
half	mAniIntensities[44];

#define	POINTTEXTURES	1

#include "Types.fxh"
#include "CommonFunctions.fxh"


VPosTex03 DepthVS(VPos input)
{
	VPosTex03	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=worldPosition;
	
	return	output;
}


VPosTex04Tex14Tex24 LightMapVS(VPosNormTex04 input)
{
	VPosTex04Tex14Tex24	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.TexCoord0.xy	=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw	=input.TexCoord0.zw;
	output.TexCoord1	=worldPosition;
	output.TexCoord2	=float4(input.Normal, 0);
	output.Position		=mul(mul(worldPosition, mView), mProjection);
	
	return	output;
}


VPosTex04Tex14Tex24 LightMapAlphaVS(VPosNormTex04Col0 input)
{
	VPosTex04Tex14Tex24	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0.xy	=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw	=input.TexCoord0.zw;
	output.TexCoord1	=worldPosition;
	output.TexCoord2	=float4(input.Normal, 0);
	output.TexCoord1.w	=input.Color.w;
	
	return	output;
}


VPosTex04Tex14Tex24Tex31 VertexLitVS(VPosNormTex0Col0 input)
{
	VPosTex04Tex14Tex24Tex31	output;

	float4	worldPosition	=mul(input.Position, mWorld);

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


VPosTex0Tex14 FullBrightVS(VPosTex0 input)
{
	VPosTex0Tex14	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0 / mTexSize;
	output.TexCoord1	=worldPosition;

	return	output;
}


VPosCubeTex0 SkyVS(VPosTex0 input)
{
	VPosCubeTex0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.TexCoord0	=worldPosition.xyz;
	output.Position		=mul(mul(worldPosition, mView), mProjection);

	return	output;
}


VPosTex04Tex14Tex24Tex34Tex44Tex54 LightMapAnimVS(VPosNormBlendTex04Tex14Tex24 input)
{
	VPosTex04Tex14Tex24Tex34Tex44Tex54	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0.xy	=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw	=input.TexCoord0.zw;
	output.TexCoord1	=input.TexCoord1;
	output.TexCoord2	=input.TexCoord2;
	output.TexCoord3	=float4(input.Normal.xyz, 0);
	output.TexCoord4	=worldPosition;
	output.TexCoord5	=float4(-1, -1, -1, -1);
	
	float4	sidx	=input.Blend0;
	
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


sampler TextureSampler = sampler_state
{
	Texture		=mTexture;

#if defined(POINTTEXTURES)
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
#else
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
#endif

	AddressU	=Wrap;
	AddressV	=Wrap;
};


sampler LightMapSampler = sampler_state
{
    Texture	=mLightMap;

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

    AddressU	=Clamp;
    AddressV	=Clamp;
};


sampler SkySampler = sampler_state
{
	Texture		=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Clamp;
	AddressV	=Clamp;
	AddressW	=Clamp;
};


//Sm2 not happy with this
#if !defined(SM2)
sampler1D DynLightSampler = sampler_state
{
	Texture	=mDynLights;

	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;

	AddressU	=Clamp;
};

float3	GetDynLight(float3 pixelPos, float3 normal)
{
	float3	nl	=0;

	for(int i=0;i < 16;i++)
	{
		float4	lPos1	=tex1D(DynLightSampler, float(i * 2) / 32);
		float4	lCol	=tex1D(DynLightSampler, float((i * 2) + 1) / 32);
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


float4 LightMapPS(VTex04Tex14Tex24 input) : COLOR0
{
	float3		color;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord0.zw);

#if !defined(SM2)
	lm	+=GetDynLight(input.TexCoord1, input.TexCoord2.xyz);
#endif

	color	*=lm;
	color	=pow(abs(color), 1 / 2.2);

	return	float4(color, input.TexCoord1.w);
}


half4 LightMapDMNPS(VTex04Tex14Tex24 input) : COLOR0
{
	half4	ret;

	ret.x	=distance(input.TexCoord1, mEyePos);
	ret.y	=mMaterialID;
	ret.zw	=EncodeNormal(input.TexCoord2.xyz);

	return	ret;
}


float4 LightMapCelPS(VTex04Tex14Tex24 input) : COLOR0
{
	float3		color;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord0.zw);

#if !defined(SM2)
	lm	+=GetDynLight(input.TexCoord1, input.TexCoord2.xyz);
#endif

#if defined(CELLIGHT)
	lm	=CalcCelColor(lm);
#endif

	color	*=lm;
	color	=pow(abs(color), 1 / 2.2);

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	return	float4(color, input.TexCoord1.w);
}


float4 LightMapShadowPS(VTex04Tex14Tex24 input) : COLOR0
{
	float4	color	=float4(0.0, 0.0, 0.0, input.TexCoord1.w);

	return	ShadowColor(mbDirectional, input.TexCoord1, input.TexCoord2.xyz, color);
}

/*
TwoTarget VertexLitPS(VTex04Tex14Tex24Tex31 input)
{
	float3		color;
	float2		tex0;
	TwoTarget	ret;

	tex0.x	=input.TexCoord0.x;
	tex0.y	=input.TexCoord0.y;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, tex0)), 2.2);
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

	//back to srgb
	color	=pow(abs(color), 1 / 2.2);

	ret.Color.xyz	=color;
	ret.Color.w		=input.TexCoord3.x;

	ret.DepthMatIDNorm.x	=distance(worldPos, mEyePos);
	ret.DepthMatIDNorm.y	=mMaterialID;
	ret.DepthMatIDNorm.zw	=EncodeNormal(norm);

	return	ret;
}


TwoTarget VertexLitCelPS(VTex04Tex14Tex24Tex31 input)
{
	float3		color;
	float2		tex0;
	TwoTarget	ret;

	tex0.x	=input.TexCoord0.x;
	tex0.y	=input.TexCoord0.y;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, tex0)), 2.2);
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

	//back to srgb
	color	=pow(abs(color), 1 / 2.2);

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	ret.Color.xyz	=color;
	ret.Color.w		=input.TexCoord3.x;

	ret.DepthMatIDNorm.x	=distance(worldPos, mEyePos);
	ret.DepthMatIDNorm.y	=mMaterialID;
	ret.DepthMatIDNorm.zw	=EncodeNormal(norm);

	return	ret;
}


float4 VertexLitShadowPassPS(VTex04Tex14Tex24Tex31 input) : COLOR0
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


TwoTarget LightMapAnimPS(VTex04Tex14Tex24Tex34Tex44Tex54 input)
{
	float3		color;
	TwoTarget	ret;

	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
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
		lm	+=(input.TexCoord5.x * tex2D(LightMapSampler, input.TexCoord0.zw));
	}
	if(input.TexCoord5.y > 0)
	{
		lm	+=(input.TexCoord5.y * tex2D(LightMapSampler, input.TexCoord1.xy));
	}
	if(input.TexCoord5.z > 0)
	{
		lm	+=(input.TexCoord5.z * tex2D(LightMapSampler, input.TexCoord1.zw));
	}
	if(input.TexCoord5.w > 0)
	{
		lm	+=(input.TexCoord5.w * tex2D(LightMapSampler, input.TexCoord2.xy));
	}

	//dyn lights
#if !defined(SM2)
	lm	+=GetDynLight(worldPos, norm);
#endif

	//Apply lighting.
	color	*=lm;
	color	=saturate(color);

	//back to srgb
	color	=pow(color, 1 / 2.2);

	ret.Color.xyz	=color;
	ret.Color.w		=input.TexCoord2.z;

	ret.DepthMatIDNorm.x	=distance(worldPos, mEyePos);
	ret.DepthMatIDNorm.y	=mMaterialID;
	ret.DepthMatIDNorm.zw	=EncodeNormal(norm);

	return	ret;
}


TwoTarget LightMapAnimCelPS(VTex04Tex14Tex24Tex34Tex44Tex54 input)
{
	float3		color;
	TwoTarget	ret;

	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
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
		lm	+=(input.TexCoord5.x * tex2D(LightMapSampler, input.TexCoord0.zw));
	}
	if(input.TexCoord5.y > 0)
	{
		lm	+=(input.TexCoord5.y * tex2D(LightMapSampler, input.TexCoord1.xy));
	}
	if(input.TexCoord5.z > 0)
	{
		lm	+=(input.TexCoord5.z * tex2D(LightMapSampler, input.TexCoord1.zw));
	}
	if(input.TexCoord5.w > 0)
	{
		lm	+=(input.TexCoord5.w * tex2D(LightMapSampler, input.TexCoord2.xy));
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

	//back to srgb
	color	=pow(abs(color), 1 / 2.2);

#if defined(CELALL)
	color	=CalcCelColor(color);
#endif

	ret.Color.xyz	=color;
	ret.Color.w		=input.TexCoord2.z;

	ret.DepthMatIDNorm.x	=distance(worldPos, mEyePos);
	ret.DepthMatIDNorm.y	=mMaterialID;
	ret.DepthMatIDNorm.zw	=EncodeNormal(norm);

	return	ret;
}


float4 LightMapAnimShadowPS(VTex04Tex14Tex24Tex34Tex44Tex54 input) : COLOR0
{
	float4	color	=float4(0.0, 0.0, 0.0, input.TexCoord2.z);

	float3	norm	=input.TexCoord3.xyz;

	//shadow map
	return	ShadowColor(mbDirectional, input.TexCoord4, norm, color);
}


TwoTarget SkyPS(VCubeTex0 input)
{
	TwoTarget	ret;
	float4		color;

	if(mbTextureEnabled)
	{
		float3	worldPosition	=input.TexCoord0;

		//calculate vector from eye to pos
		float3	eyeVec	=worldPosition - mEyePos;
	
		eyeVec	=normalize(eyeVec);

		color	=texCUBE(SkySampler, eyeVec);
	}
	else
	{
		color	=float4(1, 1, 1, 1);
	}

	ret.Color				=color;
	ret.DepthMatIDNorm.x	=MAX_HALF;
	ret.DepthMatIDNorm.y	=mMaterialID;
	ret.DepthMatIDNorm.zw	=EncodeNormal(float3(0, -1, 0));

	return	ret;
}
*/

technique LightMap
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapPS();
#endif
	}
	pass Shadow
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapShadowPS();
#endif
	}
	pass DMN
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapDMNPS();
#endif
	}
}


technique LightMapCel
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapCelPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapCelPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapCelPS();
#endif
	}
	pass Shadow
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapShadowPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapShadowPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapShadowPS();
#endif
	}
	pass DMN
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapVS();
		PixelShader		=compile ps_4_0 LightMapDMNPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapVS();
		PixelShader		=compile ps_3_0 LightMapDMNPS();
#else
		VertexShader	=compile vs_2_0 LightMapVS();
		PixelShader		=compile ps_2_0 LightMapDMNPS();
#endif
	}
}
/*
technique LightMapAlpha
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAlphaVS();
		PixelShader		=compile ps_4_0 LightMapPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAlphaVS();
		PixelShader		=compile ps_3_0 LightMapPS();
#else
		VertexShader	=compile vs_2_0 LightMapAlphaVS();
		PixelShader		=compile ps_2_0 LightMapPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAlphaVS();
		PixelShader		=compile ps_2_0 LightMapShadowPassPS();
	}
}

technique LightMapAlphaCel
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAlphaVS();
		PixelShader		=compile ps_4_0 LightMapCelPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAlphaVS();
		PixelShader		=compile ps_3_0 LightMapCelPS();
#else
		VertexShader	=compile vs_2_0 LightMapAlphaVS();
		PixelShader		=compile ps_2_0 LightMapCelPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAlphaVS();
		PixelShader		=compile ps_2_0 LightMapShadowPassPS();
	}
}

technique Alpha
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 VertexLitVS();
		PixelShader		=compile ps_3_0 VertexLitPS();
#else
		VertexShader	=compile vs_2_0 VertexLitVS();
		PixelShader		=compile ps_2_0 VertexLitPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 VertexLitVS();
		PixelShader		=compile ps_2_0 VertexLitShadowPassPS();
	}
}

technique VertexLighting
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 VertexLitVS();
		PixelShader		=compile ps_3_0 VertexLitPS();
#else
		VertexShader	=compile vs_2_0 VertexLitVS();
		PixelShader		=compile ps_2_0 VertexLitPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 VertexLitVS();
		PixelShader		=compile ps_2_0 VertexLitShadowPassPS();
	}
}

technique VertexLightingCel
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 VertexLitVS();
		PixelShader		=compile ps_4_0 VertexLitCelPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 VertexLitVS();
		PixelShader		=compile ps_3_0 VertexLitCelPS();
#else
		VertexShader	=compile vs_2_0 VertexLitVS();
		PixelShader		=compile ps_2_0 VertexLitCelPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 VertexLitVS();

		PixelShader		=compile ps_2_0 VertexLitShadowPassPS();
	}
}

technique LightMapAnim
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAnimVS();
		PixelShader		=compile ps_3_0 LightMapAnimPS();
#else
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimShadowPassPS();
	}
}

technique LightMapAnimAlpha
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAnimVS();
		PixelShader		=compile ps_3_0 LightMapAnimPS();
#else
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimShadowPassPS();
	}
}

technique LightMapAnimAlphaCel
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimCelPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAnimVS();
		PixelShader		=compile ps_3_0 LightMapAnimCelPS();
#else
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimCelPS();
#endif
	}
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimShadowPassPS();
	}
}

technique LightMapAnimCel
{
	pass Base
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 LightMapAnimVS();
		PixelShader		=compile ps_4_0 LightMapAnimCelPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 LightMapAnimVS();
		PixelShader		=compile ps_3_0 LightMapAnimCelPS();
#else
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimCelPS();
#endif
	}
}

technique LightMapAnimShadow
{
	pass Shadow
	{
		VertexShader	=compile vs_2_0 LightMapAnimVS();
		PixelShader		=compile ps_2_0 LightMapAnimShadowPassPS();
	}
}

technique Sky
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 SkyVS();
		PixelShader		=compile ps_2_0 SkyPS();
	}
}*/