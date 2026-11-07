using UnityEngine;

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

        // If it's a seed, reference the crop data
        // public CropData cropToPlant; 
    }
}
