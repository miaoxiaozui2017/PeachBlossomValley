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
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item.id != null && item.id.StartsWith("item_fish_"))
                    _fishTypes.Add(item);
            }
            Debug.Log($"[Fishing] 已加载 {_fishTypes.Count} 种鱼");
        }

        /// <summary>随机钓一条鱼（稀有度权重：普通鱼70% / 稀有鱼30%）</summary>
        public ItemData GetRandomFish()
        {
            if (_fishTypes.Count == 0) return null;
            // 按售价排序，前半为普通，后半为稀有
            _fishTypes.Sort((a, b) => a.sellPrice.CompareTo(b.sellPrice));
            int mid = Mathf.Max(1, _fishTypes.Count / 2);
            float rareChance = 0.3f;
            if (SkillSystem.Instance != null) rareChance += SkillSystem.Instance.FishingRareBonus;
            bool rare = Random.value < rareChance;
            int idx = rare ? Random.Range(mid, _fishTypes.Count) : Random.Range(0, mid);
            return _fishTypes[idx];
        }

        public int FishCount => _fishTypes.Count;
    }
}
