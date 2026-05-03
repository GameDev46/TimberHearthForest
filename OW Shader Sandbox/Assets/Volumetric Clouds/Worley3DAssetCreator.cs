#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class Worley3DAssetCreator
{
    [MenuItem("Tools/Generate Worley 3D Texture")]
    public static void Generate()
    {
        Texture3D tex = Worley3DGenerator.Generate(
            size: 128,
            seed: 1234
        );

        AssetDatabase.CreateAsset(tex, "Assets/Worley3D.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("3D Worley texture generated!");
    }
}
#endif