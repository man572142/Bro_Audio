using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using System.Collections.Generic;
using System.Linq;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public static bool TryLoadResources<T>(string path, out T resource) where T : UnityEngine.Object
        {
            // Try to be smarter about this
            var assetName = System.IO.Path.GetFileNameWithoutExtension(path);

            resource = Resources.Load<T>(assetName);

            if (resource != null)
            {
                return true;
            }

            resource = Resources.Load<T>(path);
            if (resource != null)
            {
                return true;
            }

            return false;
        }

        [System.Obsolete("legacy conversion only")]
        public static bool TryGetCoreData(out BroAudioData coreData)
        {
            coreData = Resources.Load<BroAudioData>(CoreDataResourcesPath);
            return coreData;
        }

        [System.Obsolete("legacy conversion only")]
        public static bool TryGetDemoData(out IAudioAsset demoAsset, out AudioEntity firstEntity)
        {
            demoAsset = null;
            firstEntity = null;

            if (TryGetCoreData(out var coreData))
            {
                demoAsset = coreData.Assets.FirstOrDefault(x => x.AssetName == BroName.Demo);

                List<AudioEntity> demoEntities = new List<AudioEntity>();

                if (demoAsset != null)
                {
                    GetAudioEntities(demoEntities, demoAsset as AudioAsset);

                    if (demoEntities.Count > 0)
                    {
                        firstEntity = demoEntities[0];
                    }
                }
            }
            return demoAsset != null && firstEntity != null;
        }

        public static T CreateScriptableObjectIfNotExist<T>(string path) where T : ScriptableObject
        {
            T scriptableObj;
            if (!TryLoadResources<T>(path, out scriptableObj))
            {
                scriptableObj = ScriptableObject.CreateInstance<T>();
                if (scriptableObj is EditorSetting editorSetting)
                {
                    editorSetting.ResetToFactorySettings();
                }
                else if (scriptableObj is RuntimeSetting runtimeSetting)
                {
                    runtimeSetting.ResetToFactorySettings();
                }
                AssetDatabase.CreateAsset(scriptableObj, path);
                EditorUtility.SetDirty(scriptableObj);
            }
            return scriptableObj;
        }

        public static void WriteAssetOutputPathToSetting(string path)
        {
            Undo.RecordObject(EditorSetting, "Change BroAudio Asset Output Path");
            EditorSetting.AssetOutputPath = path;
            SaveToDisk(EditorSetting);
        }

        public static void SaveToDisk(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
        }
    }
}