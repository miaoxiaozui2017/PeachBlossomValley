using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 储物箱系统：房屋扩建解锁的独立仓库，容量由 BuildSystem.HouseLevel 决定。
    /// 0 级无储物箱；1 级 12 格；2 级 24 格。存档支持。
    /// 区别于背包：背包随身携带，储物箱放在家里。
    /// </summary>
    public class StorageSystem : MonoBehaviour
    {
        public static StorageSystem Instance { get; private set; }

        private InventorySlot[] _slots = new InventorySlot[0];
        private int _capacity = 0;

        public int Capacity => _capacity;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // 等 BuildSystem 就绪后应用容量
            if (BuildSystem.Instance != null)
                ApplyCapacity(BuildSystem.Instance.StorageCapacity);
        }

        /// <summary>按容量重建槽位（容量变化或读档时调用）</summary>
        public void ApplyCapacity(int newCapacity)
        {
            newCapacity = Mathf.Max(0, newCapacity);
            if (newCapacity == _capacity) return;

            var newSlots = new InventorySlot[newCapacity];
            for (int i = 0; i < newSlots.Length; i++)
            {
                newSlots[i] = (i < _slots.Length && _slots[i] != null) ? _slots[i] : new InventorySlot();
            }
            _slots = newSlots;
            _capacity = newCapacity;
            Debug.Log($"[Storage] 储物箱容量 {_capacity} 格");
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _capacity) return null;
            return _slots[index];
        }

        /// <summary>存入物品到储物箱，成功返回 true（容量不足/已满失败）</summary>
        public bool Store(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0 || _capacity <= 0) return false;

            // 合并到已有同类槽
            for (int i = 0; i < _capacity; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item && _slots[i].count < item.maxStack)
                {
                    int space = item.maxStack - _slots[i].count;
                    int take = Mathf.Min(space, amount);
                    _slots[i].count += take;
                    amount -= take;
                    if (amount <= 0) return true;
                }
            }
            // 放入空槽
            for (int i = 0; i < _capacity && amount > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    _slots[i].item = item;
                    _slots[i].count = amount;
                    amount = 0;
                    return true;
                }
            }
            return amount <= 0;
        }

        /// <summary>从储物箱取出物品</summary>
        public bool Retrieve(ItemData item, int amount = 1)
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
            return remaining <= 0;
        }

        public int CountItem(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _capacity; i++)
                if (!_slots[i].IsEmpty && _slots[i].item == item) total += _slots[i].count;
            return total;
        }

        // 存档
        public void Export(System.Action<int, string, int> writeSlot)
        {
            for (int i = 0; i < _capacity; i++)
                if (!_slots[i].IsEmpty)
                    writeSlot(i, _slots[i].item.id, _slots[i].count);
        }

        public void SetSlot(int index, ItemData item, int count)
        {
            if (index < 0 || index >= _capacity) return;
            _slots[index].item = item;
            _slots[index].count = Mathf.Max(0, count);
        }

        public void ClearAll()
        {
            for (int i = 0; i < _capacity; i++) _slots[i].Clear();
        }
    }
}
