using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 时空驿站（传送门面板）：E 或 X 打开，提供「穿梭古代⇄现代」与「宋币⇄人民币随时兑换」。
    /// 汇率见 ExchangeSystem（50 文 = ¥1）。
    /// </summary>
    public class PortalUI : MonoBehaviour
    {
        public static PortalUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private static Font _font;

        private Text _worldText;
        private Text _songText;
        private Text _rmbText;
        private Text _rateText;
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
            // X：随时随地打开时空驿站，只为「两边随时兑换货币」；穿梭仍需确认按钮
            if (!_isOpen && Input.GetKeyDown(KeyCode.X)) { Open(); return; }
            if (_isOpen && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
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
            var w = PlayerWallet.Instance;
            if (w != null)
            {
                if (_songText != null) _songText.text = $"宋币：{w.SongCoins} 文";
                if (_rmbText != null) _rmbText.text = $"人民币：¥{w.RMB}";
            }
            if (_worldText != null)
                _worldText.text = WorldState.Instance != null
                    ? $"当前：{(WorldState.Instance.Current == WorldType.Modern ? "现代 · 现实世界" : "桃花谷 · 宋代")}"
                    : "当前：桃花谷";
            if (_rateText != null) _rateText.text = $"汇率：{ExchangeSystem.RateText()}（无损耗）";
        }

        private void Travel()
        {
            var ws = WorldState.Instance;
            if (ws == null) return;
            // 先校验通行费，不够就保持面板打开，避免「面板关了但没穿越」的错觉
            var w = PlayerWallet.Instance;
            bool leavingValley = ws.Current == WorldType.Ancient;
            int fee = leavingValley ? WorldState.VALLEY_FEE : WorldState.MODERN_FEE;
            if (w != null)
            {
                int have = leavingValley ? w.SongCoins : w.RMB;
                if (have < fee)
                {
                    HUD.Instance?.ShowNotice($"通行费不足：需要 {(leavingValley ? fee + " 文" : "¥" + fee)} · 可先在下方兑换货币");
                    return;
                }
            }
            Close();
            ws.Toggle();
        }

        private void ExchangeSongToRMB()
        {
            var w = PlayerWallet.Instance;
            if (w == null) return;
            int rmb = ExchangeSystem.SongToRMB(w.SongCoins);
            if (rmb <= 0)
            {
                HUD.Instance?.ShowNotice($"至少需 {ExchangeSystem.SONG_PER_RMB} 文才能换 ¥1");
                return;
            }
            int spend = rmb * ExchangeSystem.SONG_PER_RMB;
            w.SpendSong(spend);
            w.AddRMB(rmb);
            HUD.Instance?.ShowNotice($"兑换：花 {spend} 文 → +¥{rmb}");
            Refresh();
        }

        private void ExchangeRMBToSong()
        {
            var w = PlayerWallet.Instance;
            if (w == null) return;
            int rmb = w.RMB;
            if (rmb <= 0)
            {
                HUD.Instance?.ShowNotice("没有可兑换的人民币");
                return;
            }
            int gain = ExchangeSystem.RMBToSong(rmb);
            w.SpendRMB(rmb);
            w.AddSong(gain);
            HUD.Instance?.ShowNotice($"兑换：花 ¥{rmb} → +{gain} 文");
            Refresh();
        }

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("PortalCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32;
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
            prt.sizeDelta = new Vector2(440, 420); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.14f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var title = CreateText(panel.transform, "Title", 22, FontStyle.Bold, new Color(0.85f, 0.75f, 1f), "时空驿站");
            title.alignment = TextAnchor.MiddleCenter;

            _worldText = CreateText(panel.transform, "World", 16, FontStyle.Normal, new Color(0.9f, 0.9f, 0.95f), "当前：桃花谷");
            _worldText.alignment = TextAnchor.MiddleCenter;

            _songText = CreateText(panel.transform, "Song", 16, FontStyle.Bold, new Color(0.95f, 0.8f, 0.4f), "宋币：0 文");
            _songText.alignment = TextAnchor.MiddleCenter;
            _rmbText = CreateText(panel.transform, "RMB", 16, FontStyle.Bold, new Color(0.6f, 0.9f, 0.7f), "人民币：¥0");
            _rmbText.alignment = TextAnchor.MiddleCenter;
            _rateText = CreateText(panel.transform, "Rate", 13, FontStyle.Normal, new Color(0.8f, 0.8f, 0.85f), "汇率：50 文 = ¥1");
            _rateText.alignment = TextAnchor.MiddleCenter;

            AddButton(panel.transform, "去现代 / 回桃花谷", new Color(0.4f, 0.45f, 0.7f, 1f), Travel);
            AddButton(panel.transform, "宋币 → 人民币（按汇率全兑）", new Color(0.3f, 0.5f, 0.6f, 1f), ExchangeSongToRMB);
            AddButton(panel.transform, "人民币 → 宋币（按汇率全兑）", new Color(0.6f, 0.4f, 0.3f, 1f), ExchangeRMBToSong);

            _hintText = CreateText(panel.transform, "Hint", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.6f),
                "X 随时打开 · E / X / Esc 关闭 · 兑换无损耗 · 穿梭需通行费");
            _hintText.alignment = TextAnchor.MiddleCenter;
        }

        private void AddButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(parent, false);
            btnGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
            var img = btnGo.AddComponent<Image>();
            img.color = color;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var tGo = new GameObject("Text");
            tGo.transform.SetParent(btnGo.transform, false);
            tGo.AddComponent<RectTransform>().anchorMin = Vector2.zero; tGo.AddComponent<RectTransform>().anchorMax = Vector2.one;
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
