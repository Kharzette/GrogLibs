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


struct VPosTex0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
};


struct VPosTex0Tex1
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
};


struct VPosTex0Tex1Col0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float4 Color0 : COLOR0;
};


struct VPosTex0Norm0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float3 Normal : NORMAL0;
};


struct VPosTex0Tex1Tex2Tex3Tex4Style
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD1;
	float2 TexCoord3 : TEXCOORD1;
	float2 TexCoord4 : TEXCOORD1;
	float4 StyleIndex : BLENDINDICES0;
};


struct VPosTex0Col0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float4 Color0 : COLOR0;
};


struct VPosTex0Tex1Tex2Tex3Tex4Intensity
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 StyleIntensity : TEXCOORD5;
};


struct VTex0
{
	float2 TexCoord0 : TEXCOORD0;
};


struct VTex0Tex1
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
};


struct VTex0Tex1Col0
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float4 Color0 : COLOR0;
};


struct VTex0Col0
{
	float2 TexCoord0 : TEXCOORD0;
	float4 Color0 : COLOR0;
};


struct VTex0Tex1Tex2Tex3Tex4Intensity
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 StyleIntensity : TEXCOORD5;
};


VPosTex0Tex1 LMVertexShader(VPosTex0Tex1 input)
{
	VPosTex0Tex1	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;

	return	output;
}


VPosTex0Tex1Col0 LMAlphaVertexShader(VPosTex0Tex1Col0 input)
{
	VPosTex0Tex1Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.Color0		=input.Color0;

	return	output;
}


//todo n dot l lighting
VPosTex0Col0 VLitVertexShader(VPosTex0Norm0 input)
{
	VPosTex0Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;
	output.Color0		=float4(1, 1, 1, 1);

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


VPosTex0Col0 AlphaVertexShader(VPosTex0Col0 input)
{
	VPosTex0Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;
	output.Color0		=input.Color0;

	return	output;
}


VPosTex0Tex1Tex2Tex3Tex4Intensity LMAnimVertexShader(VPosTex0Tex1Tex2Tex3Tex4Style input)
{
	VPosTex0Tex1Tex2Tex3Tex4Intensity	output;

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


float4 LMPixelShader(VTex0Tex1 input) : COLOR0
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
	
	//Apply lighting.
	color	*=lm;
	
	return	float4(color, input.Color0.w);
}


float4 VLitPixelShader(VTex0Col0 input) : COLOR0
{
	float4	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	
	color	*=input.Color0;
	
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


float4 FullDarkPixelShader(VTex0 input) : COLOR0
{
	float4	color;
	
	float2	tex0	=input.TexCoord0;
	
	tex0.x	/=mTexSize.x;
	tex0.y	/=mTexSize.y;
	
	color	=tex2D(TextureSampler, tex0);
	
	return	color * float4(0.01, 0.01, 0.01, 1.0);
}


float4 LMAnimPixelShader(VTex0Tex1Tex2Tex3Tex4Intensity input) : COLOR0
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


float4 LMAnimAlphaPixelShader(VTex0Tex1Tex2Tex3Tex4Intensity input) : COLOR0
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
	return	float4(color, input.StyleIntensity.w);
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
		VertexShader = compile vs_2_0 AlphaVertexShader();
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
		VertexShader = compile vs_2_0 AlphaVertexShader();
		PixelShader = compile ps_2_0 VLitPixelShader();
	}
}

technique Sky
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 FullBrightVertexShader();
		PixelShader = compile ps_2_0 FullBrightPixelShader();
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
		PixelShader = compile ps_2_0 LMAnimAlphaPixelShader();
	}
}
