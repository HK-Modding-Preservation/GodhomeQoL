using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ToggleableBindings;
using ToggleableBindings.VanillaBindings;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
		private void CreateGearSwitcherPresetDeleteConfirm(Transform parent)
		{
			gearSwitcherPresetDeleteRoot = new GameObject("GearSwitcherPresetDeleteConfirm");
			gearSwitcherPresetDeleteRoot.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = gearSwitcherPresetDeleteRoot.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			Image image = gearSwitcherPresetDeleteRoot.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0.6f);
			CanvasGroup canvasGroup = gearSwitcherPresetDeleteRoot.AddComponent<CanvasGroup>();
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			GameObject gameObject = new GameObject("Dialog");
			gameObject.transform.SetParent(gearSwitcherPresetDeleteRoot.transform, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = Vector2.zero;
			rectTransform2.sizeDelta = new Vector2(520f, 220f);
			Image image2 = gameObject.AddComponent<Image>();
			StyleHkDialog(image2);
			Text text = CreateText(gameObject.transform, "Label", "Delete preset?", 28, TextAnchor.MiddleCenter);
			RectTransform rectTransform3 = text.rectTransform;
			rectTransform3.anchorMin = new Vector2(0.5f, 1f);
			rectTransform3.anchorMax = new Vector2(0.5f, 1f);
			rectTransform3.pivot = new Vector2(0.5f, 1f);
			rectTransform3.anchoredPosition = new Vector2(0f, -20f);
			rectTransform3.sizeDelta = new Vector2(480f, 50f);
			gearSwitcherPresetDeleteLabel = text;
			CreateButtonRow(gameObject.transform, "GearSwitcherPresetDeleteYesRow", "Yes", -30f, OnGearSwitcherPresetDeleteYes);
			CreateButtonRow(gameObject.transform, "GearSwitcherPresetDeleteNoRow", "No", -80f, OnGearSwitcherPresetDeleteNo);
		}

		private void CreateGearSwitcherResetConfirm(Transform parent)
		{
			gearSwitcherResetConfirmRoot = new GameObject("GearSwitcherResetConfirm");
			gearSwitcherResetConfirmRoot.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = gearSwitcherResetConfirmRoot.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			Image image = gearSwitcherResetConfirmRoot.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0.6f);
			CanvasGroup canvasGroup = gearSwitcherResetConfirmRoot.AddComponent<CanvasGroup>();
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			GameObject gameObject = new GameObject("Dialog");
			gameObject.transform.SetParent(gearSwitcherResetConfirmRoot.transform, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = Vector2.zero;
			rectTransform2.sizeDelta = new Vector2(640f, 260f);
			Image image2 = gameObject.AddComponent<Image>();
			StyleHkDialog(image2);
			Text text = CreateText(gameObject.transform, "Label", "Are you sure? This will remove all presets you've created. Do you want to continue?", 24, TextAnchor.MiddleCenter);
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			RectTransform rectTransform3 = text.rectTransform;
			rectTransform3.anchorMin = new Vector2(0.5f, 1f);
			rectTransform3.anchorMax = new Vector2(0.5f, 1f);
			rectTransform3.pivot = new Vector2(0.5f, 1f);
			rectTransform3.anchoredPosition = new Vector2(0f, -20f);
			rectTransform3.sizeDelta = new Vector2(600f, 120f);
			CreateButtonRow(gameObject.transform, "GearSwitcherResetYesRow", "Yes", -70f, OnGearSwitcherResetConfirmYes);
			CreateButtonRow(gameObject.transform, "GearSwitcherResetNoRow", "No", -120f, OnGearSwitcherResetConfirmNo);
		}

		private void SetGearSwitcherPresetDeleteVisible(bool value)
		{
			gearSwitcherPresetDeleteVisible = value;
			if (gearSwitcherPresetDeleteRoot != null)
			{
				gearSwitcherPresetDeleteRoot.SetActive(value);
			}
			if (!value)
			{
				gearSwitcherPresetDeleteTargetName = null;
			}
		}

		private void SetGearSwitcherResetConfirmVisible(bool value)
		{
			gearSwitcherResetConfirmVisible = value;
			if (gearSwitcherResetConfirmRoot != null)
			{
				gearSwitcherResetConfirmRoot.SetActive(value);
			}
		}

		private void CreateQuickSettingsResetConfirm(Transform parent)
		{
			quickSettingsResetConfirmRoot = new GameObject("QuickSettingsResetConfirm");
			quickSettingsResetConfirmRoot.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = quickSettingsResetConfirmRoot.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			Image image = quickSettingsResetConfirmRoot.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0.6f);
			CanvasGroup canvasGroup = quickSettingsResetConfirmRoot.AddComponent<CanvasGroup>();
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			GameObject gameObject = new GameObject("Dialog");
			gameObject.transform.SetParent(quickSettingsResetConfirmRoot.transform, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = Vector2.zero;
			rectTransform2.sizeDelta = new Vector2(760f, 300f);
			Image image2 = gameObject.AddComponent<Image>();
			StyleHkDialog(image2);
			Text text = CreateText(gameObject.transform, "Label", "Are you serious? This will reset all mod settings to the default ones and delete all current settings", 24, TextAnchor.MiddleCenter);
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			RectTransform rectTransform3 = text.rectTransform;
			rectTransform3.anchorMin = new Vector2(0.5f, 1f);
			rectTransform3.anchorMax = new Vector2(0.5f, 1f);
			rectTransform3.pivot = new Vector2(0.5f, 1f);
			rectTransform3.anchoredPosition = new Vector2(0f, -24f);
			rectTransform3.sizeDelta = new Vector2(700f, 150f);
			CreateButtonRow(gameObject.transform, "QuickSettingsResetYesRow", "Yes", -90f, OnQuickSettingsResetConfirmYes);
			CreateButtonRow(gameObject.transform, "QuickSettingsResetNoRow", "No", -150f, OnQuickSettingsResetConfirmNo);
		}

		private void SetQuickSettingsResetConfirmVisible(bool value)
		{
			quickSettingsResetConfirmVisible = value;
			if (quickSettingsResetConfirmRoot != null)
			{
				quickSettingsResetConfirmRoot.SetActive(value);
			}
		}

		private void StartGearSwitcherPresetDelete(string presetName)
		{
			if (!IsBuiltinPresetName(presetName))
			{
				CancelGearSwitcherPresetRename();
				gearSwitcherPresetDeleteTargetName = presetName;
				if (gearSwitcherPresetDeleteLabel != null)
				{
					string gearSwitcherPresetDisplayName = GetGearSwitcherPresetDisplayName(presetName);
					gearSwitcherPresetDeleteLabel.text = "Delete \"" + gearSwitcherPresetDisplayName + "\"?";
				}
				SetGearSwitcherPresetDeleteVisible(value: true);
			}
		}

		private void StartGearSwitcherPresetDeleteByIndex(int index)
		{
			string[] gearSwitcherPresetOptions = GetGearSwitcherPresetOptions();
			if (index >= 0 && index < gearSwitcherPresetOptions.Length)
			{
				StartGearSwitcherPresetDelete(gearSwitcherPresetOptions[index]);
			}
		}

		private void OnGearSwitcherPresetDeleteYes()
		{
			string text = gearSwitcherPresetDeleteTargetName ?? string.Empty;
			SetGearSwitcherPresetDeleteVisible(value: false);
			if (!string.IsNullOrWhiteSpace(text) && GearSwitcher.DeleteCustomPreset(text))
			{
				if (string.Equals(gearSwitcherSelectedPreset, text, StringComparison.OrdinalIgnoreCase))
				{
					gearSwitcherSelectedPreset = "FullGear";
					GearSwitcher.ApplyPreset(gearSwitcherSelectedPreset, allowQueue: true);
				}
				RebuildGearSwitcherPresetOverlay();
				SetGearSwitcherPresetVisible(value: true);
			}
		}

		private void OnGearSwitcherPresetDeleteNo()
		{
			SetGearSwitcherPresetDeleteVisible(value: false);
		}

		private void OnGearSwitcherResetDefaultsClicked()
		{
			SetGearSwitcherResetConfirmVisible(value: true);
		}

		private void OnGearSwitcherResetConfirmYes()
		{
			GearSwitcher.ResetDefaults();
			gearSwitcherSelectedPreset = "FullGear";
			SetGearSwitcherEnabled(value: false);
			RefreshGearSwitcherUi();
			SetGearSwitcherResetConfirmVisible(value: false);
		}

		private void OnGearSwitcherResetConfirmNo()
		{
			SetGearSwitcherResetConfirmVisible(value: false);
		}
    }
}
