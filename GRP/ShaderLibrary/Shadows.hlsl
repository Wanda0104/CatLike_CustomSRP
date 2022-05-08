#ifndef GRP_UNITY_SHADOW_INCLUDED
#define GRP_UNITY_SHADOW_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(sampler_DirectionalShadowAtlas);
CBUFFER_START(_CustomShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeDatas[MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
    float4 _ShadowAtlasSize;
CBUFFER_END
//DirecationalShadowData 由Light组件数据和当前Surface的ShadowData 计算得知
struct DirectionalShadowData
{
    float strength;
    float tileIndex;
    float normalBias;
    int shadowMaskChannel;
};

// 来源于 perobjData的Shadowmask 
struct ShadowMask
{
    float4 shadows;
    bool distance;
    bool always;
};

//阴影数据 来源于ShadowMap
struct ShadowData
{
    int cascadeIndex;
    float strength;
    float cascadeBlend;
    ShadowMask shadowMask;
};


//STS >> Shadow Texture Space
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    /*
     * It needs the texture, the sampler state, and the shadow position as arguments.
     * The result is 1 when the position's Z value is less than what's stored in the shadow map,
     * meaning that it is closer to the light than whatever's casting a shadow.
     * Otherwise, it is behind a shadow caster and the result is zero.
     */
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,sampler_DirectionalShadowAtlas,positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.yyxx;
        DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0;
        for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
            shadow += weights[i] * SampleDirectionalShadowAtlas(
                float3(positions[i].xy, positionSTS.z)
            );
        }
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

float GetCascadedShadow(DirectionalShadowData directional_shadow_data, ShadowData shadow_data, Surface surfaceWS)
{
    float3 normalBias = surfaceWS.interpolatedNormal * (_CascadeDatas[shadow_data.cascadeIndex].y * directional_shadow_data.normalBias);
    float3 positionSTS = mul(
        _DirectionalShadowMatrices[directional_shadow_data.tileIndex],
        float4(surfaceWS.position + normalBias, 1.0)
    ).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    //Blend Cascade
    if (shadow_data.cascadeBlend < 1.0f)
    {
        float3 normalBias = surfaceWS.interpolatedNormal * (_CascadeDatas[shadow_data.cascadeIndex + 1 ].y * directional_shadow_data.normalBias);
        float3 positionSTS = mul(
            _DirectionalShadowMatrices[directional_shadow_data.tileIndex + 1],
            float4(surfaceWS.position + normalBias, 1.0)
        ).xyz;
        shadow = lerp(
            FilterDirectionalShadow(positionSTS), shadow, shadow_data.cascadeBlend
        );
    }
    return  shadow;
}

float GetBakedShadow(ShadowMask shadow_mask,int maskChanel)
{
    float shadow = 1.0;
    if (shadow_mask.always || shadow_mask.distance)
    {
        shadow = shadow_mask.shadows[maskChanel];
    }
    return shadow;
}

float GetBakedShadow(ShadowMask shadow_mask,float strength,int maskChanel)
{
    float shadow = 1.0;
    if (shadow_mask.always || shadow_mask.distance)
    {
        shadow = lerp(1.0,GetBakedShadow(shadow_mask,maskChanel),abs(strength));
    }
    return shadow;
}

float MixBakedAndRuntimeShadow(ShadowData shadow_data,float shadow,float strength,int maskChanel)
{
    float baked = GetBakedShadow(shadow_data.shadowMask,maskChanel);
    if (shadow_data.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, shadow_data.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    
    if (shadow_data.shadowMask.distance) {
        shadow = lerp(baked,shadow,shadow_data.strength);
        return lerp(1.0,shadow,strength);
    }
    return lerp(1.0,shadow,strength * shadow_data.strength);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional_shadow_data,ShadowData shadow_data,Surface surfaceWS)
{

    #if !defined(_RECEIVE_SHADOWS)
        return 1.0;
    #endif
    float shadow;
    if (directional_shadow_data.strength * shadow_data.strength <= 0)
    {
        return GetBakedShadow(shadow_data.shadowMask,directional_shadow_data.strength,directional_shadow_data.shadowMaskChannel);
    }
    else
    {
        shadow = GetCascadedShadow(directional_shadow_data, shadow_data, surfaceWS);
        shadow = lerp(1.0,shadow,directional_shadow_data.strength);
        shadow = MixBakedAndRuntimeShadow(shadow_data,shadow,directional_shadow_data.strength,directional_shadow_data.shadowMaskChannel);
    }
    return shadow;
}

float FadedShadowStrength (float distance, float scale, float fade) {
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.distance = false;
    data.shadowMask.always = false;
    data.shadowMask.shadows = 1.0;
    int i;
    data.strength = FadedShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    data.cascadeBlend = 1.0;
    for (i = 0; i < _CascadeCount; i++) {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w) {
            float fade = FadedShadowStrength(
                distanceSqr, _CascadeDatas[i].x, _ShadowDistanceFade.z
            );
            if (i == _CascadeCount - 1) {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    if (i == _CascadeCount)
    {
        data.strength = 0.0;
    }
    #if defined(_CASCADE_BLEND_DITHER)
    else if (data.cascadeBlend < surfaceWS.dither) {
        i += 1;
    }
    #endif
    #if !defined(_CASCADE_BLEND_SOFT)
        data.cascadeBlend = 1.0;
    #endif
    data.cascadeIndex = i;
    return data;
}

#endif