using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Economy;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>
    /// 灰盒 HUD：时间/天数/金钱/待结算/体力/当前工具/任务进度/世界状态 + 操作提示。
    /// 纯展示层：排序最底、不挂 GraphicRaycaster、所有 Graphic 关 raycastTarget，
    /// 避免遮挡/拦截下层功能面板（制作、商店等）的按钮点击。
    /// 体力与任务进度每帧兜底轮询，不依赖事件订阅顺序，保证"实时更新"。
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        private Text _timeText;
        private Text _moneyText;
        private Text _pendingText;
        private Text _toolText;
        private Image _staminaFill;
        private RectTransform _staminaFillRect;
        private Text _staminaValueText;
        private Text _questText;
        private Text _worldText;
        private ShippingBinSystem _shippingBin;
        private static Font _font;

        // 屏幕中央偏上的临时提示（体力不足、穿越成功、一次播多格等）
        private GameObject _noticeGo;
        private Text _noticeText;
        private float _noticeTimer;

        // 轮询缓存：只在数值/文案变化时写 UI
        private int _lastStamina = -1;
        private int _lastMaxStamina = -1;
        private string _lastQuestLine = "";
        private string _lastWorldLine = "";
        private string _lastToolLine = "";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // 场景未放置 EventSystem，运行时补建，否则所有 UI 按钮点不动
            UIEventSystemBootstrap.Ensure();
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

            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.OnWeatherChanged += OnWeatherChanged;
            }

            if (ToolManager.Instance != null)
            {
                ToolManager.Instance.OnToolChanged += OnToolChanged;
                OnToolChanged(ToolManager.Instance.CurrentTool);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= OnTimeChanged;
                TimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= OnMoneyChanged;
            if (_shippingBin != null) _shippingBin.OnPendingChanged -= OnPendingChanged;
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnStaminaChanged -= OnStaminaChanged;
            if (ToolManager.Instance != null) ToolManager.Instance.OnToolChanged -= OnToolChanged;
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.OnWeatherChanged -= OnWeatherChanged;
        }

        /// <summary>全局临时提示；任何系统都能调用，不需要额外挂载组件</summary>
        public void ShowNotice(string message, float seconds = 2.8f)
        {
            if (_noticeGo == null || _noticeText == null) return;
            _noticeText.text = message;
            _noticeGo.SetActive(true);
            _noticeTimer = seconds;
        }

        private void Update()
        {
            // 兜底轮询：即使事件订阅没生效（脚本执行顺序、动态补挂组件），体力也实时刷新
            if (PlayerStats.Instance != null)
            {
                var s = PlayerStats.Instance;
                if (s.Stamina != _lastStamina || s.MaxStamina != _lastMaxStamina)
                    OnStaminaChanged(s.Stamina, s.MaxStamina);
            }
            RefreshQuestLine();
            RefreshWorldLine();
            RefreshToolLine();

            if (_noticeTimer > 0f)
            {
                _noticeTimer -= Time.unscaledDeltaTime;
                if (_noticeTimer <= 0f && _noticeGo != null) _noticeGo.SetActive(false);
            }
        }

        private void RefreshQuestLine()
        {
            string line;
            var qm = QuestManager.Instance;
            if (qm == null) line = "任务：加载中…";
            else
            {
                Quest active = null;
                foreach (var q in qm.Quests)
                {
                    if (q.status == QuestStatus.Active) { active = q; break; }
                }
                line = active != null
                    ? $"任务：{active.title}   {active.currentCount}/{active.targetCount}\n{active.description}"
                    : "任务：全部完成 ✓";
            }
            if (line == _lastQuestLine) return;
            _lastQuestLine = line;
            if (_questText != null) _questText.text = line;
        }

        /// <summary>工具消耗每帧比对：升级 / 读档后也能立刻显示正确数值</summary>
        private void RefreshToolLine()
        {
            var tm = ToolManager.Instance;
            if (tm == null || _toolText == null) return;
            string line = $"工具：{tm.CurrentToolName}（耗 {tm.CurrentStaminaCost} 体力）  [左键使用 · Q 切换]";
            if (line == _lastToolLine) return;
            _lastToolLine = line;
            _toolText.text = line;
        }

        private void RefreshWorldLine()
        {
            var ws = WorldState.Instance;
            string line = ws == null
                ? "【桃花谷】出货 ×1"
                : ws.Current == WorldType.Ancient
                    ? "【桃花谷】出货 ×1  ·  传送门 E 前往现代"
                    : "【现代】出货 ×2  ·  按 T 回桃花谷";
            if (line == _lastWorldLine) return;
            _lastWorldLine = line;
            if (_worldText == null) return;
            _worldText.text = line;
            _worldText.color = ws != null && ws.Current == WorldType.Modern
                ? new Color(0.85f, 0.6f, 1f)
                : new Color(0.75f, 0.9f, 0.7f);
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
            _lastStamina = current;
            _lastMaxStamina = max;
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            // 以前用 Image.Type.Filled 但没给 sprite，无 sprite 时 Unity 会退化成纯色四边形，
            // fillAmount 完全不生效 —— 看起来就是"体力永远不更新"。
            // 改为直接缩放 RectTransform 宽度，不依赖任何贴图。
            if (_staminaFillRect != null)
                _staminaFillRect.anchorMax = new Vector2(ratio, 1f);
            if (_staminaFill != null)
                _staminaFill.color = ratio < 0.3f
                    ? new Color(0.9f, 0.35f, 0.3f)   // 低体力红色
                    : new Color(0.4f, 0.85f, 0.45f); // 正常绿色
            if (_staminaValueText != null)
                _staminaValueText.text = $"{current} / {max}";
        }

        private void OnToolChanged(ToolType tool)
        {
            if (_toolText != null && ToolManager.Instance != null)
            {
                int cost = ToolManager.Instance.CurrentStaminaCost;
                _toolText.text = $"工具：{ToolManager.Instance.CurrentToolName}（耗 {cost} 体力）  [左键使用 · Q 切换]";
            }
        }

        private void OnWeatherChanged(Weather weather)
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            string time = TimeManager.Instance != null ? TimeManager.Instance.GetTimeString() : "06:00";
            RefreshTimeText(time, day);
        }

        private void RefreshTimeText(string time, int day)
        {
            if (_timeText == null) return;
            string season = SeasonSystem.Instance != null ? SeasonSystem.Instance.CurrentSeasonName : "";
            string weather = WeatherSystem.Instance != null ? $" {WeatherSystem.Instance.WeatherName}" : "";
            string festival = FestivalSystem.Instance != null && FestivalSystem.Instance.IsFestivalToday ? $" 【{FestivalSystem.Instance.FestivalName}】" : "";
            string term = SolarTermSystem.CurrentTerm;
            _timeText.text = string.IsNullOrEmpty(season) ? $"第 {day} 天   {time}{weather}{festival}" : $"{season}季·{term}·第 {day} 天   {time}{weather}{festival}";
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // 最底层：所有功能面板（23~30）都在它上面
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // 纯展示层不挂 GraphicRaycaster，不参与点击命中

            _timeText = CreateText(canvasGo.transform, "TimeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -14f), new Vector2(400f, 24f), 15, FontStyle.Bold, new Color(1f, 0.96f, 0.86f));
            _moneyText = CreateText(canvasGo.transform, "MoneyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -38f), new Vector2(220f, 24f), 17, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));
            _pendingText = CreateText(canvasGo.transform, "PendingText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -62f), new Vector2(420f, 18f), 12, FontStyle.Normal, new Color(0.95f, 0.7f, 0.35f, 0.95f));

            // 体力条（背景+填充）
            var staminaBgGo = new GameObject("StaminaBarBg");
            staminaBgGo.transform.SetParent(canvasGo.transform, false);
            var staminaBgRect = staminaBgGo.AddComponent<RectTransform>();
            staminaBgRect.anchorMin = new Vector2(0f, 1f);
            staminaBgRect.anchorMax = new Vector2(0f, 1f);
            staminaBgRect.pivot = new Vector2(0f, 1f);
            staminaBgRect.anchoredPosition = new Vector2(20f, -84f);
            staminaBgRect.sizeDelta = new Vector2(160f, 14f);
            var staminaBg = staminaBgGo.AddComponent<Image>();
            staminaBg.color = new Color(0.1f, 0.08f, 0.06f, 0.8f);
            staminaBg.raycastTarget = false;

            var staminaFillGo = new GameObject("StaminaFill");
            staminaFillGo.transform.SetParent(staminaBgGo.transform, false);
            _staminaFillRect = staminaFillGo.AddComponent<RectTransform>();
            _staminaFillRect.anchorMin = Vector2.zero;
            _staminaFillRect.anchorMax = Vector2.one;
            _staminaFillRect.offsetMin = new Vector2(2f, 2f);
            _staminaFillRect.offsetMax = new Vector2(-2f, -2f);
            _staminaFill = staminaFillGo.AddComponent<Image>();
            _staminaFill.type = Image.Type.Simple; // 不用 Filled：无 sprite 时 fillAmount 无效
            _staminaFill.color = new Color(0.4f, 0.85f, 0.45f);
            _staminaFill.raycastTarget = false;

            // 体力数值（实时变化的数字，比单看色条更直观）
            _staminaValueText = CreateText(canvasGo.transform, "StaminaValue", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(188f, -84f), new Vector2(160f, 18f), 12, FontStyle.Bold, new Color(0.7f, 0.95f, 0.75f));
            _staminaValueText.text = "100 / 100";

            _toolText = CreateText(canvasGo.transform, "ToolText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -106f), new Vector2(440f, 20f), 13, FontStyle.Bold, new Color(0.85f, 0.9f, 1f));

            // 右上角：世界状态 + 常驻任务进度（不用打开 J 也能看到进度实时变化）
            _worldText = CreateText(canvasGo.transform, "WorldText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -14f), new Vector2(480f, 22f), 13, FontStyle.Bold, new Color(0.75f, 0.9f, 0.7f));
            _worldText.alignment = TextAnchor.MiddleRight;

            _questText = CreateText(canvasGo.transform, "QuestText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -38f), new Vector2(400f, 52f), 12, FontStyle.Normal, new Color(1f, 0.92f, 0.72f));
            _questText.alignment = TextAnchor.UpperRight;
            _questText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 临时提示（顶部居中，深色底 + 亮字，默认隐藏）
            _noticeGo = new GameObject("Notice");
            _noticeGo.transform.SetParent(canvasGo.transform, false);
            var noticeRt = _noticeGo.AddComponent<RectTransform>();
            noticeRt.anchorMin = new Vector2(0.5f, 1f);
            noticeRt.anchorMax = new Vector2(0.5f, 1f);
            noticeRt.pivot = new Vector2(0.5f, 1f);
            noticeRt.anchoredPosition = new Vector2(0f, -136f);
            noticeRt.sizeDelta = new Vector2(760f, 34f);
            var noticeBg = _noticeGo.AddComponent<Image>();
            noticeBg.color = new Color(0.08f, 0.06f, 0.05f, 0.9f);
            noticeBg.raycastTarget = false;
            var noticeTextGo = new GameObject("NoticeText");
            noticeTextGo.transform.SetParent(_noticeGo.transform, false);
            var nRect = noticeTextGo.AddComponent<RectTransform>();
            nRect.anchorMin = Vector2.zero; nRect.anchorMax = Vector2.one; nRect.sizeDelta = Vector2.zero;
            _noticeText = noticeTextGo.AddComponent<Text>();
            _noticeText.font = _font;
            _noticeText.fontSize = 14;
            _noticeText.fontStyle = FontStyle.Bold;
            _noticeText.alignment = TextAnchor.MiddleCenter;
            _noticeText.color = new Color(1f, 0.88f, 0.55f);
            _noticeText.raycastTarget = false;
            _noticeGo.SetActive(false);

            // 操作条移到左下角，避开屏幕中央的 [E] 交互提示，也不压住任何面板
            var hint = CreateText(canvasGo.transform, "HintText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 6f), new Vector2(900f, 18f), 12, FontStyle.Normal, new Color(1f, 1f, 1f, 0.5f));
            hint.text = "《谷中仙》   H 帮助（全部快捷键与玩法）    F5 存档    F9 读档    N 次日";
            hint.alignment = TextAnchor.MiddleLeft;
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
            text.raycastTarget = false; // 纯展示文字不接收点击
            return text;
        }
    }
}
