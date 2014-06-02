//shader for UI and particles and such
Texture2D	mTexture;

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "RenderStates.fxh"


VVPosTex03 ParticleVS(VPos4Tex04 input)
{
	VVPosTex03	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.w;

	//copy alpha
	output.TexCoord0.z	=input.TexCoord0.z;

	float4x4	viewProj	=mul(mView, mProjection);

	//get view vector
	float3	viewDir	=-mView._m02_m12_m22;

	//all verts at 000, add instance pos
	float4	pos	=float4(input.Position.xyz, 1);
	
	//cross with up to get right
	float3	rightVec	=normalize(cross(viewDir, float3(0, 1, 0)));

	//cross right with view to get good up
	float3	upVec	=normalize(cross(rightVec, viewDir));

	//quad offset mul by size stored in tex0.x
	float4	ofs	=float4(rightVec * (input.Position.w - 0.5f) * input.TexCoord0.x, 1);
	ofs.xyz		+=upVec * (input.TexCoord0.w - 0.5f) * input.TexCoord0.x;

	//add in centerpoint
	ofs.xyz	+=pos.xyz;

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

VVPosTex03Tex13 ParticleDMNVS(VPos4Tex04 input)
{
	VVPosTex03Tex13	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.w;

	//copy alpha
	output.TexCoord0.z	=input.TexCoord0.z;

	float4x4	viewProj	=mul(mView, mProjection);

	//get view vector
	float3	viewDir	=-mView._m02_m12_m22;

	//all verts at 000, add instance pos
	float4	pos	=float4(input.Position.xyz, 1);
	
	//cross with up to get right
	float3	rightVec	=normalize(cross(viewDir, float3(0, 1, 0)));

	//cross right with view to get good up
	float3	upVec	=normalize(cross(rightVec, viewDir));

	//quad offset mul by size stored in tex0.x
	float4	ofs	=float4(rightVec * (input.Position.w - 0.5f) * input.TexCoord0.x, 1);
	ofs.xyz		+=upVec * (input.TexCoord0.w - 0.5f) * input.TexCoord0.x;

	//add in centerpoint
	ofs.xyz	+=pos.xyz;

	output.TexCoord1	=pos.xyz;

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


float4 ParticlePS(VVPosTex03 input) : SV_Target
{
	//texture
	float4	texel	=mTexture.Sample(LinearWrap, input.TexCoord0.xy);

	//gamma
	texel	=pow(abs(texel), 2.2);

	float4	texLitColor	=texel * mSolidColour;

	//store alpha in z
	texLitColor.w	*=input.TexCoord0.z;

	texLitColor	=pow(abs(texLitColor), 1 / 2.2);

	return	texLitColor;
}

//write to depth/material/normal if dense enough
half4 ParticleDMNPS(VVPosTex03Tex13 input) : SV_Target
{
	//texture
	float4	texel	=mTexture.Sample(LinearWrap, input.TexCoord0.xy);

	//gamma
	float4	texLitColor	=pow(abs(texel), 2.2);

	texLitColor.w	*=input.TexCoord0.z;

	texLitColor	*=mSolidColour;

	clip(texLitColor.w - OUTLINE_ALPHA_THRESHOLD);

	half4	ret;

	ret.x	=distance(input.TexCoord1, mEyePos);
	ret.y	=1;	//the material stuff starts at 10, 0 is an occluder
	ret.zw	=EncodeNormal(float3(1, 0, 0));

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
