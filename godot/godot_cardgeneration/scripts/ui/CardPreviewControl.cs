using System.Collections.Generic;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

[GlobalClass]
public partial class CardPreviewControl : TextureRect
{
    private const string PreviewScenePath = "res://scenes/card_preview/card_preview.tscn";
    private const int MaxQueuedRendersPerFrame = 1;

    private static readonly Queue<CardPreviewControl> RenderQueue = new();
    private static readonly Dictionary<string, Texture2D> TextureCache = new();
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

    public static CardPreviewControl Create(
        CardResource? card = null,
        bool showBack = false,
        Vector2? minimumSize = null,
        Vector2I? renderSize = null,
        bool deferRender = false,
        bool useCache = true)
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
        preview.SetCard(card, showBack);
        return preview;
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
            Texture ??= CreatePlaceholderTexture(_card.CardType, _renderSize);
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

        var image = RenderImage(_renderSize);
        var texture = ImageTexture.CreateFromImage(image);
        Texture = texture;

        if (_useCache)
        {
            TextureCache[GetCacheKey()] = texture;
        }
    }

    private Image RenderImage(Vector2I size)
    {
        if (_card is null)
        {
            return Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        }

        var image = _showBack
            ? CardImageRenderer.RenderBack(_card.CardType, _card.BackImageTexture, size)
            : CardImageRenderer.Render(_card, size);

        return image;
    }

    private bool TryGetCachedTexture(out Texture2D texture)
    {
        return TextureCache.TryGetValue(GetCacheKey(), out texture!);
    }

    private string GetCacheKey()
    {
        if (_card is null)
        {
            return "empty";
        }

        return string.Join('|', _card.Id, _card.CardType, _showBack ? "back" : "front", _renderSize.X, _renderSize.Y);
    }

    private static Vector2I ToRenderSize(Vector2? minimumSize)
    {
        if (!minimumSize.HasValue || minimumSize.Value.X <= 0 || minimumSize.Value.Y <= 0)
        {
            return new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight);
        }

        return new Vector2I(Mathf.RoundToInt(minimumSize.Value.X), Mathf.RoundToInt(minimumSize.Value.Y));
    }

    private static ImageTexture CreatePlaceholderTexture(CardType cardType, Vector2I size)
    {
        var image = Image.CreateEmpty(Mathf.Max(1, size.X), Mathf.Max(1, size.Y), false, Image.Format.Rgba8);
        image.Fill(cardType switch
        {
            CardType.Monster => new Color(0.18f, 0.08f, 0.09f),
            CardType.Terrain => new Color(0.13f, 0.16f, 0.10f),
            CardType.King => new Color(0.15f, 0.11f, 0.20f),
            _ => new Color(0.10f, 0.10f, 0.12f)
        });
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

        var preview = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(RenderImage(new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight))),
            CustomMinimumSize = new Vector2(520, 728),
            ExpandMode = ExpandModeEnum.FitWidthProportional,
            StretchMode = StretchModeEnum.KeepAspectCentered
        };
        margin.AddChild(preview);

        popup.PopupCentered(new Vector2I(620, 820));
        popup.PopupHide += popup.QueueFree;
    }
}
