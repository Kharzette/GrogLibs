//skinning shaders using TomF's trilights for light
//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mBindPose;
float4x4		mBones[MAX_BONES];

//texture layers used on the surface
texture	mTexture0;
texture mTexture1;

//material amb & diffuse
float4	mMatAmbient;
float4	mMatDiffuse;

//These are considered directional (no falloff)
float4	mLightColor0;		//trilights need 3 colors
float4	mLightColor1;		//trilights need 3 colors
float4	mLightColor2;		//trilights need 3 colors
float3	mLightDirection;


#include "Types.fxh"


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


//look up the skin transform
float4x4 GetSkinXForm(float4 bnIdxs, float4 bnWeights)
{
	float4x4 skinTransform	=0;
	skinTransform	+=mBones[bnIdxs.x] * bnWeights.x;
	skinTransform	+=mBones[bnIdxs.y] * bnWeights.y;
	skinTransform	+=mBones[bnIdxs.z] * bnWeights.z;
	skinTransform	+=mBones[bnIdxs.w] * bnWeights.w;
	
	return	skinTransform;
}


//compute the 3 light effects on the vert
float4 ComputeTrilight(float3 normal, float3 lightDir)
{
    float4	totalLight	=float4(0,0,0,1);
	float	LdotN		=dot(normal, lightDir);
	
	//trilight
	totalLight	+=(mLightColor0 * max(0, LdotN))
		+ (mLightColor1 * (1 - abs(LdotN)))
		+ (mLightColor2 * max(0, -LdotN));
		
	return	totalLight;
}


//compute the position and color of a skinned vert
VPosCol0 ComputeSkinTrilight(VPosNormBone input)
{
	VPosCol0	output;
	
	float4	vertPos	=mul(input.Position, mBindPose);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform the input position to the output
	output.Position	=mul(vertPos, wvp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal, skinTransform);
	
	//world transform the normal
	worldNormal	=mul(worldNormal, mWorld);
	
	output.Color	=ComputeTrilight(worldNormal, mLightDirection);
	
	return	output;
}


//vertex shader for skinned dual texcoord, single color
VPosTex0Tex1Col0 TrilightSkinTex0Tex1Col0VS(VPosNormBoneTex0Tex1 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert);
	
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
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert);
	
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