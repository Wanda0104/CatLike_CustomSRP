using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    [SerializeField]
    private Mesh _mesh = default;
    [SerializeField]
    private Material _material = default;

    private Matrix4x4[] _matrixs = new Matrix4x4[1023];
    private Vector4[] _baseColors = new Vector4[1023];

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
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
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
        }
        
        Graphics.DrawMeshInstanced(_mesh,0,_material,_matrixs,1023,_materialPropertyBlock);
    }
}
