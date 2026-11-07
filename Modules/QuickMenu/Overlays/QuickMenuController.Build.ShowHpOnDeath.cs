using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildShowHpOnDeathOverlayUi()
        {
            showHpOnDeathRoot = CreateOverlayFrame("ShowHPOnDeathOverlayCanvas", ShowHpOnDeathCanvasSortOrder, "ShowHPOnDeathPanel", PanelHeight, "ShowHPOnDeath".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            RectTransform content = CreateOverlayContent(panel, PanelHeight, titleRect, out float resetY, out float backY, out float topOffset, out float viewHeight);
            showHpOnDeathContent = content;

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;
            CreateToggleRowWithIcon(
                content,
                "ShowHPOnDeathGlobalRow",
                "ShowHPOnDeath/Enable".Localize(),
                rowY,
                GetShowHpOnDeathEnabled,
                SetShowHpOnDeathEnabled,
                out showHpGlobalValue,
                out showHpGlobalIcon
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateToggleRow(
                content,
                "ShowHPOnDeathShowPbRow",
                "ShowHPOnDeath/ShowPB".Localize(),
                rowY,
                () => ShowHpSettings.ShowPB,
                value => ShowHpSettings.ShowPB = value,
                out showHpShowPbValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateToggleRow(
                content,
                "ShowHPOnDeathAutoHideRow",
                "ShowHPOnDeath/AutoHide".Localize(),
                rowY,
                () => ShowHpSettings.HideAfter10Sec,
                value => ShowHpSettings.HideAfter10Sec = value,
                out showHpAutoHideValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "ShowHPOnDeathKeyRow",
                "ShowHPOnDeath/HudToggleKey".Localize(),
                rowY,
                GetShowHpBindingLabel,
                StartShowHpOnDeathRebind,
                out showHpHudToggleKeyValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateKeybindRow(
                content,
                "ShowHPOnDeathHistoryKeyRow",
                "ShowHPOnDeath/HistoryKey".Localize(),
                rowY,
                () => GetShowHpBindingLabel(ShowHpSettings.Keybinds.History),
                StartShowHpHistoryRebind,
                out showHpHistoryKeyValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateFloatSliderRow(
                content,
                "ShowHPOnDeathFadeRow",
                "ShowHPOnDeath/HudFadeTime".Localize(),
                rowY,
                rowY + 40f,
                1f,
                10f,
                ShowHpSettings.HudFadeSeconds,
                1,
                out showHpFadeValue,
                out showHpFadeSlider
            );
            if (showHpFadeSlider != null)
            {
                showHpFadeSlider.onValueChanged.AddListener(OnShowHpFadeChanged);
            }

            lastY = rowY + 40f;
            rowY += RowSpacing + 40f;

            CreateButtonRow(content, "ShowHPOnDeathHistoryRow", "ShowHPOnDeath/History".Localize(), rowY, OnShowHpOnDeathHistoryClicked);
            lastY = rowY;
            rowY += RowSpacing;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "ShowHPOnDeathResetRow", "ShowHPOnDeath/Reset".Localize(), resetY, OnShowHpOnDeathResetDefaultsClicked);
            CreateButtonRow(panel.transform, "ShowHPOnDeathBackRow", "Back", backY, OnShowHpOnDeathBackClicked);
        }
    }
}
