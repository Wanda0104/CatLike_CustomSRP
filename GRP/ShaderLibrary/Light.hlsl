#ifndef GRP_LIGHT_INCLUDED
#define GRP_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
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
    data.shadowMaskChannel =_DirectionalLightShadowData[lightIndex].w;
    return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadow_data)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadow_data);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadow_data, surfaceWS);
    return light;
}

int GetOtherLightCount()
{
    return _OtherLightCount;
}

Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadow_data)
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(
        saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w))
    );
    light.direction = normalize(ray);
    float4 spotAngles = _OtherLightSpotAngles[index];
    float spotAttenuation = Square(saturate(dot(_OtherLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y));
    light.attenuation = spotAttenuation * rangeAttenuation / distanceSqr;
    
    return light;
}

#endif
