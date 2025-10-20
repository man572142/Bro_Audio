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
        public static bool TryLoadResources<T>(string path, out T resouece) where T : UnityEngine.Object
        {
            resouece = Resources.Load<T>(path);
            return resouece != null;
        }

        public static bool TryGetCoreData(out BroAudioData coreData)
        {
            coreData = Resources.Load<BroAudioData>(CoreDataResourcesPath);
            return coreData;
        }

        public static bool TryGetDemoData(out IAudioAsset demoAsset, out IEntityIdentity firstEntity)
        {
            demoAsset = null;
            firstEntity = null;
            if (TryGetCoreData(out var coreData))
            {
                demoAsset = coreData.Assets.FirstOrDefault(x => x.AssetName == BroName.Demo);
                firstEntity = demoAsset?.GetAllAudioEntities().FirstOrDefault();
            }
            return demoAsset != null && firstEntity != null;
        }

        public static bool TryGetAssetByGUID(string assetGUID,out IAudioAsset asset)
        {
            return TryGetAssetByPath(AssetDatabase.GUIDToAssetPath(assetGUID) ,out asset);
        }

        public static bool TryGetAssetByPath(string assetPath, out IAudioAsset asset)
        {
            asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IAudioAsset;
            return asset != null;
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

        public static void AddNewAssetToCoreData(ScriptableObject asset)
        {
            if(TryGetCoreData(out var coreData))
            {
                coreData.AddAsset(asset as AudioAsset);
                SaveToDisk(coreData);
            }
        }

        public static void RemoveEmptyDatas()
        {
            if (TryGetCoreData(out BroAudioData coreData)
                && coreData.RemoveEmpty())
            {
                SaveToDisk(coreData);
            }
        }

        public static void ReorderAssets(List<string> _allAssetGUIDs)
        {
            if (TryGetCoreData(out var coreData))
            {
                coreData.ReorderAssets(_allAssetGUIDs);
                SaveToDisk(coreData);
            }
        }

        public static void SaveToDisk(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
        }

        [MenuItem(BroName.MenuItem_BroAudio + "Others/Fix Duplicate SoundIDs")]
        public static void FixDuplicateSoundIDs()
        {
            if (!TryGetCoreData(out var data))
            {
                return;
            }

            var orderedEntities = data.Assets
                    .SelectMany(x => x.GetAllAudioEntities())
                    .OrderBy(x => x.ID);

            List<IEntityIdentity> duplicates = new List<IEntityIdentity>();
            bool isDirty = false;
            int lastId = -1;
            BroAudioType lastAudioType = BroAudioType.None;
            foreach (var entity in orderedEntities)
            {
                var audioType = Utility.GetAudioType(entity.ID);
                if (audioType != lastAudioType)
                {
                    ReassignDuplicatedSoundIDs(duplicates, lastId);
                    duplicates.Clear();
                }

                if (entity.ID == lastId)
                {
                    duplicates.Add(entity);
                    isDirty = true;
                }

                lastId = entity.ID;
                lastAudioType = audioType;
            }
            ReassignDuplicatedSoundIDs(duplicates, lastId);

            if (isDirty)
            {
                foreach (var asset in data.Assets)
                {
                    if (asset is AudioAsset audioAsset)
                    {
                        SaveToDisk(audioAsset);
                    }
                }
                ShowDuplicateSoundIDResolvedDialog();
            }
        }

        private static void ReassignDuplicatedSoundIDs(IReadOnlyList<IEntityIdentity> duplicates, int lastId)
        {
            foreach (var identity in duplicates)
            {
                if (identity is AudioEntity entity)
                {
                    entity.ReassignID(lastId + 1);
                    lastId++;
                }
            }
        }

        private static void ShowDuplicateSoundIDResolvedDialog()
        {
            string title = "Duplicate SoundID Fixed";
            string message = "Duplicate SoundIDs were detected and automatically resolved in BroAudio." +
                "\nIf you're using version control, please ensure these changes are applied.";
            string ok = "More Info";
            string cancel = "OK";
            // The reason cancel = "OK" is because we want closing the dialog window to be treated as OK too.
            bool isMoreInfo = EditorUtility.DisplayDialog(title, message, ok, cancel);

            if(isMoreInfo)
            {
                Application.OpenURL("https://man572142s-organization.gitbook.io/broaudio/others/known-issues/duplicate-soundid-issue");
            }
        }
    }
}