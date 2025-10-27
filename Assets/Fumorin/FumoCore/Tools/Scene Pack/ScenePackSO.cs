#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FumoCore.Tools;
#if UNITY_EDITOR
[CustomEditor(typeof(ScenePackSO))]
public class ScenePackEditor : Editor
{
    private SerializedProperty lobbyFoldersProp;
    private SerializedProperty levelFoldersProp;
    private SerializedProperty bossFoldersProp;
    private SerializedProperty shopFoldersProp;
    private SerializedProperty extraFoldersProp;
    private SerializedProperty templateFoldersProp;
    private SerializedProperty scenePairFoldersProp;

    private void OnEnable()
    {
        lobbyFoldersProp = serializedObject.FindProperty("lobbyScenesFolders");
        levelFoldersProp = serializedObject.FindProperty("levelScenesFolders");
        bossFoldersProp = serializedObject.FindProperty("bossSceneFolders");
        shopFoldersProp = serializedObject.FindProperty("shopSceneFolders");
        extraFoldersProp = serializedObject.FindProperty("extraSceneFolders");
        templateFoldersProp = serializedObject.FindProperty("sceneTemplateFolders");
        scenePairFoldersProp = serializedObject.FindProperty("scenePairSOFolders");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Populate Scene Lists from Folders"))
        {
            ((ScenePackSO)target).AutoPopulateSceneLists();
        }

        if (GUILayout.Button("Sync Build Settings From Scene Lists"))
        {
            ((ScenePackSO)target).SyncScenesToBuildSettings();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

[CreateAssetMenu(menuName = "Fumocore/Scene Pack")]
public class ScenePackSO : ScriptableObject
{
    [Header("Scene Categories")]
    [SerializeField] List<SceneReference> LobbyScenes = new();
    [SerializeField] List<SceneReference> LevelScenes = new();
    [SerializeField] List<SceneReference> BossScenes = new();
    [SerializeField] List<SceneReference> ShopScenes = new();
    [SerializeField] List<SceneReference> ExtraScenes = new();
    [SerializeField] List<SceneReference> SceneTemplates = new();

    public IEnumerable<SceneReference> AllScenes =>
            LevelScenes
            .Concat(BossScenes)
            .Concat(ShopScenes)
            .Concat(LobbyScenes)
            .Concat(ExtraScenes);

    public bool Contains(SceneReference scene) => AllScenes.Contains(scene);
    public bool Contains(string sceneName) => AllScenes.Any(sr => sr.GetSceneName() == sceneName);
    public bool IsLobbyScene(SceneReference scene) => LobbyScenes.Contains(scene);
    public bool IsLobbyScene(string sceneName) => LobbyScenes.Any(sr => sr.GetSceneName() == sceneName);

    public static int SceneIndex { get; private set; }
    [Initialize(-100)]
    public static void Restart() => SceneIndex = -1;

#if UNITY_EDITOR
    [Header("Folders for Auto-Populate")]
    public List<DefaultAsset> lobbyScenesFolders = new();
    public List<DefaultAsset> levelScenesFolders = new();
    public List<DefaultAsset> bossSceneFolders = new();
    public List<DefaultAsset> shopSceneFolders = new();
    public List<DefaultAsset> extraSceneFolders = new();
    public List<DefaultAsset> sceneTemplateFolders = new();
    public List<DefaultAsset> permanentSceneFolders = new();

    [Header("Scene Pair SOs (Editor Only)")]
    public List<DefaultAsset> scenePairSOFolders = new();
    public void AutoPopulateSceneLists()
    {
        Undo.RecordObject(this, "Auto Populate Scene Lists");

        LobbyScenes.Clear();
        LevelScenes.Clear();
        BossScenes.Clear();
        ShopScenes.Clear();
        ExtraScenes.Clear();
        SceneTemplates.Clear();

        PopulateListFromFolders(LobbyScenes, lobbyScenesFolders);
        PopulateListFromFolders(LevelScenes, levelScenesFolders);
        PopulateListFromFolders(BossScenes, bossSceneFolders);
        PopulateListFromFolders(ShopScenes, shopSceneFolders);
        PopulateListFromFolders(ExtraScenes, extraSceneFolders);
        PopulateListFromFolders(SceneTemplates, sceneTemplateFolders);

        EditorUtility.SetDirty(this);
        Debug.Log("[ScenePackSO] Scene lists auto-populated from selected folders.");
    }

    public void SyncScenesToBuildSettings()
    {
        var buildScenes = new List<EditorBuildSettingsScene>();
        var addedPaths = new HashSet<string>();

        // Add first lobby scene as starting scene if it exists
        if (LobbyScenes.Count > 0 && LobbyScenes[0].sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(LobbyScenes[0].sceneAsset);
            if (!string.IsNullOrEmpty(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                addedPaths.Add(path);
            }
        }

        // Add all scenes from standard lists
        foreach (var scene in AllScenes)
        {
            if (scene.sceneAsset == null) continue;
            string path = AssetDatabase.GetAssetPath(scene.sceneAsset);
            if (string.IsNullOrEmpty(path) || addedPaths.Contains(path)) continue;
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
            addedPaths.Add(path);
        }
        // Add all scenes referenced in Scene Pair SOs
        foreach (var folder in scenePairSOFolders)
        {
            if (folder == null) continue;
            string folderPath = AssetDatabase.GetAssetPath(folder);
            if (!AssetDatabase.IsValidFolder(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:ScenePairSO", new[] { folderPath });
            foreach (var guid in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<ScenePairSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null || so.Scenes == null) continue;

                foreach (var scene in so.Scenes)
                {
                    if (scene.sceneAsset == null) continue;
                    string path = AssetDatabase.GetAssetPath(scene.sceneAsset);
                    if (string.IsNullOrEmpty(path) || addedPaths.Contains(path)) continue;

                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                    addedPaths.Add(path);
                }
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"[ScenePackSO] Synced {buildScenes.Count} scenes (including ScenePairSO scenes) to Build Settings.");
    }

    private void PopulateListFromFolders(List<SceneReference> targetList, List<DefaultAsset> folders)
    {
        if (folders == null || targetList == null) return;

        var folderPaths = folders
            .Where(f => f != null)
            .Select(AssetDatabase.GetAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .ToArray();

        var scenes = FindScenes(folderPaths);

        foreach (var sr in scenes)
        {
            if (!targetList.Any(x => x.sceneAsset == sr.sceneAsset))
            {
                targetList.Add(sr);
            }
        }
    }

    private List<SceneReference> FindScenes(string[] folders)
    {
        var guids = AssetDatabase.FindAssets("t:Scene", folders);
        var sceneRefs = new List<SceneReference>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".unity")) continue;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null) continue;

            if (!sceneRefs.Any(sr => sr.sceneAsset == sceneAsset))
            {
                sceneRefs.Add(new SceneReference { sceneAsset = sceneAsset });
            }
        }

        return sceneRefs;
    }
#endif
}
