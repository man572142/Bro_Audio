using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ami.BroAudio;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor;
using Ami.BroAudio.Tools;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class AssetTypeUsageFinder : EditorWindow
{
    [MenuItem(BroName.MenuItem_BroAudio + "Others/Find SoundID Usage")]
    public static void ShowWindow()
    {
        GetWindow<AssetTypeUsageFinder>("Type Usage Finder");
    }
    
    private const string RootPath = "Assets";

    private string _targetTypeName = nameof(SoundID);
    private string _targetPath = RootPath;
    private bool _findInPrefab = false;
    private bool _findInScriptableObject = false;
    private bool _findInScene = false;
    private Vector2 _scrollPosition;
    private readonly List<UsageResult> _results = new List<UsageResult>();
    
    private Dictionary<int, IEntityIdentity> _broAudioEntities = new Dictionary<int, IEntityIdentity>();

    private class UsageResult
    {
        public string AssetPath;
        public string GameObjectPath;
        public string ComponentType;
        public string FieldName;
        public string Value;
        public UnityEngine.Object Asset;
        public GameObject GameObject;
    }

    private void OnEnable()
    {
        if (BroEditorUtility.TryGetCoreData(out var data))
        {
            foreach (var asset in data.Assets)
            {
                if (asset == null)
                    continue;

                foreach(var identity in asset.GetAllAudioEntities())
                {
                    if (!identity.Validate())
                        continue;

                    if (!_broAudioEntities.ContainsKey(identity.ID))
                    {
                        _broAudioEntities.Add(identity.ID, identity);
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Asset Type Usage Finder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _targetTypeName = EditorGUILayout.TextField("Target Type Name:", _targetTypeName);
        
        bool isValidPath = _targetPath.StartsWith(RootPath);
        if (!isValidPath)
        {
            EditorGUILayout.HelpBox("The path must be under the Assets folder or its subfolders", MessageType.Error);
        }
        DrawPathTextField();

        _findInPrefab = EditorGUILayout.Toggle("Find in Prefabs", _findInPrefab);
        _findInScriptableObject = EditorGUILayout.Toggle("Find in Scriptable Objects", _findInScriptableObject);
        _findInScene = EditorGUILayout.Toggle("Find in Scenes", _findInScene);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!isValidPath);
        if (GUILayout.Button("Find Usage"))
        {
            FindTypeUsage();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        
        if (_results.Count > 0)
        {
            GUILayout.Label($"Found {_results.Count} usage(s):", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            foreach (var result in _results)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(result.AssetPath, EditorStyles.linkLabel))
                {
                    if (result.GameObject != null)
                    {
                        Selection.activeGameObject = result.GameObject;
                        EditorGUIUtility.PingObject(result.GameObject);
                    }
                    else if (result.Asset != null)
                    {
                        Selection.activeObject = result.Asset;
                        EditorGUIUtility.PingObject(result.GameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"  GameObject: {result.GameObjectPath}");
                EditorGUILayout.LabelField($"  Component: {result.ComponentType}");
                EditorGUILayout.LabelField($"  Field: {result.FieldName}");
                EditorGUILayout.LabelField($"  Value: {result.Value}");
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawPathTextField()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _targetPath = EditorGUILayout.TextField("Target Path:", _targetPath);
            if (GUILayout.Button("...", GUILayout.Width(30f)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Path", _targetPath, "");
                
                if (path.Contains(Application.dataPath))
                {
                    _targetPath = path.Remove(0, Application.dataPath.Length - RootPath.Length);;
                }
                else
                {
                    Debug.LogError("The path must be under the Assets folder or its subfolders");
                }
            }
        }
    }

    private void FindTypeUsage()
    {
        _results.Clear();

        // Find the target type
        Type targetType = FindTypeByName(_targetTypeName);
        if (targetType == null)
        {
            Debug.LogError($"Could not find type: {_targetTypeName}");
            return;
        }

        var allAssetPaths = AssetDatabase.GetAllAssetPaths().Where(x => x.StartsWith(_targetPath));
        var prefabPaths = _findInPrefab ? allAssetPaths.Where(x => x.EndsWith(".prefab")).ToArray() : Array.Empty<string>();
        var scenePaths = _findInScene ? allAssetPaths.Where(x => x.EndsWith(".unity")).ToArray() : Array.Empty<string>();
        var scriptableObjectPaths = _findInScriptableObject ? allAssetPaths.Where(x => x.EndsWith(".asset")).ToArray() : Array.Empty<string>();
        
        float totalAssets = prefabPaths.Length + scenePaths.Length + scriptableObjectPaths.Length;
        int currentAsset = 0;
        
        try
        {
            foreach (var path in prefabPaths)
            {
                currentAsset++;
                if (DisplayCancelableProgressBar(path, currentAsset, totalAssets))
                {
                    break;
                }

                CheckPrefab(path, targetType);
            }
            
            foreach (var path in scenePaths)
            {
                currentAsset++;
                if (DisplayCancelableProgressBar(path, currentAsset, totalAssets))
                {
                    break;
                }
                CheckScene(path, targetType);
            }
            
            foreach (var path in scriptableObjectPaths)
            {
                currentAsset++;
                if (DisplayCancelableProgressBar(path, currentAsset, totalAssets))
                {
                    break;
                }
                CheckScriptableObject(path, targetType);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Search completed. Found {_results.Count} usage(s) of {_targetTypeName}");
        Repaint();
    }

    private static bool DisplayCancelableProgressBar(string path, int currentAsset, float totalAssets)
    {
        if (EditorUtility.DisplayCancelableProgressBar(
                "Searching for Type Usage", 
                $"Checking: {path}", 
                currentAsset / totalAssets))
        {
            return true;
        }

        return false;
    }

    private Type FindTypeByName(string typeName)
    {
        // Search in all loaded assemblies
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.Name == typeName || type.FullName == typeName)
                    {
                        return type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Some assemblies might not be fully loaded, skip them
                continue;
            }
        }
        return null;
    }

    private void CheckPrefab(string assetPath, Type targetType)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            CheckGameObjectHierarchy(prefab, assetPath, targetType);
        }
    }

    private void CheckScene(string assetPath, Type targetType)
    {
        // Save current scene state
        var currentScene = EditorSceneManager.GetActiveScene();
        bool wasCurrentSceneDirty = currentScene.isDirty;

        try
        {
            // Load the scene additively to avoid disrupting the current scene
            Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
            
            if (scene.IsValid())
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject rootObject in rootObjects)
                {
                    CheckGameObjectHierarchy(rootObject, assetPath, targetType);
                }

                // Close the scene
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not check scene {assetPath}: {e.Message}");
        }

        // Restore scene dirty state
        if (!wasCurrentSceneDirty && currentScene.isDirty)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
        }
    }

    private void CheckScriptableObject(string assetPath, Type targetType)
    {
        ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (scriptableObject != null)
        {
            CheckObjectFields(scriptableObject, assetPath, "", scriptableObject.GetType().Name, targetType);
        }
    }

    private void CheckGameObjectHierarchy(GameObject gameObject, string assetPath, Type targetType)
    {
        CheckGameObject(gameObject, assetPath, targetType);
        
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            CheckGameObjectHierarchy(gameObject.transform.GetChild(i).gameObject, assetPath, targetType);
        }
    }

    private void CheckGameObject(GameObject gameObject, string assetPath, Type targetType)
    {
        Component[] components = gameObject.GetComponents<Component>();
        
        foreach (Component component in components)
        {
            if (component == null) continue;
            
            CheckObjectFields(component, assetPath, GetGameObjectPath(gameObject), component.GetType().Name, targetType);
        }
    }

    private void CheckObjectFields(object obj, string assetPath, string gameObjectPath, string componentType, Type targetType)
    {
        if (obj == null) return;

        Type objectType = obj.GetType();
        FieldInfo[] fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            if(field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null) continue;
            
            if (DoesFieldUseType(field, targetType))
            {
                _results.Add(new UsageResult
                {
                    AssetPath = assetPath,
                    GameObjectPath = gameObjectPath,
                    ComponentType = componentType,
                    FieldName = field.Name,
                    Value = GetValueString(field.GetValue(obj)),
                    Asset = obj as UnityEngine.Object,
                    GameObject = obj is Component comp ? comp.gameObject : null
                });
            }
        }
    }

    private bool DoesFieldUseType(FieldInfo field, Type targetType)
    {
        return DoesFieldUseType(field, targetType, new HashSet<Type>());
    }

    private bool DoesFieldUseType(FieldInfo field, Type targetType, HashSet<Type> visitedTypes)
    {
        Type fieldType = field.FieldType;

        // Direct type match
        if (fieldType == targetType)
            return true;

        // Array type
        if (fieldType.IsArray && fieldType.GetElementType() == targetType)
            return true;
        
        if (fieldType.IsGenericType)
        {
            Type[] genericArgs = fieldType.GetGenericArguments();
            foreach (Type genericArg in genericArgs)
            {
                if (genericArg == targetType)
                    return true;
            }
        }
        return false;
    }
    
    private string GetValueString(object value)
    {
        if (value == null)
        {
            return "null";
        }

        if (_targetTypeName == nameof(SoundID) && value is SoundID id && _broAudioEntities.TryGetValue(id, out var entity))
        {
            return $"{id.ID}, {entity.Name}";
        }
        return value.ToString();
    }

    private string GetGameObjectPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform current = gameObject.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }
}