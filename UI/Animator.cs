using AnimationStudio.UI.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using static AnimationStudio.ModUtils;

namespace AnimationStudio.UI;

public class AnimatorSystem : ModSystem
{
    public Animator Animator;
    public UserInterface AnimatorInterface;

    internal static AnimatorSystem Instance;

    public AnimatorSystem()
    { 
        Instance = this;
    }

    public override void Load()
    {
        if (!Main.dedServ)
        { 
            Animator = new Animator();
            Animator.Activate();

            AnimatorInterface = new UserInterface();
            AnimatorInterface.SetState(Animator);
        }
    }

    public override void Unload()
    {
        if (!Main.dedServ)
        {
            Animator = null;
            AnimatorInterface = null;
        }

        Instance = null;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int layer = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
        if (layer != -1)
        {
            layers.Insert(layer, new LegacyGameInterfaceLayer(
                "AnimationStudio: Animator",
                delegate
                {
                    if (Animator.Visible)
                    {
                        AnimatorInterface.Draw(Main.spriteBatch, new GameTime());
                    }
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (AnimatorInterface != null && Animator.Visible)
        {
            AnimatorInterface.Update(gameTime);
        }
    }
}

public class Animator : UIState
{
    // Visibility
    private static bool _visible = false;
    public static bool Visible
    { 
        get => _visible;
        set
        {
            // On open trigger an update
            if (value)
            {
                // Just in case, always try to have a filter active
                if ((SelectedFilter == null || SelectedFilter == "") && _keyFrames.Count > 0)
                {
                    SelectedFilter = _keyFrames.First().Key;   
                }

                UpdateControlGrid();
            }

            // set new visibility
            _visible = value;
        }
    }

    // Direction of the values
    private static bool _readMode = true;

    // Player
    private static int _minAnimTime = 0;
    private static int _maxAnimTime = 600;
    private static bool _playForward = false;
    private static bool _playBackward = false;
    private static bool _loop = false;
    private static bool Playing => _playForward || _playBackward;

    private static int _playerTime = 0;
    private static int PlayerTime
    {
        get => _playerTime;
        set
        {
            bool changed = _playerTime != value;

            _playerTime = value;

            if (changed)
            {
                UpdateAnimation();
            }
        }
    }

    // KeyFrame Dictionary. This saves all the information related to animatons and keyframes.
    // 1st Key => the name of the element (for example: 'Asphodene', 'Eridani' etc)
    // 2nd Key => name of the control field.
    // These settings need to be registered before use.
    private static Dictionary<string, Dictionary<string, AnimationSettings>> _keyFrames;

    private static string _selectedFilter;
    public static string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            bool changed = _selectedFilter != value;

            _selectedFilter = value;

            if (changed)
            {
                UpdateControlGrid();
            }
        }
    }

    // Objects
    private static DragableUIPanel Panel;
    private static UIText PanelName;
    private static UIGrid Grid;
    private static UIScrollbar Scrollbar;

    public static bool RegisterElement(string elementName)
    {
        // Initialize the dictionary.
        _keyFrames ??= [];

        // Check the given elementName
        if (elementName == null || elementName == "")
        {
            return false;
        }

        // Set the selected element if it's not set yet.
        _selectedFilter ??= elementName;

        // Register the Element inside the _keyFrames dictionary
        if (!_keyFrames.ContainsKey(elementName))
        {
            _keyFrames.Add(elementName, []);
        }

        return true;
    }

    public static bool RegisterControl(
        string elementName,
        string controlName,
        float value = 0f,
        float min = 0f,
        float max = 1f,
        float step = 0.1f,
        string format = "n0",
        Conversion convert = Conversion.None)
    { 
        // First initialize the Element; just in case.
        if (!RegisterElement(elementName))
        {
            return false;
        }

        // Grab the underlying dictionary object
        if (!_keyFrames.TryGetValue(elementName, out Dictionary<string, AnimationSettings> settings))
        {
            return false;
        }

        // Check if the control already exists. if not create it.
        if (!settings.TryGetValue(controlName, out AnimationSettings setting))
        {
            setting = new AnimationSettings();
            settings.Add(controlName, setting);
        }

        // Assign the values.
        setting.MinValue = min;
        setting.MaxValue = max;
        setting.Value = Math.Clamp(value, min, max);
        setting.Step = step;
        setting.Convert = convert;
        setting.Format = format;

        return true;
    }

    public override void OnInitialize()
    {
        // Initialize panel
        Panel = new DragableUIPanel();
        Panel.SetPadding(10);
        Panel.Left.Set(0f, 0.2f);
        Panel.Top.Set(0f, 0.4f);
        Panel.Width.Set(600, 0f);
        Panel.Height.Set(500, 0f);
        Panel.BackgroundColor = new Color(73, 94, 171);
        Panel.PreventDragging = true;

        // Add the panel name, this also functions as drag space
        PanelName = new UIText("Animation Studio", 1.5f);
        PanelName.Left.Set(-PanelName.MinWidth.Pixels / 2, 0.5f);
        PanelName.Top.Set(-4f, 0f);
        PanelName.OnMouseOver += (_, _) =>
        {
            Panel.PreventDragging = false;
        };
        PanelName.OnMouseOut += (_, _) =>
        {
            Panel.PreventDragging = true;
        };
        Panel.Append(PanelName);

        // Set the GRID that will hold the Field controls
        Grid = new UIGrid(2);
        Grid.Top.Set(32f, 0f);
        Grid.Left.Set(0f, 0f);
        Grid.Width.Set(560f, 0f);
        Grid.Height.Set(356f, 0f);
        Grid.ListPadding = 4f;
        Panel.Append(Grid);

        Scrollbar = new UIScrollbar();
        Scrollbar.SetView(100f, 1000f);
        Scrollbar.Top.Set(38f, 0f);
        Scrollbar.Left.Set(4f, 0f);
        Scrollbar.Height.Set(344f, 0f);
        Scrollbar.HAlign = 1f;  
        Panel.Append(Scrollbar);
        Grid.SetScrollbar(Scrollbar);

        // Fill the grid
        UpdateControlGrid();

        // Add seeker slider
        UISlider player = new(_minAnimTime, _maxAnimTime);
        player.Top.Set(Grid.Top.Pixels + Grid.Height.Pixels + 30, 0f);
        player.Left.Set(-player.Width.Pixels / 2, 0.5f);
        player.OnUpdate += (_) =>
        {
            // Always read the current value to the slider
            player.MinValue = _minAnimTime;
            player.MaxValue = _maxAnimTime;
            player.Value = PlayerTime;
        };
        player.OnLeftMouseDown += (_, _) =>
        {
            player.Control = true;
        };
        player.OnLeftMouseUp += (_, _) =>
        {
            player.Control = false;
        };
        player.OnValueChanged += (_, _) =>
        {
            // Check if we're allowed to edit time
            if (_readMode)
            {
                return;
            }

            // Write the updated value of the slider to the settings + limit
            int newTime = (int)Math.Round(player.Value);
            PlayerTime = Math.Clamp(newTime, 0, 600);
        };
        Panel.Append(player);

        UIButton addTime = new(true, "ButtonPlus");
        addTime.Top.Set(player.Top.Pixels, 0f);
        addTime.Left.Set(player.Width.Pixels / 2 + 4, player.Left.Percent);
        addTime.OnLeftMouseDown += (_, _) =>
        {
            addTime.Value = true;
        };
        addTime.OnLeftMouseUp += (_, _) =>
        {
            addTime.Value = false;
        };
        addTime.OnRightMouseDown += (_, _) =>
        {
            addTime.Value = true;
            addTime.Right = true;
        };
        addTime.OnRightMouseUp += (_, _) =>
        {
            addTime.Value = false;
            addTime.Right = false;
        };
        addTime.OnButtonTrigger += (_, _) =>
        {
            if (_readMode)
            {
                return;
            }

            int newTime = PlayerTime + (addTime.Right ? 10 : 1);
            PlayerTime = Math.Min(newTime, _maxAnimTime);
        };
        Panel.Append(addTime);

        UIButton removeTime = new(true, "ButtonMinus");
        removeTime.Top.Set(player.Top.Pixels, 0f);
        removeTime.Left.Set(-player.Width.Pixels / 2 - removeTime.Width.Pixels - 4, player.Left.Percent);
        removeTime.OnLeftMouseDown += (_, _) =>
        {
            removeTime.Value = true;
        };
        removeTime.OnLeftMouseUp += (_, _) =>
        {
            removeTime.Value = false;
        };
        removeTime.OnRightMouseDown += (_, _) =>
        {
            removeTime.Value = true;
            removeTime.Right = true;
        };
        removeTime.OnRightMouseUp += (_, _) =>
        {
            removeTime.Value = false;
            removeTime.Right = false;
        };
        removeTime.OnButtonTrigger += (_, _) =>
        {
            if (_readMode)
            {
                return;
            }

            int newTime = PlayerTime - (removeTime.Right ? 10 : 1);
            PlayerTime = Math.Max(newTime, _minAnimTime);
        };
        Panel.Append(removeTime);

        // Add current player value text
        UIText currentPlayer = new(PlayerTime.ToString());
        currentPlayer.Top.Set(player.Top.Pixels - 20, 0f);
        currentPlayer.Left.Set(-currentPlayer.MinWidth.Pixels / 2, player.Left.Precent);
        currentPlayer.OnUpdate += (_) =>
        {
            currentPlayer.SetText(PlayerTime.ToString());
            currentPlayer.Left.Set(-currentPlayer.MinWidth.Pixels / 2, player.Left.Precent);
        };
        Panel.Append(currentPlayer);

        // Add min and max player text
        UIText minPlayer = new(_minAnimTime.ToString());
        minPlayer.Top.Set(currentPlayer.Top.Pixels, 0f);
        minPlayer.Left.Set(-player.Width.Pixels / 2 - minPlayer.MinWidth.Pixels / 2, player.Left.Precent);
        minPlayer.OnUpdate += (_) =>
        {
            minPlayer.SetText(_minAnimTime.ToString());
            minPlayer.Left.Set(-player.Width.Pixels / 2 - minPlayer.MinWidth.Pixels / 2, player.Left.Precent);

        };
        minPlayer.OnLeftMouseUp += (_, _) =>
        {
            if (_minAnimTime + 1 >= _maxAnimTime)
            {
                return;
            }

            _minAnimTime++;

            if (PlayerTime < _minAnimTime)
            {
                PlayerTime = _minAnimTime;
            }

            if (_keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings))
            {
                foreach (string control in settings.Keys)
                {
                    if (settings.TryGetValue(control, out AnimationSettings setting))
                    {
                        setting.GenerateTexture = true;
                    }
                }
            }
        };
        minPlayer.OnRightMouseUp += (_, _) =>
        {
            if (_minAnimTime - 1 < 0)
            {
                return;
            }

            _minAnimTime--;

            if (_keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings))
            {
                foreach (string control in settings.Keys)
                {
                    if (settings.TryGetValue(control, out AnimationSettings setting))
                    {
                        setting.GenerateTexture = true;
                    }
                }
            }
        };
        Panel.Append(minPlayer);

        UIText maxPlayer = new(_maxAnimTime.ToString());
        maxPlayer.Top.Set(currentPlayer.Top.Pixels, 0f);
        maxPlayer.Left.Set(player.Width.Pixels / 2 - maxPlayer.MinWidth.Pixels / 2, player.Left.Precent);
        maxPlayer.OnUpdate += (_) =>
        {
            maxPlayer.SetText(_maxAnimTime.ToString());
            maxPlayer.Left.Set(player.Width.Pixels / 2 - maxPlayer.MinWidth.Pixels / 2, player.Left.Precent);
        };
        maxPlayer.OnRightMouseUp += (_, _) =>
        {
            if (_maxAnimTime - 1 <= _minAnimTime)
            {
                return;
            }

            _maxAnimTime--;

            if (PlayerTime > _maxAnimTime)
            {
                PlayerTime = _maxAnimTime;
            }

            if (_keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings))
            {
                foreach (string control in settings.Keys)
                {
                    if (settings.TryGetValue(control, out AnimationSettings setting))
                    {
                        setting.GenerateTexture = true;
                    }
                }
            }
        };
        maxPlayer.OnLeftMouseUp += (_, _) =>
        {
            _maxAnimTime++;

            if (_keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings))
            {
                foreach (string control in settings.Keys)
                {
                    if (settings.TryGetValue(control, out AnimationSettings setting))
                    { 
                        setting.GenerateTexture = true;
                    }
                }
            }
        };
        Panel.Append(maxPlayer);

        // Add Player stop button
        UIButton stop = new(false, "ButtonStop");
        stop.Top.Set(player.Top.Pixels + player.Height.Pixels + 8, 0f);
        stop.Left.Set(-stop.Width.Pixels / 2, player.Left.Percent);
        stop.OnUpdate += (_) =>
        {
            stop.Value = !Playing;
        };
        stop.OnLeftMouseUp += (_, _) =>
        {
            if (_readMode)
            {
                return;
            }

            if (Playing)
            {
                _playBackward = false;
                _playForward = false;
                stop.Value = true;
            }
        };
        Panel.Append(stop);

        // Add play forward button
        UIButton playForward = new(true, "ButtonArrowRight");
        playForward.Top.Set(stop.Top.Pixels, 0f);
        playForward.Left.Set(stop.Width.Pixels / 2 + 4, player.Left.Percent);
        playForward.OnUpdate += (_) =>
        {
            playForward.Value = _playForward;
        };
        playForward.OnLeftMouseUp += (_, _) =>
        {
            if (_readMode)
            {
                return;
            }

            if (PlayerTime >= _maxAnimTime && !_loop)
            {
                return;
            }

            _playForward = true;
            _playBackward = false;
        };
        Panel.Append(playForward);

        // Add play forward button
        UIButton playBackward = new(true, "ButtonArrowLeft");
        playBackward.Top.Set(stop.Top.Pixels, 0f);
        playBackward.Left.Set(-stop.Width.Pixels / 2 - playBackward.Width.Pixels - 4, player.Left.Percent);
        playBackward.OnUpdate += (_) =>
        {
            playBackward.Value = _playBackward;
        };
        playBackward.OnLeftMouseUp += (_, _) =>
        {
            if (_readMode)
            {
                return;
            }

            if (PlayerTime <= _minAnimTime && !_loop)
            {
                return;
            }

            _playBackward = true;
            _playForward = false;
        };
        Panel.Append(playBackward);

        // Add loop button
        UIButton loop = new(false, "ButtonLoop");
        loop.Top.Set(stop.Top.Pixels, 0f);
        loop.Left.Set(playForward.Left.Pixels + playForward.Width.Pixels + 4, player.Left.Percent);
        loop.OnUpdate += (_) =>
        {
            loop.Value = _loop;
        };
        loop.OnButtonTrigger += (_, _) =>
        {
            _loop = !_loop;
            loop.Value = _loop;
        };
        Panel.Append(loop);

        // Add Export all button
        UIButton exportAll = new(false, "ButtonExport");
        exportAll.Top.Set(loop.Top.Pixels, 0f);
        exportAll.Left.Set(loop.Left.Pixels + loop.Width.Pixels + 4, player.Left.Percent);
        exportAll.OnLeftMouseDown += (_, _) =>
        {
            exportAll.Value = true;
        };
        exportAll.OnLeftMouseUp += (_, _) =>
        {
            exportAll.Value = false;
            ExportAll(false);
        };
        exportAll.OnRightMouseDown += (_, _) =>
        {
            exportAll.Value = true;
        };
        exportAll.OnRightMouseUp += (_, _) =>
        {
            exportAll.Value = false;
            ExportAll(true);
        };
        Panel.Append(exportAll);

        // Add READ button
        UIButtonStates readWrite = new(2, "ButtonReadWrite");
        readWrite.Top.Set(stop.Top.Pixels, 0f);
        readWrite.Left.Set(playBackward.Left.Pixels - readWrite.Width.Pixels - 4, player.Left.Percent);
        readWrite.OnUpdate += (_) =>
        {
            readWrite.State = _readMode ? 0 : 1;
        };
        readWrite.OnLeftMouseDown += (_, _) =>
        {
            readWrite.Value = true;
        };
        readWrite.OnLeftMouseUp += (_, _) =>
        {
            _readMode = !_readMode;
            readWrite.Value = false;

            if (readWrite.State <= 0)
            {
                readWrite.State = 1;
            }
            else if (readWrite.State >= 1)
            {
                readWrite.State = 0;
            }
        };
        Panel.Append(readWrite);

        // Add filter cycle button
        UIText filter = new("Filter");
        filter.Top.Set(-16, 1f);
        filter.Left.Set(-filter.MinWidth.Pixels / 2, 0.5f);
        filter.OnUpdate += (_) =>
        {
            if (SelectedFilter == null || SelectedFilter == "")
            {
                return;
            }
                
            filter.SetText(SelectedFilter);
            filter.Left.Set(-filter.MinWidth.Pixels / 2, 0.5f);
        };
        filter.OnMouseOver += (_, _) =>
        {
            filter.TextColor = Color.Yellow;
        };
        filter.OnMouseOut += (_, _) =>
        {
            filter.TextColor = Color.White;
        };
        filter.OnLeftMouseUp += (_, _) =>
        {
            if (SelectedFilter == null || SelectedFilter == "")
            {
                return;
            }

            // Get the list of names inside the keyframes dictionary keys
            List<string> names = [.. _keyFrames.Keys];

            // Get the current Index inside the list
            int index = names.IndexOf(SelectedFilter);

            // Cycle to the next entry
            index++;

            // Verify if its valid
            if (index >= names.Count)
            {
                index = 0;
            }

            // Set the new index' name
            SelectedFilter = names[index];
        };
        Panel.Append(filter);

        // Text to close the panel
        UIText close = new("Close");
        close.Top.Set(-close.MinHeight.Pixels, 1f);
        close.Left.Set(-close.MinWidth.Pixels, 1f);
        close.OnMouseOver += (_, _) =>
        {
            close.TextColor = Color.Yellow;
        };
        close.OnMouseOut += (_, _) =>
        {
            close.TextColor = Color.White;
        };
        close.OnLeftMouseUp += (_, _) =>
        {
            Visible = false;
        };
        Panel.Append(close);

        Append(Panel);
    }

    private static void UpdateControlGrid()
    {
        // Clear the grid with controls
        Grid.Clear();

        // Check if the selected filter is valid and present
        if (SelectedFilter != null && _keyFrames != null && _keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings) && settings.Count > 0)
        {
            // Add the Field setting panels
            foreach (string control in settings.Keys)
            {
                if (settings[control] == null)
                {
                    continue;
                }

                AnimationSettings setting = settings[control];

                // Create a new panel as background
                UIPanel fieldPanel = new();
                fieldPanel.Top.Set(0f, 0f);
                fieldPanel.Left.Set(0f, 0f);
                fieldPanel.Width.Set(-4, 0.5f);
                fieldPanel.Height.Set(176f, 0f);

                // Add the name of the field control
                UIText name = new(Regex.Replace(control.ToString(), "([a-z?])[_ ]?([A-Z])", "$1 $2").TrimStart(' '));
                name.Left.Set(-name.MinWidth.Pixels / 2, 0.5f);
                name.Top.Set(-4f, 0f);
                fieldPanel.Append(name);

                // Add disable button
                UIButton disable = new(false, "ButtonDisable");
                disable.Top.Set(-4f, 0f);
                disable.Left.Set(-disable.Width.Pixels + 4f, 1f);
                disable.OnUpdate += (_) =>
                {
                    disable.Value = setting.Disable;
                };
                disable.OnLeftMouseUp += (_, _) =>
                {
                    setting.Disable = !setting.Disable;
                    disable.Value = setting.Disable;
                    UpdateControlGrid();
                };
                fieldPanel.Append(disable);

                // Add the panel to the underlying grid
                Grid.Add(fieldPanel);

                // If disabled don't draw anything more
                if (setting.Disable)
                {
                    fieldPanel.Height.Set(40f, 0f);
                    continue;
                }

                // Add Slider
                UISlider slider = new(setting.MinValue, setting.MaxValue);
                slider.Top.Set(24f, 0f);
                slider.Left.Set(-slider.Width.Pixels / 2, 0.5f);
                slider.OnUpdate += (_) =>
                {
                    // Always read the current value to the slider
                    if (setting.KeyFrames.Count <= 1)
                    {
                        slider.Value = setting.Value;
                        return;
                    }

                    // There is a keyframe on this position, use the keyframe's value
                    if (setting.KeyFrames.TryGetValue(PlayerTime, out KeyFrame keyFrame))
                    { 
                        slider.Value = keyFrame.Value;
                        return;
                    }

                    // There is no keyFrame on the current time but there are 2
                    // at least 2 other keyframes present.
                    setting.UpdateAnimationValue(PlayerTime);
                    slider.Value = setting.Value;
                    
                };
                slider.OnLeftMouseDown += (_, _) =>
                {
                    slider.Control = true;
                };
                slider.OnLeftMouseUp += (_, _) =>
                {
                    slider.Control = false;
                };
                slider.OnValueChanged += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    setting.Value = Math.Clamp(slider.Value, setting.MinValue, setting.MaxValue);

                    // Check if there are keyframes set
                    if (setting.KeyFrames.Count > 0)
                    {
                        // Set a keyframe with the new value (or modify existing)
                        if (setting.AddKeyFrame(PlayerTime, setting.Value, setting.KeyMode))
                        {
                            setting.GenerateTexture = true;
                        }
                    }
                };
                fieldPanel.Append(slider);

                // Add current value text
                UIText currentValue = new(setting.ValueToText);
                currentValue.Top.Set(44f, 0f);
                currentValue.Left.Set(-currentValue.MinWidth.Pixels / 2, slider.Left.Precent);
                currentValue.OnUpdate += (_) =>
                {
                    // Get the default text
                    string text = "";

                    // If there are keyframes set and there is a keyframe
                    // on this position: use the keyframe value.
                    if (setting.KeyFrames.Count > 0 && setting.KeyFrames.TryGetValue(PlayerTime, out KeyFrame keyFrame))
                    {
                        text = setting.GetValueText(keyFrame.Value);
                    }
                    // Otherwise use the normal value.
                    else
                    {
                        text = setting.ValueToText;
                    }

                    // Set new text
                    currentValue.SetText(text);
                    currentValue.Left.Set(-currentValue.MinWidth.Pixels / 2, slider.Left.Precent);
                };
                fieldPanel.Append(currentValue);

                // Add min and max text
                UIText minValue = new(setting.MinToText);
                minValue.Top.Set(44f, 0f);
                minValue.Left.Set(slider.Left.Pixels - minValue.MinWidth.Pixels / 2, slider.Left.Precent);
                fieldPanel.Append(minValue);

                UIText maxValue = new(setting.MaxToText);
                maxValue.Top.Set(44f, 0f);
                maxValue.Left.Set(slider.Left.Pixels + slider.Width.Pixels - maxValue.MinWidth.Pixels / 2, slider.Left.Precent);
                fieldPanel.Append(maxValue);

                // Add minus button
                UIButton minus = new(true, "ButtonMinus");
                minus.Top.Set(slider.Top.Pixels, 0f);
                minus.Left.Set(slider.Left.Pixels - 20, slider.Left.Precent);
                minus.OnLeftMouseDown += (_, _) =>
                {
                    minus.Value = true;
                };
                minus.OnLeftMouseUp += (_, _) =>
                {
                    minus.Value = false;
                };
                minus.OnRightMouseDown += (_, _) =>
                {
                    minus.Value = true;
                    minus.Right = true;
                };
                minus.OnRightMouseUp += (_, _) =>
                {
                    minus.Value = false;
                    minus.Right = false;
                };
                minus.OnButtonTrigger += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (setting.KeyFrames.Count > 0)
                    {
                        setting.UpdateAnimationValue(PlayerTime);
                    }

                    setting.Value -= (minus.Right ? setting.Step * 10 : setting.Step);
                    setting.Value = Math.Max(setting.Value, setting.MinValue);

                    if (setting.KeyFrames.Count <= 0)
                    {
                        return;
                    }

                    // Set a keyframe with the new value (or modify existing)
                    if (setting.AddKeyFrame(PlayerTime, setting.Value, setting.KeyMode))
                    {
                        setting.GenerateTexture = true;
                    }
                };
                fieldPanel.Append(minus);

                // Add plus button
                UIButton plus = new(true, "ButtonPlus");
                plus.Top.Set(slider.Top.Pixels, 0f);
                plus.Left.Set(slider.Left.Pixels + slider.Width.Pixels + 4, slider.Left.Precent);
                plus.OnLeftMouseDown += (_, _) =>
                {
                    plus.Value = true;
                };
                plus.OnLeftMouseUp += (_, _) =>
                {
                    plus.Value = false;
                };
                plus.OnRightMouseDown += (_, _) =>
                {
                    plus.Value = true;
                    plus.Right = true;
                };
                plus.OnRightMouseUp += (_, _) =>
                {
                    plus.Value = false;
                    plus.Right = false;
                };
                plus.OnButtonTrigger += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (setting.KeyFrames.Count > 0)
                    {
                        setting.UpdateAnimationValue(PlayerTime);
                    }

                    setting.Value += (plus.Right ? setting.Step * 10 : setting.Step);
                    setting.Value = Math.Min(setting.Value, setting.MaxValue);

                    if (setting.KeyFrames.Count <= 0)
                    {
                        return;
                    }

                    // Set a keyframe with the new value (or modify existing)
                    if (setting.AddKeyFrame(PlayerTime, setting.Value, setting.KeyMode))
                    {
                        setting.GenerateTexture = true;
                    }
                };
                fieldPanel.Append(plus);

                // Add zero button
                UIButton zero = new(false, "ButtonZero");
                zero.Top.Set(slider.Top.Pixels, 0f);
                zero.Left.Set(slider.Left.Pixels - 40, slider.Left.Percent);
                zero.OnLeftMouseDown += (_, _) =>
                {
                    zero.Value = true;
                };
                zero.OnLeftMouseUp += (_, _) =>
                {
                    zero.Value = false;
                };
                zero.OnButtonTrigger += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    // Set to Zero, then limit just in case
                    setting.Value = Math.Clamp(0f, setting.MinValue, setting.MaxValue);

                    // Check if there are keyframes set
                    if (setting.KeyFrames.Count > 0)
                    {
                        // Set a keyframe with the new value (or modify existing)
                        if (setting.AddKeyFrame(PlayerTime, setting.Value, setting.KeyMode))
                        {
                            setting.GenerateTexture = true;
                        }
                    }
                };
                fieldPanel.Append(zero);

                // Add lock button
                UIButton locker = new(false, "ButtonLock");
                locker.Top.Set(slider.Top.Pixels, 0f);
                locker.Left.Set(slider.Left.Pixels + slider.Width.Pixels + 24, slider.Left.Percent);
                locker.OnUpdate += (_) =>
                {
                    locker.Value = setting.Lock;
                };
                locker.OnButtonTrigger += (_, _) =>
                {
                    setting.Lock = !setting.Lock;
                    locker.Value = setting.Lock;
                };
                fieldPanel.Append(locker);

                // Add KeyFrame button
                UIButton keyframe = new(false, "ButtonKey");
                keyframe.Top.Set(68f, 0f);
                keyframe.Left.Set(-keyframe.Width.Pixels / 2, slider.Left.Precent);
                keyframe.OnUpdate += (_) =>
                {
                    keyframe.Value = setting.KeyFrames.ContainsKeyFrame(PlayerTime);
                };
                keyframe.OnLeftMouseUp += (_, _) =>
                {
                    // Check if we're allowed to edit keyframes
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (setting.KeyFrames == null)
                    {
                        return;
                    }

                    // If there is a keyframe on this position, remove it
                    if (setting.KeyFrames.ContainsKey(PlayerTime))
                    {
                        setting.RemoveKeyFrame(PlayerTime);
                    }
                    // If there is NO keyframe on this position, create it
                    else
                    {
                        setting.AddKeyFrame(PlayerTime, setting.Value, setting.KeyMode);
                    }

                    setting.GenerateTexture = true;
                };
                fieldPanel.Append(keyframe);

                // Add Keyframe NEXT button
                UIButton next = new(false, "ButtonArrowRight");
                next.Top.Set(keyframe.Top.Pixels, 0f);
                next.Left.Set(keyframe.Width.Pixels / 2 + 4, slider.Left.Percent);
                next.OnLeftMouseDown += (_, _) =>
                {
                    next.Value = true;
                };
                next.OnLeftMouseUp += (_, _) =>
                {
                    next.Value = false;
                };
                next.OnRightMouseDown += (_, _) =>
                {
                    next.Value = true;
                    next.Right = true;
                };
                next.OnRightMouseUp += (_, _) =>
                {
                    next.Value = false;
                    next.Right = false;
                };
                next.OnButtonTrigger += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (!next.Right)
                    {
                        int key = setting.KeyFrames.SeekKeyAfter(PlayerTime);

                        if (key != InvalidKey && key <= _maxAnimTime)
                        {
                            PlayerTime = key;
                        }
                    }
                    else
                    {
                        if (PlayerTime + 1 <= _maxAnimTime
                            && setting.KeyFrames.TryGetValue(PlayerTime, out KeyFrame key)
                            && !setting.KeyFrames.ContainsKey(PlayerTime + 1))
                        {
                            setting.KeyFrames.Remove(PlayerTime);
                            setting.KeyFrames[PlayerTime + 1] = key;
                            PlayerTime++;
                            setting.GenerateTexture = true;
                        }
                    }
                };
                fieldPanel.Append(next);

                // Add Keyframe PREVIOUS button
                UIButton previous = new(false, "ButtonArrowLeft");
                previous.Top.Set(keyframe.Top.Pixels, 0f);
                previous.Left.Set(-keyframe.Width.Pixels / 2 - previous.Width.Pixels - 4, slider.Left.Percent);
                previous.OnLeftMouseDown += (_, _) =>
                {
                    previous.Value = true;
                };
                previous.OnLeftMouseUp += (_, _) =>
                {
                    previous.Value = false;
                };
                previous.OnRightMouseDown += (_, _) =>
                {
                    previous.Value = true;
                    previous.Right = true;
                };
                previous.OnRightMouseUp += (_, _) =>
                {
                    previous.Value = false;
                    previous.Right = false;
                };
                previous.OnButtonTrigger += (_, _) =>
                {
                    // Check if we're allowed to edit values
                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (!previous.Right)
                    {
                        int key = setting.KeyFrames.SeekKeyBefore(PlayerTime);

                        if (key != InvalidKey && key >= _minAnimTime)
                        {
                            PlayerTime = key;
                        }
                    }
                    else
                    {
                        if (PlayerTime - 1 >= _minAnimTime
                            && setting.KeyFrames.TryGetValue(PlayerTime, out KeyFrame key)
                            && !setting.KeyFrames.ContainsKey(PlayerTime - 1))
                        {
                            setting.KeyFrames.Remove(PlayerTime);
                            setting.KeyFrames[PlayerTime - 1] = key;
                            PlayerTime--;
                            setting.GenerateTexture = true;
                        }
                    }
                };
                fieldPanel.Append(previous);

                // Add KeyMode button
                UIButtonStates mode = new(Enum.GetValues(typeof(KeyMode)).Length, "ButtonKeyMode");
                mode.Top.Set(keyframe.Top.Pixels, 0f);
                mode.Left.Set(next.Left.Pixels + next.Width.Pixels + 4, slider.Left.Precent);
                mode.OnUpdate += (_) =>
                {
                    if (setting.KeyFrames.Count <= 0)
                    {
                        mode.State = (int)setting.KeyMode;
                        return;
                    }

                    if (setting.KeyFrames.TryGetValue(PlayerTime, out KeyFrame keyFrame))
                    {
                        mode.State = (int)keyFrame.Mode;
                        setting.KeyMode = keyFrame.Mode;
                        return;
                    }

                    if (setting.KeyFrames.TryGetValue(setting.Begin, out KeyFrame keyFrame2))
                    {
                        mode.State = (int)keyFrame2.Mode;
                        setting.KeyMode = keyFrame2.Mode;
                    }
                };
                mode.OnLeftMouseDown += (_, _) =>
                {
                    mode.Value = true;
                };
                mode.OnLeftMouseUp += (_, _) =>
                {
                    mode.Value = false;

                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    // Go to the next state
                    mode.State++;
                    if (mode.State >= mode.States)
                    {
                        mode.State = 0;
                    }

                    // Set the state
                    setting.KeyMode = (KeyMode)mode.State;

                    // No keyframes present
                    if (setting.KeyFrames.Count <= 0)
                    {
                        return;
                    }

                    // Update the value on the current time
                    setting.UpdateAnimationValue(PlayerTime);
                    setting.AddKeyFrame(PlayerTime, setting.Value, (KeyMode)mode.State);
                    setting.GenerateTexture = true;
                };
                fieldPanel.Append(mode);

                // Add Export Single button
                UIButton exportSingle = new(false, "ButtonExport");
                exportSingle.Top.Set(keyframe.Top.Pixels, 0f);
                exportSingle.Left.Set(mode.Left.Pixels + mode.Width.Pixels + 4, slider.Left.Percent);
                exportSingle.OnLeftMouseDown += (_, _) =>
                {
                    exportSingle.Value = true;
                };
                exportSingle.OnLeftMouseUp += (_, _) =>
                {
                    exportSingle.Value = false;
                    ExportSingle(control, false);
                };
                exportSingle.OnRightMouseDown += (_, _) =>
                {
                    exportSingle.Value = true;
                };
                exportSingle.OnRightMouseUp += (_, _) =>
                {
                    exportSingle.Value = false;
                    ExportSingle(control, true);
                };
                fieldPanel.Append(exportSingle);

                // Add Clear button
                UIButton delete = new(false, "ButtonDelete");
                delete.Top.Set(keyframe.Top.Pixels, 0f);
                delete.Left.Set(previous.Left.Pixels - delete.Width.Pixels - 4, slider.Left.Precent);
                delete.OnLeftMouseDown += (_, _) =>
                {
                    delete.Value = true;
                };
                delete.OnLeftMouseUp += (_, _) =>
                {
                    delete.Value = false;

                    if (_readMode || Playing || setting.Lock)
                    {
                        return;
                    }

                    if (setting.KeyFrames.Count > 0)
                    {
                        setting.KeyFrames.Clear();
                        setting.GenerateTexture = true;
                    }
                };
                fieldPanel.Append(delete);

                UIKeyFrameView view = new();
                view.Height.Set(60f, 0f);
                view.Width.Set(slider.Width.Pixels + 64, 0f);
                view.Top.Set(keyframe.Top.Pixels + keyframe.Height.Pixels + 8, 0f);
                view.Left.Set(-view.Width.Pixels / 2, 0.5f);
                view.GenerateTexture(setting, _minAnimTime, _maxAnimTime);
                view.OnUpdate += (_) =>
                {
                    view.PlayerTime = PlayerTime;
                    view.Max = _maxAnimTime;
                    view.Min = _minAnimTime;
                    view.OnKeyFrame = keyframe.Value;

                    if (setting.GenerateTexture)
                    {
                        view.GenerateTexture(setting, _minAnimTime, _maxAnimTime);
                        setting.GenerateTexture = false;
                    }
                };

                fieldPanel.Append(view);
            }
        }

        // Recalculate the grid
        Grid.UpdateOrder();
        Grid._innerList.Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_readMode)
        {
            return;
        }

        if (_playForward)
        {
            int newTime = PlayerTime + 1;
            if (newTime > _maxAnimTime)
            {
                if (_loop)
                {
                    PlayerTime = _minAnimTime;
                }
                else
                {
                    _playForward = false;
                }
            }
            else
            {
                PlayerTime = newTime;
            }
        }

        if (_playBackward)
        { 
            int newTime = PlayerTime - 1;
            if (newTime < _minAnimTime)
            {
                if (_loop)
                {
                    PlayerTime = _maxAnimTime;
                }
                else
                {
                    _playBackward = false;
                }
            }
            else
            {
                PlayerTime = newTime;
            }
        }
    }

    private static void UpdateAnimation()
    {
        if (SelectedFilter == null || SelectedFilter == "" || _keyFrames == null)
        {
            return;
        }

        if (!_keyFrames.TryGetValue(SelectedFilter, out Dictionary<string, AnimationSettings> settings))
        {
            return;
        }

        foreach (string control in settings.Keys)
        {
            if (settings[control] == null)
            {
                continue;
            }

            settings[control].UpdateAnimationValue(PlayerTime);
        }
    }

    public static float AnimationValue(string elementName, string controlName, float value)
    {
        if (!Visible 
            || SelectedFilter != elementName
            || _keyFrames == null 
            || !_keyFrames.TryGetValue(elementName, out Dictionary<string, AnimationSettings> settings)
            || !settings.TryGetValue(controlName, out AnimationSettings setting)
            || setting.Disable)
        {
            return value;
        }

        if (_readMode)
        { 
            setting.Value = value;
        }
        else
        {
            setting.UpdateAnimationValue(PlayerTime);
            value = setting.Value;
        }

        return value;
    }

    public static void SetPlayerTime(string elementName, int time)
    {
        if (SelectedFilter != elementName)
        {
            return;
        }

        PlayerTime = Math.Clamp(time, _minAnimTime, _maxAnimTime);
    }

    public static bool GetReadMode(string elementName)
    { 
        return SelectedFilter == elementName && _readMode;
    }

    private static void ExportAll(bool fill = false)
    {
        if (!_keyFrames.TryGetValue(_selectedFilter, out Dictionary<string, AnimationSettings> settings))
        {
            Main.NewText("Export: failed!");
            return;
        }

        // The string that will contain all the information
        string export = "";

        if (fill)
        {
            export += "\r\n\r\nnew Dictionary<string, List<float>>()\r\n{\r\n";

            foreach (string control in settings.Keys)
            {
                if (settings[control].Disable || settings[control].KeyFrames == null || settings[control].KeyFrames.Count <= 0)
                {
                    continue;
                }

                export += "    { \"" + control + "\", new List<float>() {";

                for (int time  = _minAnimTime; time <= _maxAnimTime; time++)
                {
                    settings[control].UpdateAnimationValue(time);
                    export += " " + settings[control].Value.ToString().Replace(",", ".") + "f,";
                }

                export += " } },\r\n";
            }

            export += "};\r\n";
        }
        else
        {
            export += "\r\n\r\nnew Dictionary<string, SortedDictionary<int, KeyFrame>>()\r\n{\r\n";

            foreach (string control in settings.Keys)
            {
                if (settings[control].Disable || settings[control].KeyFrames == null || settings[control].KeyFrames.Count <= 0)
                {
                    continue;
                }

                export += "    { \"" + control + "\", new SortedDictionary<int, KeyFrame>() {";

                foreach (int key in settings[control].KeyFrames.Keys)
                {
                    export += " { " + key + ", new KeyFrame(" + settings[control].KeyFrames[key].Value.ToString().Replace(",", ".") + "f, KeyMode." + settings[control].KeyFrames[key].Mode.ToString() + ") },";
                }

                export += " } },\r\n";
            }

            export += "};\r\n";
        }

        ModContent.GetInstance<AnimationStudio>().Logger.Info(export);
        Main.NewText("Export: success! please check client.log");
    }

    private static void ExportSingle(string control, bool fill = false)
    {
        if (!_keyFrames.TryGetValue(_selectedFilter, out Dictionary<string, AnimationSettings> settings))
        {
            Main.NewText("Export: failed!");
            return;
        }

        if (!settings.TryGetValue(control, out AnimationSettings setting))
        {
            Main.NewText("Export: failed!");
            return;
        }

        if (setting.KeyFrames == null || setting.KeyFrames.Count <= 0)
        {
            Main.NewText("Export: maybe add some KeyFrames first?");
            return;
        }

        // The string that will contain all the information
        string export = "";

        if (fill)
        {
            export += "\r\n\r\nnew List<float>() {";

            for (int time = _minAnimTime; time <= _maxAnimTime; time++)
            {
                settings[control].UpdateAnimationValue(time);
                export += " " + settings[control].Value.ToString().Replace(",", ".") + "f,";
            }

            export += " };";
        }
        else
        {
            export += "\r\n\r\nnew SortedDictionary<int, KeyFrame>() {";

            foreach (int key in settings[control].KeyFrames.Keys)
            {
                export += " { " + key + ", new KeyFrame(" + settings[control].KeyFrames[key].Value.ToString().Replace(",", ".") + "f, KeyMode." + settings[control].KeyFrames[key].Mode.ToString() + ") },";
            }

            export += " };";
        }

        ModContent.GetInstance<AnimationStudio>().Logger.Info(export);
        Main.NewText("Export: success! Please check client.log");
    }
}
