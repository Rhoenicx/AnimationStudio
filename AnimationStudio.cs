using AnimationStudio.UI;
using System;
using Terraria.ModLoader;
using static AnimationStudio.ModUtils;

namespace AnimationStudio;

public class AnimationStudio : Mod
{
    public override object Call(params object[] args)
    {
        try
        {
            if (args.Length <= 0 || args[0] is not string message)
            {
                return "AnimationStudio Call Error: Expected argument 0 to be of string type";
            }

            switch (message)
            {
                //----------------------------------------------------------
                case "RegisterElement":
                    // Not enough arguments to even consider trying to read
                    if (args.Length < 2)
                    {
                        return "AnimationStudio Call Error [RegisterElement]: Too few arguments, expected total of 2";
                    }

                    if (args[1] is not string s1)
                    {
                        return "AnimationStudio Call Error [RegisterElement]: Expected argument 1 to be of string type";
                    }

                    if (s1 == null || s1 == "" || !Animator.RegisterElement(s1))
                    {
                        return "AnimationStudio Call Error [RegisterElement]: Given string argument (1) cannot be null or empty";
                    }

                    return "Success";

                //----------------------------------------------------------
                case "RegisterControl":
                    // Not enough arguments to even consider trying to read
                    if (args.Length < 3)
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Too few arguments, expected at least 3";
                    }

                    // Verify the given string ElementName
                    if (args[1] is not string elementName)
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Expected argument 1 to be of string type";
                    }

                    if (elementName == null || elementName == "")
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Given string argument (1) cannot be null or empty";
                    }

                    // Verify the given string ControlName
                    if (args[2] is not string controlName)
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Expected argument 2 to be of string type";
                    }

                    if (controlName == null || controlName == "")
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Given string argument (2) cannot be null or empty";
                    }

                    // If present, read the initial value, minimum value, maximum value, step value, format and conversion
                    float initialValue = args.Length > 3 && args[3] is float f1 ? f1 : 0f;
                    float minValue = args.Length > 4 && args[4] is float f2 ? f2 : 0f;
                    float maxValue = args.Length > 5 && args[5] is float f3 ? f3 : 1f;
                    float stepValue = args.Length > 6 && args[6] is float f4 ? f4 : 0.1f;
                    string format = args.Length > 7 && args[7] is string s2 ? s2 : "n0";
                    Conversion convert = args.Length > 8 && args[8] is int mode && Enum.IsDefined(typeof(Conversion), mode) ? (Conversion)mode : Conversion.None;

                    // Try to add the control
                    if (!Animator.RegisterControl(elementName, controlName, initialValue, minValue, maxValue, stepValue, format, convert))
                    {
                        return "AnimationStudio Call Error [RegisterControl]: Given element name cannot be null or empty";
                    }

                    return "Success";

                //----------------------------------------------------------
                case "AnimationValue":
                    // Not enough arguments to even consider trying to read
                    if (args.Length < 4)
                    {
                        return "AnimationStudio Call Error [AnimationValue]: Too few arguments, expected total of 4";
                    }

                    // Verify the given string ElementName
                    if (args[1] is not string elementName2)
                    {
                        return "AnimationStudio Call Error [AnimationValue]: Expected argument 1 to be of string type";
                    }

                    if (elementName2 == null || elementName2 == "")
                    {
                        return "AnimationStudio Call Error [AnimationValue]: Given string argument (1) cannot be null or empty";
                    }

                    // Verify the given string ControlName
                    if (args[2] is not string controlName2)
                    {
                        return "AnimationStudio Call Error [AnimationValue]: Expected argument 2 to be of string type";
                    }

                    if (controlName2 == null || controlName2 == "")
                    {
                        return "AnimationStudio Call Error [AnimationValue]: Given string argument (2) cannot be null or empty";
                    }

                    // Verify the given value
                    if (args[3] is not float value)
                    { 
                        return "AnimationStudio Call Error [AnimationValue]: Expected argument 3 to be of float type";
                    }

                    // When everything is parsed, try to get the animation value from the Animator
                    return Animator.AnimationValue(elementName2, controlName2, value);

                //----------------------------------------------------------
                case "AnimationStudioVisible":
                    return Animator.Visible;

                //----------------------------------------------------------
                case "ReadModeActive":
                    // Not enough arguments to even consider trying to read
                    if (args.Length < 2)
                    {
                        return "AnimationStudio Call Error [ReadModeActive]: Too few arguments, expected total of 2";
                    }

                    if (args[1] is not string s3)
                    {
                        return "AnimationStudio Call Error [ReadModeActive]: Expected argument 1 to be of string type";
                    }

                    if (s3 == null || s3 == "")
                    {
                        return "AnimationStudio Call Error [ReadModeActive]: Given string argument (1) cannot be null or empty";
                    }

                    return Animator.GetReadMode(s3);

                //----------------------------------------------------------
                case "SetPlayerTime":
                    // Not enough arguments to even consider trying to read
                    if (args.Length < 3)
                    {
                        return "AnimationStudio Call Error [SetPlayerTime]: Too few arguments, expected total of 3";
                    }

                    if (args[1] is not string s4)
                    {
                        return "AnimationStudio Call Error [SetPlayerTime]: Expected argument 1 to be of string type";
                    }

                    if (s4 == null || s4 == "")
                    {
                        return "AnimationStudio Call Error [SetPlayerTime]: Given string argument (1) cannot be null or empty";
                    }

                    if (args[2] is not int i)
                    { 
                        return "AnimationStudio Call Error [SetPlayerTime]: Expected argument 2 to be of int type";
                    }

                    Animator.SetPlayerTime(s4, i);
                    return "Success";

                //----------------------------------------------------------
                case "SelectedFilter":
                    return Animator.SelectedFilter;
            }
        }
        catch (Exception e)
        {
            Logger.Warn("AnimationStudio Call Error: " + e.StackTrace + " " + e.Message);
        }

        return "Failure";
    }
}