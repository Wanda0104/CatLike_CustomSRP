#ifndef GRP_UNITY_SHADOW_INCLUDED
#define GRP_UNITY_SHADOW_INCLUDED
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
//ShadowMap 中存贮的是灯光可到达的
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(sampler_DirectionalShadowAtlas);
CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END
struct DirectionalShadowData
{
    float strength;
    float tileIndex;
};

//STS >> Shadow Texture Space
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    //@TODO : 采样结果为啥是这个！！


    //采样结果 在完全在阴影之中的->0   不在阴影中的-> 1
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,sampler_DirectionalShadowAtlas,positionSTS);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional_shadow_data,Surface surfaceWS)
{
    if (directional_shadow_data.strength <= 0)
    {
        return 1.0;
    }
    
    float3 positionSTS = mul(
        _DirectionalShadowMatrices[directional_shadow_data.tileIndex],
        float4(surfaceWS.position, 1.0)
    ).xyz;

    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0,shadow,directional_shadow_data.strength);
}
#endif