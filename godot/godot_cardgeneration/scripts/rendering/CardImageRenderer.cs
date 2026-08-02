using System;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Rendering;

public static class CardImageRenderer
{
    public const int PreviewWidth = 750;
    public const int PreviewHeight = 1050;
    public static Color PlaceholderColor => new(0.165f, 0.157f, 0.18f);

    public static Image Render(CardResource card)
    {
        return Render(card, new Vector2I(PreviewWidth, PreviewHeight));
    }

    public static Image Render(CardResource card, Vector2I size)
    {
        var image = CreateTargetImage(size);
        image.Fill(new Color(0, 0, 0, 0));

        DrawCardBase(image, card.CardType);
        DrawCardImage(image, card);
        DrawCardPanels(image, card);

        return image;
    }

    public static Image RenderBack(CardType cardType, Texture2D? backImageTexture = null)
    {
        return RenderBack(cardType, backImageTexture, new Vector2I(PreviewWidth, PreviewHeight));
    }

    public static Image RenderBack(CardType cardType, Texture2D? backImageTexture, Vector2I size)
    {
        var image = CreateTargetImage(size);
        image.Fill(new Color(0, 0, 0, 0));

        if (backImageTexture is null && TryDrawImageSource(image, GetDefaultCardBackPath(cardType), new Rect2I(0, 0, PreviewWidth, PreviewHeight)))
        {
            return image;
        }

        DrawCardBase(image, cardType);
        DrawCardBackImage(image, cardType, backImageTexture);

        return image;
    }

    public static Image RenderResized(CardResource card, int width, int height)
    {
        return Render(card, new Vector2I(width, height));
    }

    public static Image RenderBackResized(CardType cardType, Texture2D? backImageTexture, int width, int height)
    {
        return RenderBack(cardType, backImageTexture, new Vector2I(width, height));
    }

    private static Image CreateTargetImage(Vector2I size)
    {
        return Image.CreateEmpty(Math.Max(1, size.X), Math.Max(1, size.Y), false, Image.Format.Rgba8);
    }

    private static void DrawCardBase(Image image, CardType cardType)
    {
        FillRoundedRect(image, new Rect2I(20, 20, 710, 1010), 48, new Color(0.02f, 0.02f, 0.025f));
        FillRoundedRect(image, new Rect2I(38, 38, 674, 974), 40, GetFrameColor(cardType));
        FillRoundedRect(image, new Rect2I(58, 58, 634, 934), 30, new Color(0.035f, 0.03f, 0.035f));
    }

    private static void DrawCardImage(Image image, CardResource card)
    {
        var cardImageRect = new Rect2I(62, 62, 626, 926);
        if (card.CardImageTexture is null)
        {
            if (TryDrawImageSource(image, card.CardImageSourcePath, cardImageRect))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(card.CardImageSourcePath))
            {
                DrawPlaceholderCardImage(image, cardImageRect);
            }
            else
            {
                DrawImageNotFoundPlaceholder(image, cardImageRect);
            }

            return;
        }

        DrawTexture(image, card.CardImageTexture, cardImageRect);
    }

    private static bool TryDrawImageSource(Image target, string sourcePath, Rect2I targetRect)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var globalPath = ProjectPaths.ToGlobalPath(sourcePath);
        if (!FileAccess.FileExists(sourcePath) && !System.IO.File.Exists(globalPath))
        {
            return false;
        }

        var source = Image.LoadFromFile(globalPath);
        if (source is null)
        {
            return false;
        }

        targetRect = ScaleRect(target, targetRect);
        source.Convert(Image.Format.Rgba8);
        source.Resize(targetRect.Size.X, targetRect.Size.Y, Image.Interpolation.Lanczos);
        target.BlendRect(source, new Rect2I(Vector2I.Zero, source.GetSize()), targetRect.Position);
        return true;
    }

    private static void DrawCardBackImage(Image image, CardType cardType, Texture2D? backImageTexture)
    {
        var backImageRect = new Rect2I(62, 62, 626, 926);
        if (backImageTexture is not null)
        {
            DrawTexture(image, backImageTexture, backImageRect);
            return;
        }

        FillRoundedRect(image, backImageRect, 28, GetBackFieldColor(cardType));

        var center = new Vector2I(375, 525);
        var accentColor = GetFrameColor(cardType);
        FillRoundedRect(image, new Rect2I(center.X - 170, center.Y - 170, 340, 340), 170, new Color(0.02f, 0.015f, 0.018f, 0.88f));
        FillRoundedRect(image, new Rect2I(center.X - 135, center.Y - 135, 270, 270), 135, new Color(accentColor.R, accentColor.G, accentColor.B, 0.32f));
        FillRoundedRect(image, new Rect2I(center.X - 74, center.Y - 230, 148, 460), 74, new Color(0.04f, 0.018f, 0.02f, 0.92f));
        FillRoundedRect(image, new Rect2I(center.X - 92, center.Y - 92, 184, 184), 92, GetFrameColor(cardType));
        FillRoundedRect(image, new Rect2I(center.X - 64, center.Y - 64, 128, 128), 64, new Color(0.03f, 0.02f, 0.025f));
    }

    private static void DrawCardPanels(Image image, CardResource card)
    {
        switch (card)
        {
            case MonsterCardResource monster:
                DrawMonsterPanels(image, monster);
                break;
            case TerrainCardResource terrain:
                DrawTerrainPanels(image, terrain);
                break;
        }
    }

    private static void DrawMonsterPanels(Image image, MonsterCardResource card)
    {
        DrawResourceAmounts(image, card.Requirements, new Vector2I(96, 86), 68, 10);

        var bonusCount = card.PowerBonuses?.Length ?? 0;
        var bonusPanelBottom = 950;
        var bonusPanelHeight = Math.Clamp(116 + bonusCount * 50, 116, 300);
        var bonusPanel = new Rect2I(92, bonusPanelBottom - bonusPanelHeight, 566, bonusPanelHeight);
        FillRoundedRect(image, bonusPanel, 26, new Color(0.025f, 0.018f, 0.018f, 0.82f));

        var basePowerY = bonusPanelBottom - 24 - 64 - bonusCount * 50;
        DrawPowerIconsCentered(image, card.BasePower, basePowerY, 64, 6);

        var lineY = basePowerY + 78;
        foreach (var bonus in card.PowerBonuses ?? Array.Empty<PowerBonusResource>())
        {
            var nextX = DrawResourceAmounts(image, bonus.Requirements, new Vector2I(150, lineY - 3), 48, 6);
            DrawArrowRight(image, new Rect2I(nextX + 18, lineY - 6, 70, 54));
            DrawPowerIcons(image, bonus.PowerGain, new Vector2I(nextX + 112, lineY), 42, 6);
            lineY += 50;
        }
    }

    private static void DrawTerrainPanels(Image image, TerrainCardResource card)
    {
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Neutral, new Vector2I(92, 88), drawRightToLeft: false);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Grass, new Vector2I(92, 862), drawRightToLeft: false);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Flame, new Vector2I(658, 88), drawRightToLeft: true);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Water, new Vector2I(658, 862), drawRightToLeft: true);
    }

    private static void DrawTerrainResourceCorner(Image image, ResourceAmount[] amounts, ElementType elementType, Vector2I anchor, bool drawRightToLeft)
    {
        var amount = amounts.FirstOrDefault(resourceAmount => resourceAmount.Element?.ElementType == elementType && resourceAmount.Amount > 0);
        if (amount is null)
        {
            return;
        }

        const int iconSize = 78;
        const int gap = 10;
        var count = Math.Max(1, amount.Amount);

        if (drawRightToLeft)
        {
            DrawElementIconsRightToLeft(image, amount.Element, count, new Vector2I(anchor.X - iconSize, anchor.Y), iconSize, gap);
            return;
        }

        DrawElementIcons(image, amount.Element, count, anchor, iconSize, gap);
    }

    private static int DrawResourceAmounts(Image image, ResourceAmount[] amounts, Vector2I start, int size, int gap)
    {
        var x = start.X;
        foreach (var amount in amounts)
        {
            DrawElementIcons(image, amount.Element, amount.Amount, new Vector2I(x, start.Y), size, gap);
            x += Math.Max(0, amount.Amount) * (size + gap) + gap;
        }

        return x;
    }

    private static void DrawElementIcons(Image image, ElementResource? element, int count, Vector2I start, int size, int gap)
    {
        for (var index = 0; index < count; index++)
        {
            var rect = new Rect2I(start.X + index * (size + gap), start.Y, size, size);
            if (element?.IconTexture is not null)
            {
                DrawTexture(image, element.IconTexture, rect);
            }
            else
            {
                DrawElementFallback(image, element?.ElementType ?? ElementType.Neutral, rect);
            }
        }
    }

    private static void DrawElementIconsRightToLeft(Image image, ElementResource? element, int count, Vector2I start, int size, int gap)
    {
        for (var index = 0; index < count; index++)
        {
            var rect = new Rect2I(start.X - index * (size + gap), start.Y, size, size);
            if (element?.IconTexture is not null)
            {
                DrawTexture(image, element.IconTexture, rect);
            }
            else
            {
                DrawElementFallback(image, element?.ElementType ?? ElementType.Neutral, rect);
            }
        }
    }

    private static void DrawPowerIcons(Image image, int count, Vector2I start, int size, int gap)
    {
        for (var index = 0; index < count; index++)
        {
            DrawPowerFallback(image, new Rect2I(start.X + index * (size + gap), start.Y, size, size));
        }
    }

    private static void DrawPowerIconsCentered(Image image, int count, int y, int size, int gap)
    {
        var iconCount = Math.Max(0, count);
        var width = iconCount * size + Math.Max(0, iconCount - 1) * gap;
        DrawPowerIcons(image, iconCount, new Vector2I((PreviewWidth - width) / 2, y), size, gap);
    }

    private static void DrawRepeatedTexture(Image image, Texture2D? texture, int count, Vector2I start, int size, int gap)
    {
        for (var index = 0; index < count; index++)
        {
            DrawTexture(image, texture, new Rect2I(start.X + index * (size + gap), start.Y, size, size));
        }
    }

    private static void DrawTexture(Image target, Texture2D? texture, Rect2I targetRect)
    {
        if (texture is null)
        {
            FillRoundedRect(target, targetRect, Math.Min(targetRect.Size.X, targetRect.Size.Y) / 2, new Color(0.85f, 0.82f, 0.72f));
            return;
        }

        var source = texture.GetImage();
        if (source is null)
        {
            return;
        }

        targetRect = ScaleRect(target, targetRect);
        source.Convert(Image.Format.Rgba8);
        source.Resize(targetRect.Size.X, targetRect.Size.Y, Image.Interpolation.Lanczos);
        target.BlendRect(source, new Rect2I(Vector2I.Zero, source.GetSize()), targetRect.Position);
    }

    private static void DrawPlaceholderCardImage(Image image, Rect2I rect)
    {
        FillRoundedRect(image, rect, 28, PlaceholderColor);

        // Intentionally plain: placeholder art must stay local and not imply final illustration.
    }

    private static void DrawImageNotFoundPlaceholder(Image image, Rect2I rect)
    {
        var accent = new Color(0.72f, 0.67f, 0.54f);
        FillRoundedRect(image, rect, 28, PlaceholderColor);

        var imageFrame = new Rect2I(225, 385, 300, 280);
        FillRoundedRect(image, imageFrame, 22, accent);
        FillRoundedRect(image, new Rect2I(239, 399, 272, 252), 16, new Color(0.075f, 0.07f, 0.085f));
        FillRoundedRect(image, new Rect2I(274, 434, 44, 44), 22, accent);
        DrawDiagonalBand(image, new Vector2I(260, 425), new Vector2I(490, 625), 16, accent);
        DrawDiagonalBand(image, new Vector2I(490, 425), new Vector2I(260, 625), 16, accent);
    }

    private static void DrawDiagonalBand(Image image, Vector2I start, Vector2I end, int thickness, Color color)
    {
        var steps = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)) / 4;
        for (var step = 0; step <= steps; step++)
        {
            var progress = steps == 0 ? 0f : (float)step / steps;
            var x = Mathf.RoundToInt(Mathf.Lerp(start.X, end.X, progress));
            var y = Mathf.RoundToInt(Mathf.Lerp(start.Y, end.Y, progress));
            FillRoundedRect(image, new Rect2I(x - thickness / 2, y - thickness / 2, thickness, thickness), thickness / 2, color);
        }
    }

    private static void DrawElementFallback(Image image, ElementType elementType, Rect2I rect)
    {
        FillRoundedRect(image, rect, Math.Min(rect.Size.X, rect.Size.Y) / 2, new Color(0.96f, 0.93f, 0.84f));

        var inset = Math.Max(4, rect.Size.X / 8);
        var inner = new Rect2I(rect.Position.X + inset, rect.Position.Y + inset, rect.Size.X - inset * 2, rect.Size.Y - inset * 2);
        FillRoundedRect(image, inner, Math.Min(inner.Size.X, inner.Size.Y) / 2, GetElementColor(elementType));
    }

    private static void DrawPowerFallback(Image image, Rect2I rect)
    {
        FillRoundedRect(image, rect, Math.Min(rect.Size.X, rect.Size.Y) / 2, new Color(0.98f, 0.94f, 0.75f));
        var inset = Math.Max(5, rect.Size.X / 7);
        FillRoundedRect(
            image,
            new Rect2I(rect.Position.X + inset, rect.Position.Y + rect.Size.Y / 2, rect.Size.X - inset * 2, rect.Size.Y / 4),
            rect.Size.Y / 10,
            new Color(0.12f, 0.08f, 0.07f));
        FillRoundedRect(
            image,
            new Rect2I(rect.Position.X + rect.Size.X / 3, rect.Position.Y + inset, rect.Size.X / 3, rect.Size.Y - inset * 2),
            rect.Size.Y / 9,
            new Color(0.12f, 0.08f, 0.07f));
    }

    private static void DrawArrowRight(Image image, Rect2I rect)
    {
        var centerY = rect.Position.Y + rect.Size.Y / 2;
        var stemHeight = Math.Max(6, rect.Size.Y / 5);
        FillRoundedRect(
            image,
            new Rect2I(rect.Position.X, centerY - stemHeight / 2, rect.Size.X * 2 / 3, stemHeight),
            stemHeight / 2,
            new Color(0.98f, 0.94f, 0.82f));

        var headSize = rect.Size.Y / 2;
        for (var offset = 0; offset < headSize; offset++)
        {
            var width = headSize - offset;
            FillRoundedRect(
                image,
                new Rect2I(rect.Position.X + rect.Size.X * 2 / 3 + offset, centerY - width / 2, Math.Max(2, rect.Size.X / 18), width),
                1,
                new Color(0.98f, 0.94f, 0.82f));
        }
    }

    private static Color GetFrameColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.30f, 0.24f, 0.26f),
            CardType.Terrain => new Color(0.42f, 0.34f, 0.24f),
            _ => new Color(0.22f, 0.22f, 0.25f)
        };
    }

    private static Color GetBackFieldColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.105f, 0.095f, 0.115f),
            CardType.Terrain => new Color(0.15f, 0.125f, 0.095f),
            _ => new Color(0.10f, 0.10f, 0.12f)
        };
    }

    private static string GetDefaultCardBackPath(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => "res://assets/card_backs/monster_card_back.svg",
            CardType.Terrain => "res://assets/card_backs/terrain_card_back.svg",
            _ => string.Empty
        };
    }

    private static Color GetElementColor(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Grass => new Color(0.35f, 0.66f, 0.31f),
            ElementType.Flame => new Color(0.94f, 0.42f, 0.18f),
            ElementType.Water => new Color(0.24f, 0.58f, 0.84f),
            _ => new Color(0.63f, 0.61f, 0.55f)
        };
    }

    private static void FillRoundedRect(Image image, Rect2I rect, int radius, Color color)
    {
        rect = ScaleRect(image, rect);
        radius = ScaleRadius(image, radius);

        var maxX = Math.Min(rect.End.X, image.GetWidth());
        var maxY = Math.Min(rect.End.Y, image.GetHeight());
        var minX = Math.Max(rect.Position.X, 0);
        var minY = Math.Max(rect.Position.Y, 0);
        var radiusSquared = radius * radius;

        for (var y = minY; y < maxY; y++)
        {
            for (var x = minX; x < maxX; x++)
            {
                var cornerX = x < rect.Position.X + radius
                    ? rect.Position.X + radius
                    : x >= rect.End.X - radius
                        ? rect.End.X - radius - 1
                        : x;
                var cornerY = y < rect.Position.Y + radius
                    ? rect.Position.Y + radius
                    : y >= rect.End.Y - radius
                        ? rect.End.Y - radius - 1
                        : y;

                var dx = x - cornerX;
                var dy = y - cornerY;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    BlendPixel(image, x, y, color);
                }
            }
        }
    }

    private static Rect2I ScaleRect(Image image, Rect2I rect)
    {
        if (image.GetWidth() == PreviewWidth && image.GetHeight() == PreviewHeight)
        {
            return rect;
        }

        var scaleX = image.GetWidth() / (float)PreviewWidth;
        var scaleY = image.GetHeight() / (float)PreviewHeight;
        return new Rect2I(
            Mathf.RoundToInt(rect.Position.X * scaleX),
            Mathf.RoundToInt(rect.Position.Y * scaleY),
            Math.Max(1, Mathf.RoundToInt(rect.Size.X * scaleX)),
            Math.Max(1, Mathf.RoundToInt(rect.Size.Y * scaleY)));
    }

    private static int ScaleRadius(Image image, int radius)
    {
        if (radius <= 0 || image.GetWidth() == PreviewWidth && image.GetHeight() == PreviewHeight)
        {
            return radius;
        }

        var scale = Math.Min(image.GetWidth() / (float)PreviewWidth, image.GetHeight() / (float)PreviewHeight);
        return Math.Max(1, Mathf.RoundToInt(radius * scale));
    }

    private static void BlendPixel(Image image, int x, int y, Color color)
    {
        if (color.A >= 0.999f)
        {
            image.SetPixel(x, y, color);
            return;
        }

        var existing = image.GetPixel(x, y);
        var alpha = color.A;
        var inverseAlpha = 1.0f - alpha;
        image.SetPixel(
            x,
            y,
            new Color(
                color.R * alpha + existing.R * inverseAlpha,
                color.G * alpha + existing.G * inverseAlpha,
                color.B * alpha + existing.B * inverseAlpha,
                alpha + existing.A * inverseAlpha));
    }
}
