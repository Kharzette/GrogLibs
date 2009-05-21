//shader using TomF's trilights
//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mLocal;

//texture layers used on the surface
texture	mTexture0;
texture mTexture1;

//sunlight / moonlight
shared float4	mLightColor[3];		//trilights need 3 colors
shared float3	mLightDirection;
shared float4	mAmbientColor;
shared float	mLightFactor1;
shared float	mLightFactor2;

//this comes outta the vertex shader
struct VSOutput 
{
     float4	Position	: POSITION;
     float2	TexCoord0	: TEXCOORD0;
     float2	TexCoord1	: TEXCOORD1;
     float4	Color		: COLOR0;
};

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

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


//vertex shader
VSOutput DiffuseGourad(float3 position	: POSITION,
					   float3 normal	: NORMAL,
					   float2 tex0		: TEXCOORD0,
					   float2 tex1		: TEXCOORD1,
					   float4 color		: COLOR0)
{
	VSOutput	output;
	
	float4x4	localWorld	=mul(mLocal, mWorld);

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(localWorld, mView), mProjection);
	
	float4	worldPos	=mul(float4(position, 1.0f), localWorld);
	
	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0f), wvp);
	
	float3 worldNormal	=mul(normal, mWorld);
	
    float3	totalLight	=float3(0,0,0);
	float	LdotN		=dot(worldNormal, mLightDirection);
	
	totalLight	+=(mLightColor[0] * max(0, LdotN))
		+ (mLightColor[1] * (1 - abs(LdotN)))
		+ (mLightColor[2] * max(0, -LdotN));
		
	output.Color.rgb	=totalLight;
	output.Color.a		=1.0f;
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	output.TexCoord1	=tex1;
	
	//return the output structure
	return	output;
}

float4 Gourad2TexModulate(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSampler1, input.TexCoord1);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0;// * texel1;
	
	return	texLitColor;
//	return	input.Color;
//	return	texel0;
}

technique VertexLighting
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 DiffuseGourad();

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 Gourad2TexModulate();
	}
}