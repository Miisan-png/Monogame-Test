#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float BloomThreshold;
float BloomIntensity;
float2 TextureSize;

texture ScreenTexture;
sampler2D textureSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 ExtractBrightPass(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(textureSampler, input.TextureCoordinates);
    float brightness = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    
    if (brightness > BloomThreshold)
    {
        return color * BloomIntensity;
    }
    
    return float4(0, 0, 0, 0);
}

float4 GaussianBlurH(VertexShaderOutput input) : COLOR0
{
    float2 pixelSize = 1.0 / TextureSize;
    float4 color = float4(0, 0, 0, 0);
    
    float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    
    color += tex2D(textureSampler, input.TextureCoordinates) * weights[0];
    
    for (int i = 1; i < 5; i++)
    {
        color += tex2D(textureSampler, input.TextureCoordinates + float2(pixelSize.x * i, 0)) * weights[i];
        color += tex2D(textureSampler, input.TextureCoordinates - float2(pixelSize.x * i, 0)) * weights[i];
    }
    
    return color;
}

float4 GaussianBlurV(VertexShaderOutput input) : COLOR0
{
    float2 pixelSize = 1.0 / TextureSize;
    float4 color = float4(0, 0, 0, 0);
    
    float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    
    color += tex2D(textureSampler, input.TextureCoordinates) * weights[0];
    
    for (int i = 1; i < 5; i++)
    {
        color += tex2D(textureSampler, input.TextureCoordinates + float2(0, pixelSize.y * i)) * weights[i];
        color += tex2D(textureSampler, input.TextureCoordinates - float2(0, pixelSize.y * i)) * weights[i];
    }
    
    return color;
}

technique ExtractBright
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL ExtractBrightPass();
    }
};

technique BlurHorizontal
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL GaussianBlurH();
    }
};

technique BlurVertical
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL GaussianBlurV();
    }
};
