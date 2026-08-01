using System;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Rendering;

public static class CardImageRenderer
{
    public const int PreviewWidth = 750;
    public const int PreviewHeight = 1050;

    public static Image Render(CardResource card)
    {
        var image = Image.CreateEmpty(PreviewWidth, PreviewHeight, false, Image.Format.Rgba8);
        image.Fill(new Color(0, 0, 0, 0));

        DrawCardBase(image, card.CardType);
        DrawCardImage(image, card);
        DrawCardPanels(image, card);

        return image;
    }

    public static Image RenderBack(CardType cardType, Texture2D? backImageTexture = null)
    {
        var image = Image.CreateEmpty(PreviewWidth, PreviewHeight, false, Image.Format.Rgba8);
        image.Fill(new Color(0, 0, 0, 0));

        DrawCardBase(image, cardType);
        DrawCardBackImage(image, cardType, backImageTexture);

        return image;
    }

    public static Image RenderResized(CardResource card, int width, int height)
    {
        var image = Render(card);
        image.Resize(width, height, Image.Interpolation.Lanczos);
        return image;
    }

    public static Image RenderBackResized(CardType cardType, Texture2D? backImageTexture, int width, int height)
    {
        var image = RenderBack(cardType, backImageTexture);
        image.Resize(width, height, Image.Interpolation.Lanczos);
        return image;
    }

    private static void DrawCardBase(Image image, CardType cardType)
    {
        FillRoundedRect(image, new Rect2I(20, 20, 710, 1010), 48, new Color(0.02f, 0.02f, 0.025f));
        FillRoundedRect(image, new Rect2I(38, 38, 674, 974), 40, GetBaseColor(cardType));
        FillRoundedRect(image, new Rect2I(58, 58, 634, 934), 30, new Color(0.035f, 0.03f, 0.035f));
    }

    private static void DrawCardImage(Image image, CardResource card)
    {
        var cardImageRect = new Rect2I(62, 62, 626, 926);
        if (card.CardImageTexture is null)
        {
            DrawPlaceholderCardImage(image, card, cardImageRect);
            return;
        }

        DrawTexture(image, card.CardImageTexture, cardImageRect);
    }

    private static void DrawCardBackImage(Image image, CardType cardType, Texture2D? backImageTexture)
    {
        var backImageRect = new Rect2I(62, 62, 626, 926);
        if (backImageTexture is not null)
        {
            DrawTexture(image, backImageTexture, backImageRect);
            return;
        }

        FillRoundedRect(image, backImageRect, 28, GetMutedBaseColor(cardType));

        var center = new Vector2I(375, 525);
        var accentColor = GetBaseColor(cardType);
        FillRoundedRect(image, new Rect2I(center.X - 170, center.Y - 170, 340, 340), 170, new Color(0.02f, 0.015f, 0.018f, 0.88f));
        FillRoundedRect(image, new Rect2I(center.X - 135, center.Y - 135, 270, 270), 135, new Color(accentColor.R, accentColor.G, accentColor.B, 0.32f));
        FillRoundedRect(image, new Rect2I(center.X - 74, center.Y - 230, 148, 460), 74, new Color(0.04f, 0.018f, 0.02f, 0.92f));
        FillRoundedRect(image, new Rect2I(center.X - 92, center.Y - 92, 184, 184), 92, GetBaseColor(cardType));
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
            case KingCardResource king:
                DrawKingPanels(image, king);
                break;
        }
    }

    private static void DrawMonsterPanels(Image image, MonsterCardResource card)
    {
        var requirementWidth = Math.Max(150, 52 + card.Requirements.Length * 70);
        var requirementPanel = new Rect2I(95, 85, requirementWidth, 70);
        FillRoundedRect(image, requirementPanel, 30, new Color(0.035f, 0.02f, 0.02f, 0.78f));
        DrawResourceAmounts(image, card.Requirements, new Vector2I(116, 94), 52, 8);

        var bonusPanel = new Rect2I(92, 760, 566, 190);
        FillRoundedRect(image, bonusPanel, 26, new Color(0.025f, 0.018f, 0.018f, 0.82f));

        DrawPowerIcons(image, card.BasePower, new Vector2I(335, 776), 64, 6);

        var lineY = 852;
        foreach (var bonus in card.PowerBonuses)
        {
            var nextX = DrawResourceAmounts(image, bonus.Requirements, new Vector2I(165, lineY), 42, 6);
            DrawArrowRight(image, new Rect2I(nextX + 18, lineY - 6, 70, 54));
            DrawPowerIcons(image, bonus.PowerGain, new Vector2I(nextX + 112, lineY), 42, 6);
            lineY += 50;
        }
    }

    private static void DrawTerrainPanels(Image image, TerrainCardResource card)
    {
        FillRoundedRect(image, new Rect2I(72, 72, 606, 110), 24, new Color(0.02f, 0.025f, 0.02f, 0.72f));
        DrawResourceAmounts(image, card.ProducedResources, new Vector2I(92, 88), 70, 10);
    }

    private static void DrawKingPanels(Image image, KingCardResource card)
    {
        FillRoundedRect(image, new Rect2I(92, 790, 566, 160), 26, new Color(0.035f, 0.028f, 0.045f, 0.78f));
        FillRoundedRect(image, new Rect2I(118, 820, 60, 60), 30, new Color(0.88f, 0.76f, 0.36f));
        DrawResourceAmounts(image, card.QuestRequirements, new Vector2I(205, 820), 52, 8);
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

    private static void DrawPowerIcons(Image image, int count, Vector2I start, int size, int gap)
    {
        for (var index = 0; index < count; index++)
        {
            DrawPowerFallback(image, new Rect2I(start.X + index * (size + gap), start.Y, size, size));
        }
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

        source.Convert(Image.Format.Rgba8);
        source.Resize(targetRect.Size.X, targetRect.Size.Y, Image.Interpolation.Lanczos);
        target.BlendRect(source, new Rect2I(Vector2I.Zero, source.GetSize()), targetRect.Position);
    }

    private static void DrawPlaceholderCardImage(Image image, CardResource card, Rect2I rect)
    {
        FillRoundedRect(image, rect, 28, GetMutedBaseColor(card.CardType));

        if (card.CardType == CardType.Monster)
        {
            FillRoundedRect(image, new Rect2I(rect.Position.X + 170, rect.Position.Y + 120, 286, 286), 143, new Color(0.82f, 0.22f, 0.12f, 0.18f));
            FillRoundedRect(image, new Rect2I(rect.Position.X + 260, rect.Position.Y + 280, 110, 360), 55, new Color(0.07f, 0.025f, 0.025f, 0.92f));
            DrawElementFallback(image, card.Element?.ElementType ?? ElementType.Neutral, new Rect2I(rect.Position.X + 281, rect.Position.Y + 366, 56, 56));
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

    private static Color GetBaseColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.56f, 0.08f, 0.09f),
            CardType.Terrain => new Color(0.58f, 0.40f, 0.22f),
            CardType.King => new Color(0.62f, 0.48f, 0.16f),
            _ => new Color(0.22f, 0.22f, 0.25f)
        };
    }

    private static Color GetMutedBaseColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new Color(0.18f, 0.05f, 0.05f),
            CardType.Terrain => new Color(0.12f, 0.19f, 0.10f),
            CardType.King => new Color(0.16f, 0.10f, 0.22f),
            _ => new Color(0.10f, 0.10f, 0.12f)
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
