using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace AnimationStudio.UI.Components;

public class UIButtonStates : UIElement
{
    private Asset<Texture2D> Texture
    {
        get
        {
            if (_textures.TryGetValue(_textureName, out Asset<Texture2D> tex))
            {
                return tex;
            }

            return TextureAssets.MagicPixel;
        }
    }

    public bool Value;
    public int State;
    public readonly int States;
    private static Dictionary<string, Asset<Texture2D>> _textures;
    private readonly string _textureName;

    public UIButtonStates(int states = 1, string textureName = "ButtonEmpty")
    {
        _textureName = textureName;
        LoadTexture();

        Width.Set(Texture.Width() / 2, 0f);
        Height.Set(Texture.Height() / states, 0f);
        States = states;
    }

    private void LoadTexture()
    {
        _textures ??= [];

        if (!_textures.ContainsKey(_textureName))
        {
            _textures[_textureName] = Request<Texture2D>("AnimationStudio/UI/Components/" + _textureName, AssetRequestMode.ImmediateLoad);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        // Size of this element
        CalculatedStyle dimensions = GetInnerDimensions();

        // Draw the sprite depending on pressed value
        spriteBatch.Draw(
            Texture.Value,
            dimensions.Center(),
            new Rectangle(Texture.Width() / 2 * (Value ? 1 : 0), Texture.Height() / States * State, Texture.Width() / 2, Texture.Height() / States),
            Color.White,
            0f,
            new Vector2(Texture.Width() / 2, Texture.Height() / States) * 0.5f,
            1f,
            SpriteEffects.None,
            0);
    }
}
