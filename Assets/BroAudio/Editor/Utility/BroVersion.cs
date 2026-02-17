using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public class BroVersion
    {
        public const string CodeBaseVersion = "3.0.0";

        private static Version _version = null;
        public static Version Version
        {
            get
            {
                if (_version != null)
                {
                    return _version;
                }

                // first try to load from project settings
                try
                {
                    if (File.Exists("ProjectSettings/BroAudio/version"))
                    {
                        var versionText = File.ReadAllText("ProjectSettings/BroAudio/version");

                        if (System.Version.TryParse(versionText, out _version))
                        {
                            return _version;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
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

        private static void SetVersion(System.Version version)
        {
            if (!Directory.Exists("ProjectSettings/BroAudio"))
            {
                Directory.CreateDirectory("ProjectSettings/BroAudio");
            }

            File.WriteAllText("ProjectSettings/BroAudio/version", version.ToString());

            _version = version;
        }

        public static void UpdateVersion()
        {
            SetVersion(Version.Parse(CodeBaseVersion));
        }
    }
}
