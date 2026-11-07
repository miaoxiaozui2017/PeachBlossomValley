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

        /// <summary>
        /// 从背包移除指定物品（消耗种子/收获使用）
        /// </summary>
        public bool RemoveItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int remaining = amount;
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item)
                {
                    int take = Mathf.Min(_slots[i].count, remaining);
                    _slots[i].count -= take;
                    remaining -= take;

                    if (_slots[i].count <= 0) _slots[i].Clear();
                }
            }

            if (remaining > 0)
            {
                Debug.LogWarning($"[Inventory] 物品不足：需要 {amount} x {item.itemName}，缺少 {remaining}");
                return false;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 统计背包中指定物品数量
        /// </summary>
        public int CountItem(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item)
                {
                    total += _slots[i].count;
                }
            }
            return total;
        }

        /// <summary>
        /// 查找背包中第一个种子类物品的槽位（用于播种）
        /// </summary>
        public InventorySlot FindSeedSlot()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item.type == ItemType.Seed && _slots[i].item.cropToPlant != null)
                {
                    return _slots[i];
                }
            }
            return null;
        }

        public int Capacity => _capacity;

        /// <summary>扩容背包（建造系统用）</summary>
        public void ExpandCapacity(int newCapacity)
        {
            if (newCapacity <= _capacity) return;
            var newSlots = new InventorySlot[newCapacity];
            for (int i = 0; i < _slots.Length; i++) newSlots[i] = _slots[i];
            for (int i = _slots.Length; i < newCapacity; i++) newSlots[i] = new InventorySlot();
            _slots = newSlots;
            _capacity = newCapacity;
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 存档恢复专用：按索引写入槽位（不触发堆叠合并）
        /// </summary>
        public void SetSlot(int index, ItemData item, int count)
        {
            if (index < 0 || index >= _capacity) return;
            _slots[index].item = item;
            _slots[index].count = Mathf.Max(0, count);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 清空背包（读档前调用）
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _capacity; i++)
            {
                _slots[i].Clear();
            }
            OnInventoryChanged?.Invoke();
        }

        // For Debug
        public void AddTestItem(ItemData item)
        {
            AddItem(item, 1);
        }
    }
}
