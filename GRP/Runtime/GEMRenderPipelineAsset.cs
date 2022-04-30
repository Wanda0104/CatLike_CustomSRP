using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/GEMRenderPipeline/PipelineAssets")]
public class GEMRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    ShadowSettings shadows = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new GEMRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadows);
    }
}
