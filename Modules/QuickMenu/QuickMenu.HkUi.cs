using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    internal static class HkUi
    {
        internal static Font TitleFont => QuickMenuController.GetHkTitleFont();

        internal static Font BodyFont => QuickMenuController.GetMenuFont();

        internal static Sprite SoftPlate => QuickMenuController.GetHkSoftSprite();

        internal static Sprite ChamferFill => QuickMenuController.GetHkChamferSprite(false);

        internal static Sprite ChamferLine => QuickMenuController.GetHkChamferSprite(true);

        internal static void StyleTitle(Text title) => QuickMenuController.StyleHkTitle(title);
    }
}
