#ifndef GRP_BRDF_INCLUDED
#define GRP_BRDF_INCLUDED
#define MIN_REFLECTIVITY 0.04
struct BRDF {
    float3 diffuse;//漫反射光
    float3 specular;//高光
    float roughness;//粗糙度
    float perceptualRoughness;
    float fresnel;
};

float OneMinusReflectivity (float metallic) {
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
    brdf.fresnel = saturate(surface.smoothness + 1.0 - MIN_REFLECTIVITY);
    return brdf;
}

float SpecularStrength(Surface surface,BRDF brdf,Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF (Surface surface, BRDF brdf, Light light) {
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF(Surface surface, BRDF brdf,float3 diffuse, float3 specular)
{
    float fresnelStrength =surface.fresnelStrength * Pow4(1 - dot(surface.normal,surface.viewDirection));
    float3 reflection = lerp(brdf.specular,brdf.fresnel,fresnelStrength) * specular;
    reflection /= brdf.roughness * brdf.roughness + 1;
    return (brdf.diffuse * diffuse + reflection) * surface.occlusion;
}
#endif
