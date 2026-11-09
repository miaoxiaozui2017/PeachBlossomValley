using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 野地采集点：E 键采集，每天 3 次，跨日恢复。
    /// SpotIndex 0 = 野地（野菜/野果/木材/树皮/蘑菇），1 = 林间（木材/树皮/药材/蘑菇/野果）。
    /// 这是「食」「住」两条线的基础原料来源，让玩家不买种子也能起步。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ForageSpot : MonoBehaviour, IInteractable
    {
        public int SpotIndex;

        private const int UsesPerDay = 3;
        private int _usesLeft = UsesPerDay;

        private static readonly string[][] Pools =
        {
            // 野地：野菜/野果为主，偶尔木材树皮
            new[] { "item_wild_veg", "item_wild_veg", "item_wild_fruit", "item_wild_fruit", "item_wood", "item_bark", "item_mushroom" },
            // 林间：木材/药材为主
            new[] { "item_wood", "item_wood", "item_bark", "item_herb", "item_herb", "item_mushroom", "item_wild_fruit" },
        };

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay() => _usesLeft = UsesPerDay;

        public string GetInteractPrompt()
            => _usesLeft > 0 ? $"采集：野菜野果药材木材（今日剩 {_usesLeft} 次）(E)" : "今日已采尽，明日再来 (E)";

        public void OnInteract()
        {
            if (_usesLeft <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这处今日已采尽 · 睡一觉明天再来");
                return;
            }

            var inv = InventorySystem.Instance;
            if (inv == null) return;

            var pool = Pools[Mathf.Clamp(SpotIndex, 0, Pools.Length - 1)];
            var item = Find(pool[Random.Range(0, pool.Length)]);
            if (item == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("采集物资产还没加载好 · 稍后再试");
                return;
            }

            if (!inv.AddItem(item, 1))
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，采不下（东西还留在原地）");
                return;
            }

            _usesLeft--;
            SkillSystem.Instance?.AddExp(SkillType.Farming, 2);
            PBV.UI.HUD.Instance?.ShowNotice($"采集：{item.itemName} ×1（今日剩 {_usesLeft} 次）");
            Debug.Log($"[Forage] 采集点{SpotIndex} → {item.itemName}，剩 {_usesLeft} 次");
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
