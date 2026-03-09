using System.IO;
using UnityEditor;
using UnityEngine;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Editor
{
    /// <summary>
    /// Migrates files from the legacy BroAudio folder layout to the current layout.
    ///
    /// Legacy layout (pre-UPM, shipped as a plain .unitypackage):
    ///   Assets/BroAudio/Core/Scripts/Editor/   → BroAudioEditor assembly
    ///   Assets/BroAudio/Core/Scripts/Runtime/  → BroAudio assembly
    ///   Assets/BroAudio/Core/Resources/        → runtime Resources
    ///   Assets/BroAudio/Core/Editor/Resources/ → editor Resources
    ///
    /// Current layout:
    ///   Assets/BroAudio/Editor/
    ///   Assets/BroAudio/Runtime/
    ///   Assets/BroAudio/Resources/        (generated, not in package)
    ///   Assets/BroAudio/Editor/Resources/ (generated, not in package)
    ///
    /// The upgrader is called once per import session from <see cref="AssetPostprocessorEditor"/>.
    /// It moves every file from the legacy tree to the corresponding new path, skipping files
    /// that were already placed there by the new package, and deletes the old Core folder once
    /// it is empty.
    /// </summary>
    public static class FileStructureUpgrader
    {
        private const string OldCoreFolder = "Core";
        private static string OldCoreAssetPath => MainAssetPath + "/" + OldCoreFolder;

        // Each entry: (old path relative to MainAssetPath, new path relative to MainAssetPath)
        private static readonly (string Source, string Target)[] DirectoryMappings =
        {
            ("Core/Scripts/Editor",   "Editor"),
            ("Core/Scripts/Runtime",  "Runtime"),
            ("Core/Resources",        "Resources"),
            ("Core/Editor/Resources", "Editor/Resources"),
        };

        /// <summary>
        /// Checks for a legacy file structure and migrates it to the current layout.
        /// </summary>
        /// <returns>True if any files were moved or deleted.</returns>
        public static bool TryUpgradeFileStructure()
        {
            if (!AssetDatabase.IsValidFolder(OldCoreAssetPath))
            {
                return false;
            }

            Debug.Log(Utility.LogTitle + "Legacy folder structure detected. Upgrading…");

            bool anyChanged = false;

            foreach (var (source, target) in DirectoryMappings)
            {
                string sourcePath = MainAssetPath + "/" + source;
                string targetPath = MainAssetPath + "/" + target;

                if (!AssetDatabase.IsValidFolder(sourcePath))
                {
                    continue;
                }

                anyChanged |= MigrateDirectory(sourcePath, targetPath);
            }

            // Remove the old Core tree if it is now empty.
            TryDeleteFolderRecursiveIfEmpty(OldCoreAssetPath);

            if (anyChanged)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log(Utility.LogTitle + "File structure migration complete.");
            }
            else
            {
                // The Core folder exists but was already empty – clean it up anyway.
                AssetDatabase.DeleteAsset(OldCoreAssetPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            return anyChanged;
        }

        // ────────────────────────────────────────────────────────────
        //  Private helpers
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Moves every non-folder asset from <paramref name="sourceDir"/> into the
        /// mirrored location under <paramref name="targetDir"/>, preserving the
        /// relative sub-path.  Files that already exist at the destination (placed
        /// there by the incoming package) are simply removed from the old location.
        /// </summary>
        private static bool MigrateDirectory(string sourceDir, string targetDir)
        {
            bool anyMoved = false;

            // FindAssets recursively returns GUIDs for both files and folders.
            string[] guids = AssetDatabase.FindAssets("", new[] { sourceDir });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Skip folder entries; we create them on demand below.
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                // Preserve the sub-path inside the source directory.
                string relative = assetPath.Substring(sourceDir.Length).TrimStart('/');
                string newPath  = targetDir + "/" + relative;

                // Make sure the destination folder exists before moving.
                string destFolder = Path.GetDirectoryName(newPath).Replace('\\', '/');
                EnsureFolderExists(destFolder);

                if (FileExistsInProject(newPath))
                {
                    // The new package already placed this file; discard the stale copy.
                    AssetDatabase.DeleteAsset(assetPath);
                    anyMoved = true;
                }
                else
                {
                    string error = AssetDatabase.MoveAsset(assetPath, newPath);

                    if (string.IsNullOrEmpty(error))
                    {
                        anyMoved = true;
                    }
                    else
                    {
                        Debug.LogWarning(Utility.LogTitle +
                            $"Could not migrate '{assetPath}' → '{newPath}': {error}");
                    }
                }
            }

            return anyMoved;
        }

        /// <summary>
        /// Returns true when a file already exists on disk at the given asset-relative path
        /// (e.g. "Assets/BroAudio/Editor/BroAudioEditor.asmdef").
        /// </summary>
        private static bool FileExistsInProject(string assetPath)
        {
            // Application.dataPath ends with "/Assets", so we strip that prefix and
            // re-attach the full assetPath to get an absolute file-system path.
            string fullPath = Application.dataPath + assetPath.Substring("Assets".Length);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Recursively ensures that every folder segment in <paramref name="folderPath"/>
        /// exists in the AssetDatabase.
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent     = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// Deletes <paramref name="folderPath"/> (and its meta file) if it contains
        /// no non-folder assets.
        /// </summary>
        private static void TryDeleteFolderRecursiveIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] remaining = AssetDatabase.FindAssets("", new[] { folderPath });
            bool hasFiles = false;

            foreach (string guid in remaining)
            {
                if (!AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    hasFiles = true;
                    break;
                }
            }

            if (!hasFiles)
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }
    }
}
