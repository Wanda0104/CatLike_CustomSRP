#ifndef GRP_LIGHTING_INCLUDED
#define GRP_LIGHTING_INCLUDED
float3 IncomingLight(Surface surface,Light light)
{
    return saturate(dot(surface.normal,light.direction)) * light.color * light.attenuation;
}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    return IncomingLight(surface,light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS,BRDF brdf)
{
    float3 color = 0.0;
    ShadowData shadowData = GetShadowData(surfaceWS);

    for (int i = 0; i < GetDirectionalLightCount(); i++) {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS,brdf, light);
    }
    return color;
}
#endif