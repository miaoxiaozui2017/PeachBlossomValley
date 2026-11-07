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
            // 优先级：成熟收获 > 空地锄地 > 无作物播种/浇水 > 有作物浇水
            PlotData plot = GetOrCreatePlot(targetPos);

            // 1. 成熟作物 -> 镰刀收获（最高优先级）
            if (plot.plantedCrop != null && plot.growthDays >= plot.plantedCrop.daysToMature)
            {
                HandleToolAction(targetPos, "Sickle");
                return;
            }

            // 2. 空地 -> 锄地
            if (plot.status == TileStatus.Empty)
            {
                HandleToolAction(targetPos, "Hoe");
                return;
            }

            // 3. 已耕地/已浇水 且无作物 -> 有种子则播种；无种子且未浇水则浇水
            if ((plot.status == TileStatus.Tilled || plot.status == TileStatus.Watered) && plot.plantedCrop == null)
            {
                if (TryPlantFromInventory(targetPos)) return;
                // 无种子：Tilled 顺带浇水（方便玩家先整地），Watered 则无事
                if (plot.status == TileStatus.Tilled)
                {
                    HandleToolAction(targetPos, "WateringCan");
                }
                return;
            }

            // 4. 有作物且未成熟 -> 浇水（修复 playtest bug：播种后地块仍 Tilled 时按 Space 无反应）
            if (plot.plantedCrop != null && plot.status == TileStatus.Tilled)
            {
                HandleToolAction(targetPos, "WateringCan");
                return;
            }

            // 5. 有作物且已浇水 -> 当天无需操作
        }

        /// <summary>
        /// 从背包取种子播种，成功则消耗 1 颗种子
        /// </summary>
        private bool TryPlantFromInventory(Vector3Int gridPos)
        {
            var inventory = PBV.Inventory.InventorySystem.Instance;
            if (inventory == null || _cropManager == null) return false;

            var seedSlot = inventory.FindSeedSlot();
            if (seedSlot == null) return false;

            CropData crop = seedSlot.item.cropToPlant;
            if (_cropManager.TryPlantCrop(gridPos, crop))
            {
                inventory.RemoveItem(seedSlot.item, 1);
                return true;
            }
            return false;
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
                case "Seed": // 种子包：直接指定作物播种
                    if ((plot.status == TileStatus.Tilled || plot.status == TileStatus.Watered) && plot.plantedCrop == null)
                    {
                        var inventory = PBV.Inventory.InventorySystem.Instance;
                        var seedSlot = inventory?.FindSeedSlot();
                        if (seedSlot != null && _cropManager != null && _cropManager.TryPlantCrop(gridPos, seedSlot.item.cropToPlant))
                        {
                            inventory.RemoveItem(seedSlot.item, 1);
                        }
                    }
                    break;
                case "Sickle": // 镰刀：收获成熟作物
                    if (plot.plantedCrop != null && plot.growthDays >= plot.plantedCrop.daysToMature)
                    {
                        if (_cropManager != null) _cropManager.TryHarvestCrop(gridPos);
                    }
                    break;
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

        /// <summary>查询地块（不存在则返回 null，不创建）—— 供 UI/指示器读取状态</summary>
        public PlotData GetPlot(Vector3Int pos)
        {
            _plots.TryGetValue(pos, out var plot);
            return plot;
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

        // ---------------- 存档接口 ----------------

        /// <summary>
        /// 导出改变过的地块（稀疏存储：跳过 Empty 且无作物的地块）
        /// </summary>
        public System.Collections.Generic.List<PBV.Save.PlotDTO> ExportChangedPlots()
        {
            var result = new System.Collections.Generic.List<PBV.Save.PlotDTO>();
            foreach (var kvp in _plots)
            {
                var plot = kvp.Value;
                if (plot.status == TileStatus.Empty && plot.plantedCrop == null) continue;

                result.Add(new PBV.Save.PlotDTO
                {
                    x = plot.position.x,
                    y = plot.position.y,
                    status = (int)plot.status,
                    growthDays = plot.growthDays,
                    wateredToday = plot.isWateredToday,
                    cropName = plot.plantedCrop != null ? plot.plantedCrop.cropName : string.Empty
                });
            }
            return result;
        }

        /// <summary>
        /// 从存档恢复地块（cropResolver: 作物名 -> CropData）
        /// </summary>
        public void ImportPlots(System.Collections.Generic.List<PBV.Save.PlotDTO> data, System.Func<string, CropData> cropResolver)
        {
            _plots.Clear();

            if (data != null)
            {
                foreach (var dto in data)
                {
                    var pos = new Vector3Int(dto.x, dto.y, 0);
                    var plot = new PlotData(pos)
                    {
                        status = (TileStatus)dto.status,
                        growthDays = dto.growthDays,
                        isWateredToday = dto.wateredToday,
                        plantedCrop = cropResolver != null ? cropResolver(dto.cropName) : null
                    };
                    _plots[pos] = plot;

                    // 恢复地块视觉
                    TileBase tile = null;
                    if (plot.status == TileStatus.Tilled) tile = _tilledTile;
                    else if (plot.status == TileStatus.Watered) tile = _wateredTile;
                    if (tile != null) UpdateTileVisual(pos, tile);
                }
            }

            // 恢复作物视觉
            if (_cropManager != null) _cropManager.RefreshAllVisuals();
        }
    }
}
