using System;
using System.Collections.Generic;
using System.Reflection;
using Ami.BroAudio;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor;
using Ami.BroAudio.Tools;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class AssetFieldUsageFinder : EditorWindow
{
    [MenuItem(BroName.MenuItem_BroAudio + "Others/Find SoundID Usage")]
    public static void ShowWindow()
    {
        var window = GetWindow<AssetFieldUsageFinder>("Field Usage Finder");
    }
    
    private const string RootPath = "Assets";

    private string _targetTypeName = nameof(SoundID);
    private string _targetPath = RootPath;
    private bool _findInPrefab;
    private bool _findInScriptableObject;
    private bool _findInScene;
    private Vector2 _scrollPosition;
    private readonly List<UsageResult> _results = new List<UsageResult>();
    
    private Dictionary<int, IEntityIdentity> _broAudioEntities = new Dictionary<int, IEntityIdentity>();

    private MultiColumnHeader _multiColumnHeader;
    private MultiColumnHeaderState _multiColumnHeaderState;
    private int _sortedColumnIndex = -1;
    private bool _sortedAscending = true;

    private enum ColumnType
    {
        Source,
        GameObjectPath,
        ComponentType,
        FieldName,
        Value,
        FullAssetPath,
    }

    private class UsageResult
    {
        public string AssetPath;
        public string AssetFileName;
        public string GameObjectPath;
        public string ComponentType;
        public string FieldName;
        public string Value;
        public UnityEngine.Object Asset;
        public GameObject GameObject;
    }

    private void OnEnable()
    {
        InitializeMultiColumnHeader();
        
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

    private void InitializeMultiColumnHeader()
    {
        var columns = new MultiColumnHeaderState.Column[]
        {
            CreateColumn("Source", 100),
            CreateColumn("GameObject", 150),
            CreateColumn("Component", 120),
            CreateColumn("Field", 100),
            CreateColumn("Value", 150),
            CreateColumn("Asset Path", 200),
        };

        _multiColumnHeaderState = new MultiColumnHeaderState(columns);
        _multiColumnHeader = new MultiColumnHeader(_multiColumnHeaderState);
        _multiColumnHeader.sortingChanged += OnSortingChanged;
        _multiColumnHeader.ResizeToFit();
    }
    
    private MultiColumnHeaderState.Column CreateColumn(string header, float width)
    {
        return new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent(header),
            headerTextAlignment = TextAlignment.Left,
            sortedAscending = true,
            sortingArrowAlignment = TextAlignment.Center,
            width = width,
            minWidth = 50,
            autoResize = true,
            allowToggleVisibility = true,
        };
    }

    private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
    {
        _sortedColumnIndex = multiColumnHeader.sortedColumnIndex;
        _sortedAscending = multiColumnHeader.GetColumn(_sortedColumnIndex).sortedAscending;
        SortResults();
        Repaint();
    }

    private void SortResults()
    {
        if (_sortedColumnIndex < 0 || _results.Count == 0)
            return;

        var columnType = (ColumnType)_sortedColumnIndex;
        
        switch (columnType)
        {
            case ColumnType.Source:
                _results.Sort((a, b) => Compare(a.AssetFileName, b.AssetFileName));
                break;
            case ColumnType.GameObjectPath:
                _results.Sort((a, b) => Compare(a.GameObjectPath, b.GameObjectPath));
                break;
            case ColumnType.ComponentType:
                _results.Sort((a, b) => Compare(a.ComponentType, b.ComponentType));
                break;
            case ColumnType.FieldName:
                _results.Sort((a, b) => Compare(a.FieldName, b.FieldName));
                break;
            case ColumnType.Value:
                _results.Sort((a, b) => Compare(a.Value, b.Value));
                break;
        }
        
        int Compare(string a, string b) => _sortedAscending ? 
            string.Compare(a, b, StringComparison.Ordinal) : string.Compare(b, a, StringComparison.Ordinal);
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
            
            var headerRect = GUILayoutUtility.GetRect(0, _multiColumnHeader.height, GUILayout.ExpandWidth(true));
            _multiColumnHeader.OnGUI(headerRect, 0.0f);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            for (int i = 0; i < _results.Count; i++)
            {
                var result = _results[i];
                var rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                
                if (i % 2 == 0)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));
                }
                
                DrawResultRow(rowRect, result);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawResultRow(Rect rowRect, UsageResult result)
    {
        float[] columnWidths = new float[_multiColumnHeaderState.columns.Length];
        for (int i = 0; i < _multiColumnHeaderState.columns.Length; i++)
        {
            columnWidths[i] = _multiColumnHeaderState.columns[i].width;
        }

        Rect[] columnRects = new Rect[columnWidths.Length];
        float x = rowRect.x;
        for (int i = 0; i < columnWidths.Length; i++)
        {
            columnRects[i] = new Rect(x, rowRect.y, columnWidths[i], rowRect.height);
            x += columnWidths[i];
        }
        
        if (GUI.Button(columnRects[(int)ColumnType.Source], result.AssetFileName, EditorStyles.linkLabel))
        {
            if (result.GameObject != null)
            {
                Selection.activeGameObject = result.GameObject;
                EditorGUIUtility.PingObject(result.GameObject);
            }
            else if (result.Asset != null)
            {
                Selection.activeObject = result.Asset;
                EditorGUIUtility.PingObject(result.Asset);
            }
        }
        
        GUI.Label(columnRects[(int)ColumnType.GameObjectPath], result.GameObjectPath);
        GUI.Label(columnRects[(int)ColumnType.ComponentType], result.ComponentType);
        GUI.Label(columnRects[(int)ColumnType.FieldName], result.FieldName);
        GUI.Label(columnRects[(int)ColumnType.Value], result.Value);
        GUI.Label(columnRects[(int)ColumnType.FullAssetPath], result.AssetPath);
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
        
        Type targetType = FindTypeByName(_targetTypeName);
        if (targetType == null)
        {
            Debug.LogError($"Could not find type: {_targetTypeName}");
            return;
        }

        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int currentAsset = 0;
        try
        {
            foreach (var path in allAssetPaths)
            {
                currentAsset++;
                if (DisplayCancelableProgressBar(path, currentAsset, allAssetPaths.Length))
                {
                    break;
                }

                if (!path.StartsWith(_targetPath))
                {
                    continue;
                }

                if (_findInPrefab && path.EndsWith(".prefab"))
                {
                    CheckPrefab(path, targetType);
                } 
                else if (_findInScene && path.EndsWith(".unity"))
                {
                    CheckScene(path, targetType);
                }
                else if (_findInScriptableObject && path.EndsWith(".asset"))
                {
                    CheckScriptableObject(path, targetType);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        if (_sortedColumnIndex >= 0)
        {
            SortResults();
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
                    AssetFileName = assetPath.Substring(assetPath.LastIndexOf('/') + 1),
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