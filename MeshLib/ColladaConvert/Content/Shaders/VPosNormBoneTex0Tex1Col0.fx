//shader using TomF's trilights

//constants
#define	MAX_BONES	50

//matrii
shared float4x4	mWorld;
shared float4x4 mView;
shared float4x4 mProjection;
shared float4x4	mBindPose;
float4x4		mBones[MAX_BONES];

//texture layers used on the surface
texture	mTexture0;
texture mTexture1;

//material amb & diffuse
float4	mMatAmbient;
float4	mMatDiffuse;
float	mMatSpecularPower;

//sunlight / moonlight
float4	mLightColor0;		//trilights need 3 colors
float4	mLightColor1;		//trilights need 3 colors
float4	mLightColor2;		//trilights need 3 colors
float3	mLightDirection;
float	mLightFactor1;
float	mLightFactor2;


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
VSOutput TrilightSkin(float3	position	: POSITION,
					float3	normal		: NORMAL,
					float4	bnIdxs		: BLENDINDICES0,
					float4	bnWeights	: BLENDWEIGHT0,
					float2	tex0		: TEXCOORD0,
					float2	tex1		: TEXCOORD1,
					float4	col0		: COLOR0,
					uniform int			lightMethod)
{
	VSOutput	output;
	float4		vertPos	=mul(float4(position, 1.0f), mBindPose);
//	float4		vertPos	=float4(position, 1.0f);
	
	//generate the world-view-proj matrix
	float4x4	wvp	=mul(mul(mWorld, mView), mProjection);
	
	//do the bone influences
	float4x4 skinTransform	=0;
	skinTransform	+=mBones[bnIdxs.x] * bnWeights.x;
	skinTransform	+=mBones[bnIdxs.y] * bnWeights.y;
	skinTransform	+=mBones[bnIdxs.z] * bnWeights.z;
	skinTransform	+=mBones[bnIdxs.w] * bnWeights.w;
	
	//xform the vert to the character's boney pos
	vertPos	=mul(vertPos, skinTransform);
	
	//transform the input position to the output
	output.Position	=mul(vertPos, wvp);
	
	float3 worldNormal	=mul(normal, skinTransform);
	worldNormal	=mul(worldNormal, mWorld);	
	
    float3	totalLight	=float3(0,0,0);
	float	LdotN		=dot(worldNormal, mLightDirection);
	
	if(lightMethod == 0)
	{
		//wraparound
		totalLight	+=(mLightColor0 *
			max(0, LdotN + mLightFactor1) * mLightFactor2);
	}
	else if(lightMethod == 1)
	{
		//hemispherical
		totalLight	+=(mLightColor0 + mLightColor2) * 0.5
			+ (mLightColor0 - mLightColor2) * LdotN * 0.5;
	}
	else
	{
		//trilight
		totalLight	+=(mLightColor0 * max(0, LdotN))
			+ (mLightColor1 * (1 - abs(LdotN)))
			+ (mLightColor2 * max(0, -LdotN));
	}	
		
	output.Color.rgb	=totalLight;
	output.Color.a		=1.0f;
	
	//direct copy of texcoords
	output.TexCoord0	=tex0;
	output.TexCoord1	=tex1;
	
	//return the output structure
	return	output;
}


float4 TwoTexCombine(PSInput input, uniform int pixMode) : COLOR
{
	float4	texel0;
	float4	texel1;
	float4	texLitColor;
	
	if(pixMode == 0)
	{
		texel0	=tex2D(TexSampler0, input.TexCoord0);
		texel1	=tex2D(TexSampler1, input.TexCoord1);
		
		texLitColor	=(texel1.w * texel1) + ((1.0 - texel1.w) * texel0);
		texLitColor	*=input.Color;
	}
	else if(pixMode == 1)
	{
		texel0	=tex2D(TexSampler0, input.TexCoord0);
		
		texLitColor	=texel0 * input.Color;
	}
	else if(pixMode == 2)
	{
		texel1	=tex2D(TexSampler1, input.TexCoord1);
		texLitColor	=texel1 * input.Color;
	}
	
	return	texLitColor;
}

technique WrapAroundCombine
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(0);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(0);
	}
}

technique HemisphericalCombine
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(1);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(0);
	}
}

technique TrilightCombine
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(2);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(0);
	}
}

technique WrapAroundTex0
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(0);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(1);
	}
}

technique HemisphericalTex0
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(1);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(1);
	}
}

technique TrilightTex0
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(2);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(1);
	}
}

technique WrapAroundTex1
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(0);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(2);
	}
}

technique HemisphericalTex1
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(1);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(2);
	}
}

technique TrilightTex1
{     
	pass P0
	{
		//set the VertexShader state to the vertex shader function
		VertexShader = compile vs_2_0 TrilightSkin(2);

		//set the PixelShader state to the pixel shader function          
		PixelShader = compile ps_2_0 TwoTexCombine(2);
	}
}