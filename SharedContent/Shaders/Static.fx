//ui fx, basic textured
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


//trilight
VPosTex0Col0 TrilightVS(VPosNormTex0 input)
{
	VPosTex0Col0	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	float3 worldNormal	=mul(input.Normal, mWorld);

	output.Color	=ComputeTrilight(worldNormal, mLightDirection,
						mLightColor0, mLightColor1, mLightColor2);
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	
	//return the output structure
	return	output;
}


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


VPosTex0SingleTex1 OutlineVS(VPosNormTex0 input)
{
	VPosTex0SingleTex1	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	//direct copy of texcoord0
	output.TexCoord0	=input.TexCoord0;
	
	//lighting calculation
	float3 worldNormal	=mul(input.Normal, mWorld);
	output.TexCoord1	=dot(worldNormal, mLightDirection);
	
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


//cartoony lighting
float4 ToonPS(VTex0SingleTex1 input) : COLOR0
{
	float4	color	
		=mbTextureEnabled ? tex2D(TexSampler0, input.TexCoord0) : 0;
		
	float	light;
	
	if(input.TexCoord1 > mToonThresholds[0])
	{
		light	=mToonBrightnessLevels[0];
	}
	else if(input.TexCoord1 > mToonThresholds[1])
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