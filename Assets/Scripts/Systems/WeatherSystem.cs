using System;
using UnityEngine;

namespace PBV.Systems
{
    public enum Weather { Sunny, Cloudy, Rainy }

    /// <summary>
    /// 天气系统：每日随机天气（晴/阴/雨），雨天自动浇灌全部已耕地。
    /// 订阅 TimeManager.OnDayChanged；HUD 显示当前天气。
    /// </summary>
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

            if (CurrentWeather == Weather.Rainy)
            {
                // 雨天自动浇水所有已耕地
                var farmGrid = FindObjectOfType<PBV.Farming.FarmGrid>();
                if (farmGrid != null) farmGrid.AutoWaterAll();
            }
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
