//For static geometry

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "Trilight.fxh"


//just world position
VVPosTex03 WPosVS(VPos input)
{
	float4	vertPos			=float4(input.Position, 1);
	float4	worldVertPos	=mul(vertPos, mWorld);

	VVPosTex03	output;

	output.Position		=mul(worldVertPos, mLightViewProj);
	output.TexCoord0	=worldVertPos.xyz;

	return	output;
}

//worldpos and worldnormal
VVPosTex03Tex13 WNormWPosVS(VPosNorm input)
{
	VVPosTex03Tex13	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position		=mul(float4(input.Position, 1), wvp);
	output.TexCoord0	=mul(input.Normal.xyz, mWorld);
	output.TexCoord1	=mul(input.Position, mWorld);
	
	//return the output structure
	return	output;
}

//worldpos and worldnormal and vert color
VVPosTex03Tex13Tex23 WNormWPosVColorVS(VPosNormCol0 input)
{
	VVPosTex03Tex13Tex23	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position		=mul(float4(input.Position, 1), wvp);
	output.TexCoord0	=mul(input.Normal.xyz, mWorld);
	output.TexCoord1	=mul(input.Position, mWorld);
	output.TexCoord2	=input.Color;
	
	//return the output structure
	return	output;
}

//texcoord + trilight color interpolated
VVPosTex0Col0 TexTriVS(VPosNormTex0 input)
{
	VVPosTex0Col0	output;	
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(float4(input.Position, 1), wvp);
	
	float3 worldNormal	=mul(input.Normal.xyz, mWorld);

	output.Color.xyz	=ComputeTrilight(worldNormal, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);
	output.Color.w		=1.0f;
	
	//direct copy of texcoords
	output.TexCoord0	=input.TexCoord0;
	
	//return the output structure
	return	output;
}

//tangent stuff
VVPosNormTanBiTanTex0 WNormWTanBTanTexVS(VPosNormTanTex0 input)
{
	VVPosNormTanBiTanTex0	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	output.Position		=mul(float4(input.Position, 1), wvp);
	output.Normal		=mul(input.Normal.xyz, mWorld);
	output.Tangent		=mul(input.Tangent.xyz, mWorld);
	output.TexCoord0	=input.TexCoord0;

	float3	biTan	=cross(input.Normal.xyz, input.Tangent) * input.Tangent.w;

	output.BiTangent	=normalize(biTan);

	//return the output structure
	return	output;
}

//packed tangents with worldspace pos
VVPosTex04Tex14Tex24Tex34 WNormWTanBTanWPosVS(VPosNormTanTex0 input)
{
	VVPosTex04Tex14Tex24Tex34	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	//pos4
	//tex2
	//wtan3
	//bitan3
	
	output.Position			=mul(float4(input.Position, 1), wvp);
	output.TexCoord0.xyz	=mul(input.Normal.xyz, mWorld);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.xyz	=mul(input.Tangent.xyz, mWorld);
	output.TexCoord1.w		=input.TexCoord0.y;

	float3	biTan	=cross(input.Normal.xyz, input.Tangent) * input.Tangent.w;

	output.TexCoord2		=float4(normalize(biTan), 0);
	output.TexCoord3		=mul(input.Position, mWorld);

	//return the output structure
	return	output;
}

//packed tangents with worldspace pos and instancing
VVPosTex04Tex14Tex24Tex34 WNormWTanBTanWPosInstancedVS(VPosNormTanTex0 input, float4x4 instWorld : BLENDWEIGHT)
{
	VVPosTex04Tex14Tex24Tex34	output;

	float4x4	world	=transpose(instWorld);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(world, mView), mProjection);
	
	output.Position			=mul(float4(input.Position, 1), wvp);
	output.TexCoord0.xyz	=mul(input.Normal.xyz, world);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.xyz	=mul(input.Tangent.xyz, world);
	output.TexCoord1.w		=input.TexCoord0.y;

	float3	biTan	=cross(input.Normal, input.Tangent) * input.Tangent.w;

	output.TexCoord2		=float4(normalize(biTan), 0);
	output.TexCoord3		=mul(input.Position, world);

	//return the output structure
	return	output;
}

//worldpos and normal
VVPosTex04Tex14 WNormWPosTexVS(VPosNormTex0 input)
{
	VVPosTex04Tex14	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position			=mul(float4(input.Position, 1), wvp);
	output.TexCoord0.xyz	=mul(input.Normal.xyz, mWorld);
	output.TexCoord1.xyz	=mul(input.Position, mWorld);
	output.TexCoord0.w		=input.TexCoord0.x;
	output.TexCoord1.w		=input.TexCoord0.y;
	
	//return the output structure
	return	output;
}


technique10 TriTex0
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 TexTriVS();
		PixelShader		=compile ps_5_0 Tex0Col0PS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 TexTriVS();
		PixelShader		=compile ps_4_1 Tex0Col0PS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 TexTriVS();
		PixelShader		=compile ps_4_0 Tex0Col0PS();
#else
		VertexShader	=compile vs_4_0_level_9_3 TexTriVS();
		PixelShader		=compile ps_4_0_level_9_3 Tex0Col0PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriVColorSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVColorVS();
		PixelShader		=compile ps_5_0 TriSolidVColorSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVColorVS();
		PixelShader		=compile ps_4_1 TriSolidVColorSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVColorVS();
		PixelShader		=compile ps_4_0 TriSolidVColorSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVColorVS();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidVColorSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0NormalMapSolid
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_5_0 NormalMapTriTex0SolidPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_1 NormalMapTriTex0SolidPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 NormalMapTriTex0SolidPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0Spec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexVS();
		PixelShader		=compile ps_5_0 TriTex0SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexVS();
		PixelShader		=compile ps_4_1 TriTex0SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexVS();
		PixelShader		=compile ps_4_0 TriTex0SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexVS();
		PixelShader		=compile ps_4_0_level_9_3 TriTex0SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0EM1Spec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexVS();
		PixelShader		=compile ps_5_0 TriTex0EM1SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexVS();
		PixelShader		=compile ps_4_1 TriTex0EM1SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexVS();
		PixelShader		=compile ps_4_0 TriTex0EM1SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexVS();
		PixelShader		=compile ps_4_0_level_9_3 TriTex0EM1SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriCelTex0EM1Spec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexVS();
		PixelShader		=compile ps_5_0 TriCelTex0EM1SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexVS();
		PixelShader		=compile ps_4_1 TriCelTex0EM1SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexVS();
		PixelShader		=compile ps_4_0 TriCelTex0EM1SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelTex0EM1SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriCelTex0Spec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexVS();
		PixelShader		=compile ps_5_0 TriCelTex0SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexVS();
		PixelShader		=compile ps_4_1 TriCelTex0SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexVS();
		PixelShader		=compile ps_4_0 TriCelTex0SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelTex0SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSolid
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVS();
		PixelShader		=compile ps_5_0 TriSolidPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVS();
		PixelShader		=compile ps_4_1 TriSolidPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVS();
		PixelShader		=compile ps_5_0 TriSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVS();
		PixelShader		=compile ps_4_1 TriSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriSolidSpecAlpha
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVS();
		PixelShader		=compile ps_5_0 TriSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVS();
		PixelShader		=compile ps_4_1 TriSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriSolidSpecPS();
#endif
		SetBlendState(AlphaBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(DisableDepthWrite, 0);
	}
}

technique10 TriCelSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVS();
		PixelShader		=compile ps_5_0 TriCelSolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVS();
		PixelShader		=compile ps_4_1 TriCelSolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelSolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0NormalMapSolidSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_5_0 NormalMapTriTex0SolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_1 NormalMapTriTex0SolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 NormalMapTriTex0SolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0NormalMapSpec
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_5_0 NormalMapTriTex0SpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_1 NormalMapTriTex0SpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 NormalMapTriTex0SpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriTex0NormalMapSolidSpecInstanced
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_5_0 NormalMapTriTex0SolidSpecPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_4_1 NormalMapTriTex0SolidSpecPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidSpecPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_4_0_level_9_3 NormalMapTriTex0SolidSpecPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 Shadow
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WPosVS();
		PixelShader		=compile ps_5_0 ShadowPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WPosVS();
		PixelShader		=compile ps_4_1 ShadowPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WPosVS();
		PixelShader		=compile ps_4_0 ShadowPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WPosVS();
		PixelShader		=compile ps_4_0_level_9_3 ShadowPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 DMN
{
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosVS();
		PixelShader		=compile ps_5_0 DMNPS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosVS();
		PixelShader		=compile ps_4_1 DMNPS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 DMNPS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosVS();
		PixelShader		=compile ps_4_0_level_9_3 DMNPS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}