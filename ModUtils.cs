using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using System.Linq;

namespace AnimationStudio;

public static class ModUtils
{
    #region Float Functions
    public static int Oscillate(int minValue, int maxValue, ref int currentValue, ref bool increasing)
    {
        if (currentValue == minValue)
        {
            increasing = true;
        }
        else if (currentValue == maxValue)
        {
            increasing = false;
        }

        currentValue += increasing ? 1 : -1;

        return currentValue;
    }

    public static float Pulse(float time)
    {
        return (float)(0.5 * (1 + Math.Sin(2 * Math.PI * 1 * time)));
    }

    public static float InQuad(float t) => t * t;

    public static float OutQuad(float t) => 1 - InQuad(1 - t);

    public static float InOutQuad(float t)
    {
        if (t < 0.5) return InQuad(t * 2) / 2;
        return 1 - InQuad((1 - t) * 2) / 2;
    }

    public static float EaseIn(float t)
    {
        return t * t;
    }

    public static float Flip(float x)
    {
        return 1 - x;
    }

    public static float EaseOut(float t)
    {
        return Flip((float)Math.Sqrt(Flip(t)));
    }

    public static float EaseInOut(float t)
    {
        return MathHelper.Lerp(EaseIn(t), EaseOut(t), t);
    }
    #endregion

    #region KeyFrames
    public static readonly int InvalidKey = -1;

    public static bool ContainsKeyFrame(this Dictionary<string, AnimationSettings> settings, string control, int time)
    {
        if (!settings.TryGetValue(control, out AnimationSettings setting) || setting == null || setting.KeyFrames == null)
        {
            return false;
        }

        return setting.KeyFrames.ContainsKeyFrame(time);
    }

    public static bool ContainsKeyFrame(this SortedDictionary<int, KeyFrame> keyFrames, int time)
    {
        return keyFrames.ContainsKey(time);
    }

    public static int SeekKeyAfter(this Dictionary<string, AnimationSettings> settings, string control, int time)
    {
        if (settings.TryGetValue(control, out AnimationSettings setting) || setting.KeyFrames == null)
        {
            return InvalidKey;
        }

        return setting.KeyFrames.SeekKeyAfter(time);
    }

    public static int SeekKeyAfter(this SortedDictionary<int, KeyFrame> keyFrames, int time)
    {
        // There are no KeyFrames available in the given dictionary
        if (keyFrames.Count <= 0)
        {
            return InvalidKey;
        }

        // Get the last key in the SortedDictionary.
        // Note that the key might be the same
        int last = keyFrames.Last().Key;

        // Check if the given key is the last key
        // or if its is outside the range.
        if (time >= last)
        {
            return InvalidKey;
        }

        // Scan forward to find the key
        for (int i = time + 1; i <= last; i++)
        {
            if (keyFrames.ContainsKey(i))
            {
                return i;
            }
        }

        return InvalidKey;
    }

    public static int SeekKeyBefore(this Dictionary<string, AnimationSettings> settings, string control, int time)
    {
        if (settings.TryGetValue(control, out AnimationSettings setting) || setting.KeyFrames == null)
        {
            return InvalidKey;
        }

        return setting.KeyFrames.SeekKeyBefore(time);
    }

    public static int SeekKeyBefore(this SortedDictionary<int, KeyFrame> keyFrames, int time)
    {
        // There are no KeyFrames available
        if (keyFrames.Count <= 0)
        {
            return InvalidKey;
        }

        // Get the first and last key in the SortedDictionary.
        // Note that hte key might be the same
        int first = keyFrames.First().Key;

        // Check if the given key is the first key
        // or if its is outside the range.
        if (time <= first)
        {
            return InvalidKey;
        }

        // Scan backward to find the key
        for (int i = time - 1; i >= first; i--)
        {
            if (keyFrames.ContainsKey(i))
            {
                return i;
            }
        }

        return InvalidKey;
    }

    public static bool AddKeyFrame(this Dictionary<string, AnimationSettings> settings, string control, int time, float value, KeyMode mode = KeyMode.Linear)
    {
        if (!settings.TryGetValue(control, out AnimationSettings setting) || setting == null)
        {
            setting = new();
            settings[control] = setting;
        }

        return setting.AddKeyFrame(time, value, mode);
    }

    public static bool AddKeyFrame(this AnimationSettings setting, int time, float value = 0f, KeyMode mode = KeyMode.Linear)
    {
        setting.KeyFrames ??= [];

        bool changed = false;

        if (!setting.KeyFrames.TryGetValue(time, out _))
        {
            KeyFrame keyFrame = new(value, mode);
            setting.KeyFrames[time] = keyFrame;
            setting.Begin = time;
            changed = true;
        }

        if (setting.KeyFrames[time].Value != value || setting.KeyFrames[time].Mode != mode)
        {
            changed = true;
        }

        setting.KeyFrames[time].Value = value;
        setting.KeyFrames[time].Mode = mode;

        return changed;
    }

    public static bool RemoveKeyFrame(this Dictionary<string, AnimationSettings> settings, string control, int time)
    {
        if (!settings.TryGetValue(control, out AnimationSettings setting))
        {
            return false;
        }

        return setting.RemoveKeyFrame(time);
    }

    public static bool RemoveKeyFrame(this AnimationSettings setting, int time)
    {
        if (setting.KeyFrames == null)
        {
            return false;
        }

        if (!setting.KeyFrames.ContainsKey(time))
        {
            return false;
        }

        if (time == setting.Begin)
        {
            setting.Begin = setting.KeyFrames.SeekKeyBefore(time);
        }

        return setting.KeyFrames.Remove(time);
    }

    public static bool ModifyKeyFrame(this Dictionary<string, AnimationSettings> settings, string control, int time, float value)
    {
        if (!settings.TryGetValue(control, out AnimationSettings setting))
        {
            return false;
        }

        if (setting.KeyFrames == null)
        {
            return false;
        }

        return setting.KeyFrames.ModifyKeyFrame(time, value);
    }

    public static bool ModifyKeyFrame(this SortedDictionary<int, KeyFrame> keyFrames, int time, float value)
    {
        if (!keyFrames.TryGetValue(time, out KeyFrame keyFrame))
        {
            return false;
        }

        keyFrame.Value = value;
        return true;
    }

    public static void UpdateAnimationValue(this Dictionary<string, AnimationSettings> settings, string control, int time)
    {
        if (!settings.TryGetValue(control, out AnimationSettings setting))
        {
            return;
        }

        setting.UpdateAnimationValue(time);
    }

    public static void UpdateAnimationValue(this AnimationSettings setting, int time)
    {
        // The KeyFramess are not present.
        if (setting.KeyFrames == null || setting.KeyFrames.Count <= 0)
        {
            return;
        }

        // The requested time is the same as a previous calculated
        // time => Do not update anything since it's not needed.
        if (time == setting.Time)
        {
            return;
        }

        // Shorthand to the keyframes
        SortedDictionary<int, KeyFrame> keyFrames = setting.KeyFrames;

        // There is a keyframe on this position, directly set the value
        if (keyFrames.TryGetValue(time, out KeyFrame keyFrame))
        { 
            setting.Value = keyFrame.Value;
            setting.Time = time;
            return;
        }

        // Whether the keys should be updated.
        bool update = false;

        // There are no previous keys
        if (setting.Begin == InvalidKey && setting.End == InvalidKey)
        {
            update = true;
        }

        // There is a previous key while out of bounds, check direction, check if time is now greater
        else if (setting.Begin == InvalidKey && setting.End == keyFrames.First().Key && time < setting.End)
        {
            update = true;
        }

        // There is a previous key while out of bounds, check direction, check if time is now less
        else if (setting.End == InvalidKey && setting.Begin == keyFrames.Last().Key && time >= setting.Begin)
        {
            update = true;
        }

        // There are 2 keys from previous calculation. Check if the given time falls outside these keys.
        else if (time < setting.Begin || time >= setting.End)
        {
            update = true;
        }

        // Update the keys if needed (this is expensive)
        if (update)
        {
            keyFrames.GetKeys(time, out setting.Begin, out setting.End);
        }

        // There are no Keys set; shouldn't be possible
        // This code need to be protected by a Count > 0 check!
        if (setting.Begin == InvalidKey && setting.End == InvalidKey)
        {
            return;
        }

        // No Begin Key, this means out of bounds (before all keyframes).
        if (setting.Begin == InvalidKey && keyFrames.TryGetValue(setting.End, out KeyFrame endKeyFrame))
        {
            // return value of end key
            setting.Value = endKeyFrame.Value;
            setting.Time = time;
            return;
        }

        // No End Key, this means out of bounds (after all keyframes)
        if (setting.End == InvalidKey && keyFrames.TryGetValue(setting.Begin, out KeyFrame beginKeyFrame))
        {
            // return value of begin key
            setting.Value = beginKeyFrame.Value;
            setting.Time = time;
            return;
        }

        // Verify both keys; just in case
        if (!keyFrames.TryGetValue(setting.Begin, out KeyFrame beginKeyFrame2)
            || !keyFrames.TryGetValue(setting.End, out KeyFrame endKeyFrame2))
        {
            return;
        }

        // Both keys are present, the given time is between 2 keyframes.
        // Interpolate between the 2 keys, this will be the progress between
        // the 2 keys from 0f to 1f.
        float progress = (float)(time - setting.Begin) / (float)(setting.End - setting.Begin);

        // Determine the keymode towards the next key.
        // This determines the shape of the 'progress' line.
        // Always uses the Mode of the Begin key.
        switch (beginKeyFrame2.Mode)
        {
            case KeyMode.InQuad:
                {
                    progress = InQuad(progress);
                }
                break;

            case KeyMode.OutQuad:
                {
                    progress = OutQuad(progress);
                }
                break;

            case KeyMode.InOutQuad:
                {
                    progress = InOutQuad(progress);
                }
                break;
        }

        // Return the interpolated value between the 2 keys
        setting.Value = beginKeyFrame2.Value + (endKeyFrame2.Value - beginKeyFrame2.Value) * progress;
        setting.Time = time;
    }

    public static void GetKeys(this Dictionary<string, SortedDictionary<int, KeyFrame>> keyFrames, string control, int time, out int begin, out int end)
    {
        // Initialize the keyframes to -1
        begin = InvalidKey;
        end = InvalidKey;

        if (!keyFrames.ContainsKey(control))
        {
            return;
        }

        keyFrames[control].GetKeys(time, out begin, out end);
    }

    public static void GetKeys(this SortedDictionary<int, KeyFrame> keyFrames, int time, out int begin, out int end)
    {
        // Initialize the keyframes to -1
        begin = InvalidKey;
        end = InvalidKey;

        // Check if there are enough keyframes available.
        if (keyFrames == null || keyFrames.Count <= 0)
        {
            return;
        }

        // There is only one keyFrame available.
        // Directly return the value of the key.
        if (keyFrames.Count == 1)
        {
            begin = keyFrames.First().Key;
            return;
        }

        if (keyFrames.ContainsKey(time))
        {
            begin = time;
        }
        else
        {
            begin = keyFrames.SeekKeyBefore(time);
        }

        end = keyFrames.SeekKeyAfter(time);
    }

    public class AnimationSettings(float min = 0f, float max = 1f, float step = 0.1f, string format = "n0", Conversion Convert = Conversion.None)
    {
        public SortedDictionary<int, KeyFrame> KeyFrames = [];

        public float MinValue = min;
        public float MaxValue = max;
        public float Step = step;
        public string Format = format;
        public Conversion Convert = Convert;
        public bool Lock = false;
        public KeyMode KeyMode = KeyMode.Linear;
        public bool Disable = false;
        public bool GenerateTexture = false;
        public int Time = -1;
        public float Value = 0f;
        public int Begin = InvalidKey;
        public int End = InvalidKey;

        public string ValueToText => GetValueText(Value);
        public string MinToText => GetValueText(MinValue);
        public string MaxToText => GetValueText(MaxValue);

        public string GetValueText(float value)
        {
            // If needed raw => degrees
            if (Convert == Conversion.ToDegrees)
            {
                value = MathHelper.ToDegrees(value);
            }

            // If needed raw => radians
            if (Convert == Conversion.ToRadians)
            { 
                value = MathHelper.ToRadians(value);
            }

            // Apply the string format on the returned value
            return value.ToString(Format);
        }
    }

    public class KeyFrame(float value = 0f, KeyMode mode = KeyMode.Linear)
    {
        public float Value = value;
        public KeyMode Mode = mode;
    }

    public enum KeyMode
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
    }

    public enum Conversion
    {
        None,
        ToDegrees,
        ToRadians,
    }
    #endregion
}
