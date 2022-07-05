//For static geometry
#include "Types.hlsli"
#include "CommonFunctions.hlsli"


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
