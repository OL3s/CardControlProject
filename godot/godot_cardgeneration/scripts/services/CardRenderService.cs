using CardGeneration.App;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Services;

public sealed class CardRenderService
{
    public ToolResult RenderCard(CardResource card, string outputPath)
    {
        return RenderCard(card, outputPath, card.Id);
    }

    public ToolResult RenderCard(CardResource card, string outputPath, string fileNameStem)
    {
        var image = CardImageRenderer.Render(card);
        var filePath = ProjectPaths.GetPngOutputPath(outputPath, fileNameStem);
        var error = image.SavePng(filePath);
        return error == Error.Ok
            ? ToolResult.Ok($"Rendered card '{card.Id}' to {filePath}.")
            : ToolResult.Fail($"Failed to render card '{card.Id}' to {filePath}: {error}.");
    }
}
