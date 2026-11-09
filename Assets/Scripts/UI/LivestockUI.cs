using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>养殖 UI：展示牲畜数量，购买/喂食/屠宰。</summary>
    public class LivestockUI : MonoBehaviour
    {
        public static LivestockUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private static Font _font;
        private Transform _listRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
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
            if (_listRoot == null) return;
            // 清空旧行
            foreach (Transform c in _listRoot) Destroy(c.gameObject);

            var sys = LivestockSystem.Instance;
            foreach (var def in LivestockSystem.Defs)
            {
                CreateRow(def);
            }
        }

        private void CreateRow(LivestockDef def)
        {
            var sys = LivestockSystem.Instance;
            var owned = sys != null ? sys.GetOwnedCount(def.type) : 0;

            var row = new GameObject("Row_" + def.name);
            row.transform.SetParent(_listRoot, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 52);

            // 名称 + 数量
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0); labelRt.anchorMax = new Vector2(0.5f, 1);
            labelRt.offsetMin = new Vector2(8, 0); labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.font = _font; label.fontSize = 18; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft; label.color = new Color(1f, 0.9f, 0.7f);
            label.raycastTarget = false;
            string productName = def.productId != null ? "产" + ItemName(def.productId) : "养肥屠宰";
            label.text = $"{def.name} ×{owned}　<size=12>{productName}</size>";
            label.supportRichText = true;

            // 三个按钮：买 / 喂 / 宰
            CreateButton(row.transform, "Buy", 0.5f, 0.72f, "买 " + def.buyCost + "文", new Color(0.3f, 0.5f, 0.3f),
                () => { sys?.Buy(def.type); Refresh(); });
            CreateButton(row.transform, "Feed", 0.72f, 0.86f, "喂", new Color(0.5f, 0.4f, 0.2f),
                () => { sys?.Feed(def.type); Refresh(); });
            CreateButton(row.transform, "Slaughter", 0.86f, 1f, "宰", new Color(0.6f, 0.25f, 0.2f),
                () => { sys?.Slaughter(def.type); Refresh(); });
        }

        private void CreateButton(Transform parent, string name, float minX, float maxX, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(minX, 0); rt.anchorMax = new Vector2(maxX, 1);
            rt.offsetMin = new Vector2(2, 4); rt.offsetMax = new Vector2(-2, -4);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var tgo = new GameObject("Text");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = tgo.AddComponent<Text>();
            t.font = _font; t.fontSize = 14; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.text = text;
            t.raycastTarget = false;
        }

        private static string ItemName(string id)
        {
            foreach (var item in Resources.LoadAll<PBV.Inventory.ItemData>("Data"))
                if (item != null && item.id == id) return item.itemName;
            return id;
        }

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("LivestockCanvas");
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
            bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Close);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(520, 420); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.1f, 0.09f, 0.07f, 0.96f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            title.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
            var tt = title.AddComponent<Text>();
            tt.font = _font; tt.fontSize = 22; tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleCenter; tt.color = new Color(0.8f, 0.7f, 0.5f);
            tt.text = "畜 栏（E/Esc 关闭）"; tt.raycastTarget = false;

            var hint = new GameObject("Hint");
            hint.transform.SetParent(panel.transform, false);
            hint.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            var ht = hint.AddComponent<Text>();
            ht.font = _font; ht.fontSize = 13;
            ht.alignment = TextAnchor.MiddleCenter; ht.color = new Color(0.7f, 0.65f, 0.6f);
            ht.text = "买牲畜 → 每天喂食 → 明早收蛋奶；连续喂 2 天再屠宰多得 1 块肉";
            ht.raycastTarget = false;

            _listRoot = new GameObject("List").transform;
            _listRoot.SetParent(panel.transform, false);
            var lrt = _listRoot.gameObject.AddComponent<RectTransform>();
            lrt.sizeDelta = new Vector2(0, 300);
            var lvg = _listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            lvg.spacing = 6; lvg.childControlWidth = true; lvg.childForceExpandWidth = true;
            lvg.childControlHeight = true; lvg.childForceExpandHeight = true;
        }
    }
}
