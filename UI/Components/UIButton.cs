using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using static Terraria.ModLoader.ModContent;

namespace AnimationStudio.UI.Components;

public class UIButton : UIElement
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
    public bool Right;
    private static Dictionary<string, Asset<Texture2D>> _textures;
    private readonly string _textureName;
    private int _timer;
    private readonly bool _holdable;

    public UIButton(bool holdable = true, string textureName = "ButtonEmpty")
    {
        _textureName = textureName;
        LoadTexture();

        Width.Set(Texture.Width() / 2, 0f);
        Height.Set(Texture.Height(), 0f);
        _holdable = holdable;
    }

    private void LoadTexture()
    {
        _textures ??= [];

        if (!_textures.ContainsKey(_textureName))
        {
            _textures[_textureName] = Request<Texture2D>("AnimationStudio/UI/Components/" + _textureName, AssetRequestMode.ImmediateLoad);
        }
    }

    public event EventHandler OnButtonTrigger;

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        OnButtonTrigger?.Invoke(this, new EventArgs());
        base.LeftMouseUp(evt);
    }

    public override void RightMouseUp(UIMouseEvent evt)
    {
        OnButtonTrigger?.Invoke(this, new EventArgs());
        base.RightMouseUp(evt);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_holdable && Value)
        {
            _timer++;
        }
        else
        {
            _timer = 0;
        }

        if ((_timer >= 30 && _timer < 60 && _timer % 2 == 0)
            || _timer >= 60)
        {
            OnButtonTrigger?.Invoke(this, new EventArgs());
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
            new Rectangle(Texture.Width() / 2 * (Value ? 1 : 0), 0, Texture.Width() / 2, Texture.Height()),
            Color.White,
            0f,
            new Vector2(Texture.Width() / 2, Texture.Height()) * 0.5f,
            1f,
            SpriteEffects.None,
            0);
    }
}
