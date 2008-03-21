float4x4 World;
float4x4 View;
float4x4 Projection;

texture Texture;
texture LightMap;

bool TextureEnabled;
bool LightMapEnabled;
bool FullBright;


struct VS_INPUT
{
    float4 Position : POSITION0;
    float2 TexCoord0 : TEXCOORD0;
    float2 TexCoord1 : TEXCOORD1;
};


struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 TexCoord0 : TEXCOORD0;
    float2 TexCoord1 : TEXCOORD1;
};


VS_OUTPUT VertexShader(VS_INPUT input)
{
    VS_OUTPUT output;

    // Transform the input values.
    float4 worldPosition = mul(input.Position, World);

    output.Position = mul(mul(worldPosition, View), Projection);

    output.TexCoord0 = input.TexCoord0;
    output.TexCoord1 = input.TexCoord1;
    
    return output;
}


struct PS_INPUT
{
    float2 TexCoord0 : TEXCOORD0;
    float2 TexCoord1 : TEXCOORD1;
};


sampler TextureSampler = sampler_state
{
    Texture = (Texture);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    
    AddressU = Wrap;
    AddressV = Wrap;
};


sampler LightMapSampler = sampler_state
{
    Texture = (LightMap);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    
    AddressU = Clamp;
    AddressV = Clamp;
};


float4 PixelShader(PS_INPUT input) : COLOR0
{
    // Sample the texture and environment map.
    float3 color;
    
    if (TextureEnabled)
    {
        color = tex2D(TextureSampler, input.TexCoord0);
    }
    else
    {
        color = float3(1.0, 1.0, 1.0);
    }

    if(LightMapEnabled)
    {
		float3 lm = tex2D(LightMapSampler, input.TexCoord1);
		
		// Apply lighting.
		color *= lm;
	}
	else
	{
		if(!FullBright)
		{
			color *= float3(0.01, 0.01, 0.01);
		}
	}
    
    return float4(color, 1);
}


technique LightMap
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShader();
        PixelShader = compile ps_2_0 PixelShader();
    }
}
