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

        /// <summary>静态访问：当前选中槽位（供 NPCController 送礼等外部系统读取）</summary>
        public static int SelectedSlotIndex = 0;

        // 最大可扩容到 48 格；实际可见格子数由 InventorySystem.Capacity 决定
        private const int MaxSlots = 48;
        private const int Columns = 6;

        private GameObject _panel;
        private readonly Image[] _hotbarIcons = new Image[10];
        private readonly Text[] _hotbarCounts = new Text[10];
        private readonly Image[] _hotbarBorders = new Image[10]; // 快捷栏选中高亮（此前 out _ 丢弃了）
        private Text _selectedHintText; // 常驻：当前选中物品名 + 用法提示
        private readonly Image[] _slotIcons = new Image[MaxSlots];
        private readonly Text[] _slotCounts = new Text[MaxSlots];
        private readonly Image[] _slotBorders = new Image[MaxSlots];
        private readonly Button[] _slotButtons = new Button[MaxSlots];
        private readonly GameObject[] _slotGos = new GameObject[MaxSlots];

        private RectTransform _gridRect;
        private RectTransform _panelRect;
        private Text _titleText;
        private Text _moneyText;
        private Text _infoText;
        private int _lastCapacity = -1;
        private bool _subscribed;
        private bool _toolEventsSubscribed;
        private bool _syncingToolSelection; // 防止 选中槽位 ↔ 工具切换 互相递归
        private float _refreshTimer;

        private static Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
            // 不在 Awake 订阅：InventorySystem 与本组件挂在同一物体上，Awake 顺序不保证，
            // 那时 Instance 可能还是 null，订阅会静默丢失 —— 结果就是开局背包一片空白，
            // 直到扩容等事件碰巧触发一次刷新才显示物品。
        }

        private void Start()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
                RefreshMoney(PlayerWallet.Instance.SongCoins, PlayerWallet.Instance.RMB);
            }
            EnsureSubscribed();
            // 开局对齐：实例 SelectedIndex 默认 -1，而静态 SelectedSlotIndex 默认 0，
            // 两者不一致会导致「左键用的是第 0 格，快捷栏却没有任何高亮」。
            SelectSlot(0);
            RefreshAll();
        }

        /// <summary>懒订阅：Instance 一就绪就补上，且只补一次</summary>
        private void EnsureSubscribed()
        {
            if (!_subscribed && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged += RefreshAll;
                _subscribed = true;
            }
            if (!_toolEventsSubscribed && PBV.Player.ToolManager.Instance != null)
            {
                PBV.Player.ToolManager.Instance.OnToolChanged += OnToolChangedExternally;
                _toolEventsSubscribed = true;
            }
        }

        /// <summary>
        /// Q 键循环 / 读档等外部改动工具时，把选中槽位同步到该工具物品所在格，
        /// 否则左键仍会按旧选中物品分派，Q 看起来「切了没用」。
        /// </summary>
        private void OnToolChangedExternally(PBV.Player.ToolType tool)
        {
            if (_syncingToolSelection) return;
            var inv = InventorySystem.Instance;
            if (inv == null) return;
            string id = PBV.Player.ToolManager.ItemIdFromToolType(tool);
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < inv.Capacity; i++)
            {
                var s = inv.GetSlot(i);
                if (s != null && !s.IsEmpty && s.item != null && s.item.id == id)
                {
                    _syncingToolSelection = true;
                    SelectedIndex = i;
                    SelectedSlotIndex = i;
                    UpdateSelectionHighlight();
                    UpdateInfoBar();
                    UpdateSelectedHint();
                    _syncingToolSelection = false;
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
            }
            if (_toolEventsSubscribed && PBV.Player.ToolManager.Instance != null)
            {
                PBV.Player.ToolManager.Instance.OnToolChanged -= OnToolChangedExternally;
            }
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
            }
        }

        private void Update()
        {
            // 兜底三件套：懒订阅 / 容量变化 / 定时刷新。
            // 这些不依赖键盘，必须放在 keyboard==null 的提前 return 之前。
            EnsureSubscribed();

            var inv = InventorySystem.Instance;
            if (inv != null && inv.Capacity != _lastCapacity) RefreshAll();

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.2f;
                RefreshAll(); // 初始物资由 StarterKit 在首帧 Update 发放，任何事件漏掉都能补回来
            }

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
            if (_panel == null) return;
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open) RefreshAll(); // 打开瞬间强制对齐背包真实内容
        }

        private void SelectSlot(int index)
        {
            if (index < 0) return;
            SelectedIndex = index;
            SelectedSlotIndex = index;
            UpdateSelectionHighlight();
            UpdateInfoBar();
            SyncSelectedTool();
            UpdateSelectedHint();
        }

        /// <summary>
        /// 数字键/点击选中槽位时，若该格是工具物品则同步 ToolManager.CurrentTool，
        /// 让砍树（TreeSpot 判 CurrentTool==Axe）、HUD 工具行、Q 循环状态与选中格一致。
        /// </summary>
        private void SyncSelectedTool()
        {
            var inv = InventorySystem.Instance;
            if (inv == null || SelectedIndex < 0 || SelectedIndex >= inv.Capacity) return;
            var slot = inv.GetSlot(SelectedIndex);
            if (slot == null || slot.IsEmpty || slot.item == null || slot.item.id == null) return;
            var tool = PBV.Player.ToolManager.ToolTypeFromItemId(slot.item.id);
            if (tool.HasValue && PBV.Player.ToolManager.Instance != null &&
                PBV.Player.ToolManager.Instance.CurrentTool != tool.Value)
            {
                _syncingToolSelection = true;
                PBV.Player.ToolManager.Instance.SelectTool(tool.Value);
                _syncingToolSelection = false;
            }
        }

        /// <summary>常驻提示：当前选中物品名 + 该物品怎么用</summary>
        private void UpdateSelectedHint()
        {
            if (_selectedHintText == null) return;
            var inv = InventorySystem.Instance;
            var slot = (inv != null && SelectedIndex >= 0 && SelectedIndex < inv.Capacity) ? inv.GetSlot(SelectedIndex) : null;
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                _selectedHintText.text = "数字键 1-0 选中物品 · 左键使用 · F 食用";
                return;
            }
            var item = slot.item;
            string how;
            if (PBV.Player.ToolManager.ToolTypeFromItemId(item.id).HasValue) how = "左键使用工具";
            else if (PBV.Systems.FertilizerSystem.IsFertilizer(item)) how = "左键对空地施肥";
            else if (item.type == ItemType.Seed) how = "左键对已耕地播种";
            else if (item.type == ItemType.Consumable) how = "F 或左键食用";
            else if (PBV.Systems.StoragePlacement.IsChestItem(item)) how = "左键进入放置模式";
            else how = "材料 · 拿到对应工坊制作";
            _selectedHintText.text = $"当前：{item.itemName} ×{slot.count}  ·  {how}";
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
            // 常驻快捷栏也要高亮，否则按数字键看不出选中了哪一格
            for (int i = 0; i < _hotbarBorders.Length; i++)
            {
                if (_hotbarBorders[i] != null)
                {
                    _hotbarBorders[i].color = (i == SelectedIndex)
                        ? new Color(0.62f, 0.48f, 0.12f, 0.95f)
                        : new Color(0f, 0f, 0f, 0.4f);
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

        private void RefreshMoney(int song, int rmb)
        {
            if (_moneyText != null) _moneyText.text = $"宋 {song}文   ¥{rmb}";
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

            // 完整面板：只显示已解锁的格子，扩容后可见格子直接变多
            int capacity = Mathf.Clamp(inventory.Capacity, 0, MaxSlots);
            _lastCapacity = capacity;
            int rows = Mathf.CeilToInt((float)capacity / Columns);
            if (_gridRect != null) _gridRect.sizeDelta = new Vector2(0, rows * 70f + 8f);
            if (_panelRect != null) _panelRect.sizeDelta = new Vector2(488f, rows * 70f + 130f);
            if (_titleText != null) _titleText.text = $"背包 {capacity}/{MaxSlots} 格 (Tab 开关)";

            for (int i = 0; i < _slotIcons.Length; i++)
            {
                bool visible = i < capacity;
                if (_slotGos[i] != null) _slotGos[i].SetActive(visible);
                if (visible) ApplySlotVisual(inventory.GetSlot(i), _slotIcons[i], _slotCounts[i]);
            }

            UpdateInfoBar();
            UpdateSelectedHint();
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
            canvas.sortingOrder = 15; // 常驻快捷栏不压在功能面板（23~30）之上
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
            bg.raycastTarget = false; // 快捷栏底板纯展示，点击交给格子上的 Button

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
                CreateSlot(hotbarGo.transform, i, 56f, out _hotbarIcons[i], out _hotbarCounts[i], out _hotbarBorders[i]);
                // 数字键角标：1-9、0，让玩家看清按哪个键调用哪一格
                var keyLabel = CreateTextObj("KeyLabel", _hotbarBorders[i].transform,
                    (i == 9 ? "0" : (i + 1).ToString()), 13, FontStyle.Bold, new Color(1f, 0.9f, 0.55f, 0.95f));
                var kr = keyLabel.GetComponent<RectTransform>();
                kr.anchorMin = new Vector2(0f, 1f); kr.anchorMax = new Vector2(0f, 1f);
                kr.pivot = new Vector2(0f, 1f);
                kr.anchoredPosition = new Vector2(3f, -1f);
                kr.sizeDelta = new Vector2(20f, 18f);
                keyLabel.alignment = TextAnchor.UpperLeft;
            }

            // 快捷栏上方常驻一行小字：当前选中物品 + 用法（不随 Tab 面板隐藏）
            _selectedHintText = CreateTextObj("SelectedHint", canvasTransform, string.Empty,
                14, FontStyle.Bold, new Color(1f, 0.92f, 0.72f));
            var hintRect = _selectedHintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f); hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 76f);
            hintRect.sizeDelta = new Vector2(720f, 20f);
            _selectedHintText.alignment = TextAnchor.MiddleCenter;
            UpdateSelectedHint();
        }

        private void BuildFullPanel(Transform canvasTransform)
        {
            _panel = new GameObject("InventoryPanel");
            _panel.transform.SetParent(canvasTransform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            _panelRect = panelRect;
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

            _titleText = CreateTextObj("Title", titleBar.transform, "背包 (Tab 开关)", 22, FontStyle.Bold, new Color(1f, 0.93f, 0.75f));
            _titleText.alignment = TextAnchor.MiddleLeft;

            _moneyText = CreateTextObj("Money", titleBar.transform, "宋 0文   ¥0", 22, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));
            _moneyText.alignment = TextAnchor.MiddleRight;

            // ② 格子网格（GridLayoutGroup 自动排列 6 列 x 4 行）
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(_panel.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            _gridRect = gridRect;
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

            // 预建 48 格，按当前容量决定显示多少（升级后格子会真实变多）
            for (int i = 0; i < MaxSlots; i++)
            {
                int slotIndex = i;
                CreateSlot(gridGo.transform, slotIndex, 64f, out _slotIcons[slotIndex], out _slotCounts[slotIndex], out _slotBorders[slotIndex]);
                _slotGos[slotIndex] = _slotBorders[slotIndex].gameObject;

                // Button 挂在 slot 主 Image(border) 所在 GameObject 上，拦截点击
                var button = _slotGos[slotIndex].AddComponent<Button>();
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(() => SelectSlot(slotIndex));
                _slotButtons[slotIndex] = button;
                _slotGos[slotIndex].SetActive(false); // 由 RefreshAll 按容量启用
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