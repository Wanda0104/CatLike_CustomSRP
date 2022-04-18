using System.Collections;
using System.Collections.Generic;
using GRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class GEMRenderPipeline : RenderPipeline
{
    private CameraRenderer _cameraRenderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing;
    
    public GEMRenderPipeline(bool _useDynamicBatching, bool _useGPUInstancing, bool _useSRPBatcher)
    {
        useDynamicBatching = _useDynamicBatching;
        useGPUInstancing = _useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = _useSRPBatcher;
    }
    protected override void Render(ScriptableRenderContext _context, Camera[] _cameras)
    {
        RenderCameras(_context,_cameras);
    }

    private void RenderCameras(ScriptableRenderContext _context, Camera[] _cameras)
    {
        foreach (var _camera in _cameras)
        {
            _cameraRenderer.Render(_context,_camera,useDynamicBatching,useGPUInstancing);
        }   
    }
}
