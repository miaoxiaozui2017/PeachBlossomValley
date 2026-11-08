using UnityEngine;
using PBV.Inventory;

namespace PBV.Farming
{
    /// <summary>
    /// 作物数据定义
    /// 包含生长周期、不同阶段的Sprite等
    /// </summary>
    [CreateAssetMenu(fileName = "New Crop", menuName = "谷中仙/Farming/CropData")]
    public class CropData : ScriptableObject
    {
        [Header("Basic Info")]
        public string cropName;
        [TextArea] public string description;
        
        [Header("Growth Settings")]
        [Tooltip("Total days to reach maturity")]
        public int daysToMature;
        
        [Tooltip("Sprites for each growth stage. 0 is seed/sprout, last is mature.")]
        public Sprite[] growthSprites;
        
        [Header("Harvest")]
        public ItemData harvestItem;
        public int minYield = 1;
        public int maxYield = 3;

        [Header("Season")]
        [Tooltip("可种植的季节（留空=全年可种）")]
        public PBV.Systems.Season[] seasons;
        
        public Sprite GetSpriteForStage(int stage)
        {
            if (growthSprites == null || growthSprites.Length == 0) return null;
            return growthSprites[Mathf.Clamp(stage, 0, growthSprites.Length - 1)];
        }
    }
}
