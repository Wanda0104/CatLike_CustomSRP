#ifndef GRP_UNITY_COMMON_INCLUDED
#define GRP_UNITY_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
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
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
//Although this is enough to get shadow masks working via probes, it breaks GPU instancing.
//The occlusion data can get instanced automatically, but UnityInstancing only does this when SHADOWS_SHADOWMASK is defined. 
    #define SHADOWS_SHADOWMASK
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#endif