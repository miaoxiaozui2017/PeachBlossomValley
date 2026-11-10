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
        public int fertilizedLevel; // 施肥等级：0=无肥，1=堆肥(+1)，6=现代肥(+6，产量×3)
        public int pollutedDays;    // 土地污染剩余天数（现代肥料所致）：>0 时不可种植，需空置祛毒

        public PlotData(Vector3Int pos)
        {
            position = pos;
            status = TileStatus.Empty;
            plantedCrop = null;
            growthDays = 0;
            isWateredToday = false;
            fertilizedLevel = 0;
            pollutedDays = 0;
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

            // 订阅季节变化，刷新草地颜色
            if (PBV.Systems.SeasonSystem.Instance != null)
            {
                PBV.Systems.SeasonSystem.Instance.OnSeasonChanged += OnSeasonChanged;
                ApplySeasonGroundColor(PBV.Systems.SeasonSystem.Instance.CurrentSeason);
            }
        }

        private void OnSeasonChanged(PBV.Systems.Season season)
        {
            ApplySeasonGroundColor(season);
        }

        /// <summary>四季草地染色：春绿/夏深绿/秋金黄/冬枯黄</summary>
        private void ApplySeasonGroundColor(PBV.Systems.Season season)
        {
            if (_groundTilemap == null) return;
            switch (season)
            {
                case PBV.Systems.Season.Spring: _groundTilemap.color = new Color(0.45f, 0.62f, 0.35f); break;
                case PBV.Systems.Season.Summer: _groundTilemap.color = new Color(0.32f, 0.55f, 0.28f); break;
                case PBV.Systems.Season.Autumn: _groundTilemap.color = new Color(0.62f, 0.52f, 0.28f); break;
                case PBV.Systems.Season.Winter: _groundTilemap.color = new Color(0.72f, 0.70f, 0.62f); break;
            }
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UseToolEvent -= OnUseTool;
            }
            if (PBV.Systems.SeasonSystem.Instance != null)
            {
                PBV.Systems.SeasonSystem.Instance.OnSeasonChanged -= OnSeasonChanged;
            }
        }

        private void OnUseTool()
        {
            Vector3Int targetPos = InputHandler.Instance.SelectedGridPosition;

            // 左键统一分派：优先识别「选中快捷栏格子」里的物品
            var inv = PBV.Inventory.InventorySystem.Instance;
            var selectedItem = GetSelectedItem();

            // 0) 选中储物箱 → 进入放置模式（放置由 StoragePlacement 处理点击）
            if (selectedItem != null && PBV.Systems.StoragePlacement.IsChestItem(selectedItem))
            {
                if (PBV.Systems.StorageSystem.Instance != null &&
                    !PBV.Systems.StorageSystem.Instance.IsPlacing)
                    PBV.Systems.StorageSystem.Instance.BeginPlacement(null);
                return;
            }

            // 0.5) 放置模式中：左键交给 StoragePlacement，不触发农事操作
            if (PBV.Systems.StorageSystem.Instance != null &&
                PBV.Systems.StorageSystem.Instance.IsPlacing)
                return;

            // 1) 选中肥料 → 施肥
            if (selectedItem != null && PBV.Systems.FertilizerSystem.IsFertilizer(selectedItem))
            {
                if (PBV.Systems.FertilizerSystem.Instance != null)
                    PBV.Systems.FertilizerSystem.Instance.TryFertilize(targetPos, selectedItem);
                return;
            }

            // 2) 选中种子 → 播种
            if (selectedItem != null && selectedItem.type == PBV.Inventory.ItemType.Seed && selectedItem.cropToPlant != null)
            {
                if (IsTillable(targetPos))
                {
                    var plot = GetOrCreatePlot(targetPos);
                    if ((plot.status == TileStatus.Tilled || plot.status == TileStatus.Watered) && plot.plantedCrop == null)
                        TryPlantSelected(targetPos, selectedItem);
                    else
                        PBV.UI.HUD.Instance?.ShowNotice("这里不能播种（需先锄地，或已种作物）");
                }
                return;
            }

            // 3) 选中工具 → 用该工具执行
            var toolFromItem = PBV.Player.ToolManager.ToolTypeFromItemId(selectedItem != null ? selectedItem.id : null);
            if (toolFromItem.HasValue)
            {
                // 区域限制：只能在已铺设的地面（草地）上耕种
                if (!IsTillable(targetPos)) return;
                PlotData plot = GetOrCreatePlot(targetPos);
                int cost = toolFromItem.Value == PBV.Player.ToolType.Sickle
                    ? (PBV.Player.ToolManager.Instance != null ? PBV.Player.ToolManager.Instance.GetStaminaCost(PBV.Player.ToolType.Sickle) : 1)
                    : (PBV.Player.ToolManager.Instance != null ? PBV.Player.ToolManager.Instance.GetStaminaCost(toolFromItem.Value) : 1);
                // 节气加成：雨水/谷雨 浇水不耗体力
                if (toolFromItem.Value == PBV.Player.ToolType.WateringCan && PBV.Systems.SolarTermSystem.HasWaterBonus)
                    cost = 0;
                var stats = PBV.Player.PlayerStats.Instance;
                if (stats != null && !stats.HasEnough(cost))
                {
                    PBV.UI.HUD.Instance?.ShowNotice($"体力不足（需要 {cost}，当前 {stats.Stamina}/{stats.MaxStamina}）· 吃料理 / 睡觉 / 按 N 跳日可恢复");
                    return;
                }
                bool ok = TryUseTool(toolFromItem.Value, targetPos, plot);
                if (ok) stats?.TrySpend(cost);
                return;
            }

            // 3.5) 选中食物/药品 → 左键直接食用（与 F 键一致，实现「全左键使用」）
            if (selectedItem != null && selectedItem.type == PBV.Inventory.ItemType.Consumable)
            {
                PBV.Systems.CookingSystem.TryEatSelected();
                return;
            }

            // 3.6) 选中了其他材料（矿石/木材等）→ 不误触发农事，给出用途提示
            if (selectedItem != null)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"{selectedItem.itemName} 不能对土地使用 · 拿到对应工坊按 E 制作");
                return;
            }

            // 4) 未选中物品：用当前 Q 工具兜底
            if (!IsTillable(targetPos)) return;

            PlotData fallbackPlot = GetOrCreatePlot(targetPos);
            var toolManager = PBV.Player.ToolManager.Instance;
            var tool = toolManager != null ? toolManager.CurrentTool : PBV.Player.ToolType.Hoe;

            bool mature = fallbackPlot.plantedCrop != null && fallbackPlot.growthDays >= fallbackPlot.plantedCrop.daysToMature;
            bool directHarvest = mature && tool != PBV.Player.ToolType.Sickle;
            int fallbackCost = directHarvest
                ? (toolManager != null ? toolManager.GetStaminaCost(PBV.Player.ToolType.Sickle) : 1)
                : (toolManager != null ? toolManager.CurrentStaminaCost : 0);
            // 节气加成：雨水/谷雨 浇水不耗体力（Q 工具兜底路径同样生效）
            if (!directHarvest && tool == PBV.Player.ToolType.WateringCan && PBV.Systems.SolarTermSystem.HasWaterBonus)
                fallbackCost = 0;

            var fallbackStats = PBV.Player.PlayerStats.Instance;
            if (fallbackStats != null && !fallbackStats.HasEnough(fallbackCost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"体力不足（需要 {fallbackCost}，当前 {fallbackStats.Stamina}/{fallbackStats.MaxStamina}）· 吃料理 / 睡觉 / 按 N 跳日可恢复");
                return;
            }

            bool success = directHarvest ? TryHarvestDirect(targetPos) : TryUseTool(tool, targetPos, fallbackPlot);
            if (success) fallbackStats?.TrySpend(fallbackCost);
        }

        /// <summary>获取当前选中快捷栏槽位的物品，空槽返回 null</summary>
        private PBV.Inventory.ItemData GetSelectedItem()
        {
            var inv = PBV.Inventory.InventorySystem.Instance;
            if (inv == null) return null;
            int idx = PBV.UI.InventoryUI.SelectedSlotIndex;
            var slot = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (slot == null || slot.IsEmpty || slot.item == null) return null;
            return slot.item;
        }

        /// <summary>用选中格子的种子播种（一次 1 格、扣 1 颗种子）</summary>
        private bool TryPlantSelected(Vector3Int gridPos, PBV.Inventory.ItemData seed)
        {
            var inventory = PBV.Inventory.InventorySystem.Instance;
            if (inventory == null || _cropManager == null || seed == null) return false;
            CropData crop = seed.cropToPlant;
            if (crop == null) return false;

            if (_cropManager.TryPlantCrop(gridPos, crop))
            {
                inventory.RemoveItem(seed, 1);
                PBV.Systems.QuestManager.Instance?.RecordPlant();
                return true;
            }
            return false;
        }

        /// <summary>左键直收成熟作物（不需要切换到镰刀）</summary>
        private bool TryHarvestDirect(Vector3Int pos)
        {
            if (_cropManager == null) return false;
            if (_cropManager.TryHarvestCrop(pos)) return true;
            PBV.UI.HUD.Instance?.ShowNotice("收获失败：背包已满或作物还没成熟");
            return false;
        }

        /// <summary>根据工具类型尝试操作，成功返回 true</summary>
        private bool TryUseTool(PBV.Player.ToolType tool, Vector3Int pos, PlotData plot)
        {
            switch (tool)
            {
                case PBV.Player.ToolType.Hoe:
                    if (plot.status == TileStatus.Empty)
                    {
                        HandleToolAction(pos, "Hoe");
                        return true;
                    }
                    return false;

                case PBV.Player.ToolType.WateringCan:
                    // 已耕地（无论有无作物）均可浇水
                    if (plot.status == TileStatus.Tilled)
                    {
                        HandleToolAction(pos, "WateringCan");
                        return true;
                    }
                    return false;

                case PBV.Player.ToolType.Sickle:
                    // 用真实返回值：背包满时收获失败，不应扣体力也不应记任务进度
                    return _cropManager != null && _cropManager.TryHarvestCrop(pos);
            }
            return false;
        }

        /// <summary>
        /// 从选中格子的种子播种（已由 TryPlantSelected 统一处理）。
        /// </summary>

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
                        PBV.Systems.QuestManager.Instance?.RecordTill();
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

        /// <summary>该位置是否可耕种（地面 Tilemap 有 tile 才允许）</summary>
        public bool IsTillable(Vector3Int pos) => _groundTilemap != null && _groundTilemap.HasTile(pos);

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

        /// <summary>
        /// 雨天自动浇灌所有已耕地（Tilled→Watered）。
        /// countAsWatered=false 用于雨天：视觉保持湿润，但当天生长已由 CropManager 结算过，
        /// 不再给次日留一次「免费生长」。
        /// </summary>
        public void AutoWaterAll(bool countAsWatered = true)
        {
            int count = 0;
            foreach (var kvp in _plots)
            {
                if (kvp.Value.status == TileStatus.Tilled)
                {
                    kvp.Value.status = TileStatus.Watered;
                    kvp.Value.isWateredToday = countAsWatered;
                    UpdateTileVisual(kvp.Key, _wateredTile);
                    count++;
                }
            }
            if (count > 0) Debug.Log($"[FarmGrid] 雨天自动浇水 {count} 块地");
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
                    fertilized = plot.fertilizedLevel,
                    polluted = plot.pollutedDays,
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
                        fertilizedLevel = dto.fertilized,
                        pollutedDays = dto.polluted,
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
