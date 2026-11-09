using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 屋内面板：房屋入口进入后显示。包含「储物箱」（存取物品，容量随房屋等级）与「装修」展示区。
    /// 储物箱：选中背包物品点「存入」，选中储物箱物品点「取出」。
    /// </summary>
    public class HouseInteriorUI : MonoBehaviour
    {
        public static HouseInteriorUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private Text _infoText;
        private Text _storageTitle;
        private Transform _storageGrid;
        private static Font _font;

        private const int MaxVisible = 24;
        private readonly Text[] _storageCounts = new Text[MaxVisible];
        private readonly Image[] _storageBorders = new Image[MaxVisible];
        private readonly GameObject[] _storageGos = new GameObject[MaxVisible];
        private readonly Button[] _storageButtons = new Button[MaxVisible];

        private int _selectedStorage = -1; // 选中的储物箱槽位（-1 未选）

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void Open()
        {
            _isOpen = true;
            _canvasGo.SetActive(true);
            UIEventSystemBootstrap.RegisterTopPanel(Close);
            Refresh();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            var storage = StorageSystem.Instance;
            if (storage == null) return;
            int cap = Mathf.Min(storage.Capacity, MaxVisible);
            if (_storageTitle != null)
            {
                int houseLv = BuildSystem.Instance != null ? BuildSystem.Instance.HouseLevel : 0;
                _storageTitle.text = $"储物箱（{storage.Capacity} 格 · 房屋 {houseLv} 级解锁）";
            }

            for (int i = 0; i < MaxVisible; i++)
            {
                bool visible = i < cap;
                if (_storageGos[i] != null) _storageGos[i].SetActive(visible);
                if (visible)
                {
                    var slot = storage.GetSlot(i);
                    ApplyStorageVisual(slot, i);
                }
            }
            UpdateInfoBar();
        }

        private void ApplyStorageVisual(InventorySlot slot, int index)
        {
            var icon = _storageGos[index] != null ? _storageGos[index].GetComponent<Image>() : null;
            // border 是格子主图，icon 是子物体
            var border = _storageBorders[index];
            if (border == null) return;
            var iconGo = border.transform.Find("Icon");
            var iconImg = iconGo != null ? iconGo.GetComponent<Image>() : null;

            if (slot == null || slot.IsEmpty)
            {
                if (iconImg != null) iconImg.enabled = false;
                if (_storageCounts[index] != null) _storageCounts[index].text = "";
            }
            else
            {
                if (iconImg != null) { iconImg.enabled = true; iconImg.sprite = slot.item.icon; }
                if (_storageCounts[index] != null) _storageCounts[index].text = slot.count > 1 ? slot.count.ToString() : "";
            }
        }

        private void UpdateInfoBar()
        {
            if (_infoText == null) return;
            var storage = StorageSystem.Instance;
            if (storage == null) { _infoText.text = "储物箱未解锁（扩建房屋后开启）"; return; }

            // 显示当前选中背包物品 + 选中储物箱物品提示
            var inv = InventorySystem.Instance;
            int invIdx = InventoryUI.SelectedSlotIndex;
            var invSlot = inv != null && invIdx >= 0 && invIdx < inv.Capacity ? inv.GetSlot(invIdx) : null;
            string invDesc = invSlot != null && !invSlot.IsEmpty ? $"背包选中：{invSlot.item.itemName} ×{invSlot.count}" : "背包未选中物品";

            string stoDesc = "";
            if (_selectedStorage >= 0 && _selectedStorage < storage.Capacity)
            {
                var s = storage.GetSlot(_selectedStorage);
                if (s != null && !s.IsEmpty) stoDesc = $"储物箱选中：{s.item.itemName} ×{s.count}";
            }

            _infoText.text = $"{invDesc}    {stoDesc}\n操作：选中背包物品→「存入」；选中储物箱格子→「取出」";
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("HouseInteriorCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            bg.AddComponent<Button>().onClick.AddListener(Close);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 480); rt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            // 标题
            var title = MakeText(panel.transform, "Title", "房屋 · 屋内（Esc 关闭）", 22, FontStyle.Bold, new Color(0.85f, 0.9f, 1f));
            title.rectTransform.sizeDelta = new Vector2(0, 30);

            // 储物箱标题
            _storageTitle = MakeText(panel.transform, "StorageTitle", "储物箱", 18, FontStyle.Bold, new Color(0.7f, 0.8f, 1f));
            _storageTitle.rectTransform.sizeDelta = new Vector2(0, 26);

            // 储物箱格子区（6列）
            var gridGo = new GameObject("StorageGrid");
            gridGo.transform.SetParent(panel.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(0, 200);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(60, 60);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            for (int i = 0; i < MaxVisible; i++)
            {
                int idx = i;
                var slotGo = new GameObject($"StorageSlot_{i}");
                slotGo.transform.SetParent(gridGo.transform, false);
                var srt = slotGo.AddComponent<RectTransform>();
                srt.sizeDelta = new Vector2(60, 60);
                var border = slotGo.AddComponent<Image>();
                border.color = new Color(0, 0, 0, 0.4f);
                _storageBorders[i] = border;

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform, false);
                var irect = iconGo.AddComponent<RectTransform>();
                irect.anchorMin = new Vector2(0.1f, 0.1f); irect.anchorMax = new Vector2(0.9f, 0.9f);
                irect.offsetMin = Vector2.zero; irect.offsetMax = Vector2.zero;
                var icon = iconGo.AddComponent<Image>();
                icon.preserveAspect = true; icon.raycastTarget = false;

                var count = MakeText(slotGo.transform, "Count", "", 14, FontStyle.Bold, Color.white);
                var crt = count.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.3f, 0f); crt.anchorMax = new Vector2(0.95f, 0.3f);
                crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
                count.alignment = TextAnchor.LowerRight;
                _storageCounts[i] = count;

                var btn = slotGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { _selectedStorage = idx; UpdateSelectionHighlight(); UpdateInfoBar(); });
                _storageButtons[i] = btn;
                _storageGos[i] = slotGo;
                slotGo.SetActive(false);
            }

            // 操作按钮（存入 / 取出）
            var btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(panel.transform, false);
            btnRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            var storeBtn = MakeButton(btnRow.transform, "存入储物箱", () => DoStore());
            var takeBtn = MakeButton(btnRow.transform, "取出到背包", () => DoRetrieve());

            // 信息栏
            _infoText = MakeText(panel.transform, "Info", "储物箱未解锁", 16, FontStyle.Normal, new Color(0.92f, 0.9f, 0.85f));
            _infoText.rectTransform.sizeDelta = new Vector2(0, 44);
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void DoStore()
        {
            var inv = InventorySystem.Instance;
            var storage = StorageSystem.Instance;
            if (inv == null || storage == null || storage.Capacity <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice("储物箱未解锁（扩建房屋后开启）");
                return;
            }
            int idx = InventoryUI.SelectedSlotIndex;
            var slot = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (slot == null || slot.IsEmpty)
            {
                PBV.UI.HUD.Instance?.ShowNotice("请先在背包（Tab）选中要存入的物品");
                return;
            }
            if (storage.Store(slot.item, slot.count))
            {
                inv.RemoveItem(slot.item, slot.count);
                PBV.UI.HUD.Instance?.ShowNotice($"已存入 {slot.item.itemName} ×{slot.count}");
                Refresh();
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("储物箱已满");
            }
        }

        private void DoRetrieve()
        {
            var inv = InventorySystem.Instance;
            var storage = StorageSystem.Instance;
            if (inv == null || storage == null) return;
            if (_selectedStorage < 0 || _selectedStorage >= storage.Capacity)
            {
                PBV.UI.HUD.Instance?.ShowNotice("请先点击储物箱中要取出的格子");
                return;
            }
            var s = storage.GetSlot(_selectedStorage);
            if (s == null || s.IsEmpty)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这个储物箱格子是空的");
                return;
            }
            if (inv.AddItem(s.item, s.count))
            {
                storage.Retrieve(s.item, s.count);
                PBV.UI.HUD.Instance?.ShowNotice($"已取出 {s.item.itemName} ×{s.count}");
                _selectedStorage = -1;
                Refresh();
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，取不出");
            }
        }

        private void UpdateSelectionHighlight()
        {
            for (int i = 0; i < MaxVisible; i++)
            {
                if (_storageBorders[i] != null)
                    _storageBorders[i].color = (i == _selectedStorage) ? new Color(1f, 0.85f, 0.3f) : new Color(0, 0, 0, 0.4f);
            }
        }

        private Button MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 48);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.45f, 0.6f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var txt = MakeText(go.transform, "Text", label, 16, FontStyle.Bold, Color.white);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            return btn;
        }

        private Text MakeText(Transform parent, string name, string content, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft; t.color = color; t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
