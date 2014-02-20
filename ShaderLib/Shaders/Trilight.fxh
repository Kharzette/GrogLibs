//shaders using TomF's trilights for light
//see http://home.comcast.net/~tom_forsyth/blog.wiki.html#Trilights

//texture layers used on the surface
shared texture	mTexture0;
shared texture	mTexture1;

//These are considered directional (no falloff)
shared float4	mLightColor0;		//trilights need 3 colors
shared float4	mLightColor1;		//trilights need 3 colors
shared float4	mLightColor2;		//trilights need 3 colors
shared float3	mLightDirection;

shared sampler TexSampler0 = sampler_state
{
	Texture		=mTexture0;

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

shared sampler TexSampler1 = sampler_state
{
	Texture		=mTexture1;

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


//shared pixel shaders
//just shows the shape for debugging
float4 FullBrightSkinPS(VTex03 input) : COLOR
{
	return	mSolidColour;
}

//writes distance into a float rendertarget
float4 ShadowPS(VTex03 input) : COLOR
{
	float	dist	=distance(mShadowLightPos, input.TexCoord0);

	return	float4(dist, 0, 0, 0);
}

//single texture, single color modulated
float4 Tex0Col0PS(VTex0Col0 input) : COLOR
{
	float4	texel	=tex2D(TexSampler0, input.TexCoord0);
	
	//gamma
	texel	=pow(abs(texel), 2.2);

	float4	inColor		=input.Color;	
	float4	texLitColor	=inColor * texel;
	
	return	pow(abs(texLitColor), 1 / 2.2);
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

//Solid color, trilight, and expensive specular
float4 TriSolidSpecPhysPS(VTex03Tex13 input) : COLOR
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid);

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex0, with solid color & trilight
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

//normal mapped from tex0, with solid color and trilight and specular
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
		max(pow(abs(specDot), mSpecPower), 0) * length(texLitColor);

	texLitColor	*=mSolidColour.xyz;
	texLitColor	*=spec.xyz;
	
	return	float4(saturate(texLitColor), mSolidColour.w);
}

//Texture 0, trilight, and expensive specular
float4 TriTex0SpecPhysPS(VTex04Tex14 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	texColor	=tex2D(TexSampler0, tex);

	//gamma
	texColor	=pow(abs(texColor), 2.2);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	wpos	=input.TexCoord1.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);
#endif

	float3	litColor	=texColor.xyz * triLight;

	//correct here or after specular?
	litColor	=pow(abs(litColor), 1 / 2.2);
	specular	=saturate((specular + litColor.xyz) * mSolidColour.xyz);

	return	float4(specular, texColor.w);
}

//cel shading, solid color, trilight and expensive specular
float4 TriCelSolidSpecPhysPS(VTex03Tex13 input) : COLOR
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	//this quantizes the light value
#if defined(CELLIGHT)
	triLight	=CalcCelColor(triLight);
#endif

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);

	//this will quantize the specularity as well
#if defined(CELSPECULAR)
	specular	=CalcCelColor(specular);
#endif

	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid);

	//for super retro goofy color action
#if defined(CELALL)
	specular	=CalcCelColor(specular);
#endif

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex0, with solid color, trilight and expensive specular
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
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
	float3	litSolid	=mSolidColour.xyz * triLight;

	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//normal mapped from tex1, texture from tex0, with color tinting, trilight and expensive specular
float4 NormalMapTriTex0SpecPhysPS(VTex04Tex14Tex24Tex34 input) : COLOR
{
	float2	tex;

	tex.x	=input.TexCoord0.w;
	tex.y	=input.TexCoord1.w;

	float4	norm	=tex2D(TexSampler1, tex);
	float4	texCol	=tex2D(TexSampler0, tex);

	//gamma
	texCol	=pow(abs(texCol), 2.2);

	float3	pnorm	=input.TexCoord0.xyz;
	float3	tan		=input.TexCoord1.xyz;
	float3	bitan	=input.TexCoord2.xyz;
	float3	wpos	=input.TexCoord3.xyz;

	float3	goodNorm	=ComputeNormalFromMap(norm, tan, bitan, pnorm);
	float3	triLight	=ComputeTrilight(goodNorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

#if defined(SM2)
	float3	specular	=ComputeCheapSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
#else
	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, goodNorm, triLight, mLightColor2);
#endif
	float3	litSolid	=texCol.xyz * triLight;

	//gamma here or after specular?
	litSolid	=pow(abs(litSolid), 1 / 2.2);
	specular	=saturate(specular + litSolid.xyz);

	return	float4(specular, mSolidColour.w);
}

//cel, passed in color and expensive specular
float4 TriCelColorSpecPhysPS(VTex04Tex14Tex24 input) : COLOR
{
	float3	pnorm	=input.TexCoord0;
	float3	wpos	=input.TexCoord1;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	//this quantizes the light value
#if defined(CELLIGHT)
	triLight	=CalcCelColor(triLight);
#endif

	float3	specular	=ComputeGoodSpecular(wpos, mLightDirection, pnorm, triLight, mLightColor2);

	//this will quantize the specularity as well
#if defined(CELSPECULAR)
	specular	=CalcCelColor(specular);
#endif

	float3	litSolid	=input.TexCoord2.xyz * triLight;

	specular	=saturate(specular + litSolid);

	//for super retro goofy color action
#if defined(CELALL)
	specular	=CalcCelColor(specular);
#endif

	return	float4(specular, input.TexCoord2.w);
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