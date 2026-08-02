using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class CardEditorScreen : CardToolScreen
{
    private CardResource _editingCard = new MonsterCardResource();
    private IReadOnlyList<ElementResource> _elements = Array.Empty<ElementResource>();
    private LineEdit _id = null!;
    private SpinBox _internalTier = null!;
    private LineEdit _imageSourcePath = null!;
    private CardPreviewControl _frontPreview = null!;
    private CardPreviewControl _backPreview = null!;
    private FileDialog _imageFileDialog = null!;
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
    private OptionButton? _kingElementFocus;
    private SpinBox? _kingHealth;
    private TextEdit? _kingQuestText;

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

        _internalTier = AddSpinBox(form, "Internal Tier", 0, 20, 1, _editingCard.InternalTier);
        _internalTier.ValueChanged += _ => RefreshPreview();
        _imageSourcePath = AddLineEdit(form, "Card Image Source Path", _editingCard.CardImageSourcePath);

        AddTypeSpecificFields(form);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        form.AddChild(buttons);
        AddButton(buttons, "Import Image", OpenImageDialog, 120);
        AddButton(buttons, "Refresh Preview", RefreshPreview, 136);
        AddButton(buttons, "Save", SaveCard, 86);

        var previewColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0)
        };
        previewColumn.AddThemeConstantOverride("separation", 10);
        body.AddChild(previewColumn);

        previewColumn.AddChild(new Label { Text = "Front" });
        _frontPreview = CardPreviewControl.Create(minimumSize: new Vector2(220, 308), useCache: false);
        previewColumn.AddChild(_frontPreview);

        previewColumn.AddChild(new Label { Text = "Back" });
        _backPreview = CardPreviewControl.Create(showBack: true, minimumSize: new Vector2(220, 308), useCache: false);
        previewColumn.AddChild(_backPreview);
        previewColumn.AddChild(new Label
        {
            Text = "Preview uses the same Image renderer as CLI export.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _imageFileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Select Card Image"
        };
        _imageFileDialog.FileSelected += OnImageFileSelected;
        AddChild(_imageFileDialog);

        RefreshPreview();
    }

    private OptionButton AddElementOption(VBoxContainer form, string label, ElementResource? selectedElement)
    {
        form.AddChild(new Label { Text = label });
        var option = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        option.AddItem("None");
        foreach (var element in _elements)
        {
            option.AddItem($"{element.DisplayName} ({element.ElementType})");
        }

        var selectedIndex = selectedElement is null
            ? 0
            : _elements.Select((element, index) => new { element, index })
                .FirstOrDefault(item => item.element.ElementType == selectedElement.ElementType)
                ?.index + 1 ?? 0;
        option.Select(selectedIndex);
        form.AddChild(option);
        return option;
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
                    Text = "Element type is derived from the non-neutral requirement cost.",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
                _monsterBasePower = AddSpinBox(form, "Base Power", 0, 20, 1, monster.BasePower);
                _monsterRequirementNeutral = AddSpinBox(form, "Requirement: Neutral", 0, 20, 1, CountAmount(monster.Requirements, ElementType.Neutral));
                _monsterRequirementGrass = AddSpinBox(form, "Requirement: Grass", 0, 20, 1, CountAmount(monster.Requirements, ElementType.Grass));
                _monsterRequirementFlame = AddSpinBox(form, "Requirement: Flame", 0, 20, 1, CountAmount(monster.Requirements, ElementType.Flame));
                _monsterRequirementWater = AddSpinBox(form, "Requirement: Water", 0, 20, 1, CountAmount(monster.Requirements, ElementType.Water));
                BindSpinPreview(_monsterBasePower, _monsterRequirementNeutral, _monsterRequirementGrass, _monsterRequirementFlame, _monsterRequirementWater);
                AddMonsterPowerBonusEditor(form, monster);
                break;
            case TerrainCardResource terrain:
                form.AddChild(new Label { Text = "Terrain Setup" });
                form.AddChild(new Label
                {
                    Text = "Terrain cards do not store an element focus; produced resources define what the terrain provides.",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
                _terrainProducedNeutral = AddSpinBox(form, "Produces: Neutral", 0, 20, 1, CountAmount(terrain.ProducedResources, ElementType.Neutral));
                _terrainProducedGrass = AddSpinBox(form, "Produces: Grass", 0, 20, 1, CountAmount(terrain.ProducedResources, ElementType.Grass));
                _terrainProducedFlame = AddSpinBox(form, "Produces: Flame", 0, 20, 1, CountAmount(terrain.ProducedResources, ElementType.Flame));
                _terrainProducedWater = AddSpinBox(form, "Produces: Water", 0, 20, 1, CountAmount(terrain.ProducedResources, ElementType.Water));
                BindSpinPreview(_terrainProducedNeutral, _terrainProducedGrass, _terrainProducedFlame, _terrainProducedWater);
                break;
            case KingCardResource king:
                form.AddChild(new Label { Text = "King Setup" });
                _kingElementFocus = AddElementOption(form, "Element Focus", king.ElementFocus);
                _kingElementFocus.ItemSelected += _ => RefreshPreview();
                _kingHealth = AddSpinBox(form, "Health", 1, 30, 1, king.Health);
                _kingHealth.ValueChanged += _ => RefreshPreview();
                _kingQuestText = AddTextEdit(form, "Quest Text", king.QuestText, 90);
                _kingQuestText.TextChanged += RefreshPreview;
                break;
        }

        AddSeparator(form);
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

        AddButton(form, "+ Add Power Bonus", () => AddMonsterPowerBonusRow(new PowerBonusResource(), refreshPreview: true), 170);
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

        row.AddChild(new Label
        {
            Text = "Needs",
            VerticalAlignment = VerticalAlignment.Center
        });

        var neutral = AddInlineSpin(row, "N", CountAmount(bonus.Requirements, ElementType.Neutral));
        var grass = AddInlineSpin(row, "G", CountAmount(bonus.Requirements, ElementType.Grass));
        var flame = AddInlineSpin(row, "F", CountAmount(bonus.Requirements, ElementType.Flame));
        var water = AddInlineSpin(row, "W", CountAmount(bonus.Requirements, ElementType.Water));

        row.AddChild(new Label
        {
            Text = "-> Power",
            VerticalAlignment = VerticalAlignment.Center
        });
        var powerGain = AddInlineSpin(row, "+", Math.Max(1, bonus.PowerGain), min: 1, max: 10);

        var editorRow = new MonsterPowerBonusEditorRow(panel, neutral, grass, flame, water, powerGain);
        _monsterPowerBonusRows.Add(editorRow);

        BindSpinPreview(neutral, grass, flame, water, powerGain);
        AddButton(row, "Remove", () => RemoveMonsterPowerBonusRow(editorRow), 90);

        if (refreshPreview)
        {
            RefreshPreview();
        }
    }

    private SpinBox AddInlineSpin(HBoxContainer row, string label, int value, int min = 0, int max = 20)
    {
        row.AddChild(new Label
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        });

        var spinBox = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = Math.Clamp(value, min, max),
            CustomMinimumSize = new Vector2(64, 0)
        };
        row.AddChild(spinBox);
        return spinBox;
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
        _backPreview.SetCard(_editingCard, showBack: true);
    }

    private void SaveCard()
    {
        EnsureId();
        ApplyFieldsToCard();
        var result = CardToolService.SaveCard(_editingCard);
        SetStatus(result.Message, !result.Success);
    }

    private void EnsureId()
    {
        if (!string.IsNullOrWhiteSpace(_id.Text))
        {
            return;
        }

        _id.Text = MakeResourceId(_editingCard.CardType.ToString(), "new_card");
    }

    private void ApplyFieldsToCard()
    {
        _editingCard.Id = _id.Text.Trim();
        _editingCard.InternalTier = (int)_internalTier.Value;
        _editingCard.CardImageSourcePath = _imageSourcePath.Text.Trim();
        ApplyTypeSpecificFields();
    }

    private void ApplyTypeSpecificFields()
    {
        switch (_editingCard)
        {
            case MonsterCardResource monster:
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
            case KingCardResource king:
                king.ElementFocus = GetSelectedKingElementFocus();
                king.Health = (int)(_kingHealth?.Value ?? king.Health);
                king.QuestText = _kingQuestText?.Text ?? king.QuestText;
                break;
        }
    }

    private ElementResource? GetSelectedKingElementFocus()
    {
        if (_kingElementFocus is null || _kingElementFocus.Selected <= 0)
        {
            return null;
        }

        var elementIndex = _kingElementFocus.Selected - 1;
        return elementIndex >= 0 && elementIndex < _elements.Count ? _elements[elementIndex] : null;
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
            cloneMonster.Requirements = sourceMonster.Requirements;
            cloneMonster.BasePower = sourceMonster.BasePower;
            cloneMonster.PowerBonuses = sourceMonster.PowerBonuses;
            cloneMonster.Effect = sourceMonster.Effect;
        }
        else if (source is TerrainCardResource sourceTerrain && clone is TerrainCardResource cloneTerrain)
        {
            cloneTerrain.ProducedResources = sourceTerrain.ProducedResources;
        }
        else if (source is KingCardResource sourceKing && clone is KingCardResource cloneKing)
        {
            cloneKing.ElementFocus = sourceKing.ElementFocus;
            cloneKing.Health = sourceKing.Health;
            cloneKing.QuestText = sourceKing.QuestText;
            cloneKing.QuestRequirements = sourceKing.QuestRequirements;
        }

        return clone;
    }

    private static void CopyCommonFields(CardResource source, CardResource target)
    {
        target.Id = source.Id;
        target.CardType = source.CardType;
        target.InternalTier = source.InternalTier;
        target.CardImageTexture = source.CardImageTexture;
        target.CardImageSourcePath = source.CardImageSourcePath;
        target.BackImageTexture = source.BackImageTexture;
    }

    private static CardResource CreateCardForType(CardType cardType)
    {
        return cardType switch
        {
            CardType.Terrain => new TerrainCardResource(),
            CardType.King => new KingCardResource(),
            _ => new MonsterCardResource()
        };
    }

    private static CardType CardTypeForResource(CardResource card)
    {
        return card switch
        {
            MonsterCardResource => CardType.Monster,
            TerrainCardResource => CardType.Terrain,
            KingCardResource => CardType.King,
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
