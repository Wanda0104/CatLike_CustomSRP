using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace GRP.Runtime
{
    public class Lighting
    {
        private const string bufferName = "Lighting";

        private CommandBuffer _commandBuffer = new CommandBuffer()
        {
            name = bufferName
        };

        private CullingResults m_cullingResults;
        private const int maxDirLightCount = 4, maxOtherLightCount = 64;

        private static int
            dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
            dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
            dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
            dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

        private static Vector4[]
            dirLightColors = new Vector4[maxDirLightCount],
            dirLightDirections = new Vector4[maxDirLightCount],
            dirLightShadowData = new Vector4[maxDirLightCount];

        static int
            otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
            otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
            otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
            otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
            otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");

        static Vector4[]
            otherLightColors = new Vector4[maxOtherLightCount],
            otherLightPositions = new Vector4[maxOtherLightCount],
            otherLightDirections = new Vector4[maxOtherLightCount],
            otherLightSpotAngles = new Vector4[maxOtherLightCount];

        private Shadows _shadows = new Shadows();


        public void SetUp(ScriptableRenderContext context, CullingResults _cullingResults,
            ShadowSettings _shadowSettings)
        {
            _commandBuffer.BeginSample(bufferName);
            m_cullingResults = _cullingResults;
            //Set Properties
            SetUpShadows(context, _cullingResults, _shadowSettings);
            SetUpLights();
            //Draw ShadowMap
            _shadows.Render();
            _commandBuffer.EndSample(bufferName);
            //Execute
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }

        private void SetUpShadows(ScriptableRenderContext context, CullingResults _cullingResults,
            ShadowSettings _shadowSettings)
        {
            _shadows.SetUp(context, _cullingResults, _shadowSettings);
        }

        private void SetUpLights()
        {
            NativeArray<VisibleLight> visibleLights = m_cullingResults.visibleLights;
            int dirLightCount = 0, otherLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                        {
                            SetupDirectionalLight(dirLightCount++, ref visibleLight);
                        }

                        break;

                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            SetupPointLight(otherLightCount++, ref visibleLight);
                        }

                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            SetupSpotLight(otherLightCount++, ref visibleLight);
                        }

                        break;
                }

            }

            _commandBuffer.SetGlobalInt(dirLightCountId, dirLightCount);
            if (dirLightCount > 0)
            {
                _commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
                _commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
                _commandBuffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
            }

            _commandBuffer.SetGlobalInt(otherLightCountId, otherLightCount);
            if (otherLightCount > 0)
            {
                _commandBuffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                _commandBuffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
                _commandBuffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
                _commandBuffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            }
        }

        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            dirLightColors[index] = visibleLight.finalColor;
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }

        private void SetupPointLight(int index, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightSpotAngles[index] = new Vector4(0f, 1f);
        }

        private void SetupSpotLight(int index, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
            Light light = visibleLight.light;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            otherLightSpotAngles[index] = new Vector4(
                angleRangeInv, -outerCos * angleRangeInv
            );
        }

        public void CleanUp()
        {
            _shadows.CleanUp();
        }

    }

}