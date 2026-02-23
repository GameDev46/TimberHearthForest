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
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimberHearthForest
{
    public class TimberHearthForest : ModBehaviour
    {
        public static TimberHearthForest Instance;

        private List<GameObject> spawnedTrees = new List<GameObject>();
        private List<GameObject> spawnedGrass = new List<GameObject>();

        private Sector timberHearthSector;
        private Sector quantumMoonSector;

        private List<string> assetBundles = new List<string>();

        public class PropDetails
        {
            public Vector3 rotation;
            public Vector3 position;
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
            UpdatePropDensity(treeDensityPreset, "tree");

            string grassDensityPreset = config.GetSettingsValue<string>("grassDensity");
            UpdatePropDensity(grassDensityPreset, "grass");
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

            // Wait for scene to load
            yield return new WaitForSeconds(3f);

            // Locate TimberHearth_Body
            GameObject timberHearthBody = GameObject.Find("TimberHearth_Body");

            if (timberHearthBody == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the TimberHearth_Body gameobject", MessageType.Error);
                yield break;
            }

            ModHelper.Console.WriteLine("Located TimberHearth_Body object successfully", MessageType.Success);

            // Locate the tree template gameobject
            const string treeTemplatePath = "QuantumMoon_Body/Sector_QuantumMoon/State_TH/Interactables_THState/Crater_Surface/Surface_AlpineTrees_Single/QAlpine_Tree_.25 (1)/";
            GameObject treeTemplate = GetGameObjectAtPath(treeTemplatePath);

            if (treeTemplate == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the tree template gameobject at: " + treeTemplatePath, MessageType.Error);
                yield break;
            }

            // Load the tree template's asset bundle to prevent the clones from being invisible
            foreach (var handle in treeTemplate.GetComponentsInChildren<StreamingMeshHandle>(true))
            {
                if (!string.IsNullOrEmpty(handle.assetBundle)) assetBundles.Add(handle.assetBundle);
            }

            // Locate the grass template gameobject
            const string grassTemplatePath = "TimberHearth_Body/Sector_TH/Sector_Village/Sector_LowerVillage/DetailPatches_LowerVillage/LandingGeyserVillageArea/Foliage_TH_GrassPatch (10)/";
            GameObject grassTemplate = GetGameObjectAtPath(grassTemplatePath);

            if (grassTemplate == null)
            {
                ModHelper.Console.WriteLine("Couldn't locate the grass template gameobject at: " + grassTemplatePath, MessageType.Error);
                yield break;
            }

            // Load the grass template's asset bundle to prevent the clones from being invisible
            var grassHandle = grassTemplate.GetComponent<StreamingMeshHandle>();
            if (grassHandle) if (!string.IsNullOrEmpty(grassHandle.assetBundle)) assetBundles.Add(grassHandle.assetBundle);

            // Locate the Timber Hearth and Quantum Moon Sector
            timberHearthSector = Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetComponentInChildren<Sector>();
            quantumMoonSector = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).GetComponentInChildren<Sector>();

            timberHearthSector.OnOccupantEnterSector += OnEnterTimberHearth;
            quantumMoonSector.OnOccupantExitSector += OnLeaveQuantumMoon;

            // Load all the asset bundles to prevent the clones from being invisible
            foreach (string bundle in assetBundles) StreamingManager.LoadStreamingAssets(bundle);

            // Used to group tree clones together for a cleaner hierachy
            GameObject treeParent = new GameObject("TH_Trees_Surface");
            treeParent.transform.SetParent(timberHearthSector.transform, false);
            treeParent.transform.localPosition = Vector3.zero;
            treeParent.transform.localRotation = Quaternion.identity;

            // Used to group grass clones together for a cleaner hierachy
            GameObject grassParent = new GameObject("TH_Grass_Surface");
            grassParent.transform.SetParent(timberHearthSector.transform, false);
            grassParent.transform.localPosition = Vector3.zero;
            grassParent.transform.localRotation = Quaternion.identity;

            foreach (var detail in treeData) {
                // Spawn the tree
                GameObject treeClone = Instantiate(treeTemplate);

                // Parent the tree
                treeClone.transform.SetParent(treeParent.transform, false);

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

                // Parent the tree
                grassClone.transform.SetParent(grassParent.transform, false);

                randomScale = UnityEngine.Random.Range(0.8f, 1.2f);

                // Set position, rotation and scale
                grassClone.transform.position = timberHearthBody.transform.TransformPoint(new Vector3(detail.position.x, detail.position.y, detail.position.z));
                grassClone.transform.localRotation = Quaternion.Euler(detail.rotation.x, detail.rotation.y, detail.rotation.z);
                grassClone.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                spawnedGrass.Add(grassClone);
            }

            ModHelper.Console.WriteLine("All trees and grass tufts have been spawned.", MessageType.Success);

            // Update the tree and grass density
            string treeDensityPreset = ModHelper.Config.GetSettingsValue<string>("treeDensity");
            UpdatePropDensity(treeDensityPreset, "tree");

            string grassDensityPreset = ModHelper.Config.GetSettingsValue<string>("grassDensity");
            UpdatePropDensity(treeDensityPreset, "grass");
        }

        private void OnEnterTimberHearth(SectorDetector detector)
        {
            // Should only load bundles if the player is entering Timber Hearth
            if (!timberHearthSector.ContainsOccupant(DynamicOccupant.Player)) return;
            // Load all the asset bundles
            foreach (string bundle in assetBundles) StreamingManager.LoadStreamingAssets(bundle);
        }

        private void OnLeaveQuantumMoon(SectorDetector detector)
        {
            // Should only load bundles if the player is leaving the quantum moon
            if (quantumMoonSector.ContainsOccupant(DynamicOccupant.Player)) return;
            // Load all the asset bundles
            foreach (string bundle in assetBundles) StreamingManager.LoadStreamingAssets(bundle);
        }

        private void UpdatePropDensity(string densityDescriptor, string spawnType)
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
            // Rest in peace 39097 line JSON file, you will be remembered

            // Prepare the list that will hold the prop details extracted from the JSON
            List<PropDetails> propDetailList = new List<PropDetails>();

            // Split JSON into seperate lines for easier processing
            string[] lines = json.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            PropDetails currentProp = null;

            foreach (string line in lines)
            {
                // Remove any leading or trailing whitespace
                string trimmedLine = line.Trim();

                // If the line doesn't contain both [ and ], it's not a line with position and rotation data, so skip
                if (!trimmedLine.Contains("[") || !trimmedLine.Contains("]")) continue;

                // Extract the position and rotation data
                string[] treeData = trimmedLine.Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);

                // This shouldn't be called, but protects against bad data formatting
                // as treeData should consist of 3 position values and 3 rotation values
                if (treeData.Length != 6) continue;

                currentProp = new PropDetails();

                // Extract the prop position data
                float posX = float.Parse(treeData[0].Trim(), CultureInfo.InvariantCulture);
                float posY = float.Parse(treeData[1].Trim(), CultureInfo.InvariantCulture);
                float posZ = float.Parse(treeData[2].Trim(), CultureInfo.InvariantCulture);

                currentProp.position = new Vector3(posX, posY, posZ);

                // Extract the prop rotation data
                float rotX = float.Parse(treeData[3].Trim(), CultureInfo.InvariantCulture);
                float rotY = float.Parse(treeData[4].Trim(), CultureInfo.InvariantCulture);
                float rotZ = float.Parse(treeData[5].Trim(), CultureInfo.InvariantCulture);

                currentProp.rotation = new Vector3(rotX, rotY, rotZ);

                // Add the new prop to the list
                propDetailList.Add(currentProp);

                // Clear the current prop
                currentProp = null;
            }

            ModHelper.Console.WriteLine($"Parsed {propDetailList.Count} tree details.", MessageType.Success);

            return propDetailList;
        }

    }

}
