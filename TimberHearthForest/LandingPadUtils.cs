using OWML.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TimberHearthForest
{
    internal class LandingPadUtils
    {
        private static string modFolderPath = "";
        private static IModConsole modConsole;

        private static AssetBundle landingPadBundle;
        private static GameObject landingPadPrefab;

        private static GameObject landingPadInstance;

        public static void SetModDirectoryPath(string dirPath)
        {
            modFolderPath = dirPath;
        }

        public static void SetModConsole(IModConsole console)
        {
            modConsole = console;
        }

        public static void LoadLandingPadAssetBundle()
        {
            try
            {
                if (landingPadPrefab == null)
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

                    string bundlePath = Path.Combine(modFolderPath, "Assets", platformFolder, "landingpad");
                    landingPadBundle = AssetBundle.LoadFromFile(bundlePath);

                    if (landingPadBundle == null)
                    {
                        modConsole.WriteLine("Failed to load landing pad AssetBundle", MessageType.Error);
                        return;
                    }

                    landingPadPrefab = landingPadBundle.LoadAsset<GameObject>("Landing Pad");
                }
            }
            catch (Exception e)
            {
                modConsole.WriteLine($"Failed to load the landing pad asset bundle from Assets/landingpad: {e}", MessageType.Error);
            }
        }

        public static void SpawnLandingPad(Transform planetSector)
        {
            landingPadInstance = GameObject.Instantiate(landingPadPrefab, planetSector);
            landingPadInstance.transform.localPosition = new Vector3(-119.0f, 85.0f, -248.3f);
            landingPadInstance .transform.localRotation = Quaternion.Euler(15.0f, 294.0f, 74.0f);
            landingPadInstance.transform.localScale = Vector3.one;
        }

        public static void ToggleLandingPadVisibility(bool isVisible)
        {
            if (landingPadInstance != null) landingPadInstance.SetActive(isVisible);
        }
    }
}
