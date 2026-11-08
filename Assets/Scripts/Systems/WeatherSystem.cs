using System;
using UnityEngine;

namespace PBV.Systems
{
    public enum Weather { Sunny, Cloudy, Rainy }

    /// <summary>
    /// 天气系统：每日随机天气（晴/阴/雨）。雨天视为全自动浇灌。
    /// 执行顺序调到最前，保证跨日时先定当天天气，CropManager 再做生长结算。
    /// 订阅 TimeManager.OnDayChanged；HUD 显示当前天气。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public Weather CurrentWeather { get; private set; } = Weather.Sunny;
        public event Action<Weather> OnWeatherChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 0;
            // 60%晴 / 25%阴 / 15%雨
            float r = UnityEngine.Random.value;
            CurrentWeather = r < 0.6f ? Weather.Sunny : r < 0.85f ? Weather.Cloudy : Weather.Rainy;
            OnWeatherChanged?.Invoke(CurrentWeather);
            Debug.Log($"[Weather] 第{day}天天气: {CurrentWeather}");

            // 雨天浇灌由 CropManager.OnNewDay 在同一轮结算里统一处理：
            // 先按「雨天=已浇水」推进生长，再把地块置为湿润视觉，
            // 避免两个系统抢 OnDayChanged 顺序导致生长被跳过或水状态被覆盖。
        }

        public void Restore(int weather) { CurrentWeather = (Weather)weather; }

        public string WeatherName
        {
            get
            {
                switch (CurrentWeather)
                {
                    case Weather.Sunny: return "晴";
                    case Weather.Cloudy: return "阴";
                    case Weather.Rainy: return "雨";
                    default: return "";
                }
            }
        }
    }
}
