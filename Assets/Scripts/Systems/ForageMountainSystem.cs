using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 采山系统：从「采山入口」进入，按季节采得山货（山珍菌/野蜂蜜/云雾茶/山参）、药材、野味。
    /// 每天 5 次，跨日恢复；稀有山货（山参）概率低。
    /// </summary>
    public class ForageMountainSystem : MonoBehaviour
    {
        public static ForageMountainSystem Instance { get; private set; }

        public const int UsesPerDay = 5;
        public int UsesLeft { get; private set; } = UsesPerDay;
        public int StaminaCost = 8;

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

        private void OnNewDay() => UsesLeft = UsesPerDay;

        /// <summary>采集一次；返回 (物品, 数量)，失败返回 null</summary>
        public (ItemData item, int count)? Forage()
        {
            if (UsesLeft <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice("今日山货已采尽 · 明天再来");
                return null;
            }
            if (PlayerStats.Instance == null || !PlayerStats.Instance.HasEnough(StaminaCost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"采山需 {StaminaCost} 体力");
                return null;
            }

            PlayerStats.Instance.TrySpend(StaminaCost);
            UsesLeft--;
            SkillSystem.Instance?.AddExp(SkillType.Fishing, 4); // 采山算"山野"经验，先归到钓鱼

            var item = RollItem();
            if (item == null) return null;
            int count = 1 + (Random.value < 0.2f ? 1 : 0); // 20% 双份

            if (InventorySystem.Instance != null && !InventorySystem.Instance.AddItem(item, count))
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，山货装不下");
                return null;
            }

            PBV.UI.HUD.Instance?.ShowNotice($"采山：{item.itemName} ×{count}（今日剩 {UsesLeft} 次）");
            Debug.Log($"[Mountain] 采到 {item.itemName} x{count}");
            return (item, count);
        }

        private ItemData RollItem()
        {
            var season = SeasonSystem.Instance != null ? SeasonSystem.Instance.CurrentSeason : Season.Spring;

            // 按季节分配权重：春(茶/菌)、夏(蜜/果)、秋(山参/菌)、冬(药材/野味)
            float r = Random.value;
            string id;
            if (r < 0.04f) id = "item_mountain_herb";      // 4% 山参（珍宝）
            else if (r < 0.18f) id = "item_herb";          // 药材
            else if (r < 0.30f) id = "item_poison_herb";   // 毒草（制毒）
            else if (r < 0.42f) id = "item_game_hare";     // 野味
            else if (r < 0.50f) id = "item_game_pheasant"; // 野味
            else if (r < 0.62f) id = "item_ginger";        // 生姜
            else if (r < 0.74f) id = "item_mountain_fungus"; // 山珍菌
            else if (r < 0.86f) id = "item_mountain_honey";  // 野蜂蜜
            else if (r < 0.95f) id = "item_mountain_tea";    // 云雾茶
            else id = "item_wild_fruit";                   // 野果

            return Find(id);
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
