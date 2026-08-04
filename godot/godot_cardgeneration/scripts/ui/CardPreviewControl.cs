using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public sealed record CardPreviewRenderRequest(
    CardResource Card,
    bool ShowBack,
    Vector2I RenderSize,
    IReadOnlyDictionary<ElementType, Texture2D>? ElementIconOverrides = null,
    Texture2D? PowerIconOverride = null);

[GlobalClass]
public partial class CardPreviewControl : TextureRect
{
    private const string PreviewScenePath = "res://scenes/card_preview/card_preview.tscn";
    private const int MaxQueuedRendersPerFrame = 1;
    private const int MaxCachedTextures = 256;

    private static readonly Queue<CardPreviewControl> RenderQueue = new();
    private static readonly Dictionary<string, Texture2D> TextureCache = new();
    private static readonly Queue<string> TextureCacheOrder = new();
    private static PackedScene? _previewScene;
    private static ulong _lastRenderFrame;
    private static int _renderCountThisFrame;

    private CardResource? _card;
    private bool _showBack;
    private bool _deferRender;
    private bool _useCache = true;
    private bool _isQueued;
    private bool _waitingForVisibility;
    private int _renderVersion;
    private Vector2I _renderSize = new(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight);
    private IReadOnlyDictionary<ElementType, Texture2D>? _elementIconOverrides;
    private Texture2D? _powerIconOverride;

    public static CardPreviewControl Create(
        CardResource? card = null,
        bool showBack = false,
        Vector2? minimumSize = null,
        Vector2I? renderSize = null,
        bool deferRender = false,
        bool useCache = true,
        IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides = null,
        Texture2D? powerIconOverride = null)
    {
        _previewScene ??= ResourceLoader.Load<PackedScene>(PreviewScenePath);
        var preview = _previewScene?.Instantiate<CardPreviewControl>() ?? new CardPreviewControl();

        if (minimumSize.HasValue)
        {
            preview.CustomMinimumSize = minimumSize.Value;
        }

        preview._renderSize = renderSize ?? ToRenderSize(minimumSize);
        preview._deferRender = deferRender;
        preview._useCache = useCache;
        preview._elementIconOverrides = elementIconOverrides;
        preview._powerIconOverride = powerIconOverride;
        preview.SetCard(card, showBack);
        return preview;
    }

    public static CardPreviewRenderRequest CreateRenderRequest(
        CardResource card,
        bool showBack,
        Vector2I renderSize,
        IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides = null,
        Texture2D? powerIconOverride = null)
    {
        return new CardPreviewRenderRequest(card, showBack, renderSize, elementIconOverrides, powerIconOverride);
    }

    public static string GetCacheKey(CardPreviewRenderRequest request)
    {
        return string.Join(
            '|',
            request.Card.Id,
            request.Card.CardType,
            request.Card.Element?.ElementType.ToString() ?? "missing-element",
            GetCardContentSignature(request.Card),
            GetElementIconOverridesSignature(request.ElementIconOverrides),
            GetTextureSignature(request.PowerIconOverride),
            request.ShowBack ? "back" : "front",
            request.RenderSize.X,
            request.RenderSize.Y);
    }

    public static bool IsCached(CardPreviewRenderRequest request)
    {
        return TextureCache.ContainsKey(GetCacheKey(request));
    }

    public static Image RenderImage(CardPreviewRenderRequest request)
    {
        return request.ShowBack
            ? CardImageRenderer.RenderBack(
                request.Card.CardType,
                request.Card.BackImageTexture,
                request.Card.BackImageSourcePath,
                request.Card.BackImageScaleMode,
                request.RenderSize)
            : CardImageRenderer.Render(request.Card, request.RenderSize, request.ElementIconOverrides, request.PowerIconOverride);
    }

    public static void CacheRenderedImage(CardPreviewRenderRequest request, Image image)
    {
        var key = GetCacheKey(request);
        if (TextureCache.ContainsKey(key))
        {
            return;
        }

        TextureCache[key] = ImageTexture.CreateFromImage(image);
        TextureCacheOrder.Enqueue(key);
        while (TextureCache.Count > MaxCachedTextures && TextureCacheOrder.Count > 0)
        {
            TextureCache.Remove(TextureCacheOrder.Dequeue());
        }
    }

    public override void _Ready()
    {
        if (MouseFilter != MouseFilterEnum.Ignore)
        {
            MouseFilter = MouseFilterEnum.Stop;
        }

        TooltipText = "Double-click to open large preview.";
        SetProcess(_isQueued || _waitingForVisibility);
    }

    public override void _ExitTree()
    {
        _isQueued = false;
    }

    public void SetCard(CardResource? card, bool showBack = false)
    {
        _card = card;
        _showBack = showBack;
        _renderVersion++;
        RefreshPreview();
    }

    public void SetShowBack(bool showBack)
    {
        _showBack = showBack;
        _renderVersion++;
        RefreshPreview();
    }

    public override void _Process(double delta)
    {
        ProcessRenderQueue();

        if (_waitingForVisibility && IsRenderVisible())
        {
            _waitingForVisibility = false;
            EnqueueRender();
        }

        if (!_isQueued && !_waitingForVisibility)
        {
            SetProcess(false);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true })
        {
            ShowFullscreenPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_card is null)
        {
            Texture = null;
            return;
        }

        if (_useCache && TryGetCachedTexture(out var cachedTexture))
        {
            Texture = cachedTexture;
            return;
        }

        if (_deferRender)
        {
            Texture ??= CreatePlaceholderTexture(_renderSize);
            RequestVisibleRender();
            return;
        }

        RenderNow(_renderVersion);
    }

    private void RequestVisibleRender()
    {
        if (_isQueued)
        {
            return;
        }

        _waitingForVisibility = true;
        SetProcess(true);
    }

    private void EnqueueRender()
    {
        if (_isQueued)
        {
            return;
        }

        _isQueued = true;
        RenderQueue.Enqueue(this);
        SetProcess(true);
    }

    private bool IsRenderVisible()
    {
        if (!IsInsideTree() || !IsVisibleInTree())
        {
            return false;
        }

        var rect = GetGlobalRect();
        if (!rect.Intersects(GetViewport().GetVisibleRect()))
        {
            return false;
        }

        var current = GetParent();
        while (current is not null)
        {
            if (current is ScrollContainer scrollContainer && !rect.Intersects(scrollContainer.GetGlobalRect()))
            {
                return false;
            }

            current = current.GetParent();
        }

        return true;
    }

    private static void ProcessRenderQueue()
    {
        var frame = Engine.GetProcessFrames();
        if (_lastRenderFrame != frame)
        {
            _lastRenderFrame = frame;
            _renderCountThisFrame = 0;
        }

        while (_renderCountThisFrame < MaxQueuedRendersPerFrame && RenderQueue.Count > 0)
        {
            var preview = RenderQueue.Dequeue();
            if (!IsInstanceValid(preview) || !preview._isQueued)
            {
                continue;
            }

            preview._isQueued = false;
            preview.RenderNow(preview._renderVersion);
            preview.SetProcess(false);
            _renderCountThisFrame++;
        }
    }

    private void RenderNow(int renderVersion)
    {
        if (_card is null || renderVersion != _renderVersion)
        {
            return;
        }

        if (_useCache && TryGetCachedTexture(out var cachedTexture))
        {
            Texture = cachedTexture;
            return;
        }

        using var image = RenderImage(_renderSize);
        if (_useCache)
        {
            var request = GetRenderRequest();
            CacheRenderedImage(request, image);
            Texture = TextureCache[GetCacheKey(request)];
        }
        else
        {
            Texture = ImageTexture.CreateFromImage(image);
        }
    }

    private Image RenderImage(Vector2I size)
    {
        if (_card is null)
        {
            return Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        }

        var image = _showBack
            ? CardImageRenderer.RenderBack(
                _card.CardType,
                _card.BackImageTexture,
                _card.BackImageSourcePath,
                _card.BackImageScaleMode,
                size)
            : CardImageRenderer.Render(_card, size, _elementIconOverrides, _powerIconOverride);

        return image;
    }

    private bool TryGetCachedTexture(out Texture2D texture)
    {
        texture = null!;
        if (_card is null || !TextureCache.TryGetValue(GetCacheKey(GetRenderRequest()), out var cachedTexture))
        {
            return false;
        }

        texture = cachedTexture;
        return true;
    }

    private CardPreviewRenderRequest GetRenderRequest()
    {
        return new CardPreviewRenderRequest(_card!, _showBack, _renderSize, _elementIconOverrides, _powerIconOverride);
    }

    private static string GetCardContentSignature(CardResource card)
    {
        var common = string.Join(
            ':',
            GetSourceSignature(card.CardImageSourcePath),
            GetTextureSignature(card.CardImageTexture),
            card.ImageScaleMode,
            GetTextureSignature(card.BackImageTexture),
            GetSourceSignature(card.BackImageSourcePath),
            card.BackImageScaleMode,
            GetTextureSignature(card.Element?.IconTexture),
            GetSourceSignature(CardImageRenderer.PowerIconPath),
            GetSourceSignature(CardImageRenderer.ArrowRightIconPath));

        return card switch
        {
            MonsterCardResource monster => string.Join(
                ':',
                common,
                monster.Tier,
                monster.BasePower,
                GetAmountsSignature(monster.Requirements),
                string.Join(';', (monster.PowerBonuses ?? Array.Empty<PowerBonusResource>())
                    .Select(bonus => $"{GetAmountsSignature(bonus.Requirements)}>{bonus.PowerGain}")),
                monster.Effect?.EffectId ?? string.Empty,
                monster.Effect?.RulesText ?? string.Empty),
            TerrainCardResource terrain => $"{common}:{GetAmountsSignature(terrain.ProducedResources)}",
            _ => common
        };
    }

    private static string GetTextureSignature(Texture2D? texture)
    {
        return texture is null
            ? "0"
            : $"{texture.GetInstanceId()}@{GetSourceSignature(texture.ResourcePath)}";
    }

    private static string GetElementIconOverridesSignature(IReadOnlyDictionary<ElementType, Texture2D>? elementIconOverrides)
    {
        if (elementIconOverrides is null)
        {
            return "standalone";
        }

        return string.Join(
            ',',
            Enum.GetValues<ElementType>()
                .Select(elementType => $"{elementType}={GetTextureSignature(elementIconOverrides.TryGetValue(elementType, out var texture) ? texture : null)}"));
    }

    private static string GetSourceSignature(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return string.Empty;
        }

        if (FileAccess.FileExists(sourcePath))
        {
            return $"{sourcePath}@{FileAccess.GetModifiedTime(sourcePath)}";
        }

        var globalPath = ProjectPaths.ToGlobalPath(sourcePath);
        return System.IO.File.Exists(globalPath)
            ? $"{sourcePath}@{System.IO.File.GetLastWriteTimeUtc(globalPath).Ticks}"
            : $"{sourcePath}@missing";
    }

    private static string GetAmountsSignature(ResourceAmount[] amounts)
    {
        return string.Join(
            ',',
            (amounts ?? Array.Empty<ResourceAmount>())
                .Select(amount => $"{amount.Element?.ElementType.ToString() ?? "missing"}={amount.Amount}"));
    }

    private static Vector2I ToRenderSize(Vector2? minimumSize)
    {
        if (!minimumSize.HasValue || minimumSize.Value.X <= 0 || minimumSize.Value.Y <= 0)
        {
            return new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight);
        }

        return new Vector2I(Mathf.RoundToInt(minimumSize.Value.X), Mathf.RoundToInt(minimumSize.Value.Y));
    }

    private static ImageTexture CreatePlaceholderTexture(Vector2I size)
    {
        using var image = Image.CreateEmpty(Mathf.Max(1, size.X), Mathf.Max(1, size.Y), false, Image.Format.Rgba8);
        image.Fill(CardImageRenderer.PlaceholderColor);
        return ImageTexture.CreateFromImage(image);
    }

    private void ShowFullscreenPreview()
    {
        if (_card is null)
        {
            return;
        }

        var popup = new PopupPanel
        {
            Title = _showBack ? $"{_card.Id} back" : $"{_card.Id} front"
        };
        AddChild(popup);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        popup.AddChild(margin);

        using var image = RenderImage(new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight));
        var preview = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            CustomMinimumSize = new Vector2(520, 728),
            ExpandMode = ExpandModeEnum.FitWidthProportional,
            StretchMode = StretchModeEnum.KeepAspectCentered
        };
        margin.AddChild(preview);

        popup.PopupCentered(new Vector2I(620, 820));
        popup.PopupHide += popup.QueueFree;
    }
}
