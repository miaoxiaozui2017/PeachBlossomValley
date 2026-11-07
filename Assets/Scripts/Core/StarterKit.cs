using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Save;

namespace PBV.Core
{
    /// <summary>
    /// 新游戏初始物资（从 Resources/Data 自动加载，读档时不发放）
    /// </summary>
    public class StarterKit : MonoBehaviour
    {
        [Header("初始物资（物品名:数量，物品需位于 Resources/Data）")]
        [SerializeField] private string[] _startingItems = new string[]
        {
            "seed_radish:15",
            "seed_cabbage:10"
        };

        private bool _applied;

        private void Update()
        {
            if (_applied) return;
            _applied = true;

            // 等待 SaveManager.Start 完成：读档则跳过
            if (SaveManager.Instance != null && SaveManager.Instance.LoadedFromSave) return;

            var inventory = InventorySystem.Instance;
            if (inventory == null) return;

            foreach (var entry in _startingItems)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;

                var item = Resources.Load<ItemData>($"Data/{parts[0]}");
                if (item == null)
                {
                    Debug.LogWarning($"[StarterKit] 未找到初始物品: {parts[0]}");
                    continue;
                }

                if (int.TryParse(parts[1], out int count))
                {
                    inventory.AddItem(item, count);
                }
            }

            Debug.Log("[StarterKit] 初始物资已发放");
        }
    }
}
