using System;
using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.Economy
{
    /// <summary>商店商品条目</summary>
    [Serializable]
    public class ShopEntry
    {
        public ItemData item;
        public int price;
        [Tooltip("货币：Song=宋币（文） / RMB=人民币（¥）。桃花谷货架用宋币，现代货架用人民币")]
        public Currency currency = Currency.Song;
        [Tooltip("-1 = 无限库存")]
        public int stock = -1;
    }

    /// <summary>
    /// 商店系统：买卖当场结算，有营业时间，价格随「商人好感度」浮动。
    /// - 营业时间默认 06:00-18:00（与 NPC 作息一致）；打烊后不能交易，卖货只能投出货箱、次日 06:00 结算。
    /// - 好感越高：买越便宜（挚友 8 折）、卖越贵（挚友 +30%）。
    /// - 商人不是独立 NPC（融合在建筑里），G 送礼直接送给建筑，好感按 MerchantName 存进好感系统。
    /// - 商店不乘现代 ×2 / 节日 ×1.5，出货箱仍是高价渠道，两条渠道互补。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShopSystem : MonoBehaviour, IInteractable
    {
        [Header("Shop Info")]
        [SerializeField] private string _shopName = "杂货铺";

        [Header("营业时间（含起始、不含结束）")]
        [SerializeField] private int _openHour = 6;
        [SerializeField] private int _closeHour = 18;

        [Header("商人名称（留空则按建筑自动推断）")]
        [SerializeField] private string _merchantNameOverride = "";

        [Header("商品列表（桃花谷货架 · 宋币）")]
        [SerializeField] private ShopEntry[] _items;

        [Header("现代货架（人民币 · 仅在现代世界显示）")]
        [SerializeField] private ShopEntry[] _modernItems;

        // 好感度对价格的影响上限：挚友 买 8 折 / 卖 +30%
        private const float MaxBuyDiscount = 0.20f;
        private const float MaxSellPremium = 0.30f;

        public string ShopName => _shopName;
        public int OpenHour => _openHour;
        public int CloseHour => _closeHour;

        /// <summary>根据当前世界返回对应货架：现代世界显示人民币商品，否则宋币商品</summary>
        public ShopEntry[] Items
        {
            get
            {
                bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
                return modern ? (_modernItems ?? _items) : _items;
            }
        }

        /// <summary>商人名字：与好感系统共用 key，杂货铺=商人 / 酒楼=饱饱 / 老农小摊=老农</summary>
        public string MerchantName
        {
            get
            {
                if (!string.IsNullOrEmpty(_merchantNameOverride)) return _merchantNameOverride;
                switch (gameObject.name)
                {
                    case "TavernStation": return "饱饱（酒楼大厨）";
                    case "FarmVendorStation": return "老农";
                    default: return "商人";
                }
            }
        }

        /// <summary>当前是否营业（TimeManager 从 06:00 起算，24:00 换日）</summary>
        public bool IsOpenNow
        {
            get
            {
                var tm = TimeManager.Instance;
                if (tm == null) return true; // 时间系统未就绪时不拦交易
                return tm.CurrentHour >= _openHour && tm.CurrentHour < _closeHour;
            }
        }

        public int Affection => RelationshipSystem.Instance != null
            ? RelationshipSystem.Instance.GetAffection(MerchantName) : 0;

        public AffectionLevel AffectionLevel => RelationshipSystem.Instance != null
            ? RelationshipSystem.Instance.GetLevel(MerchantName) : AffectionLevel.Stranger;

        public string AffectionLevelName => RelationshipSystem.LevelToName(AffectionLevel);

        /// <summary>买价系数：好感 0 → 1.00，好感 100 → 0.80</summary>
        public float BuyMultiplier => 1f - MaxBuyDiscount * (Affection / 100f);

        /// <summary>卖价系数：好感 0 → 1.00，好感 100 → 1.30</summary>
        public float SellMultiplier => 1f + MaxSellPremium * (Affection / 100f);

        public int GetBuyPrice(ShopEntry entry)
        {
            if (entry == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(entry.price * BuyMultiplier));
        }

        /// <summary>商店单个物品的回收价（含好感溢价 + 今日事件「货商过境」+20%）</summary>
        public int GetSellUnitPrice(ItemData item)
        {
            if (item == null) return 0;
            float eventMult = 1f + (RandomEventSystem.Instance != null ? RandomEventSystem.Instance.ShopBonus : 0f);
            return Mathf.Max(1, Mathf.RoundToInt(item.sellPrice * SellMultiplier * eventMult));
        }

        /// <summary>运行时追加桃花谷商品（内容扩展用，幂等）</summary>
        public void AddShopEntry(ItemData item, int price)
        {
            if (item == null) return;
            if (_items != null)
                foreach (var e in _items)
                    if (e != null && e.item == item) return;
            var list = new System.Collections.Generic.List<ShopEntry>();
            if (_items != null) list.AddRange(_items);
            list.Add(new ShopEntry { item = item, price = price, stock = -1 });
            _items = list.ToArray();
        }

        /// <summary>运行时追加现代货架商品（人民币，幂等）</summary>
        public void AddModernEntry(ItemData item, int price)
        {
            if (item == null) return;
            if (_modernItems != null)
                foreach (var e in _modernItems)
                    if (e != null && e.item == item) return;
            var list = new System.Collections.Generic.List<ShopEntry>();
            if (_modernItems != null) list.AddRange(_modernItems);
            list.Add(new ShopEntry { item = item, price = price, currency = Currency.RMB, stock = -1 });
            _modernItems = list.ToArray();
        }

        /// <summary>营业中才能交易（供 UI 判断按钮是否可用）</summary>
        public bool CanTrade => IsOpenNow;

        /// <summary>卖出指定数量，当场结算（不乘现代×2 / 节日×1.5，那是出货箱的倍率）</summary>
        public bool TrySell(ItemData item, int count)
        {
            if (!CheckOpen()) return false;
            if (item == null || count <= 0) return false;
            if (!ShippingBinSystem.CanSell(item))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{item.itemName} 不收 · 工具 / 种子 / 无售价材料不能卖");
                return false;
            }
            var inv = InventorySystem.Instance;
            var wallet = PlayerWallet.Instance;
            if (inv == null || wallet == null) return false;

            int have = inv.CountItem(item);
            int n = Mathf.Min(count, have);
            if (n <= 0) return false;

            int unit = GetSellUnitPrice(item);
            int total = unit * n;
            inv.RemoveItem(item, n);

            bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            if (modern) wallet.AddRMB(total); else wallet.AddSong(total);
            string sym = modern ? "¥" : "";
            string unitStr = modern ? "" : " 文";
            PBV.UI.HUD.Instance?.ShowNotice(
                $"卖出 {item.itemName} ×{n} → 当场到账 {sym}{total}{unitStr}（含{MerchantName}好感 +{Mathf.RoundToInt((SellMultiplier - 1f) * 100f)}%）");
            Debug.Log($"[Shop] 卖 {item.itemName} x{n} +{sym}{total}");
            return true;
        }

        /// <summary>一键卖出背包里全部可售物品，当场结算并只弹一条汇总提示</summary>
        public bool TrySellAll(out int totalCount, out int totalMoney)
        {
            totalCount = 0; totalMoney = 0;
            if (!CheckOpen()) return false;
            var inv = InventorySystem.Instance;
            var wallet = PlayerWallet.Instance;
            if (inv == null || wallet == null) return false;

            var toSell = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ItemData, int>>();
            for (int i = 0; i < inv.Capacity; i++)
            {
                var slot = inv.GetSlot(i);
                if (slot == null || slot.IsEmpty || !ShippingBinSystem.CanSell(slot.item)) continue;
                toSell.Add(new System.Collections.Generic.KeyValuePair<ItemData, int>(slot.item, slot.count));
                totalMoney += GetSellUnitPrice(slot.item) * slot.count;
                totalCount += slot.count;
            }
            if (totalCount == 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包里没有可卖物品 · 工具 / 种子 / 无售价材料不收");
                return false;
            }

            foreach (var kv in toSell) inv.RemoveItem(kv.Key, kv.Value);
            bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            if (modern) wallet.AddRMB(totalMoney); else wallet.AddSong(totalMoney);
            string sym = modern ? "¥" : "";
            string unit = modern ? "" : " 文";
            PBV.UI.HUD.Instance?.ShowNotice(
                $"全部卖出 {totalCount} 件 → 当场到账 {sym}{totalMoney}{unit}（含好感 +{Mathf.RoundToInt((SellMultiplier - 1f) * 100f)}%）");
            Debug.Log($"[Shop] 全部卖出 {totalCount} 件 +{sym}{totalMoney}");
            return true;
        }

        /// <summary>G 键送礼给商人（商人融合在建筑里，没有独立 NPCController）</summary>
        public void GiftToMerchant()
        {
            var inv = InventorySystem.Instance;
            if (inv == null || RelationshipSystem.Instance == null) return;
            int idx = PBV.UI.InventoryUI.SelectedSlotIndex;
            var slot = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("先用数字键 1-0 选中要送的物品，再按 G");
                return;
            }
            var item = slot.item;
            if (item.affectionValue <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{item.itemName} 不适合送礼");
                return;
            }
            RelationshipSystem.Instance.TryGift(MerchantName, item);
            inv.RemoveItem(item, 1);
            PBV.UI.ShopUI.Instance?.RefreshIfOpen(); // 面板开着就立刻刷新折扣
        }

        private bool CheckOpen()
        {
            if (IsOpenNow) return true;
            PBV.UI.HUD.Instance?.ShowNotice(
                $"{_shopName}已打烊（营业 {_openHour:00}:00-{_closeHour:00}:00）· 请把货投进出货箱，次日 06:00 结算");
            return false;
        }

        public string GetInteractPrompt()
        {
            string state = IsOpenNow
                ? $"营业中 {_openHour:00}:00-{_closeHour:00}:00"
                : "已打烊 · 请走出货箱（次日结算）";
            string price = $"买 {BuyMultiplier * 10f:0.0}折 / 卖 +{Mathf.RoundToInt((SellMultiplier - 1f) * 100f)}%";
            return $"{_shopName} · {state} · {MerchantName}[{AffectionLevelName}] {price} (E)";
        }

        public void OnInteract()
        {
            // 打烊也允许开面板：显示营业时间与好感，买卖按钮统一禁用，避免玩家不知道为什么不能交易
            if (!IsOpenNow)
                PBV.UI.HUD.Instance?.ShowNotice(
                    $"{_shopName}已打烊（{_openHour:00}:00-{_closeHour:00}:00）· 买卖停用，请把货投进出货箱，次日 06:00 结算");
            PBV.UI.ShopUI.Instance?.OpenShop(this);
        }
    }
}
