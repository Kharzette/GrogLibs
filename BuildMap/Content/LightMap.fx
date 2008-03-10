float4x4 World;
float4x4 View;
float4x4 Projection;

texture Texture;
texture LightMap;

bool TextureEnabled;
bool LightMapEnabled;


struct VS_INPUT
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};


struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};


VS_OUTPUT VertexShader(VS_INPUT input)
{
    VS_OUTPUT output;

    // Transform the input values.
    float4 worldPosition = mul(input.Position, World);

    output.Position = mul(mul(worldPosition, View), Projection);

    output.TexCoord = input.TexCoord;
    
    return output;
}


struct PS_INPUT
{
    float2 TexCoord : TEXCOORD0;
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
        color = tex2D(TextureSampler, input.TexCoord);
    else
        color = float3(1.0, 1.0, 1.0);

    //color = float3(0.1, 0.1, 0.1);
    if(LightMapEnabled)
    {
		float2	tc	=input.TexCoord;
		float3 lm = tex2D(LightMapSampler, tc);
		
		// Apply lighting.
		color *= lm;
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
