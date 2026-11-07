using System;
using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>烹饪食谱（代码内置）</summary>
    [Serializable]
    public class CookingRecipe
    {
        public string id;
        public string name;
        public string[] inputItemIds;
        public int[] inputCounts;
        public int staminaRestore;
        [NonSerialized] public ItemData[] _inputs;
    }

    /// <summary>
    /// 烹饪系统：消耗食材恢复体力。10道菜。
    /// 厨房台 E 键交互打开 CookingUI；K 键快捷开关。
    /// </summary>
    public class CookingSystem : MonoBehaviour
    {
        public static CookingSystem Instance { get; private set; }

        public event Action OnRecipesResolved;
        private readonly List<CookingRecipe> _recipes = new List<CookingRecipe>();
        private bool _resolved;

        public IReadOnlyList<CookingRecipe> Recipes => _recipes;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            DefineRecipes();
        }

        private void Start() { ResolveReferences(); }

        private void DefineRecipes()
        {
            Add("r_cook_radish_soup", "萝卜汤", new[] { "item_radish" }, new[] { 2 }, 30);
            Add("r_cook_cabbage_soup", "白菜汤", new[] { "item_cabbage" }, new[] { 2 }, 30);
            Add("r_cook_salad", "蔬菜沙拉", new[] { "item_radish", "item_cabbage" }, new[] { 1, 1 }, 20);
            Add("r_cook_tomato", "烤番茄", new[] { "item_tomato" }, new[] { 2 }, 40);
            Add("r_cook_sweetpotato", "烤红薯", new[] { "item_sweetpotato" }, new[] { 1 }, 25);
            Add("r_cook_mushroom_soup", "蘑菇汤", new[] { "item_mushroom" }, new[] { 2 }, 35);
            Add("r_cook_pumpkin", "南瓜饼", new[] { "item_pumpkin" }, new[] { 1 }, 35);
            Add("r_cook_corn", "烤玉米", new[] { "item_corn" }, new[] { 1 }, 20);
            Add("r_cook_stew", "蔬菜炖菜", new[] { "item_radish", "item_cabbage", "item_mushroom" }, new[] { 1, 1, 1 }, 50);
            Add("r_cook_xuan_chicken", "宣纸包菜", new[] { "item_paper", "item_radish" }, new[] { 1, 2 }, 60);
        }

        private void Add(string id, string name, string[] inputs, int[] counts, int stamina)
        {
            _recipes.Add(new CookingRecipe { id = id, name = name, inputItemIds = inputs, inputCounts = counts, staminaRestore = stamina });
        }

        public void ResolveReferences()
        {
            if (_resolved) return;
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var r in _recipes)
            {
                r._inputs = new ItemData[r.inputItemIds.Length];
                for (int i = 0; i < r.inputItemIds.Length; i++)
                    foreach (var item in allItems)
                        if (item.id == r.inputItemIds[i]) { r._inputs[i] = item; break; }
            }
            _resolved = true;
            OnRecipesResolved?.Invoke();
        }

        public bool CanCook(CookingRecipe r)
        {
            if (r == null || r._inputs == null || InventorySystem.Instance == null) return false;
            for (int i = 0; i < r._inputs.Length; i++)
            {
                if (r._inputs[i] == null) return false;
                if (InventorySystem.Instance.CountItem(r._inputs[i]) < r.inputCounts[i]) return false;
            }
            return true;
        }

        public bool Cook(CookingRecipe r)
        {
            if (!CanCook(r)) return false;
            for (int i = 0; i < r._inputs.Length; i++)
                InventorySystem.Instance.RemoveItem(r._inputs[i], r.inputCounts[i]);
            PlayerStats.Instance?.Restore(r.staminaRestore);
            Debug.Log($"[Cooking] 烹饪 {r.name} → 恢复 {r.staminaRestore} 体力");
            return true;
        }
    }
}
