using OWML.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace TimberHearthForest
{
    public class BirdSongUtils : MonoBehaviour
    {
        private static string modFolderPath = "";
        private static IModConsole modConsole;

        private static AssetBundle birdSongBundle;
        private static GameObject birdSongManagerPrefab;

        private static float birdSongVolume = 0.8f;

        private static List<AudioSource> birdAudioSources = new List<AudioSource>();

        private AudioSource birdAudio1;
        private AudioSource birdAudio2;
        private AudioSource birdAudio3;

        private bool isPlayingBirdSong = false;

        public static void SetModDirectoryPath(string dirPath)
        {
            modFolderPath = dirPath;
        }

        public static void SetModConsole(IModConsole console)
        {
            modConsole = console;
        }

        public static void LoadBirdSongAssets()
        {
            // Clear the list of bird audio sources
            birdAudioSources = new List<AudioSource>();

            try
            {
                if (birdSongManagerPrefab == null)
                {
                    string bundlePath = Path.Combine(modFolderPath, "Assets", "Windows", "birdsound");
                    birdSongBundle = AssetBundle.LoadFromFile(bundlePath);

                    if (birdSongBundle == null)
                    {
                        modConsole.WriteLine("Failed to load bird song AssetBundle", MessageType.Error);
                        return;
                    }

                    birdSongManagerPrefab = birdSongBundle.LoadAsset<GameObject>("Bird Song Manager");
                }
            }
            catch (Exception e)
            {
                modConsole.WriteLine($"Failed to load the bird song asset bundle from Assets/birdsound: {e}", MessageType.Error);
            }
        }

        public static void AddBirdSongAudioSource(Transform parent)
        {
            GameObject birdSongManagerInstance = GameObject.Instantiate(birdSongManagerPrefab, parent);
            birdSongManagerInstance.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            birdSongManagerInstance.transform.localRotation = Quaternion.identity;
            birdSongManagerInstance.transform.localScale = Vector3.one;

            birdSongManagerInstance.AddComponent<BirdSongUtils>();

            AudioSource birdNoise1 = birdSongManagerInstance.transform.GetChild(0)?.GetComponent<AudioSource>();
            if (birdNoise1 != null)
            {
                birdNoise1.volume = birdSongVolume;
                birdNoise1.loop = false;
                birdNoise1.playOnAwake = false;
                birdNoise1.Stop();

                birdAudioSources.Add(birdNoise1);
            }

            AudioSource birdNoise2 = birdSongManagerInstance.transform.GetChild(1)?.GetComponent<AudioSource>();
            if (birdNoise2 != null)
            {
                birdNoise2.volume = birdSongVolume;
                birdNoise2.loop = false;
                birdNoise2.playOnAwake = false;
                birdNoise2.Stop();

                birdAudioSources.Add(birdNoise2);
            }

            AudioSource birdNoise3 = birdSongManagerInstance.transform.GetChild(2)?.GetComponent<AudioSource>();
            if (birdNoise3 != null)
            {
                birdNoise3.volume = birdSongVolume;
                birdNoise3.loop = false;
                birdNoise3.playOnAwake = false;
                birdNoise3.Stop();

                birdAudioSources.Add(birdNoise3);
            }
        }

        public static void SetBirdSongVolume(float volume)
        {
            birdSongVolume = volume;

            foreach (AudioSource audioSource in birdAudioSources)
            {
                audioSource.volume = volume;
            }
        }

        private void Start()
        {
            birdAudio1 = transform.GetChild(0)?.GetComponent<AudioSource>();
            birdAudio2 = transform.GetChild(1)?.GetComponent<AudioSource>();
            birdAudio3 = transform.GetChild(2)?.GetComponent<AudioSource>();

            isPlayingBirdSong = false;
        }

        private void Update()
        {
            if (!isPlayingBirdSong)
            {
                isPlayingBirdSong = true;

                int randomIndex = UnityEngine.Random.Range(0, 5);
                switch (randomIndex)
                {
                    case 0: case 3: StartCoroutine(PlayBirdSong(birdAudio1)); break;
                    case 1: case 4: StartCoroutine(PlayBirdSong(birdAudio2)); break;
                    case 2: StartCoroutine(PlayBirdSong(birdAudio3)); break; // Noisy and memorable, so appears less often
                    default: StartCoroutine(PlayBirdSong(birdAudio1)); break;
                }
            }
        }

        private IEnumerator PlayBirdSong(AudioSource birdSong)
        {
            StopAllBirdAudio();

            birdSong.Play();

            float clipDuration = birdSong.clip.length;
            yield return new WaitForSeconds(clipDuration);

            StopAllBirdAudio();

            float restInterval = UnityEngine.Random.Range(1.0f, 10.0f);
            yield return new WaitForSeconds(restInterval);

            isPlayingBirdSong = false;
        }

        private void StopAllBirdAudio()
        {
            if (birdAudio1 != null) birdAudio1.Stop();
            if (birdAudio2 != null) birdAudio2.Stop();
            if (birdAudio3 != null) birdAudio3.Stop();

            if (birdAudio1 != null) birdAudio1.time = 0.0f;
            if (birdAudio2 != null) birdAudio2.time = 0.0f;
            if (birdAudio3 != null) birdAudio3.time = 0.0f;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isPlayingBirdSong = false;
        }
    }
}
