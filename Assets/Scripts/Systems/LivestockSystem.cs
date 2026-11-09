using System;
using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>牲畜种类</summary>
    public enum LivestockType
    {
        Chicken,   // 鸡：产鸡蛋，喂玉米/蔬菜
        Duck,      // 鸭：产鸭蛋
        Cow,       // 牛：产牛奶，喂草料/玉米
        Sheep,     // 羊：产羊毛，喂草料
        Pig        // 猪：养肥屠宰得猪肉，喂蔬菜/玉米
    }

    /// <summary>牲畜数据（代码内置）</summary>
    [Serializable]
    public class LivestockDef
    {
        public LivestockType type;
        public string name;
        public int buyCost;         // 购买价（宋币/文）
        public string productId;    // 每日产出物品 id（蛋/奶/毛）
        public string meatId;       // 屠宰得的肉 id
        public string feedId;       // 主要饲料 id
    }

    /// <summary>玩家拥有的牲畜（可存档）</summary>
    [Serializable]
    public class OwnedLivestock
    {
        public int type;            // LivestockType 枚举值
        public int count;
        public int fedDays;         // 已喂食天数（连续喂食，喂满可屠宰得更多肉）
    }

    /// <summary>
    /// 养殖系统（单例）：管理鸡/鸭/牛/羊/猪的购买、喂食、跨日产蛋奶、屠宰得肉。
    /// 需要喂食才持续产蛋奶（消耗作物/草料），屠宰得肉。
    /// </summary>
    public class LivestockSystem : MonoBehaviour
    {
        public static LivestockSystem Instance { get; private set; }

        private readonly List<OwnedLivestock> _owned = new List<OwnedLivestock>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        /// <summary>牲畜定义表</summary>
        public static LivestockDef[] Defs => new[]
        {
            new LivestockDef { type = LivestockType.Chicken, name = "鸡", buyCost = 200, productId = "item_egg", meatId = "item_chicken_meat", feedId = "item_corn" },
            new LivestockDef { type = LivestockType.Duck, name = "鸭", buyCost = 220, productId = "item_duck_egg", meatId = "item_chicken_meat", feedId = "item_corn" },
            new LivestockDef { type = LivestockType.Cow, name = "牛", buyCost = 600, productId = "item_milk", meatId = "item_beef", feedId = "item_straw" },
            new LivestockDef { type = LivestockType.Sheep, name = "羊", buyCost = 500, productId = "item_wool", meatId = "item_mutton", feedId = "item_straw" },
            new LivestockDef { type = LivestockType.Pig, name = "猪", buyCost = 450, productId = null, meatId = "item_pork", feedId = "item_corn" },
        };

        public static LivestockDef GetDef(LivestockType t)
        {
            foreach (var d in Defs) if (d.type == t) return d;
            return null;
        }

        /// <summary>拥有的牲畜列表（只读）</summary>
        public IReadOnlyList<OwnedLivestock> Owned => _owned;

        private OwnedLivestock GetOwned(LivestockType t)
        {
            foreach (var o in _owned) if ((LivestockType)o.type == t) return o;
            return null;
        }

        /// <summary>某牲畜当前拥有数量</summary>
        public int GetOwnedCount(LivestockType t)
        {
            var o = GetOwned(t);
            return o != null ? o.count : 0;
        }

        /// <summary>购买一只牲畜</summary>
        public bool Buy(LivestockType t)
        {
            var def = GetDef(t);
            var wallet = PlayerWallet.Instance;
            if (wallet == null || !wallet.SpendSong(def.buyCost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"买 {def.name} 需 {def.buyCost} 文");
                return false;
            }

            var owned = GetOwned(t);
            if (owned == null)
            {
                owned = new OwnedLivestock { type = (int)t, count = 0, fedDays = 0 };
                _owned.Add(owned);
            }
            owned.count++;
            PBV.UI.HUD.Instance?.ShowNotice($"买了一只 {def.name}（共 {owned.count} 只）");
            return true;
        }

        /// <summary>喂食：消耗 1 份饲料，标记本日已喂。喂食后跨日才产蛋奶。</summary>
        public bool Feed(LivestockType t)
        {
            var def = GetDef(t);
            var owned = GetOwned(t);
            if (owned == null || owned.count <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"还没有养 {def.name}");
                return false;
            }

            // 猪不产蛋奶，喂食只影响屠宰收益
            var inv = InventorySystem.Instance;
            var feed = Find(def.feedId);
            if (feed == null || inv == null || inv.CountItem(feed) < 1)
            {
                string alt = def.feedId == "item_corn" ? "玉米或蔬菜" : "稻草或蔬菜";
                PBV.UI.HUD.Instance?.ShowNotice($"喂 {def.name} 需要 {alt}");
                return false;
            }

            inv.RemoveItem(feed, 1);
            owned.fedDays = Mathf.Min(owned.fedDays + 1, 3);
            PBV.UI.HUD.Instance?.ShowNotice($"喂了 {def.name}（连续喂食 {owned.fedDays} 天）· 明早收产物");
            return true;
        }

        /// <summary>屠宰：得肉（喂食天数越多肉越多）。鸡鸭屠宰得鸡肉。</summary>
        public bool Slaughter(LivestockType t)
        {
            var def = GetDef(t);
            var owned = GetOwned(t);
            if (owned == null || owned.count <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"还没有养 {def.name}");
                return false;
            }

            int yield = 1 + (owned.fedDays >= 2 ? 1 : 0); // 连续喂 2 天以上多得 1 块肉
            var inv = InventorySystem.Instance;
            var meat = Find(def.meatId);
            if (meat == null || inv == null || !inv.AddItem(meat, yield))
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，屠宰取消");
                return false;
            }

            owned.count--;
            owned.fedDays = 0;
            PBV.UI.HUD.Instance?.ShowNotice($"屠宰 {def.name}，得 {meat.itemName} ×{yield}");
            return true;
        }

        /// <summary>剪羊毛：羊每日产羊毛，跨日结算时直接给</summary>
        private void OnNewDay()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            foreach (var owned in _owned)
            {
                if (owned.count <= 0) continue;
                var def = GetDef((LivestockType)owned.type);
                if (def.productId == null) continue; // 猪不产蛋奶

                // 没喂食不产
                if (owned.fedDays <= 0) continue;

                var product = Find(def.productId);
                int amount = owned.count; // 每只每天产 1 份
                if (product != null && inv.AddItem(product, amount))
                {
                    PBV.UI.HUD.Instance?.ShowNotice($"【养殖】{def.name} ×{owned.count} 产 {product.itemName} ×{amount}", 3f);
                    SkillSystem.Instance?.AddExp(SkillType.Farming, 5);
                }
                else
                {
                    PBV.UI.HUD.Instance?.ShowNotice($"背包满，{def.name} 的产物未能收取", 3f);
                }
                owned.fedDays = 0; // 每日结算后重置，需重新喂食
            }
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        // ===== 存档 =====
        public List<OwnedLivestock> Export() => new List<OwnedLivestock>(_owned);
        public void Import(List<OwnedLivestock> data)
        {
            _owned.Clear();
            if (data != null) _owned.AddRange(data);
        }
    }
}
