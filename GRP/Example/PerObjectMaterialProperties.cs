using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    [SerializeField]
    private Color baseColor = Color.white;
    [SerializeField,Range(0f,1f)]
    private float cutoff = 0.5f;
    static MaterialPropertyBlock _materialPropertyBlock; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        _materialPropertyBlock ??= new MaterialPropertyBlock();
        baseColor = Random.ColorHSV();
        baseColor.a = Random.value;
        cutoff = Random.Range(0f, 1f);
        _materialPropertyBlock.SetColor(baseColorId,baseColor);
        _materialPropertyBlock.SetFloat(cutoffId,cutoff);
        gameObject.GetComponent<Renderer>().SetPropertyBlock(_materialPropertyBlock);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
