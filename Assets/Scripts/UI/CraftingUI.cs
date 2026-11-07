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
        private Transform _listContent;
        private bool _isOpen;
        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _root.SetActive(false);
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

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _root.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            if (CraftingSystem.Instance == null || _listContent == null) return;

            // 清空
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);
            _buttons.Clear();

            foreach (var r in CraftingSystem.Instance.Recipes)
            {
                CreateRecipeRow(r);
            }
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
            infoText.text = $"<b>{r.name}</b>\n{mats}\n→ {outName} x{r.outputCount}";
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
            btn.interactable = canCraft;

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

            if (canCraft)
            {
                var captured = r;
                btn.onClick.AddListener(() => { CraftingSystem.Instance.Craft(captured); Refresh(); });
            }
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("CraftingCanvas");
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

            // 标题
            var title = CreateText(_root.transform, "Title", 22, FontStyle.Bold, new Color(1f, 0.9f, 0.6f), "造纸台 (C/Esc 关闭)");
            var titleRt = title.rectTransform;
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
