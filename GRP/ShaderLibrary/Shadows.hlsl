#ifndef GRP_UNITY_SHADOW_INCLUDED
#define GRP_UNITY_SHADOW_INCLUDED
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(sampler_DirectionalShadowAtlas);
CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
CBUFFER_END
struct DirectionalShadowData
{
    float strength;
    float tileIndex;
};

struct ShadowData
{
    int cascadeIndex;
    float strength;
};

//STS >> Shadow Texture Space
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
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
    //用Surface的真实位置去 ShadowMap中采样，导致采样结果与真实的阴影信息不同 会导致 ShadowAcne
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    return lerp(1.0,shadow,directional_shadow_data.strength);
}

float FadedShadowStrength (float distance, float scale, float fade) {
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    int i;
    data.strength = FadedShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    for (i = 0; i < _CascadeCount; i++) {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w) {
            if (i == _CascadeCount - 1) {
                data.strength *= FadedShadowStrength(
                    distanceSqr, 1.0 / sphere.w, _ShadowDistanceFade.z
                );
            }
            break;
        }
    }
    if (i == _CascadeCount)
    {
        data.strength = 0.0;
    }
    
    data.cascadeIndex = i;
    return data;
}
#endif