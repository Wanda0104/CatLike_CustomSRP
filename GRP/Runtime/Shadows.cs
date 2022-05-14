using UnityEngine;
using UnityEngine.Rendering;
namespace GRP.Runtime
{
    public class Shadows
    {
        struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        private const string _bufferName = "Shadows";
        private const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
        private int ShadowedDirectionalLightCount;

        private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
            dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
            cascadeCountId = Shader.PropertyToID("_CascadeCount"),
            cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
            shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),
            cascadeDataId = Shader.PropertyToID("_CascadeDatas"),
            shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");

        static Matrix4x4[]
            dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

        static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        static string[] cascadeBlendKeywords =
        {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER"
        };

        private static string[] shadowMaskKeywords =
        {
            "_SHADOW_MASK_ALWAYS",
            "_SHADOW_MASK_DISTANCE",
        };

        private bool useShadowMask = false;

        //xyz pos, w radius
        static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades],
            cascadeData = new Vector4[maxCascades];

        private CommandBuffer _commandBuffer = new CommandBuffer()
        {
            name = _bufferName
        };

        private ScriptableRenderContext _scriptableRenderContext;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;

        ShadowedDirectionalLight[] ShadowedDirectionalLights =
            new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

        public void SetUp(ScriptableRenderContext scriptableRenderContext, CullingResults cullingResults,
            ShadowSettings shadowSettings)
        {
            _scriptableRenderContext = scriptableRenderContext;
            _cullingResults = cullingResults;
            _shadowSettings = shadowSettings;
            ShadowedDirectionalLightCount = 0;
            useShadowMask = false;
        }

        private void ExecuteBuffer()
        {
            _scriptableRenderContext.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }

        public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
                light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                LightBakingOutput lightBakingOutput = light.bakingOutput;
                float maskChannel = -1;
                if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                    lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    useShadowMask = true;
                    maskChannel = lightBakingOutput.occlusionMaskChannel;
                }

                if (!_cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
                }

                ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                    new ShadowedDirectionalLight
                    {
                        visibleLightIndex = visibleLightIndex,
                        slopeScaleBias = light.shadowBias,
                        nearPlaneOffset = light.shadowNearPlane,
                    };

                return new Vector4(
                    light.shadowStrength, _shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++,
                    light.shadowNormalBias, maskChannel
                );
            }
            else
            {
                return Vector3.zero;
            }
        }

        public void Render()
        {
            if (ShadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                _commandBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear,
                    RenderTextureFormat.Shadowmap);
            }

            _commandBuffer.BeginSample(_bufferName);
            SetKeywords(shadowMaskKeywords,
                useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
            _commandBuffer.EndSample(_bufferName);
            ExecuteBuffer();
        }

        void RenderDirectionalShadows()
        {
            int atlasSize = (int) _shadowSettings.directional.atlasSize;
            _commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
                RenderTextureFormat.Shadowmap);
            _commandBuffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            _commandBuffer.ClearRenderTarget(true, false, Color.clear);
            _commandBuffer.BeginSample(_bufferName);
            ExecuteBuffer();
            int tiles = ShadowedDirectionalLightCount * _shadowSettings.directional.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < ShadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, split, tileSize, atlasSize);
            }

            _commandBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
            float f = 1f - _shadowSettings.directional.cascadeFade;
            _commandBuffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / _shadowSettings.maxDistance,
                1f / _shadowSettings.distanceFade, 1f / (1f - f * f)
            ));
            _commandBuffer.EndSample(_bufferName);
            ExecuteBuffer();
        }

        void RenderDirectionalShadows(int index, int split, int tileSize, int atlasSize)
        {
            ShadowedDirectionalLight _shadowedDirectionalLight = ShadowedDirectionalLights[index];
            var shadowSettings =
                new ShadowDrawingSettings(_cullingResults, _shadowedDirectionalLight.visibleLightIndex);

            int cascadeCount = _shadowSettings.directional.cascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = _shadowSettings.directional.CascadeRatios;
            float cullingFactor =
                Mathf.Max(0f, 0.8f - _shadowSettings.directional.cascadeFade);
            for (int i = 0; i < cascadeCount; i++)
            {

                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    _shadowedDirectionalLight.visibleLightIndex, i, cascadeCount, ratios, tileSize,
                    _shadowedDirectionalLight.nearPlaneOffset,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData
                );
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;
                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                SetTileViewport(index, split, tileSize);
                int tileIndex = tileOffset + i;
                dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                    projectionMatrix * viewMatrix,
                    SetTileViewport(tileIndex, split, tileSize), split
                );
                _commandBuffer.SetGlobalInt(cascadeCountId, _shadowSettings.directional.cascadeCount);
                _commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
                _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                _commandBuffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
                _commandBuffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
                _commandBuffer.SetGlobalDepthBias(0f, _shadowedDirectionalLight.slopeScaleBias);
                SetKeywords(directionalFilterKeywords, (int) _shadowSettings.directional.filterMode - 1);
                SetKeywords(cascadeBlendKeywords, (int) _shadowSettings.directional.CascadeBlendMode - 1);
                ExecuteBuffer();
                _scriptableRenderContext.DrawShadows(ref shadowSettings);
                _commandBuffer.SetGlobalDepthBias(0f, 0f);
            }
        }

        void SetKeywords(string[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == enabledIndex)
                {
                    _commandBuffer.EnableShaderKeyword(keywords[i]);
                }
                else
                {
                    _commandBuffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }

        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {

            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float) _shadowSettings.directional.filterMode + 1f);
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            cascadeData[index] = new Vector4(
                1f / cullingSphere.w,
                filterSize * 1.4142136f
            );
        }

        private Vector2 SetTileViewport(int index, int split, float tileSize)
        {
            Vector2 offset = new Vector2(index % split, index / split);
            _commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }

        public void CleanUp()
        {
            _commandBuffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }

}