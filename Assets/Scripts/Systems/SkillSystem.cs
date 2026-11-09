using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBV.Systems
{
    public enum SkillType { Farming, Mining, Fishing, Cooking, Lumberjack }

    /// <summary>
    /// 技能系统：种植/采矿/钓鱼/烹饪四个技能，各10级。
    /// 各系统使用时上报经验，升级提供加成。
    /// 存档支持。
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        public static SkillSystem Instance { get; private set; }

        public event Action<SkillType, int> OnSkillLevelUp; // skill, newLevel

        private readonly Dictionary<SkillType, int> _levels = new Dictionary<SkillType, int>();
        private readonly Dictionary<SkillType, int> _exps = new Dictionary<SkillType, int>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            foreach (SkillType s in Enum.GetValues(typeof(SkillType)))
            {
                _levels[s] = 1;
                _exps[s] = 0;
            }
        }

        private int ExpToNext(int level) => level * 100;

        public int GetLevel(SkillType skill) => _levels.TryGetValue(skill, out int l) ? l : 1;
        public int GetExp(SkillType skill) => _exps.TryGetValue(skill, out int e) ? e : 0;
        public int GetExpToNext(SkillType skill) => ExpToNext(GetLevel(skill));

        public void AddExp(SkillType skill, int exp)
        {
            if (!_levels.ContainsKey(skill)) return;
            _exps[skill] += exp;
            while (_exps[skill] >= ExpToNext(_levels[skill]) && _levels[skill] < 10)
            {
                _exps[skill] -= ExpToNext(_levels[skill]);
                _levels[skill]++;
                OnSkillLevelUp?.Invoke(skill, _levels[skill]);
                Debug.Log($"[Skill] {skill} 升级到 Lv.{_levels[skill]}");
            }
            if (_levels[skill] >= 10) _exps[skill] = 0; // 满级
        }

        // ===== 加成查询 =====
        /// <summary>种植等级>=3时收获产量+1</summary>
        public int FarmingYieldBonus => GetLevel(SkillType.Farming) >= 3 ? 1 : 0;
        /// <summary>钓鱼等级>=3时稀有鱼概率+20%</summary>
        public float FishingRareBonus => GetLevel(SkillType.Fishing) >= 3 ? 0.2f : 0f;
        /// <summary>烹饪等级>=3时体力恢复+20%</summary>
        public float CookingBonus => GetLevel(SkillType.Cooking) >= 3 ? 0.2f : 0f;
        /// <summary>采矿等级>=3时矿石产量+1</summary>
        public int MiningYieldBonus => GetLevel(SkillType.Mining) >= 3 ? 1 : 0;
        /// <summary>伐木采摘等级>=3时砍树额外+1木材</summary>
        public int LumberjackYieldBonus => GetLevel(SkillType.Lumberjack) >= 3 ? 1 : 0;

        // ===== 存档 =====
        public List<SkillDTO> ExportAll()
        {
            var list = new List<SkillDTO>();
            foreach (SkillType s in Enum.GetValues(typeof(SkillType)))
                list.Add(new SkillDTO { skill = (int)s, level = _levels[s], exp = _exps[s] });
            return list;
        }

        public void ImportAll(List<SkillDTO> data)
        {
            foreach (SkillType s in Enum.GetValues(typeof(SkillType)))
            {
                _levels[s] = 1;
                _exps[s] = 0;
            }
            if (data == null) return;
            foreach (var d in data)
            {
                var skill = (SkillType)d.skill;
                _levels[skill] = Mathf.Clamp(d.level, 1, 10);
                _exps[skill] = Mathf.Max(0, d.exp);
            }
        }

        public static string SkillName(SkillType s)
        {
            switch (s)
            {
                case SkillType.Farming: return "种植";
                case SkillType.Mining: return "采矿";
                case SkillType.Fishing: return "钓鱼";
                case SkillType.Cooking: return "烹饪";
                case SkillType.Lumberjack: return "伐木采摘";
                default: return "";
            }
        }
    }

    [Serializable]
    public class SkillDTO
    {
        public int skill;
        public int level;
        public int exp;
    }
}
