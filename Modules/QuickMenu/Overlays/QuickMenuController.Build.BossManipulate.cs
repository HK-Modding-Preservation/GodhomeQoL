using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildBossManipulateOverlayUi()
        {
            bossManipulateRoot = new GameObject("BossManipulateOverlayCanvas");
            bossManipulateRoot.transform.SetParent(transform, false);

            Canvas canvas = bossManipulateRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = BossManipulateCanvasSortOrder;

            CanvasScaler scaler = bossManipulateRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            bossManipulateRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = bossManipulateRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(bossManipulateRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new GameObject("BossManipulatePanel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(BossManipulatePanelWidth, BossManipulatePanelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", "Categories/BossManipulate".Localize(), 52, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, (BossManipulatePanelHeight * 0.5f) - 70f);
            titleRect.sizeDelta = new Vector2(BossManipulatePanelWidth - 120f, 60f);

            float backY = GetFixedBackY(BossManipulatePanelHeight);
            float controlsTopY = Mathf.Max(BossManipulateOtherRoomsY, BossManipulateGlobalP5Y) + (RowHeight * 0.5f);
            RectTransform gridRoot = CreateBossManipulateGridRoot(panel.transform, titleRect, controlsTopY);
            BuildBossManipulateImageGrid(gridRoot);
            CreateToggleRow(
                panel.transform,
                "BossManipulateGlobalP5Row",
                "P5 HP",
                BossManipulateGlobalP5Y,
                GetBossManipulateGlobalP5Enabled,
                SetBossManipulateGlobalP5Enabled,
                out bossManipulateGlobalP5Value
            );
            CreateButtonRow(panel.transform, "BossManipulateOtherRoomsRow", "Other Rooms", BossManipulateOtherRoomsY, OnBossManipulateOtherRoomsClicked);
            CreateButtonRow(panel.transform, "BossManipulateResetRow", "Reset Default", BossManipulateResetY, OnBossManipulateResetAllClicked);
            CreateButtonRow(panel.transform, "BossManipulateBackRow", "Back", backY, OnBossManipulateBackClicked);
            CreateBossManipulateResetConfirm(panel.transform);
            SetBossManipulateResetConfirmVisible(false);
            RefreshBossManipulateCardVisuals();
            RefreshBossManipulateGlobalUi();
        }

        private static RectTransform CreateBossManipulateGridRoot(Transform panel, RectTransform titleRect, float controlsTopY)
        {
            float titleBottomY = titleRect.anchoredPosition.y - (titleRect.sizeDelta.y * 0.5f);
            float gridTopY = titleBottomY - 50f;
            float gridBottomY = controlsTopY + 18f;

            GameObject gridObj = new("BossManipulateGrid");
            gridObj.transform.SetParent(panel, false);

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, (gridTopY + gridBottomY) * 0.5f);
            gridRect.sizeDelta = new Vector2(BossManipulatePanelWidth - 70f, Mathf.Max(240f, gridTopY - gridBottomY));
            return gridRect;
        }

        private void BuildBossManipulateImageGrid(RectTransform gridRoot)
        {
            bossManipulateCards.Clear();

            var cards = new (string Label, string ImageFile, Type ModuleType, Action OnClick)[]
            {
                ("Gruz Mother", "Gruz Mother.png", typeof(Modules.BossChallenge.GruzMotherHelper), OnBossManipulateGruzClicked),
                ("Vengefly King", "Vengefly King.png", typeof(Modules.BossChallenge.VengeflyKing), OnBossManipulateVengeflyKingClicked),
                ("Brooding Mawlek", "Brooding Mawlek.png", typeof(Modules.BossChallenge.BroodingMawlekHelper), OnBossManipulateMawlekClicked),
                ("False Knight", "False Knight.png", typeof(Modules.BossChallenge.FalseKnightHelper), OnBossManipulateFalseKnightClicked),
                ("Failed Champion", "Failed Champion.png", typeof(Modules.BossChallenge.FailedChampionHelper), OnBossManipulateFailedChampionClicked),
                ("Hornet Protector", "Hornet Protector.png", typeof(Modules.BossChallenge.HornetProtectorHelper), OnBossManipulateHornetClicked),
                ("Hornet Sentinel", "Hornet Sentinel.png", typeof(Modules.BossChallenge.HornetSentinelHelper), OnBossManipulateHornetSentinelClicked),
                ("Massive Moss Charger", "Massive Moss Charger.png", typeof(Modules.BossChallenge.MassiveMossChargerHelper), OnBossManipulateMassiveMossClicked),
                ("Flukemarm", "Flukemarm.png", typeof(Modules.BossChallenge.FlukemarmHelper), OnBossManipulateFlukemarmClicked),
                ("Mantis Lords", "Mantis Lords.png", typeof(Modules.BossChallenge.MantisLordHelper), OnBossManipulateMantisLordClicked),
                ("Sisters of Battle", "Sisters of Battle.png", typeof(Modules.BossChallenge.SisterOfBattleHelper), OnBossManipulateSisterOfBattleClicked),
                ("Oblobbles", "Oblobbles.png", typeof(Modules.BossChallenge.OblobblesHelper), OnBossManipulateOblobblesClicked),
                ("Hive Knight", "Hive Knight.png", typeof(Modules.BossChallenge.HiveKnightHelper), OnBossManipulateHiveKnightClicked),
                ("Broken Vessel", "Broken Vessel.png", typeof(Modules.BossChallenge.BrokenVesselHelper), OnBossManipulateBrokenVesselClicked),
                ("Lost Kin", "Lost Kin.png", typeof(Modules.BossChallenge.LostKinHelper), OnBossManipulateLostKinClicked),
                ("Nosk", "Nosk.png", typeof(Modules.BossChallenge.NoskHelper), OnBossManipulateNoskClicked),
                ("Winged Nosk", "Winged Nosk.png", typeof(Modules.BossChallenge.WingedNoskHelper), OnBossManipulateWingedNoskClicked),
                ("The Collector", "The Collector.png", typeof(Modules.CollectorPhases.CollectorPhases), OnBossManipulateCollectorClicked),
                ("God Tamer", "God Tamer.png", typeof(Modules.BossChallenge.GodTamerHelper), OnBossManipulateGodTamerClicked),
                ("Crystal Guardian", "Crystal Guardian.png", typeof(Modules.BossChallenge.CrystalGuardianHelper), OnBossManipulateCrystalGuardianClicked),
                ("Enraged Guardian", "Enraged Guardian.png", typeof(Modules.BossChallenge.EnragedGuardianHelper), OnBossManipulateEnragedGuardianClicked),
                ("Uumuu", "Uumuu.png", typeof(Modules.BossChallenge.UumuuHelper), OnBossManipulateUumuuClicked),
                ("Traitor Lord", "Traitor Lord.png", typeof(Modules.BossChallenge.TraitorLordHelper), OnBossManipulateTraitorLordClicked),
                ("Grey Prince Zote", "Grey Prince Zote.png", typeof(Modules.BossChallenge.ZoteHelper), OnBossManipulateZoteClicked),
                ("Soul Warrior", "Soul Warrior.png", typeof(Modules.BossChallenge.SoulWarriorHelper), OnBossManipulateSoulWarriorClicked),
                ("Soul Master", "Soul Master.png", typeof(Modules.BossChallenge.SoulMasterHelper), OnBossManipulateSoulMasterClicked),
                ("Soul Tyrant", "Soul Tyrant.png", typeof(Modules.BossChallenge.SoulTyrantHelper), OnBossManipulateSoulTyrantClicked),
                ("Dung Defender", "Dung Defender.png", typeof(Modules.BossChallenge.DungDefenderHelper), OnBossManipulateDungDefenderClicked),
                ("White Defender", "White Defender.png", typeof(Modules.BossChallenge.WhiteDefenderHelper), OnBossManipulateWhiteDefenderClicked),
                ("Watcher Knight", "Watcher Knight.png", typeof(Modules.BossChallenge.WatcherKnightHelper), OnBossManipulateWatcherKnightClicked),
                ("No Eyes", "No Eyes.png", typeof(Modules.BossChallenge.NoEyesHelper), OnBossManipulateNoEyesClicked),
                ("Marmu", "Marmu.png", typeof(Modules.BossChallenge.MarmuHelper), OnBossManipulateMarmuClicked),
                ("Xero", "Xero.png", typeof(Modules.BossChallenge.XeroHelper), OnBossManipulateXeroClicked),
                ("Markoth", "Markoth.png", typeof(Modules.BossChallenge.MarkothHelper), OnBossManipulateMarkothClicked),
                ("Galien", "Galien.png", typeof(Modules.BossChallenge.GalienHelper), OnBossManipulateGalienClicked),
                ("Gorb", "Gorb.png", typeof(Modules.BossChallenge.GorbHelper), OnBossManipulateGorbClicked),
                ("Elder Hu", "Elder Hu.png", typeof(Modules.BossChallenge.ElderHuHelper), OnBossManipulateElderHuClicked),
                ("Oro & Mato", "Oro & Mato.png", typeof(Modules.BossChallenge.OroMatoHelper), OnBossManipulateOroMatoClicked),
                ("Paintmaster Sheo", "Paintmaster Sheo.png", typeof(Modules.BossChallenge.PaintmasterSheoHelper), OnBossManipulatePaintmasterSheoClicked),
                ("Nailsage Sly", "Great Nailsage Sly.png", typeof(Modules.BossChallenge.NailsageSlyHelper), OnBossManipulateNailsageSlyClicked),
                ("Pure Vessel", "Pure Vessel.png", typeof(Modules.BossChallenge.PureVesselHelper), OnBossManipulatePureVesselClicked),
                ("Grimm", "Grimm.png", typeof(Modules.BossChallenge.TroupeMasterGrimmHelper), OnBossManipulateTroupeMasterGrimmClicked),
                ("Nightmare King", "Nightmare King.png", typeof(Modules.BossChallenge.NightmareKingGrimmHelper), OnBossManipulateNightmareKingGrimmClicked),
                ("Absolute Radiance", "Absolute Radiance.png", typeof(Modules.BossChallenge.AbsoluteRadianceHelper), OnBossManipulateAbsoluteRadianceClicked),
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
                    $"BossManipulateCard{index + 1:D2}",
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

        private void CreateBossManipulateImageCard(
            Transform parent,
            string name,
            string label,
            string imageFile,
            Type moduleType,
            float x,
            float y,
            float width,
            float height,
            Action onClick)
        {
            GameObject card = new(name);
            card.transform.SetParent(parent, false);

            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.sizeDelta = new Vector2(width, height);
            CanvasGroup group = card.AddComponent<CanvasGroup>();

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0.07f);

            Button button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = cardImage;
            button.onClick.AddListener(() => onClick());

            RowHighlight highlight = CreateHkCardHighlight(card, cardImage);
            AttachRowHighlight(card, highlight);

            const float labelHeight = 30f;
            float imageHeight = Mathf.Max(50f, height - labelHeight - 10f);
            GameObject imageObj = new("Image");
            imageObj.transform.SetParent(card.transform, false);

            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = new Vector2(0f, (height * 0.5f) - (imageHeight * 0.5f) - 4f);
            imageRect.sizeDelta = new Vector2(width - 12f, imageHeight);

            Image image = imageObj.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            Sprite? sprite = LoadBossManipulateIconSprite(imageFile, $"BossManipulate_{label}");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.12f);
                LogDebug($"QuickMenu: missing Boss Manipulate image {imageFile}");
            }

            Text nameText = CreateText(card.transform, "Label", label, 17, TextAnchor.MiddleCenter);
            RectTransform labelRect = nameText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 2f);
            labelRect.sizeDelta = new Vector2(0f, labelHeight);

            bossManipulateCards.Add(new BossManipulateCardVisual(group, moduleType));
        }

        private void CreateBossManipulateResetConfirm(Transform parent)
        {
            bossManipulateResetConfirmRoot = new GameObject("BossManipulateResetConfirm");
            bossManipulateResetConfirmRoot.transform.SetParent(parent, false);

            RectTransform rootRect = bossManipulateResetConfirmRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = bossManipulateResetConfirmRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.35f);

            CanvasGroup group = bossManipulateResetConfirmRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dialogObj = new("Dialog");
            dialogObj.transform.SetParent(bossManipulateResetConfirmRoot.transform, false);
            RectTransform dialogRect = dialogObj.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(740f, 340f);

            Image dialogImage = dialogObj.AddComponent<Image>();
            StyleHkDialog(dialogImage);

            Text label = CreateText(dialogObj.transform, "Label", "Do you really want to reset everything?", 24, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -28f);
            labelRect.sizeDelta = new Vector2(680f, 110f);

            CreateButtonRow(dialogObj.transform, "BossManipulateResetYesRow", "Yes", -70f, OnBossManipulateResetConfirmYes);
            CreateButtonRow(dialogObj.transform, "BossManipulateResetNoRow", "No", -130f, OnBossManipulateResetConfirmNo);
        }

        private void SetBossManipulateResetConfirmVisible(bool value)
        {
            bossManipulateResetConfirmVisible = value;
            if (bossManipulateResetConfirmRoot != null)
            {
                bossManipulateResetConfirmRoot.SetActive(value);
            }
        }
    }
}
