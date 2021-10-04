using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");

    private static MaterialPropertyBlock block;
    [SerializeField] 
    Color baseColor = Color.white;

    [SerializeField] 
    private float cutoff = 0.5f;

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId,baseColor);
        block.SetFloat(cutoffId,cutoff);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }
}
