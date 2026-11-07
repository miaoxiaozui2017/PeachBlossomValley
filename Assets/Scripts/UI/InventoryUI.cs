using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.Core;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 背包 UI（v0.1.0 灰盒版）：
    /// - 底部常驻快捷栏（槽位 0-9，数字键 1-0 选择）
    /// - Tab 键开关 24 格完整面板，点击格子选中
    /// - 面板显示当前金钱、选中物品名称与描述
    /// - 订阅 OnInventoryChanged / OnMoneyChanged 自动刷新
    /// - 使用 LayoutGroup 自动布局，保证任意分辨率下都对齐
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
        private readonly Button[] _slotButtons = new Button[24];

        private Text _moneyText;
        private Text _infoText;

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

        private void Start()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
                RefreshMoney(PlayerWallet.Instance.Money);
            }
        }

        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
            }
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
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
            UpdateInfoBar();
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

        /// <summary>选中物品信息栏：显示名称、描述、数量；未选或空槽则提示</summary>
        private void UpdateInfoBar()
        {
            if (_infoText == null) return;

            var inventory = InventorySystem.Instance;
            if (inventory == null || SelectedIndex < 0 || SelectedIndex >= inventory.Capacity)
            {
                _infoText.text = "点击格子选中物品";
                return;
            }

            var slot = inventory.GetSlot(SelectedIndex);
            if (slot == null || slot.IsEmpty)
            {
                _infoText.text = "空槽位";
                return;
            }

            _infoText.text = $"{slot.item.itemName}  x{slot.count}\n{slot.item.description}";
        }

        private void RefreshMoney(int money)
        {
            if (_moneyText != null) _moneyText.text = $"¥ {money}";
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

            UpdateInfoBar();
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

        // ---------------- 灰盒 UI 构建（LayoutGroup 自动布局） ----------------

        private void BuildUI()
        {
            var canvasGo = new GameObject("InventoryUI_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
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
            hotbarRect.sizeDelta = new Vector2(_hotbarCount * 62f, 60f);
            hotbarRect.anchoredPosition = new Vector2(0, 12f);

            var bg = hotbarGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.35f);

            // HorizontalLayoutGroup 让快捷栏内格子自动横向排列
            var hgroup = hotbarGo.AddComponent<HorizontalLayoutGroup>();
            hgroup.padding = new RectOffset(0, 0, 0, 0);
            hgroup.spacing = 4f;
            hgroup.childAlignment = TextAnchor.MiddleCenter;
            hgroup.childControlWidth = false;
            hgroup.childControlHeight = false;
            hgroup.childForceExpandWidth = false;
            hgroup.childForceExpandHeight = false;

            for (int i = 0; i < _hotbarCount; i++)
            {
                CreateSlot(hotbarGo.transform, i, 56f, out _hotbarIcons[i], out _hotbarCounts[i], out _);
            }
        }

        private void BuildFullPanel(Transform canvasTransform)
        {
            _panel = new GameObject("InventoryPanel");
            _panel.transform.SetParent(canvasTransform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(488f, 420f);
            panelRect.anchoredPosition = new Vector2(200f, 0f);

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.22f, 0.18f, 0.14f, 0.94f);

            // 顶层 VerticalLayoutGroup：标题栏 → 格子网格 → 信息栏 三段
            var vgroup = _panel.AddComponent<VerticalLayoutGroup>();
            vgroup.padding = new RectOffset(10, 10, 8, 8);
            vgroup.spacing = 8;
            vgroup.childAlignment = TextAnchor.UpperCenter;
            vgroup.childControlWidth = false;
            vgroup.childControlHeight = false;
            vgroup.childForceExpandWidth = true;
            vgroup.childForceExpandHeight = false;

            // ① 标题栏（水平布局：标题在左，金钱在右）
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panel.transform, false);
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.35f, 0.28f, 0.18f, 0.95f);
            var titleHgroup = titleBar.AddComponent<HorizontalLayoutGroup>();
            titleHgroup.padding = new RectOffset(10, 10, 8, 8);
            titleHgroup.spacing = 8;
            titleHgroup.childForceExpandWidth = true;
            titleHgroup.childForceExpandHeight = false;
            titleHgroup.childControlWidth = true;

            var titleText = CreateTextObj("Title", titleBar.transform, "背包 (Tab 开关)", 22, FontStyle.Bold, new Color(1f, 0.93f, 0.75f));
            titleText.alignment = TextAnchor.MiddleLeft;

            _moneyText = CreateTextObj("Money", titleBar.transform, "¥ 0", 22, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));
            _moneyText.alignment = TextAnchor.MiddleRight;

            // ② 格子网格（GridLayoutGroup 自动排列 6 列 x 4 行）
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(_panel.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(0, 280f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64f, 64f);
            grid.spacing = new Vector2(8f, 6f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.childControlWidth = false;
            grid.childControlHeight = false;

            // 创建 24 格，GridLayoutGroup 自动定位
            for (int i = 0; i < 24; i++)
            {
                int slotIndex = i;
                CreateSlot(gridGo.transform, slotIndex, 64f, out _slotIcons[slotIndex], out _slotCounts[slotIndex], out _slotBorders[slotIndex]);

                // Button 挂在 slot 主 Image(border) 所在 GameObject 上，拦截点击
                var button = _slotBorders[slotIndex].gameObject.AddComponent<Button>();
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(() => SelectSlot(slotIndex));
                _slotButtons[slotIndex] = button;
            }

            // ② 格子容器尺寸自适应内容（让格子区域填满，不被 force expand 压扁）
            var gridFitter = gridGo.AddComponent<ContentSizeFitter>();
            gridFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            gridFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            // ③ 信息栏
            var infoGo = new GameObject("InfoBar");
            infoGo.transform.SetParent(_panel.transform, false);
            var infoBg = infoGo.AddComponent<Image>();
            infoBg.color = new Color(0.12f, 0.1f, 0.08f, 0.92f);
            var infoLayout = infoGo.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(10, 10, 6, 6);
            infoLayout.childForceExpandWidth = true;
            infoLayout.childControlWidth = true;

            _infoText = CreateTextObj("Info", infoGo.transform, "点击格子选中物品", 16, FontStyle.Normal, new Color(0.92f, 0.9f, 0.85f));
            _infoText.alignment = TextAnchor.MiddleLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Overflow;

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
            icon.raycastTarget = false;

            count = CreateTextObj("Count", slotGo.transform, string.Empty, 16, FontStyle.Bold, Color.white);
            var countRect = count.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.35f, 0f);
            countRect.anchorMax = new Vector2(0.95f, 0.3f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            count.alignment = TextAnchor.LowerRight;
            count.raycastTarget = false;
        }

        private static Text CreateTextObj(string name, Transform parent, string content, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}