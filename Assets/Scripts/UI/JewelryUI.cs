using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 金银细工台面板（JewelryStation · E）：金矿冶炼成金锭、金锭 + 宝石镶嵌成花丝珠宝。
    /// 这是「金银细工 / 花丝镶嵌」非遗产业链的落地。
    /// </summary>
    public class JewelryUI : MonoBehaviour
    {
        public static JewelryUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private static Font _font;

        private Text _ownText;
        private Text _hintText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
                Close();
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
            _isOpen = false;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            if (_ownText != null)
            {
                var inv = InventorySystem.Instance;
                int gold = Count(inv, "item_ore_gold");
                int gem = Count(inv, "item_ore_gem");
                int ingot = Count(inv, "item_gold_ingot");
                int jewel = Count(inv, "item_jewelry");
                _ownText.text = $"持有 · 金矿×{gold}  宝石×{gem}  金锭×{ingot}  珠宝×{jewel}";
            }
        }

        private static int Count(InventorySystem inv, string id)
        {
            if (inv == null) return 0;
            var item = Find(id);
            return item != null ? inv.CountItem(item) : 0;
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        // 配方1：金矿 ×3 → 金锭 ×1
        private void Smelt()
        {
            var inv = InventorySystem.Instance;
            var gold = Find("item_ore_gold");
            var ingot = Find("item_gold_ingot");
            if (inv == null || gold == null || ingot == null) return;
            if (inv.CountItem(gold) < 3)
            {
                HUD.Instance?.ShowNotice("金矿不足：冶炼 1 金锭需 3 个金矿");
                return;
            }
            inv.RemoveItem(gold, 3);
            if (!inv.AddItem(ingot, 1))
            {
                inv.AddItem(gold, 3);
                HUD.Instance?.ShowNotice("背包已满，金锭放不下（金矿已退还）");
                return;
            }
            HUD.Instance?.ShowNotice("冶炼成功：金矿×3 → 金锭×1（可镶嵌宝石）");
            Refresh();
        }

        // 配方2：金锭 ×1 + 宝石 ×1 → 花丝珠宝 ×1
        private void Inlay()
        {
            var inv = InventorySystem.Instance;
            var ingot = Find("item_gold_ingot");
            var gem = Find("item_ore_gem");
            var jewel = Find("item_jewelry");
            if (inv == null || ingot == null || gem == null || jewel == null) return;
            if (inv.CountItem(ingot) < 1 || inv.CountItem(gem) < 1)
            {
                HUD.Instance?.ShowNotice("材料不足：镶嵌 1 件珠宝需 金锭×1 + 宝石×1");
                return;
            }
            inv.RemoveItem(ingot, 1);
            inv.RemoveItem(gem, 1);
            if (!inv.AddItem(jewel, 1))
            {
                inv.AddItem(ingot, 1);
                inv.AddItem(gem, 1);
                HUD.Instance?.ShowNotice("背包已满，珠宝放不下（材料已退还）");
                return;
            }
            HUD.Instance?.ShowNotice("镶嵌成功：金锭+宝石 → 花丝珠宝×1（高价·送礼佳）");
            Refresh();
        }

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("JewelryCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 33;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            bg.AddComponent<Button>().onClick.AddListener(Close);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(460, 340); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.14f, 0.11f, 0.06f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var title = CreateText(panel.transform, "Title", 22, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "金银细工台");
            title.alignment = TextAnchor.MiddleCenter;

            _ownText = CreateText(panel.transform, "Own", 14, FontStyle.Normal, new Color(0.9f, 0.9f, 0.7f), "持有 · 金矿×0  宝石×0  金锭×0  珠宝×0");
            _ownText.alignment = TextAnchor.MiddleCenter;

            AddButton(panel.transform, "冶炼：金矿×3 → 金锭×1", new Color(0.55f, 0.45f, 0.2f, 1f), Smelt);
            AddButton(panel.transform, "镶嵌：金锭×1 + 宝石×1 → 花丝珠宝×1", new Color(0.7f, 0.55f, 0.15f, 1f), Inlay);

            _hintText = CreateText(panel.transform, "Hint", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.6f), "金矿在矿洞深处挖取 · E/Esc 关闭");
            _hintText.alignment = TextAnchor.MiddleCenter;
        }

        private void AddButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(parent, false);
            btnGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 46);
            var img = btnGo.AddComponent<Image>();
            img.color = color;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var tGo = new GameObject("Text");
            tGo.transform.SetParent(btnGo.transform, false);
            var tRect = tGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;
            var t = tGo.AddComponent<Text>();
            t.font = _font; t.fontSize = 15; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white; t.text = label; t.raycastTarget = false;
            btn.onClick.AddListener(action);
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft; t.color = color; t.text = content; t.raycastTarget = false;
            return t;
        }
    }
}
