using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria.UI;
using Terraria;
using System;
using ReLogic.Content;
using static Terraria.ModLoader.ModContent;

namespace AnimationStudio.UI.Components
{
    internal class UISlider : UIElement
    {
        // Whether the use is dragging the slider
        private bool _dragging;

        public float Value;
        public float _oldValue;
        public bool Control;
        public float MinValue;
        public float MaxValue;

        // Base size of the Color Bar element
        private Rectangle _rect = new(0, 0, 178, 16);

        // Width of the side to the start of the inner colors
        private const int _rectSideWidth = 5;

        // Grab the vanilla textures for color sliders... (These are already loaded in during mod.load())
        private readonly Asset<Texture2D> _colorBarTex = TextureAssets.ColorBar;
        private readonly Asset<Texture2D> _sliderHighlightTex = TextureAssets.ColorHighlight;
        private readonly Asset<Texture2D> _sliderBackTex = Request<Texture2D>("AnimationStudio/UI/Components/SliderGradient");
        private readonly Asset<Texture2D> _colorSliderTex = TextureAssets.ColorSlider;

        // On create set this:
        public UISlider(float minValue = 0f, float maxValue = 1f)
        {
            // Size of the element
            Width.Set(_rect.Width, 0f);
            Height.Set(_rect.Height, 0f);
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public event EventHandler OnValueChanged;

        public void SetValue(float value)
        { 
            Value = Math.Clamp(value, MinValue, MaxValue);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            CalculatedStyle dimensions = GetInnerDimensions();

            // Check whether the user has started dragging the slider and still has the mouse button held down
            if (_dragging)
            {
                // Calculate the point of the mouse cursor as a 0f to 1f float depending on the width of the color bar
                float step = Math.Clamp((Main.MouseScreen.X - dimensions.Position().X) / dimensions.Width, 0f, 1f);
                Value = MinValue + step * (MaxValue - MinValue);

                // If the mouse cursor is outside on the left of the color bar
                if (Main.MouseScreen.X < dimensions.Position().X)
                {
                    // Limit point X to MinValue
                    Value = MinValue;
                }

                // If the mouse cursor is outside on the right of the color bar
                if (Main.MouseScreen.X > dimensions.Position().X + dimensions.Width)
                {
                    // Limit point X to MaxValue
                    Value = MaxValue;
                }
            }

            // When the value changes, trigger event to write it back
            if (Control && _oldValue != Value && OnValueChanged != null)
            {
                _oldValue = Value;
                OnValueChanged(this, new EventArgs());
            }
        }

        // Rising edge on mouse down
        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            // user started 'dragging' 
            _dragging = true;
        }

        // Rising edge on mouse up
        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            // user stopped 'dragging' 
            _dragging = false;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            // Size of this element
            CalculatedStyle dimensions = GetInnerDimensions();

            // Draw the normal color bar texture without inner texture
            spriteBatch.Draw(_colorBarTex.Value, dimensions.Center(), _rect, Color.White, 0f, _colorBarTex.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // If we're hovering over this bar with the mouse cursor
            if (IsMouseHovering)
            {
                // Draw the highlight texture
                spriteBatch.Draw(_sliderHighlightTex.Value, dimensions.Center(), _sliderHighlightTex.Value.Bounds, Main.OurFavoriteColor, 0f, _sliderHighlightTex.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            // Draw the background gradient
            spriteBatch.Draw(_sliderBackTex.Value, new Vector2(dimensions.X + _rectSideWidth, dimensions.Y + 4), Color.White);

            float SliderPos = (dimensions.Width - 4) * ((Value - MinValue) / (MaxValue - MinValue));

            // Draw the Slider on top of the color bar
            spriteBatch.Draw(_colorSliderTex.Value, new Vector2(dimensions.X + SliderPos - _colorSliderTex.Width() / 2, dimensions.Y - 4), _colorSliderTex.Value.Bounds, Color.White);
        }
    }
}