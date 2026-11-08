using System;
using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.Economy
{
    /// <summary>商店商品条目</summary>
    [Serializable]
    public class ShopEntry
    {
        public ItemData item;
        public int price;
        [Tooltip("-1 = 无限库存")]
        public int stock = -1;
    }

    /// <summary>
    /// 商店系统：商人 NPC 挂载，E 键交互打开商店 UI。
    /// v0.2.0 专注"购买"（种子/工具），卖出走出货箱。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ShopSystem : MonoBehaviour, IInteractable
    {
        [Header("Shop Info")]
        [SerializeField] private string _shopName = "杂货铺";

        [Header("商品列表")]
        [SerializeField] private ShopEntry[] _items;

        public string ShopName => _shopName;
        public ShopEntry[] Items => _items;

        /// <summary>运行时追加商品（内容扩展用，幂等）</summary>
        public void AddShopEntry(ItemData item, int price)
        {
            if (item == null) return;
            if (_items != null)
                foreach (var e in _items)
                    if (e != null && e.item == item) return;
            var list = new System.Collections.Generic.List<ShopEntry>();
            if (_items != null) list.AddRange(_items);
            list.Add(new ShopEntry { item = item, price = price, stock = -1 });
            _items = list.ToArray();
        }

        public string GetInteractPrompt() => $"打开{_shopName}";

        public void OnInteract()
        {
            if (PBV.UI.ShopUI.Instance != null)
            {
                PBV.UI.ShopUI.Instance.OpenShop(this);
            }
        }
    }
}
