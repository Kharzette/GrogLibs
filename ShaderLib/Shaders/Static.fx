//static fx, basic shaders
//texture layers used on the surface
texture	mTexture;
texture	mNMap;

shared float3	mLightDirection;

float	mGlow;			//glow fakery
float4	mSolidColour;	//for non textured
float	mFarClip;


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


//worldpos + regular
VPosTex04 BasicVS(VPosNormTex0 input)
{
	VPosTex04	output;

	float4x4	viewProj	=mul(mView, mProjection);

	//view relative pos
	output.TexCoord0	=mul(input.Position, mWorld);

	//transformed
	output.Position		=mul(output.TexCoord0, viewProj);

	return	output;
}


//instanced
VPosTex04 BasicInstancedVS(VPosNormTex0 input, float4x4 instWorld : BLENDWEIGHT)
{
	VPosTex04	output;

	float4x4	viewProj	=mul(mView, mProjection);

	//view relative pos
	output.TexCoord0	=mul(input.Position, transpose(instWorld));

	//transformed
	output.Position		=mul(output.TexCoord0, viewProj);

	return	output;
}


//regular N dot L lighting
VPosTex03 PhongSolidVS(VPosNormTex0 input)
{
	VPosTex03	output;

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	output.Position		=mul(input.Position, wvp);
	output.TexCoord0	=mul(input.Normal, mWorld);

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

VPosTex0Single ShadowInstancedVS(VPos input, float4x4 instWorld : BLENDWEIGHT)
{
	VPosTex0Single	output;

	output.Position		=mul(input.Position, mul(transpose(instWorld), mLightViewProj));
	output.TexCoord0	=output.Position.z / output.Position.w;

	return	output;
}

VPosTex0Single ShadowVS(VPos input)
{
	VPosTex0Single	output;

	output.Position		=mul(input.Position, mul(mWorld, mLightViewProj));
	output.TexCoord0	=output.Position.z / output.Position.w;

	return	output;
}

VPosTex0Single AvatarShadowVS(VPos input)
{
	VPosTex0Single	output;

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	output.Position		=mul(input.Position, wvp);
	output.TexCoord0	=output.Position.z / output.Position.w;

	return	output;
}

VPosTex04 NormalDepthVS(VPosNormTex0 input)
{
	VPosTex04	output;
	
	float4x4	worldView	=mul(mWorld, mView);
	float4x4	wvp			=mul(worldView, mProjection);

	//surface normals in view space
	output.Position			=mul(input.Position, wvp);
	output.TexCoord0.xyz	=mul(input.Normal, worldView);
	output.TexCoord0.w		=output.Position.z / mFarClip;
	
	return	output;
}

VPosTex03 ParticleVS(VPosTex04 input)
{
	VPosTex03	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=input.TexCoord0.w;

	//copy alpha
	output.TexCoord0.z	=input.TexCoord0.z;

	float4x4	viewProj	=mul(mView, mProjection);

	//get view vector
	float3	viewDir	=mView._m02_m12_m22;

	//all verts at 000, add instance pos
	float4	pos	=input.Position;
	
	//cross with up to get right
	float3	rightVec	=normalize(cross(viewDir, float3(0, 1, 0)));

	//cross right with view to get good up
	float3	upVec	=normalize(cross(rightVec, viewDir));

	float4	ofs	=pos;

	//quad offset mul by size stored in tex0.x
	ofs.xyz	=rightVec * (input.Position.w - 0.5f) * input.TexCoord0.x;
	ofs.xyz	+=upVec * (input.TexCoord0.w - 0.5f) * input.TexCoord0.x;

	//add in centerpoint
	ofs	+=pos;

	//reset w's
	pos.w	=1.0f;
	ofs.w	=1.0f;

	//screen transformed centerpoint
	float4	screenPos	=mul(pos, viewProj);

	//screen transformed quad position
	float4	screenOfs	=mul(ofs, viewProj);

	//subtract the centerpoint to just rotate the offset
	screenOfs	-=screenPos;

	//rotate ofs by rotation stored in tex0.y
	float	rot		=input.TexCoord0.y;
	float	cosRot	=cos(rot);
	float	sinRot	=sin(rot);

	//build a 2D rotation matrix
	float2x2	rotMat	=float2x2(cosRot, -sinRot, sinRot, cosRot);

	//rotation mul
	screenOfs.xy	=mul(screenOfs.xy, rotMat);

	output.Position	=screenPos + screenOfs;

	return	output;
}


float4 ParticlePS(VTex03 input) : COLOR
{
	//texture
	float4	texel	=tex2D(TexSampler0, input.TexCoord0.xy);

	//gamma
	texel	=pow(abs(texel), 2.2);

	float4	texLitColor	=texel * mSolidColour;

	//store alpha in z
	texLitColor.w	*=input.TexCoord0.z;

	texLitColor	=pow(abs(texLitColor), 1 / 2.2);

	return	texLitColor;
}


float4 ParticleCellPS(VTex03 input) : COLOR
{
	//texture
	float4	texel	=tex2D(TexSampler0, input.TexCoord0.xy);

	//gamma
	float4	texLitColor	=pow(abs(texel), 2.2);

	//store alpha in z
	texLitColor.w	*=input.TexCoord0.z;

	texLitColor	*=mSolidColour;

	texLitColor.xyz	=CalcCellColor(texLitColor.xyz);

	texLitColor	=pow(abs(texLitColor), 1 / 2.2);

	return	texLitColor;
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

float4	PhongSolidPS(VTex03 input) : COLOR
{
	float lightIntensity	=saturate(dot(-mLightDirection, normalize(input.TexCoord0)));
	
	float4	texLitColor	=mSolidColour * lightIntensity;

	return	texLitColor;
}

float4 ShadowPS(VTex0Single input) : COLOR
{
	return	float4(input.TexCoord0, 0, 0, 0);
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

//return entire component (xyz == world normal, w == depth)
float4 NormalDepthPS(float4 normDepth : TEXCOORD0) : COLOR0
{
	return	normDepth;
}

//return the normal component
float4 NormalPS(float4 normDepth : TEXCOORD0) : COLOR0
{
	return	float4(normDepth.xyz, 0);
}

//return the depth component
float4 DepthPS(float4 normDepth : TEXCOORD0) : COLOR0
{
	return	float4(normDepth.w, 0, 0, 0);
}

//write world coord Y to single rendertarget
float4 WorldYPS(VTex04 input) : COLOR
{
	return	float4(input.TexCoord0.y, 0, 0, 0);
}

//gradient sky
float4 SkyGradientPS(VTex04 input) : COLOR
{
	return	float4(CalcSkyColorGradient(input.TexCoord0), 1);
}

float4 HalfTransPS(VTex04 input) : COLOR
{
	return	float4(1, 1, 1, 0.5);
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

technique DepthOnly
{
	pass P0
	{
		VertexShader	=compile vs_2_0 NormalDepthVS();
		PixelShader		=compile ps_2_0 DepthPS();
	}
}

technique NormalOnly
{
	pass P0
	{
		VertexShader	=compile vs_2_0 NormalDepthVS();
		PixelShader		=compile ps_2_0 NormalPS();
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

technique ShadowInstanced
{
	pass P0
	{
		VertexShader	=compile vs_2_0 ShadowInstancedVS();
		PixelShader		=compile ps_2_0 ShadowPS();
	}
}

technique SkyGradient
{
	pass P0
	{
		VertexShader	=compile vs_2_0 BasicVS();
		PixelShader		=compile ps_2_0 SkyGradientPS();
	}
}

technique PhongSolid
{
	pass P0
	{
		VertexShader	=compile vs_2_0 PhongSolidVS();
		PixelShader		=compile ps_2_0 PhongSolidPS();
	}
}

technique AvatarShadow
{
	pass P0
	{
		VertexShader	=compile vs_2_0 AvatarShadowVS();
		PixelShader		=compile ps_2_0 ShadowPS();
	}
}

technique WorldY
{
	pass P0
	{
		VertexShader	=compile vs_2_0 BasicVS();
		PixelShader		=compile ps_2_0 WorldYPS();
	}
}

technique WorldYInstanced
{
	pass P0
	{
		VertexShader	=compile vs_2_0 BasicInstancedVS();
		PixelShader		=compile ps_2_0 WorldYPS();
	}
}

technique TransparentInstanced
{
	pass P0
	{
		VertexShader	=compile vs_2_0 BasicInstancedVS();
		PixelShader		=compile ps_2_0 HalfTransPS();
	}
}

technique Particle
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 ParticleVS();
		PixelShader		=compile ps_2_0 ParticlePS();
	}
}

technique ParticleCell
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 ParticleVS();
		PixelShader		=compile ps_2_0 ParticleCellPS();
	}
}