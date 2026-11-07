using System;
using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private GameObject CreateClickableRow(Transform parent, string name, float y, out Image image, out Button button)
        {
            GameObject row = CreateRow(parent, name, y, new Vector2(RowWidth, RowHeight));
            image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            button = row.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return row;
        }

        private RowHighlight AddRowHighlight(GameObject row, Image image)
        {
            RowHighlight highlight = CreateRowHighlight(row, image);
            AttachRowHighlight(row, highlight);
            return highlight;
        }

        private void CreateToggleRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<bool> getter,
            Action<bool> setter,
            out Text valueText)
        {
            GameObject row = CreateClickableRow(parent, name, y, out Image image, out Button button);

            _ = CreateRowLabel(row.transform, label);
            Text valueLabel = CreateRowValue(row.transform, getter() ? "ON" : "OFF");
            valueText = valueLabel;

            button.onClick.AddListener(() =>
            {
                bool newValue = !getter();
                setter(newValue);
                UpdateToggleValue(valueLabel, newValue);
            });

            AddRowHighlight(row, image);
        }

        private void CreateToggleRowWithIcon(
            Transform parent,
            string name,
            string label,
            float y,
            Func<bool> getter,
            Action<bool> setter,
            out Text valueText,
            out Image? iconImage)
        {
            GameObject row = CreateClickableRow(parent, name, y, out Image image, out Button button);

            bool initialValue = getter();
            _ = CreateRowLabelWithIcon(row.transform, label, initialValue, out iconImage);
            Text valueLabel = CreateRowValue(row.transform, initialValue ? "ON" : "OFF");
            valueText = valueLabel;
            Image? toggleIcon = iconImage;

            button.onClick.AddListener(() =>
            {
                bool newValue = !getter();
                setter(newValue);
                bool current = getter();
                UpdateToggleValue(valueLabel, current);
                UpdateToggleIcon(toggleIcon, current);
            });

            AddRowHighlight(row, image);
        }

        private void CreateCycleRow(
            Transform parent,
            string name,
            string label,
            float y,
            string[] options,
            Func<int> getter,
            Action<int> setter,
            out Text valueText)
        {
            GameObject row = CreateClickableRow(parent, name, y, out Image image, out Button button);

            _ = CreateRowLabel(row.transform, label);

            int current = ClampOptionIndex(getter(), options.Length);
            Text valueLabel = CreateRowValue(row.transform, options[current]);
            valueText = valueLabel;

            button.onClick.AddListener(() =>
            {
                int next = ClampOptionIndex(getter(), options.Length) + 1;
                if (next >= options.Length)
                {
                    next = 0;
                }

                setter(next);
                valueLabel.text = options[next];
            });

            AddRowHighlight(row, image);
        }

        private void CreateSelectRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<string> valueGetter,
            Action onClick,
            out Text valueText)
        {
            GameObject row = CreateClickableRow(parent, name, y, out Image image, out Button button);

            _ = CreateRowLabel(row.transform, label);
            Text valueLabel = CreateRowValue(row.transform, valueGetter());
            valueText = valueLabel;

            button.onClick.AddListener(() => onClick());

            AddRowHighlight(row, image);
        }

        private void CreateReadOnlyValueRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<string> valueGetter,
            out Text valueText)
        {
            GameObject row = CreateRow(parent, name, y, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            _ = CreateRowLabel(row.transform, label);

            Text valueLabel = CreateText(row.transform, "Value", valueGetter(), 26, TextAnchor.MiddleRight);
            RectTransform rect = valueLabel.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-20f, 0f);
            rect.sizeDelta = new Vector2(280f, RowHeight);

            valueText = valueLabel;
        }

        private void CreateAdjustInputRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<int> getter,
            Action<int> setter,
            int minValue,
            int maxValue,
            int baseStep,
            out InputField valueField)
        {
            GameObject row = CreateRow(parent, name, y, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            _ = CreateRowLabel(row.transform, label);

            InputField input = CreateInputField(
                row.transform,
                getter().ToString(),
                new Vector2(InputControlValueRight, 0f),
                new Vector2(InputControlValueWidth, RowHeight));
            valueField = input;

            input.onEndEdit.AddListener(value =>
                ApplyInputValue(value, getter, setter, input, minValue, maxValue));

            Button minus = CreateMiniButton(row.transform, "Minus", "-", new Vector2(InputControlMinusRight, 0f));
            minus.onClick.AddListener(() => AdjustIntInputValue(getter, setter, input, -1, minValue, maxValue, baseStep));

            Button plus = CreateMiniButton(row.transform, "Plus", "+", new Vector2(InputControlPlusRight, 0f));
            plus.onClick.AddListener(() => AdjustIntInputValue(getter, setter, input, 1, minValue, maxValue, baseStep));

            RowHighlight highlight = AddRowHighlight(row, image);
            AttachRowHighlight(input.gameObject, highlight);
            AttachRowHighlight(minus.gameObject, highlight);
            AttachRowHighlight(plus.gameObject, highlight);
        }

        private void CreateAdjustFloatInputRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<float> getter,
            Action<float> setter,
            float minValue,
            float maxValue,
            float baseStep,
            out InputField valueField)
        {
            GameObject row = CreateRow(parent, name, y, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            _ = CreateRowLabel(row.transform, label);

            InputField input = CreateInputField(
                row.transform,
                getter().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                new Vector2(InputControlValueRight, 0f),
                new Vector2(InputControlValueWidth, RowHeight));
            input.contentType = InputField.ContentType.DecimalNumber;
            valueField = input;

            input.onEndEdit.AddListener(value =>
                ApplyFloatInputValue(value, getter, setter, input, minValue, maxValue));

            Button minus = CreateMiniButton(row.transform, "Minus", "-", new Vector2(InputControlMinusRight, 0f));
            minus.onClick.AddListener(() => AdjustFloatInputValue(getter, setter, input, -1, minValue, maxValue, baseStep));

            Button plus = CreateMiniButton(row.transform, "Plus", "+", new Vector2(InputControlPlusRight, 0f));
            plus.onClick.AddListener(() => AdjustFloatInputValue(getter, setter, input, 1, minValue, maxValue, baseStep));

            RowHighlight highlight = AddRowHighlight(row, image);
            AttachRowHighlight(input.gameObject, highlight);
            AttachRowHighlight(minus.gameObject, highlight);
            AttachRowHighlight(plus.gameObject, highlight);
        }

        private void CreateSpeedRow(Transform parent, float labelY, float sliderY)
        {
            GameObject row = CreateRow(parent, "SpeedRow", labelY, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            _ = CreateRowLabel(row.transform, "Settings/fastSuperDashSpeedMultiplier".Localize());
            speedValueText = CreateRowValue(row.transform, FormatToggleValue(Modules.QoL.FastSuperDash.fastSuperDashSpeedMultiplier));

            GameObject sliderObj = new GameObject("SpeedSlider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            ConfigureRowRect(sliderRect, parent, sliderY, new Vector2(RowWidth - 120f, 24f));

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(1f, 1f, 1f, 0.15f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 1f;
            slider.maxValue = 10f;
            slider.value = Modules.QoL.FastSuperDash.fastSuperDashSpeedMultiplier;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 1f, 1f, 0.75f);
            slider.fillRect = fillRect;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(sliderObj.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(18f, 24f);

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            StyleHkSlider(sliderBg, fillAreaRect, fillImage, handleRect, handleImage);

            slider.onValueChanged.AddListener(OnSpeedChanged);
            speedSlider = slider;

            RowHighlight highlight = AddRowHighlight(row, image);
            AttachRowHighlight(sliderObj, highlight);
        }

        private void CreateFloatSliderRow(
            Transform parent,
            string name,
            string label,
            float labelY,
            float sliderY,
            float minValue,
            float maxValue,
            float value,
            int decimals,
            out Text valueText,
            out Slider slider)
        {
            GameObject row = CreateRow(parent, name, labelY, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            _ = CreateRowLabel(row.transform, label);
            valueText = CreateRowValue(row.transform, FormatFloatValue(value, decimals));

            GameObject sliderObj = new GameObject($"{name}Slider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            ConfigureRowRect(sliderRect, parent, sliderY, new Vector2(RowWidth - 120f, 24f));

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(1f, 1f, 1f, 0.15f);

            Slider sliderComponent = sliderObj.AddComponent<Slider>();
            sliderComponent.minValue = minValue;
            sliderComponent.maxValue = maxValue;
            sliderComponent.value = Mathf.Clamp(value, minValue, maxValue);
            sliderComponent.wholeNumbers = false;
            sliderComponent.direction = Slider.Direction.LeftToRight;

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 1f, 1f, 0.75f);
            sliderComponent.fillRect = fillRect;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(sliderObj.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(18f, 24f);

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            sliderComponent.handleRect = handleRect;
            sliderComponent.targetGraphic = handleImage;
            StyleHkSlider(sliderBg, fillAreaRect, fillImage, handleRect, handleImage);

            slider = sliderComponent;

            RowHighlight highlight = AddRowHighlight(row, image);
            AttachRowHighlight(sliderObj, highlight);
        }

        private void CreateButtonRow(Transform parent, string name, string label, float y, Action onClick)
        {
            GameObject row = CreateRow(parent, name, y, new Vector2(260f, ButtonRowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);

            Button button = row.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            Text text = CreateText(row.transform, "Label", label, 28, TextAnchor.MiddleCenter);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            StyleHkButtonText(text);

            AddRowHighlight(row, image);
        }

        private void CreateKeybindRow(
            Transform parent,
            string name,
            string label,
            float y,
            Func<string> valueGetter,
            Action onClick,
            out Text valueText)
        {
            GameObject row = CreateClickableRow(parent, name, y, out Image image, out Button button);
            button.onClick.AddListener(() => onClick());

            _ = CreateRowLabel(row.transform, label);

            Text valueLabel = CreateText(row.transform, "Value", valueGetter(), 26, TextAnchor.MiddleRight);
            RectTransform rect = valueLabel.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-20f, 0f);
            rect.sizeDelta = new Vector2(240f, RowHeight);
            valueText = valueLabel;

            AddRowHighlight(row, image);
        }
    }
}
