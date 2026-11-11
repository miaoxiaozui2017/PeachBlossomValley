using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PBV.Systems;

namespace PBV.EditorTools
{
    /// <summary>
    /// 生成现代世界场景 `02_Modern.unity`（叠加场景，不替换桃花谷）。
    ///
    /// 为什么是叠加而不是替换：
    /// - 桃花谷场景永不卸载 → 农田/牲畜/储物箱/待结算货物等状态原地保留，零迁移风险。
    /// - 现代独占一个 .unity 文件 → 美术可以单独打开这个场景作画、单独迭代。
    ///
    /// 场景里只放「看得见的东西」：地面 + 两家店。逻辑组件（ShopSystem、货架、名牌）
    /// 仍由运行时 ShopWorldBootstrap 挂载，避免场景与代码双重配置漂移。
    ///
    /// 菜单：PBV/生成现代场景 (02_Modern)
    /// </summary>
    public static class ModernSceneBuilder
    {
        private const string ArtDir = "Assets/Art/Placeholder";
        private const string ScenePath = "Assets/Scenes/02_Modern.unity";
        private const string ValleyScenePath = "Assets/Scenes/01_FarmGreyBox.unity";
        private const string InteractableLayerName = "Interactable";

        private static readonly Color GroundColor = new Color(0.16f, 0.18f, 0.22f);
        private static readonly Color ModernShopColor = new Color(0.32f, 0.48f, 0.68f);
        private static readonly Color AntiqueShopColor = new Color(0.62f, 0.45f, 0.22f);

        [MenuItem("PBV/生成现代场景 (02_Modern)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh();
            EnsureLayer(InteractableLayerName);

            var groundSprite = CreateSolidSprite("tex_modern_ground", GroundColor, 32, 0.02f);
            var shopSprite = CreateSolidSprite("spr_shop_modern", ModernShopColor, 32, 0f);
            var antiqueSprite = CreateSolidSprite("spr_shop_antique", AntiqueShopColor, 32, 0f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject(WorldSceneBootstrap.ModernRootName);

            // 地面：一整块纯色，盖住桃花谷地块（谷内 Grid 在现代会被隐藏，双保险）
            var groundGo = new GameObject("ModernGround");
            groundGo.transform.SetParent(root.transform, false);
            groundGo.transform.position = new Vector3(2f, 0f, 0f);
            groundGo.transform.localScale = new Vector3(48f, 26f, 1f);
            var groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.sprite = groundSprite;
            groundSr.color = Color.white;
            groundSr.sortingOrder = -50;

            // 两家店：与 StationLayout 里的坐标一致（16,9）/（20,9）
            CreateShop(root.transform, "ModernShop", new Vector3(16f, 9f, 0f), shopSprite);
            CreateShop(root.transform, "AntiqueShop", new Vector3(20f, 9f, 0f), antiqueSprite);

            Directory.CreateDirectory("Assets/Scenes");
            bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterInBuildSettings();

            if (ok) Debug.Log($"[ModernScene] 已生成 {ScenePath}，并加入 Build Settings（叠加加载）");
            else Debug.LogError($"[ModernScene] 保存失败: {ScenePath}");

            // 回到桃花谷场景，避免用户停在空场景里
            if (File.Exists(ValleyScenePath)) EditorSceneManager.OpenScene(ValleyScenePath);
        }

        private static void CreateShop(Transform parent, string name, Vector3 pos, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 9;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.3f, 1.3f);
            col.offset = new Vector2(0f, 0.4f);

            int layer = LayerMask.NameToLayer(InteractableLayerName);
            if (layer >= 0) go.layer = layer;
        }

        /// <summary>生成纯色 PNG 并导入为 Sprite（32px，PPU 32，Point，无 mipmap）</summary>
        private static Sprite CreateSolidSprite(string name, Color color, int size, float noise)
        {
            Directory.CreateDirectory(ArtDir);
            string path = $"{ArtDir}/{name}.png";

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var rng = new System.Random(name.GetHashCode());
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float k = 1f + (float)(rng.NextDouble() - 0.5) * noise;
                    tex.SetPixel(x, y, new Color(color.r * k, color.g * k, color.b * k, 1f));
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void RegisterInBuildSettings()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == ValleyScenePath))
                list.Insert(0, new EditorBuildSettingsScene(ValleyScenePath, true));
            if (!list.Any(s => s.path == ScenePath))
                list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
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
                var prop = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(prop.stringValue))
                {
                    prop.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }
    }
}
