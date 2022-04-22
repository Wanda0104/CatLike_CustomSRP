using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    struct ShadowedDirectionalLight {
        public int visibleLightIndex;
    }
    
    private const string _bufferName = "Shadows";
    private const int maxShadowedDirectionalLightCount = 4 , maxCascades = 4;
    private int ShadowedDirectionalLightCount;
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
                       dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
                       cascadeCountId = Shader.PropertyToID("_CascadeCount"),
                       cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
                       shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    
    static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    
    //xyz pos, w radius
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    private CommandBuffer _commandBuffer = new CommandBuffer()
    {
        name = _bufferName
    };

    private ScriptableRenderContext _scriptableRenderContext;
    private CullingResults _cullingResults;
    private ShadowSettings _shadowSettings;
    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    public void SetUp(ScriptableRenderContext scriptableRenderContext,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
        _scriptableRenderContext = scriptableRenderContext;
        _cullingResults = cullingResults;
        _shadowSettings = shadowSettings;
        ShadowedDirectionalLightCount = 0;
    }

    private void ExecuteBuffer()
    {
        _scriptableRenderContext.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f&&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight {
                    visibleLightIndex = visibleLightIndex
                };
            return new Vector2(
                light.shadowStrength,   _shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++
            );
        }
        else
        {
            return Vector2.zero;
        }
    }
    
    public void Render () {
        if (ShadowedDirectionalLightCount > 0) {
            RenderDirectionalShadows();
        }
        else
        {
            _commandBuffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)_shadowSettings.directional.atlasSize;
        _commandBuffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true,false,Color.clear);
        _commandBuffer.BeginSample(_bufferName);
        ExecuteBuffer();
        int tiles = ShadowedDirectionalLightCount * _shadowSettings.directional.cascadeCount;
        int split = ShadowedDirectionalLightCount <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++) {
            RenderDirectionalShadows(i, split, tileSize);
        }
        _commandBuffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        float f = 1f - _shadowSettings.directional.cascadeFade;
        _commandBuffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / _shadowSettings.maxDistance, 1f / _shadowSettings.distanceFade,1f / (1f - f * f)
            ));
        _commandBuffer.EndSample(_bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight _shadowedDirectionalLight = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults,_shadowedDirectionalLight.visibleLightIndex);
        
        int cascadeCount = _shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _shadowSettings.directional.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
           
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                _shadowedDirectionalLight.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            shadowSettings.splitData = splitData;
            if (index == 0) {
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }
            SetTileViewport(index,split,tileSize);
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split
            );
            _commandBuffer.SetGlobalInt(cascadeCountId, _shadowSettings.directional.cascadeCount);
            _commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId,cascadeCullingSpheres);
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            _commandBuffer.SetGlobalDepthBias(500000f, 0);
            ExecuteBuffer();
            _scriptableRenderContext.DrawShadows(ref shadowSettings);
            _commandBuffer.SetGlobalDepthBias(0, 0);

        }
    }

    private Vector2 SetTileViewport(int index,int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        _commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    
    Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
        if (SystemInfo.usesReversedZBuffer) {
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