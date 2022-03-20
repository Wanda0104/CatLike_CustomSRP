using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "Rendering/GEMRenderPipeline/PipelineAssets")]
public class GEMRenderPipelineAsset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new GEMRenderPipeline();
    }
}
