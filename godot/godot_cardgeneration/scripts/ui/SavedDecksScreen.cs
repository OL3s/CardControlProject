using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedDecksScreen : CardToolScreen
{
    private const int PreviewBatchSize = 10;

    private IReadOnlyList<CardDeckResource> _decks = Array.Empty<CardDeckResource>();
    private PopupMenu _createDeckMenu = null!;
    private Label _details = null!;
    private FileDialog _importDialog = null!;
    private ProgressBar _preloadProgress = null!;
    private Label _preloadLabel = null!;
    private CancellationTokenSource? _preloadCancellation;
    private int _preloadVersion;
    public event Action<CardDeckResource?>? EditDeckRequested;
    public event Action<CardDeckResource?>? NewDeckRequested;
    public event Action<CardDeckResource>? PreviewDeckRequested;

    public override void _Ready()
    {
        BuildUi();
    }

    public override void _ExitTree()
    {
        _preloadCancellation?.Cancel();
        _preloadCancellation?.Dispose();
        _preloadCancellation = null;
    }

    private void BuildUi()
    {
        _decks = CardToolService.LoadAllDecks();
        var content = BuildScreen("Decks", "Browse decks, inspect their card count, edit them, or export with the saved defaults.");

        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolbar);
        AddIconButton(toolbar, DeckIconPath, "Create deck", ShowCreateDeckMenu);
        AddIconButton(toolbar, ImportIconPath, "Import deck resource", OpenImportDialog);
        AddIconButton(toolbar, CheckIconPath, "Validate decks", ValidateDecks);
        AddIconButton(toolbar, RefreshIconPath, "Refresh", RefreshDefaultsAndBuildUi);
        AddResourceDialogs();

        _preloadProgress = new ProgressBar
        {
            Visible = false,
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        content.AddChild(_preloadProgress);
        _preloadLabel = new Label
        {
            Visible = false,
            Text = string.Empty,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        content.AddChild(_preloadLabel);

        _createDeckMenu = new PopupMenu();
        _createDeckMenu.AddItem("New Empty Deck", 0);
        _createDeckMenu.AddItem("Default 52-Card Preset", 1);
        _createDeckMenu.IdPressed += id => RunGuiAction("Create deck menu choice", () => OnCreateDeckMenuPressed(id), $"id={id}");
        AddChild(_createDeckMenu);

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 16);
        content.AddChild(body);

        var list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        list.AddThemeConstantOverride("separation", 10);
        body.AddChild(list);

        _details = new Label
        {
            Text = $"Deck Count: {_decks.Count}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        list.AddChild(_details);

        if (_decks.Count == 0)
        {
            list.AddChild(new Label
            {
                Text = "No decks found in default resources or user://resources/decks.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var deck in _decks)
        {
            AddDeckRow(list, deck);
        }

        SetStatus($"Loaded {_decks.Count} saved deck(s).");
    }

    private void RefreshDefaultsAndBuildUi()
    {
        var defaultResult = CardToolService.EnsureDefaultResources();
        BuildUi();
        SetStatus(defaultResult.Message, !defaultResult.Success);
    }

    private void AddDeckRow(VBoxContainer list, CardDeckResource deck)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        list.AddChild(panel);

        var rowMargin = new MarginContainer();
        rowMargin.AddThemeConstantOverride("margin_left", 10);
        rowMargin.AddThemeConstantOverride("margin_right", 10);
        rowMargin.AddThemeConstantOverride("margin_top", 8);
        rowMargin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(rowMargin);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 12);
        rowMargin.AddChild(row);

        var info = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        info.AddThemeConstantOverride("separation", 6);
        row.AddChild(info);

        info.AddChild(new Label
        {
            Text = deck.Id,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        AddDeckStatsRow(info, deck);

        var buttons = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Alignment = BoxContainer.AlignmentMode.End
        };
        buttons.AddThemeConstantOverride("separation", 8);
        row.AddChild(buttons);
        AddIconButton(buttons, PreviewIconPath, "Preview full deck", () => StartDeckPreload(deck, openEditor: false, isNewDeck: false));
        AddIconButton(buttons, EditIconPath, "Edit", () => StartDeckPreload(deck, openEditor: true, isNewDeck: false));
        AddIconButton(buttons, CopyIconPath, "Duplicate", () => DuplicateDeck(deck));
        AddIconButton(buttons, DeleteIconPath, "Delete", () => DeleteDeck(deck));
    }

    private void ShowDeck(CardDeckResource deck)
    {
        _details.Text = $"Deck Count: {_decks.Count}";
    }

    private void AddDeckStatsRow(VBoxContainer parent, CardDeckResource deck)
    {
        var stats = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stats.AddThemeConstantOverride("separation", 12);
        parent.AddChild(stats);

        AddIconCount(stats, CardCountIconPath, "Cards", GetCardCount(deck));
        AddIconCount(stats, MonsterTypeIconPath, "Monsters", GetCardTypeCount(deck, CardType.Monster));
        AddIconCount(stats, TerrainTypeIconPath, "Terrain", GetCardTypeCount(deck, CardType.Terrain));
    }

    private static void AddIconCount(HBoxContainer parent, string iconPath, string tooltip, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var item = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            TooltipText = tooltip
        };
        item.AddThemeConstantOverride("separation", 4);
        parent.AddChild(item);

        item.AddChild(new TextureRect
        {
            Texture = LoadIcon(iconPath),
            CustomMinimumSize = new Vector2(24, 24),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = tooltip
        });
        item.AddChild(new Label
        {
            Text = $"x {count}",
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = tooltip
        });
    }

    private void ShowCreateDeckMenu()
    {
        _createDeckMenu.PopupCentered(new Vector2I(260, 96));
    }

    private void OnCreateDeckMenuPressed(long id)
    {
        var deck = id == 1
            ? CardToolService.CreateDefault52CardDeck()
            : CardToolService.CreateEmptyDeck();
        StartDeckPreload(deck, openEditor: true, isNewDeck: true);
    }

    private async void StartDeckPreload(CardDeckResource deck, bool openEditor, bool isNewDeck)
    {
        _preloadCancellation?.Cancel();
        _preloadCancellation?.Dispose();
        _preloadCancellation = new CancellationTokenSource();
        var cancellationToken = _preloadCancellation.Token;
        var preloadVersion = ++_preloadVersion;
        SetPreloadBusy(true);

        try
        {
            var requests = (openEditor ? BuildEditorRequests(deck) : BuildPreviewRequests(deck))
                .GroupBy(CardPreviewControl.GetCacheKey)
                .Select(group => group.First())
                .Where(request => !CardPreviewControl.IsCached(request))
                .ToArray();
            if (requests.Length == 0)
            {
                ApplyPreloadProgress(1, 1, "Preview thumbnails are already cached.");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            else
            {
                var totalWork = requests.Length * 2;
                ApplyPreloadProgress(0, totalWork, $"Preparing {requests.Length} compact preview thumbnail(s)...");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var rendered = await Task.Run(
                    () => RenderPreviewBatches(requests, totalWork, preloadVersion, cancellationToken),
                    cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cached = 0;
                    foreach (var result in rendered)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        CardPreviewControl.CacheRenderedImage(result.Request, result.Image);
                        cached++;
                        ApplyPreloadProgress(requests.Length + cached, totalWork, $"Cached thumbnail {cached}/{requests.Length}.");
                    }
                }
                finally
                {
                    foreach (var result in rendered)
                    {
                        result.Image.Dispose();
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (preloadVersion != _preloadVersion || !IsInsideTree())
            {
                return;
            }

            SetStatus($"Loaded compact previews for '{deck.Id}'.");
            if (openEditor)
            {
                if (isNewDeck)
                {
                    NewDeckRequested?.Invoke(deck);
                }
                else
                {
                    EditDeckRequested?.Invoke(deck);
                }
            }
            else
            {
                PreviewDeckRequested?.Invoke(deck);
            }
        }
        catch (OperationCanceledException)
        {
            if (IsInsideTree())
            {
                SetStatus("Deck preview loading was cancelled.");
            }
        }
        catch (Exception exception)
        {
            AppLogger.GuiError("Could not preload deck preview thumbnails.", exception);
            if (IsInsideTree())
            {
                SetStatus($"Could not load deck previews: {exception.Message}", true);
            }
        }
        finally
        {
            if (preloadVersion == _preloadVersion && IsInsideTree())
            {
                SetPreloadBusy(false);
            }
        }
    }

    private IReadOnlyList<CardPreviewRenderRequest> BuildPreviewRequests(CardDeckResource deck)
    {
        var requests = new List<CardPreviewRenderRequest>();
        var elementOverrides = deck.GetElementIconOverrides();
        foreach (var entry in deck.Entries ?? Array.Empty<CardDeckEntryResource>())
        {
            if (entry.Card is not null && entry.Count > 0)
            {
                requests.Add(CardPreviewControl.CreateRenderRequest(entry.Card, false, DeckPreviewScreen.CardPreviewRenderSize, elementOverrides, deck.PowerIconTexture));
            }
        }

        var monsterBack = new MonsterCardResource { Id = "monster_back_preview", BackImageTexture = deck.MonsterBackImageTexture };
        var terrainBack = new TerrainCardResource { Id = "terrain_back_preview", BackImageTexture = deck.TerrainBackImageTexture };
        requests.Add(CardPreviewControl.CreateRenderRequest(monsterBack, true, DeckPreviewScreen.BackPreviewRenderSize));
        requests.Add(CardPreviewControl.CreateRenderRequest(terrainBack, true, DeckPreviewScreen.BackPreviewRenderSize));
        return requests;
    }

    private IReadOnlyList<CardPreviewRenderRequest> BuildEditorRequests(CardDeckResource deck)
    {
        var cards = CardToolService.LoadAllCards()
            .Concat((deck.Entries ?? Array.Empty<CardDeckEntryResource>())
                .Where(entry => entry.Card is not null)
                .Select(entry => entry.Card!));
        var elementOverrides = deck.GetElementIconOverrides();
        return cards
            .Select(card => CardPreviewControl.CreateRenderRequest(card, false, DeckEditorScreen.CardThumbnailRenderSize, elementOverrides, deck.PowerIconTexture))
            .ToArray();
    }

    private RenderedPreview[] RenderPreviewBatches(
        IReadOnlyList<CardPreviewRenderRequest> requests,
        int totalWork,
        int preloadVersion,
        CancellationToken cancellationToken)
    {
        var batches = requests.Chunk(PreviewBatchSize).ToArray();
        var rendered = new ConcurrentBag<RenderedPreview>();
        var completed = 0;
        var workerCount = Math.Min(batches.Length, Math.Clamp(System.Environment.ProcessorCount - 1, 1, 4));
        try
        {
            Parallel.ForEach(
                batches,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = workerCount
                },
                batch =>
                {
                    foreach (var request in batch)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var image = CardPreviewControl.RenderImage(request);
                        rendered.Add(new RenderedPreview(request, image));
                        var current = Interlocked.Increment(ref completed);
                        CallDeferred(
                            nameof(ApplyPreloadProgressForVersion),
                            preloadVersion,
                            current,
                            totalWork,
                            $"Rendered compact thumbnail {current}/{requests.Count} using {workerCount} worker(s).");
                    }
                });
            return rendered.ToArray();
        }
        catch
        {
            foreach (var result in rendered)
            {
                result.Image.Dispose();
            }

            throw;
        }
    }

    private void ApplyPreloadProgressForVersion(int preloadVersion, int current, int total, string message)
    {
        if (preloadVersion == _preloadVersion)
        {
            ApplyPreloadProgress(current, total, message);
        }
    }

    private void ApplyPreloadProgress(int current, int total, string message)
    {
        if (!IsInsideTree())
        {
            return;
        }

        _preloadProgress.MaxValue = Math.Max(1, total);
        _preloadProgress.Value = Math.Clamp(current, 0, Math.Max(1, total));
        _preloadLabel.Text = message;
    }

    private void SetPreloadBusy(bool busy)
    {
        _preloadProgress.Visible = busy;
        _preloadLabel.Visible = busy;
        if (!busy)
        {
            _preloadProgress.Value = 0;
            _preloadLabel.Text = string.Empty;
        }

        SetButtonsDisabled(this, busy);
    }

    private static void SetButtonsDisabled(Node parent, bool disabled)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is BaseButton button)
            {
                button.Disabled = disabled;
            }

            SetButtonsDisabled(child, disabled);
        }
    }

    private sealed record RenderedPreview(CardPreviewRenderRequest Request, Image Image);

    private static int GetCardCount(CardDeckResource deck)
    {
        return (deck.Entries ?? Array.Empty<CardDeckEntryResource>()).Sum(entry => Math.Max(0, entry.Count));
    }

    private static int GetCardTypeCount(CardDeckResource deck, CardType cardType)
    {
        return (deck.Entries ?? Array.Empty<CardDeckEntryResource>())
            .Where(entry => entry.Card?.CardType == cardType)
            .Sum(entry => Math.Max(0, entry.Count));
    }

    private static string GetCardComposition(CardDeckResource deck)
    {
        var groups = (deck.Entries ?? Array.Empty<CardDeckEntryResource>())
            .Where(entry => entry.Card is not null)
            .GroupBy(entry => entry.Card!.CardType)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Sum(entry => Math.Max(0, entry.Count))} {group.Key}")
            .ToArray();
        return groups.Length == 0 ? "empty" : string.Join(", ", groups);
    }

    private void AddResourceDialogs()
    {
        _importDialog = CreateResourceDialog("Import Deck Resource", FileDialog.FileModeEnum.OpenFile);
        _importDialog.FileSelected += path => RunGuiAction("Selected deck resource file", () => OnImportFileSelected(path), $"path={path}");
        AddChild(_importDialog);

    }

    private static FileDialog CreateResourceDialog(string title, FileDialog.FileModeEnum fileMode)
    {
        return new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = fileMode,
            Title = title,
            Filters = ["*.tres ; Godot Resource"]
        };
    }

    private void OpenImportDialog()
    {
        var outputDirectory = ProjectSettings.GlobalizePath(CardGeneration.Services.DeckRepository.UserDecksRootPath);
        Directory.CreateDirectory(outputDirectory);
        _importDialog.CurrentDir = outputDirectory;
        _importDialog.PopupCenteredRatio(0.72f);
    }

    private void OnImportFileSelected(string filePath)
    {
        var result = CardToolService.ImportDeckResource(filePath);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

    private void ValidateDecks()
    {
        var result = CardToolService.ValidateDecks();
        SetStatus(result.Message, !result.Success);
    }

    private void DuplicateDeck(CardDeckResource deck)
    {
        var result = CardToolService.DuplicateDeck(deck.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

    private void DeleteDeck(CardDeckResource deck)
    {
        var result = CardToolService.DeleteDeck(deck.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

}
