using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness");
    [SerializeField]
    private Mesh _mesh = default;
    [SerializeField]
    private Material _material = default;
    [SerializeField]
    LightProbeProxyVolume lightProbeVolume = null;

    private Matrix4x4[] _matrixs = new Matrix4x4[1023];
    private Vector4[] _baseColors = new Vector4[1023];
    
    float[]
        metallic = new float[1023],
        smoothness = new float[1023];

    private MaterialPropertyBlock _materialPropertyBlock;

    private void Awake()
    {
        FillBall();
    }

    private void FillBall()
    {
        for (int i = 0; i < _matrixs.Length; i++)
        {
            _matrixs[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, quaternion.identity, Vector3.one);
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f,1f));
            metallic[i] = Random.value < 0.25f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
            var lightProbes = new SphericalHarmonicsL2[1023];
           
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DrawInstanced();
    }

    private void DrawInstanced()
    {
        if (_materialPropertyBlock == null)
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
            _materialPropertyBlock.SetVectorArray(baseColorId,_baseColors);
            _materialPropertyBlock.SetFloatArray(metallicId, metallic);
            _materialPropertyBlock.SetFloatArray(smoothnessId, smoothness);
            if (lightProbeVolume == null)
            {
                var positions = new Vector3[1023];
                for (int i = 0; i < _matrixs.Length; i++) {
                    positions[i] = _matrixs[i].GetColumn(3);
                }
                var lightProbes = new SphericalHarmonicsL2[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(
                    positions, lightProbes, null
                );
                _materialPropertyBlock.CopySHCoefficientArraysFrom(lightProbes);
            }
            
        }
        
        Graphics.DrawMeshInstanced(_mesh,0,_material,_matrixs,1023,_materialPropertyBlock,
            ShadowCastingMode.On,true,0,null,lightProbeVolume ?
                LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided,
            lightProbeVolume);
    }
}
