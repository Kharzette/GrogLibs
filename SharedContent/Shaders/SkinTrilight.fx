//skinning shaders using TomF's trilights for light
//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mBindPose;
shared float4x4	mBones[MAX_BONES];

//texture layers used on the surface
texture	mTexture0;
texture mTexture1;

//These are considered directional (no falloff)
float4	mLightColor0;		//trilights need 3 colors
float4	mLightColor1;		//trilights need 3 colors
float4	mLightColor2;		//trilights need 3 colors
float3	mLightDirection;


#include "Types.fxh"
#include "CommonFunctions.fxh"


sampler TexSampler0 = sampler_state
{
	Texture	=(mTexture0);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler TexSampler1 = sampler_state
{
	Texture	=(mTexture1);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


//vertex shader for skinned dual texcoord, single color
VPosTex0Tex1Col0 TrilightSkinTex0Tex1Col0VS(VPosNormBoneTex0Tex1 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones, mBindPose,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VPosTex0Tex1Col0	output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.Color		=singleOut.Color;
	
	return	output;
}


//vertex shader for skinned single texcoord, single color
VPosTex0Col0 TrilightSkinTex0Col0VS(VPosNormBoneTex0 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones, mBindPose,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VPosTex0Col0		output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.Color		=singleOut.Color;
	
	return	output;
}


//single texture, single color modulated
float4 Tex0Col0PS(VTex0Col0 input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0;
	
	return	texLitColor;
}


//two texture lookups, but one set of texcoords
//alphas tex0 over tex1
float4 Tex0Col0DecalPS(VTex0Col0 input) : COLOR
{
	float4	texel0, texel1;
	texel0	=tex2D(TexSampler0, input.TexCoord0);
	texel1	=tex2D(TexSampler1, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=(texel1.w * texel1) + ((1.0 - texel1.w) * texel0);

	texLitColor	*=inColor;

	texLitColor.w	=1.0f;
	
	return	texLitColor;
}


//two texture lookups, 2 texcoord, alpha tex0 over tex1
float4 Tex0Tex1Col0DecalPS(VTex0Tex1Col0 input) : COLOR
{
	float4	texel0, texel1;
	texel0	=tex2D(TexSampler0, input.TexCoord0);
	texel1	=tex2D(TexSampler1, input.TexCoord1);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=(texel1.w * texel1) + ((1.0 - texel1.w) * texel0);

	texLitColor	*=inColor;

	texLitColor.w	=1.0f;
	
	return	texLitColor;
}


technique TriSkinTex0Col0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightSkinTex0Col0VS();
		PixelShader		=compile ps_2_0 Tex0Col0PS();
	}
}

technique TriSkinDecalTex0Col0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightSkinTex0Col0VS();
		PixelShader		=compile ps_2_0 Tex0Col0DecalPS();
	}
}

technique TriSkinDecalTex0Tex1Col0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TrilightSkinTex0Tex1Col0VS();
		PixelShader		=compile ps_2_0 Tex0Tex1Col0DecalPS();
	}
}