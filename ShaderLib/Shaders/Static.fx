//For static geometry

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "Trilight.fxh"


//just world position
VPosTex03 WPosVS(VPos input)
{
	float4	vertPos	=input.Position;

	float4	worldVertPos	=mul(vertPos, mWorld);

	VPosTex03	output;

	output.Position		=mul(worldVertPos, mLightViewProj);
	output.TexCoord0	=worldVertPos.xyz;

	return	output;
}

//worldpos and worldnormal
VPosTex03Tex13 WNormWPosVS(VPosNormTex0 input)
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

//texcoord + trilight color
VPosTex0Col0 TexTriVS(VPosNormTex0 input)
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

//tangent stuff
VPosNormTanBiTanTex0 WNormWTanBTanTexVS(VPosNormTanTex0 input)
{
	VPosNormTanBiTanTex0	output;
	
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
VPosTex04Tex14Tex24Tex34 WNormWTanBTanWPosVS(VPosNormTanTex0 input)
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
VPosTex04Tex14Tex24Tex34 WNormWTanBTanWPosInstancedVS(VPosNormTanTex0 input, float4x4 instWorld : BLENDWEIGHT)
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

//worldpos and normal
VPosTex04Tex14 WNormWPosTexVS(VPosNormTex0 input)
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


technique TriTex0
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 TexTriVS();
		PixelShader		=compile ps_4_0 Tex0Col0PS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 TexTriVS();
		PixelShader		=compile ps_3_0 Tex0Col0PS();
#else
		VertexShader	=compile vs_2_0 TexTriVS();
		PixelShader		=compile ps_2_0 Tex0Col0PS();
#endif
	}
}

technique TriTex0NormalMapSolid
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_3_0 NormalMapTriTex0SolidPS();
#else
		VertexShader	=compile vs_2_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidPS();
#endif
	}
}

technique TriTex0NormalMapSolidSpec
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanTexVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidSpecPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWTanBTanTexVS();
		PixelShader		=compile ps_3_0 NormalMapTriTex0SolidSpecPS();
#else
		VertexShader	=compile vs_2_0 WNormWTanBTanTexVS();
		PixelShader		=compile ps_2_0 NormalMapTriTex0SolidSpecPS();
#endif
	}
}

technique TriTex0SpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexVS();
		PixelShader		=compile ps_4_0 TriTex0SpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWPosTexVS();
		PixelShader		=compile ps_3_0 TriTex0SpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWPosTexVS();
		PixelShader		=compile ps_2_0 TriTex0SpecPhysPS();
#endif
	}
}

technique TriSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWPosVS();
		PixelShader		=compile ps_3_0 TriSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWPosVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();
#endif
	}
}

technique TriCelSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosVS();
		PixelShader		=compile ps_4_0 TriCelSolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWPosVS();
		PixelShader		=compile ps_3_0 TriCelSolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWPosVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();	//not enough instructions, fallback
#endif
	}
}

technique TriTex0NormalMapSolidSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_3_0 NormalMapTriTex0SolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();	//not enough instructions, fallback
#endif
	}
}

technique TriTex0NormalMapSpecPhys
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_3_0 NormalMapTriTex0SpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWTanBTanWPosVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();	//not enough instructions, fallback
#endif
	}
}

technique TriTex0NormalMapSolidSpecPhysInstanced
{     
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_4_0 NormalMapTriTex0SolidSpecPhysPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_3_0 NormalMapTriTex0SolidSpecPhysPS();
#else
		VertexShader	=compile vs_2_0 WNormWTanBTanWPosInstancedVS();
		PixelShader		=compile ps_2_0 TriSolidSpecPhysPS();	//not enough instructions, fall back
#endif
	}
}

technique Shadow
{
	pass P0
	{
#if defined(SM4)
		VertexShader	=compile vs_4_0 WPosVS();
		PixelShader		=compile ps_4_0 ShadowPS();
#elif defined(SM3)
		VertexShader	=compile vs_3_0 WPosVS();
		PixelShader		=compile ps_3_0 ShadowPS();
#else
		VertexShader	=compile vs_2_0 WPosVS();
		PixelShader		=compile ps_2_0 ShadowPS();
#endif
	}
}