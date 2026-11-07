using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 矿洞系统：管理矿石物品，提供挖矿逻辑。
    /// 矿石物品在 SceneBuilder 生成（id 以 item_ore_ 开头）。
    /// </summary>
    public class MineSystem : MonoBehaviour
    {
        public static MineSystem Instance { get; private set; }

        private List<ItemData> _ores = new List<ItemData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start() { ResolveOres(); }

        public void ResolveOres()
        {
            _ores.Clear();
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item.id != null && item.id.StartsWith("item_ore_"))
                    _ores.Add(item);
            }
            // 按售价排序（低到高 = 铜矿/铁矿/金矿）
            _ores.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            Debug.Log($"[Mine] 已加载 {_ores.Count} 种矿石");
        }

        /// <summary>挖矿：消耗10体力，随机获得矿石。返回(矿石, 数量)，体力不足返回null。</summary>
        public (ItemData ore, int count)? TryMine()
        {
            if (PlayerStats.Instance == null || PlayerStats.Instance.Stamina < 10) return null;
            if (_ores.Count == 0) return null;

            PlayerStats.Instance.TrySpend(10);
            SkillSystem.Instance?.AddExp(SkillType.Mining, 12);

            // 概率：前60%普通(铜矿) / 30%中级(铁矿) / 10%稀有(金矿)
            int idx;
            float r = Random.value;
            if (_ores.Count >= 3)
            {
                if (r < 0.6f) idx = 0;
                else if (r < 0.9f) idx = 1;
                else idx = 2;
            }
            else
            {
                idx = Random.Range(0, _ores.Count);
            }

            int count = 1 + (SkillSystem.Instance != null ? SkillSystem.Instance.MiningYieldBonus : 0);
            var ore = _ores[idx];
            Debug.Log($"[Mine] 挖到 {ore.itemName} x{count}");
            return (ore, count);
        }

        public int OreCount => _ores.Count;
    }
}
