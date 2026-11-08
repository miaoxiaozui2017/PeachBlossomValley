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
            var shop = FindObjectOfType<ShopSystem>();
            var indigo = FindItem("item_indigo");
            if (shop != null && indigo != null) shop.AddShopEntry(indigo, 24);

            Debug.Log($"[ContentExpansion] 补建 {created} 个交互点（共 {ContentExpansionData.Extra.Length} 个扩展点），蓝草已上架");
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
    }
}
