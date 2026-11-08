using System;
using UnityEngine;

namespace PBV.Systems
{
    public enum WorldType { Ancient, Modern }

    /// <summary>
    /// 世界状态：古代桃花谷 ⇄ 现代世界。
    /// 说明：灰盒只有一张农场地图，切换世界不换场景、不移动玩家 ——
    /// 农场、商店、制作、任务等全部功能照常可用，唯一区别是出货售价。
    /// 去现代：走到北侧功能台区的传送门按 E；回来：按 T，或对传送门再按一次 E。
    /// </summary>
    public class WorldState : MonoBehaviour
    {
        public static WorldState Instance { get; private set; }

        public WorldType Current { get; private set; } = WorldType.Ancient;
        public float SellMultiplier => Current == WorldType.Modern ? 2f : 1f;

        public event Action<WorldType> OnWorldChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Update()
        {
            // T：一键回桃花谷（在桃花谷时只提示去现代的方式，不让"去现代"变成白嫖按键）
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                if (Current == WorldType.Modern) ReturnToAncient();
                else PBV.UI.HUD.Instance?.ShowNotice("去现代：到北侧功能台区的传送门按 E（现代出货 ×2）");
            }
        }

        public void Toggle()
        {
            SetWorld(Current == WorldType.Ancient ? WorldType.Modern : WorldType.Ancient);
        }

        public void ReturnToAncient() => SetWorld(WorldType.Ancient);

        public void SetWorld(WorldType world)
        {
            if (Current == world) return;
            Current = world;
            OnWorldChanged?.Invoke(Current);
            TradeCarrySystem.Instance?.Settle(Current); // 三出三进：穿越即刻结算携带货物
            QuestManager.Instance?.RecordTrade();
            string tip = Current == WorldType.Modern
                ? "已到【现代】：同一片农场，所有功能照常可用，出货 ×2 · 按 T 回桃花谷"
                : "已回到【桃花谷】：出货恢复 ×1 · 想去现代到下方传送门按 E";
            PBV.UI.HUD.Instance?.ShowNotice(tip, 3.5f);
            Debug.Log($"[World] {(Current == WorldType.Ancient ? "古代·桃花谷" : "现代·现实世界")}（售价x{SellMultiplier}）");
        }

        /// <summary>存档恢复：也要通知 UI 刷新横幅</summary>
        public void Restore(int world)
        {
            Current = (WorldType)Mathf.Clamp(world, 0, 1);
            OnWorldChanged?.Invoke(Current);
        }

        public string WorldName => Current == WorldType.Ancient ? "桃花谷" : "现代";
    }
}
