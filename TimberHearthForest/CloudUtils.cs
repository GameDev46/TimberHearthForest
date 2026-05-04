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
                }
            }
            catch (Exception e)
            {
                modConsole.WriteLine($"Failed to load the volumetric cloud asset bundle from Assets/volumetricclouds: {e}", MessageType.Error);
            }
        }

        public static void CreateVolumetricCloud(GameObject cloudHolder, float cloudInnerRadius, float cloudOuterRadius, ref List<GameObject> volumetricClouds)
        {

            if (volumetricCloudMaterial == null)
            {
                modConsole.WriteLine($"Failed to locate the volumetric cloud material from Assets/volumetricclouds", MessageType.Error);
                return;
            }

            Material mat = volumetricCloudMaterial;

            mat.SetFloat("_ErosionStrength", 0.3f);
            mat.SetFloat("_BlueNoiseStrength", 0.3f); // Dithering power

            mat.SetFloat("_OuterRadius", cloudOuterRadius);
            mat.SetFloat("_InnerRadius", cloudInnerRadius);
            mat.SetFloat("_PlanetRadius", 254); // th radius
            mat.SetFloat("_MoonRadius", 80);

            mat.SetInt("_NumSteps", 35);
            mat.SetInt("_NumSunSteps", 12);
            mat.SetFloat("_MinStepSize", 0.5f);

            mat.SetFloat("_CloudScale", 1.5f);
            mat.SetFloat("_DensityMultiplier", 1.0f);
            mat.SetFloat("_DensityThreshold", 0.65f);

            mat.SetFloat("_LightAbsorptionThroughCloud", 0.8f);
            mat.SetFloat("_LightAbsorptionTowardsSun", 0.4f);
            // mat.SetFloat("_DarknessThreshold", 0.2f);
            mat.SetFloat("_PhaseG", 0.5f);
            mat.SetFloat("_PhaseIntensity", 4.0f);
            mat.SetFloat("_ForwardScatteringBias", 0.2f);

            mat.SetVector("_SunDirection", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            mat.SetColor("_SunColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));

            // ambient texture set in bundle
            mat.SetFloat("_AmbientStrength", 0.2f);
            // mat.SetFloat("_AmbientMixFactor", 0.0f);

            // mat.SetFloat("_PlanetShadowStrength", 1.0f);
            // mat.SetFloat("_PlanetShadowSharpness", 1.0f);

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

            // Store the volumetric cloud sphere gameobject
            volumetricClouds.Add(cloudSphere);
        }

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
