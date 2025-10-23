using UnityEngine;
using System.IO;

/// <summary>
/// Static helper class for managing file paths for debugging outputs
/// such as textures and performance logs.
/// Ensures that the target folders exist.
/// </summary>
public static class DebuggingFilePaths
{
    // Configurable folder names (relative to BaseOutputPath) 
    public static string DebugTextureFolderName { get; set; } = "DebugImages";
    public static string PerformanceLogFolderName { get; set; } = "PerformanceLogs";

    // Base path for all outputs 
    // Application.persistentDataPath is writable on most platforms,
    // including in builds. In the editor, it is outside the Assets folder.
    // Alternatively: Application.dataPath for outputs directly in the Assets folder (editor only).
    public static string BaseOutputPath { get; set; } = Application.persistentDataPath;

    /// <summary>
    /// Determines the complete, validated path for a debug texture file.
    /// Creates the target folder if it does not exist.
    /// </summary>
    /// <param name="fileName">The desired file name (e.g., “debug_chunk_0_0.png”).</param>
    /// <returns>The complete, secure path to the file.</returns>
    public static string GetDebugTexturePath(string fileName)
    {
        return GetValidatedFullPath(DebugTextureFolderName, fileName);
    }

    /// <summary>
    /// Determines the complete, validated path for a performance log file.
    /// Creates the destination folder if it does not exist.
    /// </summary>
    /// <param name="fileName">The desired file name (e.g., “performance_log.csv”).</param>
    /// <returns>The complete, secure path to the file.</returns>
    public static string GetPerformanceLogPath(string fileName)
    {
        return GetValidatedFullPath(PerformanceLogFolderName, fileName);
    }

    private static string GetValidatedFullPath(string relativeFolderName, string fileName)
    {
        string fullFolderPath = ""; 
        try
        {
            fullFolderPath = Path.Combine(BaseOutputPath, relativeFolderName);

            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
                Debug.Log($"Debugging Folder created at: {fullFolderPath}");
            }

            return Path.Combine(fullFolderPath, fileName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error creating/checking directory '{fullFolderPath}' " +
                           $"Saving to base path '{BaseOutputPath}' instead. Error: {ex.Message}");

            return Path.Combine(BaseOutputPath, fileName);
        }
    }
}