using CardGeneration.Rendering;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

[GlobalClass]
public partial class CardPreviewControl : TextureRect
{
    private CardResource? _card;

    public void SetCard(CardResource? card)
    {
        _card = card;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_card is null)
        {
            Texture = null;
            return;
        }

        var image = CardImageRenderer.Render(_card);
        Texture = ImageTexture.CreateFromImage(image);
    }
}
