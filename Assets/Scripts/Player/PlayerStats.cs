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

        [Header("HP Settings")]
        [SerializeField] private int _maxHp = 100;
        public int MaxHp => _maxHp;
        public int Hp { get; private set; }

        /// <summary>体力变化事件（current, max）</summary>
        public event Action<int, int> OnStaminaChanged;
        /// <summary>血量变化事件（current, max）</summary>
        public event Action<int, int> OnHpChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Stamina = _maxStamina;
                Hp = _maxHp;
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
            // 睡觉回满血，跳日回一半血
            Hp = Mathf.Min(_maxHp, Hp + (slept ? _maxHp : Mathf.Max(1, _maxHp / 2)));
            OnHpChanged?.Invoke(Hp, _maxHp);
            Debug.Log($"[PlayerStats] 新一天（{(slept ? "睡觉" : "直接跳日")}），体力 {Stamina}/{_maxStamina}，血量 {Hp}/{_maxHp}");
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
            // 体力归零 → 晕倒送医婆
            if (Stamina <= 0)
            {
                PBV.Systems.FaintSystem.Instance?.OnExhausted();
            }
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

        // ---- 血量 HP（战斗用，独立于体力） ----

        /// <summary>扣血，返回是否死亡（Hp 归零）</summary>
        public bool TakeDamage(int amount)
        {
            if (amount <= 0) return Hp <= 0;
            Hp = Mathf.Max(0, Hp - amount);
            OnHpChanged?.Invoke(Hp, _maxHp);
            Debug.Log($"[PlayerStats] 受到 {amount} 伤害，HP {Hp}/{_maxHp}");
            return Hp <= 0;
        }

        /// <summary>回血（吃药/睡觉），不超过上限</summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            Hp = Mathf.Min(_maxHp, Hp + amount);
            OnHpChanged?.Invoke(Hp, _maxHp);
        }

        /// <summary>回满血（睡觉）</summary>
        public void RestoreFullHp()
        {
            Hp = _maxHp;
            OnHpChanged?.Invoke(Hp, _maxHp);
        }

        /// <summary>存档恢复血量</summary>
        public void SetHp(int value)
        {
            Hp = Mathf.Clamp(value, 0, _maxHp);
            OnHpChanged?.Invoke(Hp, _maxHp);
        }
    }
}
