using UnityEngine;
using UnityEngine.UI;
using PBV.Economy;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 商店 UI：上半「买」（折后价）、下半「卖」（当场结算）。
    /// - 营业时间内才能交易；打烊后按钮禁用，引导去出货箱次日结算。
    /// - 买价 / 卖价随商人好感浮动，状态栏常驻显示。
    /// - B / Esc / E 关闭。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("Layout")]
        [SerializeField] private float _rowHeight = 60f;

        private GameObject _canvasGo;
        private GameObject _listContent;
        private Text _titleText;
        private Text _moneyText;
        private Text _statusText;
        private ShopSystem _currentShop;
        private bool _isOpen;
        private bool _lastCanTrade;
        private static Font _font;

        private static Font Font => _font ?? (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
                return;
            }
            // 营业状态跨小时变化时立即刷新（打烊瞬间禁用按钮）
            if (_currentShop != null && _currentShop.CanTrade != _lastCanTrade) Refresh();
        }

        public void OpenShop(ShopSystem shop)
        {
            _currentShop = shop;
            _isOpen = true;
            _canvasGo.SetActive(true);
            UIEventSystemBootstrap.RegisterTopPanel(CloseShop);
            Refresh();
        }

        /// <summary>面板开着时外部（如送礼涨好感）触发刷新</summary>
        public void RefreshIfOpen() { if (_isOpen) Refresh(); }

        public void CloseShop()
        {
            _isOpen = false;
            _currentShop = null;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            if (_currentShop == null || _listContent == null) return;
            var shop = _currentShop;
            bool canTrade = shop.CanTrade;
            _lastCanTrade = canTrade;

            bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            if (_titleText != null) _titleText.text = shop.ShopName + (modern ? "（现代·¥）" : "（桃花谷·文）");
            var w = PlayerWallet.Instance;
            if (_moneyText != null && w != null) _moneyText.text = $"宋 {w.SongCoins}文   ¥{w.RMB}";

            int affection = shop.Affection;
            string state = canTrade
                ? $"<color=#8fd48f>营业中 {shop.OpenHour:00}:00-{shop.CloseHour:00}:00</color>"
                : $"<color=#e58a8a>已打烊 · 请投出货箱，次日 06:00 结算</color>";
            if (_statusText != null)
                _statusText.text = $"{state}    {shop.MerchantName}[{shop.AffectionLevelName} {affection}]    " +
                                   $"买 <b>{shop.BuyMultiplier * 10f:0.0}折</b> / 卖 <b>+{Mathf.RoundToInt((shop.SellMultiplier - 1f) * 100f)}%</b>（当场结算）";

            for (int i = _listContent.transform.childCount - 1; i >= 0; i--)
                Destroy(_listContent.transform.GetChild(i).gameObject);

            CreateSectionTitle("── 购买（好感越高越便宜） ──");
            var items = shop.Items;
            if (items != null)
                foreach (var entry in items)
                    if (entry != null && entry.item != null) CreateBuyRow(shop, entry, canTrade);

            CreateSectionTitle("── 出售（当场结算 · 出货箱才享现代×2 / 节日×1.5） ──");
            var inv = InventorySystem.Instance;
            int sellable = 0;
            if (inv != null)
            {
                for (int i = 0; i < inv.Capacity; i++)
                {
                    var slot = inv.GetSlot(i);
                    if (slot == null || slot.IsEmpty || !ShippingBinSystem.CanSell(slot.item)) continue;
                    CreateSellRow(shop, slot.item, slot.count, canTrade);
                    sellable++;
                }
            }
            if (sellable == 0) CreateEmptyRow("背包里暂无可卖物品（工具 / 种子 / 无售价材料不收）");
        }

        private void CreateSectionTitle(string text)
        {
            var go = new GameObject("Section");
            go.transform.SetParent(_listContent.transform, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = 14; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft; t.color = new Color(0.85f, 0.78f, 0.55f);
            t.text = text; t.raycastTarget = false;
        }

        private void CreateEmptyRow(string text)
        {
            var go = new GameObject("Empty");
            go.transform.SetParent(_listContent.transform, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = 13; t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.65f, 0.62f, 0.58f); t.text = "    " + text; t.raycastTarget = false;
        }

        private GameObject MakeRow(string name, float height)
        {
            var row = new GameObject(name);
            row.transform.SetParent(_listContent.transform, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.12f, 0.95f);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            return row;
        }

        private static Text MakeText(Transform parent, string name, string content, float width, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 44);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = fontSize; t.alignment = align; t.color = color;
            t.text = content; t.supportRichText = true; t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, float width, Color color, UnityEngine.Events.UnityAction onClick, bool enabled)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 40);
            var img = go.AddComponent<Image>();
            img.color = enabled ? color : new Color(0.32f, 0.3f, 0.28f, 0.7f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = enabled;
            btn.onClick.AddListener(onClick);
            var tgo = new GameObject("Text");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = tgo.AddComponent<Text>();
            t.font = Font; t.fontSize = 15; t.alignment = TextAnchor.MiddleCenter;
            t.color = enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            t.text = label; t.raycastTarget = false;
            return btn;
        }

        private void CreateBuyRow(ShopSystem shop, ShopEntry entry, bool canTrade)
        {
            var row = MakeRow($"Buy_{entry.item.itemName}", _rowHeight);
            int price = shop.GetBuyPrice(entry);
            int basePrice = entry.price;
            string sym = entry.currency == Currency.RMB ? "¥" : "";
            string unit = entry.currency == Currency.RMB ? "" : " 文";
            string discount = price < basePrice ? $"\n<size=10><color=#8f8f7f>原价 {sym}{basePrice}</color></size>" : "";
            MakeText(row.transform, "Name", $"{entry.item.itemName}\n<size=11>{entry.item.description}</size>", 250, 15, new Color(1f, 0.95f, 0.8f));
            MakeText(row.transform, "Price", $"<b>{sym}{price}{unit}</b>{discount}", 92, 16, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight);
            MakeButton(row.transform, "BuyBtn", "购买", 80, new Color(0.25f, 0.5f, 0.3f, 1f),
                () => TryBuy(entry), canTrade);
        }

        private void CreateSellRow(ShopSystem shop, ItemData item, int count, bool canTrade)
        {
            var row = MakeRow($"Sell_{item.itemName}", _rowHeight);
            bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            string sym = modern ? "¥" : "";
            int unit = shop.GetSellUnitPrice(item);
            MakeText(row.transform, "Name", $"{item.itemName}  ×{count}", 232, 15, new Color(1f, 0.95f, 0.8f));
            MakeText(row.transform, "Price", $"{sym}{unit} / 件", 100, 14, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight);
            MakeButton(row.transform, "SellOne", "卖 1", 62, new Color(0.5f, 0.34f, 0.18f, 1f),
                () => { shop.TrySell(item, 1); Refresh(); }, canTrade);
            MakeButton(row.transform, "SellAll", "全卖", 62, new Color(0.55f, 0.28f, 0.2f, 1f),
                () => { shop.TrySell(item, count); Refresh(); }, canTrade);
        }

        private void TryBuy(ShopEntry entry)
        {
            var shop = _currentShop;
            if (shop == null || !shop.CanTrade)
            {
                HUD.Instance?.ShowNotice($"商店已打烊（营业 {shop?.OpenHour:00}:00-{shop?.CloseHour:00}:00）");
                return;
            }
            var wallet = PlayerWallet.Instance;
            var inv = InventorySystem.Instance;
            if (wallet == null || inv == null) return;

            int price = shop.GetBuyPrice(entry);
            bool isRMB = entry.currency == Currency.RMB;
            bool paid = isRMB ? wallet.SpendRMB(price) : wallet.SpendSong(price);
            if (!paid)
            {
                HUD.Instance?.ShowNotice(isRMB
                    ? $"人民币不足：{entry.item.itemName} 需要 ¥{price}"
                    : $"宋币不足：{entry.item.itemName} 需要 {price} 文");
                return;
            }
            if (!inv.AddItem(entry.item, 1))
            {
                if (isRMB) wallet.AddRMB(price); else wallet.AddSong(price);
                HUD.Instance?.ShowNotice("背包已满，购买取消（已退款）");
                return;
            }
            string sym = isRMB ? "¥" : "";
            string unit = isRMB ? "" : " 文";
            HUD.Instance?.ShowNotice($"购买 {entry.item.itemName} ×1  -{sym}{price}{unit}（{shop.MerchantName}好感 {shop.BuyMultiplier * 10f:0.0}折）");
            Refresh();
        }

        private void TrySellAll()
        {
            if (_currentShop == null) return;
            _currentShop.TrySellAll(out _, out _);
            Refresh();
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("ShopCanvas");
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
            bg.AddComponent<Button>().onClick.AddListener(CloseShop);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620, 560); panelRt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.08f, 0.96f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            // 标题栏
            var header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            header.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            header.AddComponent<Image>().color = new Color(0.2f, 0.17f, 0.13f, 1f);
            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            _titleText = CreateText(header.transform, "Title", 22, FontStyle.Bold, new Color(1f, 0.9f, 0.6f), "杂货铺");
            _titleText.rectTransform.sizeDelta = new Vector2(0, 40);
            _moneyText = CreateText(header.transform, "Money", 20, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "¥ 0");
            _moneyText.rectTransform.sizeDelta = new Vector2(200, 40);
            _moneyText.alignment = TextAnchor.MiddleRight;

            // 状态栏：营业时间 / 好感 / 买折扣 / 卖溢价
            _statusText = CreateText(panel.transform, "Status", 13, FontStyle.Normal, new Color(0.9f, 0.88f, 0.8f), "");
            _statusText.rectTransform.sizeDelta = new Vector2(0, 22);
            _statusText.supportRichText = true;

            var hint = CreateText(panel.transform, "Hint", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.55f),
                "买卖当场结算 · G 给掌柜送礼涨好感 · B / E / Esc 关闭");
            hint.rectTransform.sizeDelta = new Vector2(0, 16);

            // 一键全部卖出
            var sellGo = new GameObject("SellAllBtn");
            sellGo.transform.SetParent(panel.transform, false);
            sellGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
            sellGo.AddComponent<Image>().color = new Color(0.45f, 0.3f, 0.18f, 1f);
            var sellBtn = sellGo.AddComponent<Button>();
            sellBtn.onClick.AddListener(TrySellAll);
            var stgo = new GameObject("Text");
            stgo.transform.SetParent(sellGo.transform, false);
            var strt = stgo.AddComponent<RectTransform>();
            strt.anchorMin = Vector2.zero; strt.anchorMax = Vector2.one; strt.sizeDelta = Vector2.zero;
            var st = stgo.AddComponent<Text>();
            st.font = Font; st.fontSize = 15; st.alignment = TextAnchor.MiddleCenter;
            st.color = Color.white; st.text = "一键全部卖出（当场结算）"; st.raycastTarget = false;

            // 列表容器
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panel.transform, false);
            scrollGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 400);
            scrollGo.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.05f, 0.5f);
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            _listContent = new GameObject("Content");
            _listContent.transform.SetParent(vp.transform, false);
            var crt = _listContent.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f); crt.sizeDelta = Vector2.zero;
            var cvlg = _listContent.AddComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(4, 4, 4, 4); cvlg.spacing = 4;
            cvlg.childControlWidth = true; cvlg.childForceExpandWidth = true;
            cvlg.childControlHeight = false; cvlg.childForceExpandHeight = false;
            var cf = _listContent.AddComponent<ContentSizeFitter>();
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = crt;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft; t.color = color;
            t.text = content; t.raycastTarget = false;
            return t;
        }
    }
}
