using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>节日专属加成类型</summary>
    public enum FestivalBonus { None, Price, Fishing, Mining, Cooking, Affection }

    /// <summary>
    /// 节日系统：每季第 28 天触发对应节日（春=春节 / 夏=端午 / 秋=中秋 / 冬=元宵）。
    /// 统一售价 ×1.5，另按节日给专属加成与一次性节礼。
    /// </summary>
    public class FestivalSystem : MonoBehaviour
    {
        public static FestivalSystem Instance { get; private set; }

        public bool IsFestivalToday { get; private set; }
        public string FestivalName { get; private set; } = "";
        public FestivalBonus Bonus { get; private set; } = FestivalBonus.None;
        public float PriceMultiplier => IsFestivalToday ? 1.5f : 1f;

        /// <summary>钓鱼稀有率加成（端午）</summary>
        public float RareFishBonus => IsFestivalToday && Bonus == FestivalBonus.Fishing ? 0.25f : 0f;
        /// <summary>挖矿额外产出（元宵）</summary>
        public int ExtraOre => IsFestivalToday && Bonus == FestivalBonus.Mining ? 1 : 0;
        /// <summary>料理食用效果倍率（中秋）</summary>
        public float FoodMultiplier => IsFestivalToday && Bonus == FestivalBonus.Cooking ? 1.5f : 1f;
        /// <summary>送礼好感倍率（春节）</summary>
        public float GiftMultiplier => IsFestivalToday && Bonus == FestivalBonus.Affection ? 2f : 1f;

        public event Action<string> OnFestivalStarted;

        private int _lastGiftDay = -1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += OnNewDay;
                CheckFestival();
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay() => CheckFestival();

        private void CheckFestival()
        {
            var ss = SeasonSystem.Instance;
            if (ss == null || ss.DayOfSeason < 28)
            {
                IsFestivalToday = false;
                FestivalName = "";
                Bonus = FestivalBonus.None;
                return;
            }

            string s = ss.CurrentSeasonName;
            if (s.Contains("春")) { FestivalName = "春节"; Bonus = FestivalBonus.Affection; }
            else if (s.Contains("夏")) { FestivalName = "端午"; Bonus = FestivalBonus.Fishing; }
            else if (s.Contains("秋")) { FestivalName = "中秋"; Bonus = FestivalBonus.Cooking; }
            else { FestivalName = "元宵"; Bonus = FestivalBonus.Mining; }

            IsFestivalToday = true;
            OnFestivalStarted?.Invoke(FestivalName);

            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 0;
            if (day != _lastGiftDay)
            {
                _lastGiftDay = day;
                int gift = Bonus == FestivalBonus.Affection ? 88 : 66;
                if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddMoney(gift);
            }

            string bonusText = Bonus == FestivalBonus.Affection ? "送礼好感 ×2" :
                               Bonus == FestivalBonus.Fishing ? "稀有鱼概率 +25%" :
                               Bonus == FestivalBonus.Cooking ? "料理效果 ×1.5" : "挖矿产出 +1";
            PBV.UI.HUD.Instance?.ShowNotice($"【{FestivalName}】出货 ×1.5 · {bonusText} · 节礼 ¥{(Bonus == FestivalBonus.Affection ? 88 : 66)}", 4f);
            Debug.Log($"[Festival] {FestivalName}：{bonusText}");
        }
    }
}
