using UnityEngine;
using UnityEngine.Tilemaps;
using PBV.Systems;
using PBV.Inventory;

namespace PBV.Farming
{
    /// <summary>
    /// 作物生长与管理逻辑
    /// </summary>
    public class CropManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FarmGrid _farmGrid;
        [SerializeField] private Tilemap _cropTilemap; // 用于显示作物的Tilemap (或者SpriteRenderer)

        private void Start()
        {
            if (_farmGrid == null) _farmGrid = GetComponent<FarmGrid>();
            
            // Subscribe to TimeManager
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += OnNewDay;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= OnNewDay;
            }
        }

        /// <summary>
        /// 每日生长逻辑 (由 TimeManager 事件触发)
        /// </summary>
        public void OnNewDay()
        {
            if (_farmGrid == null) return;

            var plots = _farmGrid.GetAllPlots();
            foreach (var plot in plots.Values)
            {
                if (plot.plantedCrop != null)
                {
                    // 只有浇过水才生长
                    if (plot.isWateredToday)
                    {
                        plot.growthDays++;
                        UpdateCropVisual(plot);
                    }
                }
            }
            
            // 通知 FarmGrid 重置浇水状态
            _farmGrid.ResetWaterState();
        }

        /// <summary>
        /// 尝试种植作物
        /// </summary>
        public bool TryPlantCrop(Vector3Int pos, CropData crop)
        {
            if (!_farmGrid.GetAllPlots().TryGetValue(pos, out PlotData plot)) return false;

            // 必须是已耕地且没有作物
            if (plot.status != TileStatus.Tilled && plot.status != TileStatus.Watered) return false;
            if (plot.plantedCrop != null) return false;

            plot.plantedCrop = crop;
            plot.growthDays = 0;
            
            UpdateCropVisual(plot);
            Debug.Log($"Planted {crop.cropName} at {pos}");
            return true;
        }

        /// <summary>
        /// 尝试收获作物
        /// </summary>
        public bool TryHarvestCrop(Vector3Int pos)
        {
            if (!_farmGrid.GetAllPlots().TryGetValue(pos, out PlotData plot)) return false;
            if (plot.plantedCrop == null) return false;

            // 检查是否成熟
            if (plot.growthDays >= plot.plantedCrop.daysToMature)
            {
                Harvest(plot);
                return true;
            }

            return false;
        }

        private void Harvest(PlotData plot)
        {
            if (plot.plantedCrop.harvestItem != null)
            {
                int yield = Random.Range(plot.plantedCrop.minYield, plot.plantedCrop.maxYield + 1);
                
                if (InventorySystem.Instance != null)
                {
                    bool added = InventorySystem.Instance.AddItem(plot.plantedCrop.harvestItem, yield);
                    if (!added)
                    {
                        Debug.LogWarning("Inventory Full! Cannot harvest.");
                        return; // Backpack full, cancel harvest
                    }
                }
                
                Debug.Log($"Harvested {plot.plantedCrop.cropName} x {yield}!");
            }

            // 清除作物数据
            plot.plantedCrop = null;
            plot.growthDays = 0;
            
            // 清除视觉表现
            if (_cropTilemap != null)
            {
                _cropTilemap.SetTile(plot.position, null);
            }
            
            // 土地状态保持 (或者变回 Tilled/Watered)
        }

        private void UpdateCropVisual(PlotData plot)
        {
            if (_cropTilemap == null || plot.plantedCrop == null) return;

            // 计算生长阶段
            // 假设 daysToMature = 4, sprites.Length = 4
            // 0 days -> stage 0
            // 4 days -> stage 3 (mature)
            
            int stageCount = plot.plantedCrop.growthSprites.Length;
            if (stageCount == 0) return;

            float growthPercentage = (float)plot.growthDays / plot.plantedCrop.daysToMature;
            int currentStage = Mathf.FloorToInt(growthPercentage * (stageCount - 1));
            
            // 确保成熟时显示最后一张
            if (plot.growthDays >= plot.plantedCrop.daysToMature)
            {
                currentStage = stageCount - 1;
            }

            Sprite sprite = plot.plantedCrop.GetSpriteForStage(currentStage);
            
            // 创建 Tile (由于 Tilemap 需要 TileBase，这里动态创建一个简单的 Tile)
            // 注意：实际项目中通常会缓存这些 Tile 或者使用自定义 Tile
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            
            _cropTilemap.SetTile(plot.position, tile);
        }
    }
}
