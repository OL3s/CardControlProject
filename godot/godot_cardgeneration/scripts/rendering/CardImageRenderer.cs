using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Rendering;

public static class CardImageRenderer
{
    private static readonly object TextureReadLock = new();
    internal const string PowerIconPath = "res://assets/icons/symbols/power.svg";
    internal const string ArrowRightIconPath = "res://assets/icons/symbols/arrow_right.svg";
    public const int PreviewWidth = 750;
    public const int PreviewHeight = 1050;
    public static Color PlaceholderColor => new(0.165f, 0.157f, 0.18f);

    public static Image Render(CardResource card)
    {
        return Render(card, new Vector2I(PreviewWidth, PreviewHeight));
    }

    public static Image Render(CardResource card, Vector2I size)
    {
        return Render(card, size, null, null);
    }

    public static Image Render(
        CardResource card,
        Vector2I size,
        IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides,
        Texture2D? powerIconOverride)
    {
        var image = CreateTargetImage(size);
        image.Fill(new Color(0, 0, 0, 0));

        DrawCardBase(image, card.CardType);
        DrawCardImage(image, card);
        DrawCardPanels(image, card, elementIconOverrides, powerIconOverride);

        return image;
    }

    public static Image RenderBack(CardType cardType, Texture2D? backImageTexture = null)
    {
        return RenderBack(cardType, backImageTexture, string.Empty, CardImageScaleMode.Cover, new Vector2I(PreviewWidth, PreviewHeight));
    }

    public static Image RenderBack(CardType cardType, Texture2D? backImageTexture, Vector2I size)
    {
        return RenderBack(cardType, backImageTexture, string.Empty, CardImageScaleMode.Cover, size);
    }

    public static Image RenderBack(
        CardType cardType,
        Texture2D? backImageTexture,
        string backImageSourcePath,
        CardImageScaleMode scaleMode,
        Vector2I size)
    {
        var image = CreateTargetImage(size);
        image.Fill(new Color(0, 0, 0, 0));

        DrawCardBase(image, cardType);
        DrawCardBackImage(image, cardType, backImageTexture, backImageSourcePath, scaleMode);

        return image;
    }

    public static Image RenderResized(CardResource card, int width, int height)
    {
        return Render(card, new Vector2I(width, height));
    }

    public static Image RenderPrint(
        CardResource card,
        Vector2I exportSize,
        Rect2I trimRect,
        IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides,
        Texture2D? powerIconOverride,
        bool fullBleed)
    {
        var image = CreateTargetImage(exportSize);
        image.Fill(fullBleed ? GetOuterInkColor(card.CardType) : new Color(0, 0, 0, 0));
        using var trimImage = RenderPrintTrim(
            trimRect.Size,
            card.CardType,
            size => Render(card, size, elementIconOverrides, powerIconOverride));
        image.BlendRect(trimImage, new Rect2I(Vector2I.Zero, trimImage.GetSize()), trimRect.Position);
        return image;
    }

    public static Image RenderBackResized(CardType cardType, Texture2D? backImageTexture, int width, int height)
    {
        return RenderBack(cardType, backImageTexture, new Vector2I(width, height));
    }

    public static Image RenderBackResized(
        CardType cardType,
        Texture2D? backImageTexture,
        string backImageSourcePath,
        CardImageScaleMode scaleMode,
        int width,
        int height)
    {
        return RenderBack(cardType, backImageTexture, backImageSourcePath, scaleMode, new Vector2I(width, height));
    }

    public static Image RenderBackPrint(
        CardType cardType,
        Texture2D? backImageTexture,
        string backImageSourcePath,
        CardImageScaleMode scaleMode,
        Vector2I exportSize,
        Rect2I trimRect,
        bool fullBleed)
    {
        var image = CreateTargetImage(exportSize);
        image.Fill(fullBleed ? GetOuterInkColor(cardType) : new Color(0, 0, 0, 0));
        using var trimImage = RenderPrintTrim(
            trimRect.Size,
            cardType,
            size => RenderBack(cardType, backImageTexture, backImageSourcePath, scaleMode, size));
        image.BlendRect(trimImage, new Rect2I(Vector2I.Zero, trimImage.GetSize()), trimRect.Position);
        return image;
    }

    private static Image RenderPrintTrim(Vector2I trimSize, CardType cardType, Func<Vector2I, Image> renderContent)
    {
        var trimImage = CreateTargetImage(trimSize);
        trimImage.Fill(new Color(0, 0, 0, 0));
        // The visible card silhouette is the physical trim edge. Content stays at its authored
        // 5:7 ratio inside the 63 x 88 mm target instead of being stretched independently.
        FillRoundedRect(trimImage, new Rect2I(0, 0, PreviewWidth, PreviewHeight), 48, GetOuterInkColor(cardType));

        var scale = Math.Min(trimSize.X / (double)PreviewWidth, trimSize.Y / (double)PreviewHeight);
        var contentSize = new Vector2I(
            Math.Max(1, (int)Math.Round(PreviewWidth * scale)),
            Math.Max(1, (int)Math.Round(PreviewHeight * scale)));
        using var content = renderContent(contentSize);
        var position = (trimSize - contentSize) / 2;
        trimImage.BlendRect(content, new Rect2I(Vector2I.Zero, contentSize), position);
        return trimImage;
    }

    private static Image CreateTargetImage(Vector2I size)
    {
        return Image.CreateEmpty(Math.Max(1, size.X), Math.Max(1, size.Y), false, Image.Format.Rgba8);
    }

    private static void DrawCardBase(Image image, CardType cardType)
    {
        FillRoundedRect(image, new Rect2I(20, 20, 710, 1010), 48, GetOuterInkColor(cardType));
        FillRoundedRect(image, new Rect2I(48, 48, 654, 954), 38, GetFrameColor(cardType));
        FillRoundedRect(image, new Rect2I(62, 62, 626, 926), 30, GetFrameFieldColor(cardType));
        FillRoundedRect(image, new Rect2I(72, 72, 606, 906), 27, GetInsetLineColor(cardType));
        FillRoundedRect(image, new Rect2I(76, 76, 598, 898), 26, GetFrameFieldColor(cardType));
    }

    private static void DrawCardImage(Image image, CardResource card)
    {
        // Card artwork is the complete inner background. Panels and icons are drawn over it later.
        var cardImageRect = new Rect2I(76, 76, 598, 898);
        if (card.CardImageTexture is null)
        {
            if (TryDrawImageSource(image, card.CardImageSourcePath, cardImageRect, 24, card.ImageScaleMode))
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

        Image? source;
        lock (TextureReadLock)
        {
            source = card.CardImageTexture.GetImage();
        }
        if (source is null || source.IsEmpty())
        {
            source?.Dispose();
            DrawImageNotFoundPlaceholder(image, cardImageRect);
            return;
        }

        try
        {
            DrawCardArtwork(image, source, cardImageRect, 24, card.ImageScaleMode);
        }
        finally
        {
            source.Dispose();
        }
    }

    private static bool TryDrawImageSource(
        Image target,
        string sourcePath,
        Rect2I targetRect,
        int cornerRadius = 0,
        CardImageScaleMode scaleMode = CardImageScaleMode.Stretch)
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

        try
        {
            DrawCardArtwork(target, source, targetRect, cornerRadius, scaleMode);
            return true;
        }
        finally
        {
            source.Dispose();
        }
    }

    private static void DrawCardArtwork(Image target, Image source, Rect2I targetRect, int cornerRadius, CardImageScaleMode scaleMode)
    {
        targetRect = ScaleRect(target, targetRect);
        source.Convert(Image.Format.Rgba8);
        if (!Enum.IsDefined(scaleMode))
        {
            scaleMode = CardImageScaleMode.Stretch;
        }

        var sourceSize = source.GetSize();
        var scaleX = (float)targetRect.Size.X / sourceSize.X;
        var scaleY = (float)targetRect.Size.Y / sourceSize.Y;
        var scale = scaleMode switch
        {
            CardImageScaleMode.Fit => Math.Min(scaleX, scaleY),
            CardImageScaleMode.Cover => Math.Max(scaleX, scaleY),
            _ => 0f
        };
        var resizedSize = scaleMode == CardImageScaleMode.Stretch
            ? targetRect.Size
            : new Vector2I(
                Math.Max(1, Mathf.RoundToInt(sourceSize.X * scale)),
                Math.Max(1, Mathf.RoundToInt(sourceSize.Y * scale)));
        source.Resize(resizedSize.X, resizedSize.Y, Image.Interpolation.Lanczos);

        using var composed = Image.CreateEmpty(targetRect.Size.X, targetRect.Size.Y, false, Image.Format.Rgba8);
        composed.Fill(new Color(0, 0, 0, 0));
        var sourceRect = new Rect2I(Vector2I.Zero, resizedSize);
        var destination = (targetRect.Size - resizedSize) / 2;
        if (scaleMode == CardImageScaleMode.Cover)
        {
            sourceRect = new Rect2I((resizedSize - targetRect.Size) / 2, targetRect.Size);
            destination = Vector2I.Zero;
        }

        composed.BlendRect(source, sourceRect, destination);
        if (cornerRadius > 0)
        {
            ApplyRoundedAlphaMask(composed, ScaleRadius(target, cornerRadius));
        }

        target.BlendRect(composed, new Rect2I(Vector2I.Zero, composed.GetSize()), targetRect.Position);
    }

    private static void DrawCardBackImage(
        Image image,
        CardType cardType,
        Texture2D? backImageTexture,
        string backImageSourcePath,
        CardImageScaleMode scaleMode)
    {
        var backImageRect = new Rect2I(76, 76, 598, 898);
        if (backImageTexture is not null)
        {
            DrawTexture(image, backImageTexture, backImageRect, 24, scaleMode);
            return;
        }

        var sourcePath = string.IsNullOrWhiteSpace(backImageSourcePath)
            ? GetDefaultCardBackPath(cardType)
            : backImageSourcePath;
        if (TryDrawImageSource(image, sourcePath, backImageRect, 24, scaleMode))
        {
            return;
        }

        FillRoundedRect(image, backImageRect, 24, GetBackFieldColor(cardType));

        var center = new Vector2I(375, 525);
        var accentColor = GetFrameColor(cardType);
        FillRoundedRect(image, new Rect2I(center.X - 170, center.Y - 170, 340, 340), 170, new Color(0.02f, 0.015f, 0.018f, 0.88f));
        FillRoundedRect(image, new Rect2I(center.X - 135, center.Y - 135, 270, 270), 135, new Color(accentColor.R, accentColor.G, accentColor.B, 0.32f));
        FillRoundedRect(image, new Rect2I(center.X - 74, center.Y - 230, 148, 460), 74, new Color(0.04f, 0.018f, 0.02f, 0.92f));
        FillRoundedRect(image, new Rect2I(center.X - 92, center.Y - 92, 184, 184), 92, GetFrameColor(cardType));
        FillRoundedRect(image, new Rect2I(center.X - 64, center.Y - 64, 128, 128), 64, new Color(0.03f, 0.02f, 0.025f));
    }

    private static void DrawCardPanels(Image image, CardResource card, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides, Texture2D? powerIconOverride)
    {
        switch (card)
        {
            case MonsterCardResource monster:
                DrawMonsterPanels(image, monster, elementIconOverrides, powerIconOverride);
                break;
            case TerrainCardResource terrain:
                DrawTerrainPanels(image, terrain, elementIconOverrides, powerIconOverride);
                break;
        }
    }

    private static void DrawMonsterPanels(Image image, MonsterCardResource card, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides, Texture2D? powerIconOverride)
    {
        DrawResourceAmountsOverlapping(image, card.Requirements, new Vector2I(104, 94), 68, 408, 54, elementIconOverrides);
        DrawMonsterTierDiamonds(image, card.Tier);
        DrawElementIcon(image, card.Element, new Rect2I(570, 82, 92, 92), elementIconOverrides);

        var bonusCount = card.PowerBonuses?.Length ?? 0;
        var bonusPanelBottom = 950;
        var bonusPanelHeight = Math.Clamp(116 + bonusCount * 50, 116, 700);
        var bonusPanel = new Rect2I(92, bonusPanelBottom - bonusPanelHeight, 566, bonusPanelHeight);
        DrawOutlinedPanel(image, bonusPanel, 26, GetFrameColor(CardType.Monster), new Color(0.022f, 0.023f, 0.027f, 0.88f), 5);

        var basePowerY = bonusPanel.Position.Y + 24;
        DrawPowerIconsCentered(image, card.BasePower, basePowerY, 64, 6, powerIconOverride);

        var lineY = basePowerY + 78;
        var lineSpacing = bonusCount == 0
            ? 50
            : Math.Clamp((bonusPanelHeight - 110) / bonusCount, 18, 50);
        foreach (var bonus in card.PowerBonuses ?? Array.Empty<PowerBonusResource>())
        {
            DrawResourceAmountsOverlapping(image, bonus.Requirements, new Vector2I(130, lineY - 3), 48, 230, 34, elementIconOverrides);
            DrawArrowRight(image, new Rect2I(385, lineY - 6, 62, 54));
            DrawPowerIconsOverlapping(image, bonus.PowerGain, new Vector2I(470, lineY), 42, 140, 28, powerIconOverride);
            lineY += lineSpacing;
        }
    }

    private static void DrawTerrainPanels(Image image, TerrainCardResource card, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides, Texture2D? powerIconOverride)
    {
        DrawElementIcon(image, card.Element, new Rect2I(263, 413, 224, 224), elementIconOverrides);

        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Neutral, new Vector2I(92, 88), expandRight: true, elementIconOverrides: elementIconOverrides);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Grass, new Vector2I(580, 88), expandRight: false, elementIconOverrides: elementIconOverrides);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Flame, new Vector2I(92, 862), expandRight: true, elementIconOverrides: elementIconOverrides);
        DrawTerrainResourceCorner(image, card.ProducedResources, ElementType.Water, new Vector2I(580, 862), expandRight: false, elementIconOverrides: elementIconOverrides);
    }

    private static void DrawTerrainResourceCorner(Image image, ResourceAmount[] amounts, ElementType elementType, Vector2I anchor, bool expandRight, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides)
    {
        var matchingAmounts = amounts
            .Where(resourceAmount => resourceAmount.Element?.ElementType == elementType && resourceAmount.Amount > 0)
            .ToArray();
        var count = matchingAmounts.Sum(resourceAmount => resourceAmount.Amount);
        if (count < 1)
        {
            return;
        }

        const int iconSize = 78;
        const int maxSpan = 250;
        const int preferredStep = 53;
        DrawElementIconsOverlapping(image, matchingAmounts[0].Element, count, anchor, iconSize, maxSpan, preferredStep, expandRight, elementIconOverrides);
    }

    private static int DrawResourceAmountsOverlapping(Image image, ResourceAmount[] amounts, Vector2I start, int size, int maxSpan, int preferredStep, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides)
    {
        var elements = amounts
            .Where(amount => amount.Amount > 0)
            .SelectMany(amount => Enumerable.Repeat(amount.Element, amount.Amount))
            .ToArray();
        if (elements.Length == 0)
        {
            return start.X;
        }

        var step = GetOverlapStep(elements.Length, size, maxSpan, preferredStep);
        for (var index = 0; index < elements.Length; index++)
        {
            DrawElementIcon(image, elements[index], new Rect2I(start.X + index * step, start.Y, size, size), elementIconOverrides);
        }

        return start.X + size + (elements.Length - 1) * step;
    }

    private static void DrawElementIconsOverlapping(
        Image image,
        ElementResource? element,
        int count,
        Vector2I anchor,
        int size,
        int maxSpan,
        int preferredStep,
        bool expandRight,
        IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides)
    {
        var step = GetOverlapStep(count, size, maxSpan, preferredStep);

        // Draw from the inside out so the icon anchored to the card corner stays fully visible.
        for (var index = count - 1; index >= 0; index--)
        {
            var offset = index * step * (expandRight ? 1 : -1);
            DrawElementIcon(image, element, new Rect2I(anchor.X + offset, anchor.Y, size, size), elementIconOverrides);
        }
    }

    private static void DrawElementIcon(Image image, ElementResource? element, Rect2I rect, IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides = null)
    {
        if (element is null)
        {
            throw new InvalidOperationException("Card element is required before rendering an element icon.");
        }

        var elementType = element.ElementType;
        var texture = elementIconOverrides is not null && elementIconOverrides.TryGetValue(elementType, out var overrideTexture)
            ? overrideTexture
            : null;

        var radius = Math.Min(rect.Size.X, rect.Size.Y) / 2;
        FillRoundedRect(image, rect, radius, new Color(0.035f, 0.024f, 0.02f));
        var field = InsetRect(rect, Math.Max(2, rect.Size.X / 14));
        var fieldRadius = Math.Min(field.Size.X, field.Size.Y) / 2;
        if (elementType == ElementType.Any)
        {
            FillElementBlendGradient(image, field, fieldRadius);
        }
        else
        {
            FillRoundedRect(image, field, fieldRadius, GetElementBackgroundColor(elementType));
        }

        if (texture is null)
        {
            return;
        }

        var glyphRect = InsetRect(rect, Math.Max(5, rect.Size.X / 10));
        if (TryDrawTexture(image, texture, glyphRect))
        {
            return;
        }

        throw new InvalidOperationException($"Element glyph texture could not be loaded for {elementType}.");
    }

    private static void DrawPowerIconsOverlapping(Image image, int count, Vector2I start, int size, int maxSpan, int preferredStep, Texture2D? powerIconOverride)
    {
        var step = GetOverlapStep(count, size, maxSpan, preferredStep);
        for (var index = 0; index < count; index++)
        {
            DrawPowerIcon(image, new Rect2I(start.X + index * step, start.Y, size, size), powerIconOverride);
        }
    }

    private static void DrawPowerIconsCentered(Image image, int count, int y, int size, int gap, Texture2D? powerIconOverride)
    {
        var iconCount = Math.Max(0, count);
        var step = GetOverlapStep(iconCount, size, 320, size + gap);
        var width = iconCount == 0 ? 0 : size + (iconCount - 1) * step;
        DrawPowerIconsOverlapping(image, iconCount, new Vector2I((PreviewWidth - width) / 2, y), size, 320, size + gap, powerIconOverride);
    }

    private static void DrawPowerIcon(Image image, Rect2I rect, Texture2D? powerIconOverride)
    {
        var radius = Math.Min(rect.Size.X, rect.Size.Y) / 2;
        FillRoundedRect(image, rect, radius, new Color(0.035f, 0.024f, 0.02f));
        var field = InsetRect(rect, Math.Max(2, rect.Size.X / 14));
        FillRoundedRect(image, field, Math.Min(field.Size.X, field.Size.Y) / 2, new Color(0.91f, 0.82f, 0.62f));
        if (powerIconOverride is not null)
        {
            var glyphRect = InsetRect(rect, Math.Max(5, rect.Size.X / 8));
            if (!TryDrawTexture(image, powerIconOverride, glyphRect))
            {
                throw new InvalidOperationException("Deck power glyph texture could not be loaded.");
            }
        }
    }

    private static int GetOverlapStep(int count, int size, int maxSpan, int preferredStep)
    {
        return count <= 1
            ? 0
            : Math.Max(1, Math.Min(preferredStep, (maxSpan - size) / (count - 1)));
    }

    private static void DrawTexture(
        Image target,
        Texture2D? texture,
        Rect2I targetRect,
        int cornerRadius = 0,
        CardImageScaleMode scaleMode = CardImageScaleMode.Stretch)
    {
        if (!TryDrawTexture(target, texture, targetRect, cornerRadius, scaleMode))
        {
            FillRoundedRect(target, targetRect, Math.Min(targetRect.Size.X, targetRect.Size.Y) / 2, new Color(0.85f, 0.82f, 0.72f));
        }
    }

    private static bool TryDrawTexture(
        Image target,
        Texture2D? texture,
        Rect2I targetRect,
        int cornerRadius = 0,
        CardImageScaleMode scaleMode = CardImageScaleMode.Stretch)
    {
        if (texture is null)
        {
            return false;
        }

        Image? source;
        lock (TextureReadLock)
        {
            source = texture.GetImage();
        }
        if (source is null)
        {
            return false;
        }

        try
        {
            DrawCardArtwork(target, source, targetRect, cornerRadius, scaleMode);
            return true;
        }
        finally
        {
            source.Dispose();
        }
    }

    private static bool TryDrawResourceTexture(Image target, string resourcePath, Rect2I targetRect)
    {
        Texture2D? texture;
        lock (TextureReadLock)
        {
            texture = ResourceLoader.Load<Texture2D>(resourcePath);
        }

        return TryDrawTexture(target, texture, targetRect);
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

    private static void DrawArrowRight(Image image, Rect2I rect)
    {
        if (TryDrawResourceTexture(image, ArrowRightIconPath, rect))
        {
            return;
        }

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
            CardType.Monster => new Color(0.35f, 0.365f, 0.39f),
            CardType.Terrain => new Color(0.651f, 0.471f, 0.259f),
            _ => new Color(0.22f, 0.22f, 0.25f)
        };
    }

    private static Color GetOuterInkColor(CardType cardType)
    {
        return cardType == CardType.Terrain
            ? new Color(0.031f, 0.024f, 0.016f)
            : new Color(0.015f, 0.017f, 0.02f);
    }

    private static Color GetFrameFieldColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.065f, 0.068f, 0.075f),
            CardType.Terrain => new Color(0.169f, 0.114f, 0.067f),
            _ => new Color(0.10f, 0.10f, 0.12f)
        };
    }

    private static Color GetInsetLineColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.18f, 0.19f, 0.21f),
            CardType.Terrain => new Color(0.427f, 0.294f, 0.169f),
            _ => new Color(0.28f, 0.28f, 0.31f)
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

    private static Color GetElementBackgroundColor(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Grass => new Color(0.73f, 0.84f, 0.69f),
            ElementType.Flame => new Color(0.88f, 0.67f, 0.60f),
            ElementType.Water => new Color(0.66f, 0.82f, 0.89f),
            // A warm violet-gray blend keeps Any distinct from neutral while balancing all three elements.
            ElementType.Any => new Color(0.63f, 0.58f, 0.68f),
            _ => new Color(0.75f, 0.75f, 0.70f)
        };
    }

    private static void FillElementBlendGradient(Image image, Rect2I rect, int radius)
    {
        rect = ScaleRect(image, rect);
        radius = ScaleRadius(image, radius);
        var flame = new Color(0.82f, 0.43f, 0.32f);
        var grass = new Color(0.55f, 0.70f, 0.43f);
        var water = new Color(0.38f, 0.64f, 0.75f);

        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                if (!IsInsideRoundedRect(x, y, rect, radius))
                {
                    continue;
                }

                var progress = rect.Size.X <= 1
                    ? 0f
                    : (float)(x - rect.Position.X) / (rect.Size.X - 1);
                var color = progress < 0.5f
                    ? flame.Lerp(grass, progress * 2f)
                    : grass.Lerp(water, (progress - 0.5f) * 2f);
                image.SetPixel(x, y, color);
            }
        }
    }

    private static bool IsInsideRoundedRect(int x, int y, Rect2I rect, int radius)
    {
        var localX = x - rect.Position.X;
        var localY = y - rect.Position.Y;
        var nearestX = Math.Clamp(localX, radius, rect.Size.X - radius);
        var nearestY = Math.Clamp(localY, radius, rect.Size.Y - radius);
        var deltaX = localX - nearestX;
        var deltaY = localY - nearestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }

    private static void DrawOutlinedPanel(Image image, Rect2I rect, int radius, Color outline, Color fill, int thickness)
    {
        FillRoundedRect(image, rect, radius, outline);
        FillRoundedRect(image, InsetRect(rect, thickness), Math.Max(1, radius - thickness), fill);
    }

    private static Rect2I InsetRect(Rect2I rect, int inset)
    {
        return new Rect2I(
            rect.Position.X + inset,
            rect.Position.Y + inset,
            Math.Max(1, rect.Size.X - inset * 2),
            Math.Max(1, rect.Size.Y - inset * 2));
    }

    private static void ApplyRoundedAlphaMask(Image image, int radius)
    {
        radius = Math.Min(radius, Math.Min(image.GetWidth(), image.GetHeight()) / 2);
        if (radius < 1)
        {
            return;
        }

        var maxX = image.GetWidth() - 1;
        var maxY = image.GetHeight() - 1;
        var radiusSquared = radius * radius;
        for (var y = 0; y < image.GetHeight(); y++)
        {
            for (var x = 0; x < image.GetWidth(); x++)
            {
                var cornerX = x < radius ? radius : x > maxX - radius ? maxX - radius : x;
                var cornerY = y < radius ? radius : y > maxY - radius ? maxY - radius : y;
                var dx = x - cornerX;
                var dy = y - cornerY;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    continue;
                }

                var color = image.GetPixel(x, y);
                image.SetPixel(x, y, new Color(color.R, color.G, color.B, 0));
            }
        }
    }

    private static void DrawMonsterTierDiamonds(Image image, int tier)
    {
        if (tier is < 1 or > 3)
        {
            return;
        }

        var startY = 128 - (tier - 1) * 14;
        for (var index = 0; index < tier; index++)
        {
            var center = new Vector2I(550, startY + index * 28);
            FillDiamond(image, center, 11, 15, new Color(0.09f, 0.067f, 0.051f));
            FillDiamond(image, center, 8, 12, new Color(0.847f, 0.537f, 0.392f));
            FillDiamond(image, center, 4, 7, new Color(0.19f, 0.20f, 0.22f));
        }
    }

    private static void FillDiamond(Image image, Vector2I center, int radiusX, int radiusY, Color color)
    {
        FillPolygon(
            image,
            [
                new Vector2I(center.X, center.Y - radiusY),
                new Vector2I(center.X + radiusX, center.Y),
                new Vector2I(center.X, center.Y + radiusY),
                new Vector2I(center.X - radiusX, center.Y)
            ],
            color);
    }

    private static void FillPolygon(Image image, Vector2I[] points, Color color)
    {
        var scaledPoints = points.Select(point => ScalePoint(image, point)).ToArray();
        var minX = Math.Max(0, scaledPoints.Min(point => point.X));
        var maxX = Math.Min(image.GetWidth() - 1, scaledPoints.Max(point => point.X));
        var minY = Math.Max(0, scaledPoints.Min(point => point.Y));
        var maxY = Math.Min(image.GetHeight() - 1, scaledPoints.Max(point => point.Y));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (IsPointInPolygon(new Vector2(x + 0.5f, y + 0.5f), scaledPoints))
                {
                    BlendPixel(image, x, y, color);
                }
            }
        }
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2I[] polygon)
    {
        var inside = false;
        for (var index = 0; index < polygon.Length; index++)
        {
            var previous = polygon[(index + polygon.Length - 1) % polygon.Length];
            var current = polygon[index];
            if ((current.Y > point.Y) == (previous.Y > point.Y))
            {
                continue;
            }

            var edgeX = (previous.X - current.X) * (point.Y - current.Y) / (previous.Y - current.Y) + current.X;
            if (point.X < edgeX)
            {
                inside = !inside;
            }
        }

        return inside;
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

    private static Vector2I ScalePoint(Image image, Vector2I point)
    {
        if (image.GetWidth() == PreviewWidth && image.GetHeight() == PreviewHeight)
        {
            return point;
        }

        return new Vector2I(
            Mathf.RoundToInt(point.X * image.GetWidth() / (float)PreviewWidth),
            Mathf.RoundToInt(point.Y * image.GetHeight() / (float)PreviewHeight));
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
