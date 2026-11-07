using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using PBV.Core;
using PBV.Player;
using PBV.Farming;
using PBV.Inventory;
using PBV.Systems;
using PBV.Save;
using PBV.Dialogue;
using PBV.Economy;
using PBV.UI;
using PBV.NPC;

namespace PBVEditor
{
    /// <summary>
    /// v0.2.0 灰盒场景一键构建：
    /// - 12 种作物（四季各 3）+ 5 种材料物品（造纸产业链）
    /// - 3 个 NPC + 商人 + 造纸台
    /// - 全系统装配（体力/工具/季节/好感/任务/商店/制作/存档）
    /// 幂等设计：重复执行不产生重复资产
    /// 用法：菜单 PBV -> 构建灰盒场景 v0.1.0
    /// </summary>
    public static class SceneBuilder
    {
        private const string PlaceholderDir = "Assets/Art/Placeholder";
        private const string TileDir = PlaceholderDir + "/Tiles";
        private const string DataDir = "Assets/Resources/Data";
        private const string ScenePath = "Assets/Scenes/01_FarmGreyBox.unity";
        private const string InteractableLayerName = "Interactable";

        // 12 种作物定义：(名称, id, 天数, 季节, 成熟色, 售价)
        private static readonly (string, string, int, Season, Color, int)[] CropDefs =
        {
            ("萝卜", "radish", 3, Season.Spring, new Color(0.85f, 0.25f, 0.2f), 20),
            ("白菜", "cabbage", 4, Season.Spring, new Color(0.5f, 0.8f, 0.3f), 35),
            ("菠菜", "spinach", 5, Season.Spring, new Color(0.2f, 0.55f, 0.2f), 40),
            ("番茄", "tomato", 4, Season.Summer, new Color(0.9f, 0.3f, 0.25f), 45),
            ("黄瓜", "cucumber", 5, Season.Summer, new Color(0.4f, 0.7f, 0.25f), 50),
            ("水稻", "rice", 6, Season.Summer, new Color(0.85f, 0.75f, 0.3f), 55),
            ("南瓜", "pumpkin", 6, Season.Autumn, new Color(0.9f, 0.55f, 0.2f), 60),
            ("红薯", "sweetpotato", 5, Season.Autumn, new Color(0.75f, 0.4f, 0.3f), 50),
            ("玉米", "corn", 7, Season.Autumn, new Color(0.9f, 0.8f, 0.25f), 65),
            ("大葱", "scallion", 5, Season.Winter, new Color(0.3f, 0.6f, 0.25f), 45),
            ("冬萝卜", "wradish", 6, Season.Winter, new Color(0.8f, 0.7f, 0.6f), 55),
            ("蘑菇", "mushroom", 4, Season.Winter, new Color(0.6f, 0.45f, 0.3f), 70),
        };

        // 5 种材料定义：(名称, id, 色块色, 售价, 好感值)
        private static readonly (string, string, Color, int, int)[] MaterialDefs =
        {
            ("树皮", "item_bark", new Color(0.5f, 0.35f, 0.2f), 5, 2),
            ("稻草", "item_straw", new Color(0.8f, 0.7f, 0.3f), 3, 1),
            ("纸浆", "item_pulp", new Color(0.9f, 0.88f, 0.82f), 10, 0),
            ("纸张", "item_paper", new Color(0.95f, 0.93f, 0.88f), 20, 3),
            ("宣纸", "item_xuanpaper", new Color(1f, 0.98f, 0.92f), 100, 5),
        };

        // 5 种鱼类定义：(名称, id, 色块色, 售价, 好感值)
        private static readonly (string, string, Color, int, int)[] FishDefs =
        {
            ("鲫鱼", "item_fish_crucian", new Color(0.7f, 0.7f, 0.6f), 20, 1),
            ("鲤鱼", "item_fish_carp", new Color(0.8f, 0.5f, 0.3f), 30, 2),
            ("鲶鱼", "item_fish_catfish", new Color(0.5f, 0.45f, 0.4f), 25, 1),
            ("草鱼", "item_fish_grassfish", new Color(0.4f, 0.6f, 0.4f), 40, 3),
            ("金鱼", "item_fish_goldfish", new Color(0.9f, 0.7f, 0.2f), 50, 5),
        };

        // 3 种矿石定义：(名称, id, 色块色, 售价, 好感值)
        private static readonly (string, string, Color, int, int)[] OreDefs =
        {
            ("铜矿", "item_ore_copper", new Color(0.7f, 0.45f, 0.25f), 15, 1),
            ("铁矿", "item_ore_iron", new Color(0.5f, 0.5f, 0.55f), 30, 2),
            ("金矿", "item_ore_gold", new Color(0.85f, 0.7f, 0.2f), 80, 4),
        };

        [MenuItem("PBV/构建灰盒场景 v0.1.0")]
        public static void BuildAll()
        {
            Debug.Log("[SceneBuilder] === 开始构建 v0.2.0 灰盒 ===");
            AssetDatabase.Refresh();
            EnsureLayer(InteractableLayerName);
            var sprites = CreatePlaceholderSprites();
            var tiles = CreateTileAssets(sprites);
            var data = CreateDataAssets(sprites);
            BuildScene(tiles, data);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] === 构建完成，场景: {ScenePath} ===");
        }

        public static void BuildAllFromCommandLine() { BuildAll(); EditorApplication.Exit(0); }

        [MenuItem("PBV/清除存档(开始新游戏)")]
        public static void ClearSaveFile()
        {
            var path = Path.Combine(Application.persistentDataPath, "pbv_save.json");
            if (File.Exists(path)) { File.Delete(path); Debug.Log($"[SceneBuilder] 存档已删除: {path}"); }
            else Debug.Log("[SceneBuilder] 无存档文件可清除");
        }

        public static void ClearSaveFromCommandLine() { ClearSaveFile(); EditorApplication.Exit(0); }

        // ==================== 场景搭建 ====================

        private static void BuildScene(TileSet tiles, DataSet data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultLight = Object.FindObjectOfType<Light>();
            if (defaultLight != null) Object.DestroyImmediate(defaultLight.gameObject);

            var cam = Camera.main;
            cam.orthographic = true; cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.18f);
            cam.gameObject.AddComponent<CameraFollow>();

            var globalLightGo = new GameObject("GlobalLight2D");
            var light2D = globalLightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;

            // 网格与三层 Tilemap
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            Tilemap groundMap = CreateTilemap(gridGo.transform, "Ground", 0);
            Tilemap interactMap = CreateTilemap(gridGo.transform, "Interact", 1);
            Tilemap cropMap = CreateTilemap(gridGo.transform, "Crop", 2);

            for (int x = -15; x < 15; x++)
                for (int y = -10; y < 10; y++)
                    groundMap.SetTile(new Vector3Int(x, y, 0), tiles.Ground);

            // 农田系统
            var farmGo = new GameObject("FarmSystem");
            var cropManager = farmGo.AddComponent<CropManager>();
            var farmGrid = farmGo.AddComponent<FarmGrid>();
            SetRef(farmGrid, "_groundTilemap", groundMap);
            SetRef(farmGrid, "_interactTilemap", interactMap);
            SetRef(farmGrid, "_cropTilemap", cropMap);
            SetRef(farmGrid, "_tilledTile", tiles.Tilled);
            SetRef(farmGrid, "_wateredTile", tiles.Watered);
            SetRef(farmGrid, "_cropManager", cropManager);
            SetRef(cropManager, "_farmGrid", farmGrid);
            SetRef(cropManager, "_cropTilemap", cropMap);

            // 选中地块高亮
            var indicatorGo = new GameObject("SelectionIndicator");
            indicatorGo.transform.SetParent(farmGo.transform, false);
            indicatorGo.AddComponent<SpriteRenderer>();
            indicatorGo.AddComponent<SelectionIndicator>();

            // 系统管理器（核心系统）
            var systemsGo = new GameObject("Systems");
            systemsGo.AddComponent<InputHandler>();
            var timeManager = systemsGo.AddComponent<TimeManager>();
            SetRef(timeManager, "_globalLight", light2D);
            SetGradient(timeManager);
            systemsGo.AddComponent<InventorySystem>();
            systemsGo.AddComponent<PlayerWallet>();
            systemsGo.AddComponent<SeasonSystem>();
            systemsGo.AddComponent<WeatherSystem>();
            systemsGo.AddComponent<PlayerStats>();
            systemsGo.AddComponent<ToolManager>();
            systemsGo.AddComponent<RelationshipSystem>();
            systemsGo.AddComponent<QuestManager>();
            systemsGo.AddComponent<CraftingSystem>();
            systemsGo.AddComponent<CookingSystem>();
            systemsGo.AddComponent<FishingSystem>();
            systemsGo.AddComponent<SkillSystem>();
            systemsGo.AddComponent<MineSystem>();
            systemsGo.AddComponent<FestivalSystem>();

            // 存档系统 + 目录表（全部作物 + 材料）
            var allItems = data.AllItems;
            var allCrops = data.AllCrops;
            var saveManager = systemsGo.AddComponent<SaveManager>();
            SetArray(saveManager, "_itemCatalog", allItems);
            SetArray(saveManager, "_cropCatalog", allCrops);

            // UI 系统
            systemsGo.AddComponent<DialogueManager>();
            systemsGo.AddComponent<InventoryUI>();
            systemsGo.AddComponent<HUD>();
            systemsGo.AddComponent<InteractionPromptUI>();
            systemsGo.AddComponent<QuestLogUI>();
            systemsGo.AddComponent<ShopUI>();
            systemsGo.AddComponent<CraftingUI>();
            systemsGo.AddComponent<CookingUI>();
            systemsGo.AddComponent<MineUI>();
            systemsGo.AddComponent<ToolUpgradeUI>();
            systemsGo.AddComponent<StarterKit>();

            // 玩家
            var playerGo = new GameObject("Player");
            playerGo.transform.position = Vector3.zero;
            var playerSr = playerGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = data.PlayerSprite; playerSr.sortingOrder = 10;
            playerGo.AddComponent<Rigidbody2D>();
            var playerController = playerGo.AddComponent<PlayerController>();
            SetInt(playerController, "_interactLayer", 1 << LayerMask.NameToLayer(InteractableLayerName));

            // NPC：老农
            CreateNPC("NPC_LaoNong", new Vector3(5f, 2f, 0), data.NpcSprite, "老农", new string[]
            {
                "后生，你是从山外来的吧？",
                "这片地荒了三年，你若肯下力气，便是你的了。",
                "锄地、下种、浇水……庄稼认人，日日照看才有收成。",
                "收成放到西边那个木箱里，隔天一早换了钱再买些种子。",
                "有啥收成舍不得卖的，也可以拿来送人，乡里乡亲的。"
            });

            // NPC：药婆
            CreateNPC("NPC_YaoPo", new Vector3(-5f, -3f, 0), data.NpcSprite2, "药婆", new string[]
            {
                "哟，新来的后生，老身这儿有草药方子。",
                "山里的树皮、田里的稻草，别嫌它们不起眼。",
                "造纸的功夫，是咱们桃花谷的老手艺了。",
                "你要是有宣纸，拿来给老婆子瞧瞧。"
            });

            // NPC：书生
            CreateNPC("NPC_ShuSheng", new Vector3(7f, -2f, 0), data.NpcSprite3, "书生", new string[]
            {
                "桃花谷的宣纸，远近闻名。",
                "树皮制浆，稻草亦可，抄纸晾干便是纸张。",
                "五张纸再精制一番，便得宣纸一张，可卖百金。",
                "兄台若能做出宣纸，小生愿高价收之。"
            });

            // 出货箱
            var binGo = new GameObject("ShippingBin");
            binGo.transform.position = new Vector3(-4f, 2f, 0);
            var binSr = binGo.AddComponent<SpriteRenderer>();
            binSr.sprite = data.BinSprite; binSr.sortingOrder = 10;
            var binCol = binGo.AddComponent<BoxCollider2D>();
            binCol.size = new Vector2(1.2f, 1.2f); binCol.offset = new Vector2(0, 0.4f);
            binGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            binGo.AddComponent<ShippingBinSystem>();

            // 商人（杂货铺）
            var shopGo = new GameObject("NPC_ShopKeeper");
            shopGo.transform.position = new Vector3(0f, 5f, 0);
            var shopSr = shopGo.AddComponent<SpriteRenderer>();
            shopSr.sprite = data.NpcSprite; shopSr.sortingOrder = 10;
            var shopCol = shopGo.AddComponent<BoxCollider2D>();
            shopCol.size = new Vector2(1f, 1.4f); shopCol.offset = new Vector2(0, 0.5f);
            shopGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            var shop = shopGo.AddComponent<ShopSystem>();
            SetString(shop, "_shopName", "杂货铺");
            // 商品列表：全部种子 + 树皮 + 稻草
            var shopEntries = new System.Collections.Generic.List<Object>();
            foreach (var cd in CropDefs)
            {
                var seed = data.GetSeed(cd.Item2);
                if (seed != null) shopEntries.Add(seed);
            }
            var bark = data.GetItem("item_bark");
            var straw = data.GetItem("item_straw");
            if (bark != null) shopEntries.Add(bark);
            if (straw != null) shopEntries.Add(straw);
            SetShopEntries(shop, shopEntries.ToArray(), data);

            // 造纸台（制作台）
            var craftGo = new GameObject("CraftingStation");
            craftGo.transform.position = new Vector3(3f, -4f, 0);
            var craftSr = craftGo.AddComponent<SpriteRenderer>();
            craftSr.sprite = data.BinSprite; craftSr.sortingOrder = 10; // 复用木箱贴图
            craftSr.color = new Color(0.7f, 0.6f, 0.5f);
            var craftCol = craftGo.AddComponent<BoxCollider2D>();
            craftCol.size = new Vector2(1.2f, 1.2f); craftCol.offset = new Vector2(0, 0.4f);
            craftGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            craftGo.AddComponent<CraftingStation>();

            // 厨房（烹饪台）
            var kitchenGo = new GameObject("CookingStation");
            kitchenGo.transform.position = new Vector3(-3f, -4f, 0);
            var kitchenSr = kitchenGo.AddComponent<SpriteRenderer>();
            kitchenSr.sprite = data.BinSprite; kitchenSr.sortingOrder = 10;
            kitchenSr.color = new Color(0.6f, 0.4f, 0.3f);
            var kitchenCol = kitchenGo.AddComponent<BoxCollider2D>();
            kitchenCol.size = new Vector2(1.2f, 1.2f); kitchenCol.offset = new Vector2(0, 0.4f);
            kitchenGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            kitchenGo.AddComponent<CookingStation>();

            // 钓鱼点（池塘边）
            var fishGo = new GameObject("FishingSpot");
            fishGo.transform.position = new Vector3(-10f, -6f, 0);
            var fishSr = fishGo.AddComponent<SpriteRenderer>();
            fishSr.sprite = data.BinSprite; fishSr.sortingOrder = 9;
            fishSr.color = new Color(0.3f, 0.5f, 0.7f);
            var fishCol = fishGo.AddComponent<BoxCollider2D>();
            fishCol.size = new Vector2(2f, 1f); fishCol.offset = new Vector2(0, 0.3f);
            fishGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            fishGo.AddComponent<FishingSpot>();

            // 矿洞入口
            var mineGo = new GameObject("MineEntrance");
            mineGo.transform.position = new Vector3(10f, 5f, 0);
            var mineSr = mineGo.AddComponent<SpriteRenderer>();
            mineSr.sprite = data.BinSprite; mineSr.sortingOrder = 9;
            mineSr.color = new Color(0.35f, 0.3f, 0.25f);
            var mineCol = mineGo.AddComponent<BoxCollider2D>();
            mineCol.size = new Vector2(1.5f, 2f); mineCol.offset = new Vector2(0, 0.8f);
            mineGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            mineGo.AddComponent<MineEntrance>();

            // 铁匠铺（工具升级）
            var smithGo = new GameObject("ToolUpgradeStation");
            smithGo.transform.position = new Vector3(-10f, 5f, 0);
            var smithSr = smithGo.AddComponent<SpriteRenderer>();
            smithSr.sprite = data.BinSprite; smithSr.sortingOrder = 9;
            smithSr.color = new Color(0.4f, 0.35f, 0.3f);
            var smithCol = smithGo.AddComponent<BoxCollider2D>();
            smithCol.size = new Vector2(1.5f, 2f); smithCol.offset = new Vector2(0, 0.8f);
            smithGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            smithGo.AddComponent<PBV.Player.ToolUpgradeStation>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[SceneBuilder] 场景已保存");
        }

        private static void CreateNPC(string goName, Vector3 pos, Sprite sprite, string name, string[] lines)
        {
            var go = new GameObject(goName);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 10;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1.4f); col.offset = new Vector2(0, 0.5f);
            go.layer = LayerMask.NameToLayer(InteractableLayerName);
            var npc = go.AddComponent<NPCController>();
            SetString(npc, "npcName", name);
            SetStringArray(npc, "dialogueLines", lines);
        }

        private static void SetShopEntries(ShopSystem shop, Object[] items, DataSet data)
        {
            var so = new SerializedObject(shop);
            var prop = so.FindProperty("_items");
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                var entry = prop.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("item").objectReferenceValue = items[i];
                var itemData = items[i] as ItemData;
                int price = itemData != null ? Mathf.Max(1, itemData.sellPrice * 2) : 10;
                entry.FindPropertyRelative("price").intValue = price;
                entry.FindPropertyRelative("stock").intValue = -1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Tilemap CreateTilemap(Transform gridTransform, string name, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(gridTransform, false);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortOrder;
            return map;
        }

        // ==================== 占位贴图生成 ====================

        private static SpriteSet CreatePlaceholderSprites()
        {
            Directory.CreateDirectory(PlaceholderDir);
            Directory.CreateDirectory(TileDir);
            var s = new SpriteSet();
            s.Ground = GetSprite("tex_ground", CreateTileTexture(new Color(0.45f, 0.62f, 0.35f), 0.12f));
            s.Tilled = GetSprite("tex_tilled", CreateTileTexture(new Color(0.55f, 0.4f, 0.28f), 0.15f));
            s.Watered = GetSprite("tex_watered", CreateTileTexture(new Color(0.38f, 0.28f, 0.2f), 0.12f));
            s.PlayerSprite = GetSprite("spr_player", CreateCharacterTexture(new Color(0.3f, 0.45f, 0.75f), new Color(0.95f, 0.8f, 0.65f)));
            s.NpcSprite = GetSprite("spr_npc", CreateCharacterTexture(new Color(0.75f, 0.4f, 0.5f), new Color(0.92f, 0.75f, 0.6f)));
            s.NpcSprite2 = GetSprite("spr_npc2", CreateCharacterTexture(new Color(0.5f, 0.6f, 0.35f), new Color(0.9f, 0.78f, 0.62f)));
            s.NpcSprite3 = GetSprite("spr_npc3", CreateCharacterTexture(new Color(0.4f, 0.35f, 0.55f), new Color(0.93f, 0.8f, 0.68f)));
            s.BinSprite = GetSprite("spr_bin", CreateBinTexture());

            // 12 种作物的阶段贴图 + 种子/收获图标
            s.CropStages = new Sprite[CropDefs.Length][];
            s.CropSeedIcons = new Sprite[CropDefs.Length];
            s.CropItemIcons = new Sprite[CropDefs.Length];
            for (int i = 0; i < CropDefs.Length; i++)
            {
                var cd = CropDefs[i];
                s.CropStages[i] = CreateCropStages(cd.Item2, cd.Item5);
                s.CropSeedIcons[i] = GetSprite($"icon_seed_{cd.Item2}", CreateIconTexture(new Color(0.8f, 0.7f, 0.5f)));
                s.CropItemIcons[i] = GetSprite($"icon_item_{cd.Item2}", CreateIconTexture(cd.Item5));
            }

            // 5 种材料图标
            s.MaterialIcons = new Sprite[MaterialDefs.Length];
            for (int i = 0; i < MaterialDefs.Length; i++)
                s.MaterialIcons[i] = GetSprite($"icon_{MaterialDefs[i].Item2}", CreateIconTexture(MaterialDefs[i].Item3));

            return s;
        }

        private static Sprite GetSprite(string name, Texture2D tex)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            if (!File.Exists(path)) File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] CreateCropStages(string cropName, Color matureColor)
        {
            var stages = new Sprite[4];
            for (int i = 0; i < 4; i++)
                stages[i] = GetSprite($"spr_crop_{cropName}_s{i}", CreateCropStageTexture(i, matureColor));
            return stages;
        }

        private static Texture2D CreateTileTexture(Color baseColor, float noise)
        {
            var tex = NewTexture();
            var rng = new System.Random(42);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, baseColor * (1f + (float)(rng.NextDouble() - 0.5) * noise));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCharacterTexture(Color bodyColor, Color skinColor)
        {
            var tex = NewTexture(transparent: true);
            FillRect(tex, 11, 3, 10, 7, new Color(0.2f, 0.2f, 0.25f));
            FillRect(tex, 10, 10, 12, 9, bodyColor);
            FillRect(tex, 12, 19, 8, 8, skinColor);
            FillRect(tex, 12, 25, 8, 3, new Color(0.15f, 0.1f, 0.08f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBinTexture()
        {
            var tex = NewTexture(transparent: true);
            FillRect(tex, 4, 4, 24, 22, new Color(0.5f, 0.35f, 0.2f));
            FillRect(tex, 4, 18, 24, 4, new Color(0.4f, 0.26f, 0.14f));
            FillRect(tex, 4, 24, 24, 2, new Color(0.32f, 0.2f, 0.1f));
            FillRect(tex, 14, 8, 4, 14, new Color(0.42f, 0.3f, 0.17f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStageTexture(int stage, Color matureColor)
        {
            var tex = NewTexture(transparent: true);
            Color leaf = new Color(0.3f, 0.65f, 0.25f);
            Color leafDark = new Color(0.22f, 0.5f, 0.2f);
            int stemHeight = 4 + stage * 5;
            FillRect(tex, 15, 4, 2, stemHeight, leafDark);
            FillRect(tex, 12, 4 + stemHeight - 2, 3, 2, leaf);
            FillRect(tex, 17, 4 + stemHeight - 4, 3, 2, leaf);
            if (stage >= 2) FillRect(tex, 13, 2, 6, 4, leaf);
            if (stage >= 3)
            {
                FillRect(tex, 14, 2 + stemHeight, 5, 5, matureColor);
                FillRect(tex, 15, 1 + stemHeight, 3, 2, matureColor * 1.2f);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateIconTexture(Color color)
        {
            var tex = NewTexture(transparent: true);
            FillRect(tex, 8, 8, 16, 16, color);
            FillRect(tex, 10, 10, 12, 12, color * 1.15f);
            tex.Apply();
            return tex;
        }

        private static Texture2D NewTexture(bool transparent = false)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            if (transparent)
            {
                var clear = new Color(0, 0, 0, 0);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) tex.SetPixel(x, y, clear);
            }
            return tex;
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            for (int py = y; py < y + h; py++)
                for (int px = x; px < x + w; px++)
                    if (px >= 0 && px < 32 && py >= 0 && py < 32) tex.SetPixel(px, py, color);
        }

        // ==================== Tile / 数据资产 ====================

        private static TileSet CreateTileAssets(SpriteSet sprites)
        {
            Directory.CreateDirectory(TileDir);
            return new TileSet
            {
                Ground = GetOrCreateTile($"{TileDir}/tile_ground.asset", sprites.Ground),
                Tilled = GetOrCreateTile($"{TileDir}/tile_tilled.asset", sprites.Tilled),
                Watered = GetOrCreateTile($"{TileDir}/tile_watered.asset", sprites.Watered)
            };
        }

        private static Tile GetOrCreateTile(string path, Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null) { existing.sprite = sprite; EditorUtility.SetDirty(existing); return existing; }
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static DataSet CreateDataAssets(SpriteSet sprites)
        {
            Directory.CreateDirectory(DataDir);
            var data = new DataSet();

            // 12 种作物 + 种子 + 收获物品
            data.Crops = new CropData[CropDefs.Length];
            data.Seeds = new ItemData[CropDefs.Length];
            data.HarvestItems = new ItemData[CropDefs.Length];

            for (int i = 0; i < CropDefs.Length; i++)
            {
                var def = CropDefs[i];
                var crop = GetOrCreateAsset<CropData>($"{DataDir}/crop_{def.Item2}.asset");
                crop.cropName = def.Item1;
                crop.description = $"{def.Item1}，{def.Item3}天成熟。";
                crop.daysToMature = def.Item3;
                crop.seasons = new Season[] { def.Item4 };
                crop.growthSprites = sprites.CropStages[i];
                crop.minYield = 1;
                crop.maxYield = 2;
                EditorUtility.SetDirty(crop);
                data.Crops[i] = crop;

                var seed = GetOrCreateAsset<ItemData>($"{DataDir}/seed_{def.Item2}.asset");
                seed.id = $"seed_{def.Item2}";
                seed.itemName = $"{def.Item1}种子";
                seed.description = $"种下去，浇点水，等{def.Item3}天。";
                seed.type = ItemType.Seed;
                seed.icon = sprites.CropSeedIcons[i];
                seed.sellPrice = Mathf.Max(5, def.Item6 / 4);
                seed.cropToPlant = crop;
                EditorUtility.SetDirty(seed);
                data.Seeds[i] = seed;

                var harvest = GetOrCreateAsset<ItemData>($"{DataDir}/item_{def.Item2}.asset");
                harvest.id = $"item_{def.Item2}";
                harvest.itemName = def.Item1;
                harvest.description = $"{def.Item1}，{def.Item4}季作物。";
                harvest.type = ItemType.Resource;
                harvest.icon = sprites.CropItemIcons[i];
                harvest.sellPrice = def.Item6;
                EditorUtility.SetDirty(harvest);
                data.HarvestItems[i] = harvest;

                crop.harvestItem = harvest;
                EditorUtility.SetDirty(crop);
            }

            // 5 种材料物品（造纸产业链）
            data.Materials = new ItemData[MaterialDefs.Length];
            for (int i = 0; i < MaterialDefs.Length; i++)
            {
                var def = MaterialDefs[i];
                var item = GetOrCreateAsset<ItemData>($"{DataDir}/{def.Item2}.asset");
                item.id = def.Item2;
                item.itemName = def.Item1;
                item.description = def.Item2 == "item_xuanpaper" ? "桃花谷特产宣纸，百金难求。" : $"{def.Item1}，造纸材料。";
                item.type = ItemType.Resource;
                item.icon = sprites.MaterialIcons[i];
                item.sellPrice = def.Item4;
                item.affectionValue = def.Item5;
                EditorUtility.SetDirty(item);
                data.Materials[i] = item;
            }

            // 5 种鱼类物品
            data.FishItems = new ItemData[FishDefs.Length];
            for (int i = 0; i < FishDefs.Length; i++)
            {
                var def = FishDefs[i];
                var item = GetOrCreateAsset<ItemData>($"{DataDir}/{def.Item2}.asset");
                item.id = def.Item2;
                item.itemName = def.Item1;
                item.description = $"{def.Item1}，钓鱼可得。";
                item.type = ItemType.Resource;
                item.icon = GetSprite($"icon_{def.Item2}", CreateIconTexture(def.Item3));
                item.sellPrice = def.Item4;
                item.affectionValue = def.Item5;
                EditorUtility.SetDirty(item);
                data.FishItems[i] = item;
            }

            // 3 种矿石物品
            data.OreItems = new ItemData[OreDefs.Length];
            for (int i = 0; i < OreDefs.Length; i++)
            {
                var def = OreDefs[i];
                var item = GetOrCreateAsset<ItemData>($"{DataDir}/{def.Item2}.asset");
                item.id = def.Item2;
                item.itemName = def.Item1;
                item.description = $"{def.Item1}，矿洞挖矿可得。";
                item.type = ItemType.Resource;
                item.icon = GetSprite($"icon_{def.Item2}", CreateIconTexture(def.Item3));
                item.sellPrice = def.Item4;
                item.affectionValue = def.Item5;
                EditorUtility.SetDirty(item);
                data.OreItems[i] = item;
            }

            data.PlayerSprite = sprites.PlayerSprite;
            data.NpcSprite = sprites.NpcSprite;
            data.NpcSprite2 = sprites.NpcSprite2;
            data.NpcSprite3 = sprites.NpcSprite3;
            data.BinSprite = sprites.BinSprite;
            return data;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ==================== SerializedObject 工具 ====================

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[SceneBuilder] 字段未找到: {target.GetType().Name}.{field}");
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetGradient(TimeManager timeManager)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.00f),
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.23f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.26f),
                    new GradientColorKey(Color.white, 0.33f),
                    new GradientColorKey(Color.white, 0.70f),
                    new GradientColorKey(new Color(1f, 0.65f, 0.45f), 0.79f),
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.87f),
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 1.00f)
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var so = new SerializedObject(timeManager);
            var prop = so.FindProperty("_dayNightGradient");
            if (prop != null) { prop.gradientValue = gradient; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                var layerProp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[SceneBuilder] 已创建 Layer: {layerName} (index {i})");
                    return;
                }
            }
        }

        // ==================== 数据容器 ====================

        private class SpriteSet
        {
            public Sprite Ground, Tilled, Watered;
            public Sprite PlayerSprite, NpcSprite, NpcSprite2, NpcSprite3, BinSprite;
            public Sprite[][] CropStages;       // 12 种作物 x 4 阶段
            public Sprite[] CropSeedIcons;      // 12
            public Sprite[] CropItemIcons;      // 12
            public Sprite[] MaterialIcons;      // 5
        }

        private class TileSet { public Tile Ground, Tilled, Watered; }

        private class DataSet
        {
            public CropData[] Crops;       // 12
            public ItemData[] Seeds;       // 12
            public ItemData[] HarvestItems;// 12
            public ItemData[] Materials;   // 5
            public ItemData[] FishItems;   // 5
            public ItemData[] OreItems;    // 3
            public Sprite PlayerSprite, NpcSprite, NpcSprite2, NpcSprite3, BinSprite;

            public ItemData GetSeed(string cropId) { for (int i = 0; i < CropDefs.Length; i++) if (CropDefs[i].Item2 == cropId) return Seeds[i]; return null; }
            public ItemData GetItem(string itemId) { if (Materials != null) foreach (var m in Materials) if (m.id == itemId) return m; if (FishItems != null) foreach (var f in FishItems) if (f.id == itemId) return f; return null; }
            public Object[] AllItems { get { var list = new System.Collections.Generic.List<Object>(); if (Seeds != null) list.AddRange(Seeds); if (HarvestItems != null) list.AddRange(HarvestItems); if (Materials != null) list.AddRange(Materials); if (FishItems != null) list.AddRange(FishItems); if (OreItems != null) list.AddRange(OreItems); return list.ToArray(); } }
            public Object[] AllCrops { get { return Crops ?? System.Array.Empty<Object>(); } }
        }
    }
}
