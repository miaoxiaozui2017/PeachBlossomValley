using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 笼箱系统（单例）：管理笼箱的布置状态与跨日收获水产。
    /// CageSpot 只是交互入口，状态统一存在这里，便于存档。
    /// </summary>
    public class CageSystem : MonoBehaviour
    {
        public static CageSystem Instance { get; private set; }

        public bool IsBaited { get; private set; }
        public int BaitedDays { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private static readonly string[] BaitIds = { "item_poison_bait", "item_wild_veg", "item_mushroom", "item_rice" };

        /// <summary>放笼箱：消耗 1 个饵料</summary>
        public bool SetCage()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return false;

            ItemData used = null;
            foreach (var id in BaitIds)
            {
                var it = Find(id);
                if (it != null && inv.CountItem(it) >= 1) { used = it; break; }
            }
            if (used == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("放笼箱需要「毒饵 / 野菜 / 蘑菇 / 水稻」做饵料");
                return false;
            }

            inv.RemoveItem(used, 1);
            IsBaited = true;
            BaitedDays = 1;
            PBV.UI.HUD.Instance?.ShowNotice($"笼箱已放入水域（{used.itemName} 做饵）· 次日来收水产");
            Debug.Log("[Cage] 笼箱已布");
            return true;
        }

        private void OnNewDay()
        {
            if (!IsBaited) return;
            BaitedDays++;
            if (BaitedDays >= 2) Harvest();
        }

        private void Harvest()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            float r = Random.value;
            string id =
                r < 0.35f ? "item_shell_shrimp" :
                r < 0.60f ? "item_shell_clam" :
                r < 0.80f ? "item_shell_snail" :
                r < 0.93f ? "item_shell_crab" :
                "item_shell_eel";
            var shell = Find(id);
            if (shell != null && inv.AddItem(shell, 1))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"【笼箱】收到 {shell.itemName}！可做海鲜菜", 4f);
                SkillSystem.Instance?.AddExp(SkillType.Fishing, 12);
                IsBaited = false;
                BaitedDays = 0;
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("笼箱有收获，但背包满了（明早再收）", 3f);
            }
            Debug.Log($"[Cage] 收水产 {id}");
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        // 存档
        public int Export() => IsBaited ? BaitedDays : 0;
        public void Restore(int days)
        {
            IsBaited = days > 0;
            BaitedDays = Mathf.Max(0, days);
        }
    }
}
