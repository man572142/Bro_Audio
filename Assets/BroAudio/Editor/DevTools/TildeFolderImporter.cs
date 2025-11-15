using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Tools
{
    public static class TildeFolderImporter
    {
        public const string Tilde = "~";
        private const string UPMPackageRootPathTitle = "\\com.";
        private const string RootFolderName = "BroAudio";

        /// <summary>
        /// Renames a folder ending with ~ to remove the tilde suffix.
        /// If the folder is in a UPM package (Packages folder), it will be copied to Assets first.
        /// </summary>
        /// <param name="folderName">The folder name without the tilde (e.g., "Samples")</param>
        /// <param name="sourceFilePath">The path of the calling file (automatically provided by CallerFilePath)</param>
        /// <returns>True if the folder was renamed successfully, false otherwise</returns>
        public static bool ImportTildeFolder(string folderName)
        {
            string folderWithTildeName = folderName + Tilde;
            string targetFolderPath = Path.Combine(Application.dataPath, RootFolderName, folderName);

            // If target already exists, nothing to do
            if (Directory.Exists(targetFolderPath))
            {
                return false;
            }

            string folderWithTildePath = Path.Combine(Application.dataPath, RootFolderName, folderWithTildeName);
            bool existsInAssets = Directory.Exists(folderWithTildePath);

            // If not in Assets, check if it exists in Packages (UPM installation)
            if (!existsInAssets && !CopyFromPackagesFolder(folderWithTildeName, folderWithTildePath))
            {
                return false;
            }
            
            existsInAssets = Directory.Exists(folderWithTildePath);

            // Now rename the folder in Assets (if it exists there)
            if (existsInAssets)
            {
                try
                {
                    // Rename the folder using System.IO since Unity doesn't recognize folders ending with ~
                    Directory.Move(folderWithTildePath, targetFolderPath);
                    Debug.Log(Utility.LogTitle + $" Successfully renamed '{folderWithTildeName}' to '{folderName}' in Assets");

                    // Refresh the AssetDatabase to make Unity recognize the new folder
                    AssetDatabase.Refresh();
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(Utility.LogTitle + $" Failed to rename '{folderWithTildeName}' to '{folderName}': {e.Message}");
                    return false;
                }
            }

            return false;
        }

        private static bool CopyFromPackagesFolder(string folderWithTildeName,
            string folderWithTildePath, [CallerFilePath] string sourceFilePath = "")
        {
            if (!sourceFilePath.Contains(UPMPackageRootPathTitle))
            {
                return false;
            }

            string rootPath = sourceFilePath;
            string parentPath = Path.GetDirectoryName(rootPath);
            while (parentPath.Contains(UPMPackageRootPathTitle))
            {
                rootPath = parentPath;
                parentPath = Path.GetDirectoryName(rootPath);
            }

            // Construct the path to the tilde folder in the package
            string packageFolderWithTildePath = Path.Combine(rootPath, folderWithTildeName);

            if (Directory.Exists(packageFolderWithTildePath))
            {
                try
                {
                    // Copy from Packages to Assets
                    Directory.CreateDirectory(Path.GetDirectoryName(folderWithTildePath));
                    CopyDirectory(packageFolderWithTildePath, folderWithTildePath);
                    Debug.Log(Utility.LogTitle + $" Copied '{folderWithTildeName}' from UPM package to Assets");
                }
                catch (System.Exception e)
                {
                    Debug.LogError(Utility.LogTitle + $" Failed to copy '{folderWithTildeName}' from UPM package: {e.Message}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Recursively copies a directory and all its contents.
        /// </summary>
        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                // Skip .meta files as Unity will regenerate them
                if (fileName.EndsWith(".meta"))
                    continue;

                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // Copy all subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(subDir, destDir);
            }
        }
        
        [MenuItem(BroName.MenuItem_BroAudio + "Others/Set File Structure To UPM Mode")]
        public static void SetFileStructureToUPMMode()
        {
            AddTildeToFolder("Samples");
            AddTildeToFolder("Documentation");
        }

        /// <summary>
        /// Renames a folder to add a tilde suffix.
        /// </summary>
        /// <param name="basePath">The base path relative to the project root (e.g., "Assets/BroAudio")</param>
        /// <param name="folderName">The folder name without the tilde (e.g., "Samples")</param>
        /// <returns>True if the folder was renamed successfully, false otherwise</returns>
        private static bool AddTildeToFolder(string folderName)
        {
            string sourceFolderPath = Path.Combine(Application.dataPath, RootFolderName, folderName);
            string folderWithTildeName = folderName + Tilde;
            string folderWithTildePath = Path.Combine(Application.dataPath, RootFolderName, folderWithTildeName);
            
            // Check if the source folder exists and target with tilde doesn't exist
            if (Directory.Exists(sourceFolderPath) && !Directory.Exists(folderWithTildePath))
            {
                try
                {
                    // Rename the folder using System.IO to add tilde suffix
                    Directory.Move(sourceFolderPath, folderWithTildePath);
                    Debug.Log(Utility.LogTitle + $" Successfully renamed '{folderName}' to '{folderWithTildeName}'");

                    // Delete the old .meta file to prevent orphaned meta files in the project
                    string oldMetaFilePath = sourceFolderPath + ".meta";
                    if (File.Exists(oldMetaFilePath))
                    {
                        File.Delete(oldMetaFilePath);
                    }
                    
                    AssetDatabase.Refresh();
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(Utility.LogTitle + $" Failed to rename '{folderName}' to '{folderWithTildeName}': {e.Message}");
                    return false;
                }
            }

            return false;
        }
        
        public static void DeleteCallerScript([CallerFilePath] string sourceFilePath = "")
        {
            // Convert absolute file path to Unity's relative asset path
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string normalizedSourcePath = sourceFilePath.Replace('\\', '/');
            string normalizedProjectPath = projectPath.Replace('\\', '/');
            // Remove project path prefix to get relative path
            if (normalizedSourcePath.StartsWith(normalizedProjectPath))
            {
                sourceFilePath = normalizedSourcePath.Substring(normalizedProjectPath.Length + 1);
            }
            
            if (File.Exists(sourceFilePath))
            {
                AssetDatabase.DeleteAsset(sourceFilePath);
            }
        }
    }
}