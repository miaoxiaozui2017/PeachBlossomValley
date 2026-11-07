using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using PBV.Core;

namespace PBV.Farming
{
    /// <summary>
    /// 地块状态枚举
    /// </summary>
    public enum TileStatus
    {
        Empty = 0,   // 未开垦
        Tilled,      // 已耕地
        Watered,     // 已浇水
        Blocked      // 不可耕种
    }

    /// <summary>
    /// 单个地块的运行时数据
    /// </summary>
    [Serializable]
    public class PlotData
    {
        public Vector3Int position;
        public TileStatus status;
        public CropData plantedCrop;
        public int growthDays; // 当前生长时间
        public bool isWateredToday;

        public PlotData(Vector3Int pos)
        {
            position = pos;
            status = TileStatus.Empty;
            plantedCrop = null;
            growthDays = 0;
            isWateredToday = false;
        }
    }

    /// <summary>
    /// 农田网格管理器
    /// 负责处理Tilemap交互、存储地块数据
    /// </summary>
    public class FarmGrid : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap _groundTilemap; // 基础地面
        [SerializeField] private Tilemap _interactTilemap; // 耕地/浇水层
        [SerializeField] private Tilemap _cropTilemap; // 作物层 (如果使用Tilemap显示作物)
        [SerializeField] private CropManager _cropManager;

        [Header("Tile Assets")]
        [SerializeField] private TileBase _tilledTile;
        [SerializeField] private TileBase _wateredTile;
        
        [Header("Debug")]
        [SerializeField] private CropData _debugCrop; // 调试用默认种子
        
        // 存储所有活动地块的数据
        // Key: Grid Position, Value: PlotData
        private Dictionary<Vector3Int, PlotData> _plots = new Dictionary<Vector3Int, PlotData>();

        private void Start()
        {
            if (_cropManager == null) _cropManager = GetComponent<CropManager>();

            // 订阅 InputHandler 事件
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UseToolEvent += OnUseTool;
            }
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UseToolEvent -= OnUseTool;
            }
        }

        private void OnUseTool()
        {
            Vector3Int targetPos = InputHandler.Instance.SelectedGridPosition;
            
            // 上下文敏感的智能工具逻辑 (Smart Tool Logic for MVP)
            PlotData plot = GetOrCreatePlot(targetPos);

            // 1. 如果有成熟作物 -> 收获
            if (plot.plantedCrop != null && plot.growthDays >= plot.plantedCrop.daysToMature)
            {
                if (_cropManager != null) _cropManager.TryHarvestCrop(targetPos);
                return;
            }

            // 2. 如果是空地 -> 锄地
            if (plot.status == TileStatus.Empty)
            {
                HandleToolAction(targetPos, "Hoe");
                return;
            }

            // 3. 如果已耕地 -> 尝试种植 (如果有种子) 或 浇水
            if (plot.status == TileStatus.Tilled)
            {
                // 简单的逻辑：没作物就种，有作物就浇水
                // 这里为了测试，如果有 Debug Crop 且该格没作物，优先种植
                if (plot.plantedCrop == null && _debugCrop != null)
                {
                    if (_cropManager != null && _cropManager.TryPlantCrop(targetPos, _debugCrop))
                    {
                        return; // 种植成功
                    }
                }
                
                // 否则浇水
                HandleToolAction(targetPos, "WateringCan");
                return;
            }
            
            // 4. 如果已浇水但没作物 -> 尝试种植
             if (plot.status == TileStatus.Watered && plot.plantedCrop == null && _debugCrop != null)
             {
                 if (_cropManager != null && _cropManager.TryPlantCrop(targetPos, _debugCrop))
                 {
                     return;
                 }
             }
        }

        public void HandleToolAction(Vector3Int gridPos, string toolType)
        {
            PlotData plot = GetOrCreatePlot(gridPos);

            switch (toolType)
            {
                case "Hoe": // 锄头
                    if (plot.status == TileStatus.Empty)
                    {
                        plot.status = TileStatus.Tilled;
                        UpdateTileVisual(gridPos, _tilledTile);
                        Debug.Log($"Tilled ground at {gridPos}");
                    }
                    break;
                case "WateringCan": // 水壶
                    if (plot.status == TileStatus.Tilled || plot.status == TileStatus.Watered)
                    {
                        plot.status = TileStatus.Watered;
                        plot.isWateredToday = true;
                        UpdateTileVisual(gridPos, _wateredTile);
                        Debug.Log($"Watered ground at {gridPos}");
                    }
                    break;
                // TODO: 种子、镰刀逻辑
            }
        }

        private PlotData GetOrCreatePlot(Vector3Int pos)
        {
            if (!_plots.ContainsKey(pos))
            {
                _plots[pos] = new PlotData(pos);
            }
            return _plots[pos];
        }

        private void UpdateTileVisual(Vector3Int pos, TileBase tile)
        {
            if (_interactTilemap != null)
            {
                _interactTilemap.SetTile(pos, tile);
            }
        }

        // 供 CropManager 调用
        public Dictionary<Vector3Int, PlotData> GetAllPlots() => _plots;
        
        // 供每日刷新调用
        public void ResetWaterState()
        {
            foreach (var kvp in _plots)
            {
                kvp.Value.isWateredToday = false;
                if (kvp.Value.status == TileStatus.Watered)
                {
                    kvp.Value.status = TileStatus.Tilled;
                    UpdateTileVisual(kvp.Key, _tilledTile);
                }
            }
        }
    }
}
