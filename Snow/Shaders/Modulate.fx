#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4 ModulateColor;

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

float4 ModulatePass(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(textureSampler, input.TextureCoordinates);
    return color * ModulateColor;
}

technique Modulate
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL ModulatePass();
    }
};
