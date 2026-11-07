using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const int BossManipulateOtherRoomsCanvasSortOrder = 10066;
        private const float BossManipulateOtherRoomsY = -300f;
        private GameObject? bossManipulateOtherRoomsRoot;
        private bool bossManipulateOtherRoomsVisible;
        private bool returnToBossManipulateOtherRoomsOnClose;

        private void BuildBossManipulateOtherRoomsOverlayUi()
        {
            bossManipulateOtherRoomsRoot = new GameObject("BossManipulateOtherRoomsOverlayCanvas");
            bossManipulateOtherRoomsRoot.transform.SetParent(transform, false);

            Canvas canvas = bossManipulateOtherRoomsRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = BossManipulateOtherRoomsCanvasSortOrder;

            CanvasScaler scaler = bossManipulateOtherRoomsRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            bossManipulateOtherRoomsRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = bossManipulateOtherRoomsRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new("Dim");
            dim.transform.SetParent(bossManipulateOtherRoomsRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new("BossManipulateOtherRoomsPanel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(BossManipulatePanelWidth, BossManipulatePanelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", "Other Rooms", 52, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, (BossManipulatePanelHeight * 0.5f) - 70f);
            titleRect.sizeDelta = new Vector2(BossManipulatePanelWidth - 120f, 60f);

            float backY = GetFixedBackY(BossManipulatePanelHeight);
            float resetY = BossManipulateResetY;
            float controlsTopY = resetY + (ButtonRowHeight * 0.5f);
            RectTransform gridRoot = CreateBossManipulateGridRoot(panel.transform, titleRect, controlsTopY);
            BuildBossManipulateOtherRoomsImageGrid(gridRoot);
            CreateButtonRow(panel.transform, "BossManipulateOtherRoomsResetRow", "Reset Default", resetY, OnBossManipulateOtherRoomsResetAllClicked);
            CreateButtonRow(panel.transform, "BossManipulateOtherRoomsBackRow", "Back", backY, OnBossManipulateOtherRoomsBackClicked);
            CreateBossManipulateOtherRoomsResetConfirm(panel.transform);
            SetBossManipulateOtherRoomsResetConfirmVisible(false);
            RefreshBossManipulateCardVisuals();
        }

        private void BuildBossManipulateOtherRoomsImageGrid(RectTransform gridRoot)
        {
            var cards = new (string Label, string ImageFile, Type ModuleType, Action OnClick)[]
            {
                ("Vengefly King P1", "Vengefly King.png", typeof(Modules.BossChallenge.VengeflyKingP1Helper), OnBossManipulateOtherRoomsVengeflyKingP1Clicked),
                ("Gruz Mother P1", "Gruz Mother.png", typeof(Modules.BossChallenge.GruzMotherP1Helper), OnBossManipulateOtherRoomsGruzMotherP1Clicked),
                ("Gorb P1", "Gorb.png", typeof(Modules.BossChallenge.GorbP1Helper), OnBossManipulateOtherRoomsGorbP1Clicked),
                ("Soul Warrior P1", "Soul Warrior.png", typeof(Modules.BossChallenge.SoulWarriorP1Helper), OnBossManipulateOtherRoomsSoulWarriorP1Clicked),
                ("Brooding Mawlek P1", "Brooding Mawlek.png", typeof(Modules.BossChallenge.BroodingMawlekP1Helper), OnBossManipulateOtherRoomsBroodingMawlekP1Clicked),
                ("Xero P2", "Xero.png", typeof(Modules.BossChallenge.XeroP2Helper), OnBossManipulateOtherRoomsXeroP2Clicked),
                ("Marmu P2", "Marmu.png", typeof(Modules.BossChallenge.MarmuP2Helper), OnBossManipulateOtherRoomsMarmuP2Clicked),
                ("Nosk P2", "Nosk.png", typeof(Modules.BossChallenge.NoskP2Helper), OnBossManipulateOtherRoomsNoskP2Clicked),
                ("Uumuu P3", "Uumuu.png", typeof(Modules.BossChallenge.UumuuP3Helper), OnBossManipulateOtherRoomsUumuuP3Clicked),
                ("No Eyes P4", "No Eyes.png", typeof(Modules.BossChallenge.NoEyesP4Helper), OnBossManipulateOtherRoomsNoEyesP4Clicked),
                ("Markoth P4", "Markoth.png", typeof(Modules.BossChallenge.MarkothP4Helper), OnBossManipulateOtherRoomsMarkothP4Clicked),
            };

            const int columns = 11;
            const float spacingX = 10f;
            const float spacingY = 12f;
            int rows = Mathf.CeilToInt(cards.Length / (float)columns);

            float gridWidth = gridRoot.sizeDelta.x;
            float gridHeight = gridRoot.sizeDelta.y;
            float cardWidth = (gridWidth - ((columns - 1) * spacingX)) / columns;
            float cardHeight = (gridHeight - ((rows - 1) * spacingY)) / rows;

            float totalWidth = (cardWidth * columns) + ((columns - 1) * spacingX);
            float totalHeight = (cardHeight * rows) + ((rows - 1) * spacingY);
            float startX = -(totalWidth * 0.5f) + (cardWidth * 0.5f);
            float startY = (totalHeight * 0.5f) - (cardHeight * 0.5f);

            for (int index = 0; index < cards.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                float x = startX + (column * (cardWidth + spacingX));
                float y = startY - (row * (cardHeight + spacingY));
                CreateBossManipulateImageCard(
                    gridRoot,
                    $"BossManipulateOtherRoomsCard{index + 1:D2}",
                    cards[index].Label,
                    cards[index].ImageFile,
                    cards[index].ModuleType,
                    x,
                    y,
                    cardWidth,
                    cardHeight,
                    cards[index].OnClick);
            }
        }

        private void SetBossManipulateOtherRoomsVisible(bool value)
        {
            bossManipulateOtherRoomsVisible = value;
            if (bossManipulateOtherRoomsRoot != null)
            {
                bossManipulateOtherRoomsRoot.SetActive(value);
            }

            if (value)
            {
                RefreshBossManipulateCardVisuals();
                SetBossManipulateOtherRoomsResetConfirmVisible(false);
            }
            else
            {
                SetBossManipulateOtherRoomsResetConfirmVisible(false);
            }

            UpdateUiState();
        }

        private void OnBossManipulateOtherRoomsClicked()
        {
            returnToBossManipulateOnClose = true;
            SetBossManipulateVisible(false);
            SetBossManipulateOtherRoomsVisible(true);
        }

        private void OnBossManipulateOtherRoomsBackClicked()
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

            SetBossManipulateOtherRoomsVisible(false);
        }

        private void OnBossManipulateOtherRoomsResetAllClicked()
        {
            SetBossManipulateOtherRoomsResetConfirmVisible(true);
        }

        private void OnBossManipulateOtherRoomsGruzMotherP1Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetGruzMotherP1HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsVengeflyKingP1Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetVengeflyKingP1HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsBroodingMawlekP1Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetBroodingMawlekP1HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsNoskP2Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetNoskP2HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsUumuuP3Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetUumuuP3HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsSoulWarriorP1Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetSoulWarriorP1HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsNoEyesP4Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetNoEyesP4HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsMarmuP2Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetMarmuP2HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsXeroP2Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetXeroP2HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsMarkothP4Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetMarkothP4HelperVisible(true);
        }

        private void OnBossManipulateOtherRoomsGorbP1Clicked()
        {
            returnToBossManipulateOtherRoomsOnClose = true;
            SetBossManipulateOtherRoomsVisible(false);
            SetGorbP1HelperVisible(true);
        }
    }
}
