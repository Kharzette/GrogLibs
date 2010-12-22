float4x4 mWorld;
float4x4 mView;
float4x4 mProjection;

texture mTexture;
texture mLightMap;

bool mbTextureEnabled;
bool mbLightMapEnabled;
bool mbFullBright;

float2	mTexSize;

//intensity levels for the animted light styles
float	mAniIntensities[16];


struct LMVS_INPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
};


struct NonLMVS_INPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
};


struct LMAnimVS_INPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD1;
	float2 TexCoord3 : TEXCOORD1;
	float2 TexCoord4 : TEXCOORD1;
	float4 StyleIndex : BLENDINDICES0;
};


struct LMVS_OUTPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
};


struct NonLMVS_OUTPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
};


struct LMAnimVS_OUTPUT
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 StyleIntensity : TEXCOORD5;
};


LMVS_OUTPUT LMVertexShader(LMVS_INPUT input)
{
	LMVS_OUTPUT output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;

	return	output;
}


NonLMVS_OUTPUT NonLMVertexShader(NonLMVS_INPUT input)
{
	NonLMVS_OUTPUT output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;

	return	output;
}


LMAnimVS_OUTPUT LMAnimVertexShader(LMAnimVS_INPUT input)
{
	LMAnimVS_OUTPUT output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.TexCoord2	=input.TexCoord2;
	output.TexCoord3	=input.TexCoord3;
	output.TexCoord4	=input.TexCoord4;
	
	output.StyleIntensity	=float4(-1, -1, -1, 1);
	
	//look up style intensities
	if(input.StyleIndex.x < 17)
	{
		output.StyleIntensity.x	=mAniIntensities[input.StyleIndex.x];		
	}
	
	//next anim style if any
	if(input.StyleIndex.y < 17)
	{
		output.StyleIntensity.y	=mAniIntensities[input.StyleIndex.y];
	}
	
	if(input.StyleIndex.z < 17)
	{
		output.StyleIntensity.z	=mAniIntensities[input.StyleIndex.z];
	}
	return	output;
}


struct LMPS_INPUT
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
};


struct NonLMPS_INPUT
{
	float2 TexCoord0 : TEXCOORD0;
};


struct LMAnimPS_INPUT
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 StyleIntensity : TEXCOORD5;
};


sampler TextureSampler = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
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


float4 LMPixelShader(LMPS_INPUT input) : COLOR0
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
	
	//Apply lighting.
	color	*=lm;
	
	return	float4(color, 1);
}


float4 NonLMPixelShader(NonLMPS_INPUT input) : COLOR0
{
	float3	color;
	
	color	=float3(0.0, 0.0, 0.0);
	return	float4(color, 1);
}


float4 LMAnimPixelShader(LMAnimPS_INPUT input) : COLOR0
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
	
	//grab style intensity
	if(input.StyleIntensity.x > 0)
	{
		lm	+=(input.StyleIntensity.x * tex2D(LightMapSampler, input.TexCoord2));
	}
	if(input.StyleIntensity.y > 0)
	{
		lm	+=(input.StyleIntensity.y * tex2D(LightMapSampler, input.TexCoord3));
	}
	if(input.StyleIntensity.z > 0)
	{
		lm	+=(input.StyleIntensity.z * tex2D(LightMapSampler, input.TexCoord4));
	}
	
	lm	=saturate(lm);
	
	//Apply lighting.
	color	*=lm;
	return	float4(color, 1);
}


technique LightMap
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 LMVertexShader();
		PixelShader = compile ps_2_0 LMPixelShader();
	}
}

technique FullDark
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 NonLMVertexShader();
		PixelShader = compile ps_2_0 NonLMPixelShader();
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
