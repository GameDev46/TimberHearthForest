using UnityEditor;

public class BuildBundles
{
    [MenuItem("Tools/Build AssetBundles")]
    public static void BuildAll()
    {
        BuildPipeline.BuildAssetBundles(
            "Assets/AssetBundles/Windows",
            BuildAssetBundleOptions.UncompressedAssetBundle,
            BuildTarget.StandaloneWindows64
        );

        BuildPipeline.BuildAssetBundles(
            "Assets/AssetBundles/Linux",
            BuildAssetBundleOptions.UncompressedAssetBundle,
            BuildTarget.StandaloneLinux64
        );

        BuildPipeline.BuildAssetBundles(
            "Assets/AssetBundles/Mac",
            BuildAssetBundleOptions.UncompressedAssetBundle,
            BuildTarget.StandaloneOSX
        );
    }
}
