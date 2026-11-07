using System;
using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.Economy
{
    /// <summary>
    /// 出货箱系统（v0.1.0 MVP）：
    /// - 玩家按 E 交互，将背包内所有可出售物品投入出货箱
    /// - 每日 06:00（TimeManager.StartNewDay）结算总价值，金钱入账
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShippingBinSystem : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [Tooltip("是否连种子一起出售（默认不出售种子和工具）")]
        [SerializeField] private bool _sellSeeds = false;

        /// <summary>已投入未结算的总价值</summary>
        public int PendingValue { get; private set; }

        /// <summary>待结算金额变化事件（参数为最新 PendingValue），HUD 订阅显示</summary>
        public event Action<int> OnPendingChanged;

        /// <summary>存档恢复专用</summary>
        public void SetPendingValue(int value)
        {
            PendingValue = Mathf.Max(0, value);
            OnPendingChanged?.Invoke(PendingValue);
        }

        private InventorySystem _inventory;

        public string GetInteractPrompt() => "出货箱：投入物品（次日 06:00 结算）";

        private void Start()
        {
            _inventory = InventorySystem.Instance;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += SettleDailyEarnings;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= SettleDailyEarnings;
            }
        }

        /// <summary>
        /// IInteractable：投入背包中所有可出售物品
        /// </summary>
        public void OnInteract()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[ShippingBin] 背包系统未就绪");
                return;
            }

            int totalValue = 0;
            int totalItems = 0;

            // 收集后再移除，避免边遍历边修改
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;
                if (!IsSellable(slot.item)) continue;

                totalValue += slot.item.sellPrice * slot.count;
                totalItems += slot.count;
            }

            if (totalItems == 0)
            {
                Debug.Log("[ShippingBin] 没有可出售的物品");
                return;
            }

            // 二次遍历执行移除
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;
                if (!IsSellable(slot.item)) continue;

                _inventory.RemoveItem(slot.item, slot.count);
            }

            PendingValue += totalValue;
            OnPendingChanged?.Invoke(PendingValue);
            Debug.Log($"[ShippingBin] 投入 {totalItems} 件物品，价值 {totalValue}（待结算总额 {PendingValue}，次日06:00入账）");
        }

        private bool IsSellable(ItemData item)
        {
            if (item == null || item.sellPrice <= 0) return false;
            if (item.type == ItemType.Tool) return false;
            if (item.type == ItemType.Seed && !_sellSeeds) return false;
            return true;
        }

        /// <summary>
        /// 每日 06:00 结算
        /// </summary>
        private void SettleDailyEarnings()
        {
            if (PendingValue <= 0) return;

            int settled = PendingValue;
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.AddMoney(PendingValue);
                Debug.Log($"[ShippingBin] 昨日出货结算：+{PendingValue} 金钱");
            }
            PendingValue = 0;
            OnPendingChanged?.Invoke(PendingValue);
        }
    }
}
