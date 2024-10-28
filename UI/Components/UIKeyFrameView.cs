using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using static AnimationStudio.ModUtils;

namespace AnimationStudio.UI.Components;

public class UIKeyFrameView : UIElement
{
    private Texture2D _texture;

    public int PlayerTime;
    public int Min;
    public int Max;
    public bool OnKeyFrame;

    public void GenerateTexture(AnimationSettings setting, int min, int max)
    {
        // Fetch the size of the UI element
        int width = (int)Width.Pixels;
        int height = (int)Height.Pixels;

        // Create a new Color array which will hold the 
        // pixels of the texture
        Color[] data = new Color[width * height];
        Array.Fill(data, Color.Black);

        if (setting.KeyFrames != null)
        {
            // When there is no keyframe, draw a straight line
            if (setting.KeyFrames.Count <= 0)
            {
                int middle = height / 2;

                for (int i = width * middle; i < width * middle + width; i++)
                {
                    data[i] = Color.White;
                }
            }

            // When there is 1 keyframe, draw a straight line and the keyframe
            else if (setting.KeyFrames.Count <= 1)
            {
                // Draw the line for the single keyframe
                DrawVerticalLine(ref data, min, max, setting.KeyFrames.First().Key, width, height, Color.Red);

                int middle = height / 2;

                for (int i = width * middle; i < width * middle + width; i++)
                {
                    data[i] = Color.White;
                }
            }
            else
            {
                // Save the highest and lowest keyframe values
                float highest = float.MinValue;
                float lowest = float.MaxValue;

                // Draw line for all keyframes within the bounds
                foreach (int key in setting.KeyFrames.Keys)
                {
                    // Only select valid keyframes
                    if (key < min || key > max || !setting.KeyFrames.TryGetValue(key, out KeyFrame keyFrame))
                    {
                        continue;
                    }

                    // Determine the highest value
                    if (keyFrame.Value > highest)
                    { 
                        highest = keyFrame.Value;
                    }

                    // Determine the lowest value
                    if (keyFrame.Value < lowest)
                    { 
                        lowest = keyFrame.Value;
                    }

                    // Draw the line for the keyframe
                    DrawVerticalLine(ref data, min, max, key, width, height, Color.Red);
                }

                // When there is no variation between highest and lowest,
                // draw a line in the middle.
                if (lowest == highest)
                {
                    int middle = height / 2;

                    for (int i = width * middle; i < width * middle + width; i++)
                    {
                        data[i] = Color.White;
                    }
                }
                else
                {
                    // Map the animation length to the texture width
                    // by getting the distance per pixel for each tick
                    // in the animation.
                    float pieceX = (float)(max - min) / (float)width;
                    float pieceY = (float)height / (float)(highest - lowest);

                    float offset = 5f / pieceY;

                    highest += offset;
                    lowest -= offset;

                    pieceY = (float)height / (float)(highest - lowest);

                    // Loop over the width of the texture
                    for (int posX = 0; posX < width; posX++)
                    {
                        // Revert the mapping to get the time belonging
                        // to this pixel.
                        int time = (int)(posX * pieceX);

                        // Update the value of the animation
                        setting.UpdateAnimationValue(time);

                        // Get the corresponding Y position of the value
                        int posY = (int)(Math.Abs(setting.Value - highest) * pieceY);

                        // Paint this pixel
                        int pos = posY * width + posX;

                        if (pos > 0 && pos < data.Length)
                        {
                            data[pos] = Color.White;
                        }
                    }
                }
            }
        }

        // Generate the new texture
        if (_texture == null)
        {
            Main.QueueMainThreadAction(() => _texture = new Texture2D(Main.instance.GraphicsDevice, width, height));
        }

        // Set the data to the texture.
        Main.QueueMainThreadAction(() => _texture.SetData(data));
    }

    private void DrawVerticalLine(ref Color[] data, int min, int max, int key, int width, int height, Color color, int startHeight = 0, int endHeight = int.MaxValue)
    {
        float piece = (float)width / (float)(max - min);
        int pos = (int)(piece * key);

        if (startHeight < 0)
        {
            startHeight = 0;
        }
        startHeight *= width;

        if (endHeight > height)
        { 
            endHeight = height;
        }
        endHeight *= width;

        for (int i = 0; i < width * height; i++)
        {
            if (i < startHeight || i > endHeight)
            {
                continue;
            }

            if (i % width == pos)
            {
                data[i] = color;
            }
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (_texture == null)
        {
            return;
        }

        // Size of this element
        CalculatedStyle dimensions = GetInnerDimensions();

        spriteBatch.Draw(
            _texture,
            dimensions.Center(),
            new Rectangle(0, 0, _texture.Width, _texture.Height),
            Color.White,
            0f,
            _texture.Size() * 0.5f,
            1f,
            SpriteEffects.None,
            0);

        // Draw the cursor
        float posX = _texture.Width / (float)(Max - Min) * PlayerTime;

        spriteBatch.Draw(
            TextureAssets.MagicPixel.Value,
            dimensions.Position() + new Vector2(posX, 0f),
            new Rectangle(0,0,2,_texture.Height),
            OnKeyFrame ? Color.Purple : Color.Blue,
            0f,
            new Vector2(1, 0),
            1f,
            SpriteEffects.None,
            0);
    }
}
