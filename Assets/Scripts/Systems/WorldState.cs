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

        public event Action<WorldType> OnWorldChanged;

        /// <summary>通行费：出发世界币种固定额。出谷 50 文 / 回谷 ¥5（按汇率等值）</summary>
        public const int VALLEY_FEE = 50;
        public const int MODERN_FEE = 5;

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
                else PBV.UI.HUD.Instance?.ShowNotice($"去现代：传送门 E 或按 X 打开驿站（通行费 {VALLEY_FEE} 文，回程 ¥{MODERN_FEE}）");
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

            // 通行费：按「出发世界」的币种扣固定额，不够则拒绝穿越
            bool leavingValley = Current == WorldType.Ancient;
            int fee = leavingValley ? VALLEY_FEE : MODERN_FEE;
            string feeText = leavingValley ? $"{fee} 文" : $"¥{fee}";
            var wallet = PlayerWallet.Instance;
            if (wallet != null)
            {
                bool paid = leavingValley ? wallet.SpendSong(fee) : wallet.SpendRMB(fee);
                if (!paid)
                {
                    PBV.UI.HUD.Instance?.ShowNotice($"通行费不足：需要 {feeText}（可在驿站随时兑换货币）");
                    return;
                }
            }

            Current = world;
            LastChangeWasRestore = false;             // 真穿越：允许做出生点传送
            OnWorldChanged?.Invoke(Current);          // 先切活动背包 / 显隐商店与场景内容
            TradeCarrySystem.Instance?.Settle(Current); // 再把 3 槽货物实物放进对岸背包
            QuestManager.Instance?.RecordTrade();
            PBV.UI.HUD.Instance?.ShowNotice($"已付通行费 {feeText}");
            string tip = Current == WorldType.Modern
                ? "已到【现代】：只剩现代杂货铺 / 古玩店 / 驿站 · 背包与谷内互不通用 · 按 T 回桃花谷"
                : "已回到【桃花谷】：农田与全部建筑恢复 · 想去现代到传送门按 E";
            PBV.UI.HUD.Instance?.ShowNotice(tip, 3.5f);
            Debug.Log($"[World] {(Current == WorldType.Ancient ? "古代·桃花谷" : "现代·现实世界")}（已付通行费）");
        }

        /// <summary>存档恢复：也要通知 UI 刷新横幅</summary>
        public void Restore(int world)
        {
            Current = (WorldType)Mathf.Clamp(world, 0, 1);
            LastChangeWasRestore = true;              // 读档不应把玩家传送到出生点
            OnWorldChanged?.Invoke(Current);
        }

        /// <summary>上一次世界变化是否来自读档（供 WorldSceneBootstrap 跳过出生点传送）</summary>
        public bool LastChangeWasRestore { get; private set; }

        public string WorldName => Current == WorldType.Ancient ? "桃花谷" : "现代";
    }
}
