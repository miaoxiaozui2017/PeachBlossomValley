using UnityEngine;
using PBV.Core;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 砍树/采摘点：需手持「斧头」工具才能砍树得木材；
    /// 未持斧头则只可采摘浆果（野果/蘑菇/野菜）。
    /// SpotIndex 0 = 果林（浆果多），1 = 密林（木材多）。
    /// 每天 5 次，跨日恢复。伐木得「伐木采摘」经验。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TreeSpot : MonoBehaviour, IInteractable
    {
        public int SpotIndex;

        private const int UsesPerDay = 5;
        private int _usesLeft = UsesPerDay;

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay() => _usesLeft = UsesPerDay;

        public string GetInteractPrompt()
            => _usesLeft > 0
                ? $"砍树得木材 / 采浆果（今日剩 {_usesLeft} 次，需斧头）(E)"
                : "今日已砍尽，明日再来 (E)";

        public void OnInteract()
        {
            if (_usesLeft <= 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice("这处今日已砍尽 · 睡一觉明天再来");
                return;
            }

            var inv = InventorySystem.Instance;
            if (inv == null) return;

            bool hasAxe = ToolManager.Instance != null && ToolManager.Instance.CurrentTool == ToolType.Axe;

            if (hasAxe)
            {
                // 持斧砍树：得木材（+ 技能加成），偶尔得树枝（树皮）
                int count = 1 + (SkillSystem.Instance != null ? SkillSystem.Instance.LumberjackYieldBonus : 0);
                var wood = Find("item_wood");
                if (wood == null || !inv.AddItem(wood, count))
                {
                    PBV.UI.HUD.Instance?.ShowNotice("背包已满，砍下的木材放不下");
                    return;
                }
                _usesLeft--;
                SkillSystem.Instance?.AddExp(SkillType.Lumberjack, 6);
                PBV.UI.HUD.Instance?.ShowNotice($"挥斧砍树：木材 ×{count}（今日剩 {_usesLeft} 次）");
                Debug.Log($"[Tree] 砍树得木材 x{count}，剩 {_usesLeft} 次");
                return;
            }

            // 未持斧：只能采摘浆果
            string[] berryPool = SpotIndex == 0
                ? new[] { "item_wild_fruit", "item_wild_fruit", "item_mushroom", "item_wild_veg" }
                : new[] { "item_wild_fruit", "item_mushroom", "item_wild_veg", "item_bark" };
            var berry = Find(berryPool[Random.Range(0, berryPool.Length)]);
            if (berry == null || !inv.AddItem(berry, 1))
            {
                PBV.UI.HUD.Instance?.ShowNotice("背包已满，采下的浆果放不下");
                return;
            }
            _usesLeft--;
            SkillSystem.Instance?.AddExp(SkillType.Lumberjack, 3);
            PBV.UI.HUD.Instance?.ShowNotice($"采摘：{berry.itemName} ×1（今日剩 {_usesLeft} 次）· 换斧头可砍树得木材");
            Debug.Log($"[Tree] 采浆果 {berry.itemName}，剩 {_usesLeft} 次");
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
