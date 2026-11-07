using System;
using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private static void ResetAdditionalGhostHelperDefaultsGlobal()
        {
            ResetMarmuHelperDefaults();
            ResetXeroHelperDefaults();
            ResetMarkothHelperDefaults();
            ResetGalienHelperDefaults();
            ResetGorbHelperDefaults();
            ResetElderHuHelperDefaults();
            ResetNoEyesHelperDefaults();
            ResetDungDefenderHelperDefaults();
            ResetWhiteDefenderHelperDefaults();
            ResetHiveKnightHelperDefaults();
            ResetBrokenVesselHelperDefaults();
            ResetLostKinHelperDefaults();
            ResetNoskHelperDefaults();
            ResetWingedNoskHelperDefaults();
            ResetUumuuHelperDefaults();
            ResetTraitorLordHelperDefaults();
            ResetTroupeMasterGrimmHelperDefaults();
            ResetNightmareKingGrimmHelperDefaults();
            ResetPureVesselHelperDefaults();
            ResetAbsoluteRadianceHelperDefaults();
            ResetPaintmasterSheoHelperDefaults();
            ResetSoulWarriorHelperDefaults();
            ResetNailsageSlyHelperDefaults();
            ResetSoulMasterHelperDefaults();
            ResetSoulTyrantHelperDefaults();
            ResetWatcherKnightHelperDefaults();
            ResetOroMatoHelperDefaults();
            ResetGodTamerHelperDefaults();
            ResetOblobblesHelperDefaults();
            ResetFalseKnightHelperDefaults();
            ResetFailedChampionHelperDefaults();
            ResetSisterOfBattleHelperDefaults();
            ResetMantisLordHelperDefaults();
            ResetFlukemarmHelperDefaults();
            ResetVengeflyKingDefaults();
        }

        private static void SetAdditionalGhostHelpersModulesEnabled(bool value)
        {
            SetModuleEnabled<Modules.BossChallenge.MarmuHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.XeroHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.MarkothHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.GalienHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.GorbHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.ElderHuHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.NoEyesHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.DungDefenderHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.WhiteDefenderHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.HiveKnightHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.BrokenVesselHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.LostKinHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.NoskHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.WingedNoskHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.UumuuHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.TraitorLordHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.TroupeMasterGrimmHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.NightmareKingGrimmHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.PureVesselHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.AbsoluteRadianceHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.PaintmasterSheoHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.SoulWarriorHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.NailsageSlyHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.SoulMasterHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.SoulTyrantHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.WatcherKnightHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.OroMatoHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.GodTamerHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.OblobblesHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.FalseKnightHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.FailedChampionHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.SisterOfBattleHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.MantisLordHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.FlukemarmHelper>(value);
            SetModuleEnabled<Modules.BossChallenge.VengeflyKing>(value);
        }

        private bool GetAdditionalGhostHelpersEnabled()
        {
            return GetMarmuHelperEnabled()
                || GetXeroHelperEnabled()
                || GetMarkothHelperEnabled()
                || GetGalienHelperEnabled()
                || GetGorbHelperEnabled()
                || GetElderHuHelperEnabled()
                || GetNoEyesHelperEnabled()
                || GetDungDefenderHelperEnabled()
                || GetWhiteDefenderHelperEnabled()
                || GetHiveKnightHelperEnabled()
                || GetBrokenVesselHelperEnabled()
                || GetLostKinHelperEnabled()
                || GetNoskHelperEnabled()
                || GetWingedNoskHelperEnabled()
                || GetUumuuHelperEnabled()
                || GetTraitorLordHelperEnabled()
                || GetTroupeMasterGrimmHelperEnabled()
                || GetNightmareKingGrimmHelperEnabled()
                || GetPureVesselHelperEnabled()
                || GetAbsoluteRadianceHelperEnabled()
                || GetPaintmasterSheoHelperEnabled()
                || GetSoulWarriorHelperEnabled()
                || GetNailsageSlyHelperEnabled()
                || GetSoulMasterHelperEnabled()
                || GetSoulTyrantHelperEnabled()
                || GetWatcherKnightHelperEnabled()
                || GetOroMatoHelperEnabled()
                || GetGodTamerHelperEnabled()
                || GetOblobblesHelperEnabled()
                || GetFalseKnightHelperEnabled()
                || GetFailedChampionHelperEnabled()
                || GetSisterOfBattleHelperEnabled()
                || GetMantisLordHelperEnabled()
                || GetFlukemarmHelperEnabled()
                || GetVengeflyKingEnabled()
                ;
        }

        private bool IsAnyAdditionalGhostHelperVisible()
        {
            return marmuHelperVisible
                || xeroHelperVisible
                || markothHelperVisible
                || galienHelperVisible
                || gorbHelperVisible
                || elderHuHelperVisible
                || noEyesHelperVisible
                || dungDefenderHelperVisible
                || whiteDefenderHelperVisible
                || hiveKnightHelperVisible
                || brokenVesselHelperVisible
                || lostKinHelperVisible
                || noskHelperVisible
                || wingedNoskHelperVisible
                || uumuuHelperVisible
                || traitorLordHelperVisible
                || troupeMasterGrimmHelperVisible
                || nightmareKingGrimmHelperVisible
                || pureVesselHelperVisible
                || absoluteRadianceHelperVisible
                || paintmasterSheoHelperVisible
                || soulWarriorHelperVisible
                || nailsageSlyHelperVisible
                || soulMasterHelperVisible
                || soulTyrantHelperVisible
                || watcherKnightHelperVisible
                || oroMatoHelperVisible
                || godTamerHelperVisible
                || oblobblesHelperVisible
                || falseKnightHelperVisible
                || failedChampionHelperVisible
                || sisterOfBattleHelperVisible
                || mantisLordHelperVisible
                || flukemarmHelperVisible
                || vengeflyKingVisible
                ;
        }

        private void SetAllAdditionalGhostHelpersVisible(bool value)
        {
            SetMarmuHelperVisible(value);
            SetXeroHelperVisible(value);
            SetMarkothHelperVisible(value);
            SetGalienHelperVisible(value);
            SetGorbHelperVisible(value);
            SetElderHuHelperVisible(value);
            SetNoEyesHelperVisible(value);
            SetDungDefenderHelperVisible(value);
            SetWhiteDefenderHelperVisible(value);
            SetHiveKnightHelperVisible(value);
            SetBrokenVesselHelperVisible(value);
            SetLostKinHelperVisible(value);
            SetNoskHelperVisible(value);
            SetWingedNoskHelperVisible(value);
            SetUumuuHelperVisible(value);
            SetTraitorLordHelperVisible(value);
            SetTroupeMasterGrimmHelperVisible(value);
            SetNightmareKingGrimmHelperVisible(value);
            SetPureVesselHelperVisible(value);
            SetAbsoluteRadianceHelperVisible(value);
            SetPaintmasterSheoHelperVisible(value);
            SetSoulWarriorHelperVisible(value);
            SetNailsageSlyHelperVisible(value);
            SetSoulMasterHelperVisible(value);
            SetSoulTyrantHelperVisible(value);
            SetWatcherKnightHelperVisible(value);
            SetOroMatoHelperVisible(value);
            SetGodTamerHelperVisible(value);
            SetOblobblesHelperVisible(value);
            SetFalseKnightHelperVisible(value);
            SetFailedChampionHelperVisible(value);
            SetSisterOfBattleHelperVisible(value);
            SetMantisLordHelperVisible(value);
            SetFlukemarmHelperVisible(value);
            SetVengeflyKingVisible(value);
        }

        private void SetAdditionalGhostHelpersEnabled(bool value)
        {
            SetMarmuHelperEnabled(value);
            SetXeroHelperEnabled(value);
            SetMarkothHelperEnabled(value);
            SetGalienHelperEnabled(value);
            SetGorbHelperEnabled(value);
            SetElderHuHelperEnabled(value);
            SetNoEyesHelperEnabled(value);
            SetDungDefenderHelperEnabled(value);
            SetWhiteDefenderHelperEnabled(value);
            SetHiveKnightHelperEnabled(value);
            SetBrokenVesselHelperEnabled(value);
            SetLostKinHelperEnabled(value);
            SetNoskHelperEnabled(value);
            SetWingedNoskHelperEnabled(value);
            SetUumuuHelperEnabled(value);
            SetTraitorLordHelperEnabled(value);
            SetTroupeMasterGrimmHelperEnabled(value);
            SetNightmareKingGrimmHelperEnabled(value);
            SetPureVesselHelperEnabled(value);
            SetAbsoluteRadianceHelperEnabled(value);
            SetPaintmasterSheoHelperEnabled(value);
            SetSoulWarriorHelperEnabled(value);
            SetNailsageSlyHelperEnabled(value);
            SetSoulMasterHelperEnabled(value);
            SetSoulTyrantHelperEnabled(value);
            SetWatcherKnightHelperEnabled(value);
            SetOroMatoHelperEnabled(value);
            SetGodTamerHelperEnabled(value);
            SetOblobblesHelperEnabled(value);
            SetFalseKnightHelperEnabled(value);
            SetFailedChampionHelperEnabled(value);
            SetSisterOfBattleHelperEnabled(value);
            SetMantisLordHelperEnabled(value);
            SetFlukemarmHelperEnabled(value);
            SetVengeflyKingEnabled(value);
        }

        private void RefreshAdditionalGhostHelpersUi()
        {
            RefreshMarmuHelperUi();
            RefreshXeroHelperUi();
            RefreshMarkothHelperUi();
            RefreshGalienHelperUi();
            RefreshGorbHelperUi();
            RefreshElderHuHelperUi();
            RefreshNoEyesHelperUi();
            RefreshDungDefenderHelperUi();
            RefreshWhiteDefenderHelperUi();
            RefreshHiveKnightHelperUi();
            RefreshBrokenVesselHelperUi();
            RefreshLostKinHelperUi();
            RefreshNoskHelperUi();
            RefreshWingedNoskHelperUi();
            RefreshUumuuHelperUi();
            RefreshTraitorLordHelperUi();
            RefreshTroupeMasterGrimmHelperUi();
            RefreshNightmareKingGrimmHelperUi();
            RefreshPureVesselHelperUi();
            RefreshAbsoluteRadianceHelperUi();
            RefreshPaintmasterSheoHelperUi();
            RefreshSoulWarriorHelperUi();
            RefreshNailsageSlyHelperUi();
            RefreshSoulMasterHelperUi();
            RefreshSoulTyrantHelperUi();
            RefreshWatcherKnightHelperUi();
            RefreshOroMatoHelperUi();
            RefreshGodTamerHelperUi();
            RefreshOblobblesHelperUi();
            RefreshFalseKnightHelperUi();
            RefreshFailedChampionHelperUi();
            RefreshSisterOfBattleHelperUi();
            RefreshMantisLordHelperUi();
            RefreshFlukemarmHelperUi();
            RefreshVengeflyKingUi();
        }

        private void DestroyAdditionalGhostHelperRoots()
        {
            DestroyRoot(ref marmuHelperRoot);
            DestroyRoot(ref xeroHelperRoot);
            DestroyRoot(ref markothHelperRoot);
            DestroyRoot(ref galienHelperRoot);
            DestroyRoot(ref gorbHelperRoot);
            DestroyRoot(ref elderHuHelperRoot);
            DestroyRoot(ref noEyesHelperRoot);
            DestroyRoot(ref dungDefenderHelperRoot);
            DestroyRoot(ref whiteDefenderHelperRoot);
            DestroyRoot(ref hiveKnightHelperRoot);
            DestroyRoot(ref brokenVesselHelperRoot);
            DestroyRoot(ref lostKinHelperRoot);
            DestroyRoot(ref noskHelperRoot);
            DestroyRoot(ref wingedNoskHelperRoot);
            DestroyRoot(ref uumuuHelperRoot);
            DestroyRoot(ref traitorLordHelperRoot);
            DestroyRoot(ref troupeMasterGrimmHelperRoot);
            DestroyRoot(ref nightmareKingGrimmHelperRoot);
            DestroyRoot(ref pureVesselHelperRoot);
            DestroyRoot(ref absoluteRadianceHelperRoot);
            DestroyRoot(ref paintmasterSheoHelperRoot);
            DestroyRoot(ref soulWarriorHelperRoot);
            DestroyRoot(ref nailsageSlyHelperRoot);
            DestroyRoot(ref soulMasterHelperRoot);
            DestroyRoot(ref soulTyrantHelperRoot);
            DestroyRoot(ref watcherKnightHelperRoot);
            DestroyRoot(ref oroMatoHelperRoot);
            DestroyRoot(ref godTamerHelperRoot);
            DestroyRoot(ref oblobblesHelperRoot);
            DestroyRoot(ref falseKnightHelperRoot);
            DestroyRoot(ref failedChampionHelperRoot);
            DestroyRoot(ref sisterOfBattleHelperRoot);
            DestroyRoot(ref mantisLordHelperRoot);
            DestroyRoot(ref flukemarmHelperRoot);
            DestroyRoot(ref vengeflyKingRoot);
        }

        private void OpenAdditionalGhostHelperOverlay(Action<bool> setVisible)
        {
            returnToBossManipulateOnClose = true;
            SetBossManipulateVisible(false);
            setVisible(true);
        }

        private void CloseAdditionalGhostHelperOverlay(Action<bool> setVisible)
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            setVisible(false);
        }

        private void BuildAdditionalGhostHelpersOverlayUi()
        {
            BuildMarmuHelperOverlayUi();
            BuildXeroHelperOverlayUi();
            BuildMarkothHelperOverlayUi();
            BuildGalienHelperOverlayUi();
            BuildGorbHelperOverlayUi();
            BuildElderHuHelperOverlayUi();
            BuildNoEyesHelperOverlayUi();
            BuildDungDefenderHelperOverlayUi();
            BuildWhiteDefenderHelperOverlayUi();
            BuildHiveKnightHelperOverlayUi();
            BuildBrokenVesselHelperOverlayUi();
            BuildLostKinHelperOverlayUi();
            BuildNoskHelperOverlayUi();
            BuildWingedNoskHelperOverlayUi();
            BuildUumuuHelperOverlayUi();
            BuildTraitorLordHelperOverlayUi();
            BuildTroupeMasterGrimmHelperOverlayUi();
            BuildNightmareKingGrimmHelperOverlayUi();
            BuildPureVesselHelperOverlayUi();
            BuildAbsoluteRadianceHelperOverlayUi();
            BuildPaintmasterSheoHelperOverlayUi();
            BuildSoulWarriorHelperOverlayUi();
            BuildNailsageSlyHelperOverlayUi();
            BuildSoulMasterHelperOverlayUi();
            BuildSoulTyrantHelperOverlayUi();
            BuildWatcherKnightHelperOverlayUi();
            BuildOroMatoHelperOverlayUi();
            BuildGodTamerHelperOverlayUi();
            BuildOblobblesHelperOverlayUi();
            BuildFalseKnightHelperOverlayUi();
            BuildFailedChampionHelperOverlayUi();
            BuildFlukemarmHelperOverlayUi();
            BuildVengeflyKingOverlayUi();
            BuildSisterOfBattleHelperOverlayUi();
            BuildMantisLordHelperOverlayUi();
        }

        private void BuildStandardGhostHelperOverlayUi(
            ref GameObject? helperRoot,
            ref RectTransform? helperContent,
            string helperKey,
            int canvasSortOrder,
            float panelHeight,
            float rowSpacing,
            Func<bool> getEnabled,
            Action<bool> setEnabled,
            Func<bool> getP5Hp,
            Action<bool> setP5Hp,
            Func<bool> getUseMaxHp,
            Action<bool> setUseMaxHp,
            Func<int> getMaxHp,
            Action<int> setMaxHp,
            Action onResetClicked,
            Action onBackClicked,
            out Text? toggleValue,
            out Image? toggleIcon,
            out Text? p5HpValue,
            out Text? useMaxHpValue,
            out InputField? maxHpField)
        {
            helperRoot = new GameObject($"{helperKey}OverlayCanvas");
            helperRoot.transform.SetParent(transform, false);

            Canvas canvas = helperRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortOrder;

            CanvasScaler scaler = helperRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            helperRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = helperRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(helperRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new GameObject($"{helperKey}Panel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", $"Modules/{helperKey}".Localize(), 52, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 210f);
            titleRect.sizeDelta = new Vector2(RowWidth, 60f);

            float resetY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);
            helperContent = content;

            string rowKey = helperKey.EndsWith("Helper", StringComparison.Ordinal)
                ? helperKey.Substring(0, helperKey.Length - "Helper".Length)
                : helperKey;

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;
            CreateToggleRowWithIcon(
                content,
                $"{helperKey}EnableRow",
                $"Settings/{helperKey}/Enable".Localize(),
                rowY,
                getEnabled,
                setEnabled,
                out toggleValue,
                out toggleIcon
            );

            lastY = rowY;
            rowY += rowSpacing;
            CreateToggleRow(
                content,
                $"{rowKey}P5HpRow",
                $"Settings/{helperKey}/P5HP".Localize(),
                rowY,
                getP5Hp,
                setP5Hp,
                out p5HpValue
            );

            lastY = rowY;
            rowY += rowSpacing;
            CreateToggleRow(
                content,
                $"{rowKey}UseMaxHpRow",
                $"Settings/{helperKey}/UseMaxHP".Localize(),
                rowY,
                getUseMaxHp,
                setUseMaxHp,
                out useMaxHpValue
            );

            lastY = rowY;
            rowY += rowSpacing;
            CreateAdjustInputRow(
                content,
                $"{rowKey}MaxHpRow",
                $"Settings/{helperKey}/MaxHP".Localize(),
                rowY,
                getMaxHp,
                setMaxHp,
                1,
                999999,
                10,
                out maxHpField
            );

            lastY = rowY;
            rowY += rowSpacing;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, $"{helperKey}ResetRow", $"Settings/{helperKey}/Reset".Localize(), resetY, onResetClicked);
            CreateButtonRow(panel.transform, $"{helperKey}BackRow", "Back", backY, onBackClicked);
        }


    }
}
