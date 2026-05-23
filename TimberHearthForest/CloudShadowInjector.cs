using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimberHearthForest
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CloudShadowInjector : MonoBehaviour
    {
        public Light targetLight;
        private MeshRenderer meshRenderer;
        private CommandBuffer cmd;

        public void Start()
        {
            StartCoroutine(WaitToSetupVolumetricShadows());
        }

        private IEnumerator WaitToSetupVolumetricShadows()
        {
            yield return new WaitForSeconds(3.0f);

            targetLight = Locator.GetSunController()._sunLight._sunLight;
            EnabledShadows();
        }

        private void EnabledShadows()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;

            if (targetLight != null)
            {
                cmd = new CommandBuffer();
                cmd.name = "Volumetric Cloud Shadow Injection";

                // Inject into the screenspace shadow mask
                targetLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);

                Camera.onPreRender += BuildCommandBuffer;
                Camera.onPostRender += ClearCommandBuffer;
            }
        }

        private void OnDisable()
        {
            if (targetLight != null && cmd != null)
            {
                targetLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, cmd);
                cmd.Release();
                cmd = null;
            }

            Camera.onPreRender -= BuildCommandBuffer;
            Camera.onPostRender -= ClearCommandBuffer;
        }

        private void BuildCommandBuffer(Camera cam)
        {
            if (cmd == null || meshRenderer == null) return;

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

            cmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
            cmd.DrawRenderer(meshRenderer, meshRenderer.sharedMaterial);
        }

        private void ClearCommandBuffer(Camera cam)
        {
            if (cmd != null) cmd.Clear();
        }
    }
}
