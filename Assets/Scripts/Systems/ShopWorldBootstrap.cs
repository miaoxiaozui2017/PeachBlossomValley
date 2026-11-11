using UnityEngine;
using PBV.Economy;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 古今商店隔离（运行时补建，无需重建场景）：
    /// - 桃花谷：保留现有 3 家（杂货铺 / 酒楼 / 老农小摊），暖色美术、宋币货架。
    /// - 现代：只有 2 家（现代杂货铺 / 古玩店），冷色与金铜色美术、人民币货架。
    /// - 切换世界时整店隐藏，两边的店与货架互不通用。
    /// </summary>
    public class ShopWorldBootstrap : MonoBehaviour
    {
        private WorldType _lastWorld;
        private float _recheckTimer;

        private void Start()
        {
            CreateModernShops();
            if (WorldState.Instance != null)
                WorldState.Instance.OnWorldChanged += OnWorldChanged;
            _lastWorld = WorldState.Instance != null ? WorldState.Instance.Current : WorldType.Ancient;
            ApplyVisibility(_lastWorld);
        }

        private void OnDestroy()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.OnWorldChanged -= OnWorldChanged;
        }

        private void Update()
        {
            // 兜底：读档/事件顺序导致漏刷新时自动对齐；低频轮询覆盖「补建晚于事件」的情况
            var w = WorldState.Instance != null ? WorldState.Instance.Current : WorldType.Ancient;
            if (w != _lastWorld)
            {
                _lastWorld = w;
                ApplyVisibility(w);
                return;
            }
            _recheckTimer += Time.unscaledDeltaTime;
            if (_recheckTimer >= 0.5f)
            {
                _recheckTimer = 0f;
                ApplyVisibility(w);
            }
        }

        private void OnWorldChanged(WorldType world)
        {
            _lastWorld = world;
            ApplyVisibility(world);
        }

        private static void ApplyVisibility(WorldType world)
        {
            // FindObjectsOfTypeAll 含未激活对象，隐藏的现代店也能被重新显示
            foreach (var shop in Resources.FindObjectsOfTypeAll<ShopSystem>())
            {
                if (shop == null) continue;
                bool show = (world == WorldType.Modern) == (shop.World == ShopWorld.Modern);
                if (shop.gameObject.activeSelf != show) shop.gameObject.SetActive(show);
            }
            // 若某世界一家店都没有，给出诊断日志（避免静默「一家店都不见」）
            Debug.Log($"[ShopWorld] 世界={(world == WorldType.Modern ? "现代" : "桃花谷")}，已按归属显隐商店");
        }

        /// <summary>补建现代两家店；幂等，场景里已存在则只补货架</summary>
        private static void CreateModernShops()
        {
            CreateModernShop("ModernShop", "现代杂货铺 ¥", new Color(0.32f, 0.48f, 0.68f),
                new Color(0.72f, 0.88f, 1f), false);
            CreateModernShop("AntiqueShop", "古玩店 ¥", new Color(0.62f, 0.45f, 0.22f),
                new Color(1f, 0.84f, 0.46f), true);
        }

        private static void CreateModernShop(string goName, string label, Color blockColor, Color tagColor, bool antique)
        {
            var go = GameObject.Find(goName);
            bool created = false;
            if (go == null)
            {
                go = new GameObject(goName);
                StationLayoutData.Place(go);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = MakeSolidSprite(blockColor);
                sr.sortingOrder = 9;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.3f, 1.3f);
                col.offset = new Vector2(0f, 0.4f);
                int layer = LayerMask.NameToLayer("Interactable");
                if (layer >= 0) go.layer = layer;
                created = true;
            }

            var shop = go.GetComponent<ShopSystem>();
            if (shop == null) shop = go.AddComponent<ShopSystem>();
            SetPrivate(shop, "_shopName", antique ? "古玩店" : "现代杂货铺");
            SetPrivate(shop, "_world", ShopWorld.Modern);

            StationLayoutData.EnsureLabel(go, label, tagColor);

            // 本店可买的东西（人民币货架）
            if (antique) FillAntiqueShelf(shop);
            else FillModernShelf(shop);

            if (created) Debug.Log($"[ShopWorld] 补建 {goName}（{label}）");
        }

        /// <summary>现代杂货铺：现代资源 / 肥料 / 黑科技图纸 / 四件工具</summary>
        private static void FillModernShelf(ShopSystem shop)
        {
            Add(shop, "item_mod_drink", 5);
            Add(shop, "item_mod_gel", 12);
            Add(shop, "item_mod_boost", 8);
            Add(shop, "item_fertilizer_modern", 15);
            Add(shop, "item_blueprint_farm", 30);
            Add(shop, "item_blueprint_mine", 35);
            Add(shop, "item_blueprint_trade", 40);
            // 工具现在也能卖，必须能买回来，否则玩家卖光工具会卡死
            AddTools(shop, Currency.RMB);
        }

        /// <summary>四件工具上架（卖价 60，买价 90，不能低买高卖套利）</summary>
        private static void AddTools(ShopSystem shop, Currency currency)
        {
            Add(shop, "item_tool_hoe", 90, currency);
            Add(shop, "item_tool_wateringcan", 90, currency);
            Add(shop, "item_tool_sickle", 90, currency);
            Add(shop, "item_tool_axe", 90, currency);
        }

        /// <summary>古玩店：文物收藏品（原价高，可买来送礼/收藏，也能再卖回）</summary>
        private static void FillAntiqueShelf(ShopSystem shop)
        {
            Add(shop, "item_antique_pottery", 300);
            Add(shop, "item_antique_bronze", 500);
            Add(shop, "item_antique_jade", 800);
            Add(shop, "item_art_scroll", 600);
            Add(shop, "item_art_curio", 900);
            Add(shop, "item_jewelry", 800);
            Add(shop, "item_pottery_bowl", 220);
            Add(shop, "item_pottery_vase", 320);
            Add(shop, "item_xuanpaper", 100);
            Add(shop, "item_ink_medicine", 120);
        }

        private static void Add(ShopSystem shop, string id, int price, Currency currency = Currency.RMB)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
            {
                if (item == null || item.id != id) continue;
                shop.AddShopEntry(item, price, currency);
                return;
            }
        }

        private static void SetPrivate(Object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null) f.SetValue(target, value);
        }

        private static Sprite MakeSolidSprite(Color c)
        {
            var tex = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++) tex.SetPixel(x, y, c);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            sprite.name = "ShopSprite";
            return sprite;
        }
    }
}
