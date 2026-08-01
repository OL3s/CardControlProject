using System;
using System.IO;
using Godot;

namespace CardGeneration.App;

public static class ProjectPaths
{
    public static string ToGlobalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "res://output";
        }

        if (path.StartsWith("res://", StringComparison.Ordinal) || path.StartsWith("user://", StringComparison.Ordinal))
        {
            return ProjectSettings.GlobalizePath(path);
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(ProjectSettings.GlobalizePath("res://"), path);
    }

    public static string GetPngOutputPath(string outputPath, string fileNameStem)
    {
        var globalPath = ToGlobalPath(outputPath);

        if (Path.GetExtension(globalPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(globalPath) ?? ProjectSettings.GlobalizePath("res://"));
            return globalPath;
        }

        Directory.CreateDirectory(globalPath);
        return Path.Combine(globalPath, $"{SanitizeFileName(fileNameStem)}.png");
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}
