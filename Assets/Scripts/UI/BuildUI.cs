using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>建造 UI：H 键开关，背包扩容+体力上限升级。</summary>
    public class BuildUI : MonoBehaviour
    {
        public static BuildUI Instance { get; private set; }

        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private Text _moneyText;
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI_();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B) || (_isOpen && Input.GetKeyDown(KeyCode.Escape))) Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(Toggle); else UIEventSystemBootstrap.ClearTopPanel();
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            var bs = BuildSystem.Instance;
            if (bs == null) return;
            int song = PBV.Systems.PlayerWallet.Instance != null ? PBV.Systems.PlayerWallet.Instance.SongCoins : 0;
            if (_moneyText != null) _moneyText.text = $"宋 {song}文";

            // 更新按钮状态
            var bagBtn = _root.transform.Find("BagRow/Btn")?.GetComponent<Button>();
            var stamBtn = _root.transform.Find("StamRow/Btn")?.GetComponent<Button>();
            // 只有满级才禁用；钱不够时仍可点，由 BuildSystem 弹出具体提示
            if (bagBtn != null) bagBtn.interactable = bs.BagUpgradeCost > 0;
            if (stamBtn != null) stamBtn.interactable = bs.StaminaUpgradeCost > 0;
        }

        private void BuildUI_()
        {
            _canvasGo = new GameObject("BuildCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 23;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Toggle);

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(440, 260); rt.anchoredPosition = Vector2.zero;
            _root.AddComponent<Image>().color = new Color(0.1f, 0.09f, 0.07f, 0.96f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 标题+金钱
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(_root.transform, false);
            headerGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
            var hlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            var title = CreateText(headerGo.transform, "Title", 20, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "房屋扩建 (B/Esc)");
            title.rectTransform.sizeDelta = new Vector2(0, 28);
            _moneyText = CreateText(headerGo.transform, "Money", 18, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "宋 0文");
            _moneyText.alignment = TextAnchor.MiddleRight;
            _moneyText.rectTransform.sizeDelta = new Vector2(100, 28);

            // 背包扩容行
            var bagRow = CreateUpgradeRow("BagRow", "背包扩容", "24→36→48格", 500);
            var bagBtn = bagRow.transform.Find("Btn").GetComponent<Button>();
            bagBtn.onClick.AddListener(() => { if (BuildSystem.Instance != null) BuildSystem.Instance.TryUpgradeBag(); Refresh(); });

            // 体力上限行
            var stamRow = CreateUpgradeRow("StamRow", "体力上限", "100→150→200", 800);
            var stamBtn = stamRow.transform.Find("Btn").GetComponent<Button>();
            stamBtn.onClick.AddListener(() => { if (BuildSystem.Instance != null) BuildSystem.Instance.TryUpgradeStamina(); Refresh(); });
        }

        private GameObject CreateUpgradeRow(string rowName, string title, string desc, int cost)
        {
            var row = new GameObject(rowName);
            row.transform.SetParent(_root.transform, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 64);
            row.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.1f, 0.9f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.spacing = 8; hlg.childControlWidth = false; hlg.childForceExpandWidth = false;

            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(row.transform, false);
            var infoRt = infoGo.AddComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(300, 56);
            var info = infoGo.AddComponent<Text>();
            info.font = _font; info.fontSize = 15; info.alignment = TextAnchor.MiddleLeft;
            info.color = new Color(1f, 0.92f, 0.75f); info.supportRichText = true;
            info.text = $"<b>{title}</b>\n{desc}  需要: {cost} 文";
            info.raycastTarget = false;

            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(row.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(80, 48);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.5f, 0.6f, 1f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one; btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = _font; btnText.fontSize = 16; btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white; btnText.text = "升级"; btnText.raycastTarget = false;

            return row;
        }

        private Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
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
