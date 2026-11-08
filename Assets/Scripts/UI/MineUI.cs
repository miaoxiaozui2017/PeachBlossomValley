using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Inventory;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>挖矿 UI：M 键开关，挖矿按钮消耗10体力获得矿石。</summary>
    public class MineUI : MonoBehaviour
    {
        public static MineUI Instance { get; private set; }

        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private Text _staminaText;
        private Text _resultText;
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Start()
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStaminaChanged += (v, m) => { if (_isOpen) UpdateStamina(v, m); };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) || (_isOpen && Input.GetKeyDown(KeyCode.Escape))) Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(Toggle); else UIEventSystemBootstrap.ClearTopPanel();
            if (_isOpen)
            {
                if (PlayerStats.Instance != null) UpdateStamina(PlayerStats.Instance.Stamina, PlayerStats.Instance.MaxStamina);
                if (_resultText != null) _resultText.text = "点击挖矿开始挖掘";
            }
        }

        private void UpdateStamina(int value, int max)
        {
            if (_staminaText != null) _staminaText.text = $"体力: {value}/{max}";
        }

        private void DoMine()
        {
            var result = MineSystem.Instance?.TryMine();
            if (result == null)
            {
                if (_resultText != null) _resultText.text = "体力不足！需要10体力";
                return;
            }
            var (ore, count) = result.Value;
            bool added = InventorySystem.Instance != null && InventorySystem.Instance.AddItem(ore, count);
            if (_resultText != null)
            {
                _resultText.text = added
                    ? $"挖到了 <color=#ffcc66>{ore.itemName} x{count}</color>！"
                    : "背包满了！";
                _resultText.supportRichText = true;
            }
            if (PlayerStats.Instance != null) UpdateStamina(PlayerStats.Instance.Stamina, PlayerStats.Instance.MaxStamina);
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("MineCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 26;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Toggle);

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var panelRt = _root.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(360, 280); panelRt.anchoredPosition = Vector2.zero;
            var panelImg = _root.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.09f, 0.07f, 0.96f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 标题
            var title = CreateText("矿洞 (M/Esc 关闭)", 22, FontStyle.Bold, new Color(0.8f, 0.7f, 0.5f));
            title.rectTransform.sizeDelta = new Vector2(0, 30);

            // 体力
            _staminaText = CreateText("体力: 100/100", 18, FontStyle.Normal, new Color(0.5f, 0.9f, 0.5f));
            _staminaText.rectTransform.sizeDelta = new Vector2(0, 24);

            // 挖矿按钮
            var btnGo = new GameObject("MineBtn");
            btnGo.transform.SetParent(_root.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(0, 60);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.4f, 0.3f, 0.15f, 1f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one; btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = _font; btnText.fontSize = 24; btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter; btnText.color = Color.white;
            btnText.text = "挖矿 (消耗10体力)"; btnText.raycastTarget = false;
            btn.onClick.AddListener(DoMine);

            // 结果
            _resultText = CreateText("点击挖矿开始挖掘", 16, FontStyle.Normal, new Color(1f, 0.9f, 0.6f));
            _resultText.rectTransform.sizeDelta = new Vector2(0, 30);
            _resultText.supportRichText = true;

            Text CreateText(string content, int fontSize, FontStyle style, Color color)
            {
                var go = new GameObject("Text");
                go.transform.SetParent(_root.transform, false);
                go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
                var t = go.AddComponent<Text>();
                t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
                t.alignment = TextAnchor.MiddleCenter; t.color = color; t.text = content;
                t.raycastTarget = false;
                return t;
            }
        }
    }
}
