using UnityEngine;
using PBV.Economy;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 酒楼（大厨饱饱的店铺）：买卖生鲜原料（蛋奶肉等）与做好的菜肴。
    /// 玩家可在这里补充养殖业/烹饪所需食材，也可把手艺卖给大厨。
    /// 交互打开商店面板。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(ShopSystem))]
    public class TavernStation : MonoBehaviour
    {
        private void Start()
        {
            var shop = GetComponent<ShopSystem>();
            if (shop == null) return;

            // 生鲜原料：蛋奶（养殖业补充）
            Add(shop, "item_egg", 25);
            Add(shop, "item_duck_egg", 30);
            Add(shop, "item_milk", 35);
            // 肉：养殖/屠宰补充
            Add(shop, "item_chicken_meat", 55);
            Add(shop, "item_pork", 70);
            Add(shop, "item_mutton", 80);
            Add(shop, "item_beef", 90);
            // 调味与甜味
            Add(shop, "item_salt", 55);
            Add(shop, "item_sugar", 50);
            Add(shop, "item_soysauce", 40);
            Add(shop, "item_vinegar", 40);
            Add(shop, "item_soyoil", 45);
            // 家常原料
            Add(shop, "item_rice", 30);
            Add(shop, "item_flour", 25);
            Add(shop, "item_scallion", 12);
            Add(shop, "item_ginger", 10);
            // 做好的招牌菜（可买来吃/送礼）
            Add(shop, "item_dish_braised_pork", 120);
            Add(shop, "item_dish_beef_noodle", 130);
            Add(shop, "item_dish_roast_chicken", 110);
            Add(shop, "item_dish_mutton_soup", 115);
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
