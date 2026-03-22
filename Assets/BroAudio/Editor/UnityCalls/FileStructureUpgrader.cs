using System;
using System.IO;
using System.Runtime.CompilerServices;
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
        private const string NewAsmdefPath = "/Runtime";
        private const string UPMPath = "/Library/PackageCache/";
        
        // .asmref file used to prevent compilation errors before migration
        private const string TransitionalAsmrefFileName = "BroAssemblyReference";

        // Sub-paths inside the old root  →  sub-paths inside the new root
        private static readonly (string OldSub, string NewSub)[] SubPathMappings =
        {
            ("Core/Scripts/Editor",   "Editor"),
            ("Core/Scripts",  "Runtime"),
            ("Core/Resources",        "Resources"),
            ("Core/Editor/Resources", "Editor/Resources"),
            ("Core", "Resources"),
            ("Demo", "Samples/Demo"),
        };

        // ────────────────────────────────────────────────────────────
        //  Public entry point
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Searches the project for a legacy BroAudio Core tree and migrates it
        /// to the current layout, preserving the user's original installation path.
        /// If the legacy root differs from <see cref="MainAssetPath"/>, newly imported
        /// package files are relocated from <see cref="MainAssetPath"/> to the user's
        /// original location.
        /// </summary>
        /// <returns>True if any files were moved or removed.</returns>
        public static bool TryUpgradeFileStructure([CallerFilePath] string callerPath = null)
        {
            var root = FindRoot(out bool isLegacyRoot);
            if (!isLegacyRoot)
            {
#if !BroAudio_DevOnly
                if (!callerPath.Contains(UPMPath))
                {
                    if (MainAssetPath != root && AssetDatabase.IsValidFolder(MainAssetPath))
                    {
                        MigrateDirectory(MainAssetPath, root);
                    }
                    RemoveAsmrefFiles(root);
                    TryDeleteFolderRecursiveIfEmpty(MainAssetPath);
                }
#endif
                return false;
            }

            Debug.Log(Utility.LogTitle +
                $"Legacy folder structure detected at '{root}'. Start migrating…");

            bool anyChanged = false;

            // +1 for the optional relocation step after the mapping loop.
            int totalSteps = SubPathMappings.Length + 1;
            int step = 0;

            try
            {
                foreach (var (oldSub, newSub) in SubPathMappings)
                {
                    EditorUtility.DisplayProgressBar(
                        "BroAudio Migration",
                        $"Migrating {oldSub} → {newSub}",
                        (float)step / totalSteps);
                    step++;

                    string sourcePath = root + "/" + oldSub;
                    string targetPath = root + "/" + newSub;

                    if (!AssetDatabase.IsValidFolder(sourcePath))
                    {
                        continue;
                    }

                    anyChanged |= MigrateDirectory(sourcePath, targetPath);
                }

                // Relocate newly imported package files from MainAssetPath to the
                // user's original path (no-op when they are the same).
                EditorUtility.DisplayProgressBar(
                    "BroAudio Migration",
                    "Relocating package files…",
                    (float)step / totalSteps);

                if (MainAssetPath != root && AssetDatabase.IsValidFolder(MainAssetPath))
                {
                    anyChanged |= MigrateDirectory(MainAssetPath, root);
                    TryDeleteFolderRecursiveIfEmpty(MainAssetPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

#if BroAudio_DevOnly
            RemoveAsmrefFiles(MainAssetPath);
#endif
            // Remove the old tree if it is now empty.
            TryDeleteFolderRecursiveIfEmpty(root + "/Core");
            TryDeleteFolderRecursiveIfEmpty(root + "/Demo");

            if (anyChanged)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log(Utility.LogTitle + "File structure migration complete.");
            }

            return anyChanged;
        }

        // ────────────────────────────────────────────────────────────
        //  Discovery
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds the root folder of BroAudio installation by searching
        /// </summary>
        private static string FindRoot(out bool isLegacyRoot)
        {
            var path = AssetDatabase.GUIDToAssetPath(MainAsmdefGUID);
            var newPathIndex = path.IndexOf(NewAsmdefPath, StringComparison.Ordinal);
            int legacyPathIndex = path.IndexOf(LegacyCorePath, StringComparison.Ordinal);
            isLegacyRoot = legacyPathIndex >= 0;
            
            return path.Substring(0, isLegacyRoot ? legacyPathIndex : newPathIndex);
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
        /// Deletes the transitional <c>.asmref</c> files that are no longer needed
        /// once the migration is complete.
        /// </summary>
        private static void RemoveAsmrefFiles(string searchRoot)
        {
            var guids = AssetDatabase.FindAssets(TransitionalAsmrefFileName, new[] { searchRoot });
            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && FileExistsInProject(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        /// <summary>
        /// Recursively deletes <paramref name="folderPath"/> and any empty sub-folders
        /// within it.  A folder is deleted only when it contains no non-folder assets.
        /// Children are processed depth-first so that a parent becomes empty once all
        /// of its empty children have been removed.
        /// </summary>
        private static void TryDeleteFolderRecursiveIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // Process sub-folders depth-first.
            string[] childGuids = AssetDatabase.FindAssets("", new[] { folderPath });
            foreach (string guid in childGuids)
            {
                string childPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(childPath) &&
                    Path.GetDirectoryName(childPath).Replace('\\', '/') == folderPath)
                {
                    TryDeleteFolderRecursiveIfEmpty(childPath);
                }
            }

            // Now check whether this folder itself is empty.
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
