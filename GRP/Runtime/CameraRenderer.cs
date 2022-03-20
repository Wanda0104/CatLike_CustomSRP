using UnityEngine;
using UnityEngine.Rendering;

namespace GRP.Runtime
{
    public class CameraRenderer
    {
        private ScriptableRenderContext m_context;
        private Camera m_camera;
        private CommandBuffer m_cmd;
        private CullingResults m_cullingResults;
        
        private const string k_bufferName = "Render Camera";
        private ShaderTagId k_unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        public CameraRenderer()
        {
            m_cmd = CommandBufferPool.Get();
            m_cmd.name = k_bufferName;
        }

        public void SetUp()
        {
            m_context.SetupCameraProperties(m_camera);
            m_cmd.ClearRenderTarget(true,true,Color.clear);
            m_cmd.BeginSample(k_bufferName);
            ExecuteBuffer();
        }
        
        public void Render(ScriptableRenderContext _context, Camera _camera)
        {
            m_context = _context;
            m_camera = _camera;
            if (!Cull())
            {
                return;
            }
            SetUp();
            DrawVisibleGeometry();
            Submit();
        }

        private void DrawVisibleGeometry()
        {
            var sortSetting = new SortingSettings(m_camera);
            var drawingSetting = new DrawingSettings(k_unlitShaderTagId,sortSetting);
            var filteringSetting = new FilteringSettings(RenderQueueRange.all);
            m_context.DrawRenderers(m_cullingResults,ref drawingSetting,ref filteringSetting);
            
            m_context.DrawSkybox(m_camera);
        }

        private void Submit()
        {
            m_cmd.EndSample(k_bufferName);
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