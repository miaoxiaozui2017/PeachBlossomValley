using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PBV.Inventory
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;

        public InventorySlot()
        {
            item = null;
            count = 0;
        }

        public void Clear()
        {
            item = null;
            count = 0;
        }
    }

    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int _capacity = 24;
        
        // Runtime Data
        private InventorySlot[] _slots;
        
        // Events
        public UnityAction OnInventoryChanged; // UI Update

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots = new InventorySlot[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                _slots[i] = new InventorySlot();
            }
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            // 1. Check existing stackable slots
            for (int i = 0; i < _capacity; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item)
                {
                    if (_slots[i].count + amount <= item.maxStack)
                    {
                        _slots[i].count += amount;
                        OnInventoryChanged?.Invoke();
                        Debug.Log($"Added {amount} x {item.itemName} to existing stack.");
                        return true;
                    }
                    else
                    {
                        // Fill this stack and continue with remaining
                        int spaceLeft = item.maxStack - _slots[i].count;
                        _slots[i].count = item.maxStack;
                        amount -= spaceLeft;
                    }
                }
            }

            // 2. Check empty slots
            if (amount > 0)
            {
                for (int i = 0; i < _capacity; i++)
                {
                    if (_slots[i].IsEmpty)
                    {
                        _slots[i].item = item;
                        _slots[i].count = amount; // Assuming amount < maxStack for simplicity now
                        OnInventoryChanged?.Invoke();
                        Debug.Log($"Added {amount} x {item.itemName} to new slot.");
                        return true;
                    }
                }
            }
            
            Debug.LogWarning("Inventory Full!");
            return false;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _capacity) return null;
            return _slots[index];
        }
        
        // For Debug
        public void AddTestItem(ItemData item)
        {
            AddItem(item, 1);
        }
    }
}
