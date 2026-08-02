using Godot;

namespace CardGeneration.Ui;

public partial class HorizontalWheelScrollContainer : ScrollContainer
{
    private const int WheelStep = 96;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouseButton)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            ScrollHorizontal += WheelStep;
            AcceptEvent();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            ScrollHorizontal = Mathf.Max(0, ScrollHorizontal - WheelStep);
            AcceptEvent();
        }
    }
}
