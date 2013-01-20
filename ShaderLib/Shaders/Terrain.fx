//Terrain.fx

//Texture density / tiling factor
//higher is less tiling, lower is more
#define	SandDensity			60.0f;
#define	GrassDensity		20.0f;
#define	DirtDensity			80.0f;
#define	MossyStoneDensity	50.0f;
#define	GreyGraniteDensity	60.0f;
#define	SnowDetailDensity	40.0f;
#define	MossLayerDensity	50.0f;

//matrii
shared float4x4	mLocal;
shared float4x4	mLevel;

//sunlight / moonlight
shared float4	mLightColor;
shared float3	mLightDirection;
shared float4	mAmbientColor;

//shadow transforms
shared float4x4	mPUPNearLightViewProj;
shared float4x4	mPUPFarLightViewProj;
shared float4x4	mAvaLightViewProj;

//fog stuff
float	mFogEnabled;
float	mFogStart;
float	mFogEnd;
float3	mFogColor;

//texture layers used on the surface
//there is no three
texture	mTerTexture0;
texture	mTerTexture1;
texture	mTerTexture2;
texture	mTerTexture4;
texture	mTerTexture5;
texture	mTerTexture6;
texture	mTerTexture7;

//shadow textures
texture	mPUPNearShadowTex;
texture	mPUPFarShadowTex;
texture	mAvaShadowTex;

#include "Types.fxh"
#include "CommonFunctions.fxh"

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float4	WorldPos	: TEXCOORD0;
	float4	TexFactor0	: TEXCOORD1;
	float4	TexFactor1	: TEXCOORD2;
};

//output from vertex shader
struct VSOutput
{
	float4	Position	: POSITION;
	float4	Color		: COLOR0;
	float4	WorldPos	: TEXCOORD0;
	float4	TexFactor0	: TEXCOORD1;
	float4	TexFactor1	: TEXCOORD2;
};

struct DepthVSOut
{
	float4	Position	: POSITION;
	float	ViewY		: TEXCOORD0;
};

sampler	TexSampler0	=sampler_state
{
	Texture	=(mTerTexture0);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler1	=sampler_state
{
	Texture	=(mTerTexture1);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler2	=sampler_state
{
	Texture	=(mTerTexture2);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler4	=sampler_state
{
	Texture	=(mTerTexture4);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler5	=sampler_state
{
	Texture	=(mTerTexture5);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler6	=sampler_state
{
	Texture	=(mTerTexture6);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	TexSampler7	=sampler_state
{
	Texture	=(mTerTexture7);
	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;
	AddressU	=Wrap;
	AddressV	=Wrap;
};
sampler	PUPNearShadowSampler	=sampler_state
{
	Texture	=(mPUPNearShadowTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};
sampler	PUPFarShadowSampler	=sampler_state
{
	Texture	=(mPUPFarShadowTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};
sampler	AvaShadowSampler	=sampler_state
{
	Texture	=(mAvaShadowTex);
	MinFilter	=Point;
	MagFilter	=Point;
	MipFilter	=Point;
	AddressU	=Clamp;
	AddressV	=Clamp;
};


//Compute fog factor, swiped from basic effect
float ComputeFogFactor(float d)
{
    return clamp((d - mFogStart) / (mFogEnd - mFogStart), 0, 1) * mFogEnabled;
}


//gourad shading vertex shader
VSOutput DiffuseGourad(float3	position	: POSITION,
					   float3	normal		: NORMAL,
					   float4	tfac0		: COLOR0,
					   float4	tfac1		: COLOR1)
{
	VSOutput	output;

	//transform mats	
	float4x4	localLevelWorld	=mul(mul(mLocal, mLevel), mWorld);
	float4x4	viewProj		=mul(mView, mProjection);

	//generate a default world position
	float4	worldPos	=mul(float4(position, 1.0f), localLevelWorld);

	//transform to screen
	output.Position	=mul(worldPos, viewProj);

	//store for texturing
	output.WorldPos	=worldPos;
	
	float3 worldNormal	=mul(normal, mWorld);

	//normal dotproduct lightdirection
	float	diffuseIntensity	=saturate(dot(-mLightDirection, worldNormal));

	float4	diffuseColor	=mLightColor * diffuseIntensity;
	diffuseColor.a	=1.0;

	output.Color	=diffuseColor + mAmbientColor;
	
	//direct copy of texfactors
	output.TexFactor0	=tfac0;
	output.TexFactor1	=tfac1;
	
	//store fog factor in color 4
	output.Color.w	=ComputeFogFactor(length(worldPos - mEyePos));

	//return the output structure
	return	output;
}

DepthVSOut WorldYVS(float4	position : POSITION)
{
	DepthVSOut	output;
	
	float4x4	localLevelWorld	=mul(mul(mLocal, mLevel), mWorld);
	float4x4	wvp				=mul(mul(localLevelWorld, mView), mProjection);

	float4	viewPos	=mul(localLevelWorld, position);
	output.Position	=mul(position, wvp);
	output.ViewY	=viewPos.y;
	
	return	output;
}

float4 WorldYPS(DepthVSOut input) : COLOR
{
	return	float4(input.ViewY, 0, 0, 0);
}

float4 LightDepthPS(DepthVSOut input) : COLOR
{
	return	float4(1, 1, 1, 1);
}

float3 ComputeShadowCoord(float4x4 lightViewProj, float4 worldPos)
{
	float3	shadCoord;

	//powerup near shadow calculation
	float4	lightPos		=mul(worldPos, lightViewProj);

	//texCoord xy, world depth in z
	shadCoord.xy	=0.5f * lightPos.xy / lightPos.w + float2(0.5f, 0.5f);
	shadCoord.z		=saturate((lightPos.z / lightPos.w) - 0.00001f);

	//flip y
	shadCoord.y	=1.0f - shadCoord.y;

	return	shadCoord;
}

float3 ApplyShadowExtraSamples(sampler shadSamp, float3 shadCoord, float3 texLitColor)
{
	float	depth0		=tex2D(shadSamp, shadCoord).r;

	shadCoord.x	+=0.0005f;
	shadCoord.y	+=0.0005f;
	float	depth1		=tex2D(shadSamp, shadCoord).r;

	shadCoord.x	-=0.001f;
	shadCoord.y	-=0.001f;
	float	depth2		=tex2D(shadSamp, shadCoord).r;

	shadCoord.y	+=0.001f;
	float	depth3		=tex2D(shadSamp, shadCoord).r;

	shadCoord.x	+=0.001f;
	float	depth4		=tex2D(shadSamp, shadCoord).r;

	if(depth0 < shadCoord.z)
	{
		texLitColor	*=0.3f;
	}
	if(depth1 < shadCoord.z)
	{
		texLitColor	*=0.9f;
	}
	if(depth2 < shadCoord.z)
	{
		texLitColor	*=0.9f;
	}
	if(depth3 < shadCoord.z)
	{
		texLitColor	*=0.9f;
	}
	if(depth4 < shadCoord.z)
	{
		texLitColor	*=0.9f;
	}
	return	texLitColor;
}

float3 ApplyShadow(sampler shadSamp, float3 shadCoord, float3 texLitColor)
{
	float	depth0		=tex2D(shadSamp, shadCoord).r;

	if(depth0 < shadCoord.z)
	{
		texLitColor	*=0.2f;
	}
	return	texLitColor;
}

float3 Compute8TexModulate(float2 worldXZ, float4 texFact0, float4 texFact1)
{
	float2	texCoord0	=worldXZ / SandDensity;
	float2	texCoord1	=worldXZ / GrassDensity;
	float2	texCoord2	=worldXZ / DirtDensity;
	float2	texCoord4	=worldXZ / MossyStoneDensity;
	float2	texCoord5	=worldXZ / GreyGraniteDensity;
	float2	texCoord6	=worldXZ / SnowDetailDensity;
	float2	texCoord7	=worldXZ / MossLayerDensity;

	float3	texColor	=float3(0, 0, 0);
	
	if(texFact0.x > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler0, texCoord0)), 2.2) * texFact0.x;
	}
	if(texFact0.y > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler1, texCoord1)), 2.2) * texFact0.y;
	}
	if(texFact0.z > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler2, texCoord2)), 2.2) * texFact0.z;
	}
	if(texFact1.x > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler4, texCoord4)), 2.2) * texFact1.x;
	}
	if(texFact1.y > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler5, texCoord5)), 2.2) * texFact1.y;
	}
	if(texFact1.z > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler6, texCoord6)), 2.2) * texFact1.z;
	}
	if(texFact1.w > 0.0)
	{
		texColor	+=pow(abs(tex2D(TexSampler7, texCoord7)), 2.2) * texFact1.w;
	}	
	return	texColor;
}

float4 Gourad8TexModulate(PSInput input) : COLOR
{
	float4	texLitColor;
	float2	worldXZ;

	worldXZ.x	=input.WorldPos.x;
	worldXZ.y	=input.WorldPos.z;

	texLitColor.xyz	=Compute8TexModulate(worldXZ, input.TexFactor0, input.TexFactor1);

	//grab fog factor
	float	fogFactor	=input.Color.w;
	
	texLitColor	*=input.Color;

	//set solid
	texLitColor.w	=1.0f;
							
	texLitColor.rgb	=lerp(texLitColor, mFogColor, fogFactor);

	//powerup near shadows
	float3	shadCoord	=ComputeShadowCoord(mPUPNearLightViewProj, input.WorldPos);
	texLitColor.xyz		=ApplyShadow(PUPNearShadowSampler, shadCoord, texLitColor.xyz);

	//powerup far shadows
	shadCoord		=ComputeShadowCoord(mPUPFarLightViewProj, input.WorldPos);
	texLitColor.xyz	=ApplyShadow(PUPFarShadowSampler, shadCoord, texLitColor.xyz);

	//powerup avatar shadows
	shadCoord		=ComputeShadowCoord(mAvaLightViewProj, input.WorldPos);
	texLitColor.xyz	=ApplyShadow(AvaShadowSampler, shadCoord, texLitColor.xyz);

	//back to srgb
	texLitColor.xyz	=pow(abs(texLitColor.xyz), 1 / 2.2);
	
	return	texLitColor;
}

float4 Gourad8TexModulateXSamp(PSInput input) : COLOR
{
	float4	texLitColor;
	float2	worldXZ;

	worldXZ.x	=input.WorldPos.x;
	worldXZ.y	=input.WorldPos.z;

	texLitColor.xyz	=Compute8TexModulate(worldXZ, input.TexFactor0, input.TexFactor1);

	//grab fog factor
	float	fogFactor	=input.Color.w;
	
	texLitColor	*=input.Color;

	//set solid
	texLitColor.w	=1.0f;
							
	texLitColor.rgb	=lerp(texLitColor, mFogColor, fogFactor);

	//powerup near shadows
	float3	shadCoord	=ComputeShadowCoord(mPUPNearLightViewProj, input.WorldPos);
	texLitColor.xyz		=ApplyShadowExtraSamples(PUPNearShadowSampler, shadCoord, texLitColor.xyz);

	//powerup far shadows
	shadCoord		=ComputeShadowCoord(mPUPFarLightViewProj, input.WorldPos);
	texLitColor.xyz	=ApplyShadowExtraSamples(PUPFarShadowSampler, shadCoord, texLitColor.xyz);

	//powerup avatar shadows
	shadCoord		=ComputeShadowCoord(mAvaLightViewProj, input.WorldPos);
	texLitColor.xyz	=ApplyShadowExtraSamples(AvaShadowSampler, shadCoord, texLitColor.xyz);
	
	//back to srgb
	texLitColor.xyz	=pow(abs(texLitColor.xyz), 1 / 2.2);

	return	texLitColor;
}

float4 SimplePS(PSInput input) : COLOR
{
	float4	texLitColor;
	float2	worldXZ;

	worldXZ.x	=input.WorldPos.x;
	worldXZ.y	=input.WorldPos.z;

	texLitColor.xyz	=float3(1, 1, 1);

	//grab fog factor
	float	fogFactor	=input.Color.w;
	
	texLitColor	*=input.Color;

	//set solid
	texLitColor.w	=1.0f;
							
//	texLitColor.rgb	=lerp(texLitColor, mFogColor, fogFactor);

	texLitColor.rgb	=CalcCellColor(texLitColor.rgb);

	texLitColor.rgb	=lerp(texLitColor, mFogColor, fogFactor);

	//back to srgb
//	texLitColor.xyz	=pow(texLitColor.xyz, 1 / 2.2);

	return	texLitColor;
}

float4 SimpleSkyGradientFogPS(PSInput input) : COLOR
{
	float3	texLitColor;
	float2	worldXZ;

	worldXZ.x	=input.WorldPos.x;
	worldXZ.y	=input.WorldPos.z;

	texLitColor.xyz	=float3(1, 1, 1);

	//grab fog factor
	float	fogFactor	=input.Color.w;
	
	texLitColor	*=input.Color;

	texLitColor	=CalcCellColor(texLitColor);

	float3	skyColor	=CalcSkyColorGradient(input.WorldPos.xyz);

	texLitColor.rgb	=lerp(texLitColor, skyColor, fogFactor);

	//back to srgb
//	texLitColor.xyz	=pow(texLitColor.xyz, 1 / 2.2);

	return	float4(texLitColor, 1);
}


technique WorldY
{
	pass P0
	{
		VertexShader	=compile vs_2_0 WorldYVS();
		PixelShader		=compile ps_2_0 WorldYPS();
	}
}

technique VertexLighting
{     
	pass P0
	{
		VertexShader	=compile vs_3_0 DiffuseGourad();
		PixelShader		=compile ps_3_0 Gourad8TexModulate();
	}
}

technique VertexLightingXSamp
{     
	pass P0
	{
		VertexShader	=compile vs_3_0 DiffuseGourad();
		PixelShader		=compile ps_3_0 Gourad8TexModulateXSamp();
	}
}

technique Simple
{
	pass P0
	{
		VertexShader	=compile vs_2_0 DiffuseGourad();
		PixelShader		=compile ps_2_0 SimpleSkyGradientFogPS();
	}
}