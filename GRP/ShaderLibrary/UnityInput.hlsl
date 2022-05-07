#ifndef GRP_UNITY_INPUT_INCLUDED
#define GRP_UNITY_INPUT_INCLUDED
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
real4 unity_WorldTransformParams;
float3 _WorldSpaceCameraPos;

//Dynamic Object use it
float4 unity_ProbesOcclusion;

float4 unity_SpecCube0_HDR;
//LightMap
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

//LightProbe  the components of the polynomial for red, green, and blue light
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

//LightProbeProxyVolume
float4 unity_ProbeVolumeParams;
float4x4 unity_ProbeVolumeWorldToObject;
float4 unity_ProbeVolumeSizeInv;
float4 unity_ProbeVolumeMin;

CBUFFER_END
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
#endif