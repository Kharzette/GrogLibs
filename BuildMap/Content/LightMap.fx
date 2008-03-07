float4x4 World;
float4x4 View;
float4x4 Projection;

texture Texture;
texture LightMap;

bool TextureEnabled;


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
    
    AddressU = Wrap;
    AddressV = Wrap;
};


float4 PixelShader(PS_INPUT input) : COLOR0
{
    // Sample the texture and environment map.
    float3 color;
    
    if (TextureEnabled)
        color = tex2D(TextureSampler, input.TexCoord);
    else
        color = float3(1, 1, 1);

    //color = float3(0.4, 0.4, 0.4);
    float3 lm = tex2D(LightMapSampler, input.TexCoord);

    // Apply lighting.
    color *= lm;
    
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
