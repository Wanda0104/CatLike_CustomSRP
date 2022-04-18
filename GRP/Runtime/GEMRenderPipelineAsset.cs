using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "Rendering/GEMRenderPipeline/PipelineAssets")]
public class GEMRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new GEMRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher);
    }
}
