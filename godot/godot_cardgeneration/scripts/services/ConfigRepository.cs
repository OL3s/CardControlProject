using CardGeneration.App;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Services;

public sealed class ConfigRepository
{
    public const string ConfigPath = "res://resources/config/card_tool_config.tres";

    public CardToolConfigResource LoadConfig()
    {
        return ResourceLoader.Load<CardToolConfigResource>(ConfigPath) ?? new CardToolConfigResource();
    }

    public ToolResult SaveConfig(CardToolConfigResource config)
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://resources/config"));
        var error = ResourceSaver.Save(config, ConfigPath);
        return error == Error.Ok
            ? ToolResult.Ok($"Saved config to {ConfigPath}.")
            : ToolResult.Fail($"Failed to save config to {ConfigPath}: {error}.");
    }

    public ToolResult ResetConfig()
    {
        return SaveConfig(new CardToolConfigResource());
    }
}
