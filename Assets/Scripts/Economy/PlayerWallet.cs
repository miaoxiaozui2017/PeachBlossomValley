using System;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 双币种钱包：
    /// - 宋币（SongCoins）：桃花谷 / 宋代货币，单位「文」，沉浸式游玩不用 ¥。
    /// - 人民币（RMB）：现代世界货币，单位「元」，符号 ¥。
    /// 两者通过传送门「时空驿站」按汇率兑换（见 ExchangeSystem）。
    /// 存档系统通过 SetSong / SetRMB 恢复。
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [Header("Initial Settings")]
        [SerializeField] private int _startingSong = 500;   // 开局给宋币（桃花谷起步）

        public int SongCoins { get; private set; }           // 宋币（文）
        public int RMB { get; private set; }                 // 人民币（元）

        /// <summary>金钱变化事件（参数为最新 宋币 / 人民币 余额），HUD/商店订阅</summary>
        public event Action<int, int> OnMoneyChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SongCoins = _startingSong;
            }
            else Destroy(gameObject);
        }

        // ---- 宋币（文） ----
        public void AddSong(int amount)
        {
            if (amount <= 0) return;
            SongCoins += amount;
            OnMoneyChanged?.Invoke(SongCoins, RMB);
            Debug.Log($"[Wallet] +{amount} 宋币，当前余额 宋{SongCoins}文 / ¥{RMB}");
        }

        public bool SpendSong(int amount)
        {
            if (amount <= 0 || SongCoins < amount) return false;
            SongCoins -= amount;
            OnMoneyChanged?.Invoke(SongCoins, RMB);
            return true;
        }

        // ---- 人民币（元） ----
        public void AddRMB(int amount)
        {
            if (amount <= 0) return;
            RMB += amount;
            OnMoneyChanged?.Invoke(SongCoins, RMB);
            Debug.Log($"[Wallet] +¥{amount}，当前余额 宋{SongCoins}文 / ¥{RMB}");
        }

        public bool SpendRMB(int amount)
        {
            if (amount <= 0 || RMB < amount) return false;
            RMB -= amount;
            OnMoneyChanged?.Invoke(SongCoins, RMB);
            return true;
        }

        /// <summary>存档恢复专用，不触发日志</summary>
        public void SetSong(int value)
        {
            SongCoins = Mathf.Max(0, value);
            OnMoneyChanged?.Invoke(SongCoins, RMB);
        }

        public void SetRMB(int value)
        {
            RMB = Mathf.Max(0, value);
            OnMoneyChanged?.Invoke(SongCoins, RMB);
        }
    }
}
