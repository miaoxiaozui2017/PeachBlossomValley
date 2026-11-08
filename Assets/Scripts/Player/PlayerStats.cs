using System;
using UnityEngine;
using PBV.Systems;

namespace PBV.Player
{
    /// <summary>
    /// 玩家体力系统：锄地/浇水/播种/收获消耗体力，跨日恢复满值。
    /// 体力不足时无法执行工具操作。存档通过 SetStamina 恢复。
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Stamina Settings")]
        [SerializeField] private int _maxStamina = 100;
        [Tooltip("每日 06:00 跨日时恢复的体力（-1 表示恢复满）")]
        [SerializeField] private int _dailyRestore = -1;

        public int MaxStamina => _maxStamina;
        public int Stamina { get; private set; }

        /// <summary>体力变化事件（current, max）</summary>
        public event Action<int, int> OnStaminaChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Stamina = _maxStamina;
            }
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // 跨日恢复体力
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += OnNewDay;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= OnNewDay;
            }
        }

        private void OnNewDay()
        {
            // 睡觉恢复满；按 N 直接跳日只回一半，让「睡觉」真正有玩法价值
            bool slept = TimeManager.Instance != null && TimeManager.Instance.LastAdvanceWasSleep;
            int daily = _dailyRestore < 0 ? _maxStamina : _dailyRestore;
            int amount = slept ? _maxStamina : Mathf.Max(1, daily / 2);
            Stamina = Mathf.Min(_maxStamina, Stamina + amount);
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
            Debug.Log($"[PlayerStats] 新一天（{(slept ? "睡觉" : "直接跳日")}），体力 {Stamina}/{_maxStamina}");
        }

        /// <summary>体力是否足够（工具动作执行前先查询，避免"先做完再扣不动"）</summary>
        public bool HasEnough(int cost) => cost <= 0 || Stamina >= cost;

        /// <summary>尝试消耗体力，不足返回 false</summary>
        public bool TrySpend(int cost)
        {
            if (cost <= 0) return true;
            if (Stamina < cost)
            {
                Debug.Log($"[PlayerStats] 体力不足：需要 {cost}，当前 {Stamina}");
                return false;
            }
            Stamina -= cost;
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
            return true;
        }

        public void RestoreFull()
        {
            Stamina = _maxStamina;
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
        }

        /// <summary>恢复指定量体力（烹饪用）</summary>
        public void Restore(int amount)
        {
            Stamina = Mathf.Clamp(Stamina + amount, 0, _maxStamina);
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
        }

        /// <summary>扩建体力上限（建造系统用）</summary>
        public void ExpandMaxStamina(int newMax)
        {
            _maxStamina = newMax;
            if (Stamina > _maxStamina) Stamina = _maxStamina;
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
        }

        /// <summary>存档恢复专用</summary>
        public void SetStamina(int value)
        {
            Stamina = Mathf.Clamp(value, 0, _maxStamina);
            OnStaminaChanged?.Invoke(Stamina, _maxStamina);
        }
    }
}
