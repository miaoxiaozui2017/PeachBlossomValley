using System;
using System.Collections.Generic;
using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>制作配方（代码内置，不依赖 ScriptableObject）</summary>
    [Serializable]
    public class Recipe
    {
        public string id;
        public string name;
        public string[] inputItemIds;  // 材料物品 id
        public int[] inputCounts;      // 材料数量
        public string outputItemId;    // 产出物品 id
        public int outputCount;
        [NonSerialized] public ItemData[] _inputs; // 运行时解析的引用
        [NonSerialized] public ItemData _output;
    }

    /// <summary>
    /// 制作系统：管理配方，检查材料，执行制作。
    /// 含造纸产业链 4 个配方。
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        public event Action OnRecipesResolved;

        private readonly List<Recipe> _recipes = new List<Recipe>();
        private bool _resolved;

        public IReadOnlyList<Recipe> Recipes => _recipes;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            DefineRecipes();
        }

        private void Start()
        {
            // 延迟到 Start 解析物品引用（等 SceneBuilder/StarterKit 加载完资产）
            ResolveReferences();
        }

        private void DefineRecipes()
        {
            // 造纸产业链
            _recipes.Add(new Recipe { id = "r_bark_to_pulp", name = "树皮制浆", inputItemIds = new[] { "item_bark" }, inputCounts = new[] { 2 }, outputItemId = "item_pulp", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_straw_to_pulp", name = "稻草制浆", inputItemIds = new[] { "item_straw" }, inputCounts = new[] { 3 }, outputItemId = "item_pulp", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_pulp_to_paper", name = "抄纸", inputItemIds = new[] { "item_pulp" }, inputCounts = new[] { 2 }, outputItemId = "item_paper", outputCount = 3 });
            _recipes.Add(new Recipe { id = "r_paper_to_xuan", name = "制宣纸", inputItemIds = new[] { "item_paper" }, inputCounts = new[] { 5 }, outputItemId = "item_xuanpaper", outputCount = 1 });
        }

        /// <summary>解析物品引用（Resources 查找）</summary>
        public void ResolveReferences()
        {
            if (_resolved) return;
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var r in _recipes)
            {
                r._inputs = new ItemData[r.inputItemIds.Length];
                for (int i = 0; i < r.inputItemIds.Length; i++)
                {
                    foreach (var item in allItems)
                    {
                        if (item.id == r.inputItemIds[i]) { r._inputs[i] = item; break; }
                    }
                }
                foreach (var item in allItems)
                {
                    if (item.id == r.outputItemId) { r._output = item; break; }
                }
            }
            _resolved = true;
            OnRecipesResolved?.Invoke();
        }

        /// <summary>检查是否能制作某配方</summary>
        public bool CanCraft(Recipe r)
        {
            if (r == null || r._inputs == null || InventorySystem.Instance == null) return false;
            for (int i = 0; i < r._inputs.Length; i++)
            {
                if (r._inputs[i] == null) return false;
                if (InventorySystem.Instance.CountItem(r._inputs[i]) < r.inputCounts[i]) return false;
            }
            return true;
        }

        /// <summary>执行制作</summary>
        public bool Craft(Recipe r)
        {
            if (!CanCraft(r)) return false;
            // 消耗材料
            for (int i = 0; i < r._inputs.Length; i++)
            {
                InventorySystem.Instance.RemoveItem(r._inputs[i], r.inputCounts[i]);
            }
            // 产出
            if (r._output != null)
            {
                bool added = InventorySystem.Instance.AddItem(r._output, r.outputCount);
                if (!added)
                {
                    // 背包满，退还材料
                    for (int i = 0; i < r._inputs.Length; i++)
                        InventorySystem.Instance.AddItem(r._inputs[i], r.inputCounts[i]);
                    Debug.Log("[Craft] 背包已满，制作取消");
                    return false;
                }
            }
            Debug.Log($"[Craft] 制作 {r.name} → {r._output?.itemName} x{r.outputCount}");
            return true;
        }
    }
}
