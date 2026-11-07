using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 玩家金钱管理（存档系统会通过 SetMoney 恢复）
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [Header("Initial Settings")]
        [SerializeField] private int _startingMoney = 500;

        public int Money { get; private set; }

        /// <summary>金钱变化事件（参数为最新余额），HUD/商店订阅</summary>
        public event Action<int> OnMoneyChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Money = _startingMoney;
            }
            else Destroy(gameObject);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            Money += amount;
            OnMoneyChanged?.Invoke(Money);
            Debug.Log($"[Wallet] +{amount} 金钱，当前余额 {Money}");
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Money < amount) return false;
            Money -= amount;
            OnMoneyChanged?.Invoke(Money);
            return true;
        }

        /// <summary>存档恢复专用，不触发日志</summary>
        public void SetMoney(int value)
        {
            Money = Mathf.Max(0, value);
            OnMoneyChanged?.Invoke(Money);
        }
    }
}
