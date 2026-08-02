using System;
using System.Text;
using CardGeneration.Resources.Enums;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public abstract partial class CardToolScreen : Control
{
    protected CardToolService CardToolService { get; private set; } = null!;

    private Label? _status;

    public event Action? BackRequested;

    public void Setup(CardToolService cardToolService)
    {
        CardToolService = cardToolService;
    }

    protected VBoxContainer BuildScreen(string titleText, string subtitleText)
    {
        ClearChildren();
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var background = new ColorRect
        {
            Color = new Color(0.055f, 0.048f, 0.07f)
        };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        shell.AddThemeConstantOverride("separation", 14);
        margin.AddChild(shell);

        var header = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        header.AddThemeConstantOverride("separation", 14);
        shell.AddChild(header);

        var titleBlock = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        header.AddChild(titleBlock);

        var title = new Label
        {
            Text = titleText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        titleBlock.AddChild(title);

        var subtitle = new Label
        {
            Text = subtitleText,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleBlock.AddChild(subtitle);

        var backButton = new Button
        {
            Text = "Back",
            CustomMinimumSize = new Vector2(110, 42)
        };
        backButton.Pressed += () => BackRequested?.Invoke();
        header.AddChild(backButton);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        shell.AddChild(scroll);

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.AddChild(panel);

        var contentMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        contentMargin.AddThemeConstantOverride("margin_left", 18);
        contentMargin.AddThemeConstantOverride("margin_right", 18);
        contentMargin.AddThemeConstantOverride("margin_top", 18);
        contentMargin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(contentMargin);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 12);
        contentMargin.AddChild(content);

        _status = new Label
        {
            Text = string.Empty,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        shell.AddChild(_status);

        return content;
    }

    protected void SetStatus(string message, bool isError = false)
    {
        if (_status is null)
        {
            return;
        }

        _status.Text = message;
        _status.RemoveThemeColorOverride("font_color");
        _status.AddThemeColorOverride("font_color", isError ? new Color(1.0f, 0.42f, 0.36f) : new Color(0.75f, 0.93f, 0.74f));

        if (isError)
        {
            GD.PushError(message);
        }
    }

    protected static Button AddButton(Container parent, string text, Action onPressed, float minWidth = 110)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(minWidth, 40),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        button.Pressed += onPressed;
        parent.AddChild(button);
        return button;
    }

    protected static LineEdit AddLineEdit(VBoxContainer parent, string labelText, string initialText = "")
    {
        parent.AddChild(new Label { Text = labelText });
        var lineEdit = new LineEdit
        {
            Text = initialText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(lineEdit);
        return lineEdit;
    }

    protected static TextEdit AddTextEdit(VBoxContainer parent, string labelText, string initialText = "", int minHeight = 90)
    {
        parent.AddChild(new Label { Text = labelText });
        var textEdit = new TextEdit
        {
            Text = initialText,
            CustomMinimumSize = new Vector2(0, minHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(textEdit);
        return textEdit;
    }

    protected static OptionButton AddOptionButton(VBoxContainer parent, string labelText, string[] options)
    {
        parent.AddChild(new Label { Text = labelText });
        var optionButton = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        foreach (var option in options)
        {
            optionButton.AddItem(option);
        }

        parent.AddChild(optionButton);
        return optionButton;
    }

    protected static SpinBox AddSpinBox(VBoxContainer parent, string labelText, double minValue, double maxValue, double step, double initialValue = 0)
    {
        parent.AddChild(new Label { Text = labelText });
        var spinBox = new SpinBox
        {
            MinValue = minValue,
            MaxValue = maxValue,
            Step = step,
            Value = initialValue,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(spinBox);
        return spinBox;
    }

    protected static void AddSeparator(Container parent)
    {
        parent.AddChild(new HSeparator());
    }

    protected static CardType GetSelectedCardType(OptionButton optionButton)
    {
        return Enum.TryParse<CardType>(GetSelectedText(optionButton), true, out var cardType)
            ? cardType
            : CardType.Unknown;
    }

    protected static string GetSelectedText(OptionButton optionButton)
    {
        return optionButton.Selected >= 0 ? optionButton.GetItemText(optionButton.Selected) : string.Empty;
    }

    protected static void SelectOption(OptionButton optionButton, string value)
    {
        for (var index = 0; index < optionButton.ItemCount; index++)
        {
            if (optionButton.GetItemText(index).Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                optionButton.Select(index);
                return;
            }
        }
    }

    protected static string MakeResourceId(string text, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(text) ? fallback : text;
        var builder = new StringBuilder();
        var lastWasSeparator = false;

        foreach (var character in source.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasSeparator = false;
                continue;
            }

            if (!lastWasSeparator)
            {
                builder.Append('_');
                lastWasSeparator = true;
            }
        }

        var id = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(id) ? fallback : id;
    }

    private void ClearChildren()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }
}
