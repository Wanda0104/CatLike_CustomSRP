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
        
        private const string k_bufferName = "[GRP] Render Camera";
        
        private ShaderTagId k_unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        public CameraRenderer()
        {
            m_cmd = CommandBufferPool.Get();
            m_cmd.name = k_bufferName;
        }

        public void SetUp()
        {
            m_context.SetupCameraProperties(m_camera);
            CameraClearFlags cameraClearFlags = m_camera.clearFlags;
            m_cmd.ClearRenderTarget( cameraClearFlags <= CameraClearFlags.Depth,cameraClearFlags == CameraClearFlags.Color,cameraClearFlags == CameraClearFlags.Color ? m_camera.backgroundColor.linear : Color.clear);
            m_cmd.BeginSample(SampleName);
            ExecuteBuffer();
        }
        
        public void Render(ScriptableRenderContext _context, Camera _camera,
            bool _enableInstancing,bool _enableDynamicBatching)
        {
            m_context = _context;
            m_camera = _camera;
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull())
            {
                return;
            }
            SetUp();
            DrawVisibleGeometry(_enableInstancing,_enableDynamicBatching);
            DrawUnSupportedShaders();
            DrawGizmos();
            Submit();
        }


        private void DrawVisibleGeometry(bool _enableInstancing,bool _enableDynamicBatching)
        {
            var sortSetting = new SortingSettings(m_camera){criteria = SortingCriteria.CommonOpaque};
            var drawingSetting = new DrawingSettings(k_unlitShaderTagId,sortSetting)
            {
                enableInstancing = _enableInstancing,
                enableDynamicBatching = _enableDynamicBatching
            };
            var filteringSetting = new FilteringSettings(RenderQueueRange.opaque);
            m_context.DrawRenderers(m_cullingResults,ref drawingSetting,ref filteringSetting);
            
            m_context.DrawSkybox(m_camera);
            sortSetting.criteria = SortingCriteria.CommonTransparent;
            filteringSetting.renderQueueRange = RenderQueueRange.transparent;
            m_context.DrawRenderers(m_cullingResults,ref drawingSetting,ref filteringSetting);

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

        private bool Cull()
        {
            if (m_camera.TryGetCullingParameters(out var p))
            {
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