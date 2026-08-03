using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class CardEditorScreen : CardToolScreen
{
    private const string PowerIconPath = "res://assets/icons/symbols/power.svg";
    private const int MissingElementItemId = 1000;

    private CardResource _editingCard = new MonsterCardResource();
    private IReadOnlyList<ElementResource> _elements = Array.Empty<ElementResource>();
    private LineEdit _id = null!;
    private LineEdit _imageSourcePath = null!;
    private OptionButton _cardElement = null!;
    private CardPreviewControl _frontPreview = null!;
    private FileDialog _imageFileDialog = null!;
    private FileDialog _saveAsDialog = null!;
    private SpinBox? _monsterTier;
    private SpinBox? _monsterBasePower;
    private SpinBox? _monsterRequirementNeutral;
    private SpinBox? _monsterRequirementGrass;
    private SpinBox? _monsterRequirementFlame;
    private SpinBox? _monsterRequirementWater;
    private VBoxContainer? _monsterPowerBonusesList;
    private readonly List<MonsterPowerBonusEditorRow> _monsterPowerBonusRows = [];
    private SpinBox? _terrainProducedNeutral;
    private SpinBox? _terrainProducedGrass;
    private SpinBox? _terrainProducedFlame;
    private SpinBox? _terrainProducedWater;

    public void SetCard(CardResource? card)
    {
        _editingCard = card is null ? new MonsterCardResource() : CloneCard(card);
    }

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _elements = CardToolService.LoadAllElements();
        var isNewCard = string.IsNullOrWhiteSpace(_editingCard.Id);
        var content = BuildScreen(isNewCard ? "New Card" : "Edit Card", "Create or edit a card resource, import an image source, preview it and save it.");

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 18);
        content.AddChild(body);

        var form = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        form.AddThemeConstantOverride("separation", 8);
        body.AddChild(form);

        _id = AddLineEdit(form, "Card ID", _editingCard.Id);
        form.AddChild(new Label { Text = $"Card Type: {_editingCard.CardType}" });
        _cardElement = AddElementSelector(form, _editingCard.Element);

        _imageSourcePath = AddImageSourcePathRow(form);

        AddTypeSpecificFields(form);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        form.AddChild(buttons);
        if (!isNewCard)
        {
            AddIconButton(buttons, SaveIconPath, "Save", SaveCard);
        }

        AddIconButton(buttons, SaveAddIconPath, "Save as new", OpenSaveAsDialog);
        AddIconButton(buttons, RefreshIconPath, "Refresh preview", RefreshPreview);

        var previewColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0)
        };
        previewColumn.AddThemeConstantOverride("separation", 10);
        body.AddChild(previewColumn);

        previewColumn.AddChild(new Label
        {
            Text = "Front",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        _frontPreview = CardPreviewControl.Create(minimumSize: new Vector2(220, 308), useCache: false);
        previewColumn.AddChild(_frontPreview);

        previewColumn.AddChild(new Label
        {
            Text = "Front preview uses the same Image renderer as CLI export. Card backs are set by decks.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _imageFileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Select Card Image",
            Filters = [
                "*.png, *.jpg, *.jpeg, *.webp, *.svg ; Supported Images",
                "*.png ; PNG",
                "*.jpg, *.jpeg ; JPEG",
                "*.webp ; WebP",
                "*.svg ; SVG"
            ]
        };
        _imageFileDialog.FileSelected += path => RunGuiAction("Selected card image file", () => OnImageFileSelected(path), $"path={path}");
        AddChild(_imageFileDialog);

        _saveAsDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = "Save Card As New Resource",
            Filters = ["*.tres ; Godot Resource"]
        };
        _saveAsDialog.FileSelected += path => RunGuiAction("Selected card save-as file", () => OnSaveAsFileSelected(path), $"path={path}");
        AddChild(_saveAsDialog);

        RefreshPreview();
    }

    private LineEdit AddImageSourcePathRow(VBoxContainer form)
    {
        form.AddChild(new Label { Text = "Card Image Source Path" });
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        form.AddChild(row);

        var lineEdit = new LineEdit
        {
            Text = _editingCard.CardImageSourcePath,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddChild(lineEdit);
        AddIconButton(row, BrowseIconPath, "Import image", OpenImageDialog);
        return lineEdit;
    }

    private void AddTypeSpecificFields(VBoxContainer form)
    {
        AddSeparator(form);

        switch (_editingCard)
        {
            case MonsterCardResource monster:
                form.AddChild(new Label { Text = "Monster Setup" });
                form.AddChild(new Label
                {
                    Text = "The monster element is selected independently from its resource requirements.",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
                _monsterTier = AddSpinBox(form, "Tier", 1, 3, 1, monster.Tier);
                _monsterBasePower = AddSpinBox(form, "Base Power", 0, 20, 1, monster.BasePower);
                AddElementAmountGrid(
                    form,
                    "Requirements",
                    monster.Requirements,
                    out _monsterRequirementNeutral,
                    out _monsterRequirementGrass,
                    out _monsterRequirementFlame,
                    out _monsterRequirementWater);
                BindSpinPreview(_monsterTier, _monsterBasePower, _monsterRequirementNeutral, _monsterRequirementGrass, _monsterRequirementFlame, _monsterRequirementWater);
                AddMonsterPowerBonusEditor(form, monster);
                break;
            case TerrainCardResource terrain:
                form.AddChild(new Label { Text = "Terrain Setup" });
                form.AddChild(new Label
                {
                    Text = "The terrain element is its core identity and is independent from produced resources.",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
                AddElementAmountGrid(
                    form,
                    "Produces",
                    terrain.ProducedResources,
                    out _terrainProducedNeutral,
                    out _terrainProducedGrass,
                    out _terrainProducedFlame,
                    out _terrainProducedWater);
                BindSpinPreview(_terrainProducedNeutral, _terrainProducedGrass, _terrainProducedFlame, _terrainProducedWater);
                break;
        }

        AddSeparator(form);
    }

    private OptionButton AddElementSelector(VBoxContainer form, ElementResource? selectedElement)
    {
        form.AddChild(new Label { Text = "Card Element" });
        var selector = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        selector.AddItem("Select element", MissingElementItemId);

        foreach (var element in _elements.OrderBy(element => element.ElementType))
        {
            if (element.IconTexture is not null)
            {
                selector.AddIconItem(element.IconTexture, element.DisplayName, (int)element.ElementType);
            }
            else
            {
                selector.AddItem(element.DisplayName, (int)element.ElementType);
            }
        }

        for (var index = 0; index < selector.ItemCount; index++)
        {
            if (selectedElement is not null && selector.GetItemId(index) == (int)selectedElement.ElementType)
            {
                selector.Select(index);
                break;
            }
        }

        selector.ItemSelected += _ =>
        {
            if (_frontPreview is not null)
            {
                RefreshPreview();
            }
        };
        form.AddChild(selector);
        return selector;
    }

    private void AddMonsterPowerBonusEditor(VBoxContainer form, MonsterCardResource monster)
    {
        AddSeparator(form);
        form.AddChild(new Label { Text = "Power Bonuses" });
        form.AddChild(new Label
        {
            Text = "Each row is an optional extra attack/power line: required resources -> power gain.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _monsterPowerBonusesList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _monsterPowerBonusesList.AddThemeConstantOverride("separation", 8);
        form.AddChild(_monsterPowerBonusesList);

        _monsterPowerBonusRows.Clear();
        foreach (var bonus in monster.PowerBonuses ?? Array.Empty<PowerBonusResource>())
        {
            AddMonsterPowerBonusRow(bonus, refreshPreview: false);
        }

        AddIconButton(form, AddIconPath, "Add power bonus", () => AddMonsterPowerBonusRow(new PowerBonusResource(), refreshPreview: true));
    }

    private void AddMonsterPowerBonusRow(PowerBonusResource bonus, bool refreshPreview)
    {
        if (_monsterPowerBonusesList is null)
        {
            return;
        }

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _monsterPowerBonusesList.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        var needs = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        needs.AddThemeConstantOverride("separation", 4);
        row.AddChild(needs);

        needs.AddChild(new Label { Text = "Needs" });
        var needsGrid = CreateElementAmountGrid();
        needs.AddChild(needsGrid);
        var neutral = AddElementAmountCell(needsGrid, ElementType.Neutral, CountAmount(bonus.Requirements, ElementType.Neutral));
        var grass = AddElementAmountCell(needsGrid, ElementType.Grass, CountAmount(bonus.Requirements, ElementType.Grass));
        var flame = AddElementAmountCell(needsGrid, ElementType.Flame, CountAmount(bonus.Requirements, ElementType.Flame));
        var water = AddElementAmountCell(needsGrid, ElementType.Water, CountAmount(bonus.Requirements, ElementType.Water));

        var powerGain = AddPowerGainCell(row, Math.Max(1, bonus.PowerGain));

        var editorRow = new MonsterPowerBonusEditorRow(panel, neutral, grass, flame, water, powerGain);
        _monsterPowerBonusRows.Add(editorRow);

        BindSpinPreview(neutral, grass, flame, water, powerGain);
        AddIconButton(row, DeleteIconPath, "Remove", () => RemoveMonsterPowerBonusRow(editorRow), new Vector2(36, 34));

        if (refreshPreview)
        {
            RefreshPreview();
        }
    }

    private void AddElementAmountGrid(
        VBoxContainer form,
        string label,
        ResourceAmount[] amounts,
        out SpinBox neutral,
        out SpinBox grass,
        out SpinBox flame,
        out SpinBox water)
    {
        form.AddChild(new Label { Text = label });
        var grid = CreateElementAmountGrid();
        form.AddChild(grid);

        neutral = AddElementAmountCell(grid, ElementType.Neutral, CountAmount(amounts, ElementType.Neutral));
        grass = AddElementAmountCell(grid, ElementType.Grass, CountAmount(amounts, ElementType.Grass));
        flame = AddElementAmountCell(grid, ElementType.Flame, CountAmount(amounts, ElementType.Flame));
        water = AddElementAmountCell(grid, ElementType.Water, CountAmount(amounts, ElementType.Water));
    }

    private static GridContainer CreateElementAmountGrid()
    {
        var grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 8);
        return grid;
    }

    private SpinBox AddElementAmountCell(GridContainer grid, ElementType elementType, int value, int min = 0, int max = 20)
    {
        var cell = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(126, 0)
        };
        cell.AddThemeConstantOverride("separation", 6);
        grid.AddChild(cell);

        cell.AddChild(CreateElementIcon(elementType));

        var spinBox = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = Math.Clamp(value, min, max),
            CustomMinimumSize = new Vector2(76, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        cell.AddChild(spinBox);
        return spinBox;
    }

    private SpinBox AddPowerGainCell(HBoxContainer row, int value)
    {
        var cell = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        cell.AddThemeConstantOverride("separation", 6);
        row.AddChild(cell);

        cell.AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(PowerIconPath),
            CustomMinimumSize = new Vector2(26, 26),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = "Power gain"
        });

        var spinBox = new SpinBox
        {
            MinValue = 1,
            MaxValue = 10,
            Step = 1,
            Value = Math.Clamp(value, 1, 10),
            CustomMinimumSize = new Vector2(76, 0)
        };
        cell.AddChild(spinBox);
        return spinBox;
    }

    private TextureRect CreateElementIcon(ElementType elementType)
    {
        return new TextureRect
        {
            Texture = GetElement(elementType)?.IconTexture,
            CustomMinimumSize = new Vector2(26, 26),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = elementType.ToString()
        };
    }

    private void RemoveMonsterPowerBonusRow(MonsterPowerBonusEditorRow row)
    {
        _monsterPowerBonusRows.Remove(row);
        row.Panel.GetParent()?.RemoveChild(row.Panel);
        row.Panel.QueueFree();
        RefreshPreview();
    }

    private void OpenImageDialog()
    {
        _imageFileDialog.PopupCenteredRatio(0.72f);
    }

    private void OnImageFileSelected(string path)
    {
        _imageSourcePath.Text = path;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        ApplyFieldsToCard();
        _frontPreview.SetCard(_editingCard);
    }

    private void SaveCard()
    {
        EnsureId();
        ApplyFieldsToCard();
        var result = CardToolService.SaveCard(_editingCard);
        SetStatus(result.Message, !result.Success);
    }

    private void OpenSaveAsDialog()
    {
        EnsureId();
        var outputDirectory = ProjectSettings.GlobalizePath(CardGeneration.Services.CardRepository.UserCardsRootPath);
        Directory.CreateDirectory(outputDirectory);
        _saveAsDialog.CurrentDir = outputDirectory;
        _saveAsDialog.CurrentFile = $"{SanitizeFileName(CreateCopyId(_id.Text))}.tres";
        _saveAsDialog.PopupCenteredRatio(0.72f);
    }

    private void OnSaveAsFileSelected(string filePath)
    {
        var fileId = MakeResourceId(Path.GetFileNameWithoutExtension(filePath), CreateCopyId(_id.Text));
        _id.Text = fileId;
        ApplyFieldsToCard();
        var result = CardToolService.ExportCardResource(_editingCard, EnsureTresExtension(filePath));
        SetStatus(result.Message, !result.Success);
        RefreshPreview();
    }

    private void EnsureId()
    {
        if (!string.IsNullOrWhiteSpace(_id.Text))
        {
            return;
        }

        _id.Text = MakeResourceId(_editingCard.CardType.ToString(), "new_card");
    }

    private string CreateCopyId(string sourceId)
    {
        var baseId = MakeResourceId(sourceId, _editingCard.CardType.ToString().ToLowerInvariant());
        var existingIds = CardToolService.LoadAllCards()
            .Select(card => card.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 1; index < 1000; index++)
        {
            var candidate = $"{baseId}_copy_{index}";
            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseId}_copy_{DateTime.Now:yyyyMMddHHmmss}";
    }

    private static string EnsureTresExtension(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".tres", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : $"{filePath}.tres";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "card" : fileName;
    }

    private void ApplyFieldsToCard()
    {
        _editingCard.Id = _id.Text.Trim();
        _editingCard.Element = GetSelectedCardElement();
        _editingCard.CardImageSourcePath = _imageSourcePath.Text.Trim();
        ApplyTypeSpecificFields();
    }

    private void ApplyTypeSpecificFields()
    {
        switch (_editingCard)
        {
            case MonsterCardResource monster:
                monster.Tier = (int)(_monsterTier?.Value ?? monster.Tier);
                monster.BasePower = (int)(_monsterBasePower?.Value ?? monster.BasePower);
                monster.Requirements = BuildAmounts(
                    (ElementType.Neutral, _monsterRequirementNeutral),
                    (ElementType.Grass, _monsterRequirementGrass),
                    (ElementType.Flame, _monsterRequirementFlame),
                    (ElementType.Water, _monsterRequirementWater));
                monster.PowerBonuses = BuildPowerBonuses();
                break;
            case TerrainCardResource terrain:
                terrain.ProducedResources = BuildAmounts(
                    (ElementType.Neutral, _terrainProducedNeutral),
                    (ElementType.Grass, _terrainProducedGrass),
                    (ElementType.Flame, _terrainProducedFlame),
                    (ElementType.Water, _terrainProducedWater));
                break;
        }
    }

    private ResourceAmount[] BuildAmounts(params (ElementType ElementType, SpinBox? Input)[] specs)
    {
        return specs
            .Where(spec => spec.Input is not null && spec.Input.Value > 0)
            .Select(spec => new ResourceAmount
            {
                Element = GetElement(spec.ElementType),
                Amount = (int)spec.Input!.Value
            })
            .ToArray();
    }

    private PowerBonusResource[] BuildPowerBonuses()
    {
        return _monsterPowerBonusRows
            .Select(row => new PowerBonusResource
            {
                Requirements = BuildAmounts(
                    (ElementType.Neutral, row.NeutralRequirement),
                    (ElementType.Grass, row.GrassRequirement),
                    (ElementType.Flame, row.FlameRequirement),
                    (ElementType.Water, row.WaterRequirement)),
                PowerGain = (int)row.PowerGain.Value
            })
            .Where(bonus => bonus.Requirements.Length > 0 && bonus.PowerGain > 0)
            .ToArray();
    }

    private ElementResource? GetElement(ElementType elementType)
    {
        return _elements.FirstOrDefault(element => element.ElementType == elementType);
    }

    private ElementResource? GetSelectedCardElement()
    {
        if (_cardElement.Selected < 0)
        {
            return null;
        }

        var elementId = _cardElement.GetItemId(_cardElement.Selected);
        return Enum.IsDefined(typeof(ElementType), elementId)
            ? GetElement((ElementType)elementId)
            : null;
    }

    private static int CountAmount(ResourceAmount[] amounts, ElementType elementType)
    {
        return amounts
            .Where(amount => amount.Element?.ElementType == elementType)
            .Sum(amount => Math.Max(0, amount.Amount));
    }

    private void BindSpinPreview(params SpinBox?[] spinBoxes)
    {
        foreach (var spinBox in spinBoxes)
        {
            if (spinBox is not null)
            {
                spinBox.ValueChanged += _ => RefreshPreview();
            }
        }
    }

    private static CardResource CloneCard(CardResource source)
    {
        var clone = CreateCardForType(source.CardType);
        CopyCommonFields(source, clone);

        if (source is MonsterCardResource sourceMonster && clone is MonsterCardResource cloneMonster)
        {
            cloneMonster.Tier = sourceMonster.Tier;
            cloneMonster.Requirements = sourceMonster.Requirements;
            cloneMonster.BasePower = sourceMonster.BasePower;
            cloneMonster.PowerBonuses = sourceMonster.PowerBonuses;
            cloneMonster.Effect = sourceMonster.Effect;
        }
        else if (source is TerrainCardResource sourceTerrain && clone is TerrainCardResource cloneTerrain)
        {
            cloneTerrain.ProducedResources = sourceTerrain.ProducedResources;
        }

        return clone;
    }

    private static void CopyCommonFields(CardResource source, CardResource target)
    {
        target.Id = source.Id;
        target.CardType = source.CardType;
        target.Element = source.Element;
        target.CardImageTexture = source.CardImageTexture;
        target.CardImageSourcePath = source.CardImageSourcePath;
        target.BackImageTexture = source.BackImageTexture;
    }

    private static CardResource CreateCardForType(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new MonsterCardResource(),
            CardType.Terrain => new TerrainCardResource(),
            _ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, "Only monster and terrain cards are supported.")
        };
    }

    private static CardType CardTypeForResource(CardResource card)
    {
        return card switch
        {
            MonsterCardResource => CardType.Monster,
            TerrainCardResource => CardType.Terrain,
            _ => CardType.Unknown
        };
    }

    private sealed record MonsterPowerBonusEditorRow(
        PanelContainer Panel,
        SpinBox NeutralRequirement,
        SpinBox GrassRequirement,
        SpinBox FlameRequirement,
        SpinBox WaterRequirement,
        SpinBox PowerGain);
}
