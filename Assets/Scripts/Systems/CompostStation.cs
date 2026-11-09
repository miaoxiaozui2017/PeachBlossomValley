using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 堆肥桶：谷中原生肥料的制作点。
    /// E 键交互：有稻草/树皮等原生材料时自动沤制堆肥；否则进入施肥模式。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CompostStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "堆肥桶：沤制堆肥 (E)";

        public void OnInteract()
        {
            // 用原生材料制作堆肥；没有材料则提示
            if (!TryMakeCompost())
            {
                PBV.UI.HUD.Instance?.ShowNotice("堆肥桶：需要 稻草×3 或 树皮×2 才能沤制堆肥（去野地/林间采集）");
            }
        }

        /// <summary>用稻草/树皮沤制堆肥，返回是否成功</summary>
        private bool TryMakeCompost()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return false;

            var straw = FindItem("item_straw");
            var bark = FindItem("item_bark");
            var compost = FindItem("item_compost");

            if (compost == null) return false;

            // 稻草优先：3 稻草 → 1 堆肥
            if (straw != null && inv.CountItem(straw) >= 3)
            {
                inv.RemoveItem(straw, 3);
                inv.AddItem(compost, 1);
                PBV.UI.HUD.Instance?.ShowNotice("堆肥桶沤制完成：稻草 ×3 → 堆肥 ×1");
                Debug.Log("[Compost] 稻草沤制堆肥");
                return true;
            }
            // 其次树皮：2 树皮 → 1 堆肥
            if (bark != null && inv.CountItem(bark) >= 2)
            {
                inv.RemoveItem(bark, 2);
                inv.AddItem(compost, 1);
                PBV.UI.HUD.Instance?.ShowNotice("堆肥桶沤制完成：树皮 ×2 → 堆肥 ×1");
                Debug.Log("[Compost] 树皮沤制堆肥");
                return true;
            }

            return false;
        }

        private static ItemData FindItem(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
