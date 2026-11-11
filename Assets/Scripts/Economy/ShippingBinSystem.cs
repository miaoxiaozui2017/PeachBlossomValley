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
        /// <summary>已投入未结算的总价值</summary>
        public int PendingValue { get; private set; }

        /// <summary>待结算金额对应的货币（投入那一刻按世界锁定）：true=人民币，false=宋币</summary>
        public bool PendingIsRMB { get; private set; }

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

            // 价格倍率在"投入的这一刻"锁定：穿越到现代后投入即享 ×2，
            // 不必等到次日 06:00 还得恰好人在现代，规则更直观。
            bool isModern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            PendingIsRMB = isModern; // 待结算币种也一并锁定
            float worldMult = WorldState.Instance != null ? WorldState.Instance.SellMultiplier : 1f;
            float festMult = FestivalSystem.Instance != null ? FestivalSystem.Instance.PriceMultiplier : 1f;
            float repMult = ReputationSystem.Instance != null ? ReputationSystem.Instance.SellMultiplier : 1f;
            float buffMult = 1f + (BuffSystem.Instance != null ? BuffSystem.Instance.TradeBonus : 0f);

            // 收集后再移除，避免边遍历边修改
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;
                if (!IsSellable(slot.item)) continue;

                totalValue += Mathf.RoundToInt(slot.item.sellPrice * slot.count * worldMult * festMult * repMult * buffMult);
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
            PBV.Systems.QuestManager.Instance?.RecordShip(totalItems);
            string bonus = "";
            if (worldMult > 1f) bonus += "现代×2 ";
            if (festMult > 1f) bonus += "节日×1.5 ";
            string sym = PendingIsRMB ? "¥" : "";
            int display = PendingIsRMB ? Mathf.RoundToInt(totalValue / (float)PBV.Systems.ExchangeSystem.SONG_PER_RMB) : totalValue;
            PBV.UI.HUD.Instance?.ShowNotice($"出货 {totalItems} 件 → 待结算 {sym}{display}（{bonus}次日 06:00 入账）");
            Debug.Log($"[ShippingBin] 投入 {totalItems} 件物品，价值 {totalValue}（待结算总额 {PendingValue}，次日06:00入账）");
        }

        /// <summary>
        /// 统一出售规则：**背包里一切物品可售卖**（工具/种子/材料同样能卖）。
        /// 每个 ItemData 资产都已直接给定 sellPrice，所以只需判非空与有价即可。
        /// </summary>
        public static bool CanSell(ItemData item) => item != null && item.sellPrice > 0;

        private bool IsSellable(ItemData item) => CanSell(item);

        /// <summary>
        /// 每日 06:00 结算
        /// </summary>
        private void SettleDailyEarnings()
        {
            if (PendingValue <= 0) return;

            // 倍率已在投入时结算，次日只负责入账，避免重复乘算
            int earnings = PendingValue;
            bool rmb = PendingIsRMB;
            if (PlayerWallet.Instance != null)
            {
                if (rmb) PlayerWallet.Instance.AddRMB(Mathf.RoundToInt(earnings / (float)PBV.Systems.ExchangeSystem.SONG_PER_RMB));
                else PlayerWallet.Instance.AddSong(earnings);
                Debug.Log($"[ShippingBin] 昨日出货结算：+{(rmb ? "¥" : "")}{earnings}（{ (rmb ? "人民币" : "宋币") }）");
            }
            PendingValue = 0;
            PendingIsRMB = false;
            OnPendingChanged?.Invoke(PendingValue);
        }
    }
}
