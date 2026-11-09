using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 矿洞系统（v0.3.0）：共 10 层，越深矿石越稀有、单次体力消耗越高。
    /// 矿石资产 id 以 item_ore_ 开头，按售价升序 = 普通→中级→稀有。
    /// </summary>
    public class MineSystem : MonoBehaviour
    {
        public static MineSystem Instance { get; private set; }

        public const int MaxFloor = 10;
        public int CurrentFloor { get; private set; } = 1;

        private readonly List<ItemData> _ores = new List<ItemData>();
        private readonly List<ItemData> _relics = new List<ItemData>(); // 古董（item_antique_*），越深越珍贵

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start() { ResolveOres(); }

        public void ResolveOres()
        {
            _ores.Clear();
            _relics.Clear();
            var allItems = Resources.LoadAll<ItemData>("Data");
            foreach (var item in allItems)
            {
                if (item.id == null) continue;
                if (item.id.StartsWith("item_ore_")) _ores.Add(item);
                else if (item.id.StartsWith("item_antique_")) _relics.Add(item);
            }
            _ores.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            _relics.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            Debug.Log($"[Mine] 已加载 {_ores.Count} 种矿石、{_relics.Count} 种古董，当前第 {CurrentFloor}/{MaxFloor} 层");
        }

        /// <summary>本层挖一次需要的体力：10 → 28</summary>
        public int StaminaCost => 8 + CurrentFloor * 2;
        /// <summary>下潜消耗的体力</summary>
        public const int DescendStamina = 12;

        public (ItemData ore, int count)? TryMine()
        {
            if (PlayerStats.Instance == null) return null;
            if (!PlayerStats.Instance.HasEnough(StaminaCost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"第 {CurrentFloor} 层挖矿需 {StaminaCost} 体力，先吃料理 / 睡觉恢复");
                return null;
            }
            if (_ores.Count == 0) return null;

            PlayerStats.Instance.TrySpend(StaminaCost);
            SkillSystem.Instance?.AddExp(SkillType.Mining, 12);

            // 稀有率随层数上升：1 层 15% → 10 层 60% 出最高级矿
            float rare = Mathf.Clamp01(0.10f + CurrentFloor * 0.05f
                         + (FestivalSystem.Instance != null ? FestivalSystem.Instance.RareFishBonus * 0.5f : 0f));
            float r = Random.value;
            int idx;
            if (_ores.Count >= 3)
            {
                if (r < rare) idx = _ores.Count - 1;
                else if (r < rare + 0.35f) idx = Mathf.Min(_ores.Count - 2, 1);
                else idx = 0;
            }
            else idx = Random.Range(0, _ores.Count);

            int count = 1 + (SkillSystem.Instance != null ? SkillSystem.Instance.MiningYieldBonus : 0)
                          + (FestivalSystem.Instance != null ? FestivalSystem.Instance.ExtraOre : 0)
                          + (BuffSystem.Instance != null ? BuffSystem.Instance.MiningBonus : 0)
                          + (RandomEventSystem.Instance != null ? RandomEventSystem.Instance.OreBonus : 0);
            var ore = _ores[idx];
            QuestManager.Instance?.RecordMine();
            PBV.UI.HUD.Instance?.ShowNotice($"第 {CurrentFloor} 层：挖到 {ore.itemName} ×{count}");
            Debug.Log($"[Mine] 第{CurrentFloor}层 挖到 {ore.itemName} x{count}");

            // 古董掉落：越深概率越高（第1层 ~10% → 第10层 ~60%），且越深越可能出珍贵古董
            TryDropRelic();
            return (ore, count);
        }

        /// <summary>按矿层深度概率掉落古董，越深越珍贵；直接入背包并弹提示。</summary>
        private void TryDropRelic()
        {
            if (_relics.Count == 0) return;
            float chance = Mathf.Clamp01(0.10f + CurrentFloor * 0.05f);
            if (Random.value >= chance) return;

            // 深度决定古董档次：浅层出普通，深层出珍贵
            int rIdx;
            if (CurrentFloor >= 7) rIdx = _relics.Count - 1;
            else if (CurrentFloor >= 4) rIdx = Mathf.Min(_relics.Count - 1, 1);
            else rIdx = 0;
            var relic = _relics[rIdx];

            var inv = PBV.Inventory.InventorySystem.Instance;
            if (inv != null && inv.AddItem(relic, 1))
                PBV.UI.HUD.Instance?.ShowNotice($"【古董】挖到 {relic.itemName}！可带去现代卖高价", 3.5f);
            else
                PBV.UI.HUD.Instance?.ShowNotice($"背包已满，{relic.itemName} 未能拾取", 3.5f);
            Debug.Log($"[Mine] 第{CurrentFloor}层 掉落古董 {relic.itemName}");
        }

        /// <summary>下潜一层；顶层返回 false</summary>
        public bool Descend()
        {
            if (CurrentFloor >= MaxFloor)
            {
                PBV.UI.HUD.Instance?.ShowNotice("已到矿洞最深层（第 10 层）");
                return false;
            }
            if (PlayerStats.Instance == null || !PlayerStats.Instance.HasEnough(DescendStamina))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"下潜需 {DescendStamina} 体力");
                return false;
            }
            PlayerStats.Instance.TrySpend(DescendStamina);
            CurrentFloor++;
            PBV.UI.HUD.Instance?.ShowNotice($"下潜到第 {CurrentFloor} 层 · 稀有矿概率提升，挖矿耗 {StaminaCost} 体力");
            Debug.Log($"[Mine] 下潜到第 {CurrentFloor} 层");
            return true;
        }

        /// <summary>回到第 1 层（离开矿洞时）</summary>
        public void ResetFloor() => CurrentFloor = 1;

        public void RestoreFloor(int floor) => CurrentFloor = Mathf.Clamp(floor, 1, MaxFloor);
        public int ExportFloor() => CurrentFloor;
        public int OreCount => _ores.Count;
    }
}
