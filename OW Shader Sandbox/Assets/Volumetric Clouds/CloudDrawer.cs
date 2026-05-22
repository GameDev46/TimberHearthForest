using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CloudDrawer : MonoBehaviour
{
    public Light light;
    public Renderer cloudRenderer, cloudShadowRenderer;
    public Material compositeMaterial, shadowCompositeMaterial;
    private CommandBuffer drawCmd, shadowDrawCmd, compositeCmd, shadowCompositeCmd;
    private RenderTexture drawBuffer, shadowBuffer;

    public int DOWNSAMPLE = 4;

    private void OnEnable()
    {
        if (light == null ||
            cloudRenderer == null ||
            cloudShadowRenderer == null ||
            compositeMaterial == null ||
            shadowCompositeMaterial == null) return;

        cloudRenderer.enabled = false;
        cloudShadowRenderer.enabled = false;

        drawCmd = new CommandBuffer();
        drawCmd.name = nameof(drawCmd);
        shadowDrawCmd = new CommandBuffer();
        shadowDrawCmd.name = nameof(shadowDrawCmd);
        compositeCmd = new CommandBuffer();
        compositeCmd.name = nameof(compositeCmd);
        shadowCompositeCmd = new CommandBuffer();
        shadowCompositeCmd.name = nameof(shadowCompositeCmd);

        var mainCam = Camera.main;
        drawBuffer = new RenderTexture(mainCam.pixelWidth / DOWNSAMPLE, mainCam.pixelHeight / DOWNSAMPLE, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        shadowBuffer = new RenderTexture(mainCam.pixelWidth / DOWNSAMPLE, mainCam.pixelHeight / DOWNSAMPLE, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

        foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>())
        {
            cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, drawCmd);
            // i think render target gets restored here hopefully
            cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, compositeCmd);
        }

        light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, shadowDrawCmd);
        light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, shadowCompositeCmd);

        Camera.onPreRender += BuildCommandBuffer;
        Camera.onPostRender += ClearCommandBuffer;
    }

    private void OnDisable()
    {
        if (light == null ||
            cloudRenderer == null ||
            cloudShadowRenderer == null ||
            compositeMaterial == null ||
            shadowCompositeMaterial == null) return;
        
        cloudRenderer.enabled = true;
        cloudShadowRenderer.enabled = true;
        
        Camera.onPreRender -= BuildCommandBuffer;
        Camera.onPostRender -= ClearCommandBuffer;

        foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>())
        {
            cam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, drawCmd);
            // i think render target gets restored here hopefully
            cam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, compositeCmd);
        }

        light.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, shadowCompositeCmd);

        drawCmd.Release();
        drawCmd = null;
        compositeCmd.Release();
        compositeCmd = null;
        shadowCompositeCmd.Release();
        shadowCompositeCmd = null;
        
        drawBuffer.Release();
        drawBuffer = null;
        shadowBuffer.Release();
        shadowBuffer = null;
    }

    private void BuildCommandBuffer(Camera cam)
    {
        // Shadow hack quad initialization for stable culling
        if (cam.transform.Find("shadow hack") == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "shadow hack";
            quad.transform.SetParent(cam.transform, false);
            quad.transform.localPosition = Vector3.forward * 0.1f;
            quad.transform.localScale = Vector3.zero;
        }

        // this section draws to our downsampled buffers. separate because we mess with render target and that needs to be restored after this executes
        // idk if should be linear or srgb. mess with it
        // matrices should already be set to camera at this point
        // rebuild this one every frame since it uses camera width/height which might change (like in Camera.onPreRender cuz that gives you the camera)
        /*foreach cloud*/
        drawCmd.SetRenderTarget(drawBuffer);
        drawCmd.ClearRenderTarget(true, true, Color.clear);
        drawCmd.DrawRenderer(cloudRenderer, cloudRenderer.sharedMaterial);
        // this copies to screen
        compositeCmd.Blit(drawBuffer, BuiltinRenderTextureType.CurrentActive, compositeMaterial);

        shadowDrawCmd.SetRenderTarget(shadowBuffer);
        shadowDrawCmd.ClearRenderTarget(true, true, Color.clear);
        shadowDrawCmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        shadowDrawCmd.DrawRenderer(cloudShadowRenderer, cloudShadowRenderer.sharedMaterial);
        // this copies to shadow mask
        shadowCompositeCmd.Blit(shadowBuffer, BuiltinRenderTextureType.CurrentActive, shadowCompositeMaterial);
    }

    private void ClearCommandBuffer(Camera cam)
    {
        drawCmd.Clear();
        compositeCmd.Clear();
        shadowDrawCmd.Clear();
        shadowCompositeCmd.Clear();
    }
}