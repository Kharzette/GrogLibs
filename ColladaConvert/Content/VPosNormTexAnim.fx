//shader using TomF's trilights

//constants
#define	MAX_BONES	30

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mLocal;
float4x4		mBones[MAX_BONES];

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
	float4	Color		: COLOR0;	
};

//this plugs into the pixel shader
struct PSInput
{
	float4	Color		: COLOR0;
	float2	TexCoord0	: TEXCOORD0;
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
VSOutput DiffuseGouradSkin(float3	position	: POSITION,
							float3	normal		: NORMAL,
							float4	bnIdxs		: BLENDINDICES0,
							float4	bnWeights	: BLENDWEIGHT0,
							float2	tex0		: TEXCOORD0)
{
	VSOutput	output;
	
	float4x4	localWorld	=mul(mLocal, mWorld);

	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(localWorld, mView), mProjection);
	
	float4	worldPos	=mul(float4(position, 1.0f), localWorld);

	//do the bone influences
//	float4x4 skinTransform	=0;
//	skinTransform	+=mBones[bnIdxs.x] * bnWeights.x;
//	skinTransform	+=mBones[bnIdxs.y] * bnWeights.y;
//	skinTransform	+=mBones[bnIdxs.z] * bnWeights.z;
//	skinTransform	+=mBones[bnIdxs.w] * bnWeights.w;
	
	//xform the vert to the character's boney pos
//	output.Position	=mul(float4(position, 1.0f), skinTransform);
	output.Position =float4(position, 1.0);
	
	//transform the input position to the output
	output.Position	=mul(output.Position, wvp);
	
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
	
	//return the output structure
	return	output;
}

float4 Gourad2TexModulate(PSInput input) : COLOR
{
	float4	texel0	=tex2D(TexSampler0, input.TexCoord0);
	
	float4	inColor	=input.Color;
	
	float4	texLitColor	=inColor * texel0;
	
	return	texLitColor;
}

technique VertexLighting
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 DiffuseGouradSkin();

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 Gourad2TexModulate();
	}
}