using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 储物箱放置处理器：
    /// - 背包选中「储物箱」物品 + 左键 → 进入放置模式
    /// - 放置模式下左键点击地图 → 在该世界坐标生成箱子（扣 1 个储物箱物品）
    /// - 读档后重建所有已放置箱子的场景物体
    /// 挂在全局自动创建的 GameObject 上（由 UIEventSystemBootstrap 补建）。
    /// </summary>
    public class StoragePlacement : MonoBehaviour
    {
        public static StoragePlacement Instance { get; private set; }

        private Camera _cam;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (StorageSystem.Instance == null) return;
            if (!StorageSystem.Instance.IsPlacing) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            // Esc 取消放置
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StorageSystem.Instance.CancelPlacement();
                PBV.UI.HUD.Instance?.ShowNotice("已取消放置");
                return;
            }

            // 左键确认放置（忽略进入放置模式那一帧的残留点击）
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (StorageSystem.Instance.ShouldIgnoreCurrentFrameClick()) return;
                var world = MouseToWorld(mouse);
                ConfirmPlace(world);
            }
        }

        private Vector2 MouseToWorld(UnityEngine.InputSystem.Mouse mouse)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return Vector2.zero;
            return _cam.ScreenToWorldPoint(mouse.position.ReadValue());
        }

        private void ConfirmPlace(Vector2 worldPos)
        {
            // 从背包扣 1 个储物箱物品
            var inv = InventorySystem.Instance;
            var chestItem = FindChestItem();
            if (inv == null || chestItem == null)
            {
                StorageSystem.Instance.CancelPlacement();
                PBV.UI.HUD.Instance?.ShowNotice("背包里没有储物箱了");
                return;
            }
            if (!inv.RemoveItem(chestItem, 1))
            {
                StorageSystem.Instance.CancelPlacement();
                PBV.UI.HUD.Instance?.ShowNotice("放置失败：背包里没有储物箱");
                return;
            }

            // 生成箱子
            var chest = StorageSystem.Instance.PlaceChest(worldPos);
            SpawnChestObject(chest);
            PBV.UI.HUD.Instance?.ShowNotice($"已放置「{chest.label}」于 ({Mathf.RoundToInt(worldPos.x)}, {Mathf.RoundToInt(worldPos.y)})");
        }

        /// <summary>为某个箱子生成场景物体（放置 / 读档重建共用）</summary>
        public GameObject SpawnChestObject(StorageChest chest)
        {
            var go = new GameObject($"StorageChest_{chest.uid}");
            go.transform.position = new Vector3(chest.x, chest.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeChestSprite();
            sr.sortingOrder = 8;
            int ci = Mathf.Clamp(chest.colorIndex, 0, StorageSystem.ChestColors.Length - 1);
            sr.color = StorageSystem.ChestColors[ci];

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 1.0f);
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;

            var station = go.AddComponent<StorageChestStation>();
            station.Init(chest.uid);

            // 头顶名牌（用标签名）
            EnsureLabel(go, chest.label);

            return go;
        }

        /// <summary>重建所有箱子场景物体（读档后调用），幂等</summary>
        public void RebuildAll()
        {
            if (StorageSystem.Instance == null) return;
            foreach (var chest in StorageSystem.Instance.Chests)
            {
                string goName = $"StorageChest_{chest.uid}";
                if (GameObject.Find(goName) != null) continue;
                SpawnChestObject(chest);
            }
        }

        private void EnsureLabel(GameObject go, string text)
        {
            if (go.transform.Find("ChestLabel") != null) return;
            var tagGo = new GameObject("ChestLabel");
            tagGo.transform.SetParent(go.transform, false);
            tagGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);

            var canvas = tagGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 40f);
            rect.localScale = new Vector3(0.012f, 0.012f, 0.012f);

            var t = tagGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.93f, 0.68f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;
        }

        private static Sprite MakeChestSprite()
        {
            // 简单储物箱贴图：棕色箱体 + 深色盖/锁
            var tex = new Texture2D(16, 16);
            var body = new Color(0.55f, 0.40f, 0.24f);
            var dark = new Color(0.40f, 0.28f, 0.16f);
            var metal = new Color(0.85f, 0.80f, 0.40f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    var c = body;
                    if (y >= 12) c = dark;              // 盖
                    if (y >= 11 && y <= 12) c = dark;   // 盖沿
                    if (x == 8 && y >= 5 && y <= 8) c = metal; // 锁扣
                    if (x == 0 || x == 15 || y == 0 || y == 15) c = dark; // 边框
                    tex.SetPixel(x, y, c);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = "StorageChestSprite";
            return sprite;
        }

        private static ItemData FindChestItem()
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == StorageSystem.ChestItemId) return item;
            return null;
        }

        /// <summary>判断物品是否是储物箱（供 FarmGrid 输入分派调用）</summary>
        public static bool IsChestItem(ItemData item)
        {
            return item != null && item.id == StorageSystem.ChestItemId;
        }
    }
}
