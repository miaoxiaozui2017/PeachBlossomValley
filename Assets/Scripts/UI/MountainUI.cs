using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>采山 UI：MountainEntrance 打开，按钮采集山货/药材/野味。</summary>
    public class MountainUI : MonoBehaviour
    {
        public static MountainUI Instance { get; private set; }

        private GameObject _canvasGo;
        private Transform _panel;
        private Text _staminaText;
        private Text _resultText;
        private Text _usesText;
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
            if (_isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
                Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(Toggle);
            else UIEventSystemBootstrap.ClearTopPanel();
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            var ms = ForageMountainSystem.Instance;
            if (ms == null) return;
            if (_usesText != null) _usesText.text = $"今日剩余 {ms.UsesLeft} / {ForageMountainSystem.UsesPerDay} 次";
            if (PlayerStats.Instance != null)
                _staminaText.text = $"体力: {PlayerStats.Instance.Stamina}/{PlayerStats.Instance.MaxStamina}（采一次 -{ms.StaminaCost}）";
        }

        private void DoForage()
        {
            var r = ForageMountainSystem.Instance?.Forage();
            if (_resultText != null)
            {
                _resultText.text = r == null
                    ? "采不到（体力不足或今日已采尽）"
                    : $"采到 <color=#ffcc66>{r.Value.item.itemName} ×{r.Value.count}</color>！";
                _resultText.supportRichText = true;
            }
            Refresh();
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("MountainCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Toggle);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            _panel = panel.transform;
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(360, 340); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.09f, 0.96f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var title = CreateText("采 山 (E/Esc 关闭)", 22, FontStyle.Bold, new Color(0.6f, 0.85f, 0.55f));
            title.rectTransform.sizeDelta = new Vector2(0, 30);

            _staminaText = CreateText("体力: 100/100", 16, FontStyle.Normal, new Color(0.5f, 0.9f, 0.5f));
            _usesText = CreateText("今日剩余 5 / 5 次", 16, FontStyle.Bold, new Color(0.9f, 0.75f, 0.5f));

            var btnGo = new GameObject("ForageBtn");
            btnGo.transform.SetParent(panel.transform, false);
            btnGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.45f, 0.3f, 1f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one; btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = _font; btnText.fontSize = 22; btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter; btnText.color = Color.white;
            btnText.text = "采 集"; btnText.raycastTarget = false;
            btn.onClick.AddListener(DoForage);

            _resultText = CreateText("点击采集，得山货/药材/野味", 15, FontStyle.Normal, new Color(1f, 0.9f, 0.6f));
            _resultText.rectTransform.sizeDelta = new Vector2(0, 40);
            _resultText.supportRichText = true;

            var hint = CreateText("稀有：山参(4%) / 云雾茶 / 野蜂蜜；毒草可制毒饵", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.6f));
        }

        private Text CreateText(string content, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_panel, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter; t.color = color; t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
