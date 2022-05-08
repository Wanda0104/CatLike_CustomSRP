#ifndef GRP_UNITY_COMMON_INCLUDED
#define GRP_UNITY_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "UnityInput.hlsl"
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 pA, float3 pB) {
    return dot(pA - pB, pA - pB);
}

void ClipLOD(float2 positionCS,float fade)
{
    #ifdef LOD_FADE_CROSSFADE
        float dither = InterleavedGradientNoise(positionCS.xy, 0);
        clip(fade + (fade < 0.0 ? dither : -dither));
    #endif
}

float3 DecodeNormal(float4 sample, float scale)
{
    #if defined(UNITY_NO_DXT5nm)
        return UnpackNormalRGB(sample, scale);
    #else
        return UnpackNormalmapRGorAG(sample, scale);
    #endif
}

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
//Although this is enough to get shadow masks working via probes, it breaks GPU instancing.
//The occlusion data can get instanced automatically, but UnityInstancing only does this when SHADOWS_SHADOWMASK is defined. 
    #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
    float3x3 tangentToWorld =
        CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}

struct InputConfig {
    float2 baseUV;
    float2 detailUV;
    bool useMask;
    bool useDetail;
};

InputConfig GetInputConfig (float2 baseUV, float2 detailUV = 0.0) {
    InputConfig c;
    c.baseUV = baseUV;
    c.detailUV = detailUV;
    c.useMask = false;
    c.useDetail = false;
    return c;
}
#endif