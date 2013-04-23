//texture stuff
texture mTexture;
texture mLightMap;
bool	mbTextureEnabled;
float2	mTexSize;

//intensity levels for the animated / switchable light styles
half	mAniIntensities[44];

//warp factor for warping faces
float	mWarpFactor;

//vertical range for 2D collision hull drawing
float	mYRangeMax;
float	mYRangeMin;


#include "Types.fxh"
#include "CommonFunctions.fxh"


VPosTex04Tex14Tex24 LMVertexShader(VPosNormTex04 input)
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


VPosTex04Tex14Tex24 LMAlphaVertexShader(VPosNormTex04Col0 input)
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


VPosTex04Tex14Tex24Tex31 VLitVertexShader(VPosNormTex0Col0 input)
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


VPosTex04Tex14Tex24Tex34 MirrorVertexShader(VPosNormTex0Tex1Col0 input)
{
	VPosTex04Tex14Tex24Tex34	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position			=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0.xy		=input.TexCoord0.xy / mTexSize;
	output.TexCoord0.zw		=input.TexCoord1.xy;
	output.TexCoord1		=worldPosition;
	output.TexCoord2.xyz	=input.Normal;
	output.TexCoord2.w		=0;
	output.TexCoord3		=input.Color;
	
	return	output;
}


VPosTex0Tex1Single YRangeVertexShader(VPosNormTex0Col0 input)
{
	VPosTex0Tex1Single	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;	
	output.TexCoord1	=worldPosition.y;
	
	return	output;
}


VPosTex0Tex14 FullBrightVertexShader(VPosTex0 input)
{
	VPosTex0Tex14	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0 / mTexSize;
	output.TexCoord1	=worldPosition;

	return	output;
}


VPosCubeTex0 SkyVertexShader(VPosTex0 input)
{
	VPosCubeTex0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.TexCoord0	=worldPosition.xyz;
	output.Position		=mul(mul(worldPosition, mView), mProjection);

	return	output;
}


VPosTex04Tex14Tex24Tex34Tex44Tex54 LMAnimVertexShader(VPosNormBlendTex04Tex14Tex24 input)
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
	Texture		=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
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


sampler LightMapSampler = sampler_state
{
    Texture	=(mLightMap);

    MinFilter	=Linear;
    MagFilter	=Linear;
    MipFilter	=None;
    
    AddressU	=Clamp;
    AddressV	=Clamp;
};


float4 LMPixelShader(VTex04Tex14Tex24 input) : COLOR0
{
	float3	color;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord0.zw);

	float	dist	=distance(input.TexCoord1.xyz, mLight0Position);
	if(dist < mLightRange)
	{
		float3	lightDir	=normalize(mLight0Position - input.TexCoord1.xyz);
		float	ndl			=dot(input.TexCoord2, lightDir);

		if(ndl > 0)
		{
			if(dist > mLightFalloffRange)
			{
				ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));			
			}
			lm	+=(ndl * mLight0Color);
		}
	}

	color.xyz	=ShadowColor(mbDirectional, input.TexCoord1, color.xyz);

	color	*=lm;
	color	=saturate(color);

	//back to srgb
	color	=pow(color, 1 / 2.2);

	return	float4(color, input.TexCoord1.w);
}


float4 LMCellPixelShader(VTex04Tex14Tex24 input) : COLOR0
{
	float3	color;
	
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord0.zw);

	lm	=ShadowColor(mbDirectional, input.TexCoord1, lm);

	color.rgb	*=lm;

	//do the Cell thing
	color.rgb	=CalcCellColor(color.rgb);

	//back to srgb
	color	=pow(abs(color), 1 / 2.2);
	
	return	float4(color, input.TexCoord1.w);
}


float4 VLitPixelShader(VTex04Tex14Tex24Tex31 input) : COLOR0
{
	float3	color;	
	float2	tex0;

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
	
	//shadow map
	inColor.xyz	=ShadowColor(mbDirectional, worldPos, inColor.xyz);

	color	*=inColor;
	color	=saturate(color);

	//back to srgb
	color	=pow(color, 1 / 2.2);

	return	float4(color, input.TexCoord3.x);
}


float4 MirrorPixelShader(VTex04Tex14Tex24Tex34 input) : COLOR0
{
	float3	mirrorColor;
	float4	texColor;	
	
	if(mbTextureEnabled)
	{
		//no need to gamma correct rendertarget
		mirrorColor	=tex2D(LightMapSampler, input.TexCoord0.zw);
		texColor	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		mirrorColor	=float3(1.0, 1.0, 1.0);
		texColor	=(1.0, 1.0, 1.0, 1.0);
	}

	float3	worldPos	=input.TexCoord1.xyz;
	float3	norm		=input.TexCoord2.xyz;
	float4	vertColor	=input.TexCoord3;
	
	texColor	*=vertColor;

	//alpha mix with mirror
	texColor.xyz	+=mirrorColor / vertColor.w;
	texColor		=saturate(texColor);

	//back to srgb
	texColor	=pow(texColor, 1 / 2.2);

	return	float4(texColor.xyz, 1.0);
}


float4 VLitCellPS(VTex04Tex14Tex24Tex31 input) : COLOR0
{
	float3	color;
	float2	tex0;

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
	
	//shadow map
	inColor.xyz	=ShadowColor(mbDirectional, worldPos, inColor.xyz);

	color.rgb	*=inColor;

	//do the Cell thing
	color.rgb	=CalcCellColor(color.rgb);

	//back to srgb
	color	=pow(abs(color), 1 / 2.2);

	return	float4(color, input.TexCoord3.x);
}


float4 YRangePixelShader(VTex0Tex1Single input) : COLOR0
{
	float	alpha	=step(mYRangeMin, input.TexCoord1);
	alpha			*=step(input.TexCoord1, mYRangeMax);

	clip(alpha - 1);

	float3	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	

	return	float4(color, alpha);
}


float4 FullBrightPixelShader(VTex0Tex14 input) : COLOR0
{
	float4	color;

	if(mbTextureEnabled)
	{
		color	=tex2D(TextureSampler, input.TexCoord0);
	}
	else
	{
		color	=float4(1, 1, 1, 1);
	}

	//shadow map
	color.xyz	=ShadowColor(mbDirectional, input.TexCoord1, color.xyz);

	return	color;
}


float4 WarpyPixelShader(VTex0 input) : COLOR0
{
	float2	warpy;

	warpy.x	=sin(mWarpFactor);
	warpy.y	=cos(mWarpFactor);

	float2	texCoord	=input.TexCoord0 * warpy;
	if(mbTextureEnabled)
	{
		return	tex2D(TextureSampler, texCoord);
	}
	return	float4(1, 1, 1, 1);
}


float4 SkyPixelShader(VCubeTex0 input) : COLOR0
{
	if(mbTextureEnabled)
	{
		float3	worldPosition	=input.TexCoord0;

		//calculate vector from eye to pos
		float3	eyeVec	=worldPosition - mEyePos;
	
		eyeVec	=normalize(eyeVec);
	
		return	texCUBE(SkySampler, eyeVec);		
	}

	return	float4(1, 1, 1, 1);
}


float4 SkyCellPixelShader(VCubeTex0 input) : COLOR0
{
	if(mbTextureEnabled)
	{
		float3	worldPosition	=input.TexCoord0;

		//calculate vector from eye to pos
		float3	eyeVec	=worldPosition - mEyePos;
	
		eyeVec	=normalize(eyeVec);

		float4	texel	=texCUBE(SkySampler, eyeVec);

		texel.xyz	=CalcCellColor(texel.xyz);
	
		return	texel;
	}

	return	float4(1, 1, 1, 1);
}


float4 FullDarkPixelShader(VTex0 input) : COLOR0
{
	float4	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	
	return	color * float4(0.01, 0.01, 0.01, 1.0);
}


float4 LMAnimPixelShader(VTex04Tex14Tex24Tex34Tex44Tex54 input) : COLOR0
{
	float3	color;
	if(mbTextureEnabled)
	{
		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float3	lm		=float3(0, 0, 0);
	float3	norm	=input.TexCoord3.xyz;

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

	//shadow map
	color.xyz	=ShadowColor(mbDirectional, input.TexCoord4, color.xyz);

	//Apply lighting.
	color	*=lm;
	color	=saturate(color);

	//back to srgb
	color	=pow(color, 1 / 2.2);

	return	float4(color, input.TexCoord2.z);
}


float4 LMAnimCellPS(VTex04Tex14Tex24Tex34Tex44Tex54 input) : COLOR0
{
	float3	color;
	if(mbTextureEnabled)
	{
		//don't have enough instructions for the abs
//		color	=pow(abs(tex2D(TextureSampler, input.TexCoord0.xy)), 2.2);
		color	=pow(tex2D(TextureSampler, input.TexCoord0.xy), 2.2);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}

	float3	lm		=float3(0, 0, 0);
	float3	norm	=input.TexCoord3.xyz;

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

	//shadow map
	color.xyz	=ShadowColor(mbDirectional, input.TexCoord4, color.xyz);

	color.rgb	*=lm;

	//do the Cell thing
	color.rgb	=CalcCellColor(color.rgb);

	//back to srgb
//	color	=pow(abs(color), 1 / 2.2);
	color	=pow(color, 1 / 2.2);

	return	float4(color, input.TexCoord2.z);
}


technique LightMap
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMVertexShader();
		PixelShader		=compile ps_2_0 LMPixelShader();
	}
}

technique LightMapCell
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMVertexShader();
		PixelShader		=compile ps_2_0 LMCellPixelShader();
	}
}

technique LightMapAlpha
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAlphaVertexShader();
		PixelShader		=compile ps_2_0 LMPixelShader();
	}
}

technique LightMapAlphaCell
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAlphaVertexShader();
		PixelShader		=compile ps_2_0 LMCellPixelShader();
	}
}

technique Alpha
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 VLitVertexShader();
		PixelShader		=compile ps_2_0 VLitPixelShader();
	}
}

technique VertexLighting
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 VLitVertexShader();
		PixelShader		=compile ps_2_0 VLitPixelShader();
	}
}

technique VLitCell
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 VLitVertexShader();
		PixelShader		=compile ps_2_0 VLitCellPS();
	}
}

technique FullBright
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 FullBrightVertexShader();
		PixelShader		=compile ps_2_0 FullBrightPixelShader();
	}
}

technique FullDark
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 FullBrightVertexShader();
		PixelShader		=compile ps_2_0 FullDarkPixelShader();
	}
}

technique Mirror
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 MirrorVertexShader();
		PixelShader		=compile ps_2_0 MirrorPixelShader();
	}
}

technique Sky
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 SkyVertexShader();
		PixelShader		=compile ps_2_0 SkyCellPixelShader();
	}
}

technique LightMapAnim
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAnimVertexShader();
		PixelShader		=compile ps_2_0 LMAnimPixelShader();
	}
}

technique LightMapAnimAlpha
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAnimVertexShader();
		PixelShader		=compile ps_2_0 LMAnimPixelShader();
	}
}

technique LightMapAnimAlphaCell
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAnimVertexShader();
		PixelShader		=compile ps_2_0 LMAnimCellPS();
	}
}

technique LightMapAnimCell
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 LMAnimVertexShader();
		PixelShader		=compile ps_2_0 LMAnimCellPS();
	}
}

technique Warpy
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 FullBrightVertexShader();
		PixelShader		=compile ps_2_0 WarpyPixelShader();
	}
}

technique VerticalRange
{
	pass Pass1
	{
		VertexShader	=compile vs_2_0 YRangeVertexShader();
		PixelShader		=compile ps_2_0 YRangePixelShader();
	}
}