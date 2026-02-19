using Epic.OnlineServices;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimberHearthForest
{
    public class TimberHearthForest : ModBehaviour
    {
        public static TimberHearthForest Instance;

        private GameObject timberHearthBody;
        private Sector timberHearthSector;

        private List<GameObject> spawnedTrees = new List<GameObject>();
        private List<GameObject> spawnedGrass = new List<GameObject>();

        [System.Serializable]
        public class PropDetails
        {
            public string path;
            public ModelRotation rotation;
            public bool alignRadial;
            public ModelPosition position;
        }

        [System.Serializable]
        public class ModelRotation
        {
            public float x;
            public float y;
            public float z;
        }

        [System.Serializable]
        public class ModelPosition
        {
            public float x;
            public float y;
            public float z;
        }

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"{nameof(TimberHearthForest)} is loaded!", MessageType.Success);

            new Harmony("GameDev46.TimberHearthForest").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            if (newScene != OWScene.SolarSystem) return;
            //ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

            // Loaded into Solar System!

            // Load tree data and spawn trees
            string treeSpawnDataPath = ModHelper.Manifest.ModFolderPath + "Assets/treeSpawnData.json";
            LoadAndSpawnProps(treeSpawnDataPath);
        }

        /// Called by OWML; once at the start and upon each config setting change.
        public override void Configure(IModConfig config)
        {
            string treeDensityPreset = config.GetSettingsValue<string>("treeDensity");
            UpdateDensity(treeDensityPreset, "tree");

            string grassDensityPreset = config.GetSettingsValue<string>("grassDensity");
            UpdateDensity(grassDensityPreset, "grass");
        }

        private void LoadAndSpawnProps(string jsonFilePath)
        {

            if (!System.IO.File.Exists(jsonFilePath))
            {
                ModHelper.Console.WriteLine($"Couldn't find {jsonFilePath}", MessageType.Error);
                return;
            }

            string json = System.IO.File.ReadAllText(jsonFilePath);
            List<PropDetails> spawnData = ParseJson(json);

            if (spawnData == null)
            {
                ModHelper.Console.WriteLine("Loaded data is null.", MessageType.Error);
                return;
            }

            StartCoroutine(SpawnTrees(spawnData));
        }

        IEnumerator SpawnTrees(List<PropDetails> treeData)
        {
            // Clear the stored trees and grass tufts
            spawnedTrees = new List<GameObject>();
            spawnedGrass = new List<GameObject>();

            yield return new WaitForSeconds(3f); // Wait a moment to ensure the scene is fully loaded

            // Locate TimberHearth_Body
            timberHearthBody = GameObject.Find("TimberHearth_Body");

            if (timberHearthBody == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the TimberHearth_Body gameobject", MessageType.Error);
                yield break;
            }

            ModHelper.Console.WriteLine("Located TimberHearth_Body object successfully", MessageType.Success);

            // Get the sector component from TimberHearth_Body
            timberHearthSector = timberHearthBody.GetComponentInChildren<Sector>();

            // Locate the tree template gameobject
            const string treeTemplatePath = "QuantumMoon_Body/Sector_QuantumMoon/State_TH/Interactables_THState/Crater_Surface/Surface_AlpineTrees_Single/QAlpine_Tree_.25 (1)/";
            GameObject treeTemplate = GetGameObjectAtPath(treeTemplatePath);

            if (treeTemplate == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the tree template gameobject at: " + treeTemplatePath, MessageType.Error);
                yield break;
            }

            foreach (var handle in treeTemplate.GetComponentsInChildren<StreamingMeshHandle>(true))
            {
                if (!string.IsNullOrEmpty(handle.assetBundle)) StreamingManager.LoadStreamingAssets(handle.assetBundle);
            }

            // Locate the grass template gameobject
            const string grassTemplatePath = "TimberHearth_Body/Sector_TH/Sector_Village/Sector_LowerVillage/DetailPatches_LowerVillage/LandingGeyserVillageArea/Foliage_TH_GrassPatch (10)/";
            GameObject grassTemplate = GetGameObjectAtPath(grassTemplatePath);

            if (grassTemplate == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the grass template gameobject at: " + grassTemplatePath, MessageType.Error);
                yield break;
            }

            var grassHandle = grassTemplate.GetComponent<StreamingMeshHandle>();
            if (grassHandle) if (!string.IsNullOrEmpty(grassHandle.assetBundle)) StreamingManager.LoadStreamingAssets(grassHandle.assetBundle);

            // Locate the tall grass template gameobject
            /*const string tallGrassTemplatePath = "TimberHearth_Body/Sector_TH/Sector_NomaiCrater/DetailPatches_NomaiCrater/NomaiCrater Foliage/Foliage_TH_NomaiCrater_GrassLow (1)/";
            GameObject tallGrassTemplate = GetGameObjectAtPath(tallGrassTemplatePath);

            if (tallGrassTemplate == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the tall grass template gameobject at: " + grassTemplatePath, MessageType.Error);
                yield break;
            }

            var tallGrassHandle = tallGrassTemplate.GetComponent<StreamingMeshHandle>();
            if (tallGrassHandle) if (!string.IsNullOrEmpty(tallGrassHandle.assetBundle)) StreamingManager.LoadStreamingAssets(tallGrassHandle.assetBundle);*/

            foreach (var detail in treeData) {
                // Spawn the tree
                GameObject treeClone = Instantiate(treeTemplate);

                treeClone.transform.position = timberHearthSector.transform.position;
                treeClone.transform.rotation = Quaternion.identity;

                // Parent the tree
                treeClone.transform.SetParent(timberHearthSector.transform, false);

                // Remove quantum components to prevent weird interactions with the tree clones
                StripQuantumComponents(treeClone);

                // Add some random rotation to make the trees look more natural
                Vector3 randOffsets = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );

                float randomScale = UnityEngine.Random.Range(0.7f, 1.4f);

                // Set position, rotation and scale
                treeClone.transform.position = timberHearthBody.transform.TransformPoint(new Vector3(detail.position.x, detail.position.y, detail.position.z));
                treeClone.transform.localRotation = Quaternion.Euler(detail.rotation.x + randOffsets.x, detail.rotation.y + randOffsets.y, detail.rotation.z + randOffsets.z);
                treeClone.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                foreach (var tracker in treeClone.GetComponentsInChildren<ShapeVisibilityTracker>(true))
                {
                    DestroyImmediate(tracker);
                }

                spawnedTrees.Add(treeClone);

                /* ------------ */
                /* GRASS TUFTS */
                /* ------------*/

                // Spawn the grass tuft
                GameObject grassClone = Instantiate(grassTemplate);

                grassClone.transform.position = timberHearthSector.transform.position;
                grassClone.transform.rotation = Quaternion.identity;

                // Parent the tree
                grassClone.transform.SetParent(timberHearthSector.transform, false);

                randomScale = UnityEngine.Random.Range(0.8f, 1.2f);

                // Set position, rotation and scale
                grassClone.transform.position = timberHearthBody.transform.TransformPoint(new Vector3(detail.position.x, detail.position.y, detail.position.z));
                grassClone.transform.localRotation = Quaternion.Euler(detail.rotation.x, detail.rotation.y, detail.rotation.z);
                grassClone.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                spawnedGrass.Add(grassClone);

                /* -----------*/
                /* TALL GRASS */
                /* -----------*/

                // Spawn the grass tuft
                /*GameObject tallGrassClone = Instantiate(tallGrassTemplate);

                tallGrassClone.transform.position = timberHearthSector.transform.position;
                tallGrassClone.transform.rotation = Quaternion.identity;

                // Parent the tree
                tallGrassClone.transform.SetParent(timberHearthSector.transform, false);

                randomScale = UnityEngine.Random.Range(0.8f, 1.2f);

                // Set position, rotation and scale
                tallGrassClone.transform.position = timberHearthBody.transform.TransformPoint(new Vector3(detail.position.x, detail.position.y, detail.position.z));
                tallGrassClone.transform.localRotation = Quaternion.Euler(detail.rotation.x, detail.rotation.y, detail.rotation.z);
                tallGrassClone.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                spawnedGrass.Add(tallGrassClone);*/
            }

            ModHelper.Console.WriteLine("All trees and grass tufts have been spawned.", MessageType.Success);

            string treeDensityPreset = ModHelper.Config.GetSettingsValue<string>("treeDensity");
            UpdateDensity(treeDensityPreset, "tree");

            string grassDensityPreset = ModHelper.Config.GetSettingsValue<string>("grassDensity");
            UpdateDensity(treeDensityPreset, "grass");
        }

        private void UpdateDensity(string densityDescriptor, string spawnType)
        {
            if (spawnedTrees == null && spawnType == "tree")
            {
                ModHelper.Console.WriteLine("spawnedTrees not initialized yet.", MessageType.Warning);
                return;
            }

            if (spawnedGrass == null && spawnType == "grass")
            {
                ModHelper.Console.WriteLine("spawnedGrass not initialized yet.", MessageType.Warning);
                return;
            }

            int density = 0;

            switch (densityDescriptor)
            {
                case "Hidden":
                    if (spawnType == "tree") density = spawnedTrees.Count * 2;
                    else density = spawnedGrass.Count * 2;
                    break;
                case "Low":
                    density = 4;
                    break;
                case "Medium":
                    density = 3;
                    break;
                case "High":
                    density = 2;
                    break;
                case "Ultra":
                    density = 1;
                    break;
                default:
                    ModHelper.Console.WriteLine($"Unknown {spawnType} density setting: {density}", MessageType.Error);
                    return;
            }

            int spawnTicker = 0;

            if (spawnType == "tree")
            {
                for (int i = 0; i < spawnedTrees.Count; i++)
                {
                    if (spawnTicker >= density)
                    {
                        spawnedTrees[i].SetActive(true);
                        spawnTicker = 0;
                    }
                    else
                    {
                        spawnedTrees[i].SetActive(false);
                    }

                    spawnTicker++;
                }
            } else
            {
                for (int i = 0; i < spawnedGrass.Count; i++)
                {
                    if (spawnTicker >= density)
                    {
                        spawnedGrass[i].SetActive(true);
                        spawnTicker = 0;
                    }
                    else
                    {
                        spawnedGrass[i].SetActive(false);
                    }

                    spawnTicker++;
                }
            }

            ModHelper.Console.WriteLine($"Sucessfully updated Timber Hearth {spawnType} detail mode", MessageType.Success);
        }

        private void StripQuantumComponents(GameObject obj)
        {
            foreach (var q in obj.GetComponentsInChildren<QuantumObject>(true)) Destroy(q);

            foreach (var q in obj.GetComponentsInChildren<SocketedQuantumObject>(true)) Destroy(q);

            foreach (var v in obj.GetComponentsInChildren<VisibilityObject>(true)) Destroy(v);

            foreach (var s in obj.GetComponentsInChildren<ShapeVisibilityTracker>(true)) Destroy(s);
        }

        public GameObject GetGameObjectAtPath(string path)
        {
            string[] step_names = path.Split('/');

            GameObject go = GameObject.Find(step_names[0]);

            if (go == null)
            {
                ModHelper.Console.WriteLine($"Couldn't find object at path: {path}, failed to locate {step_names[0]}", MessageType.Error);
                return null;
            }

            for (int i = 1; i < step_names.Length - 1; i++) {
                Transform next_step = go.transform.Find(step_names[i]);

                if (next_step == null) {
                    ModHelper.Console.WriteLine($"Couldn't find object at path: {path}, failed to locate {step_names[i]}", MessageType.Error);
                    return null;
                }

                go = next_step.gameObject;
            }

            return go;
        }

        private List<PropDetails> ParseJson(string json)
        {
            // Prepare data holders
            List<PropDetails> PropDetailsList = new List<PropDetails>();

            // Split JSON into lines
            string[] lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            PropDetails currentDetail = null;
            string collectionMode = "";

            foreach (string line in lines)
            {
                
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("\"path\""))
                {
                    // Extract the path value
                    string path = ExtractValue(trimmedLine);
                    currentDetail = new PropDetails();
                    currentDetail.path = path;
                }

                if (trimmedLine.Contains("rotation")) collectionMode = "rotation";
                if (trimmedLine.Contains("position")) collectionMode = "position";

                if (trimmedLine.StartsWith("\"x\""))
                {
                    // Extract rotation.x
                    float x = float.Parse(ExtractValue(trimmedLine));

                    if (collectionMode == "rotation")
                    {
                        currentDetail.rotation = new ModelRotation();
                        currentDetail.rotation.x = x;
                    }
                    else if (collectionMode == "position")
                    {
                        currentDetail.position = new ModelPosition();
                        currentDetail.position.x = x;
                    }
                }
                else if (trimmedLine.StartsWith("\"y\""))
                {
                    // Extract rotation.y
                    float y = float.Parse(ExtractValue(trimmedLine));

                    if (collectionMode == "rotation")
                    {
                        currentDetail.rotation.y = y;
                    }
                    else if (collectionMode == "position")
                    {
                        currentDetail.position.y = y;
                    }
                }
                else if (trimmedLine.StartsWith("\"z\""))
                {
                    // Extract rotation.z
                    float z = float.Parse(ExtractValue(trimmedLine));

                    if (collectionMode == "rotation")
                    {
                        currentDetail.rotation.z = z;
                    }
                    else if (collectionMode == "position")
                    {
                        currentDetail.position.z = z;
                    }
                }

                if (trimmedLine.StartsWith("\"alignRadial\""))
                {
                    // Extract alignRadial
                    bool alignRadial = bool.Parse(ExtractValue(trimmedLine));
                    currentDetail.alignRadial = alignRadial;
                }
                
                if (trimmedLine.Contains("}"))
                {
                    // End of one detail
                    if (currentDetail != null && collectionMode == null)
                    {
                        PropDetailsList.Add(currentDetail);
                        currentDetail = null;
                    }

                    collectionMode = null;
                }
            }

            // Final add if there's leftover detail
            if (currentDetail != null)
            {
                PropDetailsList.Add(currentDetail);
            }

            ModHelper.Console.WriteLine($"Parsed {PropDetailsList.Count} tree details.", MessageType.Success);

            return PropDetailsList;
        }

        private string ExtractValue(string line)
        {
            // Extract the value between quotes or after the colon
            int colonIndex = line.IndexOf(':');
            string value = line.Substring(colonIndex + 1).Trim();

            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2); // Remove quotes
            }

            return value.Trim(',', '}'); // Remove trailing comma or closing bracket
        }

    }

}
