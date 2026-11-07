using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private GameObject? bossManipulateOtherRoomsResetConfirmRoot;
        private bool bossManipulateOtherRoomsResetConfirmVisible;

        private void CreateBossManipulateOtherRoomsResetConfirm(Transform parent)
        {
            bossManipulateOtherRoomsResetConfirmRoot = new GameObject("BossManipulateOtherRoomsResetConfirm");
            bossManipulateOtherRoomsResetConfirmRoot.transform.SetParent(parent, false);

            RectTransform rootRect = bossManipulateOtherRoomsResetConfirmRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = bossManipulateOtherRoomsResetConfirmRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.35f);

            CanvasGroup group = bossManipulateOtherRoomsResetConfirmRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dialogObj = new("Dialog");
            dialogObj.transform.SetParent(bossManipulateOtherRoomsResetConfirmRoot.transform, false);
            RectTransform dialogRect = dialogObj.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(740f, 340f);

            Image dialogImage = dialogObj.AddComponent<Image>();
            StyleHkDialog(dialogImage);

            Text label = CreateText(dialogObj.transform, "Label", "Do you really want to reset Other Rooms?", 24, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -28f);
            labelRect.sizeDelta = new Vector2(680f, 110f);

            CreateButtonRow(dialogObj.transform, "BossManipulateOtherRoomsResetYesRow", "Yes", -70f, OnBossManipulateOtherRoomsResetConfirmYes);
            CreateButtonRow(dialogObj.transform, "BossManipulateOtherRoomsResetNoRow", "No", -130f, OnBossManipulateOtherRoomsResetConfirmNo);
        }

        private void SetBossManipulateOtherRoomsResetConfirmVisible(bool value)
        {
            bossManipulateOtherRoomsResetConfirmVisible = value;
            if (bossManipulateOtherRoomsResetConfirmRoot != null)
            {
                bossManipulateOtherRoomsResetConfirmRoot.SetActive(value);
            }
        }

        private void OnBossManipulateOtherRoomsResetConfirmYes()
        {
            SetBossManipulateOtherRoomsResetConfirmVisible(false);

            OnVengeflyKingP1HelperResetDefaultsClicked();
            OnGruzMotherP1HelperResetDefaultsClicked();
            OnGorbP1HelperResetDefaultsClicked();
            OnSoulWarriorP1HelperResetDefaultsClicked();
            OnBroodingMawlekP1HelperResetDefaultsClicked();
            OnXeroP2HelperResetDefaultsClicked();
            OnMarmuP2HelperResetDefaultsClicked();
            OnNoskP2HelperResetDefaultsClicked();
            OnUumuuP3HelperResetDefaultsClicked();
            OnNoEyesP4HelperResetDefaultsClicked();
            OnMarkothP4HelperResetDefaultsClicked();

            RefreshBossManipulateCardVisuals();
            UpdateQuickMenuEntryStateColors();
        }

        private void OnBossManipulateOtherRoomsResetConfirmNo()
        {
            SetBossManipulateOtherRoomsResetConfirmVisible(false);
        }
    }
}
