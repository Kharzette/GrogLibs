//Character - stuff with bones

#include "Types.hlsli"
#include "CommonFunctions.hlsli"


cbuffer Character : register(b4)
{
	//matrii for skinning
	float4x4	mBones[MAX_BONES];	
}


//functions
//skinning with a dangly force applied
VVPosTex03Tex13 ComputeSkinWorldDangly(VPosNormBoneCol0 input, float4x4 bones[MAX_BONES])
{
	VVPosTex03Tex13	output;
	
	float4	vertPos	=float4(input.Position, 1);
	
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
	float3	worldNormal	=mul(input.Normal.xyz, skinTransform);
	
	//world transform the normal
	output.TexCoord0	=mul(worldNormal, mWorld);

	return	output;
}

//skin pos and normal
VVPosNorm ComputeSkin(VPosNormBone input, float4x4 bones[MAX_BONES])
{
	VVPosNorm	output;
	
	float4	vertPos	=float4(input.Position, 1);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, bones);
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform the input position to the output
	output.Position	=mul(vertPos, wvp);

	//skin transform the normal
	float3	worldNormal	=mul(input.Normal.xyz, skinTransform);
	
	//world transform the normal
	output.Normal	=mul(worldNormal, mWorld);

	return	output;
}

//compute the position and color of a skinned vert
VVPosCol0 ComputeSkinTrilight(VPosNormBone input, float4x4 bones[MAX_BONES],
							 float3 lightDir, float4 c0, float4 c1, float4 c2)
{
	VVPosCol0	output;
	VVPosNorm	skinny	=ComputeSkin(input, bones);

	output.Position		=skinny.Position;	
	output.Color.xyz	=ComputeTrilight(skinny.Normal.xyz, lightDir, c0, c1, c2);
	output.Color.w		=1.0;
	
	return	output;
}

//skin with world info
VVPosTex03Tex13 ComputeSkinWorld(VPosNormBone input, float4x4 bones[MAX_BONES])
{
	VVPosTex03Tex13	output;
	
	float4	vertPos	=float4(input.Position, 1);
	
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
	float3	worldNormal	=mul(input.Normal.xyz, skinTransform);
	
	//world transform the normal
	output.TexCoord0	=mul(worldNormal, mWorld);

	return	output;
}


//vertex shaders
//depth material normal
VVPosTex03Tex13 DMNVS(VPosNormBone input)
{
	return	ComputeSkinWorld(input, mBones);
}

//dangly depth
VVPosTex03Tex13 DMNDanglyVS(VPosNormBoneCol0 input)
{
	return	ComputeSkinWorldDangly(input, mBones);
}

//skin to world normal
VVPosTex03 SkinWNormVS(VPosNormBone input)
{
	return	ComputeSkin(input, mBones);
}

//skin world norm and pos
VVPosTex03Tex13 SkinWNormWPosVS(VPosNormBone input)
{
	return	ComputeSkinWorld(input, mBones);
}

//skin world norm and pos and texcoord
VVPosTex04Tex14 SkinWNormWPosTex0VS(VPosNormBoneTex0 input)
{
	VPosNormBone skinny;

	skinny.Position	=input.Position;
	skinny.Normal	=input.Normal;
	skinny.Blend0	=input.Blend0;
	skinny.Weight0	=input.Weight0;

	VVPosTex03Tex13	skint	=ComputeSkinWorld(skinny, mBones);

	VVPosTex04Tex14	output;

	output.Position			=skint.Position;
	output.TexCoord0.xyz	=skint.TexCoord0.xyz;
	output.TexCoord1.xyz	=skint.TexCoord1.xyz;
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.w		=input.TexCoord0.y;
	
	return	output;
}

//skin world pos
VVPosTex03 ShadowSkinWPosVS(VPosNormBone input)
{
	float4	vertPos	=float4(input.Position, 1);

	float4x4	skinTransform	=GetSkinXForm(input.Blend0, input.Weight0, mBones);

	vertPos	=mul(vertPos, skinTransform);

	float4	worldVertPos	=mul(vertPos, mWorld);

	VVPosTex03	output;

	output.Position		=mul(worldVertPos, mLightViewProj);
	output.TexCoord0	=worldVertPos.xyz;

	return	output;
}

//skin, world norm, world pos, vert color
VVPosTex04Tex14Tex24 SkinWNormWPosColorVS(VPosNormBoneCol0 input)
{
	VPosNormBone	inSkin;

	inSkin.Position	=input.Position;
	inSkin.Normal	=input.Normal;
	inSkin.Blend0	=input.Blend0;
	inSkin.Weight0	=input.Weight0;

	VVPosTex03Tex13	skin	=ComputeSkinWorld(inSkin, mBones);

	VVPosTex04Tex14Tex24	ret;

	ret.Position		=skin.Position;
	ret.TexCoord0.xyz	=skin.TexCoord0;
	ret.TexCoord1.xyz	=skin.TexCoord1;
	ret.TexCoord2		=input.Color;

	ret.TexCoord0.w	=0;
	ret.TexCoord1.w	=0;

	return	ret;
}

//vert color's red multiplies dangliness
VVPosTex04Tex14 SkinDanglyWnormWPos(VPosNormBoneCol0 input)
{
	VVPosTex03Tex13	skin	=ComputeSkinWorldDangly(input, mBones);

	VVPosTex04Tex14	ret;

	ret.Position		=skin.Position;
	ret.TexCoord0.xyz	=skin.TexCoord0;
	ret.TexCoord1.xyz	=skin.TexCoord1;

	ret.TexCoord0.w	=0;
	ret.TexCoord1.w	=0;

	return	ret;
}

VVPosTex0Col0 SkinTexTriColVS(VPosNormBoneTex0 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VVPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VVPosTex0Col0		output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.Color		=singleOut.Color;
	
	return	output;
}

//skinned dual texcoord
VVPosTex0Tex1Col0 SkinTex0Tex1TriColVS(VPosNormBoneTex0Tex1 input)
{
	VPosNormBone	skVert;
	skVert.Position	=input.Position;
	skVert.Normal	=input.Normal;
	skVert.Blend0	=input.Blend0;
	skVert.Weight0	=input.Weight0;
	
	VVPosCol0	singleOut	=ComputeSkinTrilight(skVert, mBones,
								mLightDirection, mLightColor0, mLightColor1, mLightColor2);
	
	VVPosTex0Tex1Col0	output;
	output.Position		=singleOut.Position;
	output.TexCoord0	=input.TexCoord0;
	output.TexCoord1	=input.TexCoord1;
	output.Color		=singleOut.Color;
	
	return	output;
}

/*
technique10 TriSkinTex0
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinTexTriColVS();
		PixelShader		=compile ps_5_0 Tex0Col0PS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinTexTriColVS();
		PixelShader		=compile ps_4_1 Tex0Col0PS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinTexTriColVS();
		PixelShader		=compile ps_4_0 Tex0Col0PS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinTexTriColVS();
		PixelShader		=compile ps_4_0_level_9_3 Tex0Col0PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinCelTex0Spec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinWNormWPosTex0VS();
		PixelShader		=compile ps_5_0 TriCelTex0SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinWNormWPosTex0VS();
		PixelShader		=compile ps_4_1 TriCelTex0SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosTex0VS();
		PixelShader		=compile ps_4_0 TriCelTex0SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinWNormWPosTex0VS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelTex0SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinWNormWPosVS();
		PixelShader		=compile ps_5_0 TriSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinWNormWPosVS();
		PixelShader		=compile ps_4_1 TriSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinCelSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinWNormWPosVS();
		PixelShader		=compile ps_5_0 TriCelSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinWNormWPosVS();
		PixelShader		=compile ps_4_1 TriCelSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinWNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinDanglySolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_5_0 TriSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_1 TriSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0 TriSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinDanglyCelSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_5_0 TriCelSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_1 TriCelSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinDanglyWnormWPos();
		PixelShader		=compile ps_4_0_level_9_3 TriCelSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinCelColorSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinWNormWPosColorVS();
		PixelShader		=compile ps_5_0 TriCelColorSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinWNormWPosColorVS();
		PixelShader		=compile ps_4_1 TriCelColorSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormWPosColorVS();
		PixelShader		=compile ps_4_0 TriCelColorSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinWNormWPosColorVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelColorSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinDecalTex0
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinTexTriColVS();
		PixelShader		=compile ps_5_0 Tex0Col0DecalPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinTexTriColVS();
		PixelShader		=compile ps_4_1 Tex0Col0DecalPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinTexTriColVS();
		PixelShader		=compile ps_4_0 Tex0Col0DecalPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinTexTriColVS();
		PixelShader		=compile ps_4_0_level_9_3 Tex0Col0DecalPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSkinDecalTex0Tex1
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_5_0 Tex0Tex1Col0DecalPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_4_1 Tex0Tex1Col0DecalPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_4_0 Tex0Tex1Col0DecalPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinTex0Tex1TriColVS();
		PixelShader		=compile ps_4_0_level_9_3 Tex0Tex1Col0DecalPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 FullBrightSkin
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 SkinWNormVS();
		PixelShader		=compile ps_5_0	FullBrightSkinPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 SkinWNormVS();
		PixelShader		=compile ps_4_1	FullBrightSkinPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 SkinWNormVS();
		PixelShader		=compile ps_4_0	FullBrightSkinPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 SkinWNormVS();
		PixelShader		=compile ps_4_0_level_9_3	FullBrightSkinPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 ShadowSkin
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 ShadowSkinWPosVS();
		PixelShader		=compile ps_5_0 ShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 ShadowSkinWPosVS();
		PixelShader		=compile ps_4_1 ShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 ShadowSkinWPosVS();
		PixelShader		=compile ps_4_0 ShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 ShadowSkinWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 ShadowPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 DMN	//depth material normal
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 DMNVS();
		PixelShader		=compile ps_5_0 DMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 DMNVS();
		PixelShader		=compile ps_4_1 DMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 DMNVS();
		PixelShader		=compile ps_4_0 DMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 DMNVS();
		PixelShader		=compile ps_4_0_level_9_3 DMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 DMNDangly
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 DMNDanglyVS();
		PixelShader		=compile ps_5_0 DMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 DMNDanglyVS();
		PixelShader		=compile ps_4_1 DMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 DMNDanglyVS();
		PixelShader		=compile ps_4_0 DMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 DMNDanglyVS();
		PixelShader		=compile ps_4_0_level_9_3 DMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}*/