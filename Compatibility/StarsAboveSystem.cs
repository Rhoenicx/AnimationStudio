using AnimationStudio.UI;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static AnimationStudio.ModUtils;

namespace AnimationStudio.Compatibility;

public class StarsAboveSystem : ModSystem
{
    // Reference to Stars Above
    private static bool _starsAboveLoaded;
    private static Mod _starsAbove;

    // Injections into Stars Above
    private static MethodInfo _starfarerMenuDrawSelf;
    private static ILHook _starfarerMenuDrawSelfHook;
    private static Type _starsAbovePlayerType;
    private static Type _starfarerMenuAnimationType;
    private static MethodInfo _starsAbovePlayerPostUpdate;
    private static ILHook _starsAbovePlayerPostUpdateHook;
    private static MethodInfo _starfarerMenuAnimationPreUpdate;
    private static ILHook _starfarerMenuAnimationPreUpdateHook;

    // TSA names
    private const string Asphodene = "Stars Above - Asphodene";
    private const string Eridani = "Stars Above - Eridani";

    public override void Load()
    {
        _starsAboveLoaded = false;
        _starsAbove = null;
    }

    public override void PostSetupContent()
    {
        // Check if Stars Above mod is loaded.
        if (ModLoader.TryGetMod("StarsAbove", out _starsAbove))
        {
            _starsAboveLoaded = true;
        }

        if (_starsAboveLoaded)
        {
            // Get the types of the The Stars Above assembly
            Type[] starsAboveTypes = _starsAbove.GetType().Assembly.GetTypes();

            // Search for the type 'StarfarerMenu' inside the TSA assembly
            if (starsAboveTypes.Any(t => t.Name == "StarfarerMenu"))
            {
                // StarfarerMenu type found, now grab the type from the array.
                Type StarfarerMenu = starsAboveTypes.First(t => t.Name == "StarfarerMenu");

                // Try to get the DrawSelf method from the StarfarerMenu class.
                // This code contains all the draw calls to the StarfarerMenu UI element(s)
                _starfarerMenuDrawSelf = StarfarerMenu?.GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.NonPublic);

                if (_starfarerMenuDrawSelf != null)
                {
                    _starfarerMenuDrawSelfHook = new ILHook(_starfarerMenuDrawSelf, EditStarfarerMenuDrawSelf);
                    _starfarerMenuDrawSelfHook.Apply();
                }
            }

            // Search for the type 'StarsAbovePlayer' inside the TSA assembly
            if (starsAboveTypes.Any(t => t.Name == "StarsAbovePlayer"))
            {
                _starsAbovePlayerType = starsAboveTypes.First(t => t.Name == "StarsAbovePlayer");

                _starsAbovePlayerPostUpdate = _starsAbovePlayerType?.GetMethod("PostUpdate", BindingFlags.Instance | BindingFlags.Public);

                if (_starsAbovePlayerPostUpdate != null)
                {
                    _starsAbovePlayerPostUpdateHook = new ILHook(_starsAbovePlayerPostUpdate, EditStarsAbovePlayerPostUpdate);
                    _starsAbovePlayerPostUpdateHook.Apply();
                }
            }

            if (starsAboveTypes.Any(t => t.Name == "StarfarerMenuAnimation"))
            {
                _starfarerMenuAnimationType = starsAboveTypes.First(t => t.Name == "StarfarerMenuAnimation");

                _starfarerMenuAnimationPreUpdate = _starfarerMenuAnimationType?.GetMethod("PreUpdate", BindingFlags.Instance | BindingFlags.Public);

                if (_starfarerMenuAnimationPreUpdate != null)
                {
                    _starfarerMenuAnimationPreUpdateHook = new ILHook(_starfarerMenuAnimationPreUpdate, EditStarfarerMenuAnimationPostUpdate);
                    _starfarerMenuAnimationPreUpdateHook.Apply();
                }
            }

            // Add the controls
            RegisterStarsAbove();
        }
    }

    public override void Unload()
    {
        _starsAboveLoaded = false;
        _starsAbove = null;

        _starfarerMenuDrawSelf = null;
        _starfarerMenuDrawSelfHook?.Undo();
        _starfarerMenuDrawSelfHook = null;

        _starsAbovePlayerPostUpdate = null;
        _starsAbovePlayerPostUpdateHook?.Undo();
        _starsAbovePlayerPostUpdateHook = null;

        _starfarerMenuAnimationPreUpdate = null;
        _starfarerMenuAnimationPreUpdateHook?.Undo();
        _starfarerMenuAnimationPreUpdateHook = null;
    }


    private static void RegisterStarsAbove()
    {
        #region Animator controls Asphodene
        Animator.RegisterControl(Asphodene, "HeadRotation", 0f, -180f, 180f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationRotation", 0f, -180f, 180f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationTimer", 0f, 0, 1800f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationActive", 0f, 0, 1f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationProgress", 0f, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationProgressAlt", 0f, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationAlpha", 0f, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleAnimationAlphaFast", 0f, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Asphodene, "IdleMovement", 0f, -1000f, 1000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "MenuUIOpacity", 0f, 0f, 1f, 0.01f, "n2", Conversion.None);
        Animator.RegisterControl(Asphodene, "Outfit", 0f, 0f, 7f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Asphodene, "HairStyle", 0f, 0f, 1f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Asphodene, "BlinkTimer", 0f, 0f, 600f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Asphodene, "HeadPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "HeadPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "UpperArmPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "UpperArmPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "LowerArmPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "LowerArmPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "SwordPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "SwordPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "RightLegPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "RightLegPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "LeftLegPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "LeftLegPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "BodyPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "BodyPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "EyeRightPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "EyeRightPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "EyeLeftPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "EyeLeftPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "PonytailPosition1X", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "PonytailPosition1Y", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "PonytailPosition2X", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Asphodene, "PonytailPosition2Y", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        #endregion

        #region Animator controls Eridani
        Animator.RegisterControl(Eridani, "HeadRotation", -180f, -180f, 180f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationRotation", -180f, -180f, 180f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationTimer", 0f, 0, 1800f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationActive", 0f, 0f, 1f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationProgress", 0f, 0f, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationProgressAlt", 0, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationAlpha", 0, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationAlphaFast", 0, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleAnimationReading", 0f, 0, 1f, 0.001f, "n3", Conversion.None);
        Animator.RegisterControl(Eridani, "IdleMovement", 0f, -1000f, 1000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "MenuUIOpacity", 0f, 0f, 1f, 0.01f, "n2", Conversion.None);
        Animator.RegisterControl(Eridani, "Outfit", 0f, 0f, 7f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Eridani, "HairStyle", 0f, 0f, 1f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Eridani, "BlinkTimer", 0f, 0f, 600f, 1f, "n0", Conversion.None);
        Animator.RegisterControl(Eridani, "HeadPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "HeadPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "LowerArmPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "LowerArmPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "PonytailPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "PonytailPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "RightLegPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "RightLegPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "LeftLegPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "LeftLegPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "BodyPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "BodyPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "EyeRightPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "EyeRightPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "EyeLeftPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "EyeLeftPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "BookPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "BookPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "ArmsCrossedPositionX", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        Animator.RegisterControl(Eridani, "ArmsCrossedPositionY", 0f, 0f, 2000f, 0.1f, "n1", Conversion.None);
        #endregion
    }

    #region TSA Injections
    private static void EditStarfarerMenuDrawSelf(ILContext il)
    {
        ILCursor c = new(il);

        #region fields asphodene
        int AsphodeneHeadPosition = -1;
        int AsphodeneUpperArmPosition = -1;
        int AsphodeneLowerArmPosition = -1;
        int SwordPosition = -1;
        int AsphodeneRightLegPosition = -1;
        int AsphodeneLeftLegPosition = -1;
        int AsphodeneBodyPosition = -1;
        int AsphodeneEyeRightPosition = -1;
        int AsphodeneEyeLeftPosition = -1;
        int AsphodenePonytailPosition1 = -1;
        int AsphodenePonytailPosition2 = -1;
        #endregion

        if (c.TryGotoNext(
            x => x.MatchLdloca(out AsphodeneHeadPosition),
            x => x.MatchLdloca(out AsphodeneUpperArmPosition),
            x => x.MatchLdloca(out AsphodeneLowerArmPosition),
            x => x.MatchLdloca(out SwordPosition),
            x => x.MatchLdloca(out AsphodeneRightLegPosition),
            x => x.MatchLdloca(out AsphodeneLeftLegPosition),
            x => x.MatchLdloca(out AsphodeneBodyPosition),
            x => x.MatchLdloca(out AsphodeneEyeRightPosition),
            x => x.MatchLdloca(out AsphodeneEyeLeftPosition),
            x => x.MatchLdloca(out AsphodenePonytailPosition1),
            x => x.MatchLdloca(out AsphodenePonytailPosition2),
            x => x.MatchCall("StarsAbove.UI.StarfarerMenu.StarfarerMenu", "SetupAsphodeneTextures")))
        {
            c.Index += 12;

            // Head position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "HeadPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneHeadPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneHeadPosition);

            // Upper arm position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "UpperArmPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneUpperArmPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneUpperArmPosition);

            // Lower arm position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "LowerArmPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneLowerArmPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneLowerArmPosition);

            // Sword position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "SwordPosition");
            c.Emit(OpCodes.Ldloc, SwordPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, SwordPosition);

            // Right leg position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "RightLegPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneRightLegPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneRightLegPosition);

            // Left leg position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "LeftLegPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneLeftLegPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneLeftLegPosition);

            // Body position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "BodyPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneBodyPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneBodyPosition);

            // Eye right position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "EyeRightPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneEyeRightPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneEyeRightPosition);

            // Eye left position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "EyeLeftPosition");
            c.Emit(OpCodes.Ldloc, AsphodeneEyeLeftPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodeneEyeLeftPosition);

            // Ponytail 1 position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "PonytailPosition1");
            c.Emit(OpCodes.Ldloc, AsphodenePonytailPosition1);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodenePonytailPosition1);

            // Ponytail 2 position
            c.Emit(OpCodes.Ldstr, Asphodene);
            c.Emit(OpCodes.Ldstr, "PonytailPosition2");
            c.Emit(OpCodes.Ldloc, AsphodenePonytailPosition2);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, AsphodenePonytailPosition2);
        }

        #region fields Eridani
        int EridaniHeadPosition = -1;
        int EridaniLowerArmPosition = -1;
        int EridaniPonytailPosition = -1;
        int EridaniRightLegPosition = -1;
        int EridaniLeftLegPosition = -1;
        int EridaniBodyPosition = -1;
        int EridaniEyeRightPosition = -1;
        int EridaniEyeLeftPosition = -1;
        int EridaniBookPosition = -1;
        int EridaniArmsCrossedPosition = -1;
        #endregion

        if (c.TryGotoNext(
            x => x.MatchLdloca(out EridaniHeadPosition),
            x => x.MatchLdloca(out EridaniLowerArmPosition),
            x => x.MatchLdloca(out EridaniPonytailPosition),
            x => x.MatchLdloca(out EridaniRightLegPosition),
            x => x.MatchLdloca(out EridaniLeftLegPosition),
            x => x.MatchLdloca(out EridaniBodyPosition),
            x => x.MatchLdloca(out EridaniEyeRightPosition),
            x => x.MatchLdloca(out EridaniEyeLeftPosition),
            x => x.MatchLdloca(out EridaniBookPosition),
            x => x.MatchLdloca(out EridaniArmsCrossedPosition),
            x => x.MatchCall("StarsAbove.UI.StarfarerMenu.StarfarerMenu", "SetupEridaniTextures")))
        {
            c.Index += 11;

            // Head position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "HeadPosition");
            c.Emit(OpCodes.Ldloc, EridaniHeadPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniHeadPosition);

            // Upper arm position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "ArmsCrossedPosition");
            c.Emit(OpCodes.Ldloc, EridaniArmsCrossedPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniArmsCrossedPosition);

            // Lower arm position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "LowerArmPosition");
            c.Emit(OpCodes.Ldloc, EridaniLowerArmPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniLowerArmPosition);

            // Book position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "BookPosition");
            c.Emit(OpCodes.Ldloc, EridaniBookPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniBookPosition);

            // Right leg position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "RightLegPosition");
            c.Emit(OpCodes.Ldloc, EridaniRightLegPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniRightLegPosition);

            // Left leg position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "LeftLegPosition");
            c.Emit(OpCodes.Ldloc, EridaniLeftLegPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniLeftLegPosition);

            // Body position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "BodyPosition");
            c.Emit(OpCodes.Ldloc, EridaniBodyPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniBodyPosition);

            // Eye right position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "EyeRightPosition");
            c.Emit(OpCodes.Ldloc, EridaniEyeRightPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniEyeRightPosition);

            // Eye left position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "EyeLeftPosition");
            c.Emit(OpCodes.Ldloc, EridaniEyeLeftPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniEyeLeftPosition);

            // Ponytail position
            c.Emit(OpCodes.Ldstr, Eridani);
            c.Emit(OpCodes.Ldstr, "PonytailPosition");
            c.Emit(OpCodes.Ldloc, EridaniPonytailPosition);
            c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorVector2", BindingFlags.Static | BindingFlags.NonPublic));
            c.Emit(OpCodes.Stloc, EridaniPonytailPosition);
        }
    }

    private static void EditStarsAbovePlayerPostUpdate(ILContext il)
    {
        ILCursor c = new(il);

        // Move to the end of the method
        while (c.Next != null)
        {
            c.Index++;
        }

        // Before RET
        if (!c.TryGotoPrev(moveType: MoveType.AfterLabel, x => x.MatchRet()))
        {
            return;
        }

        ILLabel exit = c.DefineLabel();

        // Only run on the own player.
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Call, typeof(ModPlayer).GetMethod("get_Player", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Ldfld, typeof(Player).GetField("whoAmI", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("myPlayer", BindingFlags.Static | BindingFlags.Public));
        c.Emit(OpCodes.Bne_Un, exit);

        // Asphodene Outfit
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "Outfit");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerOutfitVisible", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerOutfitVisible", BindingFlags.Instance | BindingFlags.Public));

        // Eridani Outfit
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "Outfit");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerOutfitVisible", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerOutfitVisible", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene HairStyle
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "HairStyle");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerHairstyle", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerHairstyle", BindingFlags.Instance | BindingFlags.Public));

        // Eridani HairStyle
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "HairStyle");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerHairstyle", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerHairstyle", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene MenuUIOpacity
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "MenuUIOpacity");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerMenuUIOpacity", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerMenuUIOpacity", BindingFlags.Instance | BindingFlags.Public));

        // Eridani MenuUIOpacity
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "MenuUIOpacity");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("starfarerMenuUIOpacity", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("starfarerMenuUIOpacity", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene BlinkTimer
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "BlinkTimer");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("blinkTimer", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("blinkTimer", BindingFlags.Instance | BindingFlags.Public));

        // Eridani BlinkTimer
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "BlinkTimer");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starsAbovePlayerType.GetField("blinkTimer", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starsAbovePlayerType.GetField("blinkTimer", BindingFlags.Instance | BindingFlags.Public));

        c.MarkLabel(exit);
    }

    private static void EditStarfarerMenuAnimationPostUpdate(ILContext il)
    {
        ILCursor c = new(il);

        while (c.Next != null)
        {
            c.Index++;
        }

        // Before RET
        if (!c.TryGotoPrev(moveType: MoveType.AfterLabel, x => x.MatchRet()))
        {
            return;
        }

        ILLabel exit = c.DefineLabel();

        // Only run on the own player.
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Call, typeof(ModPlayer).GetMethod("get_Player", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Ldfld, typeof(Player).GetField("whoAmI", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("myPlayer", BindingFlags.Static | BindingFlags.Public));
        c.Emit(OpCodes.Bne_Un, exit);

        // Asphodene HeadRotation
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "HeadRotation");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuHeadRotation", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuHeadRotation", BindingFlags.Instance | BindingFlags.Public));

        // Eridani HeadRotation
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "HeadRotation");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuHeadRotation", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuHeadRotation", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationRotation
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationRotation");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleAnimationRotation", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleAnimationRotation", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationRotation
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationRotation");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleAnimationRotation", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleAnimationRotation", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleMovement
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleMovement");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleMovement", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleMovement", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleMovement
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleMovement");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleMovement", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("StarfarerMenuIdleMovement", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationTimer
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationTimer");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationTimer", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationTimer", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationTimer
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationTimer");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationTimer", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorInt", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationTimer", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationActive
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationActive");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationActive", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorBool", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationActive", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationActive
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationActive");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationActive", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorBool", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationActive", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationProgress
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationProgress");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationProgress", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationProgress", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationProgress
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationProgress");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationProgress", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationProgress", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationProgressAlt
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationProgressAlt");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationProgressAlt", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationProgressAlt", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationProgressAlt
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationProgressAlt");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationProgressAlt", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationProgressAlt", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene idleAnimationAlpha
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationAlpha");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationAlpha", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationAlpha", BindingFlags.Instance | BindingFlags.Public));

        // Eridani idleAnimationAlpha
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationAlpha");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationAlpha", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationAlpha", BindingFlags.Instance | BindingFlags.Public));

        // Asphodene IdleAnimationAlphaFast
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Asphodene);
        c.Emit(OpCodes.Ldstr, "IdleAnimationAlphaFast");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationAlphaFast", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationAlphaFast", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationAlphaFast
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationAlphaFast");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationAlphaFast", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationAlphaFast", BindingFlags.Instance | BindingFlags.Public));

        // Eridani IdleAnimationAlphaFast
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldstr, Eridani);
        c.Emit(OpCodes.Ldstr, "IdleAnimationReading");
        c.Emit(OpCodes.Ldarg, 0);
        c.Emit(OpCodes.Ldfld, _starfarerMenuAnimationType.GetField("idleAnimationReading", BindingFlags.Instance | BindingFlags.Public));
        c.Emit(OpCodes.Call, typeof(StarsAboveSystem).GetMethod("AnimatorFloat", BindingFlags.Static | BindingFlags.NonPublic));
        c.Emit(OpCodes.Stfld, _starfarerMenuAnimationType.GetField("idleAnimationReading", BindingFlags.Instance | BindingFlags.Public));

        c.MarkLabel(exit);
    }

    private static Vector2 AnimatorVector2(string element, string control, Vector2 value)
    {
        return new Vector2(AnimatorFloat(element, control + "X", value.X), AnimatorFloat(element, control + "Y", value.Y));
    }

    private static float AnimatorFloat(string element, string control, float value)
    {
        return Animator.AnimationValue(element, control, value);
    }

    private static int AnimatorInt(string element, string control, int value)
    {
        return (int)Math.Round(Animator.AnimationValue(element, control, value));
    }

    private static int AnimatorBool(string element, string control, bool value)
    {
        return (float)Math.Round(Animator.AnimationValue(element, control, value ? 1f : 0f)) > 0.5f ? 1 : 0;
    }
    #endregion
}
