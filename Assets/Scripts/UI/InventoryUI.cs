using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.Core;

namespace PBV.UI
{
    /// <summary>
    /// 背包 UI（v0.1.0 灰盒版）：
    /// - 底部常驻快捷栏（槽位 0-9，数字键 1-0 选择）
    /// - Tab 键开关 24 格完整面板，点击格子选中
    /// - 订阅 OnInventoryChanged 自动刷新
    /// - UI 在 Awake 自动构建，后续可替换为正式美术 UI
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _hotbarCount = 10;

        /// <summary>当前选中的槽位索引（-1 未选中）</summary>
        public int SelectedIndex { get; private set; } = -1;

        private GameObject _panel;
        private readonly Image[] _hotbarIcons = new Image[10];
        private readonly Text[] _hotbarCounts = new Text[10];
        private readonly Image[] _slotIcons = new Image[24];
        private readonly Text[] _slotCounts = new Text[24];
        private readonly Image[] _slotBorders = new Image[24];

        private static Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
            RefreshAll();

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged += RefreshAll;
            }
        }

        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
            }
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.tabKey.wasPressedThisFrame) TogglePanel();

            // 数字键 1-0 选择快捷栏槽位
            SelectByKey(keyboard.digit1Key, 0);
            SelectByKey(keyboard.digit2Key, 1);
            SelectByKey(keyboard.digit3Key, 2);
            SelectByKey(keyboard.digit4Key, 3);
            SelectByKey(keyboard.digit5Key, 4);
            SelectByKey(keyboard.digit6Key, 5);
            SelectByKey(keyboard.digit7Key, 6);
            SelectByKey(keyboard.digit8Key, 7);
            SelectByKey(keyboard.digit9Key, 8);
            SelectByKey(keyboard.digit0Key, 9);
        }

        private void SelectByKey(UnityEngine.InputSystem.Controls.KeyControl key, int slotIndex)
        {
            if (key.wasPressedThisFrame) SelectSlot(slotIndex);
        }

        private void TogglePanel()
        {
            if (_panel != null) _panel.SetActive(!_panel.activeSelf);
        }

        private void SelectSlot(int index)
        {
            SelectedIndex = index;
            UpdateSelectionHighlight();
        }

        private void UpdateSelectionHighlight()
        {
            for (int i = 0; i < _slotBorders.Length; i++)
            {
                if (_slotBorders[i] != null)
                {
                    _slotBorders[i].color = (i == SelectedIndex) ? new Color(1f, 0.85f, 0.3f) : new Color(0, 0, 0, 0.4f);
                }
            }
        }

        private void RefreshAll()
        {
            var inventory = InventorySystem.Instance;
            if (inventory == null) return;

            // 快捷栏（前 10 格）
            for (int i = 0; i < _hotbarCount && i < inventory.Capacity; i++)
            {
                ApplySlotVisual(inventory.GetSlot(i), _hotbarIcons[i], _hotbarCounts[i]);
            }

            // 完整面板 24 格
            for (int i = 0; i < _slotIcons.Length && i < inventory.Capacity; i++)
            {
                ApplySlotVisual(inventory.GetSlot(i), _slotIcons[i], _slotCounts[i]);
            }
        }

        private static void ApplySlotVisual(InventorySlot slot, Image icon, Text countText)
        {
            if (icon == null) return;

            if (slot == null || slot.IsEmpty)
            {
                icon.enabled = false;
                icon.sprite = null;
                if (countText != null) countText.text = string.Empty;
            }
            else
            {
                icon.enabled = true;
                icon.sprite = slot.item.icon;
                if (countText != null) countText.text = slot.count > 1 ? slot.count.ToString() : string.Empty;
            }
        }

        // ---------------- 灰盒 UI 构建 ----------------

        private void BuildUI()
        {
            var canvasGo = new GameObject("InventoryUI_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildHotbar(canvasGo.transform);
            BuildFullPanel(canvasGo.transform);
        }

        private void BuildHotbar(Transform canvasTransform)
        {
            var hotbarGo = new GameObject("Hotbar");
            hotbarGo.transform.SetParent(canvasTransform, false);
            var hotbarRect = hotbarGo.AddComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0.0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0.0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.sizeDelta = new Vector2(_hotbarCount * 62f, 62f);
            hotbarRect.anchoredPosition = new Vector2(0, 12f);

            var bg = hotbarGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.25f);

            for (int i = 0; i < _hotbarCount; i++)
            {
                CreateSlot(hotbarGo.transform, i, 58f, out _slotIcons[i], out _slotCounts[i], out _);

                var slotRect = _slotIcons[i].GetComponentInParent<RectTransform>();
                slotRect.anchoredPosition = new Vector2(-_hotbarCount * 62f / 2f + 31f + i * 62f, 0);
            }
        }

        private void BuildFullPanel(Transform canvasTransform)
        {
            _panel = new GameObject("InventoryPanel");
            _panel.transform.SetParent(canvasTransform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(6 * 76f + 24f, 4 * 76f + 60f);
            panelRect.anchoredPosition = new Vector2(-16f, -16f);

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.22f, 0.18f, 0.14f, 0.94f);

            // 标题栏背景（与面板区分）
            var titleBgGo = new GameObject("TitleBg");
            titleBgGo.transform.SetParent(_panel.transform, false);
            var titleBgRect = titleBgGo.AddComponent<RectTransform>();
            titleBgRect.anchorMin = new Vector2(0f, 0.88f);
            titleBgRect.anchorMax = new Vector2(1f, 1f);
            titleBgRect.offsetMin = Vector2.zero;
            titleBgRect.offsetMax = Vector2.zero;
            var titleBg = titleBgGo.AddComponent<Image>();
            titleBg.color = new Color(0.35f, 0.28f, 0.18f, 0.95f);

            CreateText("Title", _panel.transform, new Vector2(0f, 0.88f), new Vector2(1f, 1f), 22, FontStyle.Bold, new Color(1f, 0.93f, 0.75f), "背包 (Tab 开关)");

            // 6 x 4 = 24 格
            int index = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int slotIndex = index;
                    CreateSlot(_panel.transform, slotIndex, 66f, out _slotIcons[slotIndex], out _slotCounts[slotIndex], out _slotBorders[slotIndex]);

                    var slotRect = _slotIcons[slotIndex].GetComponentInParent<RectTransform>();
                    slotRect.anchoredPosition = new Vector2(-6f * 76f / 2f + 12f + 38f + col * 76f, -30f - row * 76f);

                    // 点击选中
                    var button = slotRect.gameObject.AddComponent<Button>();
                    button.transition = UnityEngine.UI.Selectable.Transition.None;
                    button.onClick.AddListener(() => SelectSlot(slotIndex));
                    index++;
                }
            }

            _panel.SetActive(false);
        }

        private void CreateSlot(Transform parent, int slotIndex, float size, out Image icon, out Text count, out Image border)
        {
            var slotGo = new GameObject($"Slot_{slotIndex}");
            slotGo.transform.SetParent(parent, false);
            var slotRect = slotGo.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(size, size);

            border = slotGo.AddComponent<Image>();
            border.color = new Color(0, 0, 0, 0.4f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.15f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;

            count = CreateText("Count", slotGo.transform, new Vector2(0.35f, 0f), new Vector2(0.95f, 0.3f), 16, FontStyle.Bold, Color.white, string.Empty);
            count.alignment = TextAnchor.LowerRight;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int size, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
