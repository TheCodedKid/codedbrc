using CrewBoomMono;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CustomCharacterBundleBuilder
{
    public const string BUNDLE_OUTPUT_FOLDER = "CharacterBundles";

    public static void BuildBundle(GameObject prefab)
    {
        if (!EditorUtility.IsPersistent(prefab))
        {
            EditorUtility.DisplayDialog("Custom Character Bundle Builder", "A custom character can only be built from the prefab it originates from, but the prefab was not on disk.", "OK");
            return;
        }

        CharacterDefinition definition = prefab.GetComponent<CharacterDefinition>();
        if (definition == null)
        {
            EditorUtility.DisplayDialog("Custom Character Bundle Builder", $"{AssetDatabase.GetAssetPath(prefab)} is not a CharacterDefinition prefab.\nTry re-opening your character prefab to re-calibrate.", "OK");
            return;
        }

        string id = Guid.NewGuid().ToString();
        definition.Id = id;
        AssetDatabase.SaveAssets();

        List<AssetBundleBuild> assetBundleDefinitionList = new();

        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = $"{definition.CharacterName}_{definition.CharacterToReplace}";

        string pathToAsset = AssetDatabase.GetAssetPath(prefab);
        string fileName = Path.GetFileName(pathToAsset);
        string folderPath = pathToAsset.Remove(pathToAsset.Length - fileName.Length);
        build.assetNames = RecursiveGetAllAssetsInDirectory(folderPath).ToArray();

        assetBundleDefinitionList.Add(build);

        if (!Directory.Exists(BUNDLE_OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(BUNDLE_OUTPUT_FOLDER);
        }
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(BUNDLE_OUTPUT_FOLDER, assetBundleDefinitionList.ToArray(), BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);

        if (manifest != null)
        {
            foreach (var bundleName in manifest.GetAllAssetBundles())
            {
                string projectRelativePath = BUNDLE_OUTPUT_FOLDER + "/" + bundleName;
                Debug.Log($"Size of AssetBundle {projectRelativePath} is {new FileInfo(projectRelativePath).Length * 0.0009765625} KB");

                Debug.Log($"Built character bundle for {definition.CharacterName} with GUID {id}");
                EditorUtility.RevealInFinder(projectRelativePath);
            }
        }
        else
        {
            Debug.Log("Build failed, see Console and Editor log for details");
        }
    }

    public static List<string> RecursiveGetAllAssetsInDirectory(string path)
    {
        List<string> assets = new();
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) != ".meta" &&
                Path.GetExtension(file) != ".cs" &&  // Scripts are not supported in AssetBundles
                Path.GetExtension(file) != ".unity") // Scenes cannot be mixed with other file types in a bundle
            {
                assets.Add(file);
            }
        }
        return assets;
    }
}