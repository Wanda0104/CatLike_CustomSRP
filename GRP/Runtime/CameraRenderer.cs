using UnityEngine;
using UnityEngine.Rendering;
namespace GRP.Runtime
{
    public partial class CameraRenderer
    {
        private ScriptableRenderContext m_context;
        private Camera m_camera;
        private CommandBuffer m_cmd;
        private CullingResults m_cullingResults;
        private Lighting m_lighting = new Lighting();

        private const string k_bufferName = "[GRP] Render Camera";

        private ShaderTagId k_unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private ShaderTagId k_litShaderTagId = new ShaderTagId("CustomLight");

        public CameraRenderer()
        {
            m_cmd = CommandBufferPool.Get();
            m_cmd.name = k_bufferName;
        }

        public void SetUp()
        {
            m_context.SetupCameraProperties(m_camera);
            CameraClearFlags cameraClearFlags = m_camera.clearFlags;
            m_cmd.ClearRenderTarget(cameraClearFlags <= CameraClearFlags.Depth,
                cameraClearFlags == CameraClearFlags.Color,
                cameraClearFlags == CameraClearFlags.Color ? m_camera.backgroundColor.linear : Color.clear);
            m_cmd.BeginSample(SampleName);
            ExecuteBuffer();
        }

        public void Render(ScriptableRenderContext _context, Camera _camera,
            bool _enableInstancing, bool _enableDynamicBatching,
            ShadowSettings shadowSettings)
        {
            m_context = _context;
            m_camera = _camera;
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(shadowSettings.maxDistance))
            {
                return;
            }

            m_cmd.BeginSample(SampleName);
            ExecuteBuffer();
            m_lighting.SetUp(_context, m_cullingResults, shadowSettings);
            m_cmd.EndSample(SampleName);
            SetUp();
            DrawVisibleGeometry(_enableInstancing, _enableDynamicBatching);
            DrawUnSupportedShaders();
            DrawGizmos();
            m_lighting.CleanUp();
            Submit();
        }


        private void DrawVisibleGeometry(bool _enableInstancing, bool _enableDynamicBatching)
        {

            var sortSetting = new SortingSettings(m_camera) {criteria = SortingCriteria.CommonOpaque};
            var drawingSetting = new DrawingSettings(k_unlitShaderTagId, sortSetting)
            {
                enableInstancing = _enableInstancing,
                enableDynamicBatching = _enableDynamicBatching,
                perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe |
                                PerObjectData.LightProbeProxyVolume | PerObjectData.ShadowMask |
                                PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume |
                                PerObjectData.ReflectionProbes,
            };
            drawingSetting.SetShaderPassName(1, k_litShaderTagId);

            var filteringSetting = new FilteringSettings(RenderQueueRange.opaque);
            //Draw Opaque
            m_context.DrawRenderers(m_cullingResults, ref drawingSetting, ref filteringSetting);

            m_context.DrawSkybox(m_camera);
            sortSetting.criteria = SortingCriteria.CommonTransparent;
            filteringSetting.renderQueueRange = RenderQueueRange.transparent;
            //Draw Transparent
            m_context.DrawRenderers(m_cullingResults, ref drawingSetting, ref filteringSetting);

        }

        private void Submit()
        {
            m_cmd.EndSample(SampleName);
            ExecuteBuffer();
            m_context.Submit();
        }

        private void ExecuteBuffer()
        {
            m_context.ExecuteCommandBuffer(m_cmd);
            m_cmd.Clear();
        }

        private bool Cull(float maxShadowDistance)
        {
            //Camera Cull
            if (m_camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, m_camera.farClipPlane);
                m_cullingResults = m_context.Cull(ref p);
                return true;
            }
            else
            {
                return false;
            }
        }
}
}