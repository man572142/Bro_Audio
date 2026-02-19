using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public static class BroVersion
    {
        public const string CodeBaseVersion = "3.1.0";

        private const string VersionResourceName = "BroAudioVersion";
        public const string VersionFileName = VersionResourceName + ".txt";

        private static Version _version = null;
        public static Version Version
        {
            get
            {
                if (_version != null)
                {
                    return _version;
                }

                // first try to load from Editor/Resources
                var versionAsset = Resources.Load<TextAsset>(VersionResourceName);
                if (versionAsset != null)
                {
                    if (System.Version.TryParse(versionAsset.text, out _version))
                    {
                        Resources.UnloadAsset(versionAsset);
                        return _version;
                    }
                    Resources.UnloadAsset(versionAsset);
                }

                // then try from old legacy broaudio data

#pragma warning disable CS0618 // Type or member is obsolete
                var coreData = Resources.Load<Data.BroAudioData>(BroEditorUtility.CoreDataResourcesPath);

                if (coreData != null)
                {
                    if (!string.IsNullOrEmpty(coreData._version) && System.Version.TryParse(coreData._version, out _version))
                    {
                        coreData._version = null;
                        EditorUtility.SetDirty(coreData);
                        SetVersion(_version);
                        return _version;
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete

                _version = new Version(CodeBaseVersion);
                SetVersion(_version);

                return _version;
            }
        }

        private static string GetEditorResourcesPath([CallerFilePath] string callerFilePath = "")
        {
            // Navigate from Editor/Utility/BroVersion.cs up to Editor/, then into Resources/
            string utilityDir = Path.GetDirectoryName(callerFilePath);
            string editorDir = Path.GetDirectoryName(utilityDir);
            return Path.Combine(editorDir, "Resources");
        }

        private static void SetVersion(System.Version version)
        {
            string resourcesDir = GetEditorResourcesPath();

            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }

            string filePath = Path.Combine(resourcesDir, VersionFileName);
            File.WriteAllText(filePath, version.ToString());

            string assetPath = "Assets" + filePath.Substring(Application.dataPath.Length).Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath);

            _version = version;
        }

        public static void UpdateVersion()
        {
            SetVersion(Version.Parse(CodeBaseVersion));
        }

    }
}
