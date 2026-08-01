using System.Collections.Generic;
using Godot;

namespace CardGeneration.Services;

public static class ResourceRepository
{
    public static IReadOnlyList<TResource> LoadAll<TResource>(string rootPath)
        where TResource : Resource
    {
        var resources = new List<TResource>();
        LoadDirectory(rootPath, resources);
        return resources;
    }

    private static void LoadDirectory<TResource>(string directoryPath, List<TResource> resources)
        where TResource : Resource
    {
        using var directory = DirAccess.Open(directoryPath);
        if (directory is null)
        {
            GD.PushWarning($"Resource directory was not found: {directoryPath}");
            return;
        }

        directory.ListDirBegin();
        var fileName = directory.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName != "." && fileName != "..")
            {
                var childPath = $"{directoryPath}/{fileName}";
                if (directory.CurrentIsDir())
                {
                    LoadDirectory(childPath, resources);
                }
                else if (IsResourceFile(fileName))
                {
                    var resource = ResourceLoader.Load<TResource>(childPath);
                    if (resource is not null)
                    {
                        resources.Add(resource);
                    }
                }
            }

            fileName = directory.GetNext();
        }

        directory.ListDirEnd();
    }

    private static bool IsResourceFile(string fileName)
    {
        return fileName.EndsWith(".tres") || fileName.EndsWith(".res");
    }
}
