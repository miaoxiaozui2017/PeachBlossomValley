using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PBV.Systems;

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

    /// <summary>
    /// 背包系统：**桃花谷 / 现代各自独立一套背包**。
    /// 切世界时只切换「当前活动槽位数组」，对外 API 不变（所以农事/工坊/商店等几十处调用点无需改动）。
    /// 跨境唯一通道是穿梭贸易 3 槽（TradeCarrySystem），普通背包物品不会跟着穿越。
    /// 扩容对两套背包同时生效（容量是账号级进度，属共用层）。
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int _capacity = 24;

        private InventorySlot[] _valleySlots;   // 桃花谷背包
        private InventorySlot[] _modernSlots;   // 现代背包
        private InventorySlot[] _slots;         // 当前活动引用

        public UnityAction OnInventoryChanged;

        /// <summary>当前所处世界（供 UI 标注）</summary>
        public bool IsModern => WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            _valleySlots = MakeSlots(_capacity);
            _modernSlots = MakeSlots(_capacity);
            _slots = _valleySlots;
        }

        private void Start()
        {
            if (WorldState.Instance != null)
            {
                WorldState.Instance.OnWorldChanged += OnWorldChanged;
                ApplyWorld(WorldState.Instance.Current, false);
            }
        }

        private void OnDestroy()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.OnWorldChanged -= OnWorldChanged;
        }

        private void OnWorldChanged(WorldType world) => ApplyWorld(world, true);

        private void ApplyWorld(WorldType world, bool notify)
        {
            _slots = world == WorldType.Modern ? _modernSlots : _valleySlots;
            // 选中槽位可能指向另一套背包里不存在的格子，越界保护由 GetSlot 处理
            if (notify)
            {
                OnInventoryChanged?.Invoke();
                Debug.Log($"[Inventory] 切换到{(world == WorldType.Modern ? "现代" : "桃花谷")}背包");
            }
        }

        private static InventorySlot[] MakeSlots(int capacity)
        {
            var arr = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++) arr[i] = new InventorySlot();
            return arr;
        }

        public bool AddItem(ItemData item, int amount = 1) => AddItemCore(_slots, item, amount);

        /// <summary>明确发到桃花谷背包（StarterKit 用，避免读档在现代时把开局物资发错地方）</summary>
        public bool AddItemToValley(ItemData item, int amount = 1) => AddItemCore(_valleySlots, item, amount, IsModern);

        private bool AddItemCore(InventorySlot[] slots, ItemData item, int amount, bool suppressNotify = false)
        {
            if (item == null || amount <= 0) return false;

            for (int i = 0; i < _capacity; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    if (slots[i].count + amount <= item.maxStack)
                    {
                        slots[i].count += amount;
                        if (!suppressNotify) OnInventoryChanged?.Invoke();
                        return true;
                    }
                    else
                    {
                        int spaceLeft = item.maxStack - slots[i].count;
                        slots[i].count = item.maxStack;
                        amount -= spaceLeft;
                    }
                }
            }

            if (amount > 0)
            {
                for (int i = 0; i < _capacity; i++)
                {
                    if (slots[i].IsEmpty)
                    {
                        slots[i].item = item;
                        slots[i].count = amount;
                        if (!suppressNotify) OnInventoryChanged?.Invoke();
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

        public int CountItem(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _capacity; i++)
                if (!_slots[i].IsEmpty && _slots[i].item == item) total += _slots[i].count;
            return total;
        }

        public InventorySlot FindSeedSlot()
        {
            for (int i = 0; i < _capacity; i++)
                if (!_slots[i].IsEmpty && _slots[i].item.type == ItemType.Seed && _slots[i].item.cropToPlant != null)
                    return _slots[i];
            return null;
        }

        public int Capacity => _capacity;

        /// <summary>扩容：两套背包同步扩，容量属共用层</summary>
        public void ExpandCapacity(int newCapacity)
        {
            if (newCapacity <= _capacity) return;
            _valleySlots = Expand(_valleySlots, newCapacity);
            _modernSlots = Expand(_modernSlots, newCapacity);
            _slots = IsModern ? _modernSlots : _valleySlots;
            _capacity = newCapacity;
            OnInventoryChanged?.Invoke();
        }

        private static InventorySlot[] Expand(InventorySlot[] old, int newCapacity)
        {
            var arr = new InventorySlot[newCapacity];
            for (int i = 0; i < old.Length; i++) arr[i] = old[i];
            for (int i = old.Length; i < newCapacity; i++) arr[i] = new InventorySlot();
            return arr;
        }

        public void SetSlot(int index, ItemData item, int count)
        {
            if (index < 0 || index >= _capacity) return;
            _slots[index].item = item;
            _slots[index].count = Mathf.Max(0, count);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>清空当前世界背包</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _capacity; i++) _slots[i].Clear();
            OnInventoryChanged?.Invoke();
        }

        // ===== 存档：两套背包分别导出/导入 =====

        /// <summary>导出指定世界的背包（只导出非空槽位）</summary>
        public List<(int index, ItemData item, int count)> ExportWorld(WorldType world)
        {
            var list = new List<(int, ItemData, int)>();
            var src = world == WorldType.Modern ? _modernSlots : _valleySlots;
            for (int i = 0; i < _capacity; i++)
                if (!src[i].IsEmpty) list.Add((i, src[i].item, src[i].count));
            return list;
        }

        /// <summary>导入指定世界的背包（先清空该套）</summary>
        public void ImportWorld(WorldType world, IEnumerable<(int index, ItemData item, int count)> entries)
        {
            var dst = world == WorldType.Modern ? _modernSlots : _valleySlots;
            for (int i = 0; i < _capacity; i++) dst[i].Clear();
            if (entries == null) return;
            foreach (var e in entries)
                if (e.index >= 0 && e.index < _capacity && e.item != null)
                { dst[e.index].item = e.item; dst[e.index].count = e.count; }
            if ((world == WorldType.Modern) == IsModern) OnInventoryChanged?.Invoke();
        }

        public void AddTestItem(ItemData item) => AddItem(item, 1);
    }
}
