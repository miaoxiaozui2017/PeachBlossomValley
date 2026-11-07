using UnityEngine;
using PBV.Farming;

namespace PBV.Inventory
{
    public enum ItemType
    {
        Resource,
        Tool,
        Consumable,
        Seed
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "PeachBlossom/Inventory/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        public ItemType type;
        public int maxStack = 99;
        public int sellPrice = 0;

        [Header("Seed Settings")]
        [Tooltip("仅当 type == Seed 时有效：播种后生成的作物")]
        public CropData cropToPlant;
    }
}
