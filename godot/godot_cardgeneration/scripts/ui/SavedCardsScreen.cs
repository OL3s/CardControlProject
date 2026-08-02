using System;
using System.Collections.Generic;
using System.IO;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedCardsScreen : CardToolScreen
{
    private IReadOnlyList<CardResource> _cards = Array.Empty<CardResource>();
    private CardPreviewControl _frontPreview = null!;
    private Label _details = null!;
    private FileDialog _importDialog = null!;

    public event Action<CardResource?>? EditCardRequested;
    public event Action? NewCardRequested;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _cards = CardToolService.LoadAllCards();

        var content = BuildScreen("Cards", "Browse card resources, preview them, edit them, or export a single card.");

        var toolbar = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        toolbar.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolbar);
        AddIconButton(toolbar, CardIconPath, "Create card", () => NewCardRequested?.Invoke());
        AddIconButton(toolbar, ImportIconPath, "Import card resource", OpenImportDialog);
        AddIconButton(toolbar, CheckIconPath, "Validate cards", ValidateCards);
        AddIconButton(toolbar, RefreshIconPath, "Refresh", BuildUi);
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

        foreach (var card in _cards)
        {
            AddCardRow(list, card);
        }

        ShowCard(_cards[0]);
        SetStatus($"Loaded {_cards.Count} saved card(s).");
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
            CardType.King => KingTypeIconPath,
            _ => CardIconPath
        };
    }

    private void AddResourceDialogs()
    {
        _importDialog = CreateResourceDialog("Import Card Resource", FileDialog.FileModeEnum.OpenFile);
        _importDialog.FileSelected += OnImportFileSelected;
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
        var outputDirectory = ProjectSettings.GlobalizePath(CardGeneration.Services.CardRepository.UserCardsRootPath);
        Directory.CreateDirectory(outputDirectory);
        _importDialog.CurrentDir = outputDirectory;
        _importDialog.PopupCenteredRatio(0.72f);
    }

    private void OnImportFileSelected(string filePath)
    {
        var result = CardToolService.ImportCardResource(filePath);
        BuildUi();
        SetStatus(result.Message, !result.Success);
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
