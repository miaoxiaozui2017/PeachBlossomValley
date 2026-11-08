using UnityEngine;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 建造系统：花钱扩建背包容量(24→36→48)和体力上限(100→150→200)。
    /// 存档支持。
    /// </summary>
    public class BuildSystem : MonoBehaviour
    {
        public static BuildSystem Instance { get; private set; }

        public int BagLevel { get; private set; } = 0;       // 0=24格, 1=36格, 2=48格
        public int StaminaLevel { get; private set; } = 0;    // 0=100, 1=150, 2=200

        private static readonly int[] BagCapacities = { 24, 36, 48 };
        private static readonly int[] BagCosts = { 500, 1000 };
        private static readonly int[] StaminaMaxes = { 100, 150, 200 };
        private static readonly int[] StaminaCosts = { 800, 1500 };

        public int CurrentBagCapacity => BagCapacities[BagLevel];
        public int CurrentMaxStamina => StaminaMaxes[StaminaLevel];
        public int BagUpgradeCost => BagLevel < 2 ? BagCosts[BagLevel] : -1;
        public int StaminaUpgradeCost => StaminaLevel < 2 ? StaminaCosts[StaminaLevel] : -1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public bool TryUpgradeBag()
        {
            if (BagLevel >= 2) return false;
            int cost = BagCosts[BagLevel];
            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(cost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"背包扩容需 ¥{cost}，金钱不足");
                return false;
            }
            BagLevel++;
            InventorySystem.Instance?.ExpandCapacity(BagCapacities[BagLevel]);
            PBV.UI.HUD.Instance?.ShowNotice($"背包扩容到 {BagCapacities[BagLevel]} 格 · 按 Tab 查看新增格子");
            Debug.Log($"[Build] 背包扩容到 {BagCapacities[BagLevel]} 格");
            return true;
        }

        public bool TryUpgradeStamina()
        {
            if (StaminaLevel >= 2) return false;
            int cost = StaminaCosts[StaminaLevel];
            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpend(cost))
            {
                PBV.UI.HUD.Instance?.ShowNotice($"体力上限扩建需 ¥{cost}，金钱不足");
                return false;
            }
            StaminaLevel++;
            PlayerStats.Instance?.ExpandMaxStamina(StaminaMaxes[StaminaLevel]);
            PBV.UI.HUD.Instance?.ShowNotice($"体力上限提升到 {StaminaMaxes[StaminaLevel]}");
            Debug.Log($"[Build] 体力上限提升到 {StaminaMaxes[StaminaLevel]}");
            return true;
        }

        // 存档
        public (int, int) Export() => (BagLevel, StaminaLevel);
        public void Import(int bag, int stamina)
        {
            BagLevel = Mathf.Clamp(bag, 0, 2);
            StaminaLevel = Mathf.Clamp(stamina, 0, 2);
            // 立即应用到系统
            InventorySystem.Instance?.ExpandCapacity(BagCapacities[BagLevel]);
            PlayerStats.Instance?.ExpandMaxStamina(StaminaMaxes[StaminaLevel]);
        }
    }
}
