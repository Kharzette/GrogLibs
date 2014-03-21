//shader for UI and particles and such
texture	mTexture;

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


VPosTex03 ParticleVS(VPosTex04 input)
{
	VPosTex03	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.w;

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

VPosTex03Tex13 ParticleDMNVS(VPosTex04 input)
{
	VPosTex03Tex13	output;

	//copy texcoords
	output.TexCoord0.x	=input.Position.w;
	output.TexCoord0.y	=-input.TexCoord0.w;

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

float4 ParticleCelPS(VTex03 input) : COLOR
{
	//texture
	float4	texel	=tex2D(TexSampler0, input.TexCoord0.xy);

	//gamma
	float4	texLitColor	=pow(abs(texel), 2.2);

	//store alpha in z
	texLitColor.w	*=input.TexCoord0.z;

	texLitColor	*=mSolidColour;

	texLitColor.xyz	=CalcCelColor(texLitColor.xyz);

	texLitColor	=pow(abs(texLitColor), 1 / 2.2);

	return	texLitColor;
}

//write to depth/material/normal if dense enough
half4 ParticleDMNPS(VTex03Tex13 input) : COLOR
{
	//texture
	float4	texel	=tex2D(TexSampler0, input.TexCoord0.xy);

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


technique ParticleCel
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 ParticleVS();
		PixelShader		=compile ps_2_0 ParticleCelPS();
	}
}

technique Particle
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 ParticleVS();
		PixelShader		=compile ps_4_0 ParticlePS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 ParticleVS();
		PixelShader		=compile ps_3_0 ParticlePS();
#else
		VertexShader	=compile vs_2_0 ParticleVS();
		PixelShader		=compile ps_2_0 ParticlePS();
#endif
	}
}

technique ParticleDMN
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 ParticleDMNVS();
		PixelShader		=compile ps_4_0 ParticleDMNPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 ParticleDMNVS();
		PixelShader		=compile ps_3_0 ParticleDMNPS();
#else
		VertexShader	=compile vs_2_0 ParticleDMNVS();
		PixelShader		=compile ps_2_0 ParticleDMNPS();
#endif
	}
}
