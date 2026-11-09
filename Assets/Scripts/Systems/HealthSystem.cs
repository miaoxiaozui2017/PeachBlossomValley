using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>疾病类型</summary>
    public enum IllnessType { None, Cold, Hangover, Diarrhea, Trauma }

    /// <summary>
    /// 健康系统：模拟"得病 → 吃药 → 痊愈"的循环。
    /// - 淋雨（雨天不睡觉在外面）→ 风寒(Cold)
    /// - 喝酒过量 → 醉酒(Hangover)
    /// - 吃生野味 → 腹泻(Diarrhea)
    /// - 长期疲劳(体力归零) → 跌打(Trauma)
    /// 疾病效果：风寒/跌打 降低当日体力上限；腹泻/醉酒 消耗体力。
    /// 用药（姜汤/风寒药/醒酒汤/止泻药/跌打膏）按 F 食用即治愈对应疾病。
    /// 存档支持。
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        public static HealthSystem Instance { get; private set; }

        public IllnessType CurrentIllness { get; private set; } = IllnessType.None;
        public int IllnessDaysLeft { get; private set; }

        public event Action<IllnessType> OnIllnessChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            // 淋雨：昨天是雨天、且玩家是"直接跳日"（没睡觉），则今天风寒
            bool rainedYesterday = WeatherSystem.Instance != null && WeatherSystem.Instance.CurrentWeather == Weather.Rainy;
            bool slept = TimeManager.Instance != null && TimeManager.Instance.LastAdvanceWasSleep;
            if (rainedYesterday && !slept && CurrentIllness == IllnessType.None)
            {
                SetIllness(IllnessType.Cold, 2);
                PBV.UI.HUD.Instance?.ShowNotice("昨夜淋了雨，染上风寒了！喝碗姜汤驱寒吧", 4f);
            }

            // 疾病持续：自然痊愈（约 2 天自愈）
            if (CurrentIllness != IllnessType.None)
            {
                IllnessDaysLeft--;
                if (IllnessDaysLeft <= 0)
                {
                    Debug.Log($"[Health] {CurrentIllness} 自然痊愈");
                    SetIllness(IllnessType.None, 0);
                }
                else
                {
                    // 疾病当日副作用
                    ApplyIllnessPenalty();
                }
            }
        }

        private void ApplyIllnessPenalty()
        {
            var ps = Player.PlayerStats.Instance;
            if (ps == null) return;
            switch (CurrentIllness)
            {
                case IllnessType.Cold:
                    PBV.UI.HUD.Instance?.ShowNotice("风寒未愈，今日精神不济（体力上限减半）", 3f);
                    break;
                case IllnessType.Hangover:
                    PBV.UI.HUD.Instance?.ShowNotice("宿醉未醒，头疼欲裂", 3f);
                    break;
                case IllnessType.Diarrhea:
                    ps.TrySpend(15);
                    PBV.UI.HUD.Instance?.ShowNotice("腹泻未愈，跑肚虚脱（-15 体力）", 3f);
                    break;
                case IllnessType.Trauma:
                    PBV.UI.HUD.Instance?.ShowNotice("跌打未愈，动作迟缓", 3f);
                    break;
            }
        }

        /// <summary>风寒/跌打造成的体力上限折扣（1=正常，0.5=减半）</summary>
        public float StaminaMaxMultiplier
        {
            get
            {
                switch (CurrentIllness)
                {
                    case IllnessType.Cold:
                    case IllnessType.Trauma:
                        return 0.5f;
                    default:
                        return 1f;
                }
            }
        }

        /// <summary>喝酒：过量则醉酒</summary>
        public void OnDrink()
        {
            // 30% 概率醉酒
            if (UnityEngine.Random.value < 0.3f && CurrentIllness == IllnessType.None)
            {
                SetIllness(IllnessType.Hangover, 1);
                PBV.UI.HUD.Instance?.ShowNotice("喝多了，醉醺醺的……来碗醒酒汤吧", 4f);
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("小酌一杯，暖了身子");
            }
        }

        /// <summary>吃生食（生野味）→ 腹泻</summary>
        public void OnEatRaw()
        {
            if (CurrentIllness == IllnessType.None)
            {
                SetIllness(IllnessType.Diarrhea, 1);
                PBV.UI.HUD.Instance?.ShowNotice("生野味没熟透，闹肚子了！", 4f);
            }
        }

        /// <summary>尝试用药（按 F 吃对应的药）</summary>
        public bool TryCure(string itemId)
        {
            if (CurrentIllness == IllnessType.None) return false;

            bool match =
                (CurrentIllness == IllnessType.Cold && (itemId == "item_ginger_soup" || itemId == "item_med_cold")) ||
                (CurrentIllness == IllnessType.Hangover && itemId == "item_hangover_soup") ||
                (CurrentIllness == IllnessType.Diarrhea && itemId == "item_med_diarrhea") ||
                (CurrentIllness == IllnessType.Trauma && itemId == "item_med_trauma");

            if (!match) return false;

            SetIllness(IllnessType.None, 0);
            PBV.UI.HUD.Instance?.ShowNotice("药到病除！身体痊愈了", 3f);
            return true;
        }

        private void SetIllness(IllnessType t, int days)
        {
            CurrentIllness = t;
            IllnessDaysLeft = days;
            OnIllnessChanged?.Invoke(t);
            Debug.Log($"[Health] 疾病状态 → {t}（剩 {days} 天）");
        }

        public string IllnessName
        {
            get
            {
                switch (CurrentIllness)
                {
                    case IllnessType.Cold: return "风寒";
                    case IllnessType.Hangover: return "醉酒";
                    case IllnessType.Diarrhea: return "腹泻";
                    case IllnessType.Trauma: return "跌打";
                    default: return "无";
                }
            }
        }

        // 存档
        public int Export() => (int)CurrentIllness * 10 + Mathf.Max(0, IllnessDaysLeft);
        public void Restore(int packed)
        {
            int t = packed / 10;
            int d = packed % 10;
            CurrentIllness = (IllnessType)Mathf.Clamp(t, 0, 4);
            IllnessDaysLeft = Mathf.Max(0, d);
        }
    }
}
