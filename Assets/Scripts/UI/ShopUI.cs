using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBV.Economy;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 商店 UI：商品列表 + 购买按钮 + 金钱显示 + 关闭。
    /// 灰盒自建（无 Prefab 依赖），B 键快捷开关。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Layout")]
        [SerializeField] private float _rowHeight = 64f;

        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private GameObject _listContent;
        private Text _titleText;
        private Text _moneyText;
        private ShopSystem _currentShop;
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CloseShop();
            }
        }

        private static Font Font => _font ?? (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public void OpenShop(ShopSystem shop)
        {
            _currentShop = shop;
            _isOpen = true;
            _canvasGo.SetActive(true);
            Refresh();
        }

        public void CloseShop()
        {
            _isOpen = false;
            _currentShop = null;
            _canvasGo.SetActive(false);
        }

        private void Refresh()
        {
            if (_currentShop == null) return;

            if (_titleText != null) _titleText.text = _currentShop.ShopName;
            int money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
            if (_moneyText != null) _moneyText.text = $"¥ {money}";

            // 清空列表
            for (int i = _listContent.transform.childCount - 1; i >= 0; i--)
                Destroy(_listContent.transform.GetChild(i).gameObject);

            foreach (var entry in _currentShop.Items)
            {
                if (entry == null || entry.item == null) continue;
                CreateRow(entry);
            }
        }

        private void CreateRow(ShopEntry entry)
        {
            var row = new GameObject($"Row_{entry.item.itemName}");
            row.transform.SetParent(_listContent.transform, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, _rowHeight);

            var img = row.AddComponent<Image>();
            img.color = new Color(0.18f, 0.16f, 0.12f, 0.95f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 图标
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(48, 48);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.4f, 0.35f, 0.25f, 1f);
            if (entry.item.icon != null)
            {
                iconImg.sprite = entry.item.icon;
                iconImg.preserveAspect = true;
            }

            // 名称+描述
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(row.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(260, _rowHeight - 8);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Font;
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = new Color(1f, 0.95f, 0.8f);
            nameText.text = $"{entry.item.itemName}\n<size=11>{entry.item.description}</size>";
            nameText.supportRichText = true;

            // 价格
            var priceGo = new GameObject("Price");
            priceGo.transform.SetParent(row.transform, false);
            var priceRt = priceGo.AddComponent<RectTransform>();
            priceRt.sizeDelta = new Vector2(80, _rowHeight - 8);
            var priceText = priceGo.AddComponent<Text>();
            priceText.font = Font;
            priceText.fontSize = 18;
            priceText.alignment = TextAnchor.MiddleRight;
            priceText.color = new Color(1f, 0.85f, 0.4f);
            priceText.text = $"¥{entry.price}";

            // 购买按钮
            var btnGo = new GameObject("BuyBtn");
            btnGo.transform.SetParent(row.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(80, _rowHeight - 16);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.5f, 0.3f, 1f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = Font;
            btnText.fontSize = 16;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "购买";
            btnText.raycastTarget = false;

            int price = entry.price;
            ItemData item = entry.item;
            btn.onClick.AddListener(() => TryBuy(item, price));
        }

        private void TryBuy(ItemData item, int price)
        {
            if (PlayerWallet.Instance == null || InventorySystem.Instance == null) return;
            if (!PlayerWallet.Instance.TrySpend(price))
            {
                Debug.Log("[Shop] 金钱不足");
                return;
            }
            if (!InventorySystem.Instance.AddItem(item, 1))
            {
                // 背包满，退款
                PlayerWallet.Instance.AddMoney(price);
                Debug.Log("[Shop] 背包已满");
                return;
            }
            Debug.Log($"[Shop] 购买 {item.itemName} -¥{price}");
            Refresh();
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("ShopCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            ((CanvasScaler)canvasGo.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 半透明背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.5f);
            var bgBtn = bg.AddComponent<Button>();
            bgBtn.onClick.AddListener(CloseShop);

            // 面板
            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var panelRt = _root.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560, 520);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = _root.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.1f, 0.08f, 0.96f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 标题栏（标题 + 金钱 + 关闭提示）
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(_root.transform, false);
            var headerRt = headerGo.AddComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(0, 40);
            var headerImg = headerGo.AddComponent<Image>();
            headerImg.color = new Color(0.2f, 0.17f, 0.13f, 1f);
            var hlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;

            _titleText = CreateText(headerGo.transform, "Title", 22, FontStyle.Bold, new Color(1f, 0.9f, 0.6f), "杂货铺");
            var titleRt = _titleText.rectTransform;
            titleRt.sizeDelta = new Vector2(0, 40);
            var fitter = headerGo.AddComponent<ContentSizeFitter>();

            _moneyText = CreateText(headerGo.transform, "Money", 20, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "¥ 0");
            var moneyRt = _moneyText.rectTransform;
            moneyRt.sizeDelta = new Vector2(120, 40);

            // 提示行
            var hint = CreateText(_root.transform, "Hint", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.55f), "点击购买 · B/Esc 关闭");
            var hintRt = hint.rectTransform;
            hintRt.sizeDelta = new Vector2(0, 16);

            // 列表容器（带滚动）
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0, 420);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0.08f, 0.07f, 0.05f, 0.5f);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            var vpMask = viewportGo.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRt;

            _listContent = new GameObject("Content");
            _listContent.transform.SetParent(viewportGo.transform, false);
            var contentRt = _listContent.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.sizeDelta = new Vector2(0, 0);
            contentRt.pivot = new Vector2(0.5f, 1f);
            var vlg2 = _listContent.AddComponent<VerticalLayoutGroup>();
            vlg2.padding = new RectOffset(4, 4, 4, 4);
            vlg2.spacing = 4;
            vlg2.childControlWidth = true;
            vlg2.childForceExpandWidth = true;
            vlg2.childForceExpandHeight = false;
            var cf = _listContent.AddComponent<ContentSizeFitter>();
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
