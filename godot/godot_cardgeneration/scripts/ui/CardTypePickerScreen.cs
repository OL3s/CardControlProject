using System;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class CardTypePickerScreen : CardToolScreen
{
    public event Action<CardType>? CardTypeSelected;

    public override void _Ready()
    {
        var content = BuildScreen("New Card", "Choose the card type before editing. Monster, terrain and king cards have different data and setup.");

        var choices = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        choices.AddThemeConstantOverride("separation", 18);
        content.AddChild(choices);

        AddCardTypeChoice(
            choices,
            "Monster",
            "Combat card with element, requirements, base power, bonuses and optional effect.",
            CardType.Monster);
        AddCardTypeChoice(
            choices,
            "Terrain",
            "Map card with element focus and produced resources.",
            CardType.Terrain);
        AddCardTypeChoice(
            choices,
            "King",
            "Player king card with health and quest text.",
            CardType.King);
    }

    private void AddCardTypeChoice(HBoxContainer parent, string title, string description, CardType cardType)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(240, 360),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        layout.AddChild(titleLabel);

        var previewRow = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        layout.AddChild(previewRow);
        previewRow.AddChild(CardPreviewControl.Create(
            CreatePreviewCard(cardType),
            showBack: true,
            minimumSize: new Vector2(120, 168),
            renderSize: new Vector2I(120, 168)));

        layout.AddChild(new Label
        {
            Text = description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill
        });

        AddButton(layout, $"Create {title}", () => CardTypeSelected?.Invoke(cardType), 0);
    }

    private static CardResource CreatePreviewCard(CardType cardType)
    {
        return cardType switch
        {
            CardType.Terrain => new TerrainCardResource { Id = "terrain_back_preview" },
            CardType.King => new KingCardResource { Id = "king_back_preview" },
            _ => new MonsterCardResource { Id = "monster_back_preview" }
        };
    }
}
