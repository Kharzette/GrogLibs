float4x4	mWorld;
float4x4	mView;
float4x4	mProjection;
float3		mEyePos;

//texture stuff
texture mTexture;
texture mLightMap;
bool	mbTextureEnabled;
float2	mTexSize;

//nearby dynamic lights?
float3		mLight0Position;
float3		mLight0Color;
float		mLightRange;
float		mLightFalloffRange;	//under this light at full strength

//intensity levels for the animted light styles
float	mAniIntensities[16];

#include "Types.fxh"


float3 ComputeLight(float3 worldPos, float3 lightPos, float3 normal)
{
	float3	col		=float3(0, 0, 0);
	float	dist	=distance(worldPos, lightPos);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(lightPos - worldPos);
		float3	worldNormal		=mul(normal, mWorld);
		float	ndl				=dot(worldNormal, lightPos);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}		
		col	=mLight0Color * ndl;
	}
	return	col;
}


VPosTex0Tex1Col0 LMVertexShader(VPosNormTex0Tex1 input)
{
	VPosTex0Tex1Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	
	output.Color	=float4(ComputeLight(worldPosition, mLight0Position,
						input.Normal), 1);
	
	return	output;
}


VPosTex0Tex1Col0 LMAlphaVertexShader(VPosNormTex0Tex1Col0 input)
{
	VPosTex0Tex1Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	
	output.Color	=float4(ComputeLight(worldPosition, mLight0Position,
						input.Normal), input.Color.w);
	
	return	output;
}


VPosTex0Col0 VLitVertexShader(VPosNormTex0Col0 input)
{
	VPosTex0Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;
	
	output.Color	=input.Color + float4(ComputeLight(worldPosition,
						mLight0Position, input.Normal), 0);						
	output.Color	=saturate(output.Color);
	
	return	output;
}


VPosTex0 FullBrightVertexShader(VPosTex0 input)
{
	VPosTex0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;

	return	output;
}


VPosCubeTex0 SkyVertexShader(VPosTex0 input)
{
	VPosCubeTex0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	
	//calculate vector from eye to pos
	float3	eyeVec	=worldPosition - mEyePos;
	
	eyeVec	=normalize(eyeVec);
	
	output.TexCoord0	=eyeVec;

	return	output;
}


VPosTex0Tex1Tex2Tex3Tex4Col0Intensity LMAnimVertexShader(VPosNormBlendTex0Tex1Tex2Tex3Tex4 input)
{
	VPosTex0Tex1Tex2Tex3Tex4Col0Intensity	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.TexCoord2	=input.TexCoord2;
	output.TexCoord3	=input.TexCoord3;
	output.TexCoord4	=input.TexCoord4;
	
	output.Intensity	=float4(-1, -1, -1, 1);
	
	float4	sidx	=input.Blend0;
	
	//look up style intensities
	if(sidx.x < 17)
	{
		output.Intensity.x	=mAniIntensities[sidx.x];
	}
	
	//next anim style if any
	if(sidx.y < 17)
	{
		output.Intensity.y	=mAniIntensities[sidx.y];
	}
	
	if(sidx.z < 17)
	{
		output.Intensity.z	=mAniIntensities[sidx.z];
	}
	
	output.Color	=float4(ComputeLight(worldPosition, mLight0Position,
						input.Normal), 1);
	
	//for alpha if any
	output.Intensity.w	=sidx.w;
	
	return	output;
}


sampler TextureSampler = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


sampler SkySampler = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
	AddressW	=Wrap;
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


float4 LMPixelShader(VTex0Tex1Col0 input) : COLOR0
{
	float3	color;
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	if(mbTextureEnabled)
	{
		color	=tex2D(TextureSampler, tex0);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord1);
	
	lm	+=input.Color;
	lm	=saturate(lm);
	
	//Apply lighting.
	color	*=lm;
	
	return	float4(color, 1);
}


float4 LMAlphaPixelShader(VTex0Tex1Col0 input) : COLOR0
{
	float3	color;
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	if(mbTextureEnabled)
	{
		color	=tex2D(TextureSampler, tex0);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	
	float3	lm	=tex2D(LightMapSampler, input.TexCoord1);
	
	lm	+=float3(input.Color.x, input.Color.y, input.Color.z);	
	lm	=saturate(lm);
	
	//Apply lighting.
	color	*=lm;
	
	return	float4(color, input.Color.w);
}


float4 VLitPixelShader(VTex0Col0 input) : COLOR0
{
	float4	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	
	color	*=input.Color;
	
	return	color;
}


float4 FullBrightPixelShader(VTex0 input) : COLOR0
{
	float4	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	
	return	color;
}


float4 SkyPixelShader(VCubeTex0 input) : COLOR0
{
	float4	color;
	
	float3	tex0	=input.TexCoord0;
	
	color	=texCUBE(SkySampler, tex0);
	
	return	color;
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


float4 LMAnimPixelShader(VTex0Tex1Tex2Tex3Tex4Col0Intensity input) : COLOR0
{
	float3	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	if(mbTextureEnabled)
	{
		color	=tex2D(TextureSampler, tex0);
	}
	else
	{
		color	=float3(1.0, 1.0, 1.0);
	}
	float3	lm	=float3(0, 0, 0);
	
	//grab style intensity
	if(input.Intensity.x > 0)
	{
		lm	+=(input.Intensity.x * tex2D(LightMapSampler, input.TexCoord1));
	}
	if(input.Intensity.y > 0)
	{
		lm	+=(input.Intensity.y * tex2D(LightMapSampler, input.TexCoord2));
	}
	if(input.Intensity.z > 0)
	{
		lm	+=(input.Intensity.z * tex2D(LightMapSampler, input.TexCoord3));
	}
	
	lm	+=input.Color;	
	lm	=saturate(lm);
	
	//Apply lighting.
	color	*=lm;
	return	float4(color, input.Intensity.w);
}


technique LightMap
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 LMVertexShader();
		PixelShader = compile ps_2_0 LMPixelShader();
	}
}

technique LightMapAlpha
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 LMAlphaVertexShader();
		PixelShader = compile ps_2_0 LMAlphaPixelShader();
	}
}

technique Alpha
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VLitVertexShader();
		PixelShader = compile ps_2_0 VLitPixelShader();
	}
}

technique VertexLighting
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VLitVertexShader();
		PixelShader = compile ps_2_0 VLitPixelShader();
	}
}

technique FullBright
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 FullBrightVertexShader();
		PixelShader = compile ps_2_0 FullBrightPixelShader();
	}
}

technique FullDark
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 FullBrightVertexShader();
		PixelShader = compile ps_2_0 FullDarkPixelShader();
	}
}

technique Mirror
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VLitVertexShader();
		PixelShader = compile ps_2_0 VLitPixelShader();
	}
}

technique Sky
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 SkyVertexShader();
		PixelShader = compile ps_2_0 SkyPixelShader();
	}
}

technique LightMapAnim
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 LMAnimVertexShader();
		PixelShader = compile ps_2_0 LMAnimPixelShader();
	}
}

technique LightMapAnimAlpha
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 LMAnimVertexShader();
		PixelShader = compile ps_2_0 LMAnimPixelShader();
	}
}
