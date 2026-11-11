using UnityEngine;
using PBV.Inventory;
using PBV.Economy;
using PBV.NPC;

namespace PBV.Systems
{
    /// <summary>
    /// v0.3.0 内容扩展数据：4 名新 NPC、2 个钓点、1 座染坊。
    /// 全部由 ContentExpansionBootstrap 在运行时补建，不要求重建场景。
    /// </summary>
    public static class ContentExpansionData
    {
        // (物体名, 类型, NPC 显示名, 对话, 色块色/名牌色, 钓点编号)
        public static readonly (string goName, string kind, string displayName, string[] lines, Color color, int arg)[] Extra =
        {
            ("DyeingStation", "dye", "", null, new Color(0.35f, 0.45f, 0.75f), 0),
            ("FishingSpot_River", "fish", "", null, new Color(0.30f, 0.60f, 0.80f), 1),
            ("FishingSpot_Lake", "fish", "", null, new Color(0.20f, 0.40f, 0.75f), 2),

            ("NPC_RanFang", "npc", "阿蓝（染坊女主人）", new[]
            {
                "蓝草要趁晨露未干时割，染出的青才正。",
                "素布染两回，纹样才不褪。",
                "你若得了成衣，记得给书斋先生看看。"
            }, new Color(0.55f, 0.75f, 0.95f), 0),

            ("NPC_TieJiangNiang", "npc", "柳娘（铁匠娘子）", new[]
            {
                "别看我成天抡锤子，剑法我可没落下。",
                "矿石带够了？我给你把工具再淬一遍。",
                "若能寻到剑谱……算了，不提也罢。"
            }, new Color(0.95f, 0.60f, 0.55f), 0),

            ("NPC_DaoShi", "npc", "守卷道人", new[]
            {
                "这卷古画，你已修复了几分？",
                "节气交替之时，两界的门最薄。",
                "稀有的种子，我这里倒是有一些。"
            }, new Color(0.80f, 0.72f, 0.95f), 0),

            ("NPC_TaoGong", "npc", "老窑（陶工）", new[]
            {
                "龙窑塌了三年，高岭土倒还堆着。",
                "青瓷茶具能存食物不坏，可惜手艺断了。",
                "帮我重建窑，我教你烧瓷。"
            }, new Color(0.80f, 0.68f, 0.50f), 0),

            ("NPC_BaoBao", "npc", "饱饱（酒楼大厨）", new[]
            {
                "民以食为天，我这酒楼啊，就缺个会种菜的主儿。",
                "新鲜鸡蛋、好肉，到我这儿能卖个好价钱。",
                "想学手艺？先把地里那几垄菜伺候明白喽。"
            }, new Color(0.95f, 0.85f, 0.45f), 0),

            ("TavernStation", "tavern", "", null, new Color(0.85f, 0.40f, 0.25f), 0),

            ("JewelryStation", "jewel", "", null, new Color(0.85f, 0.7f, 0.2f), 0),
            ("PotteryStation", "craft", "", null, new Color(0.72f, 0.55f, 0.38f), 0),
            ("MedicineStation", "craft", "", null, new Color(0.45f, 0.62f, 0.42f), 0),
            ("CarpentryStation", "craft", "", null, new Color(0.60f, 0.45f, 0.30f), 0),
            ("ForageSpot_Wild", "forage", "", null, new Color(0.42f, 0.58f, 0.32f), 0),
            ("ForageSpot_Forest", "forage", "", null, new Color(0.30f, 0.45f, 0.28f), 1),
            ("MountainEntrance", "mountain", "", null, new Color(0.25f, 0.50f, 0.40f), 0),
            ("TrapSpot", "trap", "", null, new Color(0.55f, 0.50f, 0.30f), 0),
            ("Wardrobe", "wardrobe", "", null, new Color(0.55f, 0.35f, 0.55f), 0),
            ("MillStation", "craft", "", null, new Color(0.75f, 0.70f, 0.45f), 0),
            ("SauceStation", "craft", "", null, new Color(0.55f, 0.40f, 0.20f), 0),
            ("TeaStation", "craft", "", null, new Color(0.45f, 0.65f, 0.45f), 0),
            ("TreeSpot_A", "tree", "", null, new Color(0.30f, 0.50f, 0.25f), 0),
            ("TreeSpot_B", "tree", "", null, new Color(0.30f, 0.50f, 0.25f), 1),
            ("CageSpot", "cage", "", null, new Color(0.30f, 0.55f, 0.75f), 0),
            ("CompostStation", "compost", "", null, new Color(0.45f, 0.35f, 0.20f), 0),
            ("HouseStation", "house", "", null, new Color(0.60f, 0.50f, 0.70f), 0),
            ("LivestockStation", "livestock", "", null, new Color(0.72f, 0.52f, 0.28f), 0),
            ("SugarStation", "craft", "", null, new Color(0.85f, 0.80f, 0.60f), 0),
            ("SaltStation", "craft", "", null, new Color(0.85f, 0.85f, 0.90f), 0),
            ("FarmVendorStation", "farmvendor", "", null, new Color(0.65f, 0.55f, 0.30f), 0),
        };
    }

    /// <summary>
    /// 运行时补建内容并把蓝草加入杂货铺。幂等：已存在的物体不重复创建。
    /// 由 UIEventSystemBootstrap.Ensure() 自动补挂。
    /// </summary>
    public class ContentExpansionBootstrap : MonoBehaviour
    {
        private void Start()
        {
            int created = 0;
            foreach (var e in ContentExpansionData.Extra)
            {
                if (GameObject.Find(e.goName) != null) continue;
                CreateStation(e.goName, e.kind, e.displayName, e.lines, e.color, e.arg);
                created++;
            }

            // 扎染产业链的入口原料：蓝草进杂货铺（重建场景时 SceneBuilder 也会加）
            // 必须按物体名定位桃花谷杂货铺：现代店由 ShopWorldBootstrap 另行创建，
            // 且读档直接进现代时该店已被隐藏，GameObject.Find / FindObjectOfType 都找不到它。
            ShopSystem shop = null;
            foreach (var s in Resources.FindObjectsOfTypeAll<ShopSystem>())
                if (s != null && s.gameObject.name == "NPC_ShopKeeper") { shop = s; break; }
            var indigo = FindItem("item_indigo");
            if (shop != null && indigo != null) shop.AddShopEntry(indigo, 24);
            // 陶瓷 / 药墨产业的入口原料：陶土、药材同样在杂货铺买到（也可去野地采集）
            var clay = FindItem("item_clay");
            if (shop != null && clay != null) shop.AddShopEntry(clay, 12);
            var herb = FindItem("item_herb");
            if (shop != null && herb != null) shop.AddShopEntry(herb, 10);
            // 制药·制毒·酿酒入口原料
            var ginger = FindItem("item_ginger");
            if (shop != null && ginger != null) shop.AddShopEntry(ginger, 8);
            var ricewine = FindItem("item_ricewine");
            if (shop != null && ricewine != null) shop.AddShopEntry(ricewine, 25);
            var poisonHerb = FindItem("item_poison_herb");
            if (shop != null && poisonHerb != null) shop.AddShopEntry(poisonHerb, 10);
            // 磨坊/酱园/茶坊入口原料（收获物可买来应急，也可自己种）
            var wheat = FindItem("item_wheat");
            if (shop != null && wheat != null) shop.AddShopEntry(wheat, 15);
            var soybean = FindItem("item_soybean");
            if (shop != null && soybean != null) shop.AddShopEntry(soybean, 15);
            var teaLeaf = FindItem("item_tea_leaf");
            if (shop != null && teaLeaf != null) shop.AddShopEntry(teaLeaf, 20);

            // 可种植作物种子：小麦/大豆/玉米（买了就能种，无需升级种子包）
            var seedWheat = FindItem("seed_wheat");
            if (shop != null && seedWheat != null) shop.AddShopEntry(seedWheat, 14);
            var seedSoy = FindItem("seed_soybean");
            if (shop != null && seedSoy != null) shop.AddShopEntry(seedSoy, 14);
            var seedCorn = FindItem("seed_corn");
            if (shop != null && seedCorn != null) shop.AddShopEntry(seedCorn, 16);
            // 制糖作物种子：甘蔗/甜菜（制糖坊熬糖）
            var seedSugarcane = FindItem("seed_sugarcane");
            if (shop != null && seedSugarcane != null) shop.AddShopEntry(seedSugarcane, 18);
            var seedSugarbeet = FindItem("seed_sugarbeet");
            if (shop != null && seedSugarbeet != null) shop.AddShopEntry(seedSugarbeet, 16);

            // 工具现在也能卖，桃花谷杂货铺必须能买回来（否则卖光工具会卡死）
            if (shop != null)
            {
                AddTool(shop, "item_tool_hoe");
                AddTool(shop, "item_tool_wateringcan");
                AddTool(shop, "item_tool_sickle");
                AddTool(shop, "item_tool_axe");
            }

            // 现代货架已迁到「现代杂货铺 / 古玩店」两家独立店铺（见 ShopWorldBootstrap），
            // 古今商店互不通用，桃花谷杂货铺不再持有人民币商品。

            Debug.Log($"[ContentExpansion] 补建 {created} 个交互点（共 {ContentExpansionData.Extra.Length} 个扩展点）");
        }

        private static void CreateStation(string goName, string kind, string displayName, string[] lines, Color color, int arg)
        {
            var go = new GameObject(goName);
            StationLayoutData.Place(go); // 位置 + 名字标签（NPC 用自带名牌）

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSolidSprite(color);
            sr.sortingOrder = 9;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.3f, 1.3f);
            col.offset = new Vector2(0f, 0.4f);
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;

            switch (kind)
            {
                case "dye":
                    go.AddComponent<CraftingStation>();
                    break;
                case "fish":
                    var spot = go.AddComponent<FishingSpot>();
                    spot.SpotIndex = arg;
                    break;
                case "npc":
                    var npc = go.AddComponent<NPCController>();
                    npc.Configure(displayName, lines, color);
                    // 作息：白天在 StationLayout 的工作位上班，晚上回家（地图南侧村舍区）
                    Vector2 work = StationLayoutData.GetPosition(goName);
                    npc.EnableSchedule(work, work + new Vector2(0f, -15f));
                    break;
                case "jewel":
                    go.AddComponent<JewelryStation>();
                    break;
                case "craft":
                    go.AddComponent<CraftingStation>();
                    break;
                case "forage":
                    var forage = go.AddComponent<ForageSpot>();
                    forage.SpotIndex = arg;
                    break;
                case "mountain":
                    go.AddComponent<MountainEntrance>();
                    break;
                case "trap":
                    go.AddComponent<TrapStation>();
                    break;
                case "wardrobe":
                    go.AddComponent<WardrobeStation>();
                    break;
                case "tree":
                    var tree = go.AddComponent<TreeSpot>();
                    tree.SpotIndex = arg;
                    break;
                case "cage":
                    go.AddComponent<CageSpot>();
                    break;
                case "compost":
                    go.AddComponent<CompostStation>();
                    break;
                case "house":
                    go.AddComponent<HouseStation>();
                    break;
                case "livestock":
                    go.AddComponent<LivestockStation>();
                    break;
                case "tavern":
                    go.AddComponent<TavernStation>();
                    break;
                case "farmvendor":
                    go.AddComponent<FarmVendorStation>();
                    break;
            }
        }

        private static Sprite MakeSolidSprite(Color c)
        {
            var tex = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++) tex.SetPixel(x, y, c);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            sprite.name = "ContentSprite";
            return sprite;
        }

        private static ItemData FindItem(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        /// <summary>工具上架（宋币 90 文，卖价 60 文，防套利）</summary>
        private static void AddTool(ShopSystem shop, string id)
        {
            var item = FindItem(id);
            if (item != null) shop.AddShopEntry(item, 90);
        }
    }
}
