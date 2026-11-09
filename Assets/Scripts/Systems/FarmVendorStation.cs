using UnityEngine;
using PBV.Economy;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 老农小摊：卖少量基础种子 + 自制堆肥（谷中原生肥料）。
    /// 老农 NPC 站在摊旁，交易走摊位（符合「NPC 融合建筑、交易用建筑本身」）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(ShopSystem))]
    public class FarmVendorStation : MonoBehaviour
    {
        private void Start()
        {
            var shop = GetComponent<ShopSystem>();
            if (shop == null) return;

            Add(shop, "seed_radish", 12);
            Add(shop, "seed_cabbage", 12);
            Add(shop, "seed_spinach", 12);
            Add(shop, "seed_wheat", 14);
            Add(shop, "seed_soybean", 14);
            // 自制堆肥（老农沤的天然肥料，产量 +1，不污染土地）
            Add(shop, "item_compost", 10);
        }

        private static void Add(ShopSystem shop, string id, int price)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
            {
                if (item != null && item.id == id)
                {
                    shop.AddShopEntry(item, price);
                    return;
                }
            }
        }
    }
}
