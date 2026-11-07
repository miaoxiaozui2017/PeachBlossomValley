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
    /// v0.1.0 灰盒场景一键构建：
    /// 1. 生成占位像素贴图(32x32)并导入为 Sprite
    /// 2. 生成 Tile / CropData / ItemData 资产(Assets/Resources/Data)
    /// 3. 搭建 01_FarmGreyBox 场景(相机/灯光/农田/玩家/NPC/出货箱/系统管理器)
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

        [MenuItem("PBV/构建灰盒场景 v0.1.0")]
        public static void BuildAll()
        {
            Debug.Log("[SceneBuilder] === 开始构建 v0.1.0 灰盒 ===");
            AssetDatabase.Refresh();

            EnsureLayer(InteractableLayerName);

            var sprites = CreatePlaceholderSprites();
            var tiles = CreateTileAssets(sprites);
            var data = CreateDataAssets(sprites);
            BuildScene(tiles, data);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] === 构建完成，场景: {ScenePath} ===");
        }

        /// <summary>命令行批处理入口</summary>
        public static void BuildAllFromCommandLine()
        {
            BuildAll();
            EditorApplication.Exit(0);
        }

        /// <summary>清除运行时存档（开始新游戏），用于摆脱冒烟测试等残留存档</summary>
        [MenuItem("PBV/清除存档(开始新游戏)")]
        public static void ClearSaveFile()
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "pbv_save.json");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                Debug.Log($"[SceneBuilder] 存档已删除: {path}");
            }
            else
            {
                Debug.Log("[SceneBuilder] 无存档文件可清除");
            }
        }

        /// <summary>命令行：清除存档后退出</summary>
        public static void ClearSaveFromCommandLine()
        {
            ClearSaveFile();
            EditorApplication.Exit(0);
        }

        // ---------------- 场景搭建 ----------------

        private static void BuildScene(TileSet tiles, DataSet data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 移除默认 3D 平行光（URP 2D 用 Global Light2D）
            var defaultLight = Object.FindObjectOfType<Light>();
            if (defaultLight != null) Object.DestroyImmediate(defaultLight.gameObject);

            // 相机
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.18f);
            cam.gameObject.AddComponent<CameraFollow>();

            // 全局 2D 灯光（昼夜）
            var globalLightGo = new GameObject("GlobalLight2D");
            var light2D = globalLightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;

            // 网格与三层 Tilemap
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            Tilemap groundMap = CreateTilemap(gridGo.transform, "Ground", 0);
            Tilemap interactMap = CreateTilemap(gridGo.transform, "Interact", 1);
            Tilemap cropMap = CreateTilemap(gridGo.transform, "Crop", 2);

            // 填充 30x20 草地
            for (int x = -15; x < 15; x++)
            {
                for (int y = -10; y < 10; y++)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), tiles.Ground);
                }
            }

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
            SetRef(farmGrid, "_debugCrop", data.CropRadish);
            SetRef(cropManager, "_farmGrid", farmGrid);
            SetRef(cropManager, "_cropTilemap", cropMap);

            // 选中地块高亮指示器（修复 playtest bug：运行时看不到要耕哪块地）
            var indicatorGo = new GameObject("SelectionIndicator");
            indicatorGo.transform.SetParent(farmGo.transform, false);
            indicatorGo.AddComponent<SpriteRenderer>();
            var indicator = indicatorGo.AddComponent<SelectionIndicator>();

            // 系统管理器
            var systemsGo = new GameObject("Systems");
            systemsGo.AddComponent<InputHandler>();

            var timeManager = systemsGo.AddComponent<TimeManager>();
            SetRef(timeManager, "_globalLight", light2D);
            SetGradient(timeManager);

            systemsGo.AddComponent<InventorySystem>();
            systemsGo.AddComponent<PlayerWallet>();

            var saveManager = systemsGo.AddComponent<SaveManager>();
            SetArray(saveManager, "_itemCatalog", new Object[] { data.SeedRadish, data.ItemRadish, data.SeedCabbage, data.ItemCabbage });
            SetArray(saveManager, "_cropCatalog", new Object[] { data.CropRadish, data.CropCabbage });

            systemsGo.AddComponent<DialogueManager>();
            systemsGo.AddComponent<InventoryUI>();
            systemsGo.AddComponent<HUD>();
            systemsGo.AddComponent<InteractionPromptUI>();
            systemsGo.AddComponent<StarterKit>();

            // 玩家
            var playerGo = new GameObject("Player");
            playerGo.transform.position = Vector3.zero;
            var playerSr = playerGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = data.PlayerSprite;
            playerSr.sortingOrder = 10;
            playerGo.AddComponent<Rigidbody2D>();
            var playerController = playerGo.AddComponent<PlayerController>();
            int layerMask = 1 << LayerMask.NameToLayer(InteractableLayerName);
            SetInt(playerController, "_interactLayer", layerMask);

            // NPC（老农）
            var npcGo = new GameObject("NPC_LaoNong");
            npcGo.transform.position = new Vector3(5f, 2f, 0);
            var npcSr = npcGo.AddComponent<SpriteRenderer>();
            npcSr.sprite = data.NpcSprite;
            npcSr.sortingOrder = 10;
            var npcCollider = npcGo.AddComponent<BoxCollider2D>();
            npcCollider.size = new Vector2(1f, 1.4f);
            npcCollider.offset = new Vector2(0, 0.5f);
            npcGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            var npc = npcGo.AddComponent<NPCController>();
            SetString(npc, "npcName", "老农");
            SetStringArray(npc, "dialogueLines", new string[]
            {
                "后生，你是从山外来的吧？",
                "这片地荒了三年，你若肯下力气，便是你的了。",
                "锄地、下种、浇水……庄稼认人，日日照看才有收成。",
                "收成放到西边那个木箱里，隔天一早换了钱再买些种子。"
            });

            // 出货箱
            var binGo = new GameObject("ShippingBin");
            binGo.transform.position = new Vector3(-4f, 2f, 0);
            var binSr = binGo.AddComponent<SpriteRenderer>();
            binSr.sprite = data.BinSprite;
            binSr.sortingOrder = 10;
            var binCollider = binGo.AddComponent<BoxCollider2D>();
            binCollider.size = new Vector2(1.2f, 1.2f);
            binCollider.offset = new Vector2(0, 0.4f);
            binGo.layer = LayerMask.NameToLayer(InteractableLayerName);
            binGo.AddComponent<ShippingBinSystem>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[SceneBuilder] 场景已保存");
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

        // ---------------- 占位贴图生成 ----------------

        private static SpriteSet CreatePlaceholderSprites()
        {
            Directory.CreateDirectory(PlaceholderDir);
            Directory.CreateDirectory(TileDir);

            return new SpriteSet
            {
                Ground = GetSprite("tex_ground", CreateTileTexture(new Color(0.45f, 0.62f, 0.35f), 0.12f)),
                Tilled = GetSprite("tex_tilled", CreateTileTexture(new Color(0.55f, 0.4f, 0.28f), 0.15f)),
                Watered = GetSprite("tex_watered", CreateTileTexture(new Color(0.38f, 0.28f, 0.2f), 0.12f)),
                PlayerSprite = GetSprite("spr_player", CreateCharacterTexture(new Color(0.3f, 0.45f, 0.75f), new Color(0.95f, 0.8f, 0.65f))),
                NpcSprite = GetSprite("spr_npc", CreateCharacterTexture(new Color(0.75f, 0.4f, 0.5f), new Color(0.92f, 0.75f, 0.6f))),
                BinSprite = GetSprite("spr_bin", CreateBinTexture()),
                RadishStages = CreateCropStages("radish", new Color(0.85f, 0.25f, 0.2f)),
                CabbageStages = CreateCropStages("cabbage", new Color(0.5f, 0.8f, 0.3f)),
                SeedRadishIcon = GetSprite("icon_seed_radish", CreateIconTexture(new Color(0.8f, 0.7f, 0.5f))),
                ItemRadishIcon = GetSprite("icon_item_radish", CreateIconTexture(new Color(0.85f, 0.25f, 0.2f))),
                SeedCabbageIcon = GetSprite("icon_seed_cabbage", CreateIconTexture(new Color(0.7f, 0.75f, 0.5f))),
                ItemCabbageIcon = GetSprite("icon_item_cabbage", CreateIconTexture(new Color(0.5f, 0.8f, 0.3f)))
            };
        }

        private static Sprite GetSprite(string name, Texture2D tex)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
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
            {
                stages[i] = GetSprite($"spr_crop_{cropName}_s{i}", CreateCropStageTexture(i, matureColor));
            }
            return stages;
        }

        /// <summary>带噪点的纯色地块贴图</summary>
        private static Texture2D CreateTileTexture(Color baseColor, float noise)
        {
            var tex = NewTexture();
            var rng = new System.Random(42);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float v = 1f + (float)(rng.NextDouble() - 0.5) * noise;
                    tex.SetPixel(x, y, baseColor * v);
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>占位角色：头+身+腿色块（32x32，人物约 16x26）</summary>
        private static Texture2D CreateCharacterTexture(Color bodyColor, Color skinColor)
        {
            var tex = NewTexture(transparent: true);
            FillRect(tex, 11, 3, 10, 7, new Color(0.2f, 0.2f, 0.25f));      // 腿
            FillRect(tex, 10, 10, 12, 9, bodyColor);                        // 身体
            FillRect(tex, 12, 19, 8, 8, skinColor);                         // 头
            FillRect(tex, 12, 25, 8, 3, new Color(0.15f, 0.1f, 0.08f));     // 头发
            tex.Apply();
            return tex;
        }

        /// <summary>出货箱：棕色木箱</summary>
        private static Texture2D CreateBinTexture()
        {
            var tex = NewTexture(transparent: true);
            FillRect(tex, 4, 4, 24, 22, new Color(0.5f, 0.35f, 0.2f));
            FillRect(tex, 4, 18, 24, 4, new Color(0.4f, 0.26f, 0.14f));     // 箱盖
            FillRect(tex, 4, 24, 24, 2, new Color(0.32f, 0.2f, 0.1f));      // 顶部边缘
            FillRect(tex, 14, 8, 4, 14, new Color(0.42f, 0.3f, 0.17f));     // 板缝
            tex.Apply();
            return tex;
        }

        /// <summary>作物生长阶段贴图：0萌芽 -> 3成熟(带果实色)</summary>
        private static Texture2D CreateCropStageTexture(int stage, Color matureColor)
        {
            var tex = NewTexture(transparent: true);
            Color leaf = new Color(0.3f, 0.65f, 0.25f);
            Color leafDark = new Color(0.22f, 0.5f, 0.2f);

            int stemHeight = 4 + stage * 5;
            FillRect(tex, 15, 4, 2, stemHeight, leafDark);                  // 茎
            FillRect(tex, 12, 4 + stemHeight - 2, 3, 2, leaf);              // 左叶
            FillRect(tex, 17, 4 + stemHeight - 4, 3, 2, leaf);              // 右叶

            if (stage >= 2)
            {
                FillRect(tex, 13, 2, 6, 4, leaf);                           // 冠叶
            }
            if (stage >= 3)
            {
                FillRect(tex, 14, 2 + stemHeight, 5, 5, matureColor);       // 果实
                FillRect(tex, 15, 1 + stemHeight, 3, 2, matureColor * 1.2f);
            }
            tex.Apply();
            return tex;
        }

        /// <summary>物品图标：中心色块</summary>
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
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                        tex.SetPixel(x, y, clear);
            }
            return tex;
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            for (int py = y; py < y + h; py++)
                for (int px = x; px < x + w; px++)
                    if (px >= 0 && px < 32 && py >= 0 && py < 32)
                        tex.SetPixel(px, py, color);
        }

        // ---------------- Tile / 数据资产 ----------------

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
            if (existing != null)
            {
                existing.sprite = sprite;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static DataSet CreateDataAssets(SpriteSet sprites)
        {
            Directory.CreateDirectory(DataDir);

            var cropRadish = GetOrCreateAsset<CropData>($"{DataDir}/crop_radish.asset");
            cropRadish.cropName = "萝卜";
            cropRadish.description = "最常见的作物，3天成熟。";
            cropRadish.daysToMature = 3;
            cropRadish.growthSprites = sprites.RadishStages;
            cropRadish.minYield = 1;
            cropRadish.maxYield = 2;
            EditorUtility.SetDirty(cropRadish);

            var cropCabbage = GetOrCreateAsset<CropData>($"{DataDir}/crop_cabbage.asset");
            cropCabbage.cropName = "白菜";
            cropCabbage.description = "耐寒的作物，4天成熟。";
            cropCabbage.daysToMature = 4;
            cropCabbage.growthSprites = sprites.CabbageStages;
            cropCabbage.minYield = 1;
            cropCabbage.maxYield = 2;
            EditorUtility.SetDirty(cropCabbage);

            var seedRadish = GetOrCreateAsset<ItemData>($"{DataDir}/seed_radish.asset");
            seedRadish.id = "seed_radish";
            seedRadish.itemName = "萝卜种子";
            seedRadish.description = "种下去，浇点水，等三天。";
            seedRadish.type = ItemType.Seed;
            seedRadish.icon = sprites.SeedRadishIcon;
            seedRadish.sellPrice = 5;
            seedRadish.cropToPlant = cropRadish;
            EditorUtility.SetDirty(seedRadish);

            var itemRadish = GetOrCreateAsset<ItemData>($"{DataDir}/item_radish.asset");
            itemRadish.id = "item_radish";
            itemRadish.itemName = "萝卜";
            itemRadish.description = "爽脆可口的白萝卜。";
            itemRadish.type = ItemType.Resource;
            itemRadish.icon = sprites.ItemRadishIcon;
            itemRadish.sellPrice = 20;
            EditorUtility.SetDirty(itemRadish);

            var seedCabbage = GetOrCreateAsset<ItemData>($"{DataDir}/seed_cabbage.asset");
            seedCabbage.id = "seed_cabbage";
            seedCabbage.itemName = "白菜种子";
            seedCabbage.description = "耐寒的种子，四天成熟。";
            seedCabbage.type = ItemType.Seed;
            seedCabbage.icon = sprites.SeedCabbageIcon;
            seedCabbage.sellPrice = 8;
            seedCabbage.cropToPlant = cropCabbage;
            EditorUtility.SetDirty(seedCabbage);

            var itemCabbage = GetOrCreateAsset<ItemData>($"{DataDir}/item_cabbage.asset");
            itemCabbage.id = "item_cabbage";
            itemCabbage.itemName = "白菜";
            itemCabbage.description = "炖汤最宜。";
            itemCabbage.type = ItemType.Resource;
            itemCabbage.icon = sprites.ItemCabbageIcon;
            itemCabbage.sellPrice = 35;
            EditorUtility.SetDirty(itemCabbage);

            cropRadish.harvestItem = itemRadish;
            cropCabbage.harvestItem = itemCabbage;
            EditorUtility.SetDirty(cropRadish);
            EditorUtility.SetDirty(cropCabbage);

            return new DataSet
            {
                CropRadish = cropRadish,
                CropCabbage = cropCabbage,
                SeedRadish = seedRadish,
                ItemRadish = itemRadish,
                SeedCabbage = seedCabbage,
                ItemCabbage = itemCabbage,
                PlayerSprite = sprites.PlayerSprite,
                NpcSprite = sprites.NpcSprite,
                BinSprite = sprites.BinSprite
            };
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------------- SerializedObject 工具 ----------------

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning($"[SceneBuilder] 字段未找到: {target.GetType().Name}.{field}");
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.stringValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).stringValue = values[i];
                }
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
                for (int i = 0; i < values.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetGradient(TimeManager timeManager)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.00f),  // 0:00 夜
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.23f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.26f),      // 6:00 晨
                    new GradientColorKey(Color.white, 0.33f),
                    new GradientColorKey(Color.white, 0.70f),
                    new GradientColorKey(new Color(1f, 0.65f, 0.45f), 0.79f),   // 19:00 黄昏
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 0.87f),
                    new GradientColorKey(new Color(0.35f, 0.4f, 0.65f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            var so = new SerializedObject(timeManager);
            var prop = so.FindProperty("_dayNightGradient");
            if (prop != null)
            {
                prop.gradientValue = gradient;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[SceneBuilder] TagManager.asset 未找到，请手动创建 Layer: " + layerName);
                return;
            }

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
            Debug.LogWarning("[SceneBuilder] 没有空余 Layer 槽位");
        }

        // ---------------- 数据容器 ----------------

        private class SpriteSet
        {
            public Sprite Ground, Tilled, Watered;
            public Sprite PlayerSprite, NpcSprite, BinSprite;
            public Sprite[] RadishStages, CabbageStages;
            public Sprite SeedRadishIcon, ItemRadishIcon, SeedCabbageIcon, ItemCabbageIcon;
        }

        private class TileSet
        {
            public Tile Ground, Tilled, Watered;
        }

        private class DataSet
        {
            public CropData CropRadish, CropCabbage;
            public ItemData SeedRadish, ItemRadish, SeedCabbage, ItemCabbage;
            public Sprite PlayerSprite, NpcSprite, BinSprite;
        }
    }
}
