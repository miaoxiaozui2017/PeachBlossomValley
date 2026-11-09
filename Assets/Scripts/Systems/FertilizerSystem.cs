using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Farming;

namespace PBV.Systems
{
    /// <summary>
    /// 施肥系统：肥料是背包物品，选中后左键对着「没有种子的已耕地」使用即可施肥。
    /// 肥料两种：堆肥（谷中原生，产量+1）、现代复合肥（现代商店购买，产量×3，高风险高收益）。
    /// 现代复合肥会污染土地，收获后需空置祛毒数日。
    /// </summary>
    public class FertilizerSystem : MonoBehaviour
    {
        public static FertilizerSystem Instance { get; private set; }

        /// <summary>现代复合肥导致的土地污染天数：收获后需空置这么久才能再种</summary>
        public const int ModernPollutionDays = 3;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>判断某物品是否为肥料（堆肥 / 现代复合肥）</summary>
        public static bool IsFertilizer(ItemData item)
        {
            if (item == null) return false;
            return item.id == "item_compost" || item.id == "item_fertilizer_modern";
        }

        /// <summary>肥料的产量加成：堆肥=+1，现代肥=+6（产量×3，高风险高收益），非肥料=0</summary>
        public static int FertilizerBonus(ItemData item)
        {
            if (item == null) return 0;
            if (item.id == "item_compost") return 1;
            if (item.id == "item_fertilizer_modern") return 6;
            return 0;
        }

        /// <summary>
        /// 用指定肥料对地块施肥（选中背包物品 + 左键触发）。
        /// 只能对「已耕且没有种子（未种植）」的地块使用。
        /// 成功则消耗 1 份肥料并返回 true。
        /// </summary>
        public bool TryFertilize(Vector3Int pos, ItemData fertilizer)
        {
            if (fertilizer == null || !IsFertilizer(fertilizer))
            {
                PBV.UI.HUD.Instance?.ShowNotice("这不是肥料");
                return false;
            }

            var farmGrid = FindObjectOfType<FarmGrid>();
            if (farmGrid == null) return false;

            var plot = farmGrid.GetPlot(pos);
            if (plot == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这里不是耕地，无法施肥");
                return false;
            }
            if (plot.status != TileStatus.Tilled && plot.status != TileStatus.Watered)
            {
                PBV.UI.HUD.Instance?.ShowNotice("先锄好地再施肥");
                return false;
            }
            if (plot.plantedCrop != null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这块地已经种了作物，只能对没有种子的地块施肥");
                return false;
            }

            int bonus = FertilizerBonus(fertilizer);
            if (plot.fertilizedLevel >= bonus)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这块地已施过肥（或当前肥料加成不高于现有），无需重复施肥");
                return false;
            }

            var inv = InventorySystem.Instance;
            if (inv == null || inv.CountItem(fertilizer) < 1)
            {
                PBV.UI.HUD.Instance?.ShowNotice("肥料不足");
                return false;
            }

            inv.RemoveItem(fertilizer, 1);
            plot.fertilizedLevel = bonus;

            bool isModern = fertilizer.id == "item_fertilizer_modern";
            if (isModern)
            {
                plot.pollutedDays = ModernPollutionDays;
                PBV.UI.HUD.Instance?.ShowNotice($"已施现代复合肥（收获产量 ×3）· 注意：收获后土地将污染 {ModernPollutionDays} 天，需空置祛毒");
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice($"已施肥：{fertilizer.itemName}（收获产量 +{bonus}）");
            }
            Debug.Log($"[Fertilizer] 对 {pos} 施肥 {fertilizer.itemName}，加成 +{bonus}{(isModern ? $"，污染 {ModernPollutionDays} 天" : "")}");
            return true;
        }

        private static ItemData FindItem(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
