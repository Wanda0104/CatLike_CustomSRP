using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Profiling;

#endif

namespace GRP.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawUnSupportedShaders();
        partial void DrawGizmos();
        partial void PrepareForSceneWindow();
        partial void PrepareBuffer();
#if UNITY_EDITOR
        private string SampleName
        {
            get;
            set;
        }
#else
        private const string SampleName = k_bufferName;
#endif
        
#if UNITY_EDITOR
        private static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrePassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        private static Material m_errorMatrial;
        partial void DrawUnSupportedShaders()
        {
            if (m_errorMatrial == null)
            {
                m_errorMatrial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            var drawSetting = new DrawingSettings(CameraRenderer.legacyShaderTagIds[0], new SortingSettings(m_camera))
            {
                overrideMaterial = m_errorMatrial
            };
            for (int idx = 1; idx < CameraRenderer.legacyShaderTagIds.Length - 1; idx++)
            {
                drawSetting.SetShaderPassName(idx,CameraRenderer.legacyShaderTagIds[idx]);
            }
            var filteringSetting = FilteringSettings.defaultValue;
            m_context.DrawRenderers(m_cullingResults,ref drawSetting,ref filteringSetting);
        }

        partial void DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                m_context.DrawGizmos(m_camera,GizmoSubset.PreImageEffects);
                m_context.DrawGizmos(m_camera,GizmoSubset.PostImageEffects);
            }
        }

        partial void PrepareForSceneWindow()
        {
            if (m_camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(m_camera);
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("[GRP] Editor Only");
            m_cmd.name = SampleName = string.Format("[GRP]{0}",m_camera.name);
            Profiler.EndSample();
        }
#endif
    }
}
