using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 陷阱系统：用毒饵/迷药下陷阱，次日随机捕获野味（野兔/野鸡/野鹿）。
    /// 每个陷阱独立计时，跨日结算。存档支持（简化：只存已布置天数）。
    /// </summary>
    public class TrapSystem : MonoBehaviour
    {
        public static TrapSystem Instance { get; private set; }

        /// <summary>当前陷阱是否已布饵（等待捕获）</summary>
        public bool IsBaited { get; private set; }
        /// <summary>已布饵的天数（第 1 天布，第 2 天起有概率捕获）</summary>
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

        /// <summary>布置陷阱：消耗 1 个毒饵（或迷药），次日开始有概率捕获</summary>
        public bool SetTrap()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return false;

            var bait = Find("item_poison_bait");
            var powder = Find("item_poison_powder");
            ItemData used = null;
            if (bait != null && inv.CountItem(bait) >= 1) used = bait;
            else if (powder != null && inv.CountItem(powder) >= 1) used = powder;

            if (used == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("布置陷阱需要「毒饵」或「迷药」（药庐可制作）");
                return false;
            }

            inv.RemoveItem(used, 1);
            IsBaited = true;
            BaitedDays = 1;
            PBV.UI.HUD.Instance?.ShowNotice($"陷阱已布置（{used.itemName}）· 次日查看捕获");
            Debug.Log("[Trap] 陷阱已布置");
            return true;
        }

        private void OnNewDay()
        {
            if (!IsBaited) return;
            BaitedDays++;

            // 第 2 天起每天 40% 概率捕获
            if (BaitedDays >= 2 && Random.value < 0.4f)
            {
                Catch();
            }
        }

        private void Catch()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            float r = Random.value;
            string id = r < 0.5f ? "item_game_hare" : r < 0.8f ? "item_game_pheasant" : "item_game_deer";
            var game = Find(id);
            if (game != null && inv.AddItem(game, 1))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"【陷阱】捕获到 {game.itemName}！", 4f);
                IsBaited = false;
                BaitedDays = 0;
            }
            else
            {
                PBV.UI.HUD.Instance?.ShowNotice("陷阱捕获到野味，但背包满了（明早再收）", 3f);
            }
            Debug.Log($"[Trap] 捕获 {id}");
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
