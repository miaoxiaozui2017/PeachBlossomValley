using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Economy;

namespace PBV.UI
{
    /// <summary>
    /// 灰盒 HUD：时间/天数/金钱 + 操作提示（事件驱动，零 Update 开销）
    /// </summary>
    public class HUD : MonoBehaviour
    {
        private Text _timeText;
        private Text _moneyText;
        private Text _pendingText; // 出货箱待结算金额
        private ShippingBinSystem _shippingBin;
        private static Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged += OnTimeChanged;
                TimeManager.Instance.OnDayChanged += OnDayChanged;
                RefreshTimeText(TimeManager.Instance.GetTimeString(), TimeManager.Instance.CurrentDay);
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged += OnMoneyChanged;
                OnMoneyChanged(PlayerWallet.Instance.Money);
            }

            _shippingBin = FindObjectOfType<ShippingBinSystem>();
            if (_shippingBin != null)
            {
                _shippingBin.OnPendingChanged += OnPendingChanged;
                OnPendingChanged(_shippingBin.PendingValue);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= OnTimeChanged;
                TimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.OnMoneyChanged -= OnMoneyChanged;
            }
            if (_shippingBin != null)
            {
                _shippingBin.OnPendingChanged -= OnPendingChanged;
            }
        }

        private void OnTimeChanged(int hour, int minute)
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            RefreshTimeText($"{hour:00}:{minute:00}", day);
        }

        private void OnDayChanged()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            string time = TimeManager.Instance != null ? TimeManager.Instance.GetTimeString() : "06:00";
            RefreshTimeText(time, day);
        }

        private void OnMoneyChanged(int money)
        {
            if (_moneyText != null) _moneyText.text = $"¥ {money}";
        }

        private void OnPendingChanged(int pending)
        {
            if (_pendingText != null)
            {
                _pendingText.text = pending > 0 ? $"出货待结算 ¥ {pending}（次日06:00入账）" : string.Empty;
            }
        }

        private void RefreshTimeText(string time, int day)
        {
            if (_timeText != null) _timeText.text = $"第 {day} 天   {time}";
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _timeText = CreateText(canvasGo.transform, "TimeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -20f), new Vector2(420f, 36f), 24, FontStyle.Bold, new Color(1f, 0.96f, 0.86f));
            _moneyText = CreateText(canvasGo.transform, "MoneyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -62f), new Vector2(420f, 36f), 24, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));

            _pendingText = CreateText(canvasGo.transform, "PendingText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -100f), new Vector2(460f, 28f), 16, FontStyle.Normal, new Color(0.95f, 0.7f, 0.35f, 0.95f));

            var hint = CreateText(canvasGo.transform, "HintText", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 80f), new Vector2(-40f, 30f), 15, FontStyle.Normal, new Color(1f, 1f, 1f, 0.65f));
            hint.text = "WASD 移动   Space 使用工具   E 交互   Tab 背包   F5 存档   F9 读档   N 跳到次日";
            hint.alignment = TextAnchor.MiddleCenter;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
