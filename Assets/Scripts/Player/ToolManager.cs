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
        Axe            // 斧头：砍树得木材，也可采集浆果
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

        private readonly ToolType[] _cycle = { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Axe };
        private int _index = 0;

        private static readonly string[] _toolNames = { "锄头", "水壶", "镰刀", "斧头" };
        public string CurrentToolName => _toolNames[_index];

        // 工具物品 id 映射（工具做成背包物品放入快捷栏格子）
        private static readonly string[] _toolItemIds = { "item_tool_hoe", "item_tool_wateringcan", "item_tool_sickle", "item_tool_axe" };

        // 基础体力消耗
        private static readonly int[] _staminaCosts = { 3, 2, 1, 4 };
        // 工具等级（1-3）
        private readonly int[] _toolLevels = { 1, 1, 1, 1 };

        /// <summary>指定工具的体力消耗（等级越高消耗越低）</summary>
        public int GetStaminaCost(ToolType tool)
        {
            int idx = Array.IndexOf(_cycle, tool);
            return idx < 0 ? 0 : Mathf.Max(0, _staminaCosts[idx] - (_toolLevels[idx] - 1));
        }

        /// <summary>当前工具体力消耗（等级越高消耗越低）</summary>
        public int CurrentStaminaCost => GetStaminaCost(CurrentTool);

        public int CurrentLevel => _toolLevels[_index];

        /// <summary>工具效果说明（升级面板/帮助页用）</summary>
        public string GetToolEffect(int toolIndex) => DescribeEffect(toolIndex, _toolLevels[toolIndex]);

        /// <summary>指定等级下的效果说明（用于预览"下级会变成什么"）</summary>
        public static string DescribeEffect(int toolIndex, int level)
        {
            int cost = Mathf.Max(0, _staminaCosts[toolIndex] - (level - 1));
            return $"每次耗 {cost} 体力";
        }

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

        /// <summary>升级消耗：(矿石id, 矿石数量, 金钱)。满级返回null。</summary>
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
            if (ore == null || InventorySystem.Instance == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"升级{_toolNames[toolIndex]}缺材料：{cost.Value.oreId} 资产未找到");
                return false;
            }
            if (InventorySystem.Instance.CountItem(ore) < cost.Value.oreCount)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"升级{_toolNames[toolIndex]}缺 {ore.itemName} ×{cost.Value.oreCount}（去矿洞 M 挖矿）");
                return false;
            }
            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.SpendSong(cost.Value.money))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"升级{_toolNames[toolIndex]}宋币不足，需 {cost.Value.money} 文");
                return false;
            }

            InventorySystem.Instance.RemoveItem(ore, cost.Value.oreCount);
            _toolLevels[toolIndex]++;
            OnToolUpgraded?.Invoke(_cycle[toolIndex], _toolLevels[toolIndex]);
            // 升级会改变体力消耗，通知 HUD 立刻刷新「耗 N 体力」提示
            OnToolChanged?.Invoke(_cycle[toolIndex]);
            PBV.UI.HUD.Instance?.ShowNotice($"{_toolNames[toolIndex]} 升级到 Lv{_toolLevels[toolIndex]}：{DescribeEffect(toolIndex, _toolLevels[toolIndex])}");
            Debug.Log($"[Tool] {_toolNames[toolIndex]} 升级到 Lv{_toolLevels[toolIndex]}");
            return true;
        }

        private ItemData FindItemById(string id)
        {
            var all = Resources.LoadAll<ItemData>("Data");
            foreach (var i in all) if (i.id == id) return i;
            return null;
        }

        public static string GetToolName(ToolType tool)
        {
            int idx = Array.IndexOf(new[] { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Axe }, tool);
            return idx >= 0 ? _toolNames[idx] : tool.ToString();
        }

        /// <summary>根据工具物品 id 获取对应工具类型（工具占格子用）；非工具返回 null</summary>
        public static ToolType? ToolTypeFromItemId(string itemId)
        {
            var cycle = new[] { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Axe };
            for (int i = 0; i < _toolItemIds.Length; i++)
                if (_toolItemIds[i] == itemId) return cycle[i];
            return null;
        }

        /// <summary>根据工具类型获取对应工具物品 id（工具占格子用）</summary>
        public static string ItemIdFromToolType(ToolType tool)
        {
            var cycle = new[] { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Axe };
            int idx = Array.IndexOf(cycle, tool);
            return idx >= 0 ? _toolItemIds[idx] : null;
        }

        // ===== 存档 =====
        public int[] ExportLevels() => (int[])_toolLevels.Clone();
        public void ImportLevels(int[] levels)
        {
            if (levels == null || levels.Length < 4) return;
            // 兼容旧档：旧档可能有 5 个工具（含种子包），斧头默认 Lv1
            for (int i = 0; i < _toolLevels.Length && i < levels.Length; i++) _toolLevels[i] = Mathf.Clamp(levels[i], 1, 3);
        }
    }
}
