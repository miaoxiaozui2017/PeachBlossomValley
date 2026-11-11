using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;
using PBV.Save;
using PBV.Systems;

namespace PBV.Core
{
    /// <summary>
    /// 新游戏初始物资（从 Resources/Data 自动加载）
    /// 读档时不发放；但若存档异常（空背包+0金钱，如被测试污染），兜底补发。
    /// 修复 playtest bug: 冒烟测试残留存档导致玩家开局背包空。
    /// </summary>
    public class StarterKit : MonoBehaviour
    {
        [Header("初始物资（物品名:数量，物品需位于 Resources/Data）")]
        [SerializeField] private string[] _startingItems = new string[]
        {
            "item_tool_hoe:1",
            "item_tool_wateringcan:1",
            "item_tool_sickle:1",
            "item_tool_axe:1",
            "seed_radish:10",
            "seed_cabbage:8",
            "seed_spinach:5",
            "item_bark:4",
            "item_straw:4"
        };

        private bool _applied;

        private void Update()
        {
            if (_applied) return;
            _applied = true;

            var inventory = InventorySystem.Instance;
            if (inventory == null)
            {
                _applied = false; // 等下一帧重试
                return;
            }

            bool loadedFromSave = SaveManager.Instance != null && SaveManager.Instance.LoadedFromSave;
            // 两界背包都要检查，否则「谷内有货、人在现代」会被误判成异常存档重复发物资
            bool inventoryEmpty = inventory.ExportWorld(PBV.Systems.WorldType.Ancient).Count == 0
                               && inventory.ExportWorld(PBV.Systems.WorldType.Modern).Count == 0;
            int money = PlayerWallet.Instance != null ? (PlayerWallet.Instance.SongCoins + PlayerWallet.Instance.RMB) : 0;

            // 读档且状态正常（有东西或有钱）-> 不发
            if (loadedFromSave && !(inventoryEmpty && money == 0))
            {
                Debug.Log($"[StarterKit] 已读档(钱={money}, 背包空={inventoryEmpty})，跳过初始物资");
                return;
            }

            // 新游戏 或 异常存档 -> 发放
            GrantStarterItems(inventory);
            Debug.Log(loadedFromSave
                ? "[StarterKit] 检测到异常存档(空背包+0金钱)，补发初始物资"
                : "[StarterKit] 初始物资已发放");
        }

        private void GrantStarterItems(InventorySystem inventory)
        {
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
                    // 开局物资永远属于桃花谷，即便读档时人身在现代
                    inventory.AddItemToValley(item, count);
                }
            }
        }

    }
}
