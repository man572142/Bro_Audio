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
        [MenuItem(BroName.MenuItem_BroAudio + "/Others/Upgrade Sound IDs (v2 -> v3+ upgrade)")]
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
                upgrader.Upgrade();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
        }

        private void Upgrade()
        {
            // find all scenes, prefabs, and scriptable objects
            var assetPaths = AssetDatabase.FindAssets("t:Object", new string[] { "Assets", "Packages" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            foreach (var assetPath in assetPaths)
            {
                if (assetPath.StartsWith("Packages/", System.StringComparison.OrdinalIgnoreCase))
                {
                    // packages need to be checked to see if they're actually editable
                    var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                    if (info == null)
                    {
                        continue;
                    }

                    // Editable if the package is local or embedded
                    if (info.source != UnityEditor.PackageManager.PackageSource.Embedded && info.source != UnityEditor.PackageManager.PackageSource.Local)
                    {
                        continue;
                    }
                }

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

        private void CheckScene(string assetPath)
        {
            bool changed = false;
            var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);

            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                changed |= Upgrade(rootGameObject);
            }

            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private void CheckObject(string assetPath)
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadMainAssetAtPath(assetPath)
            };

            foreach (var obj in objects)
            {
                if (Upgrade(obj))
                {
                    AssetDatabase.SaveAssetIfDirty(obj);
                }
            }
        }

        private bool Upgrade(UnityEngine.Object obj)
        {
            bool changed = false;

            if (obj == null)
            {
                return changed;
            }

            using (var so = new SerializedObject(obj))
            {
                changed |= Upgrade(so);
            }

            if (obj is GameObject gameObject)
            {
                // and all components
                foreach (Component component in gameObject.GetComponents<Component>())
                {
                    changed |= Upgrade(component);
                }

                // and all children
                foreach (Transform child in gameObject.transform)
                {
                    changed |= Upgrade(child.gameObject);
                }
            }

            return changed;
        }

        private bool Upgrade(SerializedObject so)
        {
            bool changed = false;

            using (var property = so.GetIterator())
            {
                while (property.Next(true))
                {
                    changed |= Upgrade(property);
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        private bool Upgrade(SerializedProperty property)
        {
            if (property.isInstantiatedPrefab && !property.prefabOverride)
            {
                return false;
            }

            HashSet<object> traversed = null;
            bool changed = false;

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                if (property.isArray)
                {
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        changed |= Upgrade(property.GetArrayElementAtIndex(i));
                    }
                }
                else
                {
                    if (property.type.Contains("SoundID"))
                    {
                        var entityProperty = property.FindPropertyRelative(SoundID.NameOf.Entity);
                        var idProperty = property.FindPropertyRelative(SoundID.NameOf.ID);

                        if (idProperty != null && entityProperty != null &&
                            idProperty.propertyType == SerializedPropertyType.Integer &&
                            entityProperty.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (property.isInstantiatedPrefab && property.prefabOverride)
                            {
                                Debug.Log($"Check {property.serializedObject.targetObject.name} property {property.propertyPath} in asset {AssetDatabase.GetAssetOrScenePath(property.serializedObject.targetObject)}");
                            }

                            if (idProperty.intValue != 0 && idProperty.intValue != -1)
                            {
                                if (entityProperty.objectReferenceValue == null)
                                {
                                    if (BroAudio.TryConvertIdToEntity(idProperty.intValue, out var entity))
                                    {
                                        entityProperty.objectReferenceValue = entity;
                                    }
                                }

                                idProperty.intValue = 0;
                                changed = true;
                            }
                        }
                    }
                }
            }
            else if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
#if UNITY_2022_1_OR_NEWER
                var obj = property.managedReferenceValue;
                if (UpgradeObject(obj))
                {
                    property.managedReferenceValue = obj;
                    changed = true;
                }
#endif
            }

            return changed;

            bool UpgradeObject(object obj)
            {
                traversed ??= new HashSet<object>();

                bool changed = false;

                if (obj == null || !traversed.Add(obj))
                {
                    return changed;
                }

                var type = obj.GetType();

                if (type.IsValueType || type.IsPrimitive)
                {
                    return changed;
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
                        var idField = fieldType.GetField(SoundID.NameOf.ID, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var id = (int)idField.GetValue(soundId);

                        // upgrade it

                        if (id != 0 && id != -1)
                        {
                            if (BroAudio.TryConvertIdToEntity(id, out var entity))
                            {
                                field.SetValue(value, new SoundID(entity));
                                changed = true;
                            }
                            else
                            {
                                Debug.LogError($"Unable to convert SoundID {id} to entity");
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
        }
    }
}
