using System;
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
    ///   &lt;root&gt;/Core/Scripts/Editor/   → BroAudioEditor assembly
    ///   &lt;root&gt;/Core/Scripts/Runtime/  → BroAudio assembly
    ///   &lt;root&gt;/Core/Resources/        → runtime Resources
    ///   &lt;root&gt;/Core/Editor/Resources/ → editor Resources
    ///
    /// Current layout (relative to <see cref="MainAssetPath"/>):
    ///   Editor/
    ///   Runtime/
    ///   Resources/        (generated, not in package)
    ///   Editor/Resources/ (generated, not in package)
    ///
    /// The old root is discovered dynamically by locating the legacy
    /// BroAudioEditor.asmdef wherever it happens to be in the project, so this
    /// works even if the user moved BroAudio to a non-standard folder.
    ///
    /// The upgrader is called once per import session from <see cref="AssetPostprocessorEditor"/>
    /// before <c>BroUserDataGenerator</c> runs.
    /// </summary>
    public static class FileStructureUpgrader
    {
        // Marker: in the legacy layout the editor asmdef lives at
        // <broAudioRoot>/Core/Scripts/Editor/BroAudioEditor.asmdef
        private const string MainAsmdefGUID = "111d4e39aeddad44898002abada9c174";
        private const string LegacyCorePath = "/Core/Scripts/";

        // Sub-paths inside the old root  →  sub-paths inside the new root
        private static readonly (string OldSub, string NewSub)[] SubPathMappings =
        {
            ("Core/Scripts/Editor",   "Editor"),
            ("Core/Scripts",  "Runtime"),
            ("Core/Resources",        "Resources"),
            ("Core/Editor/Resources", "Editor/Resources"),
        };

        // ────────────────────────────────────────────────────────────
        //  Public entry point
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Searches the project for a legacy BroAudio Core tree and migrates it
        /// to the current layout under <see cref="MainAssetPath"/>.
        /// </summary>
        /// <returns>True if any files were moved or removed.</returns>
        public static bool TryUpgradeFileStructure()
        {
            if (!TryFindLegacyRoot(out string legacyRoot))
            {
                return false;
            }

            string oldCorePath = legacyRoot + "/Core";
            string newRoot     = MainAssetPath; // canonical location of the new package

            Debug.Log(Utility.LogTitle +
                $"Legacy folder structure detected at '{legacyRoot}'. Migrating to '{newRoot}'…");

            bool anyChanged = false;

            foreach (var (oldSub, newSub) in SubPathMappings)
            {
                string sourcePath = legacyRoot + "/" + oldSub;
                string targetPath = newRoot     + "/" + newSub;

                if (!AssetDatabase.IsValidFolder(sourcePath))
                {
                    continue;
                }

                anyChanged |= MigrateDirectory(sourcePath, targetPath);
            }

            // Remove the old Core tree if it is now empty.
            TryDeleteFolderRecursiveIfEmpty(oldCorePath);

            if (anyChanged)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log(Utility.LogTitle + "File structure migration complete.");
            }
            else
            {
                // Core existed but contained nothing migratable – clean it up anyway.
                AssetDatabase.DeleteAsset(oldCorePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            return anyChanged;
        }

        // ────────────────────────────────────────────────────────────
        //  Discovery
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds the root folder of a legacy BroAudio installation by searching
        /// for the old <c>BroAudioEditor.asmdef</c> inside a <c>Core/Scripts/Editor/</c>
        /// sub-path.  Works regardless of where BroAudio was placed in the project.
        /// </summary>
        private static bool TryFindLegacyRoot(out string legacyRoot)
        {
            legacyRoot = null;
            var path = AssetDatabase.GUIDToAssetPath(MainAsmdefGUID);
            int markerIndex = path.IndexOf(LegacyCorePath, StringComparison.Ordinal);

            if (markerIndex >= 0)
            {
                // Everything before "/Core/Scripts/Editor/" is the BroAudio root.
                legacyRoot = path.Substring(0, markerIndex);
                return true;
            }
            return false;
        }

        // ────────────────────────────────────────────────────────────
        //  Migration helpers
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

                // Preserve the relative sub-path inside the source directory.
                string relative  = assetPath.Substring(sourceDir.Length).TrimStart('/');
                string newPath   = targetDir + "/" + relative;

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
        /// Returns true when a file already exists on disk at the given asset-relative
        /// path (e.g. "Assets/BroAudio/Editor/BroAudioEditor.asmdef").
        /// </summary>
        private static bool FileExistsInProject(string assetPath)
        {
            // Application.dataPath ends with "/Assets"; strip that and append the
            // full asset path to get an absolute file-system path.
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
        /// Deletes <paramref name="folderPath"/> (and its .meta file) if it contains
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
