using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace TimberHearthForest
{
    internal class CloudUtils
    {
        public static float MAX_CLOUD_SPHERE_RADIUS = 295.0f;

        private static string modFolderPath = "";
        private static IModConsole modConsole;

        private static AssetBundle cloudBundle;
        private static Material cloudMaterial;

        private static AssetBundle volumetricCloudBundle;
        private static Material volumetricCloudMaterial;
        private static Material volumetricCloudShadowMaterial;

        private static GameObject cloudShadowSphere;

        public static void SetModDirectoryPath(string dirPath)
        {
            modFolderPath = dirPath;
        }

        public static void SetModConsole(IModConsole console)
        {
            modConsole = console;
        }

        public static void LoadCloudsAssetBundle()
        {
            try
            {
                if (cloudMaterial == null) {

                    string platformFolder = "Windows";

                    /*switch (Application.platform)
                    {
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            platformFolder = "Windows";
                            break;
                        case RuntimePlatform.LinuxPlayer:
                            platformFolder = "Linux";
                            break;
                        case RuntimePlatform.OSXPlayer:
                            platformFolder = "Mac";
                            break;
                        default:
                            modConsole.WriteLine($"Unsupported platform: {Application.platform}", MessageType.Warning);
                            return;
                    }*/

                    string bundlePath = Path.Combine(modFolderPath, "Assets", platformFolder, "cloudbundle");
                    cloudBundle = AssetBundle.LoadFromFile(bundlePath);

                    if (cloudBundle == null)
                    {
                        modConsole.WriteLine("Failed to load cloud AssetBundle", MessageType.Error);
                        return;
                    }

                    cloudMaterial = cloudBundle.LoadAsset<Material>("CloudMaterial");
                }
            }
            catch (Exception e)
            {
                modConsole.WriteLine($"Failed to load the cloud asset bundle from Assets/cloudbundle: {e}", MessageType.Error);
            }
        }


        public static void CreateCloud(GameObject cloudHolder, float cloudRadius, string textureName, string normalName, float cloudSpeed, bool isOutwardFacing, ref List<(GameObject, float)> cloudObjects)
        {
            // Check if the cloud radius is larger than the maximum
            if (MAX_CLOUD_SPHERE_RADIUS < cloudRadius) MAX_CLOUD_SPHERE_RADIUS = cloudRadius;

            // Load the cloud texture
            string albedoTexturePath = Path.Combine(modFolderPath, "Assets", textureName + ".png");
            Texture2D albedoMap = FileLoadingUtils.LoadTexture(albedoTexturePath, modConsole);

            if (albedoMap == null)
            {
                modConsole.WriteLine($"Failed to load the cloud texture file: {textureName}", MessageType.Error);
                return;
            }

            if (cloudMaterial == null)
            {
                modConsole.WriteLine($"Failed to locate the cloud material from Assets/cloudbundle", MessageType.Error);
                return;
            }

            Material mat = UnityEngine.Material.Instantiate(cloudMaterial);

            mat.SetFloat("_AmbientStrength", isOutwardFacing ? 0.3f: 0.1f);
            mat.SetColor("_AmbientColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));

            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Glossiness", 0.0f);

            mat.SetFloat("_FresnelPower", isOutwardFacing ? 1.0f : 0.0f);
            mat.SetFloat("_FresnelFade", isOutwardFacing ? 1.0f : 0.0f);
            mat.SetFloat("_AlphaBoost", 1.0f);

            mat.mainTexture = albedoMap;
            mat.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            /*var geyserStrips = new List<(int, int)>
            {
                (516 - 84, 522 - 84),
                (530 - 84, 536 - 84),
                (582 - 84, 588 - 84),
                (591 - 84, 597 - 84)
            };*/

            // Load the cloud normal texture
            string normalTexturePath = Path.Combine(modFolderPath, "Assets", normalName + ".png");
            Texture2D normalMap = FileLoadingUtils.LoadTexture(normalTexturePath, modConsole);

            if (normalMap == null)
            {
                modConsole.WriteLine("Failed to load the cloud normal texture file", MessageType.Error);
            }
            else
            {
                mat.SetTexture("_NormalTex", normalMap);
                mat.SetFloat("_NormalStrength", 0.5f);
                // If the cloud faces towards space then the normal map should be inverted
                if (isOutwardFacing) mat.SetFloat("_NormalStrength", -1.0f);
            }

            // Create the cloud sphere
            GameObject cloudSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloudSphere.transform.SetParent(cloudHolder.transform, false);

            cloudSphere.GetComponent<MeshFilter>()?.mesh = CreateSphereMesh(64, 64, 1.0f);

            // Invert the normals of the mesh if the cloud faces outwards towards space
            if (isOutwardFacing) cloudSphere.GetComponent<MeshFilter>().mesh = InvertMesh(cloudSphere.GetComponent<MeshFilter>().mesh);

            cloudSphere.name = isOutwardFacing ? "TH_Clouds_Out" : "TH_Clouds_In";
            cloudSphere.GetComponent<SphereCollider>()?.enabled = false;

            cloudSphere.transform.localPosition = Vector3.zero;
            cloudSphere.transform.localRotation = Quaternion.identity;
            cloudSphere.transform.localScale = Vector3.one * cloudRadius;

            MeshRenderer cloudRenderer = cloudSphere.GetComponent<MeshRenderer>();
            cloudRenderer.material = mat;
            cloudRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cloudRenderer.receiveShadows = true;

            // Store the in facing cloud sphere renderer
            cloudObjects.Add((cloudSphere, cloudSpeed));
        }

        public static void LoadVolumetricCloudsAssetBundle()
        {
            try
            {
                if (volumetricCloudMaterial == null)
                {
                    string platformFolder = "Windows";

                    /*switch (Application.platform)
                    {
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            platformFolder = "Windows";
                            break;
                        case RuntimePlatform.LinuxPlayer:
                            platformFolder = "Linux";
                            break;
                        case RuntimePlatform.OSXPlayer:
                            platformFolder = "Mac";
                            break;
                        default:
                            modConsole.WriteLine($"Unsupported platform: {Application.platform}", MessageType.Warning);
                            return;
                    }*/

                    string bundlePath = Path.Combine(modFolderPath, "Assets", platformFolder, "volumetricclouds");
                    volumetricCloudBundle = AssetBundle.LoadFromFile(bundlePath);

                    if (volumetricCloudBundle == null)
                    {
                        modConsole.WriteLine("Failed to load volumetric clouds AssetBundle", MessageType.Error);
                        return;
                    }

                    volumetricCloudMaterial = volumetricCloudBundle.LoadAsset<Material>("VolumetricCloudMaterial");
                    volumetricCloudShadowMaterial = volumetricCloudBundle.LoadAsset<Material>("VolumetricShadowMaterial");
                }
            }
            catch (Exception e)
            {
                modConsole.WriteLine($"Failed to load the volumetric cloud asset bundle from Assets/volumetricclouds: {e}", MessageType.Error);
            }
        }

        public static void CreateVolumetricCloud(GameObject cloudHolder, float cloudInnerRadius, float cloudOuterRadius, ref List<(GameObject, GameObject)> volumetricClouds)
        {

            if (volumetricCloudMaterial == null)
            {
                modConsole.WriteLine($"Failed to locate the volumetric cloud material from Assets/volumetricclouds", MessageType.Error);
                return;
            }

            if (volumetricCloudShadowMaterial == null)
            {
                modConsole.WriteLine($"Failed to locate the volumetric cloud shadow material from Assets/volumetricclouds", MessageType.Error);
                return;
            }

            // Setup the volumetric clouds material
            Material mat = volumetricCloudMaterial;

            mat.SetFloat("_ErosionStrength", 0.3f);
            mat.SetFloat("_BlueNoiseStrength", 1.0f); // Dithering power

            mat.SetFloat("_OuterRadius", cloudOuterRadius);
            mat.SetFloat("_InnerRadius", cloudInnerRadius);
            mat.SetFloat("_PlanetRadius", 254); // th radius
            mat.SetFloat("_MoonRadius", 80);

            mat.SetFloat("_StepSize", 10.0f);
            mat.SetFloat("_SunStepSize", 20.0f);
            mat.SetFloat("_DenseStepSize", 4.0f); // The step size in the denser dark clouds (where banding is most prevalent)

            mat.SetFloat("_CloudScale", 1.5f);
            mat.SetFloat("_DensityMultiplier", 1.0f);
            mat.SetFloat("_DensityThreshold", 0.65f);

            mat.SetFloat("_LightAbsorptionThroughCloud", 0.8f);
            mat.SetFloat("_LightAbsorptionTowardsSun", 0.4f);
            // mat.SetFloat("_DarknessThreshold", 0.2f);

            mat.SetFloat("_PhaseIntensity", 4.0f);
            mat.SetFloat("_ForwardScatteringBias", 0.2f);

            mat.SetVector("_SunDirection", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            mat.SetColor("_SunColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));

            // ambient texture set in bundle
            mat.SetFloat("_AmbientStrength", 0.2f);

            mat.SetVector("_Offset", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_Center", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

            // Create the cloud sphere
            GameObject cloudSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloudSphere.transform.SetParent(cloudHolder.transform, false);

            cloudSphere.GetComponent<MeshFilter>()?.mesh = CreateSphereMesh(64, 64, 1.0f);
            cloudSphere.GetComponent<MeshFilter>()?.mesh = InvertMesh(cloudSphere.GetComponent<MeshFilter>()?.mesh);

            cloudSphere.name = "Volumetric Clouds";
            cloudSphere.GetComponent<SphereCollider>()?.enabled = false;

            cloudSphere.transform.localPosition = Vector3.zero;
            cloudSphere.transform.localRotation = Quaternion.identity;
            cloudSphere.transform.localScale = Vector3.one * cloudOuterRadius * 1.5f;

            MeshRenderer cloudRenderer = cloudSphere.GetComponent<MeshRenderer>();
            cloudRenderer.material = mat;
            cloudRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cloudRenderer.receiveShadows = false;

            // Setup the volumetric cloud's shadow material
            Material shadowMat = volumetricCloudShadowMaterial;

            shadowMat.SetFloat("_ErosionStrength", 0.3f);
            shadowMat.SetFloat("_BlueNoiseStrength", 0.2f); // Dithering power
            shadowMat.SetFloat("_BlueNoiseScale", 15.0f);

            shadowMat.SetFloat("_OuterRadius", cloudOuterRadius);
            shadowMat.SetFloat("_InnerRadius", cloudInnerRadius);

            shadowMat.SetFloat("_CloudScale", 1.5f);
            shadowMat.SetFloat("_DensityMultiplier", 1.0f);
            shadowMat.SetFloat("_DensityThreshold", 0.65f);

            shadowMat.SetVector("_SunDirection", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));

            shadowMat.SetVector("_Offset", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            shadowMat.SetVector("_Center", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));

            // Create the cloud shadowcaster sphere
            cloudShadowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloudShadowSphere.transform.SetParent(cloudHolder.transform, false);

            cloudShadowSphere.GetComponent<MeshFilter>()?.mesh = CreateSphereMesh(64, 64, 1.0f);
            cloudShadowSphere.GetComponent<MeshFilter>()?.mesh = InvertMesh(cloudShadowSphere.GetComponent<MeshFilter>()?.mesh);

            cloudShadowSphere.name = "Volumetric Clouds Shadowcaster";
            cloudShadowSphere.GetComponent<SphereCollider>()?.enabled = false;

            cloudShadowSphere.transform.localPosition = Vector3.zero;
            cloudShadowSphere.transform.localRotation = Quaternion.identity;
            cloudShadowSphere.transform.localScale = Vector3.one * cloudOuterRadius * 1.5f;

            MeshRenderer cloudShadowRenderer = cloudShadowSphere.GetComponent<MeshRenderer>();
            cloudShadowRenderer.material = shadowMat;
            cloudShadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cloudShadowRenderer.receiveShadows = false;

            CloudShadowInjector cloudShadowInjector = cloudShadowSphere.AddComponent<CloudShadowInjector>();
            cloudShadowInjector.enabled = true;

            // Store the volumetric cloud sphere gameobject
            volumetricClouds.Add((cloudSphere, cloudShadowSphere));
        }

        #region command buffer shenanigans

        /*public static void Stuff()
        {
            // draft code
            var drawCmd = new CommandBuffer();
            var compositeCmd = new CommandBuffer();
            var shadowCmd = new CommandBuffer();

            var cam = Locator.GetActiveCamera().mainCamera; // This covers player, map and FREECAM - not sure about the probe and sattelite cam
            var light = Locator.GetSunController()._sunLight._sunLight; // First _sunLight is "SunLightController", second is "Light"
            var cloudRenderer = new Renderer(); // TODO: draws to regular screen eventually
            var cloudShadowRenderer = cloudShadowSphere.GetComponent<MeshRenderer>();
            var compositeMaterial = new Material(""); // TODO: copies to screen with `Blend One OneMinusSrcAlpha` 
            
            // put some geometry in front of the camera so shadows are forced to render
            if (cam.transform.Cast<Transform>().All(x => x.name != "shadow hack"))
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                GameObject.Destroy(quad.GetComponent<Collider>());
                quad.name = "shadow hack";
                quad.transform.SetParent(cam.transform, false);
                quad.transform.localPosition = Vector3.forward;
                quad.transform.localScale = Vector3.zero;
                Debug.Log("placed quad");
            }

            const int DOWNSAMPLE = 4;
            var drawBuffer = Shader.PropertyToID("_DrawBuffer");
            var shadowBuffer = Shader.PropertyToID("_ShadowBuffer");
            // this section draws to our downsampled buffers. separate because we mess with render target and that needs to be restored after this executes
            // idk if should be linear or srgb. mess with it
            // matrices should already be set to camera at this point
            // rebuild this one every frame since it uses camera width/height which might change (like in Camera.onPreRender cuz that gives you the camera)
            drawCmd.GetTemporaryRT(drawBuffer, cam.pixelWidth / DOWNSAMPLE, cam.pixelHeight / DOWNSAMPLE, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            drawCmd.GetTemporaryRT(shadowBuffer, cam.pixelWidth / DOWNSAMPLE, cam.pixelHeight / DOWNSAMPLE, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            //foreach cloud
            drawCmd.SetRenderTarget(drawBuffer);
            drawCmd.DrawRenderer(cloudRenderer, cloudRenderer.sharedMaterial);
            drawCmd.SetRenderTarget(drawBuffer);
            drawCmd.DrawRenderer(cloudShadowRenderer, cloudShadowRenderer.sharedMaterial);
            
            // !! alternatively
            drawCmd.GetTemporaryRT(drawBuffer, cam.pixelWidth / DOWNSAMPLE, cam.pixelHeight / DOWNSAMPLE, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            drawCmd.GetTemporaryRT(shadowBuffer, cam.pixelWidth / DOWNSAMPLE, cam.pixelHeight / DOWNSAMPLE, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            drawCmd.SetRenderTarget([drawBuffer, shadowBuffer], drawBuffer);
            //foreach cloud
            drawCmd.DrawRenderer(cloudRenderer, cloudRenderer.sharedMaterial);
            /*
             * in the shader you can return a struct:
            struct FragmentOutput
            {
                float3 color : SV_Target0;
                float shadow : SV_Target1;
            };
             */

        // everything else can probably just build once since its not passing in different data per frame

        // this copies to screen
        /*compositeCmd.Blit(drawBuffer, BuiltinRenderTextureType.CurrentActive, compositeMaterial);

        // this copies to shadow mask
        shadowCmd.Blit(shadowBuffer, BuiltinRenderTextureType.CurrentActive);

        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, drawCmd);
        // i think render target gets restored here hopefully
        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, compositeCmd);
        light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, shadowCmd);
    }*/

        #endregion

        private static Mesh InvertMesh(Mesh original)
        {
            Mesh mesh = UnityEngine.GameObject.Instantiate(original);

            // Flip normals
            for (int i = 0; i < mesh.normals.Length; i++) mesh.normals[i] = -mesh.normals[i];

            // Flip triangle winding
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);

                for (int j = 0; j < triangles.Length; j += 3)
                {
                    // swap 0 and 1
                    int temp = triangles[j];
                    triangles[j] = triangles[j + 1];
                    triangles[j + 1] = temp;
                }

                mesh.SetTriangles(triangles, i);
            }

            return mesh;
        }

        private static Mesh CreateSphereMesh(int latitudeSegments, int longitudeSegments, float radius)
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int latitude = 0; latitude <= latitudeSegments; latitude++)
            {
                // Calculate the angle corresponding to the current latitude segment
                float latitudeAngle = Mathf.PI * latitude / latitudeSegments;
                float sinLat = Mathf.Sin(latitudeAngle);
                float cosLat = Mathf.Cos(latitudeAngle);

                for (int longitude = 0; longitude <= longitudeSegments; longitude++)
                {
                    // Calculate the angle corresponding to the current longitude segment
                    float longitudeAngle = 2 * Mathf.PI * longitude / longitudeSegments;
                    float sinLongitude = Mathf.Sin(longitudeAngle);
                    float cosLongitude = Mathf.Cos(longitudeAngle);

                    // Convert 3D polar coordinates to Cartesian coordiantes
                    Vector3 pos = new Vector3(sinLat * cosLongitude, cosLat, sinLat * sinLongitude) * radius;

                    // Add the new vertix, compute the normal and UV
                    vertices.Add(pos);
                    normals.Add(pos.normalized);
                    uvs.Add(new Vector2((float)longitude / longitudeSegments, (float)latitude / latitudeSegments));
                }
            }

            // Construct the mesh
            for (int latitude = 0; latitude < latitudeSegments; latitude++)
            {
                for (int longitude = 0; longitude < longitudeSegments; longitude++)
                {
                    int current = latitude * (longitudeSegments + 1) + longitude;
                    int next = current + longitudeSegments + 1;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            return mesh;
        }
    }
}
