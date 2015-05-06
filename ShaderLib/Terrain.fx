//terrain texturing variables
#if defined(SM2)
#define	MAX_TERRAIN_TEX		8
#else
#define	MAX_TERRAIN_TEX		16
#endif

float4	mAtlasUVData[MAX_TERRAIN_TEX];
float	mAtlasTexScale[MAX_TERRAIN_TEX];

#include "Types.fxh"
#include "CommonFunctions.fxh"
#include "Trilight.fxh"


//worldpos and normal and texture factors
VVPosTex04Tex14Tex24Tex34 WNormWPosTexFactVS(VPosNormTex04Col0 input)
{
	VVPosTex04Tex14Tex24Tex34	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position			=mul(float4(input.Position, 1), wvp);
	output.TexCoord0.xyz	=mul(input.Normal.xyz, mWorld);
	output.TexCoord1.xyz	=mul(input.Position, mWorld);
	output.TexCoord2		=input.TexCoord0;	//4 texture factors (adds to 1)
	output.TexCoord3		=input.Color * 256;	//4 texture lookups (scale up to byte)

	output.TexCoord0.w	=0;
	output.TexCoord1.w	=0;
	
	//return the output structure
	return	output;
}


//trilight, up to 4 texture lookups
float4	TriTexFact4PS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float4	texColor	=float4(0, 0, 0, 0);

	//texcoord1 has worldspace position
	float2	worldXZ	=input.TexCoord1.xz;

	//texcoord2 has texture factor
#if defined(SM2)
	float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.x];

	//texture atlas offsets and scales
	float2	scales	=mAtlasUVData[input.TexCoord3.x].xy;
	float2	offsets	=mAtlasUVData[input.TexCoord3.x].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.x;

	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.y];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.y].xy;
	offsets	=mAtlasUVData[input.TexCoord3.y].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.y;

	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.z];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.z].xy;
	offsets	=mAtlasUVData[input.TexCoord3.z].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.z;
	
	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.w];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.w].xy;
	offsets	=mAtlasUVData[input.TexCoord3.w].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.w;
#else
	if(input.TexCoord2.x > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.x];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.x].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.x].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.x;
	}
	if(input.TexCoord2.y > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.y];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.y].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.y].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.y;
	}
	if(input.TexCoord2.z > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.z];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.z].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.z].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.z;
	}
	if(input.TexCoord2.w > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.w];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.w].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.w].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.w;
	}
#endif

	float3	pnorm	=input.TexCoord0.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	texColor.xyz	*=triLight;

	return	texColor;
}

//trilight, up to 4 texture lookups
float4	TriCelTexFact4PS(VVPosTex04Tex14Tex24Tex34 input) : SV_Target
{
	float4	texColor	=float4(0, 0, 0, 0);

	//texcoord1 has worldspace position
	float2	worldXZ	=input.TexCoord1.xz;

	//texcoord2 has texture factor
#if defined(SM2)
	float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.x];

	//texture atlas offsets and scales
	float2	scales	=mAtlasUVData[input.TexCoord3.x].xy;
	float2	offsets	=mAtlasUVData[input.TexCoord3.x].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.x;

	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.y];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.y].xy;
	offsets	=mAtlasUVData[input.TexCoord3.y].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.y;

	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.z];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.z].xy;
	offsets	=mAtlasUVData[input.TexCoord3.z].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.z;
	
	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.w];

	//texture atlas offsets and scales
	scales	=mAtlasUVData[input.TexCoord3.w].xy;
	offsets	=mAtlasUVData[input.TexCoord3.w].zw;

	uv	=offsets + scales * frac(uv);

	texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.w;
#else
	if(input.TexCoord2.x > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.x];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.x].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.x].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.x;
	}
	if(input.TexCoord2.y > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.y];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.y].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.y].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.y;
	}
	if(input.TexCoord2.z > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.z];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.z].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.z].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.z;
	}
	if(input.TexCoord2.w > 0)
	{
		float2	uv	=worldXZ * mAtlasTexScale[input.TexCoord3.w];

		//texture atlas offsets and scales
		float2	scales	=mAtlasUVData[input.TexCoord3.w].xy;
		float2	offsets	=mAtlasUVData[input.TexCoord3.w].zw;

		uv	=offsets + scales * frac(uv);

		texColor	+=mTexture0.Sample(PointClamp, uv) * input.TexCoord2.w;
	}
#endif

	float3	pnorm	=input.TexCoord0.xyz;

	pnorm	=normalize(pnorm);

	float3	triLight	=ComputeTrilight(pnorm, mLightDirection,
							mLightColor0, mLightColor1, mLightColor2);

	triLight	=CalcCelColor(triLight);

	texColor.xyz	*=triLight;

	return	texColor;
}


technique10 TriTerrain
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexFactVS();
		PixelShader		=compile ps_5_0 TriTexFact4PS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_1 TriTexFact4PS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_0 TriTexFact4PS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_0_level_9_3 TriTexFact4PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}

technique10 TriCelTerrain
{     
	pass P0
	{
#if defined(SM5)
		VertexShader	=compile vs_5_0 WNormWPosTexFactVS();
		PixelShader		=compile ps_5_0 TriCelTexFact4PS();
#elif defined(SM41)
		VertexShader	=compile vs_4_1 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_1 TriCelTexFact4PS();
#elif defined(SM4)
		VertexShader	=compile vs_4_0 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_0 TriCelTexFact4PS();
#else
		VertexShader	=compile vs_4_0_level_9_3 WNormWPosTexFactVS();
		PixelShader		=compile ps_4_0_level_9_3 TriCelTexFact4PS();
#endif
		SetBlendState(NoBlending, float4(0, 0, 0, 0), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}