using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 单个储物箱数据：36 格、可命名、可改色、固定世界坐标。
    /// </summary>
    [System.Serializable]
    public class StorageChest
    {
        public string uid;          // 唯一 id（存档/恢复用）
        public float x, y;          // 世界坐标（放置位置）
        public string label;        // 标签名称（默认「储物箱」）
        public int colorIndex;      // 颜色索引（0..N，改色用）
        public InventorySlot[] slots; // 36 格

        public const int Capacity = 36;

        public StorageChest(string uid, float x, float y)
        {
            this.uid = uid;
            this.x = x;
            this.y = y;
            this.label = "储物箱";
            this.colorIndex = 0;
            slots = new InventorySlot[Capacity];
            for (int i = 0; i < Capacity; i++) slots[i] = new InventorySlot();
        }

        public bool Store(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            for (int i = 0; i < Capacity; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item && slots[i].count < item.maxStack)
                {
                    int space = item.maxStack - slots[i].count;
                    int take = Mathf.Min(space, amount);
                    slots[i].count += take;
                    amount -= take;
                    if (amount <= 0) return true;
                }
            }
            for (int i = 0; i < Capacity && amount > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].item = item;
                    slots[i].count = amount;
                    return true;
                }
            }
            return amount <= 0;
        }

        public bool Retrieve(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return false;
            var s = slots[slotIndex];
            if (s.IsEmpty) return false;
            s.count -= amount;
            if (s.count <= 0) s.Clear();
            return true;
        }
    }

    /// <summary>
    /// 储物箱系统（多实例）：管理房屋内外可放置的多个储物箱。
    /// 与房屋扩建解耦——储物箱通过木工坊制作「储物箱」物品后手动放置。
    /// 放置模式：背包选中储物箱物品后左键进入放置，再左键点击地图放置。
    /// </summary>
    public class StorageSystem : MonoBehaviour
    {
        public static StorageSystem Instance { get; private set; }

        private readonly List<StorageChest> _chests = new List<StorageChest>();

        // 放置模式状态
        private bool _placementMode;
        private System.Action<Vector2> _placementCallback;
        private int _placementStartFrame = -1; // 进入放置的那一帧，用于忽略同帧点击

        public IReadOnlyList<StorageChest> Chests => _chests;
        public bool IsPlacing => _placementMode;
        public const string ChestItemId = "item_storage_chest";

        // 储物箱可选颜色（改色用）
        public static readonly Color[] ChestColors =
        {
            new Color(0.62f, 0.45f, 0.28f), // 原木棕
            new Color(0.55f, 0.60f, 0.65f), // 灰
            new Color(0.75f, 0.35f, 0.30f), // 朱红
            new Color(0.35f, 0.55f, 0.75f), // 青蓝
            new Color(0.45f, 0.60f, 0.40f), // 草绿
            new Color(0.80f, 0.70f, 0.35f), // 明黄
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>进入放置模式：点击地图选定位置后回调 worldPos 并放箱</summary>
        public void BeginPlacement(System.Action<Vector2> onPlace)
        {
            _placementMode = true;
            _placementCallback = onPlace;
            _placementStartFrame = Time.frameCount;
            PBV.UI.HUD.Instance?.ShowNotice("放置模式：左键点击地图选位置放箱（Esc 取消）");
        }

        /// <summary>是否应忽略本次点击（进入放置模式的那一帧的残留点击）</summary>
        public bool ShouldIgnoreCurrentFrameClick() => _placementStartFrame == Time.frameCount;

        public void CancelPlacement()
        {
            _placementMode = false;
            _placementCallback = null;
        }

        /// <summary>放置模式收到左键点击（世界坐标）</summary>
        public bool TryConfirmPlacement(Vector2 worldPos)
        {
            if (!_placementMode) return false;
            _placementMode = false;
            var cb = _placementCallback;
            _placementCallback = null;
            cb?.Invoke(worldPos);
            return true;
        }

        /// <summary>放置一个新的储物箱到指定位置，返回新建箱子（供生成场景物体）</summary>
        public StorageChest PlaceChest(Vector2 worldPos, string uid = null)
        {
            if (uid == null) uid = System.Guid.NewGuid().ToString("N");
            var chest = new StorageChest(uid, worldPos.x, worldPos.y);
            _chests.Add(chest);
            return chest;
        }

        public StorageChest GetChest(string uid)
        {
            foreach (var c in _chests) if (c.uid == uid) return c;
            return null;
        }

        public void RemoveChest(string uid)
        {
            for (int i = _chests.Count - 1; i >= 0; i--)
                if (_chests[i].uid == uid) _chests.RemoveAt(i);
        }

        // ---------------- 存档 ----------------
        public List<PBV.Save.StorageChestDTO> Export()
        {
            var list = new List<PBV.Save.StorageChestDTO>();
            foreach (var c in _chests)
            {
                var dto = new PBV.Save.StorageChestDTO
                {
                    uid = c.uid,
                    x = c.x,
                    y = c.y,
                    label = c.label,
                    colorIndex = c.colorIndex,
                    items = new List<PBV.Save.SlotDTO>()
                };
                for (int i = 0; i < StorageChest.Capacity; i++)
                {
                    var s = c.slots[i];
                    if (s.IsEmpty) continue;
                    string itemId = string.IsNullOrEmpty(s.item.id) ? s.item.itemName : s.item.id;
                    dto.items.Add(new PBV.Save.SlotDTO { index = i, itemId = itemId, count = s.count });
                }
                list.Add(dto);
            }
            return list;
        }

        public void Import(List<PBV.Save.StorageChestDTO> data, System.Func<string, ItemData> itemResolver)
        {
            _chests.Clear();
            if (data == null) return;
            foreach (var dto in data)
            {
                var chest = new StorageChest(dto.uid, dto.x, dto.y)
                {
                    label = string.IsNullOrEmpty(dto.label) ? "储物箱" : dto.label,
                    colorIndex = dto.colorIndex
                };
                if (dto.items != null)
                {
                    foreach (var s in dto.items)
                    {
                        var item = itemResolver(s.itemId);
                        if (item == null || s.index < 0 || s.index >= StorageChest.Capacity) continue;
                        chest.slots[s.index].item = item;
                        chest.slots[s.index].count = s.count;
                    }
                }
                _chests.Add(chest);
            }
        }
    }
}
