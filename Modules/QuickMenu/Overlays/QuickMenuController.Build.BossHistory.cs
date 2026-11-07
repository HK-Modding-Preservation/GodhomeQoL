using System.Globalization;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const int BossHistoryCanvasSortOrder = 10034;
        private const float BossHistoryRowSpacing = 52f;
        private static readonly Color BossHistoryGoldColor = new(0.902f, 0.749f, 0.384f, 1f);
        private static readonly string[] BossHistoryLevelNames = { "Attuned", "Ascended", "Radiant" };

        private GameObject? bossHistoryRoot;
        private bool bossHistoryVisible;
        private string? bossHistorySelected;
        private bool returnToShowHpOnDeathOnClose;
        private GameObject? bossHistoryConfirmRoot;
        private Text? bossHistoryConfirmLabel;
        private Action? bossHistoryConfirmAction;
        private bool bossHistoryConfirmVisible;
        private readonly Dictionary<string, Sprite?> bossHistorySprites = new(StringComparer.Ordinal);
        private readonly List<(BossHistoryEntry Entry, RectTransform Row)> bossHistoryRows = new();
        private UnityEngine.UI.InputField? bossHistorySearchField;
        private Text? bossHistorySearchCount;
        private RectTransform? bossHistoryContent;
        private string bossHistorySearchText = string.Empty;
        private float bossHistoryRowsStartY;
        private float bossHistoryViewHeight;

        private void BuildBossHistoryOverlayUi()
        {
            bossHistoryConfirmAction = null;
            bossHistoryConfirmVisible = false;
            bossHistorySearchField = null;
            bossHistorySearchCount = null;
            bossHistoryContent = null;
            bossHistoryRows.Clear();

            BossHistoryBoss? boss = bossHistorySelected == null ? null : BossHistory.Find(bossHistorySelected);
            if (boss != null && BossHistory.CurrentSlot > 0)
            {
                BuildBossHistoryDetail(boss);
            }
            else
            {
                bossHistorySelected = null;
                BuildBossHistoryGrid();
            }
        }

        private void BuildBossHistoryGrid()
        {
            bossHistoryRoot = CreateOverlayFrame("BossHistoryCanvas", BossHistoryCanvasSortOrder, "BossHistoryPanel", BossManipulatePanelHeight, "BossHistory/Title".Localize(), 52, 0f, out GameObject panel, out RectTransform titleRect);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.sizeDelta = new Vector2(BossManipulatePanelWidth, BossManipulatePanelHeight);
            titleRect.anchoredPosition = new Vector2(0f, (BossManipulatePanelHeight * 0.5f) - 70f);
            titleRect.sizeDelta = new Vector2(BossManipulatePanelWidth - 120f, 60f);

            float backY = GetFixedBackY(BossManipulatePanelHeight);
            float clearY = BossManipulateResetY;
            if (BossHistory.CurrentSlot > 0)
            {
                RectTransform gridRoot = CreateBossManipulateGridRoot(panel.transform, titleRect, clearY + (RowHeight * 0.5f));
                BuildBossHistoryCards(gridRoot);
                CreateButtonRow(panel.transform, "BossHistoryClearAllRow", "BossHistory/ClearAll".Localize(), clearY, OnBossHistoryClearAllClicked);
            }
            else
            {
                Text note = CreateText(panel.transform, "NoSave", "BossHistory/NoSave".Localize(), 30, TextAnchor.MiddleCenter);
                RectTransform noteRect = note.rectTransform;
                noteRect.anchorMin = new Vector2(0.5f, 0.5f);
                noteRect.anchorMax = new Vector2(0.5f, 0.5f);
                noteRect.pivot = new Vector2(0.5f, 0.5f);
                noteRect.anchoredPosition = Vector2.zero;
                noteRect.sizeDelta = new Vector2(1200f, 80f);
            }

            CreateButtonRow(panel.transform, "BossHistoryBackRow", "Back", backY, OnBossHistoryBackClicked);
            CreateBossHistoryConfirm(panel.transform);
            SetBossHistoryConfirmVisible(false);
        }

        private void BuildBossHistoryCards(RectTransform gridRoot)
        {
            const int columns = 11;
            const float spacingX = 10f;
            const float spacingY = 12f;
            BossHistoryBoss[] bosses = BossHistory.Bosses;
            int rows = Mathf.CeilToInt(bosses.Length / (float)columns);

            float cardWidth = (gridRoot.sizeDelta.x - ((columns - 1) * spacingX)) / columns;
            float cardHeight = (gridRoot.sizeDelta.y - ((rows - 1) * spacingY)) / rows;
            float startX = -((cardWidth * columns) + ((columns - 1) * spacingX)) * 0.5f + (cardWidth * 0.5f);
            float startY = ((cardHeight * rows) + ((rows - 1) * spacingY)) * 0.5f - (cardHeight * 0.5f);

            for (int index = 0; index < bosses.Length; index++)
            {
                float x = startX + ((index % columns) * (cardWidth + spacingX));
                float y = startY - ((index / columns) * (cardHeight + spacingY));
                CreateBossHistoryCard(gridRoot, index, bosses[index], x, y, cardWidth, cardHeight);
            }
        }

        private void CreateBossHistoryCard(Transform parent, int index, BossHistoryBoss boss, float x, float y, float width, float height)
        {
            int count = BossHistory.GetEntries(boss.Key).Count;

            GameObject card = new($"BossHistoryCard{index + 1:D2}");
            card.transform.SetParent(parent, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.sizeDelta = new Vector2(width, height);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0.07f);
            Button button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = cardImage;
            string key = boss.Key;
            button.onClick.AddListener(() => OnBossHistoryBossClicked(key));
            RowHighlight highlight = CreateHkCardHighlight(card, cardImage);
            AttachRowHighlight(card, highlight);

            const float labelHeight = 48f;
            float imageHeight = Mathf.Max(50f, height - labelHeight - 10f);
            GameObject imageObject = new("Image");
            imageObject.transform.SetParent(card.transform, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = new Vector2(0f, (height * 0.5f) - (imageHeight * 0.5f) - 4f);
            imageRect.sizeDelta = new Vector2(width - 12f, imageHeight);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            ApplyBossHistorySprite(image, boss);
            if (count == 0)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a * 0.45f);
            }

            Text nameText = CreateText(card.transform, "Label", boss.Key, 17, TextAnchor.MiddleCenter);
            if (count == 0)
            {
                nameText.color = new Color(nameText.color.r, nameText.color.g, nameText.color.b, 0.6f);
            }
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 20f);
            nameRect.sizeDelta = new Vector2(0f, 26f);

            string countText = count > 0 ? string.Format("BossHistory/Records".Localize(), count) : "BossHistory/NoRecords".Localize();
            Text countLabel = CreateText(card.transform, "Count", countText, 15, TextAnchor.MiddleCenter);
            countLabel.color = count > 0 ? BossHistoryGoldColor : new Color(0.75f, 0.75f, 0.75f, 0.6f);
            RectTransform countRect = countLabel.rectTransform;
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.anchoredPosition = new Vector2(0f, 2f);
            countRect.sizeDelta = new Vector2(0f, 20f);
        }

        private void ApplyBossHistorySprite(Image image, BossHistoryBoss boss)
        {
            if (!bossHistorySprites.TryGetValue(boss.Image, out Sprite? sprite))
            {
                sprite = LoadBossManipulateIconSprite(boss.Image, $"BossHistory_{boss.Key}");
                if (sprite != null)
                {
                    sprite.texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }

                bossHistorySprites[boss.Image] = sprite;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.12f);
            }
        }

        private void BuildBossHistoryDetail(BossHistoryBoss boss)
        {
            bossHistoryRoot = CreateOverlayFrame("BossHistoryCanvas", BossHistoryCanvasSortOrder, "BossHistoryPanel", PanelHeight, boss.Key, 52, 210f, out GameObject panel, out RectTransform titleRect);

            GameObject iconObject = new("BossImage");
            iconObject.transform.SetParent(panel.transform, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-(PanelWidth * 0.5f) + 70f, titleRect.anchoredPosition.y);
            iconRect.sizeDelta = new Vector2(84f, 84f);
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            ApplyBossHistorySprite(icon, boss);

            float panelHeight = PanelHeight;
            float clearY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, clearY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            List<BossHistoryEntry> entries = BossHistory.GetEntries(boss.Key);
            bossHistoryRows.Clear();
            bossHistoryContent = content;
            bossHistoryViewHeight = viewHeight;

            if (entries.Count > 0)
            {
                CreateBossHistorySearchRow(content, rowY);
                rowY += RowSpacing;
            }

            GameObject summaryRow = CreateRow(content, "BossHistorySummaryRow", rowY, new Vector2(RowWidth, RowHeight));
            Text summary = CreateRowLabel(summaryRow.transform, entries.Count == 0 ? "BossHistory/Empty".Localize() : BuildBossHistorySummary(entries));
            summary.supportRichText = true;
            summary.alignment = TextAnchor.MiddleCenter;
            summary.color = BossHistoryGoldColor;
            ConfigureBossHistoryLabel(summary, RowWidth - 40f, 24);
            rowY += RowSpacing;
            bossHistoryRowsStartY = rowY;

            HashSet<BossHistoryEntry> best = GetBossHistoryBest(entries);
            for (int i = 0; i < entries.Count; i++)
            {
                RectTransform row = CreateBossHistoryEntryRow(content, i, boss.Key, entries[i], best.Contains(entries[i]), rowY);
                bossHistoryRows.Add((entries[i], row));
                rowY += BossHistoryRowSpacing;
            }

            ApplyBossHistoryFilter();
            if (entries.Count > 0)
            {
                string key = boss.Key;
                CreateButtonRow(panel.transform, "BossHistoryClearBossRow", "BossHistory/ClearBoss".Localize(), clearY, () => OnBossHistoryClearBossClicked(key));
            }

            CreateButtonRow(panel.transform, "BossHistoryBackRow", "Back", backY, OnBossHistoryBackClicked);
            CreateBossHistoryConfirm(panel.transform);
            SetBossHistoryConfirmVisible(false);
        }

        private RectTransform CreateBossHistoryEntryRow(Transform parent, int index, string bossKey, BossHistoryEntry entry, bool best, float y)
        {
            GameObject row = CreateRow(parent, $"BossHistoryRow{index}", y, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            RowHighlight highlight = CreateRowHighlight(row, image);
            AttachRowHighlight(row, highlight);

            Text label = CreateRowLabel(row.transform, FormatBossHistoryEntry(entry));
            label.supportRichText = true;
            ConfigureBossHistoryLabel(label, RowWidth - 90f, 26);
            if (best)
            {
                label.color = BossHistoryGoldColor;
            }

            Button? delete = CreateGearSwitcherPresetDeleteButton(row.transform, () => StartBossHistoryDeleteEntry(bossKey, entry));
            if (delete != null)
            {
                AttachRowHighlight(delete.gameObject, highlight);
            }

            return (RectTransform)row.transform;
        }

        private void CreateBossHistorySearchRow(Transform parent, float y)
        {
            GameObject row = CreateRow(parent, "BossHistorySearchRow", y, new Vector2(RowWidth, RowHeight));

            GameObject fieldObject = new("SearchField");
            fieldObject.transform.SetParent(row.transform, false);
            RectTransform fieldRect = fieldObject.AddComponent<RectTransform>();
            fieldRect.anchorMin = new Vector2(0.5f, 0.5f);
            fieldRect.anchorMax = new Vector2(0.5f, 0.5f);
            fieldRect.pivot = new Vector2(0.5f, 0.5f);
            fieldRect.anchoredPosition = new Vector2(-(RowWidth * 0.5f) + 200f, 0f);
            fieldRect.sizeDelta = new Vector2(380f, 40f);
            Image fieldImage = fieldObject.AddComponent<Image>();
            fieldImage.sprite = GetHkChamferSprite(false);
            fieldImage.type = Image.Type.Sliced;
            fieldImage.color = new Color(1f, 1f, 1f, 0.1f);

            UnityEngine.UI.InputField field = fieldObject.AddComponent<UnityEngine.UI.InputField>();
            field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            field.contentType = UnityEngine.UI.InputField.ContentType.Standard;
            field.characterLimit = 11;
            field.caretColor = Color.white;
            field.selectionColor = new Color(1f, 1f, 1f, 0.25f);
            field.targetGraphic = fieldImage;

            Text text = CreateText(fieldObject.transform, "Text", string.Empty, 24, TextAnchor.MiddleLeft);
            StretchBossHistoryFieldText(text.rectTransform);
            text.supportRichText = false;
            field.textComponent = text;

            Text placeholder = CreateText(fieldObject.transform, "Placeholder", "BossHistory/SearchHint".Localize(), 20, TextAnchor.MiddleLeft);
            StretchBossHistoryFieldText(placeholder.rectTransform);
            placeholder.color = new Color(0.7f, 0.68f, 0.64f, 0.8f);
            placeholder.fontStyle = FontStyle.Italic;
            field.placeholder = placeholder;

            field.text = bossHistorySearchText;
            field.onValueChanged.AddListener(OnBossHistorySearchChanged);
            bossHistorySearchField = field;

            CreateSettingsPresetTextButton(row.transform, "SearchMinus", "-100", new Vector2(70f, 0f), () => StepBossHistorySearch(-100));
            CreateSettingsPresetTextButton(row.transform, "SearchPlus", "+100", new Vector2(156f, 0f), () => StepBossHistorySearch(100));

            Text count = CreateText(row.transform, "Count", string.Empty, 22, TextAnchor.MiddleRight);
            RectTransform countRect = count.rectTransform;
            countRect.anchorMin = new Vector2(0.5f, 0.5f);
            countRect.anchorMax = new Vector2(0.5f, 0.5f);
            countRect.pivot = new Vector2(1f, 0.5f);
            countRect.anchoredPosition = new Vector2((RowWidth * 0.5f) - 10f, 0f);
            countRect.sizeDelta = new Vector2(200f, 40f);
            count.color = BossHistoryGoldColor;
            bossHistorySearchCount = count;
        }

        private static void StretchBossHistoryFieldText(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
        }

        private void OnBossHistorySearchChanged(string value)
        {
            bossHistorySearchText = value ?? string.Empty;
            ApplyBossHistoryFilter();
        }

        private void StepBossHistorySearch(int delta)
        {
            if (bossHistorySearchField == null)
            {
                return;
            }

            int next;
            if (TryParseBossHistoryRange(bossHistorySearchField.text, out int min, out _))
            {
                next = Math.Max(0, min + delta);
            }
            else if (delta > 0)
            {
                next = 0;
            }
            else
            {
                return;
            }

            bossHistorySearchField.text = next.ToString(CultureInfo.InvariantCulture);
        }

        private bool IsBossHistorySearchFocused() => bossHistoryVisible && bossHistorySearchField != null && bossHistorySearchField.isFocused;

        private static bool TryParseBossHistoryRange(string? text, out int min, out int max)
        {
            min = 0;
            max = int.MaxValue;
            string value = (text ?? string.Empty).Replace(" ", string.Empty);
            if (value.Length == 0)
            {
                return false;
            }

            int dash = value.IndexOf('-', 1);
            if (dash > 0)
            {
                if (!int.TryParse(value.Substring(0, dash), NumberStyles.None, CultureInfo.InvariantCulture, out int first)
                    || !int.TryParse(value.Substring(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int second))
                {
                    return false;
                }

                min = Math.Min(first, second);
                max = Math.Max(first, second);
                return true;
            }

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int start))
            {
                return false;
            }

            min = start;
            max = start + 99;
            return true;
        }

        private void ApplyBossHistoryFilter()
        {
            if (bossHistoryContent == null)
            {
                return;
            }

            bool filtered = TryParseBossHistoryRange(bossHistorySearchText, out int min, out int max);
            bool invalid = !filtered && !string.IsNullOrWhiteSpace(bossHistorySearchText);
            float y = bossHistoryRowsStartY;
            float lastY = bossHistoryRowsStartY - RowSpacing;
            int shown = 0;
            foreach ((BossHistoryEntry entry, RectTransform row) in bossHistoryRows)
            {
                if (row == null)
                {
                    continue;
                }

                int hp = entry.Victory ? 0 : entry.TotalHp;
                bool visible = !filtered || (hp >= min && hp <= max);
                row.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                ConfigureRowRect(row, bossHistoryContent, y, row.sizeDelta);
                lastY = y;
                y += BossHistoryRowSpacing;
                shown++;
            }

            SetScrollContentHeight(bossHistoryContent, bossHistoryViewHeight, lastY, RowHeight);
            if (bossHistorySearchCount != null)
            {
                bossHistorySearchCount.text = filtered
                    ? string.Format("BossHistory/Found".Localize(), shown, bossHistoryRows.Count)
                    : invalid ? "BossHistory/SearchInvalid".Localize() : string.Empty;
            }
        }

        private static void ConfigureBossHistoryLabel(Text label, float width, int size)
        {
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = size;
            label.resizeTextMinSize = 14;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.sizeDelta = new Vector2(width, 0f);
        }

        private static string GetBossHistoryCategory(BossHistoryEntry entry)
        {
            if (entry.Pantheon > 0 || entry.PantheonBoss > 0)
            {
                return "BossHistory/Pantheons".Localize();
            }

            return entry.Level >= 0 && entry.Level < BossHistoryLevelNames.Length ? BossHistoryLevelNames[entry.Level] : "?";
        }

        private static string GetBossHistoryKind(BossHistoryEntry entry)
        {
            if (entry.Pantheon > 0)
            {
                return string.Format("BossHistory/PantheonBoss".Localize(), entry.Pantheon, entry.PantheonBoss);
            }

            if (entry.PantheonBoss > 0)
            {
                return string.Format("BossHistory/PantheonUnknown".Localize(), entry.PantheonBoss);
            }

            return GetBossHistoryCategory(entry);
        }

        private static string FormatBossHistoryResult(BossHistoryEntry entry)
        {
            return entry.Victory ? "BossHistory/Victory".Localize() : $"{entry.TotalHp} / {entry.TotalMax}";
        }

        private static string FormatBossHistoryTime(BossHistoryEntry entry)
        {
            return entry.Time.ToString("dd.MM.yyyy  HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string FormatBossHistoryEntry(BossHistoryEntry entry)
        {
            string dim = ColorUtility.ToHtmlStringRGB(new Color(0.7f, 0.68f, 0.64f, 1f));
            string details = entry.Victory || entry.Parts.Count < 2
                ? string.Empty
                : $"   <color=#{dim}>({FormatBossHistoryParts(entry.Parts)})</color>";
            return $"{FormatBossHistoryTime(entry)}   <color=#{dim}>{GetBossHistoryKind(entry)}</color>   {FormatBossHistoryResult(entry)}{details}";
        }

        private static string FormatBossHistoryParts(List<BossHistoryPart> parts)
        {
            List<string> groups = new();
            foreach (IGrouping<string, BossHistoryPart> group in parts.GroupBy(part => part.Phase ?? string.Empty))
            {
                List<BossHistoryPart> items = group.ToList();
                bool sameName = items.All(part => string.Equals(part.Name, items[0].Name, StringComparison.Ordinal));
                string values = sameName
                    ? string.Join(", ", items.Select(part => $"{part.Hp}/{part.Max}"))
                    : string.Join(", ", items.Select(part => string.IsNullOrEmpty(part.Name) ? $"{part.Hp}/{part.Max}" : $"{part.Name} {part.Hp}/{part.Max}"));
                string prefix = string.Join(" ", new[] { group.Key, sameName ? items[0].Name : string.Empty }.Where(text => !string.IsNullOrEmpty(text)));
                groups.Add(prefix.Length > 0 ? $"{prefix} {values}" : values);
            }

            return string.Join("  |  ", groups);
        }

        private static IEnumerable<BossHistoryEntry> GetBossHistoryBestByCategory(List<BossHistoryEntry> entries)
        {
            return entries
                .GroupBy(GetBossHistoryCategory)
                .Select(group => group.OrderBy(entry => entry.Ratio).ThenBy(entry => entry.TotalHp).ThenByDescending(entry => entry.Time).First());
        }

        private static HashSet<BossHistoryEntry> GetBossHistoryBest(List<BossHistoryEntry> entries)
        {
            return new HashSet<BossHistoryEntry>(GetBossHistoryBestByCategory(entries));
        }

        private static string BuildBossHistorySummary(List<BossHistoryEntry> entries)
        {
            string[] order = { "Attuned", "Ascended", "Radiant", "BossHistory/Pantheons".Localize(), "?" };
            List<string> parts = GetBossHistoryBestByCategory(entries)
                .OrderBy(entry => Array.IndexOf(order, GetBossHistoryCategory(entry)))
                .Select(entry => $"{GetBossHistoryCategory(entry)}: {FormatBossHistoryResult(entry)}")
                .ToList();
            return $"{"BossHistory/Best".Localize()}   {string.Join("     ", parts)}";
        }

        private void CreateBossHistoryConfirm(Transform parent)
        {
            bossHistoryConfirmRoot = new GameObject("BossHistoryConfirm");
            bossHistoryConfirmRoot.transform.SetParent(parent, false);
            RectTransform rootRect = bossHistoryConfirmRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image dim = bossHistoryConfirmRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            CanvasGroup group = bossHistoryConfirmRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dialog = new("Dialog");
            dialog.transform.SetParent(bossHistoryConfirmRoot.transform, false);
            RectTransform dialogRect = dialog.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(680f, 280f);
            Image dialogImage = dialog.AddComponent<Image>();
            StyleHkDialog(dialogImage);

            Text text = CreateText(dialog.transform, "Label", string.Empty, 26, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -24f);
            textRect.sizeDelta = new Vector2(620f, 130f);
            bossHistoryConfirmLabel = text;

            CreateButtonRow(dialog.transform, "BossHistoryConfirmYesRow", "Yes", -70f, OnBossHistoryConfirmYes);
            CreateButtonRow(dialog.transform, "BossHistoryConfirmNoRow", "No", -122f, OnBossHistoryConfirmNo);
        }

        private void SetBossHistoryConfirmVisible(bool value)
        {
            bossHistoryConfirmVisible = value;
            if (bossHistoryConfirmRoot != null)
            {
                bossHistoryConfirmRoot.SetActive(value);
            }

            if (!value)
            {
                bossHistoryConfirmAction = null;
            }
        }

        private void ShowBossHistoryConfirm(string message, Action onYes)
        {
            if (bossHistoryConfirmLabel != null)
            {
                bossHistoryConfirmLabel.text = message;
            }

            SetBossHistoryConfirmVisible(true);
            bossHistoryConfirmAction = onYes;
        }

        private void OnBossHistoryConfirmYes()
        {
            Action? action = bossHistoryConfirmAction;
            SetBossHistoryConfirmVisible(false);
            action?.Invoke();
        }

        private void OnBossHistoryConfirmNo()
        {
            SetBossHistoryConfirmVisible(false);
        }

        private void SetBossHistoryVisible(bool value)
        {
            bossHistoryVisible = value;
            if (value)
            {
                RebuildBossHistoryOverlay(false);
            }
            else
            {
                SetBossHistoryConfirmVisible(false);
                bossHistorySelected = null;
                bossHistorySearchText = string.Empty;
                returnToShowHpOnDeathOnClose = false;
                if (bossHistoryRoot != null)
                {
                    bossHistoryRoot.SetActive(false);
                }
            }

            UpdateUiState();
        }

        private void RebuildBossHistoryOverlay(bool keepScroll)
        {
            float? scroll = null;
            if (keepScroll && bossHistoryRoot != null)
            {
                ScrollRect? oldScroll = bossHistoryRoot.GetComponentInChildren<ScrollRect>();
                if (oldScroll != null && oldScroll.content != null)
                {
                    scroll = oldScroll.content.anchoredPosition.y;
                }
            }

            DestroyRoot(ref bossHistoryRoot);
            BuildBossHistoryOverlayUi();
            if (bossHistoryRoot == null)
            {
                return;
            }

            bossHistoryRoot.SetActive(bossHistoryVisible);
            ScrollRect? newScroll = bossHistoryRoot.GetComponentInChildren<ScrollRect>();
            if (scroll != null && newScroll != null && newScroll.content != null)
            {
                Vector2 position = newScroll.content.anchoredPosition;
                newScroll.content.anchoredPosition = new Vector2(position.x, scroll.Value);
            }
        }

        private void OnShowHpOnDeathHistoryClicked()
        {
            bossHistorySelected = null;
            SetBossHistoryVisible(true);
            returnToShowHpOnDeathOnClose = true;
            SetShowHpOnDeathVisible(false);
        }

        private void OnBossHistoryBackClicked()
        {
            if (bossHistorySelected != null)
            {
                bossHistorySelected = null;
                bossHistorySearchText = string.Empty;
                RebuildBossHistoryOverlay(false);
                return;
            }

            bool reopenShowHp = returnToShowHpOnDeathOnClose;
            returnToShowHpOnDeathOnClose = false;
            if (reopenShowHp)
            {
                SetShowHpOnDeathVisible(true);
            }

            SetBossHistoryVisible(false);
        }

        private void OnBossHistoryBossClicked(string key)
        {
            bossHistorySearchText = string.Empty;
            bossHistorySelected = key;
            RebuildBossHistoryOverlay(false);
        }

        private void StartBossHistoryDeleteEntry(string key, BossHistoryEntry entry)
        {
            ShowBossHistoryConfirm(string.Format("BossHistory/ConfirmDeleteRow".Localize(), FormatBossHistoryTime(entry)), () =>
            {
                BossHistory.Remove(key, entry);
                RebuildBossHistoryOverlay(true);
                ShowStatusMessage("BossHistory/Deleted".Localize());
            });
        }

        private void OnBossHistoryClearBossClicked(string key)
        {
            int count = BossHistory.GetEntries(key).Count;
            ShowBossHistoryConfirm(string.Format("BossHistory/ConfirmClearBoss".Localize(), count, key), () =>
            {
                BossHistory.ClearBoss(key);
                RebuildBossHistoryOverlay(false);
                ShowStatusMessage(string.Format("BossHistory/ClearedBoss".Localize(), key));
            });
        }

        private void OnBossHistoryClearAllClicked()
        {
            ShowBossHistoryConfirm("BossHistory/ConfirmClearAll".Localize(), () =>
            {
                BossHistory.ClearAll();
                RebuildBossHistoryOverlay(false);
                ShowStatusMessage("BossHistory/ClearedAll".Localize());
            });
        }
    }
}
