using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AnimationStudio;

public class AnimationStudioClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public ExportFormat Format = ExportFormat.Default;

    public bool MapFromZeroToOne = false;

    [DefaultValue("n2")]
    public string FloatToStringNumberFormat;

    public override void OnLoaded()
    {
        AnimationStudio.AnimationStudioClientConfig = this;
    }
}

public enum ExportFormat
{
    Default,
    KivotosMod
}