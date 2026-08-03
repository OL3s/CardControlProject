using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedCardsScreen : CardToolScreen
{
    private IReadOnlyList<CardResource> _cards = Array.Empty<CardResource>();
    private CardPreviewControl _frontPreview = null!;
    private Label _details = null!;
    private FileDialog _importDialog = null!;
    private FileDialog _importFolderDialog = null!;
    private readonly HashSet<string> _expandedFolders = new(StringComparer.OrdinalIgnoreCase);

    public event Action<CardResource?>? EditCardRequested;
    public event Action? NewCardRequested;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _cards = CardToolService.LoadAllCards();

        var content = BuildScreen("Cards", "Browse card resources, preview them, and edit their rules, stats and background images. Export is deck-only.");

        var toolbar = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        toolbar.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolbar);
        AddIconButton(toolbar, CardIconPath, "Create card", () => NewCardRequested?.Invoke());
        AddIconButton(toolbar, ImportIconPath, "Import card resource files", OpenImportDialog);
        AddIconButton(toolbar, BrowseIconPath, "Import card folder recursively", OpenImportFolderDialog);
        AddIconButton(toolbar, CheckIconPath, "Validate cards", ValidateCards);
        AddIconButton(toolbar, RefreshIconPath, "Refresh", RefreshDefaultsAndBuildUi);
        AddResourceDialogs();

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 16);
        content.AddChild(body);

        var listScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(420, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        body.AddChild(listScroll);

        var list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        list.AddThemeConstantOverride("separation", 10);
        listScroll.AddChild(list);

        var previewColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(260, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        previewColumn.AddThemeConstantOverride("separation", 8);
        body.AddChild(previewColumn);

        previewColumn.AddChild(new Label
        {
            Text = "Front",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        _frontPreview = CardPreviewControl.Create(minimumSize: new Vector2(220, 308), renderSize: new Vector2I(220, 308));
        previewColumn.AddChild(_frontPreview);

        _details = new Label
        {
            Text = $"Card Count: {_cards.Count}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        previewColumn.AddChild(_details);

        if (_cards.Count == 0)
        {
            list.AddChild(new Label
            {
                Text = "No cards found in default resources or user://resources/cards.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        AddCardFolderSections(list);

        ShowCard(_cards[0]);
        SetStatus($"Loaded {_cards.Count} saved card(s).");
    }

    private void RefreshDefaultsAndBuildUi()
    {
        var defaultResult = CardToolService.EnsureDefaultResources();
        BuildUi();
        SetStatus(defaultResult.Message, !defaultResult.Success);
    }

    private void AddCardRow(VBoxContainer list, CardResource card)
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
            Text = card.Id,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        });

        AddCardTypeRow(info, card.CardType);

        var buttons = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Alignment = BoxContainer.AlignmentMode.End
        };
        buttons.AddThemeConstantOverride("separation", 8);
        row.AddChild(buttons);

        AddIconButton(buttons, PreviewIconPath, "Preview", () => ShowCard(card));
        AddIconButton(buttons, EditIconPath, "Edit", () => EditCardRequested?.Invoke(card));
        AddIconButton(buttons, CopyIconPath, "Duplicate", () => DuplicateCard(card));
        AddIconButton(buttons, DeleteIconPath, "Delete", () => DeleteCard(card));
    }

    private void AddCardFolderSections(VBoxContainer list)
    {
        foreach (var group in _cards.GroupBy(GetCardFolderLabel).OrderBy(group => group.Key))
        {
            AddFolderSection(list, group.Key, group.OrderBy(card => card.Id).ToArray());
        }
    }

    private void AddFolderSection(VBoxContainer list, string folderName, IReadOnlyList<CardResource> cards)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        list.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var expanded = _expandedFolders.Contains(folderName);
        var button = new Button
        {
            Text = $"{(expanded ? "[-]" : "[+]")} {folderName} ({cards.Count})",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = expanded ? "Collapse folder" : "Open folder",
            Alignment = HorizontalAlignment.Left,
            Icon = LoadIcon(BrowseIconPath),
            IconAlignment = HorizontalAlignment.Left
        };
        button.Text = $"{folderName} ({cards.Count})";
        button.AddThemeColorOverride("font_color", expanded ? new Color(0.78f, 0.93f, 0.75f) : new Color(0.88f, 0.84f, 0.74f));
        button.Pressed += LogGuiAction("Toggle card folder", () => ToggleFolder(folderName), $"folder={folderName}; expanded={!expanded}");
        margin.AddChild(button);

        if (!expanded)
        {
            return;
        }

        foreach (var card in cards)
        {
            AddCardRow(list, card);
        }
    }

    private void ToggleFolder(string folderName)
    {
        if (!_expandedFolders.Add(folderName))
        {
            _expandedFolders.Remove(folderName);
        }

        BuildUi();
    }

    private static string GetCardFolderLabel(CardResource card)
    {
        var resourcePath = NormalizeResourcePath(card.ResourcePath);
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return "unknown";
        }

        if (IsUnderRoot(resourcePath, CardRepository.UserCardsRootPath))
        {
            if (IsUnderRoot(resourcePath, CardRepository.UserDefaultCardsRootPath))
            {
                return "default_cards";
            }

            return GetRelativeFolder(resourcePath, CardRepository.UserCardsRootPath, "user");
        }

        if (IsUnderRoot(resourcePath, CardRepository.CardsRootPath))
        {
            var folder = GetRelativeFolder(resourcePath, CardRepository.CardsRootPath, "root");
            return folder == "root" ? "packaged" : $"packaged/{folder}";
        }

        return "external";
    }

    private static string GetRelativeFolder(string resourcePath, string rootPath, string rootLabel)
    {
        var relativePath = resourcePath[rootPath.Length..].TrimStart('/');
        var folder = NormalizeResourcePath(Path.GetDirectoryName(relativePath) ?? string.Empty);
        return string.IsNullOrWhiteSpace(folder) ? rootLabel : folder;
    }

    private static bool IsUnderRoot(string resourcePath, string rootPath)
    {
        return resourcePath.StartsWith(NormalizeResourcePath(rootPath) + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeResourcePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private void ShowCard(CardResource card)
    {
        _frontPreview.SetCard(card);
        _details.Text = $"Card Count: {_cards.Count}";
    }

    private static void AddCardTypeRow(VBoxContainer parent, CardType cardType)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        parent.AddChild(row);

        var tooltip = cardType.ToString();
        row.AddChild(new TextureRect
        {
            Texture = LoadIcon(GetCardTypeIconPath(cardType)),
            CustomMinimumSize = new Vector2(24, 24),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = tooltip
        });
        row.AddChild(new Label
        {
            Text = tooltip,
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = tooltip
        });
    }

    private static string GetCardTypeIconPath(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => MonsterTypeIconPath,
            CardType.Terrain => TerrainTypeIconPath,
            _ => CardIconPath
        };
    }

    private void AddResourceDialogs()
    {
        _importDialog = CreateResourceDialog("Import Card Resources", FileDialog.FileModeEnum.OpenFiles);
        _importDialog.FilesSelected += paths => RunGuiAction("Selected card resource files", () => OnImportFilesSelected(paths), $"files={paths.Length}");
        AddChild(_importDialog);

        _importFolderDialog = CreateResourceDialog("Import Card Folder", FileDialog.FileModeEnum.OpenDir);
        _importFolderDialog.DirSelected += path => RunGuiAction("Selected card import folder", () => OnImportFolderSelected(path), $"path={path}");
        AddChild(_importFolderDialog);
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
        var outputDirectory = ProjectSettings.GlobalizePath(CardRepository.UserCardsRootPath);
        Directory.CreateDirectory(outputDirectory);
        _importDialog.CurrentDir = outputDirectory;
        _importDialog.PopupCenteredRatio(0.72f);
    }

    private void OpenImportFolderDialog()
    {
        var outputDirectory = ProjectSettings.GlobalizePath(CardRepository.UserCardsRootPath);
        Directory.CreateDirectory(outputDirectory);
        _importFolderDialog.CurrentDir = outputDirectory;
        _importFolderDialog.PopupCenteredRatio(0.72f);
    }

    private void OnImportFilesSelected(string[] filePaths)
    {
        ImportCardFiles(filePaths);
    }

    private void OnImportFolderSelected(string folderPath)
    {
        var globalFolderPath = ProjectPaths.ToGlobalPath(folderPath);
        if (!Directory.Exists(globalFolderPath))
        {
            SetStatus($"Import folder was not found: {folderPath}.", true);
            return;
        }

        ImportCardFiles(Directory.EnumerateFiles(globalFolderPath, "*.*", SearchOption.AllDirectories).Where(IsResourceFilePath));
    }

    private void ImportCardFiles(IEnumerable<string> filePaths)
    {
        var imported = 0;
        var scanned = 0;
        var failures = new List<string>();
        foreach (var filePath in filePaths)
        {
            scanned++;
            var result = CardToolService.ImportCardResource(filePath);
            if (result.Success)
            {
                imported++;
            }
            else
            {
                failures.Add(result.Message);
            }
        }

        AppLogger.GuiInfo($"Card import completed. scanned={scanned}; imported={imported}; failed={failures.Count}");
        BuildUi();
        SetStatus(CreateImportStatus(imported, scanned, failures), failures.Count > 0);
    }

    private static string CreateImportStatus(int imported, int scanned, IReadOnlyList<string> failures)
    {
        if (scanned == 0)
        {
            return "No card resource files found.";
        }

        if (failures.Count == 0)
        {
            return $"Imported {imported} card resource(s).";
        }

        return $"Imported {imported} card resource(s). Failed {failures.Count}: {string.Join(" | ", failures)}";
    }

    private static bool IsResourceFilePath(string filePath)
    {
        return filePath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".res", StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateCards()
    {
        var result = CardToolService.ValidateCards();
        SetStatus(result.Message, !result.Success);
    }

    private void DuplicateCard(CardResource card)
    {
        var result = CardToolService.DuplicateCard(card.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

    private void DeleteCard(CardResource card)
    {
        var result = CardToolService.DeleteCard(card.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

}
