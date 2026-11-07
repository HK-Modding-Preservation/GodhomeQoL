using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildTeleportKitOverlayUi()
        {
            teleportKitRoot = CreateOverlayFrame("TeleportKitOverlayCanvas", TeleportKitCanvasSortOrder, "TeleportKitPanel", PanelHeight, "TeleportKit".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            RectTransform content = CreateOverlayContent(panel, PanelHeight, titleRect, out float resetY, out float backY, out float topOffset, out float viewHeight);
            teleportKitContent = content;

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;
            CreateToggleRowWithIcon(
                content,
                "TeleportKitToggleRow",
                "Settings/TeleportKit/Enable".Localize(),
                rowY,
                GetTeleportKitEnabled,
                SetTeleportKitEnabled,
                out teleportKitToggleValue,
                out teleportKitToggleIcon
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "TeleportKitMenuKeyRow",
                "TeleportKit/MenuHotkey".Localize(),
                rowY,
                GetTeleportKitMenuKeyLabel,
                StartTeleportKitMenuRebind,
                out teleportKitMenuKeyValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "TeleportKitSaveKeyRow",
                "TeleportKit/SaveHotkey".Localize(),
                rowY,
                GetTeleportKitSaveKeyLabel,
                StartTeleportKitSaveRebind,
                out teleportKitSaveKeyValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "TeleportKitTeleportKeyRow",
                "TeleportKit/TeleportHotkey".Localize(),
                rowY,
                GetTeleportKitTeleportKeyLabel,
                StartTeleportKitTeleportRebind,
                out teleportKitTeleportKeyValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "TeleportKitGamepadButtonRow",
                "TeleportKit/GamepadButton".Localize(),
                rowY,
                GetTeleportKitControllerButtonLabel,
                StartTeleportKitControllerRebind,
                out Text teleportGamepadValue
            );
            teleportKitControllerButtonValue = teleportGamepadValue;

            lastY = rowY;
            rowY += RowSpacing;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "TeleportKitResetRow", "TeleportKit/Reset".Localize(), resetY, OnTeleportKitResetDefaultsClicked);
            CreateButtonRow(panel.transform, "TeleportKitBackRow", "Back", backY, OnTeleportKitBackClicked);
        }
    }
}
