using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBV.Systems
{
    public enum QuestStatus { Locked, Active, Completed, Rewarded }

    [Serializable]
    public class Quest
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int targetCount;
        public int rewardMoney;
        public string rewardItemId; // 可选奖励物品 id
        public int rewardItemCount;
        public QuestStatus status;
        public int currentCount;

        public bool IsFinished => status == QuestStatus.Completed || status == QuestStatus.Rewarded;
    }

    /// <summary>
    /// 任务系统：5个引导任务自动激活、自动追踪进度。
    /// 各系统通过 RecordXxx() 上报进度。
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public event Action OnQuestsChanged;

        private readonly List<Quest> _quests = new List<Quest>();

        public IReadOnlyList<Quest> Quests => _quests;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            InitQuests();
        }

        private void InitQuests()
        {
            _quests.Add(new Quest { id = "q_till", title = "开垦土地", description = "用锄头开垦 3 块土地。", targetCount = 3, rewardMoney = 50 });
            _quests.Add(new Quest { id = "q_plant", title = "播种希望", description = "在已开垦的土地上播种 3 颗种子。", targetCount = 3, rewardMoney = 50 });
            _quests.Add(new Quest { id = "q_harvest", title = "初次收获", description = "收获 1 个成熟作物。", targetCount = 1, rewardMoney = 80 });
            _quests.Add(new Quest { id = "q_ship", title = "第一桶金", description = "往出货箱投入 1 件可出售物品。", targetCount = 1, rewardMoney = 30 });
            _quests.Add(new Quest { id = "q_gift", title = "礼尚往来", description = "给任意 NPC 送 1 次礼。", targetCount = 1, rewardMoney = 60 });
            // 第一个任务激活
            ActivateNext();
        }

        /// <summary>激活下一个未开始的任务（前一个完成后）</summary>
        private void ActivateNext()
        {
            foreach (var q in _quests)
            {
                if (q.status == QuestStatus.Locked)
                {
                    q.status = QuestStatus.Active;
                    Debug.Log($"[Quest] 新任务: {q.title}");
                    OnQuestsChanged?.Invoke();
                    return;
                }
            }
        }

        private void UpdateCount(string id, int delta)
        {
            var q = _quests.Find(x => x.id == id);
            if (q == null || q.status != QuestStatus.Active) return;
            q.currentCount = Math.Min(q.targetCount, q.currentCount + delta);
            if (q.currentCount >= q.targetCount)
            {
                q.status = QuestStatus.Completed;
                Debug.Log($"[Quest] 完成: {q.title} (+¥{q.rewardMoney})");
                // 自动领奖
                ClaimReward(q);
                ActivateNext();
            }
            OnQuestsChanged?.Invoke();
        }

        private void ClaimReward(Quest q)
        {
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddMoney(q.rewardMoney);
            if (!string.IsNullOrEmpty(q.rewardItemId) && q.rewardItemCount > 0)
            {
                var item = FindItemById(q.rewardItemId);
                if (item != null && Inventory.InventorySystem.Instance != null)
                    Inventory.InventorySystem.Instance.AddItem(item, q.rewardItemCount);
            }
            q.status = QuestStatus.Rewarded;
        }

        private Inventory.ItemData FindItemById(string id)
        {
            var all = Resources.FindObjectsOfTypeAll<Inventory.ItemData>();
            foreach (var i in all) if (i.id == id) return i;
            return null;
        }

        // ===== 各系统上报进度的接口 =====
        public void RecordTill(int n = 1) => UpdateCount("q_till", n);
        public void RecordPlant(int n = 1) => UpdateCount("q_plant", n);
        public void RecordHarvest(int n = 1) => UpdateCount("q_harvest", n);
        public void RecordShip(int n = 1) => UpdateCount("q_ship", n);
        public void RecordGift(int n = 1) => UpdateCount("q_gift", n);

        // ===== 存档支持 =====
        public List<QuestDTO> ExportAll()
        {
            var list = new List<QuestDTO>();
            foreach (var q in _quests)
                list.Add(new QuestDTO { id = q.id, status = (int)q.status, currentCount = q.currentCount });
            return list;
        }

        public void ImportAll(List<QuestDTO> data)
        {
            if (data == null) return;
            foreach (var d in data)
            {
                var q = _quests.Find(x => x.id == d.id);
                if (q != null) { q.status = (QuestStatus)d.status; q.currentCount = d.currentCount; }
            }
            OnQuestsChanged?.Invoke();
        }
    }

    [Serializable]
    public class QuestDTO
    {
        public string id;
        public int status;
        public int currentCount;
    }
}
