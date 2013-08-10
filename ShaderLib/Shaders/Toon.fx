#include "Types.fxh"
#include "CommonFunctions.fxh"

//cartoon shading stuff

//matrii for skinning
shared float4x4	mBones[MAX_BONES];

//texture layers used on the surface
texture	mTexture;
texture mTexture2;
float2	mTexSize;	//for lightmap

shared float3 mLightDirection;

//for dangly hair type shader
float3	mHeadDelta;

//for non textured
float4	mMatColor;

//outline / toon related
float	mToonThresholds[4] = { 0.6, 0.4, 0.25, 0.1 };
float	mToonBrightnessLevels[5] = { 1.0f, 0.7f, 0.5f, 0.2f, 0.05f };
bool	mbTextureEnabled;


sampler TexSampler0 = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler TexSampler1 = sampler_state
{
	Texture	=(mTexture2);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


float CalcToonLight(float lightVal)
{
	float	light;

	float	d	=lightVal * 0.33;

	if(d > mToonThresholds[0])
	{
		light	=mToonBrightnessLevels[0];
	}
	else if(d > mToonThresholds[1])
	{
		light	=mToonBrightnessLevels[1];
	}
	else if(d > mToonThresholds[2])
	{
		light	=mToonBrightnessLevels[2];
	}
	else if(d > mToonThresholds[3])
	{
		light	=mToonBrightnessLevels[3];
	}
	else
	{
		light	=mToonBrightnessLevels[4];
	}

	return	light;
}


float CalcToonLight3(float3 lightVal)
{
	float	light;

	float	d	=lightVal.x + lightVal.y + lightVal.z;

	return	CalcToonLight(d);
}


VPosTex0Tex1Single DanglyToonVS(VPosNormTex0Col0 input)
{
	VPosTex0Tex1Single	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	//direct copy of texcoord0
	output.TexCoord0	=input.TexCoord0;

	//lighting calculation
	float3 worldNormal	=mul(input.Normal, mWorld);
	output.TexCoord1	=dot(worldNormal, mLightDirection);

	//vert color used as a weight for wiggles
	output.Position	-=float4(input.Color.x * mHeadDelta, 0.0);
	
	return	output;
}


VPosTex0Tex1Single ToonTex0VS(VPosNormTex0 input)
{
	VPosTex0Tex1Single	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	//direct copy of texcoord0
	output.TexCoord0	=input.TexCoord0;

	//lighting calculation
	float3 worldNormal	=normalize(mul(input.Normal, mWorld));
	output.TexCoord1	=dot(worldNormal, mLightDirection);
	
	return	output;
}


VPosTex0Tex1 ToonTex0Tex1VS(VPosNormTex0Tex1 input)
{
	VPosTex0Tex1	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	
	return	output;
}


VPosTex0Tex1Single ToonSkinTex0VS(VPosNormBoneTex0 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;

	VPosNorm	skOut	=ComputeSkin(skVert, mBones);
	
	VPosTex0Tex1Single	output;
	output.Position		=skOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=dot(skOut.Normal, mLightDirection);
	
	return	output;
}


VPosTex0Tex13 ToonSkinTex0NMapVS(VPosNormBoneTex0 input)
{
	VPosTex0Tex13	output;

	float4	vertPos	=input.Position;

	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	float4x4	skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, mBones);

	//store light direction in the tex1
	output.TexCoord1	=mul(mLightDirection, skinTransform);
	output.Position		=mul(vertPos, wvp);
	output.TexCoord0	=input.TexCoord0;
	
	return	output;
}


VPosTex0Tex1Single ToonSkinTex0DanglyVS(VPosNormBoneTex0Col0 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;

	VPosNorm	skOut	=ComputeSkin(skVert, mBones);
	
	VPosTex0Tex1Single	output;
	output.Position		=skOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=dot(skOut.Normal, mLightDirection);

	//vert color used as a weight for wiggles
	output.Position	-=float4(input.Color.x * mHeadDelta, 0.0);
	
	return	output;
}


//cartoony lighting
float4 ToonPS(VTex0Tex1Single input) : COLOR0
{
	float4	color	
		=mbTextureEnabled ? tex2D(TexSampler0, input.TexCoord0) : mMatColor;

	float	light	=CalcToonLight(input.TexCoord1);
	
	color.rgb	*=light;
	
	return	color;
}

float4 ToonNormalMapPS(VTex0Tex13 input) : COLOR0
{
	float4	color	
		=mbTextureEnabled ? tex2D(TexSampler0, input.TexCoord0) : mMatColor;

	float3	norm	=tex2D(TexSampler1, input.TexCoord0);

	float	d	=dot(norm, input.TexCoord1);
		
	float	light	=CalcToonLight(d);
	
	color.rgb	*=light;
	
	return	color;
}


technique ToonTex0
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ToonTex0VS();
		PixelShader		=compile ps_2_0	ToonPS();
	}
}

technique ToonTex0Dangly
{
	pass P0
	{
		VertexShader	=compile vs_2_0 DanglyToonVS();
		PixelShader		=compile ps_2_0	ToonPS();
	}
}

technique ToonSkinTex0
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ToonSkinTex0VS();
		PixelShader		=compile ps_2_0	ToonPS();
	}
}

technique ToonSkinTex0Dangly
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ToonSkinTex0DanglyVS();
		PixelShader		=compile ps_2_0	ToonPS();
	}
}

technique ToonSkinTex0Norm1
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ToonSkinTex0VS();
		PixelShader		=compile ps_2_0	ToonNormalMapPS();
	}
}