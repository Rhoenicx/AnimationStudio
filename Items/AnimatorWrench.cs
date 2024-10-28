using AnimationStudio.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AnimationStudio.Items;

public class AnimatorWrench : ModItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.Wrench}";

    public override void SetDefaults()
    {
        Item.height = 20; 
        Item.width = 20;

        Item.useStyle = ItemUseStyleID.Swing;

        Item.useTime = 20;
        Item.useAnimation = 20;
    }

    public override bool CanUseItem(Player player)
    {
        Animator.Visible = !Animator.Visible;

        return true;
    }
}
