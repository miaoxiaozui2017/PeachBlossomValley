using System;
using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>怪物定义（代码内置，灰盒阶段简化）</summary>
    [Serializable]
    public class MonsterDef
    {
        public string id;
        public string name;
        public int hp;
        public int attack;
        public int minFloor;   // 出现在该层及以上
        public string lootId;  // 击败掉落物 id
    }

    /// <summary>
    /// 战斗系统（单例）：矿洞挖矿概率遇怪，触发回合制简化战斗。
    /// 战斗掉血（PlayerStats.Hp），可用药品回血。战斗状态供 CombatUI 展示。
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        // 怪物表：矿工僵尸 / 岩蛇 / 蝙蝠（越深越强）
        private static readonly MonsterDef[] Monsters =
        {
            new MonsterDef { id = "m_zombie", name = "矿工僵尸", hp = 30, attack = 8, minFloor = 1, lootId = "item_ore_copper" },
            new MonsterDef { id = "m_bat", name = "岩洞蝙蝠", hp = 20, attack = 6, minFloor = 2, lootId = "item_ore_iron" },
            new MonsterDef { id = "m_snake", name = "岩蛇", hp = 45, attack = 12, minFloor = 4, lootId = "item_ore_gem" },
        };

        // 当前战斗状态
        public bool InCombat { get; private set; }
        public MonsterDef CurrentMonster { get; private set; }
        public int MonsterHp { get; private set; }

        public event Action OnCombatChanged; // 战斗开始/状态变化

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>按矿层深度随机一只怪物</summary>
        private static MonsterDef RollMonster(int floor)
        {
            var pool = new System.Collections.Generic.List<MonsterDef>();
            foreach (var m in Monsters) if (floor >= m.minFloor) pool.Add(m);
            if (pool.Count == 0) return Monsters[0];
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        /// <summary>尝试触发遭遇（矿洞挖矿后调用）。返回是否进入战斗。</summary>
        public bool TryEncounter(int floor)
        {
            if (InCombat) return false;
            // 越深越容易遇怪：1 层 12% → 10 层 ~30%
            float chance = Mathf.Clamp01(0.10f + floor * 0.02f);
            if (UnityEngine.Random.value >= chance) return false;

            CurrentMonster = RollMonster(floor);
            MonsterHp = CurrentMonster.hp;
            InCombat = true;
            OnCombatChanged?.Invoke();
            PBV.UI.HUD.Instance?.ShowNotice($"【遭遇】矿洞里窜出一只 {CurrentMonster.name}！", 3f);
            Debug.Log($"[Combat] 遭遇 {CurrentMonster.name}");
            return true;
        }

        /// <summary>玩家攻击：造成伤害，怪物反击。返回是否击杀。</summary>
        public bool PlayerAttack(out string log)
        {
            log = "";
            if (!InCombat || CurrentMonster == null) return false;
            var ps = PBV.Player.PlayerStats.Instance;
            int dmg = UnityEngine.Random.Range(10, 16);
            MonsterHp -= dmg;
            log = $"你攻击 {CurrentMonster.name}，造成 {dmg} 伤害";
            if (MonsterHp <= 0)
            {
                EndCombat(true, ref log);
                return true;
            }
            // 怪物反击
            if (ps != null)
            {
                int enemyDmg = CurrentMonster.attack;
                bool dead = ps.TakeDamage(enemyDmg);
                log += $"，{CurrentMonster.name} 反击造成 {enemyDmg} 伤害";
                if (dead)
                {
                    EndCombat(false, ref log);
                }
            }
            OnCombatChanged?.Invoke();
            return false;
        }

        /// <summary>逃跑：有概率成功，失败吃一次伤害</summary>
        public void Flee()
        {
            if (!InCombat) return;
            if (UnityEngine.Random.value < 0.6f)
            {
                PBV.UI.HUD.Instance?.ShowNotice("你成功逃离了战斗");
                InCombat = false;
                CurrentMonster = null;
                OnCombatChanged?.Invoke();
            }
            else
            {
                var ps = PBV.Player.PlayerStats.Instance;
                if (ps != null && !ps.TakeDamage(CurrentMonster.attack))
                    PBV.UI.HUD.Instance?.ShowNotice($"逃跑失败！{CurrentMonster.name} 造成 {CurrentMonster.attack} 伤害");
                else
                {
                    InCombat = false;
                    CurrentMonster = null;
                }
                OnCombatChanged?.Invoke();
            }
        }

        /// <summary>结束战斗（击杀=true 掉战利品；死亡=false 玩家 HP 归零）</summary>
        private void EndCombat(bool killed, ref string log)
        {
            InCombat = false;
            if (killed)
            {
                var inv = InventorySystem.Instance;
                var loot = Find(CurrentMonster.lootId);
                if (inv != null && loot != null && inv.AddItem(loot, 2))
                    log += $"\n击败 {CurrentMonster.name}！获得 {loot.itemName} ×2";
                PBV.Systems.SkillSystem.Instance?.AddExp(SkillType.Mining, 15);
            }
            CurrentMonster = null;
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }
    }
}
