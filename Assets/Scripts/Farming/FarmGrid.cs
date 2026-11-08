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

            // 区域限制：只能在已铺设的地面（草地）上耕种，绿地外不响应
            if (!IsTillable(targetPos)) return;

            PlotData plot = GetOrCreatePlot(targetPos);

            // 按 ToolManager 当前工具执行（v0.2.0：取代上下文智能单键，消除歧义）
            var toolManager = PBV.Player.ToolManager.Instance;
            var tool = toolManager != null ? toolManager.CurrentTool : PBV.Player.ToolType.Hoe;

            // 成熟作物：左键直接收获，不必先切镰刀；体力按镰刀等级消耗计。
            bool mature = plot.plantedCrop != null && plot.growthDays >= plot.plantedCrop.daysToMature;
            bool directHarvest = mature && tool != PBV.Player.ToolType.Sickle;
            int cost = directHarvest
                ? (toolManager != null ? toolManager.GetStaminaCost(PBV.Player.ToolType.Sickle) : 1)
                : (toolManager != null ? toolManager.CurrentStaminaCost : 0);

            // 关键：先查体力再执行动作。以前是"先干完活再 TrySpend 且忽略返回值"，
            // 导致体力为 0 时依旧能锄地/浇水/收获。
            var stats = PBV.Player.PlayerStats.Instance;
            if (stats != null && !stats.HasEnough(cost))
            {
                PBV.UI.HUD.Instance?.ShowNotice(
                    $"体力不足（需要 {cost}，当前 {stats.Stamina}/{stats.MaxStamina}）· 吃料理 / 睡觉 / 按 N 跳日可恢复");
                return;
            }

            bool success = directHarvest ? TryHarvestDirect(targetPos) : TryUseTool(tool, targetPos, plot);
            if (success) stats?.TrySpend(cost);
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

                case PBV.Player.ToolType.Seed:
                    if ((plot.status == TileStatus.Tilled || plot.status == TileStatus.Watered) && plot.plantedCrop == null)
                    {
                        return TryPlantFromInventory(pos);
                    }
                    return false;
            }
            return false;
        }

        /// <summary>
        /// 从背包取种子播种。种子包等级决定一次可覆盖几格（Lv1=1 / Lv2=3 / Lv3=5），
        /// 每格消耗 1 颗种子，整个动作只扣 1 次体力。
        /// </summary>
        private bool TryPlantFromInventory(Vector3Int gridPos)
        {
            var inventory = PBV.Inventory.InventorySystem.Instance;
            if (inventory == null || _cropManager == null) return false;

            var tm = PBV.Player.ToolManager.Instance;
            int maxPlots = tm != null ? tm.SeedPlotsPerUse : 1;

            // 顺序：脚下 → 右 → 左 → 上 → 下；只挑已耕且空着的地块
            Vector3Int[] offsets =
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            };

            int planted = 0;
            for (int i = 0; i < offsets.Length && planted < maxPlots; i++)
            {
                Vector3Int p = gridPos + offsets[i];
                if (!IsTillable(p)) continue;

                PlotData plot = GetPlot(p);
                if (plot == null) continue;
                if (plot.status != TileStatus.Tilled && plot.status != TileStatus.Watered) continue;
                if (plot.plantedCrop != null) continue;

                var seedSlot = inventory.FindSeedSlot();
                if (seedSlot == null) break;
                CropData crop = seedSlot.item.cropToPlant;
                if (crop == null) continue;

                if (_cropManager.TryPlantCrop(p, crop))
                {
                    inventory.RemoveItem(seedSlot.item, 1);
                    PBV.Systems.QuestManager.Instance?.RecordPlant();
                    planted++;
                }
            }

            if (planted > 1)
                PBV.UI.HUD.Instance?.ShowNotice($"种子包 Lv{tm?.CurrentLevel ?? 1}：一次播种 {planted} 格");
            return planted > 0;
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
                case "Seed": // 统一走背包播种（含种子包多格效果），避免两套逻辑不一致
                    TryPlantFromInventory(gridPos);
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
