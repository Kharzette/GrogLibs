//Character - stuff with bones

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "Trilight.fxh"

//matrii for skinning
shared float4x4	mBones[MAX_BONES];

//for dangly shaders
shared float3	mDanglyForce;


//functions
//skinning with a dangly force applied
VPosTex03Tex13 ComputeSkinWorldDangly(VPosNormBoneCol0 input, float4x4 bones[MAX_BONES])
{
	VPosTex03Tex13	output;
	
	float4	vertPos	=input.Position;
	
	//generate view-proj matrix
	float4x4	vp	=mul(mView, mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, bones);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform to world
	float4	worldPos	=mul(vertPos, mWorld);

	//dangliness
	worldPos.xyz	-=input.Color.x * mDanglyForce;

	output.TexCoord1	=worldPos.xyz;

	//viewproj
	output.Position	=mul(worldPos, vp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal, skinTransform);
	
	//world transform the normal
	output.TexCoord0	=mul(worldNormal, mWorld);

	return	output;
}

//skin pos and normal
VPosNorm ComputeSkin(VPosNormBone input, float4x4 bones[MAX_BONES])
{
	VPosNorm	output;
	
	float4	vertPos	=input.Position;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, bones);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform the input position to the output
	output.Position	=mul(vertPos, wvp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal, skinTransform);
	
	//world transform the normal
	output.Normal	=mul(worldNormal, mWorld);

	return	output;
}

//compute the position and color of a skinned vert
VPosCol0 ComputeSkinTrilight(VPosNormBone input, float4x4 bones[MAX_BONES],
							 float3 lightDir, float4 c0, float4 c1, float4 c2)
{
	VPosCol0	output;
	VPosNorm	skinny	=ComputeSkin(input, bones);

	output.Position		=skinny.Position;	
	output.Color.xyz	=ComputeTrilight(skinny.Normal, lightDir, c0, c1, c2);
	output.Color.w		=1.0;
	
	return	output;
}

//skin with world info
VPosTex03Tex13 ComputeSkinWorld(VPosNormBone input, float4x4 bones[MAX_BONES])
{
	VPosTex03Tex13	output;
	
	float4	vertPos	=input.Position;
	
	//generate view-proj matrix
	float4x4	vp	=mul(mView, mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, bones);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform to world
	float4	worldPos	=mul(vertPos, mWorld);
	output.TexCoord1	=worldPos.xyz;

	//viewproj
	output.Position	=mul(worldPos, vp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal, skinTransform);
	
	//world transform the normal
	output.TexCoord0	=mul(worldNormal, mWorld);

	return	output;
}


//vertex shaders
//depth material normal
VPosTex03Tex13 DMNVS(VPosNormBone input)
{
	return	ComputeSkinWorld(input, mBones);
}

//dangly depth
VPosTex03Tex13 DMNDanglyVS(VPosNormBoneCol0 input)
{
	return	ComputeSkinWorldDangly(input, mBones);
}

//skin to world normal
VPosTex03 SkinWNormVS(VPosNormBone input)
{
	return	ComputeSkin(input, mBones);
}

//skin world norm and pos
VPosTex03Tex13 SkinWNormWPosVS(VPosNormBone input)
{
	return	ComputeSkinWorld(input, mBones);
}

//skin world pos
VPosTex03 SkinWPosVS(VPosBone input)
{
	float4	vertPos	=input.Position;

	float4x4	skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, mBones);

	vertPos	=mul(vertPos, skinTransform);

	float4	worldVertPos	=mul(vertPos, mWorld);

	VPosTex03	output;

	output.Position		=mul(worldVertPos, mLightViewProj);
	output.TexCoord0	=worldVertPos.xyz;

	return	output;
}

//skin, world norm, world pos, vert color
VPosTex04Tex14Tex24 SkinWNormWPosColorVS(VPosNormBoneCol0 input)
{
	VPosNormBone	inSkin;

	inSkin.Position	=input.Position;
	inSkin.Normal	=input.Normal;
	inSkin.Blend0	=input.Blend0;
	inSkin.Weight0	=input.Weight0;

	VPosTex03Tex13	skin	=ComputeSkinWorld(inSkin, mBones);

	VPosTex04Tex14Tex24	ret;

	ret.Position		=skin.Position;
	ret.TexCoord0.xyz	=skin.TexCoord0;
	ret.TexCoord1.xyz	=skin.TexCoord1;
	ret.TexCoord2		=input.Color;

	ret.TexCoord0.w	=0;
	ret.TexCoord1.w	=0;

	return	ret;
}

//vert color's red multiplies dangliness
VPosTex04Tex14 SkinDanglyWnormWPos(VPosNormBoneCol0 input)
{
	VPosTex03Tex13	skin	=ComputeSkinWorldDangly(input, mBones);

	VPosTex04Tex14	ret;

	ret.Position		=skin.Position;
	ret.TexCoord0.xyz	=skin.TexCoord0;
	ret.TexCoord1.xyz	=skin.TexCoord1;

	ret.TexCoord0.w	=0;
	ret.TexCoord1.w	=0;

	return	ret;
}

VPosTex0Col0 SkinTexTriColVS(VPosNormBoneTex0 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VPosTex0Col0		output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.Color		=singleOut.Color;
	
	return	output;
}

//skinned dual texcoord
VPosTex0Tex1Col0 SkinTex0Tex1TriColVS(VPosNormBoneTex0Tex1 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VPosTex0Tex1Col0	output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.Color		=singleOut.Color;
	
	return	output;
}


technique TriSkinTex0
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinTexTriColVS();
		PixelShader		=compile ps_4_0 Tex0Col0PS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinTexTriColVS();
		PixelShader		=compile ps_3_0 Tex0Col0PS();
#else
		VertexShader	=compile vs_2_0 SkinTexTriColVS();
		PixelShader		=compile ps_2_0 Tex0Col0PS();
#endif
	}
}

technique TriSkinSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinWNormWPosVS();
		PixelShader		=compile ps_3_0 TriSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 SkinWNormWPosVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();
#endif
	}
}

technique TriSkinCelSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinWNormWPosVS();
		PixelShader		=compile ps_3_0 TriCelSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 SkinWNormWPosVS();
		PixelShader		=compile ps_2_0 TriCelSolidSpecPhysPS();
#endif
	}
}

technique TriSkinDanglySolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0 TriSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_3_0 TriSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();
#endif
	}
}

technique TriSkinDanglyCelSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_3_0 TriCelSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_2_0 TriCelSolidSpecPhysPS();
#endif
	}
}

technique TriSkinCelColorSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosColorVS();
		PixelShader		=compile ps_4_0 TriCelColorSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinWNormWPosColorVS();
		PixelShader		=compile ps_3_0 TriCelColorSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 SkinWNormWPosColorVS();
		PixelShader		=compile ps_2_0 TriCelColorSpecPhysPS();
#endif
	}
}

technique TriSkinDecalTex0
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinTexTriColVS();
		PixelShader		=compile ps_4_0 Tex0Col0DecalPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinTexTriColVS();
		PixelShader		=compile ps_3_0 Tex0Col0DecalPS();
#else
		VertexShader	=compile vs_2_0 SkinTexTriColVS();
		PixelShader		=compile ps_2_0 Tex0Col0DecalPS();
#endif
	}
}

technique TriSkinDecalTex0Tex1
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_4_0 Tex0Tex1Col0DecalPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_3_0 Tex0Tex1Col0DecalPS();
#else
		VertexShader	=compile vs_2_0 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_2_0 Tex0Tex1Col0DecalPS();
#endif
	}
}

technique FullBrightSkin
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormVS();
		PixelShader		=compile ps_4_0	FullBrightSkinPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinWNormVS();
		PixelShader		=compile ps_3_0	FullBrightSkinPS();
#else
		VertexShader	=compile vs_2_0 SkinWNormVS();
		PixelShader		=compile ps_2_0	FullBrightSkinPS();
#endif
	}
}

technique ShadowSkin
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 SkinWPosVS();
		PixelShader		=compile ps_4_0 ShadowPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 SkinWPosVS();
		PixelShader		=compile ps_3_0 ShadowPS();
#else
		VertexShader	=compile vs_2_0 SkinWPosVS();
		PixelShader		=compile ps_2_0 ShadowPS();
#endif
	}
}

technique DMN	//depth material normal
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 DMNVS();
		PixelShader		=compile ps_4_0 DMNPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 DMNVS();
		PixelShader		=compile ps_3_0 DMNPS();
#else
		VertexShader	=compile vs_2_0 DMNVS();
		PixelShader		=compile ps_2_0 DMNPS();
#endif
	}
}

technique DMNDangly
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 DMNDanglyVS();
		PixelShader		=compile ps_4_0 DMNPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 DMNDanglyVS();
		PixelShader		=compile ps_3_0 DMNPS();
#else
		VertexShader	=compile vs_2_0 DMNDanglyVS();
		PixelShader		=compile ps_2_0 DMNPS();
#endif
	}
}