using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 单个储物箱面板：36 格存取 + 改名 + 改色。
    /// 选中背包物品点「存入」；选中箱内格子点「取出」。
    /// </summary>
    public class StorageChestUI : MonoBehaviour
    {
        public static StorageChestUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private string _currentUid;

        private Text _titleText;
        private Text _infoText;
        private Transform _grid;
        private InputField _nameInput;

        private static Font _font;

        private const int GridCols = 6;
        private const int Capacity = 36;

        private readonly Text[] _counts = new Text[Capacity];
        private readonly Image[] _borders = new Image[Capacity];
        private readonly GameObject[] _gos = new GameObject[Capacity];
        private int _selectedSlot = -1;

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

        public void Open(string uid)
        {
            _currentUid = uid;
            _selectedSlot = -1;
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

        private StorageChest CurrentChest()
        {
            if (StorageSystem.Instance == null) return null;
            return StorageSystem.Instance.GetChest(_currentUid);
        }

        private void Refresh()
        {
            var chest = CurrentChest();
            if (chest == null) { Close(); return; }

            if (_titleText != null)
                _titleText.text = $"{chest.label}（36 格 · 可改名改色）";
            if (_nameInput != null && _nameInput.text != chest.label)
                _nameInput.text = chest.label;

            for (int i = 0; i < Capacity; i++)
            {
                var slot = chest.slots[i];
                ApplyVisual(i, slot);
            }
            UpdateInfoBar();
            UpdateSelectionHighlight();
        }

        private void ApplyVisual(int index, InventorySlot slot)
        {
            var border = _borders[index];
            if (border == null) return;
            var iconImg = border.transform.Find("Icon")?.GetComponent<Image>();

            if (slot == null || slot.IsEmpty)
            {
                if (iconImg != null) iconImg.enabled = false;
                if (_counts[index] != null) _counts[index].text = "";
            }
            else
            {
                if (iconImg != null) { iconImg.enabled = true; iconImg.sprite = slot.item.icon; }
                if (_counts[index] != null) _counts[index].text = slot.count > 1 ? slot.count.ToString() : "";
            }
        }

        private void UpdateInfoBar()
        {
            if (_infoText == null) return;
            var inv = InventorySystem.Instance;
            int invIdx = InventoryUI.SelectedSlotIndex;
            var invSlot = inv != null && invIdx >= 0 && invIdx < inv.Capacity ? inv.GetSlot(invIdx) : null;
            string invDesc = invSlot != null && !invSlot.IsEmpty ? $"背包选中：{invSlot.item.itemName} ×{invSlot.count}" : "背包未选中物品";

            string stoDesc = "";
            var chest = CurrentChest();
            if (chest != null && _selectedSlot >= 0 && _selectedSlot < Capacity)
            {
                var s = chest.slots[_selectedSlot];
                if (!s.IsEmpty) stoDesc = $"箱内选中：{s.item.itemName} ×{s.count}";
            }
            _infoText.text = $"{invDesc}    {stoDesc}\n操作：选中背包物品→「存入」；选中箱内格子→「取出」";
        }

        private void UpdateSelectionHighlight()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_borders[i] != null)
                    _borders[i].color = (i == _selectedSlot) ? new Color(1f, 0.85f, 0.3f) : new Color(0, 0, 0, 0.4f);
            }
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("StorageChestCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
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
            rt.sizeDelta = new Vector2(560, 680); rt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            // 标题
            _titleText = MakeText(panel.transform, "Title", "储物箱（36 格）", 22, FontStyle.Bold, new Color(0.85f, 0.9f, 1f));
            _titleText.rectTransform.sizeDelta = new Vector2(0, 30);

            // 改名行
            var nameRow = new GameObject("NameRow");
            nameRow.transform.SetParent(panel.transform, false);
            nameRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
            var nhlg = nameRow.AddComponent<HorizontalLayoutGroup>();
            nhlg.spacing = 8; nhlg.childControlWidth = true; nhlg.childForceExpandWidth = true;

            var nameLabel = MakeText(nameRow.transform, "NameLabel", "名称", 16, FontStyle.Bold, new Color(0.7f, 0.8f, 1f));
            nameLabel.alignment = TextAnchor.MiddleLeft;
            nameLabel.rectTransform.sizeDelta = new Vector2(60, 44);

            var inputGo = new GameObject("NameInput");
            inputGo.transform.SetParent(nameRow.transform, false);
            inputGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            inputGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.13f, 1f);
            _nameInput = inputGo.AddComponent<InputField>();
            _nameInput.textComponent = MakeText(inputGo.transform, "Text", "", 18, FontStyle.Normal, Color.white);
            _nameInput.textComponent.alignment = TextAnchor.MiddleLeft;
            _nameInput.lineType = InputField.LineType.SingleLine;
            _nameInput.onEndEdit.AddListener(OnRename);

            // 改色按钮行
            var colorRow = new GameObject("ColorRow");
            colorRow.transform.SetParent(panel.transform, false);
            colorRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 48);
            var chlg = colorRow.AddComponent<HorizontalLayoutGroup>();
            chlg.spacing = 6; chlg.childControlWidth = false; chlg.childForceExpandWidth = false;

            for (int c = 0; c < StorageSystem.ChestColors.Length; c++)
            {
                int ci = c;
                var colorBtn = new GameObject($"Color_{c}");
                colorBtn.transform.SetParent(colorRow.transform, false);
                colorBtn.AddComponent<RectTransform>().sizeDelta = new Vector2(48, 40);
                var cimg = colorBtn.AddComponent<Image>();
                cimg.color = StorageSystem.ChestColors[c];
                var cbtn = colorBtn.AddComponent<Button>();
                cbtn.targetGraphic = cimg;
                cbtn.onClick.AddListener(() => OnRecolor(ci));
            }

            // 格子区
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(panel.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(0, 380);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(60, 60);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridCols;
            _grid = gridGo.transform;

            for (int i = 0; i < Capacity; i++)
            {
                int idx = i;
                var slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(_grid, false);
                slotGo.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
                var border = slotGo.AddComponent<Image>();
                border.color = new Color(0, 0, 0, 0.4f);
                _borders[i] = border;

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
                _counts[i] = count;

                var btn = slotGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { _selectedSlot = idx; UpdateSelectionHighlight(); UpdateInfoBar(); });
                _gos[i] = slotGo;
            }

            // 操作按钮
            var btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(panel.transform, false);
            btnRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;

            MakeButton(btnRow.transform, "存入", DoStore);
            MakeButton(btnRow.transform, "取出", DoRetrieve);

            // 信息栏
            _infoText = MakeText(panel.transform, "Info", "", 16, FontStyle.Normal, new Color(0.92f, 0.9f, 0.85f));
            _infoText.rectTransform.sizeDelta = new Vector2(0, 44);
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void OnRename(string newName)
        {
            var chest = CurrentChest();
            if (chest == null) return;
            if (string.IsNullOrWhiteSpace(newName)) return;
            chest.label = newName.Trim();
            var station = FindStationForChest(chest.uid);
            if (station != null) station.RefreshLabel(chest.label);
            Refresh();
            PBV.UI.HUD.Instance?.ShowNotice($"已改名为「{chest.label}」");
        }

        private void OnRecolor(int colorIndex)
        {
            var chest = CurrentChest();
            if (chest == null) return;
            chest.colorIndex = colorIndex;
            var station = FindStationForChest(chest.uid);
            if (station != null) station.RefreshVisual();
            PBV.UI.HUD.Instance?.ShowNotice("已改色");
        }

        private StorageChestStation FindStationForChest(string uid)
        {
            foreach (var st in FindObjectsOfType<StorageChestStation>())
                if (st.ChestUid == uid) return st;
            return null;
        }

        private void DoStore()
        {
            var inv = InventorySystem.Instance;
            var chest = CurrentChest();
            if (inv == null || chest == null) return;
            int idx = InventoryUI.SelectedSlotIndex;
            var slot = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (slot == null || slot.IsEmpty)
            {
                PBV.UI.HUD.Instance?.ShowNotice("请先在背包（Tab）选中要存入的物品");
                return;
            }
            if (chest.Store(slot.item, slot.count))
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
            var chest = CurrentChest();
            if (inv == null || chest == null) return;
            if (_selectedSlot < 0 || _selectedSlot >= Capacity)
            {
                PBV.UI.HUD.Instance?.ShowNotice("请先点击箱内要取出的格子");
                return;
            }
            var s = chest.slots[_selectedSlot];
            if (s.IsEmpty)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这个格子是空的");
                return;
            }
            if (inv.AddItem(s.item, s.count))
            {
                chest.Retrieve(_selectedSlot, s.count);
                PBV.UI.HUD.Instance?.ShowNotice($"已取出 {s.item.itemName} ×{s.count}");
                _selectedSlot = -1;
                Refresh();
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，取不出");
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
