//flyer shader

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;

//sunlight / moonlight
shared float4	mLightColor;
shared float3	mLightDirection;
shared float4	mAmbientColor;

//texture layers used on the flyer
texture	mTexture;

//this comes outta the vertex shader
struct VSOutput 
{
     float4	Position	: POSITION;
     float4	Color		: COLOR0;
     float2	TexCoord0	: TEXCOORD0;
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
sampler TexSampler = sampler_state
{
	Texture	=(mTexture);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

//gonad shading vertex shader
VSOutput DiffuseGourad(float3 position	: POSITION,
					   float3 normal	: NORMAL,
					   float2 tex0		: TEXCOORD)
{
	VSOutput	output;

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);

	//transform the input position to the output
	output.Position	=mul(float4(position, 1.0), wvp);

	float3 worldNormal	=mul(normal, mWorld);

	//normal dotproduct lightdirection
	float	diffuseIntensity	=saturate(dot(-mLightDirection, worldNormal));

	float4	diffuseColor	=mLightColor * diffuseIntensity;

	output.Color	=diffuseColor + mAmbientColor;
	diffuseColor.a	=1.0;
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;

	//return the output structure
	return	output;
}

float4 GouradTexModulate(PSInput input) : COLOR
{
	float3	texel	=tex2D(TexSampler, input.TexCoord0);
	
	return input.Color * float4(texel, 1);
}

technique VertexLighting
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 DiffuseGourad();

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 GouradTexModulate();
	}
}