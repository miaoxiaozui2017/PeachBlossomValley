using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 季节系统：每季 28 天，一年 4 季（春→夏→秋→冬）循环。
    /// 订阅 TimeManager.OnDayChanged 推进；提供 CanPlant 供种植系统检查作物季节兼容性。
    /// </summary>
    public class SeasonSystem : MonoBehaviour
    {
        public static SeasonSystem Instance { get; private set; }

        public const int DaysPerSeason = 28;

        public Season CurrentSeason { get; private set; } = Season.Spring;
        public int DayOfSeason { get; private set; } = 1;

        /// <summary>季节变化事件</summary>
        public event Action<Season> OnSeasonChanged;

        private static readonly string[] _seasonNames = { "春", "夏", "秋", "冬" };
        public string CurrentSeasonName => _seasonNames[(int)CurrentSeason];

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
                UpdateForDay(TimeManager.Instance.CurrentDay);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            if (TimeManager.Instance != null) UpdateForDay(TimeManager.Instance.CurrentDay);
        }

        public void UpdateForDay(int day)
        {
            if (day < 1) day = 1;
            int seasonIdx = ((day - 1) / DaysPerSeason) % 4;
            int dayOfSeason = ((day - 1) % DaysPerSeason) + 1;
            var newSeason = (Season)seasonIdx;
            if (newSeason != CurrentSeason)
            {
                CurrentSeason = newSeason;
                OnSeasonChanged?.Invoke(CurrentSeason);
                Debug.Log($"[Season] 进入{_seasonNames[seasonIdx]}季（当季第 {dayOfSeason} 天）");
            }
            DayOfSeason = dayOfSeason;
        }

        /// <summary>存档恢复</summary>
        public void Restore(int seasonIdx, int dayOfSeason)
        {
            CurrentSeason = (Season)Mathf.Clamp(seasonIdx, 0, 3);
            DayOfSeason = Mathf.Clamp(dayOfSeason, 1, DaysPerSeason);
        }

        /// <summary>该作物能否在当前季节种植（seasons 为空视为全年可种）</summary>
        public bool CanPlant(PBV.Farming.CropData crop)
        {
            if (crop == null || crop.seasons == null || crop.seasons.Length == 0) return true;
            foreach (var s in crop.seasons)
            {
                if (s == CurrentSeason) return true;
            }
            return false;
        }
    }

    /// <summary>四季枚举</summary>
    public enum Season { Spring, Summer, Autumn, Winter }
}
