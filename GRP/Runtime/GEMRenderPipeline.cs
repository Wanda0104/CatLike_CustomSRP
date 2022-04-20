using System.Collections;
using System.Collections.Generic;
using GRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class GEMRenderPipeline : RenderPipeline
{
    private CameraRenderer _cameraRenderer = new CameraRenderer();
    private bool useDynamicBatching, useGPUInstancing;
    private ShadowSettings shadowSettings;
    public GEMRenderPipeline(bool _useDynamicBatching, bool _useGPUInstancing, bool _useSRPBatcher,ShadowSettings _shadowSettings)
    {
        useDynamicBatching = _useDynamicBatching;
        useGPUInstancing = _useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = _useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        shadowSettings = _shadowSettings;
    }
    protected override void Render(ScriptableRenderContext _context, Camera[] _cameras)
    {
        RenderCameras(_context,_cameras);
    }

    private void RenderCameras(ScriptableRenderContext _context, Camera[] _cameras)
    {
        foreach (var _camera in _cameras)
        {
            _cameraRenderer.Render(_context,_camera,useGPUInstancing,useDynamicBatching,shadowSettings);
        }   
    }
}
