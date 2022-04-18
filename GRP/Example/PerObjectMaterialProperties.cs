using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    [SerializeField]
    private Color baseColor = Color.white;
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
        _materialPropertyBlock.SetColor(baseColorId,Random.ColorHSV());
        gameObject.GetComponent<Renderer>().SetPropertyBlock(_materialPropertyBlock);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
