#ifndef GRP_SURFACE_INCLUDED
#define GRP_SURFACE_INCLUDED
struct Surface {
    float3 normal;
    float3 color;
    float3 viewDirection;
    float alpha;
    float metallic;
    float smoothness;
};
#endif
