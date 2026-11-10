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
        /// <summary>归属建筑的 goName（如 "MillStation"）。制作台按此过滤，避免所有配方挤在一个面板。</summary>
        public string stationId;
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

        /// <summary>建筑 goName → 中文台名（CraftingStation 交互提示与 CraftingUI 标题共用）</summary>
        public static string StationName(string goName)
        {
            switch (goName)
            {
                case "CraftingStation": return "造纸台";
                case "DyeingStation": return "染坊";
                case "PotteryStation": return "陶坊";
                case "MedicineStation": return "药庐";
                case "CarpentryStation": return "木工坊";
                case "MillStation": return "磨坊";
                case "SauceStation": return "酱园";
                case "TeaStation": return "茶坊";
                case "SugarStation": return "制糖坊";
                case "SaltStation": return "盐坊";
                default: return "制作台";
            }
        }

        /// <summary>添加一个配方并标注归属建筑</summary>
        private void Add(string stationId, string id, string name, string[] inputs, int[] counts, string output, int outCount)
        {
            _recipes.Add(new Recipe
            {
                id = id, name = name, stationId = stationId,
                inputItemIds = inputs, inputCounts = counts,
                outputItemId = output, outputCount = outCount
            });
        }

        private void DefineRecipes()
        {
            const string PAPER = "CraftingStation";
            const string DYE = "DyeingStation";
            const string POTTERY = "PotteryStation";
            const string MEDICINE = "MedicineStation";
            const string CARPENTRY = "CarpentryStation";
            const string MILL = "MillStation";
            const string SAUCE = "SauceStation";
            const string TEA = "TeaStation";
            const string SUGAR = "SugarStation";
            const string SALT = "SaltStation";

            // 造纸台：造纸产业链（树皮/稻草 → 纸浆 → 纸 → 宣纸）
            Add(PAPER, "r_bark_to_pulp", "树皮制浆", new[] { "item_bark" }, new[] { 2 }, "item_pulp", 1);
            Add(PAPER, "r_straw_to_pulp", "稻草制浆", new[] { "item_straw" }, new[] { 3 }, "item_pulp", 1);
            Add(PAPER, "r_pulp_to_paper", "抄纸", new[] { "item_pulp" }, new[] { 2 }, "item_paper", 3);
            Add(PAPER, "r_paper_to_xuan", "制宣纸", new[] { "item_paper" }, new[] { 5 }, "item_xuanpaper", 1);

            // 染坊：草木扎染（蓝草 → 素色布 → 纹样布 → 成衣）
            Add(DYE, "r_dye_indigo", "蓝草染布", new[] { "item_indigo" }, new[] { 2 }, "item_cloth_plain", 1);
            Add(DYE, "r_dye_pattern", "扎出纹样", new[] { "item_cloth_plain" }, new[] { 2 }, "item_cloth_pattern", 1);
            Add(DYE, "r_dye_garment", "缝制成衣", new[] { "item_cloth_pattern", "item_bark" }, new[] { 1, 1 }, "item_garment", 1);

            // 陶坊：陶瓷（陶土 → 陶坯 → 青瓷盏 / 青花瓷瓶 / 陶罐）
            Add(POTTERY, "r_clay_to_pot", "淘土制坯", new[] { "item_clay" }, new[] { 2 }, "item_clay_pot", 1);
            Add(POTTERY, "r_pot_to_jar", "拉坯成罐", new[] { "item_clay_pot" }, new[] { 1 }, "item_pottery_jar", 1);
            Add(POTTERY, "r_pot_to_celadon", "烧青瓷盏", new[] { "item_clay_pot" }, new[] { 2 }, "item_pottery_bowl", 1);
            Add(POTTERY, "r_pot_to_bluewhite", "烧青花瓷瓶", new[] { "item_clay_pot" }, new[] { 3 }, "item_pottery_vase", 1);

            // 药庐：药墨·药酒 + 制药·制毒（药材/姜/酒/毒草 → 各类药与毒）
            Add(MEDICINE, "r_herb_to_ink", "和药制墨", new[] { "item_herb" }, new[] { 3 }, "item_ink_medicine", 1);
            Add(MEDICINE, "r_herb_to_wine", "浸酿药酒", new[] { "item_herb", "item_wild_fruit" }, new[] { 2, 2 }, "item_medicine_wine", 1);
            Add(MEDICINE, "r_herb_to_plaster", "熬制膏药", new[] { "item_herb" }, new[] { 2 }, "item_plaster", 2);
            Add(MEDICINE, "r_brew_ricewine", "酿米酒", new[] { "item_rice", "item_wild_fruit" }, new[] { 3, 1 }, "item_ricewine", 1);
            Add(MEDICINE, "r_med_cold", "煎风寒药", new[] { "item_herb", "item_ginger" }, new[] { 2, 1 }, "item_med_cold", 1);
            Add(MEDICINE, "r_med_diarrhea", "配止泻药", new[] { "item_herb", "item_wild_fruit" }, new[] { 2, 1 }, "item_med_diarrhea", 1);
            Add(MEDICINE, "r_med_trauma", "调跌打膏", new[] { "item_herb", "item_clay" }, new[] { 2, 1 }, "item_med_trauma", 1);
            Add(MEDICINE, "r_hangover", "煮醒酒汤", new[] { "item_ricewine", "item_herb" }, new[] { 1, 1 }, "item_hangover_soup", 1);
            Add(MEDICINE, "r_poison_bait", "拌毒饵", new[] { "item_poison_herb", "item_wild_fruit" }, new[] { 1, 2 }, "item_poison_bait", 2);
            Add(MEDICINE, "r_poison_powder", "磨迷药", new[] { "item_poison_herb" }, new[] { 3 }, "item_poison_powder", 1);

            // 木工坊：木材 → 木梁 → 木家具 / 储物箱（储物箱制作后手动放置）
            Add(CARPENTRY, "r_wood_to_beam", "刨木成梁", new[] { "item_wood" }, new[] { 3 }, "item_wood_beam", 1);
            Add(CARPENTRY, "r_beam_to_furniture", "打制家具", new[] { "item_wood_beam", "item_wood" }, new[] { 1, 2 }, "item_furniture", 1);
            Add(CARPENTRY, "r_wood_to_chest", "打制储物箱", new[] { "item_wood" }, new[] { 6 }, "item_storage_chest", 1);

            // 磨坊：小麦→面粉、大豆→豆油、玉米→玉米油
            Add(MILL, "r_wheat_to_flour", "磨小麦粉", new[] { "item_wheat" }, new[] { 2 }, "item_flour", 1);
            Add(MILL, "r_soy_to_oil", "榨大豆油", new[] { "item_soybean" }, new[] { 3 }, "item_soyoil", 1);
            Add(MILL, "r_corn_to_oil", "榨玉米油", new[] { "item_corn" }, new[] { 3 }, "item_cornoil", 1);

            // 酱园：酱油、陈醋 + 酱菜（白菜/萝卜/黄瓜 用盐腌制）
            Add(SAUCE, "r_soy_to_sauce", "酿酱油", new[] { "item_soybean", "item_wheat" }, new[] { 2, 1 }, "item_soysauce", 1);
            Add(SAUCE, "r_soy_to_vinegar", "酿陈醋", new[] { "item_soybean", "item_rice" }, new[] { 2, 1 }, "item_vinegar", 1);
            Add(SAUCE, "r_pickle_cabbage", "腌白菜酱菜", new[] { "item_cabbage", "item_salt" }, new[] { 2, 1 }, "item_pickle", 2);
            Add(SAUCE, "r_pickle_radish", "腌萝卜酱菜", new[] { "item_radish", "item_salt" }, new[] { 2, 1 }, "item_pickle", 2);
            Add(SAUCE, "r_pickle_cucumber", "酱黄瓜", new[] { "item_cucumber", "item_salt" }, new[] { 2, 1 }, "item_pickle", 2);

            // 茶坊：茶叶→茶饼、沏茶饮
            Add(TEA, "r_tea_to_cake", "压制茶饼", new[] { "item_tea_leaf" }, new[] { 3 }, "item_tea_cake", 1);
            Add(TEA, "r_tea_to_drink", "沏茶饮", new[] { "item_tea_leaf", "item_mountain_tea" }, new[] { 1, 1 }, "item_tea_drink", 2);

            // 制糖坊（甘蔗/甜菜 → 糖）
            Add(SUGAR, "r_sugarcane_to_sugar", "甘蔗榨糖", new[] { "item_sugarcane" }, new[] { 3 }, "item_sugar", 1);
            Add(SUGAR, "r_sugarbeet_to_sugar", "甜菜熬糖", new[] { "item_sugarbeet" }, new[] { 3 }, "item_sugar", 1);

            // 盐坊（盐石 → 盐）
            Add(SALT, "r_salt_ore_to_salt", "盐石煮盐", new[] { "item_salt_ore" }, new[] { 2 }, "item_salt", 2);
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
