//2TexGourad shader
//combines two textures and diffuse lighting
//from a directional (the sun or moon)

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mLocal;

//sunlight / moonlight
shared float4	mLightColor;
shared float3	mLightDirection;
shared float4	mAmbientColor;

//texture layers used on the surface
texture	mTerTexture0;
texture	mTerTexture1;

//this comes outta the vertex shader
struct VSOutput 
{
     float4	Position	: POSITION;
     float4	Color		: COLOR0;
     float2	TexCoord0	: TEXCOORD0;
     float2	TexCoord1	: TEXCOORD1;
};

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float2	TexCoord0	: TEXCOORD0;
	float2	TexCoord1	: TEXCOORD1;
};

//simple texture sampler
//might be able to go aniso
//depending on how this runs on surface
sampler TexSampler0 = sampler_state
{
	Texture	=(mTerTexture0);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};

sampler TexSampler1 = sampler_state
{
	Texture	=(mTerTexture1);

	MinFilter	=Linear;
	MagFilter	=Linear;
	MipFilter	=Linear;

	AddressU	=Wrap;
	AddressV	=Wrap;
};


//gonad shading vertex shader
VSOutput DiffuseGourad(float3 position	: POSITION,
					   float3 normal	: NORMAL,
					   float2 tex0		: TEXCOORD,
					   float2 tex1		: TEXCOORD)
{
	VSOutput	output;

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mul(mWorld, mView), mLocal), mProjection);

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
	output.TexCoord1	=tex1;

	//return the output structure
	return	output;
}

float4 Gourad2TexModulate(PSInput input) : COLOR
{
	float3	texel0	=tex2D(TexSampler0, input.TexCoord0);
	float3	texel1	=tex2D(TexSampler1, input.TexCoord1);
	
	return input.Color * float4(texel0, 1) * float4(texel1, 1);
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