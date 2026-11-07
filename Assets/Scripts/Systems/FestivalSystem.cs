using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 节日系统：每季第28天触发"季节庆典"，出货售价x1.5。
    /// HUD 显示节日信息；ShippingBinSystem 结算时乘以倍率。
    /// </summary>
    public class FestivalSystem : MonoBehaviour
    {
        public static FestivalSystem Instance { get; private set; }

        public bool IsFestivalToday { get; private set; }
        public string FestivalName { get; private set; } = "";
        public float PriceMultiplier => IsFestivalToday ? 1.5f : 1f;

        public event Action<string> OnFestivalStarted; // festivalName

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
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            CheckFestival();
        }

        private void CheckFestival()
        {
            var ss = SeasonSystem.Instance;
            if (ss != null && ss.DayOfSeason >= 28)
            {
                IsFestivalToday = true;
                FestivalName = $"{ss.CurrentSeasonName}季节典";
                OnFestivalStarted?.Invoke(FestivalName);
                Debug.Log($"[Festival] 今日节日: {FestivalName}（出货售价x1.5）");
            }
            else
            {
                IsFestivalToday = false;
                FestivalName = "";
            }
        }
    }
}
