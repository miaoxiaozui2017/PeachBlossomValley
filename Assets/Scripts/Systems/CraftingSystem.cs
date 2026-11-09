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

            // 第二条非遗产业链：草木扎染（蓝草 → 素色布 → 纹样布 → 成衣）
            _recipes.Add(new Recipe { id = "r_dye_indigo", name = "蓝草染布", inputItemIds = new[] { "item_indigo" }, inputCounts = new[] { 2 }, outputItemId = "item_cloth_plain", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_dye_pattern", name = "扎出纹样", inputItemIds = new[] { "item_cloth_plain" }, inputCounts = new[] { 2 }, outputItemId = "item_cloth_pattern", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_dye_garment", name = "缝制成衣", inputItemIds = new[] { "item_cloth_pattern", "item_bark" }, inputCounts = new[] { 1, 1 }, outputItemId = "item_garment", outputCount = 1 });

            // 第三条非遗产业链：陶瓷（陶土 → 陶坯 → 青瓷盏 / 青花瓷瓶 / 陶罐）
            _recipes.Add(new Recipe { id = "r_clay_to_pot", name = "淘土制坯", inputItemIds = new[] { "item_clay" }, inputCounts = new[] { 2 }, outputItemId = "item_clay_pot", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_pot_to_jar", name = "拉坯成罐", inputItemIds = new[] { "item_clay_pot" }, inputCounts = new[] { 1 }, outputItemId = "item_pottery_jar", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_pot_to_celadon", name = "烧青瓷盏", inputItemIds = new[] { "item_clay_pot" }, inputCounts = new[] { 2 }, outputItemId = "item_pottery_bowl", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_pot_to_bluewhite", name = "烧青花瓷瓶", inputItemIds = new[] { "item_clay_pot" }, inputCounts = new[] { 3 }, outputItemId = "item_pottery_vase", outputCount = 1 });

            // 第四条非遗产业链：药墨·药酒（药材 → 药墨 / 药酒 / 膏药）
            _recipes.Add(new Recipe { id = "r_herb_to_ink", name = "和药制墨", inputItemIds = new[] { "item_herb" }, inputCounts = new[] { 3 }, outputItemId = "item_ink_medicine", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_herb_to_wine", name = "浸酿药酒", inputItemIds = new[] { "item_herb", "item_wild_fruit" }, inputCounts = new[] { 2, 2 }, outputItemId = "item_medicine_wine", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_herb_to_plaster", name = "熬制膏药", inputItemIds = new[] { "item_herb" }, inputCounts = new[] { 2 }, outputItemId = "item_plaster", outputCount = 2 });

            // 制药·制毒（药婆药庐）：药材/姜/酒/毒草 → 各类药与毒
            _recipes.Add(new Recipe { id = "r_brew_ricewine", name = "酿米酒", inputItemIds = new[] { "item_rice", "item_wild_fruit" }, inputCounts = new[] { 3, 1 }, outputItemId = "item_ricewine", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_med_cold", name = "煎风寒药", inputItemIds = new[] { "item_herb", "item_ginger" }, inputCounts = new[] { 2, 1 }, outputItemId = "item_med_cold", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_med_diarrhea", name = "配止泻药", inputItemIds = new[] { "item_herb", "item_wild_fruit" }, inputCounts = new[] { 2, 1 }, outputItemId = "item_med_diarrhea", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_med_trauma", name = "调跌打膏", inputItemIds = new[] { "item_herb", "item_clay" }, inputCounts = new[] { 2, 1 }, outputItemId = "item_med_trauma", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_hangover", name = "煮醒酒汤", inputItemIds = new[] { "item_ricewine", "item_herb" }, inputCounts = new[] { 1, 1 }, outputItemId = "item_hangover_soup", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_poison_bait", name = "拌毒饵", inputItemIds = new[] { "item_poison_herb", "item_wild_fruit" }, inputCounts = new[] { 1, 2 }, outputItemId = "item_poison_bait", outputCount = 2 });
            _recipes.Add(new Recipe { id = "r_poison_powder", name = "磨迷药", inputItemIds = new[] { "item_poison_herb" }, inputCounts = new[] { 3 }, outputItemId = "item_poison_powder", outputCount = 1 });

            // 住：木工坊（木材 → 木梁 → 木家具）
            _recipes.Add(new Recipe { id = "r_wood_to_beam", name = "刨木成梁", inputItemIds = new[] { "item_wood" }, inputCounts = new[] { 3 }, outputItemId = "item_wood_beam", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_beam_to_furniture", name = "打制家具", inputItemIds = new[] { "item_wood_beam", "item_wood" }, inputCounts = new[] { 1, 2 }, outputItemId = "item_furniture", outputCount = 1 });
            // 住：木工坊（制作储物箱 → 手动放置，36 格可改名改色）
            _recipes.Add(new Recipe { id = "r_wood_to_chest", name = "打制储物箱", inputItemIds = new[] { "item_wood", "item_wood" }, inputCounts = new[] { 3, 3 }, outputItemId = "item_storage_chest", outputCount = 1 });

            // 食：磨坊（小麦→面粉、大豆→豆油、玉米→玉米油）
            _recipes.Add(new Recipe { id = "r_wheat_to_flour", name = "磨小麦粉", inputItemIds = new[] { "item_wheat" }, inputCounts = new[] { 2 }, outputItemId = "item_flour", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_soy_to_oil", name = "榨大豆油", inputItemIds = new[] { "item_soybean" }, inputCounts = new[] { 3 }, outputItemId = "item_soyoil", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_corn_to_oil", name = "榨玉米油", inputItemIds = new[] { "item_corn" }, inputCounts = new[] { 3 }, outputItemId = "item_cornoil", outputCount = 1 });

            // 食：酱园（大豆→酱油、陈醋）
            _recipes.Add(new Recipe { id = "r_soy_to_sauce", name = "酿酱油", inputItemIds = new[] { "item_soybean", "item_wheat" }, inputCounts = new[] { 2, 1 }, outputItemId = "item_soysauce", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_soy_to_vinegar", name = "酿陈醋", inputItemIds = new[] { "item_soybean", "item_rice" }, inputCounts = new[] { 2, 1 }, outputItemId = "item_vinegar", outputCount = 1 });

            // 食：茶坊（茶叶→茶饼、沏茶饮）
            _recipes.Add(new Recipe { id = "r_tea_to_cake", name = "压制茶饼", inputItemIds = new[] { "item_tea_leaf" }, inputCounts = new[] { 3 }, outputItemId = "item_tea_cake", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_tea_to_drink", name = "沏茶饮", inputItemIds = new[] { "item_tea_leaf", "item_mountain_tea" }, inputCounts = new[] { 1, 1 }, outputItemId = "item_tea_drink", outputCount = 2 });

            // 食：制糖坊（甘蔗/甜菜 → 糖）
            _recipes.Add(new Recipe { id = "r_sugarcane_to_sugar", name = "甘蔗榨糖", inputItemIds = new[] { "item_sugarcane" }, inputCounts = new[] { 3 }, outputItemId = "item_sugar", outputCount = 1 });
            _recipes.Add(new Recipe { id = "r_sugarbeet_to_sugar", name = "甜菜熬糖", inputItemIds = new[] { "item_sugarbeet" }, inputCounts = new[] { 3 }, outputItemId = "item_sugar", outputCount = 1 });

            // 食：盐坊（盐石 → 盐）
            _recipes.Add(new Recipe { id = "r_salt_ore_to_salt", name = "盐石煮盐", inputItemIds = new[] { "item_salt_ore" }, inputCounts = new[] { 2 }, outputItemId = "item_salt", outputCount = 2 });
        }

        /// <summary>解析物品引用（Resources 查找）</summary>
        public void ResolveReferences()
        {
            if (_resolved) return;
            // 必须主动从 Resources/Data 加载：FindObjectsOfTypeAll 只返回已加载对象，
            // 未被背包引用过的材料资产不会在内存里，会导致配方引用为 null、永远判定材料不足。
            var allItems = Resources.LoadAll<ItemData>("Data");
            if (allItems == null || allItems.Length == 0) return; // 资产还没就绪，下次重试

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

            // 有任何一个引用没解析到就不锁定 _resolved，允许后续重试，
            // 否则配方会永久判定「材料不足」，制作按钮一直是灰的。
            foreach (var r in _recipes)
            {
                if (r._output == null) return;
                foreach (var inp in r._inputs) if (inp == null) return;
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
        /// <summary>材料不足的具体清单</summary>
        public string GetMissingText(Recipe r)
        {
            if (r == null) return string.Empty;
            var inv = InventorySystem.Instance;
            if (inv == null) return "背包未就绪";
            var missing = new List<string>();
            for (int i = 0; i < r._inputs.Length; i++)
            {
                var item = r._inputs[i];
                if (item == null) { missing.Add(r.inputItemIds[i]); continue; }
                int have = inv.CountItem(item);
                if (have < r.inputCounts[i]) missing.Add($"{item.itemName} {have}/{r.inputCounts[i]}");
            }
            return missing.Count == 0 ? string.Empty : "缺 " + string.Join("、", missing);
        }

        public bool Craft(Recipe r)
        {
            if (!CanCraft(r))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{r?.name} 材料不足 · {GetMissingText(r)}");
                return false;
            }
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
                    PBV.UI.HUD.Instance?.ShowNotice("背包已满，制作取消（材料已退还）");
                    Debug.Log("[Craft] 背包已满，制作取消");
                    return false;
                }
            }
            PBV.UI.HUD.Instance?.ShowNotice($"制作 {r.name} → {r._output?.itemName} ×{r.outputCount}");
            Debug.Log($"[Craft] 制作 {r.name} → {r._output?.itemName} x{r.outputCount}");
            return true;
        }
    }
}
