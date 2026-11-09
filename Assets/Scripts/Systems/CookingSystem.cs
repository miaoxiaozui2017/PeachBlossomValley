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
        public string outputItemId;
        public string[] inputItemIds;
        public int[] inputCounts;
        public int staminaRestore;
        [NonSerialized] public ItemData[] _inputs;
        [NonSerialized] public ItemData _output;
    }

    /// <summary>
    /// 烹饪系统：消耗食材「做出一道菜放进背包」，不再立刻回体力。
    /// 菜可以之后按 F 吃、送给 NPC 加好感、或卖给商店/出货箱。
    /// 厨房台 E 键交互打开 CookingUI；K 键快捷开关。
    /// </summary>
    public class CookingSystem : MonoBehaviour
    {
        public static CookingSystem Instance { get; private set; }

        /// <summary>配方里的通配材料：任意一条 item_fish_* 鱼都可以</summary>
        public const string AnyFishId = "any_fish";

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
            Add("r_cook_radish_soup", "萝卜汤", "item_dish_radish_soup", new[] { "item_radish" }, new[] { 2 }, 30);
            Add("r_cook_cabbage_soup", "白菜汤", "item_dish_cabbage_soup", new[] { "item_cabbage" }, new[] { 2 }, 30);
            Add("r_cook_salad", "蔬菜沙拉", "item_dish_salad", new[] { "item_radish", "item_cabbage" }, new[] { 1, 1 }, 20);
            Add("r_cook_tomato", "烤番茄", "item_dish_tomato", new[] { "item_tomato" }, new[] { 2 }, 40);
            Add("r_cook_sweetpotato", "烤红薯", "item_dish_sweetpotato", new[] { "item_sweetpotato" }, new[] { 1 }, 25);
            Add("r_cook_mushroom_soup", "蘑菇汤", "item_dish_mushroom_soup", new[] { "item_mushroom" }, new[] { 2 }, 35);
            Add("r_cook_pumpkin", "南瓜饼", "item_dish_pumpkin", new[] { "item_pumpkin" }, new[] { 1 }, 35);
            Add("r_cook_corn", "烤玉米", "item_dish_corn", new[] { "item_corn" }, new[] { 1 }, 20);
            Add("r_cook_stew", "蔬菜炖菜", "item_dish_stew", new[] { "item_radish", "item_cabbage", "item_mushroom" }, new[] { 1, 1, 1 }, 50);
            Add("r_cook_xuan_chicken", "宣纸包菜", "item_dish_xuan_chicken", new[] { "item_paper", "item_radish" }, new[] { 1, 2 }, 60);
            // 鱼料理：任意鱼 1 条即可，钓到什么都能做
            Add("r_cook_steamed_fish", "清蒸鱼", "item_dish_steamed_fish", new[] { AnyFishId }, new[] { 1 }, 45);
            Add("r_cook_fish_soup", "鱼头萝卜汤", "item_dish_fish_soup", new[] { AnyFishId, "item_radish" }, new[] { 1, 1 }, 60);
            Add("r_cook_braised_fish", "红烧鱼", "item_dish_braised_fish", new[] { AnyFishId, "item_cabbage" }, new[] { 1, 1 }, 80);
            // 采集食材做的家常菜（不依赖种子，开局就能上手）
            Add("r_cook_veg_soup", "野菜汤", "item_dish_veg_soup", new[] { "item_wild_veg" }, new[] { 2 }, 30);
            Add("r_cook_fruit_tea", "野果茶", "item_dish_fruit_tea", new[] { "item_wild_fruit" }, new[] { 2 }, 25);
            // 野味菜（陷阱/采山捕获的野兔野鸡野鹿）
            Add("r_cook_hare_stew", "红烧野兔", "item_dish_hare_stew", new[] { "item_game_hare" }, new[] { 1 }, 50);
            Add("r_cook_pheasant_soup", "野鸡菌汤", "item_dish_pheasant_soup", new[] { "item_game_pheasant", "item_mountain_fungus" }, new[] { 1, 1 }, 65);
            Add("r_cook_deer_roast", "烤鹿肉", "item_dish_deer_roast", new[] { "item_game_deer" }, new[] { 1 }, 80);
            // 药膳：姜汤（生姜熬制，治风寒）
            Add("r_cook_ginger_soup", "熬姜汤", "item_ginger_soup", new[] { "item_ginger" }, new[] { 2 }, 10);
        }

        private void Add(string id, string name, string output, string[] inputs, int[] counts, int stamina)
        {
            _recipes.Add(new CookingRecipe
            {
                id = id, name = name, outputItemId = output,
                inputItemIds = inputs, inputCounts = counts, staminaRestore = stamina
            });
        }

        public void ResolveReferences()
        {
            if (_resolved) return;
            var allItems = Resources.LoadAll<ItemData>("Data");
            foreach (var r in _recipes)
            {
                r._inputs = new ItemData[r.inputItemIds.Length];
                for (int i = 0; i < r.inputItemIds.Length; i++)
                {
                    if (r.inputItemIds[i] == AnyFishId) continue; // 通配，运行时从背包找
                    foreach (var item in allItems)
                        if (item.id == r.inputItemIds[i]) { r._inputs[i] = item; break; }
                }
                foreach (var item in allItems)
                    if (item.id == r.outputItemId) { r._output = item; break; }
            }

            // 有引用没解析到就不锁 _resolved，允许后续重试，避免配方永久灰掉
            foreach (var r in _recipes)
            {
                if (r._output == null) return;
                for (int i = 0; i < r._inputs.Length; i++)
                    if (r.inputItemIds[i] != AnyFishId && r._inputs[i] == null) return;
            }
            _resolved = true;
            OnRecipesResolved?.Invoke();
        }

        private static ItemData FindAnyFish()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return null;
            for (int i = 0; i < inv.Capacity; i++)
            {
                var s = inv.GetSlot(i);
                if (s != null && !s.IsEmpty && s.item != null && s.item.id != null && s.item.id.StartsWith("item_fish_"))
                    return s.item;
            }
            return null;
        }

        public bool CanCook(CookingRecipe r)
        {
            if (r == null || r._output == null || InventorySystem.Instance == null) return false;
            var inv = InventorySystem.Instance;
            for (int i = 0; i < r.inputItemIds.Length; i++)
            {
                if (r.inputItemIds[i] == AnyFishId)
                {
                    if (FindAnyFish() == null) return false;
                    continue;
                }
                if (r._inputs[i] == null) return false;
                if (inv.CountItem(r._inputs[i]) < r.inputCounts[i]) return false;
            }
            return true;
        }

        /// <summary>材料不足的具体清单，供界面和提示复用</summary>
        public string GetMissingText(CookingRecipe r)
        {
            if (r == null) return string.Empty;
            var inv = InventorySystem.Instance;
            if (inv == null) return "背包未就绪";
            var missing = new List<string>();
            for (int i = 0; i < r.inputItemIds.Length; i++)
            {
                if (r.inputItemIds[i] == AnyFishId)
                {
                    if (FindAnyFish() == null) missing.Add("任意鱼x1");
                    continue;
                }
                var item = r._inputs[i];
                if (item == null) { missing.Add(r.inputItemIds[i]); continue; }
                int have = inv.CountItem(item);
                if (have < r.inputCounts[i]) missing.Add($"{item.itemName} {have}/{r.inputCounts[i]}");
            }
            return missing.Count == 0 ? string.Empty : "缺 " + string.Join("、", missing);
        }

        /// <summary>做菜：材料进背包 → 产出菜品；不再自动恢复体力</summary>
        public bool Cook(CookingRecipe r)
        {
            if (r == null) return false;
            if (r._output == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("菜品资产还没加载好：退出 Play 等 Unity 导入完成，或重建灰盒场景");
                return false;
            }
            if (!CanCook(r))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{r?.name} 材料不足 · {GetMissingText(r)}");
                return false;
            }

            var inv = InventorySystem.Instance;
            bool usesFish = Array.IndexOf(r.inputItemIds, AnyFishId) >= 0;
            ItemData fish = usesFish ? FindAnyFish() : null;

            for (int i = 0; i < r.inputItemIds.Length; i++)
            {
                if (r.inputItemIds[i] == AnyFishId) inv.RemoveItem(fish, r.inputCounts[i]);
                else inv.RemoveItem(r._inputs[i], r.inputCounts[i]);
            }

            SkillSystem.Instance?.AddExp(SkillType.Cooking, 8);

            if (!inv.AddItem(r._output, 1))
            {
                // 背包满：退还全部材料
                for (int i = 0; i < r.inputItemIds.Length; i++)
                {
                    if (r.inputItemIds[i] == AnyFishId) inv.AddItem(fish, r.inputCounts[i]);
                    else inv.AddItem(r._inputs[i], r.inputCounts[i]);
                }
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，菜放不下（材料已退还）");
                return false;
            }

            PBV.UI.HUD.Instance?.ShowNotice($"做好 {r._output.itemName} ×1 · 按 F 食用 / 可送礼 / 可出售");
            QuestManager.Instance?.RecordCook();
            Debug.Log($"[Cooking] 做出 {r._output.itemName}");
            return true;
        }

        /// <summary>F 键：吃当前选中槽位的料理；若选中槽不是食物，自动找背包里第一份</summary>
        public static void TryEatSelected()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            int idx = PBV.UI.InventoryUI.SelectedSlotIndex;
            var slot = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (slot == null || slot.IsEmpty || slot.item == null || slot.item.type != ItemType.Consumable)
            {
                slot = null;
                for (int i = 0; i < inv.Capacity; i++)
                {
                    var s = inv.GetSlot(i);
                    if (s != null && !s.IsEmpty && s.item != null && s.item.type == ItemType.Consumable) { slot = s; break; }
                }
            }

            if (slot == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包里没有可吃的料理 · 到厨房按 K 做菜");
                return;
            }

            var food = slot.item;
            inv.RemoveItem(food, 1);

            // 药品优先：按 F 吃药治疗对应疾病（不恢复体力）
            if (food.id != null && HealthSystem.Instance != null &&
                HealthSystem.Instance.TryCure(food.id))
            {
                Debug.Log($"[Cooking] 用药 {food.itemName} 治愈疾病");
                return;
            }

            // 饮酒：过量会醉酒
            if (food.id == "item_ricewine" || food.id == "item_medicine_wine")
            {
                HealthSystem.Instance?.OnDrink();
                int wineRestore = food.staminaRestore;
                PlayerStats.Instance?.Restore(wineRestore);
                PBV.UI.HUD.Instance?.ShowNotice($"饮下 {food.itemName}，体力 +{wineRestore}");
                return;
            }

            // 生野味直接吃 → 腹泻（野味应做熟了吃）
            if (food.id != null && food.id.StartsWith("item_game_"))
            {
                HealthSystem.Instance?.OnEatRaw();
                int rawRestore = Mathf.Max(5, food.staminaRestore / 3);
                PlayerStats.Instance?.Restore(rawRestore);
                PBV.UI.HUD.Instance?.ShowNotice($"生吃 {food.itemName}（体力 +{rawRestore}，但闹肚子了！）");
                return;
            }

            int restore = food.staminaRestore;
            if (SkillSystem.Instance != null) restore = Mathf.RoundToInt(restore * (1f + SkillSystem.Instance.CookingBonus));
            if (FestivalSystem.Instance != null) restore = Mathf.RoundToInt(restore * FestivalSystem.Instance.FoodMultiplier);
            PlayerStats.Instance?.Restore(restore);
            var buff = BuffOf(food);
            if (buff != BuffType.None)
            {
                // 黑科技图纸研究后增益持续 3 天，其余料理 1 天
                int days = food.id != null && food.id.StartsWith("item_blueprint_") ? 3 : 1;
                BuffSystem.Instance?.Apply(buff, days);
            }
            string msg = restore > 0
                ? $"吃掉 {food.itemName}，体力 +{restore}"
                : $"使用 {food.itemName}";
            PBV.UI.HUD.Instance?.ShowNotice(msg +
                (buff != BuffType.None ? $" · 获得「{BuffSystem.BuffName(buff)}」" : ""));
            Debug.Log($"[Cooking] 食用 {food.itemName} → 恢复 {restore} 体力");
        }

        /// <summary>八大菜系效果（按菜品 id 归类，避免改动已有资产文件）</summary>
        private static BuffType BuffOf(ItemData food)
        {
            string id = food != null ? food.id : "";
            if (id.StartsWith("item_blueprint_")) // 现代黑科技图纸 → 研究获得对应增益
            {
                if (id == "item_blueprint_farm") return BuffType.Farming;
                if (id == "item_blueprint_mine") return BuffType.Mining;
                if (id == "item_blueprint_trade") return BuffType.Trade;
                return BuffType.None;
            }
            if (id == "item_mod_boost") return BuffType.Farming; // 现代增产营养剂 → 收获产量+1
            // 药庐制品与采集家常菜
            if (id == "item_medicine_wine") return BuffType.Trade;  // 药酒：气血畅，行商有劲
            if (id == "item_plaster") return BuffType.Mining;       // 膏药：松筋活络，下矿更耐
            if (id == "item_dish_veg_soup") return BuffType.Farming;
            if (id == "item_dish_fruit_tea") return BuffType.Fishing;
            // 野味菜：大补，给挖矿加成
            if (id == "item_dish_hare_stew" || id == "item_dish_pheasant_soup" || id == "item_dish_deer_roast") return BuffType.Mining;
            if (id.Contains("fish") || id == "item_dish_salad") return BuffType.Fishing;
            if (id == "item_dish_cabbage_soup" || id == "item_dish_mushroom_soup" || id == "item_dish_corn") return BuffType.Mining;
            if (id == "item_dish_radish_soup" || id == "item_dish_tomato" || id == "item_dish_pumpkin") return BuffType.Farming;
            if (id == "item_dish_stew" || id == "item_dish_xuan_chicken" || id == "item_dish_sweetpotato") return BuffType.Trade;
            return BuffType.None;
        }
    }
}
