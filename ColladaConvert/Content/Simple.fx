//ui fx, basic textured
//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;

//texture layers used on the surface
texture	mTexture;
texture	mNMap;

shared float3 mLightDir;

//this comes outta the vertex shader
struct VSOutput 
{
     float4	Position	: POSITION;
     float2	TexCoord0	: TEXCOORD0;
     float4	Color		: COLOR0;
};

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float2	TexCoord0	: TEXCOORD0;
};

//simple texture sampler
//might be able to go aniso
//depending on how this runs on surface
sampler TexSampler0 = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler TexSamplerNorm = sampler_state
{
	Texture	=(mNMap);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


//vertex shader
VSOutput DiffuseGourad(float3 position	: POSITION,
					   float3 normal	: NORMAL,
					   float2 tex0		: TEXCOORD0)
{
	VSOutput	output;
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0f), wvp);
	
	output.Color	=float4(1.0, 1.0, 1.0, 1.0);
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	
	//return the output structure
	return	output;
}

float4 Gourad2TexModulate(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float4	texel1	=tex2D(TexSamplerNorm, input.TexCoord0);
	
	float lightIntensity	=saturate(dot(mLightDir, texel1));
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0;// * lightIntensity;
	
	return	texLitColor;
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