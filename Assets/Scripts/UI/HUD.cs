using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Economy;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>
    /// 灰盒 HUD：时间/天数/金钱/待结算/体力条/当前工具 + 操作提示（事件驱动）
    /// </summary>
    public class HUD : MonoBehaviour
    {
        private Text _timeText;
        private Text _moneyText;
        private Text _pendingText;
        private Text _toolText;
        private Image _staminaFill;
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

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnStaminaChanged += OnStaminaChanged;
                OnStaminaChanged(PlayerStats.Instance.Stamina, PlayerStats.Instance.MaxStamina);
            }

            if (PBV.Systems.WeatherSystem.Instance != null)
            {
                PBV.Systems.WeatherSystem.Instance.OnWeatherChanged += OnWeatherChanged;
            }

            if (ToolManager.Instance != null)
            {
                ToolManager.Instance.OnToolChanged += OnToolChanged;
                OnToolChanged(ToolManager.Instance.CurrentTool);
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
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnStaminaChanged -= OnStaminaChanged;
            }
            if (ToolManager.Instance != null)
            {
                ToolManager.Instance.OnToolChanged -= OnToolChanged;
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

        private void OnStaminaChanged(int current, int max)
        {
            if (_staminaFill != null)
            {
                _staminaFill.fillAmount = max > 0 ? (float)current / max : 0f;
                _staminaFill.color = current < max * 0.3f
                    ? new Color(0.9f, 0.35f, 0.3f)   // 低体力红色
                    : new Color(0.4f, 0.85f, 0.45f); // 正常绿色
            }
        }

        private void OnToolChanged(ToolType tool)
        {
            if (_toolText != null && ToolManager.Instance != null)
            {
                int cost = ToolManager.Instance.CurrentStaminaCost;
                _toolText.text = $"工具：{ToolManager.Instance.CurrentToolName}（耗 {cost} 体力）  [Q 切换]";
            }
        }

        private void OnWeatherChanged(PBV.Systems.Weather weather)
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            string time = TimeManager.Instance != null ? TimeManager.Instance.GetTimeString() : "06:00";
            RefreshTimeText(time, day);
        }

        private void RefreshTimeText(string time, int day)
        {
            if (_timeText == null) return;
            string season = PBV.Systems.SeasonSystem.Instance != null ? PBV.Systems.SeasonSystem.Instance.CurrentSeasonName : "";
            string weather = PBV.Systems.WeatherSystem.Instance != null ? $" {PBV.Systems.WeatherSystem.Instance.WeatherName}" : "";
            string festival = PBV.Systems.FestivalSystem.Instance != null && PBV.Systems.FestivalSystem.Instance.IsFestivalToday ? $" 【{PBV.Systems.FestivalSystem.Instance.FestivalName}】" : "";
            _timeText.text = string.IsNullOrEmpty(season) ? $"第 {day} 天   {time}{weather}{festival}" : $"{season}季·第 {day} 天   {time}{weather}{festival}";
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

            // 体力条（背景+填充）
            var staminaBgGo = new GameObject("StaminaBarBg");
            staminaBgGo.transform.SetParent(canvasGo.transform, false);
            var staminaBgRect = staminaBgGo.AddComponent<RectTransform>();
            staminaBgRect.anchorMin = new Vector2(0f, 1f);
            staminaBgRect.anchorMax = new Vector2(0f, 1f);
            staminaBgRect.pivot = new Vector2(0f, 1f);
            staminaBgRect.anchoredPosition = new Vector2(24f, -140f);
            staminaBgRect.sizeDelta = new Vector2(220f, 22f);
            var staminaBg = staminaBgGo.AddComponent<Image>();
            staminaBg.color = new Color(0.1f, 0.08f, 0.06f, 0.8f);

            var staminaFillGo = new GameObject("StaminaFill");
            staminaFillGo.transform.SetParent(staminaBgGo.transform, false);
            var staminaFillRect = staminaFillGo.AddComponent<RectTransform>();
            staminaFillRect.anchorMin = Vector2.zero;
            staminaFillRect.anchorMax = Vector2.one;
            staminaFillRect.offsetMin = new Vector2(2f, 2f);
            staminaFillRect.offsetMax = new Vector2(-2f, -2f);
            _staminaFill = staminaFillGo.AddComponent<Image>();
            _staminaFill.type = Image.Type.Filled;
            _staminaFill.fillMethod = Image.FillMethod.Horizontal;
            _staminaFill.fillAmount = 1f;
            _staminaFill.color = new Color(0.4f, 0.85f, 0.45f);

            // 体力数值标签
            var staminaLabel = CreateText(canvasGo.transform, "StaminaLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(252f, -140f), new Vector2(120f, 22f), 16, FontStyle.Bold, new Color(0.7f, 0.95f, 0.75f));
            staminaLabel.text = "体力";
            staminaLabel.alignment = TextAnchor.MiddleLeft;

            // 当前工具
            _toolText = CreateText(canvasGo.transform, "ToolText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -175f), new Vector2(460f, 28f), 18, FontStyle.Bold, new Color(0.85f, 0.9f, 1f));

            var hint = CreateText(canvasGo.transform, "HintText", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 80f), new Vector2(-40f, 30f), 15, FontStyle.Normal, new Color(1f, 1f, 1f, 0.65f));
            hint.text = "WASD 移动   Space 使用工具   Q 切换工具   E 交互   Tab 背包   F5 存档   F9 读档   N 次日";
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
