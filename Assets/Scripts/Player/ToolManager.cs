using System;
using UnityEngine;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.Player
{
    /// <summary>工具类型</summary>
    public enum ToolType
    {
        Hoe,           // 锄头：空地→已耕地
        WateringCan,   // 水壶：已耕地→浇水
        Sickle,        // 镰刀：收获成熟作物
        Seed           // 种子包：已耕地→播种
    }

    /// <summary>
    /// 工具管理器：Q 键循环切换，工具升级（Lv1-3，升级减体力消耗）。
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        public ToolType CurrentTool { get; private set; } = ToolType.Hoe;

        public event Action<ToolType> OnToolChanged;
        public event Action<ToolType, int> OnToolUpgraded;

        private readonly ToolType[] _cycle = { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Seed };
        private int _index = 0;

        private static readonly string[] _toolNames = { "锄头", "水壶", "镰刀", "种子包" };
        public string CurrentToolName => _toolNames[_index];

        // 基础体力消耗
        private static readonly int[] _staminaCosts = { 3, 2, 1, 1 };
        // 工具等级（1-3）
        private readonly int[] _toolLevels = { 1, 1, 1, 1 };

        /// <summary>当前工具体力消耗（等级越高消耗越低）</summary>
        public int CurrentStaminaCost => Mathf.Max(0, _staminaCosts[_index] - (_toolLevels[_index] - 1));

        public int CurrentLevel => _toolLevels[_index];

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (PBV.Core.InputHandler.Instance != null)
                PBV.Core.InputHandler.Instance.ToolCycleEvent += CycleTool;
        }

        private void OnDestroy()
        {
            if (PBV.Core.InputHandler.Instance != null)
                PBV.Core.InputHandler.Instance.ToolCycleEvent -= CycleTool;
        }

        public void CycleTool()
        {
            _index = (_index + 1) % _cycle.Length;
            CurrentTool = _cycle[_index];
            OnToolChanged?.Invoke(CurrentTool);
            Debug.Log($"[Tool] 切换至 {CurrentToolName} Lv{_toolLevels[_index]}");
        }

        public void SelectTool(ToolType tool)
        {
            int idx = Array.IndexOf(_cycle, tool);
            if (idx < 0) return;
            _index = idx;
            CurrentTool = tool;
            OnToolChanged?.Invoke(CurrentTool);
        }

        public int GetToolLevel(int toolIndex) => _toolLevels[toolIndex];
        public string GetToolName(int toolIndex) => _toolNames[toolIndex];
        public int ToolCount => _cycle.Length;

        /// <summary>升级消耗：(矿石id, 矿石数量, 金钱)。满级返回null</summary>
        public (string oreId, int oreCount, int money)? GetUpgradeCost(int toolIndex)
        {
            int level = _toolLevels[toolIndex];
            if (level >= 3) return null;
            return level == 1 ? ("item_ore_copper", 3, 100) : ("item_ore_iron", 3, 300);
        }

        /// <summary>尝试升级工具，成功返回true</summary>
        public bool TryUpgrade(int toolIndex)
        {
            if (toolIndex < 0 || toolIndex >= _cycle.Length) return false;
            int level = _toolLevels[toolIndex];
            if (level >= 3) return false;

            var cost = GetUpgradeCost(toolIndex);
            if (cost == null) return false;

            // 查找矿石物品
            var ore = FindItemById(cost.Value.oreId);
            if (ore == null || InventorySystem.Instance == null) return false;
            if (InventorySystem.Instance.CountItem(ore) < cost.Value.oreCount) return false;
            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(cost.Value.money)) return false;

            InventorySystem.Instance.RemoveItem(ore, cost.Value.oreCount);
            _toolLevels[toolIndex]++;
            OnToolUpgraded?.Invoke(_cycle[toolIndex], _toolLevels[toolIndex]);
            Debug.Log($"[Tool] {_toolNames[toolIndex]} 升级到 Lv{_toolLevels[toolIndex]}");
            return true;
        }

        private ItemData FindItemById(string id)
        {
            var all = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var i in all) if (i.id == id) return i;
            return null;
        }

        public static string GetToolName(ToolType tool)
        {
            int idx = Array.IndexOf(new[] { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Seed }, tool);
            return idx >= 0 ? _toolNames[idx] : tool.ToString();
        }

        // ===== 存档 =====
        public int[] ExportLevels() => (int[])_toolLevels.Clone();
        public void ImportLevels(int[] levels)
        {
            if (levels == null || levels.Length != 4) return;
            for (int i = 0; i < 4; i++) _toolLevels[i] = Mathf.Clamp(levels[i], 1, 3);
        }
    }
}
