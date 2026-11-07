using System;
using UnityEngine;

namespace PBV.Player
{
    /// <summary>工具类型</summary>
    public enum ToolType
    {
        Hoe,           // 锄头：空地→已耕地
        WateringCan,   // 水壶：已耕地→浇水（有无作物均可）
        Sickle,        // 镰刀：收获成熟作物
        Seed           // 种子包：已耕地→播种
    }

    /// <summary>
    /// 工具管理器：Q 键循环切换工具，FarmGrid 根据当前工具执行对应动作。
    /// 解决 v0.1.0 Space 单键上下文歧义（分不清想种还是想浇）。
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        public ToolType CurrentTool { get; private set; } = ToolType.Hoe;

        /// <summary>工具变化事件</summary>
        public event Action<ToolType> OnToolChanged;

        private readonly ToolType[] _cycle = { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Seed };
        private int _index = 0;

        // 工具中文名（供 HUD 显示）
        private static readonly string[] _toolNames = { "锄头", "水壶", "镰刀", "种子包" };
        public string CurrentToolName => _toolNames[_index];

        // 各工具体力消耗
        private static readonly int[] _staminaCosts = { 3, 2, 1, 1 };
        public int CurrentStaminaCost => _staminaCosts[_index];

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (PBV.Core.InputHandler.Instance != null)
            {
                PBV.Core.InputHandler.Instance.ToolCycleEvent += CycleTool;
            }
        }

        private void OnDestroy()
        {
            if (PBV.Core.InputHandler.Instance != null)
            {
                PBV.Core.InputHandler.Instance.ToolCycleEvent -= CycleTool;
            }
        }

        /// <summary>Q 键循环切换到下一个工具</summary>
        public void CycleTool()
        {
            _index = (_index + 1) % _cycle.Length;
            CurrentTool = _cycle[_index];
            OnToolChanged?.Invoke(CurrentTool);
            Debug.Log($"[Tool] 切换至 {CurrentToolName}");
        }

        /// <summary>直接选择工具</summary>
        public void SelectTool(ToolType tool)
        {
            int idx = Array.IndexOf(_cycle, tool);
            if (idx < 0) return;
            _index = idx;
            CurrentTool = tool;
            OnToolChanged?.Invoke(CurrentTool);
        }

        public static string GetToolName(ToolType tool)
        {
            int idx = Array.IndexOf(new[] { ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Seed }, tool);
            return idx >= 0 ? _toolNames[idx] : tool.ToString();
        }
    }
}
