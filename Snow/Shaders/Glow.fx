#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4 GlowColor;
float GlowIntensity;

texture SpriteTexture;
sampler2D textureSampler = sampler_state
{
    Texture = <SpriteTexture>;
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

float4 GlowPass(VertexShaderOutput input) : COLOR0
{
    float4 texColor = tex2D(textureSampler, input.TextureCoordinates);
    float4 color = texColor * input.Color;
    
    if (texColor.a > 0.1)
    {
        color.rgb = GlowColor.rgb * GlowIntensity;
        color.a = texColor.a;
    }
    
    return color;
}

technique Glow
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL GlowPass();
    }
};
