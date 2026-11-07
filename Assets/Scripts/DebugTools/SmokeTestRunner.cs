#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PBV.Core;
using PBV.Farming;
using PBV.Inventory;
using PBV.Systems;
using PBV.Save;
using PBV.Dialogue;
using PBV.Economy;
using PBV.UI;

namespace PBV.DebugTools
{
    /// <summary>
    /// 开发用冒烟测试 Runner（仅在编辑器内编译，不打进构建）
    /// 由 SceneSmokeTest 注入场景并在 Play 模式下自动验证核心循环
    /// </summary>
    public class SmokeTestRunner : MonoBehaviour
    {
        private readonly List<string> _passes = new List<string>();
        private readonly List<string> _failures = new List<string>();

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f); // 等所有 Awake/Start 完成

            try
            {
                RunChecks();
            }
            catch (System.Exception e)
            {
                _failures.Add("[FAIL] 异常中断: " + e);
            }

            WriteResult();

            yield return null;
            EditorApplication.Exit(_failures.Count == 0 ? 0 : 1);
        }

        private void RunChecks()
        {
            // 1. 系统装配
            Check("TimeManager 存在", TimeManager.Instance != null);
            Check("InventorySystem 存在", InventorySystem.Instance != null);
            Check("PlayerWallet 存在", PlayerWallet.Instance != null);
            Check("SaveManager 存在", SaveManager.Instance != null);
            Check("DialogueManager 存在", DialogueManager.Instance != null);
            Check("InputHandler 存在", InputHandler.Instance != null);
            Check("HUD 存在", Object.FindObjectOfType<HUD>() != null);
            Check("InventoryUI 存在", Object.FindObjectOfType<InventoryUI>() != null);

            var farmGrid = Object.FindObjectOfType<FarmGrid>();
            var cropManager = Object.FindObjectOfType<CropManager>();
            Check("FarmGrid 存在", farmGrid != null);
            Check("CropManager 存在", cropManager != null);
            if (farmGrid == null || cropManager == null) return;

            var inventory = InventorySystem.Instance;

            // 2. 资产与初始物资
            var seedRadish = Resources.Load<ItemData>("Data/seed_radish");
            var itemRadish = Resources.Load<ItemData>("Data/item_radish");
            var cropRadish = Resources.Load<CropData>("Data/crop_radish");
            Check("作物/物品资产可加载", seedRadish != null && cropRadish != null && itemRadish != null);
            if (seedRadish == null || cropRadish == null || itemRadish == null) return;
            Check($"初始萝卜种子 15 个（实际 {inventory.CountItem(seedRadish)}）", inventory.CountItem(seedRadish) == 15);

            // 3. 农耕闭环：锄地 -> 播种 -> 浇水 -> 3天生长 -> 收获
            var pos = new Vector3Int(0, 0, 0);
            farmGrid.HandleToolAction(pos, "Hoe");
            Check("锄地后状态 Tilled", farmGrid.GetAllPlots()[pos].status == TileStatus.Tilled);

            var plot = farmGrid.GetAllPlots()[pos];
            Check("播种成功", cropManager.TryPlantCrop(pos, cropRadish) && plot.plantedCrop == cropRadish);

            for (int day = 0; day < 3; day++)
            {
                farmGrid.HandleToolAction(pos, "WateringCan");
                cropManager.OnNewDay();
            }
            Check($"3天后生长进度>=3（实际 {plot.growthDays}）", plot.growthDays >= 3);

            bool harvested = cropManager.TryHarvestCrop(pos);
            int radishCount = inventory.CountItem(itemRadish);
            Check($"收获入包（萝卜 x{radishCount}）", harvested && radishCount > 0);

            // 4. 金钱
            int before = PlayerWallet.Instance.Money;
            PlayerWallet.Instance.AddMoney(100);
            Check($"加钱生效（{before} -> {PlayerWallet.Instance.Money}）", PlayerWallet.Instance.Money == before + 100);
            Check("扣钱生效", PlayerWallet.Instance.TrySpend(50) && PlayerWallet.Instance.Money == before + 50);

            // 5. 存读档往返
            bool hadSave = SaveManager.Instance.HasSaveFile;
            int moneyBefore = PlayerWallet.Instance.Money;
            SaveManager.Instance.SaveGame();
            PlayerWallet.Instance.AddMoney(999);
            SaveManager.Instance.LoadGame();
            Check($"读档恢复金钱（{moneyBefore} vs {PlayerWallet.Instance.Money}）", PlayerWallet.Instance.Money == moneyBefore);
            Check("读档标记生效", SaveManager.Instance.LoadedFromSave);
            if (!hadSave) SaveManager.Instance.DeleteSave();

            // 6. 对话
            DialogueManager.Instance.StartDialogue("测试NPC", new[] { "第一行", "第二行" });
            Check("对话可开启", DialogueManager.Instance.IsDialogueActive);
        }

        private void Check(string description, bool condition)
        {
            if (condition) _passes.Add("[PASS] " + description);
            else _failures.Add("[FAIL] " + description);
        }

        private void WriteResult()
        {
            var lines = new List<string>
            {
                "===== v0.1.0 冒烟测试结果 =====",
                $"通过: {_passes.Count}    失败: {_failures.Count}",
                ""
            };
            lines.AddRange(_passes);
            lines.AddRange(_failures);

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/smoke_result.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, lines);
            foreach (var line in lines) Debug.Log("[SmokeTest] " + line);
        }
    }
}
#endif
