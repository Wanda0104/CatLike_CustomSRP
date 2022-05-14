using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;
namespace GRP.Runtime
{
    public partial class GEMRenderPipeline
    {
        partial void InitializeForEditor();

#if UNITY_EDITOR

        private static Lightmapping.RequestLightsDelegate lightsDelegate =
            (Light[] lights, NativeArray<LightDataGI> output) =>
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    LightDataGI lightData = new LightDataGI();
                    Light light = lights[i];
                    switch (light.type)
                    {
                        case LightType.Directional:
                            DirectionalLight directionalLight = new DirectionalLight();
                            LightmapperUtils.Extract(light, ref directionalLight);
                            lightData.Init(ref directionalLight);
                            break;
                        case LightType.Point:
                            PointLight pointLight = new PointLight();
                            LightmapperUtils.Extract(light, ref pointLight);
                            lightData.Init(ref pointLight);
                            break;
                        case LightType.Spot:
                            SpotLight spotLight = new SpotLight();
                            LightmapperUtils.Extract(light, ref spotLight);
                            spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                            spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                            lightData.Init(ref spotLight);
                            break;
                        case LightType.Area:
                            RectangleLight rectangleLight = new RectangleLight();
                            LightmapperUtils.Extract(light, ref rectangleLight);
                            rectangleLight.mode = LightMode.Baked;
                            lightData.Init(ref rectangleLight);
                            break;
                        default:
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                    }

                    lightData.falloff = FalloffType.InverseSquared;
                    output[i] = lightData;
                }

            };

        partial void InitializeForEditor()
        {
            Lightmapping.SetDelegate(lightsDelegate);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Lightmapping.ResetDelegate();
        }
#endif
    }

}