#ifndef GRP_LIGHT_INCLUDED
#define GRP_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};
int GetDirectionalLightCount () {
    return _DirectionalLightCount;
}
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadow_data)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x ;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadow_data.cascadeIndex;
    data.normalBias =_DirectionalLightShadowData[lightIndex].z;
    return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadow_data)
{
    Light light;
    light.color = _DirectionalLightColors[index].rbg;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadow_data);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadow_data, surfaceWS);
    return light;
}

#endif
