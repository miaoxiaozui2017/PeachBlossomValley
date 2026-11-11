using System;
using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.Economy
{
    /// <summary>商店归属世界：古今不通用，切世界时整店显隐</summary>
    public enum ShopWorld { Ancient, Modern }

    /// <summary>商店商品条目</summary>
    [Serializable]
    public class ShopEntry
    {
        public ItemData item;
        public int price;
        [Tooltip("货币：Song=宋币（文） / RMB=人民币（¥）。桃花谷店铺用宋币，现代店铺用人民币")]
        public Currency currency = Currency.Song;
        [Tooltip("-1 = 无限库存")]
        public int stock = -1;
    }

    /// <summary>
    /// 商店系统：只列「本店可买的东西」，买卖当场结算，有营业时间，价格随商人好感浮动。
    /// - 古今不通用：ShopWorld 决定这家店属于桃花谷还是现代，切世界时整店隐藏。
    /// - 卖货不放在商店面板：面板只列货架；玩家按 Tab 打开背包选中物品，
    ///   在背包信息栏看价格，点商店面板「卖选中」完成交易（见 ShopUI / InventoryUI）。
    /// - 背包里一切物品可售卖（每个 ItemData 都直接给过 sellPrice）。
    /// - 营业 06:00-18:00；打烊后不能交易，卖货只能投出货箱、次日 06:00 结算。
    /// - 好感越高：买越便宜（挚友 8 折）、卖越贵（挚友 +30%）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShopSystem : MonoBehaviour, IInteractable
    {
        [Header("Shop Info")]
        [SerializeField] private string _shopName = "杂货铺";
        [SerializeField] private ShopWorld _world = ShopWorld.Ancient;

        [Header("营业时间（含起始、不含结束）")]
        [SerializeField] private int _openHour = 6;
        [SerializeField] private int _closeHour = 18;

        [Header("商人名称（留空则按建筑自动推断）")]
        [SerializeField] private string _merchantNameOverride = "";

        [Header("本店货架（只列可买的东西）")]
        [SerializeField] private ShopEntry[] _items;

        private const float MaxBuyDiscount = 0.20f;
        private const float MaxSellPremium = 0.30f;

        /// <summary>
        /// 运行时新建的店（尤其现代店）自我对齐世界：若不属于当前世界就立刻隐藏。
        /// 显示的职责归 ShopWorldBootstrap，这里只负责「补建时序晚于 bootstrap」时不漏隐藏。
        /// </summary>
        private void Start()
        {
            if (!IsAvailable) gameObject.SetActive(false);
        }

        public string ShopName => _shopName;
        public ShopWorld World => _world;
        public int OpenHour => _openHour;
        public int CloseHour => _closeHour;

        /// <summary>本店是否属于当前世界</summary>
        public bool IsAvailable => WorldState.Instance == null ||
            ((WorldState.Instance.Current == WorldType.Modern) == (_world == ShopWorld.Modern));

        /// <summary>本店收付币种：现代店 ¥，桃花谷店 文</summary>
        public bool IsRMBShop => _world == ShopWorld.Modern;
        public Currency ShopCurrency => IsRMBShop ? Currency.RMB : Currency.Song;

        /// <summary>货架：每店独立，不再按世界切换（古今不通用）</summary>
        public ShopEntry[] Items => _items;

        /// <summary>商人名字：与好感系统共用 key</summary>
        public string MerchantName
        {
            get
            {
                if (!string.IsNullOrEmpty(_merchantNameOverride)) return _merchantNameOverride;
                switch (gameObject.name)
                {
                    case "TavernStation": return "饱饱（酒楼大厨）";
                    case "FarmVendorStation": return "老农";
                    case "ModernShop": return "现代店员";
                    case "AntiqueShop": return "古玩店老板";
                    default: return "商人";
                }
            }
        }

        public bool IsOpenNow
        {
            get
            {
                var tm = TimeManager.Instance;
                if (tm == null) return true;
                return tm.CurrentHour >= _openHour && tm.CurrentHour < _closeHour;
            }
        }

        public bool CanTrade => IsOpenNow && IsAvailable;

        public int Affection => RelationshipSystem.Instance != null
            ? RelationshipSystem.Instance.GetAffection(MerchantName) : 0;

        public AffectionLevel AffectionLevel => RelationshipSystem.Instance != null
            ? RelationshipSystem.Instance.GetLevel(MerchantName) : AffectionLevel.Stranger;

        public string AffectionLevelName => RelationshipSystem.LevelToName(AffectionLevel);
        public float BuyMultiplier => 1f - MaxBuyDiscount * (Affection / 100f);
        public float SellMultiplier => 1f + MaxSellPremium * (Affection / 100f);

        public int GetBuyPrice(ShopEntry entry)
        {
            if (entry == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(entry.price * BuyMultiplier));
        }

        /// <summary>单个物品的回收价（含好感溢价 + 今日事件「货商过境」+20%）</summary>
        public int GetSellUnitPrice(ItemData item)
        {
            if (item == null) return 0;
            float eventMult = 1f + (RandomEventSystem.Instance != null ? RandomEventSystem.Instance.ShopBonus : 0f);
            return Mathf.Max(1, Mathf.RoundToInt(item.sellPrice * SellMultiplier * eventMult));
        }

        public string CurrencySymbol => IsRMBShop ? "¥" : "";
        public string CurrencyUnit => IsRMBShop ? "" : " 文";

        /// <summary>运行时追加商品（幂等）</summary>
        public void AddShopEntry(ItemData item, int price, Currency currency = Currency.Song)
        {
            if (item == null) return;
            if (_items != null)
                foreach (var e in _items)
                    if (e != null && e.item == item) return;
            var list = new System.Collections.Generic.List<ShopEntry>();
            if (_items != null) list.AddRange(_items);
            list.Add(new ShopEntry { item = item, price = price, currency = currency, stock = -1 });
            _items = list.ToArray();
        }

        /// <summary>兼容旧调用：人民币货架条目</summary>
        public void AddModernEntry(ItemData item, int price) => AddShopEntry(item, price, Currency.RMB);

        /// <summary>卖出指定数量，当场结算</summary>
        public bool TrySell(ItemData item, int count)
        {
            if (!CheckOpen()) return false;
            if (item == null || count <= 0) return false;
            if (!ShippingBinSystem.CanSell(item))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{item.itemName} 不可售卖");
                return false;
            }
            var inv = InventorySystem.Instance;
            var wallet = PlayerWallet.Instance;
            if (inv == null || wallet == null) return false;

            int have = inv.CountItem(item);
            int n = Mathf.Min(count, have);
            if (n <= 0) return false;

            int total = GetSellUnitPrice(item) * n;
            inv.RemoveItem(item, n);
            if (IsRMBShop) wallet.AddRMB(total); else wallet.AddSong(total);
            PBV.UI.HUD.Instance?.ShowNotice(
                $"卖给{_shopName}：{item.itemName} ×{n} → 当场到账 {CurrencySymbol}{total}{CurrencyUnit}（好感 +{Mathf.RoundToInt((SellMultiplier - 1f) * 100f)}%）");
            Debug.Log($"[Shop] 卖 {item.itemName} x{n} +{CurrencySymbol}{total}");
            return true;
        }

        /// <summary>一键卖出背包里全部物品，当场结算并只弹一条汇总提示</summary>
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
                PBV.UI.HUD.Instance?.ShowNotice("背包是空的");
                return false;
            }

            foreach (var kv in toSell) inv.RemoveItem(kv.Key, kv.Value);
            if (IsRMBShop) wallet.AddRMB(totalMoney); else wallet.AddSong(totalMoney);
            PBV.UI.HUD.Instance?.ShowNotice($"全部卖出 {totalCount} 件 → 当场到账 {CurrencySymbol}{totalMoney}{CurrencyUnit}");
            Debug.Log($"[Shop] 全部卖出 {totalCount} 件 +{CurrencySymbol}{totalMoney}");
            return true;
        }

        /// <summary>G 键送礼给掌柜（商人融合在建筑里，没有独立 NPCController）</summary>
        public void GiftToMerchant()
        {
            if (!IsAvailable) return;
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
            PBV.UI.ShopUI.Instance?.RefreshIfOpen();
        }

        private bool CheckOpen()
        {
            if (!IsAvailable)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这家店不在当前世界");
                return false;
            }
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
            if (!IsAvailable) return;
            if (!IsOpenNow)
                PBV.UI.HUD.Instance?.ShowNotice(
                    $"{_shopName}已打烊（{_openHour:00}:00-{_closeHour:00}:00）· 买卖停用，请把货投进出货箱，次日 06:00 结算");
            PBV.UI.ShopUI.Instance?.OpenShop(this);
        }
    }
}
