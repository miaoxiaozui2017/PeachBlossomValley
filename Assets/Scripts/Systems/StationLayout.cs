using UnityEngine;
using UnityEngine.UI;

namespace PBV.Systems
{
    /// <summary>
    /// 测试期交互点布局：全部集中到地图北侧两排，每 4 格一个，头顶挂「名字 + 快捷键」标签。
    /// SceneBuilder（重建场景）与 StationLayoutBootstrap（运行时）共用这一张表，保证一致。
    /// 地形/地图生成本阶段不动，留到下一阶段。
    /// </summary>
    public static class StationLayoutData
    {
        // (场景物体名, 头顶标签；NPC 自带好感名牌所以留空, x, y)
        public static readonly (string goName, string label, float x, float y)[] Layout =
        {
            // 第 1 排 y = 9：生产 / 交易 / 采集
            ("ShippingBin",        "出货箱 E",    -12f, 9f),
            ("NPC_ShopKeeper",     "杂货铺 E",     -8f, 9f),
            ("CraftingStation",    "造纸台 C/E",   -4f, 9f),
            ("CookingStation",     "厨房 K/E",      0f, 9f),
            ("FishingSpot",        "溪钓点 E",     4f, 9f),
            ("MineEntrance",       "矿洞 M/E",      8f, 9f),
            ("ToolUpgradeStation", "铁匠铺 U/E",   12f, 9f),
            // 第 2 排 y = 6：生活 / 养成 / 老 NPC
            ("Bed",                "床 E",        -12f, 6f),
            ("BuildStation",       "建造台 B/E",   -8f, 6f),
            ("WorldPortal",        "传送门 E",     -4f, 6f),
            ("PetAdoptionSpot",    "灵兽祠 E",      0f, 6f),
            ("NPC_LaoNong",        null,            4f, 6f),
            ("NPC_YaoPo",          null,            8f, 6f),
            ("NPC_ShuSheng",       null,           12f, 6f),
            // 第 3 排 y = 3：v0.3.0 内容扩展（运行时自动补建，无需重建场景）
            ("DyeingStation",      "染坊 C/E",   -12f, 3f),
            ("FishingSpot_River",  "河钓点 E",    -8f, 3f),
            ("FishingSpot_Lake",   "湖钓点 E",    -4f, 3f),
            ("NPC_RanFang",        null,           0f, 3f),
            ("NPC_TieJiangNiang",  null,           4f, 3f),
            ("NPC_DaoShi",         null,           8f, 3f),
            ("NPC_TaoGong",        null,          12f, 3f),
            // 第 4 排 y = 0：非遗产业链 + 采集（金银细工 / 陶坊 / 药庐 / 木工坊 / 野地采集）
            ("JewelryStation",      "金银细工台 E", -12f, 0f),
            ("PotteryStation",      "陶坊 C/E",      -8f, 0f),
            ("MedicineStation",     "药庐 C/E",      -4f, 0f),
            ("CarpentryStation",    "木工坊 C/E",     0f, 0f),
            ("ForageSpot_Wild",     "野地采集 E",     4f, 0f),
            ("ForageSpot_Forest",   "林间采集 E",     8f, 0f),
            // 第 5 排 y = -3：采山 / 陷阱 / 衣柜
            ("MountainEntrance",    "采山入口 E",   -12f, -3f),
            ("TrapSpot",            "布陷阱 E",      -8f, -3f),
            ("Wardrobe",            "衣柜 E",        -4f, -3f),
        };

        public static Vector3 GetPosition(string goName)
        {
            foreach (var e in Layout)
                if (e.goName == goName) return new Vector3(e.x, e.y, 0f);
            return Vector3.zero;
        }

        public static string GetLabel(string goName)
        {
            foreach (var e in Layout)
                if (e.goName == goName) return e.label;
            return null;
        }

        /// <summary>摆到表里的位置并补名牌（幂等，可反复调用）</summary>
        public static void Place(GameObject go)
        {
            if (go == null) return;
            go.transform.position = GetPosition(go.name);
            var label = GetLabel(go.name);
            if (!string.IsNullOrEmpty(label)) EnsureLabel(go, label);
        }

        /// <summary>头顶名牌：世界空间 Canvas + 半透明底 + 亮字，与 NPC 名牌同一套做法</summary>
        public static void EnsureLabel(GameObject go, string text)
        {
            if (go == null || go.transform.Find("StationLabel") != null) return;

            var tagGo = new GameObject("StationLabel");
            tagGo.transform.SetParent(go.transform, false);
            tagGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            var canvas = tagGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 44f);
            rect.localScale = new Vector3(0.012f, 0.012f, 0.012f);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(tagGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.04f, 0.72f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(tagGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2); textRect.offsetMax = new Vector2(-4, -2);
            var t = textGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.93f, 0.68f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
        }
    }

    /// <summary>
    /// 运行时重排 + 补名牌。由 UIEventSystemBootstrap.Ensure() 自动补挂，
    /// 所以不重建场景也能立刻看到新布局和名字标签。
    /// </summary>
    public class StationLayoutBootstrap : MonoBehaviour
    {
        private void Start()
        {
            int count = 0;
            foreach (var e in StationLayoutData.Layout)
            {
                var go = GameObject.Find(e.goName);
                if (go == null) continue;
                StationLayoutData.Place(go);
                count++;
            }
            Debug.Log($"[StationLayout] 已重排 {count}/{StationLayoutData.Layout.Length} 个交互点（北侧两排，间隔 4 格）");
        }
    }
}
