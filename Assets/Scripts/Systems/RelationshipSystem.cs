using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBV.Systems
{
    /// <summary>好感度等级</summary>
    public enum AffectionLevel
    {
        Stranger,   // 0-9  陌生
        Acquainted, // 10-29 认识
        Friendly,   // 30-59 友好
        Close,      // 60-89 亲密
        Soulmate    // 90-100 挚友
    }

    /// <summary>
    /// 好感度系统：管理所有 NPC 的好感值，送礼增加好感。
    /// 单例，存档支持。
    /// </summary>
    public class RelationshipSystem : MonoBehaviour
    {
        public static RelationshipSystem Instance { get; private set; }

        public event Action<string, int, AffectionLevel> OnAffectionChanged; // npcName, value, level

        private readonly Dictionary<string, int> _affections = new Dictionary<string, int>();

        // 好感等级阈值
        private static readonly int[] LevelThresholds = { 0, 10, 30, 60, 90 };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>获取好感值（不存在返回0）</summary>
        public int GetAffection(string npcName)
        {
            return _affections.TryGetValue(npcName, out int v) ? v : 0;
        }

        /// <summary>获取好感等级</summary>
        public AffectionLevel GetLevel(string npcName)
        {
            return (AffectionLevel)ValueToLevel(GetAffection(npcName));
        }

        /// <summary>增加好感（可为负），返回最终值</summary>
        public int AddAffection(string npcName, int delta)
        {
            if (!_affections.ContainsKey(npcName)) _affections[npcName] = 0;
            int oldLevel = ValueToLevel(_affections[npcName]);
            _affections[npcName] = Mathf.Clamp(_affections[npcName] + delta, 0, 100);
            int newLevel = ValueToLevel(_affections[npcName]);
            OnAffectionChanged?.Invoke(npcName, _affections[npcName], (AffectionLevel)newLevel);
            if (oldLevel != newLevel)
            {
                Debug.Log($"[Relationship] {npcName} 好感度等级提升: {(AffectionLevel)oldLevel} -> {(AffectionLevel)newLevel}");
                GrantAffinityGift(npcName, oldLevel, newLevel);
            }
            return _affections[npcName];
        }

        /// <summary>送礼：根据物品 affectionValue 增加好感</summary>
        public bool TryGift(string npcName, PBV.Inventory.ItemData item)
        {
            if (item == null) return false;
            float festMult = FestivalSystem.Instance != null ? FestivalSystem.Instance.GiftMultiplier : 1f;
            int gain = Mathf.Max(1, Mathf.RoundToInt(item.affectionValue * festMult));
            int before = GetAffection(npcName);
            int after = AddAffection(npcName, gain);
            QuestManager.Instance?.RecordGift();
            PBV.UI.HUD.Instance?.ShowNotice(
                $"送 {npcName} {item.itemName} · 好感 +{gain}（{before}→{after} · {LevelToName(GetLevel(npcName))}）");
            Debug.Log($"[Relationship] 送 {npcName} {item.itemName} +{gain} 好感 ({before}->{after})");
            return true;
        }

        private static int ValueToLevel(int value)
        {
            for (int i = LevelThresholds.Length - 1; i >= 0; i--)
            {
                if (value >= LevelThresholds[i]) return i;
            }
            return 0;
        }

        /// <summary>好感度跨过「亲密 / 挚友」门槛时，NPC 赠予文玩字画（可带去现代卖高价）。</summary>
        private void GrantAffinityGift(string npcName, int oldLevel, int newLevel)
        {
            string giftId = null, label = null;
            if (oldLevel < 3 && newLevel >= 3) { giftId = "item_art_scroll"; label = "亲密馈赠·名家字画"; }
            else if (oldLevel < 4 && newLevel >= 4) { giftId = "item_art_curio"; label = "挚友馈赠·古玉文玩"; }
            if (giftId == null) return;

            var inv = PBV.Inventory.InventorySystem.Instance;
            if (inv == null) return;
            PBV.Inventory.ItemData item = null;
            foreach (var it in Resources.LoadAll<PBV.Inventory.ItemData>("Data"))
                if (it != null && it.id == giftId) { item = it; break; }
            if (item == null) return;

            if (inv.AddItem(item, 1))
                PBV.UI.HUD.Instance?.ShowNotice($"【{label}】{npcName} 赠你 {item.itemName}！可带去现代卖高价", 4f);
            else
                PBV.UI.HUD.Instance?.ShowNotice($"背包已满，{npcName} 的 {item.itemName} 未能领取", 4f);
            Debug.Log($"[Relationship] {npcName} 赠 {item.itemName}");
        }

        public static string LevelToName(AffectionLevel level)
        {
            switch (level)
            {
                case AffectionLevel.Stranger: return "陌生";
                case AffectionLevel.Acquainted: return "认识";
                case AffectionLevel.Friendly: return "友好";
                case AffectionLevel.Close: return "亲密";
                case AffectionLevel.Soulmate: return "挚友";
                default: return "陌生";
            }
        }

        // ===== 存档支持 =====
        public List<AffectionDTO> ExportAll()
        {
            var list = new List<AffectionDTO>();
            foreach (var kvp in _affections)
                list.Add(new AffectionDTO { npcName = kvp.Key, value = kvp.Value });
            return list;
        }

        public void ImportAll(List<AffectionDTO> data)
        {
            _affections.Clear();
            if (data == null) return;
            foreach (var d in data)
                if (!string.IsNullOrEmpty(d.npcName))
                    _affections[d.npcName] = Mathf.Clamp(d.value, 0, 100);
        }
    }

    [Serializable]
    public class AffectionDTO
    {
        public string npcName;
        public int value;
    }
}
