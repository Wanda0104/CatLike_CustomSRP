using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
   private const string bufferName = "Lighting";
   private CommandBuffer _commandBuffer = new CommandBuffer()
   {
      name = bufferName
   };

   private CullingResults m_cullingResults;
   private const int maxDirLightCount = 4;
   private static int 
      dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
      dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
      dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
   private static Vector4[]
      dirLightColors = new Vector4[maxDirLightCount],
      dirLightDirections = new Vector4[maxDirLightCount];
   public void SetUp(ScriptableRenderContext context,CullingResults _cullingResults)
   {
      _commandBuffer.BeginSample(bufferName);
      // SetupDirectionalLight();
      m_cullingResults = _cullingResults;
      SetUpLights();
      _commandBuffer.EndSample(bufferName);
      context.ExecuteCommandBuffer(_commandBuffer);
      _commandBuffer.Clear();
   }

   private void SetUpLights()
   {
      NativeArray<VisibleLight> visibleLights = m_cullingResults.visibleLights;
      int dirLightCount = 0;
      for (int i = 0; i < visibleLights.Length; i++)
      {
         VisibleLight visibleLight = visibleLights[i];
         if (visibleLight.lightType == LightType.Directional)
         {
            SetupDirectionalLight(dirLightCount++,ref visibleLight);
            if (dirLightCount >= maxDirLightCount) {
               break;
            }
         }
      }
      _commandBuffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
      _commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
      _commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
   }
   
   private void SetupDirectionalLight(int index,ref VisibleLight visibleLight)
   {
      dirLightColors[index] = visibleLight.finalColor;
      dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
   }
   
}
