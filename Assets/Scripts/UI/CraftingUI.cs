using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Inventory;

namespace PBV.UI
{
    /// <summary>
    /// 制作 UI：配方列表 + 制作按钮。C 键开关。
    /// 灰盒自建。
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        public static CraftingUI Instance { get; private set; }

        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private Transform _listContent;
        private bool _isOpen;
        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
        private static Font _font;

        /// <summary>当前只看哪个建筑的配方；null = 全部配方总览（C 键）</summary>
        private string _stationFilter;
        private string _stationTitle = "全部制作配方";
        private Text _titleText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Start()
        {
            if (CraftingSystem.Instance != null)
            {
                CraftingSystem.Instance.OnRecipesResolved += Refresh;
            }
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C) || (_isOpen && Input.GetKeyDown(KeyCode.Escape)))
            {
                Toggle();
            }
        }

        public bool IsOpen => _isOpen;

        /// <summary>C 键：全部配方总览（按建筑标注，方便查找去哪个台做）</summary>
        public void Toggle()
        {
            if (_isOpen) { Close(); return; }
            OpenFor(null);
        }

        /// <summary>
        /// 由具体制作台按 E 调用：只显示该建筑的配方，标题显示台名。
        /// stationId 传 null 表示全部配方总览。
        /// </summary>
        public void OpenFor(string stationId)
        {
            _stationFilter = stationId;
            _stationTitle = string.IsNullOrEmpty(stationId)
                ? "全部制作配方（总览）"
                : $"{CraftingSystem.StationName(stationId)} · 制作配方";
            _isOpen = true;
            _canvasGo.SetActive(true);
            if (_titleText != null) _titleText.text = $"{_stationTitle}  (C/Esc 关闭)";
            UIEventSystemBootstrap.RegisterTopPanel(Close);
            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            if (CraftingSystem.Instance == null || _listContent == null) return;
            // 打开面板/背包变化时重试解析，避免 Start 时序过早导致配方引用为空、按钮一直灰
            CraftingSystem.Instance.ResolveReferences();

            // 清空
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);
            _buttons.Clear();

            int shown = 0;
            foreach (var r in CraftingSystem.Instance.Recipes)
            {
                if (!string.IsNullOrEmpty(_stationFilter) && r.stationId != _stationFilter) continue;
                CreateRecipeRow(r);
                shown++;
            }

            if (shown == 0)
                CreateEmptyRow();
        }

        /// <summary>该建筑暂无配方时的占位提示</summary>
        private void CreateEmptyRow()
        {
            var go = new GameObject("EmptyHint");
            go.transform.SetParent(_listContent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
            var t = go.AddComponent<Text>();
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = _font; t.fontSize = 15; t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.75f, 0.7f, 0.62f);
            t.text = "此台暂无制作配方 · 按 C 查看全部配方总览";
            t.raycastTarget = false;
        }

        private void CreateRecipeRow(Recipe r)
        {
            var row = new GameObject($"Recipe_{r.id}");
            row.transform.SetParent(_listContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);

            var img = row.AddComponent<Image>();
            bool canCraft = CraftingSystem.Instance.CanCraft(r);
            img.color = canCraft ? new Color(0.15f, 0.18f, 0.12f, 0.95f) : new Color(0.12f, 0.1f, 0.1f, 0.8f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;

            // 信息区
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(row.transform, false);
            var infoRt = infoGo.AddComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(380, 68);
            var infoText = infoGo.AddComponent<Text>();
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.font = _font;
            infoText.fontSize = 14;
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.color = canCraft ? new Color(1f, 0.92f, 0.75f) : new Color(0.6f, 0.55f, 0.5f);
            infoText.supportRichText = true;

            // 材料文本
            string mats = "";
            for (int i = 0; i < r.inputItemIds.Length; i++)
            {
                string itemName = r._inputs != null && i < r._inputs.Length && r._inputs[i] != null ? r._inputs[i].itemName : r.inputItemIds[i];
                int have = r._inputs != null && i < r._inputs.Length && r._inputs[i] != null && InventorySystem.Instance != null
                    ? InventorySystem.Instance.CountItem(r._inputs[i]) : 0;
                bool enough = have >= r.inputCounts[i];
                mats += $"{(enough ? "" : "<color=#cc6666>")}{itemName} {have}/{r.inputCounts[i]}{(enough ? "" : "</color>")}  ";
            }
            string outName = r._output != null ? r._output.itemName : r.outputItemId;
            // 总览模式下标注该配方归属哪个建筑，方便玩家知道去哪个台制作
            string stationTag = string.IsNullOrEmpty(_stationFilter)
                ? $"<color=#8ab4d8>[{CraftingSystem.StationName(r.stationId)}]</color> "
                : "";
            infoText.text = $"<b>{stationTag}{r.name}</b>\n{mats}\n→ {outName} x{r.outputCount}";
            infoText.raycastTarget = false;

            // 制作按钮
            var btnGo = new GameObject("CraftBtn");
            btnGo.transform.SetParent(row.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(80, 56);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = canCraft ? new Color(0.25f, 0.5f, 0.3f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.6f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            // 常可点：材料不足时 CraftingSystem.Craft 会弹出具体缺什么
            btn.interactable = true;

            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = _font;
            btnText.fontSize = 16;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "制作";
            btnText.raycastTarget = false;

            var captured = r;
            btn.onClick.AddListener(() => { CraftingSystem.Instance.Craft(captured); Refresh(); });
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("CraftingCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 半透明背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.onClick.AddListener(Toggle);

            // 面板
            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var panelRt = _root.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520, 420);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = _root.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.09f, 0.07f, 0.96f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            // 标题（动态：按当前制作台名显示，不再写死「造纸台」）
            _titleText = CreateText(_root.transform, "Title", 22, FontStyle.Bold, new Color(1f, 0.9f, 0.6f), "全部制作配方 (C/Esc 关闭)");
            var titleRt = _titleText.rectTransform;
            titleRt.sizeDelta = new Vector2(0, 28);

            // 滚动列表
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0, 360);
            scrollGo.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.5f);
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 0);
            var cvlg = content.AddComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(4, 4, 4, 4);
            cvlg.spacing = 4;
            cvlg.childControlWidth = true;
            cvlg.childForceExpandWidth = true;
            var cf = content.AddComponent<ContentSizeFitter>();
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = contentRt;
            _listContent = content.transform;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = _font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.UpperLeft;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
