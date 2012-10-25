//shaders using TomF's trilights for light
//constants
#define	MAX_BONES		50
#define	PI_OVER_FOUR	0.7853981634f
#define	PI_OVER_TWO		1.5707963268f

//matrii for skinning
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

float4	mSolidColour;	//for non textured

//specular stuff
float4	mSpecColor;
float	mSpecPower;

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


VPosTex0Col0 TriTex0VS(VPosNormTex0 input)
{
	VPosTex0Col0	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(input.Position, wvp);
	
	float3 worldNormal	=mul(input.Normal, mWorld);

	output.Color.xyz	=ComputeTrilight(worldNormal, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);
	output.Color.w		=1.0f;
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	
	//return the output structure
	return	output;
}

VPosTex03Tex13 TriVS(VPosNormTex0 input)
{
	VPosTex03Tex13	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position		=mul(input.Position, wvp);
	output.TexCoord0	=mul(input.Normal, mWorld);
	output.TexCoord1	=mul(input.Position, mWorld);
	
	//return the output structure
	return	output;
}

VPosTex04Tex14 TriTex0PhysVS(VPosNormTex0 input)
{
	VPosTex04Tex14	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position			=mul(input.Position, wvp);
	output.TexCoord0.xyz	=mul(input.Normal, mWorld);
	output.TexCoord1.xyz	=mul(input.Position, mWorld);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.w		=input.TexCoord0.y;
	
	//return the output structure
	return	output;
}

//tangent stuff
VOutPosNormTanBiTanTex0 TriTanVS(VPosNormTanTex0 input)
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

//packed tangents with worldspace pos
VPosTex04Tex14Tex24Tex34 TriTanWorldVS(VPosNormTanTex0 input)
{
	VPosTex04Tex14Tex24Tex34	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	output.Position			=mul(input.Position, wvp);
	output.TexCoord0.xyz	=mul(input.Normal, mWorld);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.xyz	=mul(input.Tangent.xyz, mWorld);
	output.TexCoord1.w		=input.TexCoord0.y;

	float3	biTan	=cross(input.Normal, input.Tangent) * input.Tangent.w;

	output.TexCoord2		=float4(normalize(biTan), 0);
	output.TexCoord3		=mul(input.Position, mWorld);

	//return the output structure
	return	output;
}

//packed tangents with worldspace pos and instancing
VPosTex04Tex14Tex24Tex34 TriTanWorldInstancedVS(VPosNormTanTex0 input, float4x4 instWorld : BLENDWEIGHT)
{
	VPosTex04Tex14Tex24Tex34	output;

	float4x4	world	=transpose(instWorld);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(world, mView), mProjection);
	
	output.Position			=mul(input.Position, wvp);
	output.TexCoord0.xyz	=mul(input.Normal, world);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.xyz	=mul(input.Tangent.xyz, world);
	output.TexCoord1.w		=input.TexCoord0.y;

	float3	biTan	=cross(input.Normal, input.Tangent) * input.Tangent.w;

	output.TexCoord2		=float4(normalize(biTan), 0);
	output.TexCoord3		=mul(input.Position, world);

	//return the output structure
	return	output;
}

VPosTex0Col0 TriSkinTex0VS(VPosNormBoneTex0 input)
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

//skinned dual texcoord
VPosTex0Tex1Col0 TriSkinTex0Tex1VS(VPosNormBoneTex0Tex1 input)
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

//normal mapped from tex0
float4 NormalMapTriTex0PS(VNormTanBiTanTex0 input) : COLOR
{
	float4	norm	=tex2D(TexSampler0, input.TexCoord0);

	float3	goodNorm	=ComputeNormalFromMap(
		norm, input.Tangent, input.BiTangent, input.Normal);
	
	float3	texLitColor	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);
	
	return	float4(texLitColor, 1.0f);
}

//normal mapped from tex1, with tex0 texturing
float4 NormalMapTriTex0Tex1PS(VNormTanBiTanTex0Tex1 input) : COLOR
{
	float4	norm	=tex2D(TexSampler1, input.TexCoord1);
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);

	float3	goodNorm	=ComputeNormalFromMap(
		norm, input.Tangent, input.BiTangent, input.Normal);
	
	float3	texLitColor	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	texLitColor	*=texel0.xyz;
	
	return	float4(texLitColor, texel0.w);
}

//normal mapped from tex0, with solid color
float4 NormalMapTriTex0SolidPS(VNormTanBiTanTex0Tex1 input) : COLOR
{
	float4	norm	=tex2D(TexSampler0, input.TexCoord0);

	float3	goodNorm	=ComputeNormalFromMap(
		norm, input.Tangent, input.BiTangent, input.Normal);
	
	float3	texLitColor	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	texLitColor	*=mSolidColour.xyz;
	
	return	float4(texLitColor, mSolidColour.w);
}

//normal mapped from tex0, with solid color and specular
float4 NormalMapTriTex0SolidSpecPS(VTex04Tex14Tex24Tex34 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=tex2D(TexSampler0, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);

	float3	eyeVec	=normalize(mEyePos - wpos);

	float3	r	=normalize(2 * dot(mLightDirection, goodNorm) * goodNorm - mLightDirection);

	float	specDot	=dot(r, eyeVec);

	float3	texLitColor	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float4	spec	=mSpecPower * mSpecColor *
		max(pow(specDot, mSpecPower), 0) * length(texLitColor);

	texLitColor	*=mSolidColour.xyz;
	texLitColor	*=spec.xyz;
	
	return	float4(saturate(texLitColor), mSolidColour.w);
}

//normal mapped from tex0, with solid color and expensive specular
float4 NormalMapTriTex0SolidSpecPhysPS(VTex04Tex14Tex24Tex34 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=tex2D(TexSampler0, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	lightDir	=-mLightDirection;
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, goodNorm));
	float	ndoth	=saturate(dot(halfVec, goodNorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(triLight * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * triLight * fresTerm * visTerm * mLightColor2;
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//one set of texcoord
//normal mapped from tex1, texture from tex0, with solid color and expensive specular
float4 NormalMapTriTex0SpecPhysPS(VTex04Tex14Tex24Tex34 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=tex2D(TexSampler1, tex);
	float4	texCol	=tex2D(TexSampler0, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	lightDir	=-mLightDirection;
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, goodNorm));
	float	ndoth	=saturate(dot(halfVec, goodNorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(triLight * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * triLight * fresTerm * visTerm * mLightColor2;
	float3	litSolid	=texCol.xyz * triLight;

	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//Texture 0 and expensive specular
float4 TriTex0SpecPhysPS(VTex04Tex14 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	texColor	=tex2D(TexSampler0, tex);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	wpos	=input.TexCoord1.xyz;

	pnorm	=normalize(pnorm);

	float3	lightDir	=-mLightDirection;
	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(triLight * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * triLight * fresTerm * visTerm * mLightColor2;
	float3	litColor	=texColor.xyz * triLight;

	specular	=saturate((specular + litColor.xyz) * mSolidColour.xyz);

	return	float4(specular, texColor.w);
}

//Solid color and expensive specular
float4 TriSolidSpecPhysPS(VTex03Tex13 input) : COLOR
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	lightDir	=-mLightDirection;
	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	eyeVec	=normalize(mEyePos - wpos);
	float3	halfVec	=normalize(eyeVec + lightDir);
	float	ndotv	=saturate(dot(eyeVec, pnorm));
	float	ndoth	=saturate(dot(halfVec, pnorm));

	float	normalizationTerm	=(mSpecPower + 2.0f) / 8.0f;
	float	blinnPhong			=pow(ndoth, mSpecPower);
	float	specTerm			=normalizationTerm * blinnPhong;
	
	//fresnel stuff
	float	base		=1.0f - dot(halfVec, lightDir);
	float	exponential	=pow(base, 5.0f);
	float	fresTerm	=mSpecColor + (1.0f - mSpecColor) * exponential;

	//vis stuff
	float	alpha	=1.0f / (sqrt(PI_OVER_FOUR * mSpecPower + PI_OVER_TWO));
	float	visTerm	=(triLight * (1.0f - alpha) + alpha) *
				(ndotv * (1.0f - alpha) + alpha);

	visTerm	=1.0f / visTerm;

	float3	specular	=specTerm * triLight * fresTerm * visTerm * mLightColor2;
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid);

	return	float4(specular, mSolidColour.w);
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


technique TriTex0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTex0VS();
		PixelShader		=compile ps_2_0 Tex0Col0PS();
	}
}

technique TriTex0NormalMap
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0PS();
	}
}

technique TriTex0NormalMapSolid
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidPS();
	}
}

technique TriTex0NormalMapSolidSpec
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanWorldVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidSpecPS();
	}
}

technique TriTex0SpecPhys
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTex0PhysVS();
		PixelShader		=compile ps_2_0 TriTex0SpecPhysPS();
	}
}

technique TriSolidSpecPhys
{     
	pass P0
	{
		VertexShader	=compile vs_3_0 TriVS();
		PixelShader		=compile ps_3_0 TriSolidSpecPhysPS();
	}
}

technique TriTex0NormalMapSolidSpecPhys
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanWorldVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidSpecPhysPS();
	}
}

technique TriTex0NormalMapSpecPhys
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanWorldVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SpecPhysPS();
	}
}

technique TriTex0NormalMapSolidSpecPhysInstanced
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriTanWorldInstancedVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidSpecPhysPS();
	}
}

technique TriSkinTex0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriSkinTex0VS();
		PixelShader		=compile ps_2_0 Tex0Col0PS();
	}
}

technique TriSkinSolidSpecPhys
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriSkinTex0VS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();
	}
}

technique TriSkinDecalTex0
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriSkinTex0VS();
		PixelShader		=compile ps_2_0 Tex0Col0DecalPS();
	}
}

technique TriSkinDecalTex0Tex1
{     
	pass P0
	{
		VertexShader	=compile vs_2_0 TriSkinTex0Tex1VS();
		PixelShader		=compile ps_2_0 Tex0Tex1Col0DecalPS();
	}
}