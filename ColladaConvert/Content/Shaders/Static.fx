//ui fx, basic textured
//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;

//texture layers used on the surface
texture	mTexture;
texture	mNMap;

shared float3 mLightDirection;

//light up / glow fakery
float	mGlow;

//sunlight / moonlight
float4	mLightColor0;		//trilights need 3 colors
float4	mLightColor1;		//trilights need 3 colors
float4	mLightColor2;		//trilights need 3 colors

//outline / toon related
bool	mbTextureEnabled;
float	mToonThresholds[2] = { 0.8, 0.4 };
float	mToonBrightnessLevels[3] = { 1.3, 0.9, 0.5 };

//this comes outta the vertex shader
struct VSOutput 
{
     float4	Position	: POSITION;
     float2	TexCoord0	: TEXCOORD0;
     float4	Color		: COLOR0;
};

//output for edge / outline vertex shader
struct VSOutline
{
	float4	Position	: POSITION;
	float2	TexCoord0	: TEXCOORD0;
	float	LightAmount	: TEXCOORD1;
};

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float2	TexCoord0	: TEXCOORD0;
};

struct ToonPSInput
{
	float2	TexCoord0	: TEXCOORD0;
	float	LightAmount	: TEXCOORD1;
};

struct NormalDepthVSOutput
{
	float4	Position	: POSITION0;
	float4	Color		: COLOR0;
};

sampler TexSampler0 = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler TexSamplerNorm = sampler_state
{
	Texture	=(mNMap);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


//trilight
VSOutput TrilightVS(float3	position	: POSITION,
					float3	normal		: NORMAL,
					float2	tex0		: TEXCOORD0)
{
	VSOutput	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0f), wvp);
	
	float3 worldNormal	=mul(normal, mWorld);
	
    float3	totalLight	=float3(0,0,0);
	float	LdotN		=dot(worldNormal, mLightDirection);
	
	//trilight
	totalLight	+=(mLightColor0 * max(0, LdotN))
		+ (mLightColor1 * (1 - abs(LdotN)))
		+ (mLightColor2 * max(0, -LdotN));
		
	output.Color.rgb	=totalLight;
	output.Color.a		=1.0f;
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	
	//return the output structure
	return	output;
}


//regular N dot L lighting
VSOutput GouradVS(float3 position	: POSITION,
				  float3 normal		: NORMAL,
				  float2 tex0		: TEXCOORD0)
{
	VSOutput	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0f), wvp);
	
	float3 worldNormal	=mul(normal, mWorld);
	float	LdotN		=dot(worldNormal, mLightDirection);
	
	output.Color	=float4(LdotN, LdotN, LdotN, 1.0);
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	
	//return the output structure
	return	output;
}


VSOutput FullBrightVS(float3 position	: POSITION,
					  float3 normal		: NORMAL,
					  float2 tex0		: TEXCOORD0)
{
	VSOutput	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0f), wvp);
	
	output.Color	=float4(1.0, 1.0, 1.0, 1.0);
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	
	//return the output structure
	return	output;
}


VSOutline OutlineVS(float4	pos		: POSITION,
					float3	normal	: NORMAL,
					float2	tex0	: TEXCOORD0)
{
	VSOutline	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(pos, wvp);
	
	//direct copy of texcoord0
	output.TexCoord0	=tex0;
	
	//lighting calculation
	float3 worldNormal	=mul(normal, mWorld);
	output.LightAmount	=dot(worldNormal, mLightDirection);
	
	return	output;
}


NormalDepthVSOutput NormalDepthVS(float4	pos		: POSITION,
								  float3	normal	: NORMAL,
								  float2	tex0	: TEXCOORD0)
{
	NormalDepthVSOutput	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(pos, wvp);
	
	//lighting calculation
	float3 worldNormal	=mul(normal, mWorld);
	
	output.Color.rgb	=(worldNormal + 1) / 2;
	
	output.Color.a		=output.Position.z / output.Position.w;
	
	return	output;
}


float4 TexColorPS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=texel0 * inColor;
	
	return	texLitColor;
}


float4 TexPS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	return	texel0;	
}


float4 TexPSGlow(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	glow	=float4(mGlow, mGlow, mGlow, 1);
	
	texel0	=saturate(texel0 + glow);
	
	return	texel0;	
}


float4 TwoTexAddPS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * (texel0 + texel1);
	
	return	texLitColor;
}


float4 TwoTexModulatePS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0 * texel1;
	
	return	texLitColor;
}


float4 TwoTexDecalPS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * ((texel0 * texel0.a) + texel1);
	
	return	texLitColor;
}


float4 NormalMapPS(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float lightIntensity	=saturate(dot(mLightDirection, texel1));
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0 * lightIntensity;
	
	return	texLitColor;
}


//cartoony lighting
float4 ToonPS(ToonPSInput input) : COLOR0
{
	float4	color	
		=mbTextureEnabled ? tex2D(TexSampler0, input.TexCoord0) : 0;
		
	float	light;
	
	if(input.LightAmount > mToonThresholds[0])
	{
		light	=mToonBrightnessLevels[0];
	}
	else if(input.LightAmount > mToonThresholds[1])
	{
		light	=mToonBrightnessLevels[1];
	}
	else
	{
		light	=mToonBrightnessLevels[2];
	}
	
	color.rgb	*=light;
	
	return	color;
}


//just return the color
float4 NormalDepthPS(float4 color : COLOR0) : COLOR0
{
	return	color;
}


technique Trilight
{
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightVS();
		PixelShader		=compile ps_2_0 TexColorPS();
	}
}


technique FullBright
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 FullBrightVS();
		PixelShader		=compile ps_2_0 TexPS();
	}
}


technique FullBrightGlow
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 FullBrightVS();
		PixelShader		=compile ps_2_0 TexPSGlow();
	}
}


technique TrilightDecal
{
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightVS();
		PixelShader		=compile ps_2_0 TwoTexDecalPS();
	}
}


technique GouradNormalMap
{
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
		PixelShader		=compile ps_2_0 NormalMapPS();
	}
}


technique TrilightNormalMap
{
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightVS();
		PixelShader		=compile ps_2_0 NormalMapPS();
	}
}


technique GouradTwoTexModulate
{
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
		PixelShader		=compile ps_2_0 TwoTexModulatePS();
	}
}


technique TrilightTwoTexModulate
{
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightVS();
		PixelShader		=compile ps_2_0 TwoTexModulatePS();
	}
}


technique GouradTwoTexAdd
{
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
		PixelShader		=compile ps_2_0 TwoTexModulatePS();
	}
}


technique TrilightTwoTexAdd
{
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightVS();
		PixelShader		=compile ps_2_0 TwoTexModulatePS();
	}
}


technique VertexLighting
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
		PixelShader		=compile ps_2_0 TexColorPS();
	}
}


technique Toon
{
	pass P0
	{
		VertexShader	=compile vs_2_0 OutlineVS();
		PixelShader		=compile ps_2_0	ToonPS();
	}
}


technique NormalDepth
{
	pass P0
	{
		VertexShader	=compile vs_2_0 NormalDepthVS();
		PixelShader		=compile ps_2_0 NormalDepthPS();
	}
}