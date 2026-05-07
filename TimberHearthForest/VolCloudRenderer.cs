using System;
using UnityEngine;

namespace TimberHearthForest;

[RequireComponent(typeof(Camera))]
public class VolCloudRenderer : MonoBehaviour
{
    public Material Material;
    public Material CompositeMaterial;
    
    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var rt = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0, RenderTextureFormat.ARGBHalf);
        RenderTexture.active = rt;
        
        Material.SetPass(0);
        Graphics.DrawMeshNow();
        
        Graphics.Blit(source, destination);
        Graphics.Blit(rt, destination, CompositeMaterial); // will additively blend
    }
}