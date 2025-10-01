using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Auto-creates AuthScene with AuthBootstrap and ensures it's first in Build Settings
[InitializeOnLoad]
public static class AuthSceneAutoCreator
{
    static AuthSceneAutoCreator()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        EditorApplication.update -= OnEditorUpdate;
        EnsureResourcesConfig();
        EnsureAuthScene();
        EnsureOptionalGameScene();
        EnsureBuildSettingsOrder();
    }

    private static void EnsureResourcesConfig()
    {
        const string resourcesDir = "Assets/Resources";
        const string assetPath = resourcesDir + "/AuthConfig.asset";
        if (!Directory.Exists(resourcesDir)) Directory.CreateDirectory(resourcesDir);
        var cfg = AssetDatabase.LoadAssetAtPath<Content.Back.Authorization.AuthConfig>(assetPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<Content.Back.Authorization.AuthConfig>();
            AssetDatabase.CreateAsset(cfg, assetPath);
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureOptionalGameScene()
    {
        // If a scene named GameScene exists, ensure it's added to Build Settings
        var guids = AssetDatabase.FindAssets("GameScene t:Scene");
        if (guids == null || guids.Length == 0) return;

        string gameScenePath = null;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/GameScene.unity") || path.EndsWith("\\GameScene.unity"))
            {
                gameScenePath = path;
                break;
            }
        }
        if (string.IsNullOrEmpty(gameScenePath)) return;

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == gameScenePath)
            {
                scenes[i].enabled = true;
                found = true;
                break;
            }
        }
        if (!found)
        {
            scenes.Add(new EditorBuildSettingsScene(gameScenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureAuthScene()
    {
        const string scenesDir = "Assets/Scenes";
        const string scenePath = scenesDir + "/AuthScene.unity";

        if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);
        if (!File.Exists(scenePath))
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AuthBootstrap");
            root.AddComponent<Content.Back.Authorization.AuthBootstrap>();
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureBuildSettingsOrder()
    {
        const string sceneAssetPath = "Assets/Scenes/AuthScene.unity";
        var scenes = EditorBuildSettings.scenes;
        bool found = false;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == sceneAssetPath)
            {
                found = true;
                // ensure it's enabled
                scenes[i].enabled = true;
                // move to index 0 if needed
                if (i != 0)
                {
                    var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                    var item = list[i];
                    list.RemoveAt(i);
                    list.Insert(0, item);
                    scenes = list.ToArray();
                }
                break;
            }
        }
        if (!found)
        {
            var newList = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(sceneAssetPath, true)
            };
            scenes = newList.ToArray();
        }
        EditorBuildSettings.scenes = scenes;
    }
}
