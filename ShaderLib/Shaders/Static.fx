//static fx, basic shaders
//texture layers used on the surface
texture	mTexture;
texture	mNMap;

shared float3	mLightDirection;
shared float4x4	mLightViewProj;	//for shadowing

float	mGlow;			//glow fakery
float4	mSolidColour;	//for non textured


#include "Types.fxh"
#include "CommonFunctions.fxh"


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


//regular N dot L lighting
VPosTex0Col0 GouradVS(VPosNormTex0 input)
{
	VPosTex0Col0	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	float3 worldNormal	=mul(input.Normal, mWorld);
	float	LdotN		=dot(worldNormal, mLightDirection);
	
	output.Color	=float4(LdotN, LdotN, LdotN, 1.0);
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	
	//return the output structure
	return	output;
}

//tangent stuff
VOutPosNormTanBiTanTex0 VSTan(VPosNormTanTex0 input)
{
	VOutPosNormTanBiTanTex0	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	output.Position		=mul(input.Position, wvp);
	output.Normal		=mul(input.Normal, mWorld);
	output.Tangent		=mul(input.Tangent.xyz, mWorld);
	output.TexCoord0	=input.TexCoord0;

	float3	biTan	=cross(input.Normal, input.Tangent) * input.Tangent.w;

	output.BiTangent	=normalize(biTan);

	//return the output structure
	return	output;
}

VPosTex0Col0 FullBrightVS(VPosNormTex0 input)
{
	VPosTex0Col0	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	output.Color	=float4(1.0, 1.0, 1.0, 1.0);
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	
	//return the output structure
	return	output;
}

VPosTex0Single ShadowVS(VPos input)
{
	VPosTex0Single	output;

	output.Position		=mul(input.Position, mul(mWorld, mLightViewProj));
	output.TexCoord0	=output.Position.z / output.Position.w;

	return	output;
}

VPosCol0 NormalDepthVS(VPosNormTex0 input)
{
	VPosCol0	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	//lighting calculation
	float3 worldNormal	=mul(input.Normal, mWorld);
	
	output.Color.rgb	=(worldNormal + 1) / 2;	
	output.Color.a		=output.Position.z / output.Position.w;
	
	return	output;
}


float4 TexColorPS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=texel0 * inColor;
	
	return	texLitColor;
}

float4 TexPS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	return	texel0;	
}

float4 ShadowPS(VTex0Single input) : COLOR
{
	return	float4(input.TexCoord0, 0, 0, 1);
}

float4 TexPSGlow(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	glow	=float4(mGlow, mGlow, mGlow, 1);
	
	texel0	=saturate(texel0 + glow);
	
	return	texel0;	
}

float4 TwoTexAddPS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * (texel0 + texel1);
	
	return	texLitColor;
}

float4 TwoTexModulatePS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0 * texel1;
	
	return	texLitColor;
}

float4 TwoTexDecalPS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * ((texel0 * texel0.a) + texel1);
	
	return	texLitColor;
}

float4 NormalMapPS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float lightIntensity	=saturate(dot(mLightDirection, texel1));
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0 * lightIntensity;
	
	return	texLitColor;
}

float4 NormalMapSolidPS(VNormTanBiTanTex0 input) : COLOR
{
	float4	norm	=tex2D(TexSamplerNorm, input.TexCoord0);

	//convert normal from 0 to 1 to -1 to 1
	norm	=2.0 * norm - float4(1.0, 1.0, 1.0, 1.0);

	float3x3	tbn	=float3x3(
					normalize(input.Tangent),
					normalize(input.BiTangent),
					normalize(input.Normal));
	
	//I borrowed a bunch of my math from GL samples thus
	//this is needed to get things back into XNA Land
	tbn	=transpose(tbn);

	//rotate normal into worldspace
	norm.xyz	=mul(tbn, norm.xyz);
	norm.xyz	=normalize(norm.xyz);

	float lightIntensity	=saturate(dot(mLightDirection, norm.xyz));
	
	float4	texLitColor	=mSolidColour * lightIntensity;

	texLitColor.w	=1.0;
	
	return	texLitColor;
}

//just return the color
float4 NormalDepthPS(float4 color : COLOR0) : COLOR0
{
	return	color;
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

technique GouradNormalMap
{
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
		PixelShader		=compile ps_2_0 NormalMapPS();
	}
}

technique GouradNormalMapSolid
{
	pass P0
	{
		VertexShader	=compile vs_2_0 VSTan();
		PixelShader		=compile ps_2_0 NormalMapSolidPS();
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

technique GouradTwoTexAdd
{
	pass P0
	{
		VertexShader	=compile vs_2_0 GouradVS();
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

technique NormalDepth
{
	pass P0
	{
		VertexShader	=compile vs_2_0 NormalDepthVS();
		PixelShader		=compile ps_2_0 NormalDepthPS();
	}
}

technique Shadow
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ShadowVS();
		PixelShader		=compile ps_2_0 ShadowPS();
	}
}