shared float4x4 World;
shared float4x4 View;
shared float4x4 Projection;

float4 lightColor;
float3 lightDirection;
float4 ambientColor;
texture TerTexture0;
texture TerTexture1;

struct VertexShaderOutput 
{
     float4 Position : POSITION;
     float4 Color : COLOR0;
     float2 TexCoord0 : TEXCOORD0;
     float2 TexCoord1 : TEXCOORD1;
};

struct PixelShaderInput
{
	float4 Color: COLOR0;
	float2	TexCoord0 : TEXCOORD0;
	float2	TexCoord1 : TEXCOORD1;
};

sampler TextureSampler0 = sampler_state
{
    Texture = (TerTexture0);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler TextureSampler1 = sampler_state
{
    Texture = (TerTexture1);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    
    AddressU = Wrap;
    AddressV = Wrap;
};

VertexShaderOutput DiffuseLighting(
     float3 position : POSITION,
     float3 normal : NORMAL,
     float2 tex0 : TEXCOORD,
     float2 tex1 : TEXCOORD)
{
     VertexShaderOutput output;

     //generate the world-view-proj matrix
     float4x4 wvp = mul(mul(World, View), Projection);
     
     //transform the input position to the output
     output.Position = mul(float4(position, 1.0), wvp);
     
     float3 worldNormal =  mul(normal, World);
     
     float diffuseIntensity = saturate( dot(-lightDirection, worldNormal));
     
     float4 diffuseColor = lightColor * diffuseIntensity;
     
     output.Color = diffuseColor + ambientColor;
     diffuseColor.a = 1.0;
     output.TexCoord0	=tex0;
     output.TexCoord1	=tex1;

     //return the output structure
     return output;
}

float4 SimplePixelShader(PixelShaderInput input) : COLOR
{
	float3	texel0	=tex2D(TextureSampler0, input.TexCoord0);
	float3	texel1	=tex2D(TextureSampler1, input.TexCoord1);
	
	return input.Color * float4(texel0, 1) * float4(texel1, 1);
}

technique VertexLighting
{     
    pass P0
    {
          //set the VertexShader state to the vertex shader function
          VertexShader = compile vs_2_0 DiffuseLighting();
          
          //set the PixelShader state to the pixel shader function          
          PixelShader = compile ps_2_0 SimplePixelShader();
    }
}