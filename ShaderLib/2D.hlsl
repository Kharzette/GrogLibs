//shader for UI and particles and such
Texture2D	mTexture, mTexture2;

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "RenderStates.fxh"

float2	mTextPosition, mSecondLayerOffset;
float2	mTextScale;
float4	mTextColor;


VVPosTex0 TextVS(VPos2Tex02 input)
{
	VVPosTex0	output;

	float4	pos;

	pos.xy	=(input.Position.xy * mTextScale) + mTextPosition;
	pos.z	=4;
	pos.w	=1;

	float4x4	viewProj	=mul(mView, mProjection);

	output.Position	=mul(pos, viewProj);

	output.TexCoord0.x	=input.TexCoord0.x;
	output.TexCoord0.y	=input.TexCoord0.y;

	return	output;
}


VVPosTex04 KeyedGumpVS(VPos2Tex04 input)
{
	VVPosTex04	output;

	float4	pos;

	pos.xy	=(input.Position.xy * mTextScale) + mTextPosition;
	pos.z	=4;
	pos.w	=1;

	float4x4	viewProj	=mul(mView, mProjection);

	output.Position	=mul(pos, viewProj);

	output.TexCoord0	=input.TexCoord04;

	return	output;
}


VVPosTex04Tex14 ParticleVS(VPos4Tex04Tex14 input)
{
	VVPosTex04Tex14	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.x;

	//copy color
	output.TexCoord1	=input.TexCoord1;

	float4x4	viewProj	=mul(mView, mProjection);

	//get matrix vectors
	float3	rightDir	=mView._m00_m10_m20;
	float3	upDir		=mView._m01_m11_m21;
	float3	viewDir		=mView._m02_m12_m22;

	//all verts at 000, add instance pos
	float4	pos	=float4(input.Position.xyz, 1);
	
	//store distance to eye
	output.TexCoord0.z	=distance(mEyePos, pos.xyz);

	//centering offset
	float3	centering	=-rightDir * input.TexCoord0.y;
	centering			-=upDir * input.TexCoord0.y;
	centering			*=0.5;

	//quad offset mul by size stored in tex0.y
	float4	ofs	=float4(rightDir * input.Position.w * input.TexCoord0.y, 1);
	ofs.xyz		+=upDir * input.TexCoord0.x * input.TexCoord0.y;

	//add in centerpoint
	ofs.xyz	+=pos.xyz;

	//center around pos
	ofs.xyz	+=centering;

	//screen transformed centerpoint
	float4	screenPos	=mul(pos, viewProj);

	//screen transformed quad position
	float4	screenOfs	=mul(ofs, viewProj);

	//subtract the centerpoint to just rotate the offset
	screenOfs	-=screenPos;

	//rotate ofs by rotation stored in tex0.z
	float	rot		=input.TexCoord0.z;
	float	cosRot	=cos(rot);
	float	sinRot	=sin(rot);

	//build a 2D rotation matrix
	float2x2	rotMat	=float2x2(cosRot, -sinRot, sinRot, cosRot);

	//rotation mul
	screenOfs.xy	=mul(screenOfs.xy, rotMat);

	output.Position	=screenPos + screenOfs;

	return	output;
}

VVPosTex04Tex14Tex24 ParticleDMNVS(VPos4Tex04Tex14 input)
{
	VVPosTex04Tex14Tex24	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.x;

	//copy color
	output.TexCoord1	=input.TexCoord1;

	float4x4	viewProj	=mul(mView, mProjection);

	//get matrix vectors
	float3	rightDir	=mView._m00_m10_m20;
	float3	upDir		=mView._m01_m11_m21;
	float3	viewDir		=mView._m02_m12_m22;

	//all verts at 000, add instance pos
	float4	pos	=float4(input.Position.xyz, 1);
	
	//copy world pos
	output.TexCoord2	=pos;

	//centering offset
	float3	centering	=-rightDir * input.TexCoord0.y;
	centering			-=upDir * input.TexCoord0.y;
	centering			*=0.5;

	//quad offset mul by size stored in tex0.y
	float4	ofs	=float4(rightDir * input.Position.w * input.TexCoord0.y, 1);
	ofs.xyz		+=upDir * input.TexCoord0.x * input.TexCoord0.y;

	//add in centerpoint
	ofs.xyz	+=pos.xyz;

	//center around pos
	ofs.xyz	+=centering;

	//screen transformed centerpoint
	float4	screenPos	=mul(pos, viewProj);

	//screen transformed quad position
	float4	screenOfs	=mul(ofs, viewProj);

	//subtract the centerpoint to just rotate the offset
	screenOfs	-=screenPos;

	//rotate ofs by rotation stored in tex0.z
	float	rot		=input.TexCoord0.z;
	float	cosRot	=cos(rot);
	float	sinRot	=sin(rot);

	//build a 2D rotation matrix
	float2x2	rotMat	=float2x2(cosRot, -sinRot, sinRot, cosRot);

	//rotation mul
	screenOfs.xy	=mul(screenOfs.xy, rotMat);

	output.Position	=screenPos + screenOfs;

	return	output;
}


float4 ParticlePS(VVPosTex04Tex14 input) : SV_Target
{
	//texture
	float4	texel	=mTexture.Sample(LinearWrap, input.TexCoord0.xy);

	//multiply by color
	texel	*=input.TexCoord1;

	return	texel;
}

float4 TextPS(VVPosTex0 input) : SV_Target
{
	//texture
	float4	texel	=mTexture.Sample(PointClamp, input.TexCoord0.xy);

	//multiply by color
	texel	*=mTextColor;

	return	texel;
}

float4 KeyedGumpPS(VVPosTex04 input) : SV_Target
{
	//2 layers
	//top is a mask
	//second draws multiplied by top alpha
	float4	texel	=mTexture.Sample(PointClamp, input.TexCoord0.xy);
	float4	texel2	=mTexture2.Sample(PointClamp,
		input.TexCoord0.zw + mSecondLayerOffset);

	//multiply under layer by color
	texel2	*=mTextColor;

	//mask
	texel2	*=texel.w;

	return	texel2;
}

float4 GumpPS(VVPosTex04 input) : SV_Target
{
	float4	texel	=mTexture.Sample(PointClamp, input.TexCoord0.xy);

	texel	*=mTextColor;

	return	texel;
}

struct TwoHalf4Targets
{
	half4	targ1, targ2;
};

//write to depth/material/normal if dense enough
TwoHalf4Targets ParticleDMNPS(VVPosTex04Tex14Tex24 input) : SV_Target
{
	//texture
	float4	texel	=mTexture.Sample(LinearWrap, input.TexCoord0.xy);

	//multiply by color
	texel	*=input.TexCoord1;

	float	colorAmount	=texel.x + texel.y + texel.z;

	//if color output is below threshold, clip
	clip(colorAmount - OUTLINE_ALPHA_THRESHOLD);

	TwoHalf4Targets	ret;

	ret.targ1.x		=1;	//the material stuff starts at 10, 0 is an occluder
	ret.targ1.yzw	=float3(1, 0, 0);
	ret.targ2		=input.TexCoord2;

	return	ret;
}


technique10 Particle
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 ParticleVS();
		PixelShader		=compile ps_5_0 ParticlePS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 ParticleVS();
		PixelShader		=compile ps_4_1 ParticlePS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 ParticleVS();
		PixelShader		=compile ps_4_0 ParticlePS();
#else
		VertexShader	=compile vs_4_0_level_9_3 ParticleVS();
		PixelShader		=compile ps_4_0_level_9_3 ParticlePS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
}

technique10 ParticleDMN
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 ParticleDMNVS();
		PixelShader		=compile ps_5_0 ParticleDMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 ParticleDMNVS();
		PixelShader		=compile ps_4_1 ParticleDMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 ParticleDMNVS();
		PixelShader		=compile ps_4_0 ParticleDMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 ParticleDMNVS();
		PixelShader		=compile ps_4_0_level_9_3 ParticleDMNPS();
#endif
	}
}

technique10 Text
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 TextVS();
		PixelShader		=compile ps_5_0 TextPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 TextVS();
		PixelShader		=compile ps_4_1 TextPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 TextVS();
		PixelShader		=compile ps_4_0 TextPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 TextVS();
		PixelShader		=compile ps_4_0_level_9_3 TextPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 KeyedGump
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 KeyedGumpVS();
		PixelShader		=compile ps_5_0 KeyedGumpPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 KeyedGumpVS();
		PixelShader		=compile ps_4_1 KeyedGumpPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 KeyedGumpVS();
		PixelShader		=compile ps_4_0 KeyedGumpPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 KeyedGumpVS();
		PixelShader		=compile ps_4_0_level_9_3 KeyedGumpPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}

technique10 Gump
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 KeyedGumpVS();
		PixelShader		=compile ps_5_0 GumpPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 KeyedGumpVS();
		PixelShader		=compile ps_4_1 GumpPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 KeyedGumpVS();
		PixelShader		=compile ps_4_0 GumpPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 KeyedGumpVS();
		PixelShader		=compile ps_4_0_level_9_3 GumpPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepth, 0);
	}
}