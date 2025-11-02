using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Tools
{
    public static class TildeFolderRenamer
    {
        public const string Tilde = "~";
        
        /// <summary>
        /// Renames a folder ending with ~ to remove the tilde suffix.
        /// </summary>
        /// <param name="basePath">The base path relative to the project root (e.g., "Assets/BroAudio")</param>
        /// <param name="folderName">The folder name without the tilde (e.g., "Samples")</param>
        /// <returns>True if the folder was renamed successfully, false otherwise</returns>
        public static bool RenameTildeFolder(string basePath, string folderName)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string folderWithTildeName = folderName + Tilde;
            string folderWithTildePath = Path.Combine(projectPath, basePath, folderWithTildeName);
            string targetFolderPath = Path.Combine(projectPath, basePath, folderName);

            // Check if the tilde folder exists and target doesn't exist
            if (Directory.Exists(folderWithTildePath) && !Directory.Exists(targetFolderPath))
            {
                try
                {
                    // Rename the folder using System.IO since Unity doesn't recognize folders ending with ~
                    Directory.Move(folderWithTildePath, targetFolderPath);
                    Debug.Log(Utility.LogTitle + $" Successfully renamed '{folderWithTildeName}' to '{folderName}' in '{basePath}'");

                    // Refresh the AssetDatabase to make Unity recognize the new folder
                    AssetDatabase.Refresh();
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(Utility.LogTitle + $" Failed to rename '{folderWithTildeName}' to '{folderName}' in '{basePath}': {e.Message}");
                    return false;
                }
            }

            return false;
        }
        
        [MenuItem(BroName.MenuItem_BroAudio + "Others/Set File Structure To UPM Mode")]
        public static void SetFileStructureToUPMMode()
        {
            const string BroMainPath = "Assets/BroAudio/";
            AddTildeToFolder(BroMainPath, "Samples");
            AddTildeToFolder(BroMainPath, "Documentation");
        }

        /// <summary>
        /// Renames a folder to add a tilde suffix.
        /// </summary>
        /// <param name="basePath">The base path relative to the project root (e.g., "Assets/BroAudio")</param>
        /// <param name="folderName">The folder name without the tilde (e.g., "Samples")</param>
        /// <returns>True if the folder was renamed successfully, false otherwise</returns>
        private static bool AddTildeToFolder(string basePath, string folderName)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string sourceFolderPath = Path.Combine(projectPath, basePath, folderName);
            string folderWithTildeName = folderName + Tilde;
            string folderWithTildePath = Path.Combine(projectPath, basePath, folderWithTildeName);

            // Check if the source folder exists and target with tilde doesn't exist
            if (Directory.Exists(sourceFolderPath) && !Directory.Exists(folderWithTildePath))
            {
                try
                {
                    // Rename the folder using System.IO to add tilde suffix
                    Directory.Move(sourceFolderPath, folderWithTildePath);
                    Debug.Log(Utility.LogTitle + $" Successfully renamed '{folderName}' to '{folderWithTildeName}' in '{basePath}'");

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
                    Debug.LogError(Utility.LogTitle + $" Failed to rename '{folderName}' to '{folderWithTildeName}' in '{basePath}': {e.Message}");
                    return false;
                }
            }

            return false;
        }
        
        public static void DeleteThisScript([CallerFilePath] string sourceFilePath = "")
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