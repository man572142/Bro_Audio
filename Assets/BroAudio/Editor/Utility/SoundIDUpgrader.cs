using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    [System.Obsolete("legacy conversion only")]
    internal class SoundIDUpgrader
    {
        private const int MaxPropertyDepth = 10;
        private const int MaxIterations = 10000;

        [MenuItem(BroName.MenuItem_BroAudio + "/Others/Upgrade Sound IDs")]
        private static void UpgradeSoundIDs()
        {
            if (!EditorUtility.DisplayDialog("Upgrade all Sound IDs?", 
                "Are you sure you want to upgrade all Sound IDs? This process may take a long time and it will not be undoable without version control or backups.", 
                "Yes, upgrade all Sound IDs",
                "No"))
            {
                return;
            }

            StartUpgrade();
        }

        public static void StartUpgrade()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Upgrading Sound IDs", "Upgrading Sound IDs", float.NaN);
                AssetDatabase.StartAssetEditing();

                var upgrader = new SoundIDUpgrader();
                Upgrade();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
        }

        private static void Upgrade()
        {
            // find all scenes, prefabs, and scriptable objects
            var searchFolders = new string[] { "Assets", "Packages" };
            var assetPaths = AssetDatabase.FindAssets("t:SceneAsset", searchFolders)
                .Concat(AssetDatabase.FindAssets("t:GameObject", searchFolders))
                .Concat(AssetDatabase.FindAssets("t:ScriptableObject", searchFolders))
                .Distinct()
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();

            for (int i = assetPaths.Count - 1; i >= 0; i--)
            {
                var assetPath = assetPaths[i];

                if (assetPath.StartsWith("Packages/", System.StringComparison.OrdinalIgnoreCase))
                {
                    // packages need to be checked to see if they're actually editable
                    var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                    if (info == null)
                    {
                        assetPaths.RemoveAt(i);
                        continue;
                    }

                    // Editable if the package is local or embedded
                    if (info.source != UnityEditor.PackageManager.PackageSource.Embedded && info.source != UnityEditor.PackageManager.PackageSource.Local)
                    {
                        assetPaths.RemoveAt(i);
                        continue;
                    }
                }
            }

            for (int i = 0, count = assetPaths.Count; i < count; i++)
            {
                EditorUtility.DisplayProgressBar("Upgrading", $"Upgrading assets ({i + 1}/{count})", (i + 1) / (float)count);

                var assetPath = assetPaths[i];

                if (assetPath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                {
                    CheckScene(assetPath);
                }
                else
                {
                    CheckObject(assetPath);
                }
            }

            // And once all is said and done, clear out our stored entities
            List<AudioAsset> audioAssets = new List<AudioAsset>();
            BroEditorUtility.GetAudioAssets(audioAssets);
            foreach (var audioAsset in audioAssets)
            {
                audioAsset.ClearStoredEntities();
            }

            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        private static void CheckScene(string assetPath)
        {
            bool changed = false;
            var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);

            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                changed |= UpgradeGameObject(rootGameObject);
            }

            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void CheckObject(string assetPath)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (obj is GameObject gameObject)
            {
                if (UpgradeGameObject(gameObject))
                {
                    AssetDatabase.SaveAssetIfDirty(gameObject);
                }
            }
            else if (obj is ScriptableObject)
            {
                if (Upgrade(obj))
                {
                    AssetDatabase.SaveAssetIfDirty(obj);
                }
            }
        }

        private static bool UpgradeGameObject(GameObject gameObject)
        {
            bool changed = false;

            foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
            {
                changed |= Upgrade(component);
            }

            foreach (Transform child in gameObject.transform)
            {
                changed |= UpgradeGameObject(child.gameObject);
            }

            return changed;
        }

        private static bool Upgrade(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            using (var so = new SerializedObject(obj))
            {
                return Upgrade(so);
            }
        }

        private static bool Upgrade(SerializedObject so)
        {
            bool changed = false;

            using (var property = so.GetIterator())
            {
                bool enterChildren = true;
                int iterations = 0;
                while (property.Next(enterChildren))
                {
                    enterChildren = property.depth < MaxPropertyDepth;

                    if (++iterations > MaxIterations)
                    {
                        Debug.LogWarning($"[BroAudio] Skipped \"{so.targetObject?.name}\": property iteration exceeded {MaxIterations}, possible circular reference.");
                        break;
                    }

                    changed |= Upgrade(property);
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        private static bool Upgrade(SerializedProperty property)
        {
            if (property.isInstantiatedPrefab && !property.prefabOverride)
            {
                // If the containing component was itself added as an override (doesn't exist in the
                // original prefab), its fields won't individually be marked as prefabOverride even
                // though they are effectively overrides. We still need to upgrade them.
                var targetObj = property.serializedObject?.targetObject;
                if (!(targetObj is Component comp && PrefabUtility.IsAddedComponentOverride(comp)))
                {
                    return false;
                }
            }
            
            bool changed = false;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    UpgradeGenericSerializedProperty(property, ref changed);
                    break;
                case SerializedPropertyType.ManagedReference:
                    UpgradeManageReference(property, ref changed);
                    break;
            }

            return changed;
        }

        private static void UpgradeGenericSerializedProperty(SerializedProperty property, ref bool changed)
        {
            if (property.isArray)
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    changed |= Upgrade(property.GetArrayElementAtIndex(i));
                }
            }
            else if (property.type.Contains(nameof(SoundID)))
            {
                var entityProperty = property.FindPropertyRelative(SoundID.NameOf.Entity);
                var idProperty = property.FindPropertyRelative(SoundID.NameOf.ID);

                if (idProperty != null && entityProperty != null &&
                    idProperty.propertyType == SerializedPropertyType.Integer &&
                    entityProperty.propertyType == SerializedPropertyType.ObjectReference &&
                    entityProperty.objectReferenceValue == null && 
                    idProperty.intValue != 0 && idProperty.intValue != -1)
                {
                    if (BroEditorUtility.TryConvertIdToEntity(idProperty.intValue, out var entity))
                    {
                        entityProperty.objectReferenceValue = entity;
                    }
                    else
                    {
                        Debug.LogError($"Failed to convert SoundID:{idProperty.intValue} on {idProperty.serializedObject?.targetObject?.name}");
                    }

                    //idProperty.intValue = 0;
                    changed = true;
                }
            }
        }

        private static void UpgradeManageReference(SerializedProperty property, ref bool changed)
        {
#if UNITY_2022_1_OR_NEWER
            HashSet<object> traversed = null;
            var obj = property.managedReferenceValue;
            if (UpgradeObject(obj))
            {
                property.managedReferenceValue = obj;
                changed = true;
            }
            
            bool UpgradeObject(object obj)
            {
                traversed ??= new HashSet<object>();

                bool changed = false;

                if (obj == null || !traversed.Add(obj))
                {
                    return false;
                }

                var type = obj.GetType();

                if (type.IsValueType || type.IsPrimitive)
                {
                    return false;
                }

                try
                {
                    if (obj is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            changed |= UpgradeObject(item);
                        }
                    }
                }
                catch
                {
                }

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    var value = field.GetValue(obj);

                    if (value == null)
                    {
                        continue;
                    }

                    var fieldType = field.FieldType;

                    if (fieldType.IsValueType && fieldType == typeof(SoundID) && value is SoundID soundId)
                    {
                        if (!soundId.IsValid())
                        {
                            var idField = fieldType.GetField(SoundID.NameOf.ID, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var id = (int)idField.GetValue(soundId);

                            // upgrade it

                            if (id != 0 && id != -1)
                            {
                                if (BroEditorUtility.TryConvertIdToEntity(id, out var entity))
                                {
                                    var newSoundId = new SoundID(entity);
                                    SoundID.__setLegacyId(ref newSoundId, id);
                                    field.SetValue(value, newSoundId);

                                    changed = true;
                                }
                                else
                                {
                                    Debug.LogError($"Unable to convert SoundID {id} to entity");
                                }
                            }
                        }
                    }

                    if (UpgradeObject(value))
                    {
                        changed = true;
                        field.SetValue(obj, value);
                    }
                }

                return changed;
            }
#endif
        }
    }
}
