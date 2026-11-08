using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 钓鱼系统：管理鱼类物品，提供随机钓鱼结果。
    /// 鱼类物品在 SceneBuilder 生成（id 以 item_fish_ 开头）。
    /// </summary>
    public class FishingSystem : MonoBehaviour
    {
        public static FishingSystem Instance { get; private set; }

        private List<ItemData> _fishTypes = new List<ItemData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start() { ResolveFish(); }

        public void ResolveFish()
        {
            _fishTypes.Clear();
            var allItems = Resources.LoadAll<ItemData>("Data");
            foreach (var item in allItems)
            {
                if (item.id != null && item.id.StartsWith("item_fish_"))
                    _fishTypes.Add(item);
            }
            Debug.Log($"[Fishing] 已加载 {_fishTypes.Count} 种鱼");
        }

        /// <summary>
        /// 按钓点分池随机钓鱼：0=溪(低价常见) / 1=河(中档) / 2=湖(高价稀有)。
        /// 12 种鱼按售价升序分成有重叠的三段，越往湖里越容易出稀有鱼。
        /// </summary>
        public ItemData GetRandomFish(int spotIndex = 0)
        {
            if (_fishTypes.Count == 0) return null;
            _fishTypes.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            int n = _fishTypes.Count;
            int spot = Mathf.Clamp(spotIndex, 0, 2);

            int lo = spot == 0 ? 0 : spot == 1 ? n / 3 : n * 2 / 3;
            int hi = spot == 0 ? Mathf.Max(lo + 1, n / 2) : spot == 1 ? Mathf.Max(lo + 1, n * 5 / 6) : n;
            hi = Mathf.Min(hi, n);

            float rareChance = 0.3f + spot * 0.1f;
            if (SkillSystem.Instance != null) rareChance += SkillSystem.Instance.FishingRareBonus;
            if (FestivalSystem.Instance != null) rareChance += FestivalSystem.Instance.RareFishBonus;
            if (BuffSystem.Instance != null) rareChance += BuffSystem.Instance.FishingBonus;
            if (RandomEventSystem.Instance != null) rareChance += RandomEventSystem.Instance.FishBonus;
            bool rare = Random.value < rareChance;
            int idx = rare ? Random.Range(Mathf.Max(lo, hi - Mathf.Max(1, (hi - lo) / 2)), hi) : lo;
            return _fishTypes[Mathf.Clamp(idx, 0, n - 1)];
        }

        public int FishCount => _fishTypes.Count;
    }
}
