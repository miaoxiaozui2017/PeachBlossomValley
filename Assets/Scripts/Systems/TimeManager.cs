using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal; // For Light2D if needed, but using simple approach first

namespace PBV.Systems
{
    /// <summary>
    /// 时间系统管理器
    /// 负责游戏内时间的流逝、昼夜更替、跨日逻辑
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [Tooltip("Real seconds per game minute")]
        [SerializeField] private float _realSecondsPerGameMinute = 0.5f; // 0.5s = 1min -> 1天约9分钟
        
        [Header("Day/Night Cycle")]
        [SerializeField] private Light2D _globalLight;
        [SerializeField] private Gradient _dayNightGradient;

        [Header("Events")]
        public UnityAction<int, int> OnTimeChanged; // Hour, Minute
        public UnityAction OnDayChanged; // New Day

        private float _timer;
        private int _currentHour = 6; // Start at 6:00 AM
        private int _currentMinute = 0;
        private int _currentDay = 1;

        private bool _isPaused = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (_isPaused) return;

            // 调试：N 跳次日；Shift+N 只把时间推到 20:00（不换日、不结算），
            // 用来快速验证「商店打烊 → 只能投出货箱 → 次日结算」这条循环。
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.nKey.wasPressedThisFrame)
            {
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) SkipToHour(20);
                else ForceNextDay();
                return;
            }

            _timer += Time.deltaTime;

            if (_timer >= _realSecondsPerGameMinute)
            {
                _timer -= _realSecondsPerGameMinute;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            _currentMinute++;

            if (_currentMinute >= 60)
            {
                _currentMinute = 0;
                _currentHour++;
                
                if (_currentHour >= 24)
                {
                    StartNewDay();
                }
                else if (_currentHour >= 26) // 02:00 AM next day technically (26 for logic simplicity)
                {
                   // Force sleep logic if implemented
                }
            }

            OnTimeChanged?.Invoke(_currentHour, _currentMinute);
            UpdateLighting();
        }

        public void StartNewDay()
        {
            _currentDay++;
            _currentHour = 6;
            _currentMinute = 0;
            
            Debug.Log($"[TimeManager] Day {_currentDay} Started!");
            OnDayChanged?.Invoke();
            
            // Notify other systems
            // FindObjectOfType<CropManager>()?.OnNewDay(); // Better use event subscription
        }

        private void UpdateLighting()
        {
            if (_globalLight != null && _dayNightGradient != null)
            {
                // Map 0-24h to 0-1f
                float timePercent = (_currentHour + _currentMinute / 60f) / 24f;
                _globalLight.color = _dayNightGradient.Evaluate(timePercent);
            }
        }

        /// <summary>本次跨日是否由「睡觉」触发：睡觉恢复满体力，直接跳日只回一半</summary>
        public bool LastAdvanceWasSleep { get; private set; }

        // For Debug/Testing（N 键走 fromBed=false）
        public void ForceNextDay(bool fromBed = false)
        {
            LastAdvanceWasSleep = fromBed;
            StartNewDay();
        }

        /// <summary>测试用：只把钟点推到指定小时，不换日、不触发结算（Shift+N → 20:00 打烊）</summary>
        public void SkipToHour(int hour)
        {
            _currentHour = Mathf.Clamp(hour, 0, 23);
            _currentMinute = 0;
            LastAdvanceWasSleep = false;
            OnTimeChanged?.Invoke(_currentHour, _currentMinute);
            UpdateLighting();
            Debug.Log($"[TimeManager] 时间跳到 {GetTimeString()}（第 {_currentDay} 天，未换日）");
        }

        /// <summary>当前天数（存档用）</summary>
        public int CurrentDay => _currentDay;

        /// <summary>当前小时（0-23，NPC 作息用）</summary>
        public int CurrentHour => _currentHour;

        /// <summary>读档恢复天数（不触发事件）</summary>
        public void RestoreDay(int day)
        {
            _currentDay = Mathf.Max(1, day);
        }
        
        public string GetTimeString()
        {
            return $"{_currentHour:00}:{_currentMinute:00}";
        }
    }
}
