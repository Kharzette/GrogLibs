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


struct VPosTex0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
};


struct VPosCubeTex0
{
	float4 Position : POSITION0;
	float3 TexCoord0 : TEXCOORD0;
};


struct VPosNormTex0Tex1
{
	float4 Position : POSITION0;
	float3 Normal0 : NORMAL0;
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


struct VPosNormTex0Tex1Col0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float3 Normal0 : NORMAL0;
	float4 Color0 : COLOR0;
};


struct VPosNormTex0Col0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float3 Normal0 : NORMAL0;
	float4 Color0 : COLOR0;
};


struct VPosNormBlendTex0Tex1Tex2Tex3Tex4
{
	float4 Position : POSITION0;
	float3 Normal0 : NORMAL0;
	float4 StyleIndex : BLENDINDICES0;	
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
};


struct VPosTex0Col0
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float4 Color0 : COLOR0;
};


struct VPosTex0Tex1Tex2Tex3Tex4Color0Intensity
{
	float4 Position : POSITION0;
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 Color0 : Color0;
	float4 StyleIntensity : TEXCOORD5;
};


struct VTex0
{
	float2 TexCoord0 : TEXCOORD0;
};


struct VCubeTex0
{
	float3 TexCoord0 : TEXCOORD0;
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


struct VTex0Tex1Tex2Tex3Tex4Color0Intensity
{
	float2 TexCoord0 : TEXCOORD0;
	float2 TexCoord1 : TEXCOORD1;
	float2 TexCoord2 : TEXCOORD2;
	float2 TexCoord3 : TEXCOORD3;
	float2 TexCoord4 : TEXCOORD4;
	float4 Color0 : COLOR0;
	float4 StyleIntensity : TEXCOORD5;
};


VPosTex0Tex1Col0 LMVertexShader(VPosNormTex0Tex1 input)
{
	VPosTex0Tex1Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	
	float	dist	=distance(worldPosition, mLight0Position);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(mLight0Position - worldPosition);
		float3	worldNormal		=mul(input.Normal0, mWorld);
		float	ndl				=dot(worldNormal, lightDirection);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}
		
		output.Color0	=float4(mLight0Color * ndl, 1);
	}
	else
	{
		//let the lightmap do all the work
		output.Color0	=float4(0, 0, 0, 1);
	}

	return	output;
}


VPosTex0Tex1Col0 LMAlphaVertexShader(VPosNormTex0Tex1Col0 input)
{
	VPosTex0Tex1Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	
	float	dist	=distance(worldPosition, mLight0Position);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(mLight0Position - worldPosition);
		float3	worldNormal		=mul(input.Normal0, mWorld);
		float	ndl				=dot(worldNormal, lightDirection);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}
		
		output.Color0	=float4(mLight0Color * ndl, input.Color0.w);
	}
	else
	{
		output.Color0	=input.Color0;
	}
	return	output;
}


VPosTex0Col0 VLitVertexShader(VPosNormTex0Col0 input)
{
	VPosTex0Col0	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position		=mul(mul(worldPosition, mView), mProjection);
	output.TexCoord0	=input.TexCoord0;
	
	float	dist	=distance(worldPosition, mLight0Position);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(mLight0Position - worldPosition);
		float3	worldNormal		=mul(input.Normal0, mWorld);
		float	ndl				=dot(worldNormal, lightDirection);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}
		
		output.Color0	=input.Color0 + float4(mLight0Color * ndl, 0);		
		output.Color0	=saturate(output.Color0);
	}
	else
	{
		output.Color0	=input.Color0;
	}	
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


VPosTex0Tex1Tex2Tex3Tex4Color0Intensity LMAnimVertexShader(VPosNormBlendTex0Tex1Tex2Tex3Tex4 input)
{
	VPosTex0Tex1Tex2Tex3Tex4Color0Intensity	output;

	float4	worldPosition	=mul(input.Position, mWorld);

	output.Position	=mul(mul(worldPosition, mView), mProjection);

	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.TexCoord2	=input.TexCoord2;
	output.TexCoord3	=input.TexCoord3;
	output.TexCoord4	=input.TexCoord4;
	
	output.StyleIntensity	=float4(-1, -1, -1, 1);
	
	float4	sidx	=input.StyleIndex;
	
	//look up style intensities
	if(sidx.x < 17)
	{
		output.StyleIntensity.x	=mAniIntensities[sidx.x];
	}
	
	//next anim style if any
	if(sidx.y < 17)
	{
		output.StyleIntensity.y	=mAniIntensities[sidx.y];
	}
	
	if(sidx.z < 17)
	{
		output.StyleIntensity.z	=mAniIntensities[sidx.z];
	}
	
	float	dist	=distance(worldPosition, mLight0Position);
	if(dist < mLightRange)
	{
		float3	lightDirection	=normalize(mLight0Position - worldPosition);
		float3	worldNormal		=mul(input.Normal0, mWorld);
		float	ndl				=dot(worldNormal, lightDirection);
		
		//distance falloff
		if(dist > mLightFalloffRange)
		{
			ndl	*=(1 - ((dist - mLightFalloffRange) / (mLightRange - mLightFalloffRange)));
		}
		
		output.Color0	=float4(mLight0Color * ndl, 1);
	}
	else
	{
		//let the lightmap do all the work
		output.Color0	=float4(0, 0, 0, 1);
	}
	
	//for alpha if any
	output.StyleIntensity.w	=sidx.w;
	
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
	
	lm	+=input.Color0;
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
	
	lm	+=float3(input.Color0.x, input.Color0.y, input.Color0.z);	
	lm	=saturate(lm);
	
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


float4 LMAnimPixelShader(VTex0Tex1Tex2Tex3Tex4Color0Intensity input) : COLOR0
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
	if(input.StyleIntensity.x > 0)
	{
		lm	+=(input.StyleIntensity.x * tex2D(LightMapSampler, input.TexCoord1));
	}
	if(input.StyleIntensity.y > 0)
	{
		lm	+=(input.StyleIntensity.y * tex2D(LightMapSampler, input.TexCoord2));
	}
	if(input.StyleIntensity.z > 0)
	{
		lm	+=(input.StyleIntensity.z * tex2D(LightMapSampler, input.TexCoord3));
	}
	
	lm	+=input.Color0;	
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
