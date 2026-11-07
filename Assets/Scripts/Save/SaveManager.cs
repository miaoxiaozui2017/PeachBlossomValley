using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using PBV.Inventory;
using PBV.Farming;
using PBV.Player;
using PBV.Economy;
using PBV.Systems;

namespace PBV.Save
{
    /// <summary>
    /// 存档管理器（v0.1.0）：
    /// - JSON 序列化写入 persistentDataPath
    /// - 每日 06:00 自动存档，F5 手动存档 / F9 读档
    /// - 地块采用稀疏存储（仅保存改变过的地块）
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Restore Catalogs (灰盒场景必填，用于恢复资产引用)")]
        [SerializeField] private ItemData[] _itemCatalog;
        [SerializeField] private CropData[] _cropCatalog;

        [Header("Settings")]
        [SerializeField] private bool _autoSaveOnNewDay = true;
        [SerializeField] private bool _loadOnStart = true;

        /// <summary>本次运行是否已从存档恢复（StarterKit 依据此判断是否发初始物资）</summary>
        public bool LoadedFromSave { get; private set; }

        public string SavePath => Path.Combine(Application.persistentDataPath, "pbv_save.json");
        public bool HasSaveFile => File.Exists(SavePath);

        public event Action OnSaved;
        public event Action OnLoaded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // 灰盒兜底：Inspector 未配置目录时自动从 Resources/Data 加载
            if (_itemCatalog == null || _itemCatalog.Length == 0)
            {
                _itemCatalog = Resources.LoadAll<ItemData>("Data");
            }
            if (_cropCatalog == null || _cropCatalog.Length == 0)
            {
                _cropCatalog = Resources.LoadAll<CropData>("Data");
            }
        }

        private void Start()
        {
            if (TimeManager.Instance != null && _autoSaveOnNewDay)
            {
                TimeManager.Instance.OnDayChanged += SaveGame;
            }

            if (_loadOnStart && HasSaveFile)
            {
                LoadGame();
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null && _autoSaveOnNewDay)
            {
                TimeManager.Instance.OnDayChanged -= SaveGame;
            }
        }

        private void Update()
        {
            // 调试快捷键（新 Input System 直读）
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f5Key.wasPressedThisFrame) SaveGame();
            if (keyboard.f9Key.wasPressedThisFrame) LoadGame();
        }

        /// <summary>
        /// 存档
        /// </summary>
        public void SaveGame()
        {
            try
            {
                var data = new SaveFile();

                // 玩家与时间
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    data.playerX = player.transform.position.x;
                    data.playerY = player.transform.position.y;
                }
                data.day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;

                // 金钱与出货箱
                if (PlayerWallet.Instance != null) data.money = PlayerWallet.Instance.Money;
                var bin = FindObjectOfType<ShippingBinSystem>();
                if (bin != null) data.shippingPending = bin.PendingValue;

                // 体力
                if (PlayerStats.Instance != null) data.stamina = PlayerStats.Instance.Stamina;

                // 季节
                if (PBV.Systems.SeasonSystem.Instance != null)
                {
                    data.currentSeason = (int)PBV.Systems.SeasonSystem.Instance.CurrentSeason;
                    data.dayOfSeason = PBV.Systems.SeasonSystem.Instance.DayOfSeason;
                }

                // 天气
                if (PBV.Systems.WeatherSystem.Instance != null)
                    data.weather = (int)PBV.Systems.WeatherSystem.Instance.CurrentWeather;

                // 任务进度
                if (PBV.Systems.QuestManager.Instance != null)
                    data.quests = PBV.Systems.QuestManager.Instance.ExportAll();

                // 好感度
                if (PBV.Systems.RelationshipSystem.Instance != null)
                    data.affections = PBV.Systems.RelationshipSystem.Instance.ExportAll();

                // 技能
                if (PBV.Systems.SkillSystem.Instance != null)
                    data.skills = PBV.Systems.SkillSystem.Instance.ExportAll();

                // 建造等级
                if (PBV.Systems.BuildSystem.Instance != null)
                {
                    var (bag, stam) = PBV.Systems.BuildSystem.Instance.Export();
                    data.bagLevel = bag;
                    data.staminaLevel = stam;
                }

                // 世界状态
                if (PBV.Systems.WorldState.Instance != null)
                    data.world = (int)PBV.Systems.WorldState.Instance.Current;

                // 灵兽
                if (PBV.Systems.PetSystem.Instance != null)
                    data.hasPet = PBV.Systems.PetSystem.Instance.Export();

                // 背包（只存非空槽位，含索引）
                if (InventorySystem.Instance != null)
                {
                    for (int i = 0; i < InventorySystem.Instance.Capacity; i++)
                    {
                        var slot = InventorySystem.Instance.GetSlot(i);
                        if (slot == null || slot.IsEmpty) continue;
                        data.inventory.Add(new SlotDTO { index = i, itemId = GetItemId(slot.item), count = slot.count });
                    }
                }

                // 地块（稀疏：只存改变过的）
                var farmGrid = FindObjectOfType<FarmGrid>();
                if (farmGrid != null) data.plots = farmGrid.ExportChangedPlots();

                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                Debug.Log($"[Save] 已存档 -> {SavePath} (Day {data.day}, {data.plots.Count} 地块)");
                OnSaved?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 存档失败: {e.Message}");
            }
        }

        /// <summary>
        /// 读档
        /// </summary>
        public bool LoadGame()
        {
            if (!HasSaveFile) return false;

            try
            {
                var data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));

                // 时间
                if (TimeManager.Instance != null) TimeManager.Instance.RestoreDay(data.day);

                // 玩家位置
                var player = FindObjectOfType<PlayerController>();
                if (player != null) player.transform.position = new Vector3(data.playerX, data.playerY, 0);

                // 金钱与出货箱
                if (PlayerWallet.Instance != null) PlayerWallet.Instance.SetMoney(data.money);
                var bin = FindObjectOfType<ShippingBinSystem>();
                if (bin != null) bin.SetPendingValue(data.shippingPending);

                // 体力
                if (PlayerStats.Instance != null) PlayerStats.Instance.SetStamina(data.stamina);

                // 季节
                if (PBV.Systems.SeasonSystem.Instance != null) PBV.Systems.SeasonSystem.Instance.Restore(data.currentSeason, data.dayOfSeason);

                // 天气
                if (PBV.Systems.WeatherSystem.Instance != null) PBV.Systems.WeatherSystem.Instance.Restore(data.weather);

                // 任务进度
                if (PBV.Systems.QuestManager.Instance != null)
                    PBV.Systems.QuestManager.Instance.ImportAll(data.quests);

                // 好感度
                if (PBV.Systems.RelationshipSystem.Instance != null)
                    PBV.Systems.RelationshipSystem.Instance.ImportAll(data.affections);

                // 技能
                if (PBV.Systems.SkillSystem.Instance != null)
                    PBV.Systems.SkillSystem.Instance.ImportAll(data.skills);

                // 建造等级
                if (PBV.Systems.BuildSystem.Instance != null)
                    PBV.Systems.BuildSystem.Instance.Import(data.bagLevel, data.staminaLevel);

                // 世界状态
                if (PBV.Systems.WorldState.Instance != null)
                    PBV.Systems.WorldState.Instance.Restore(data.world);

                // 灵兽
                if (PBV.Systems.PetSystem.Instance != null)
                    PBV.Systems.PetSystem.Instance.Import(data.hasPet);

                // 背包
                if (InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.ClearAll();
                    foreach (var slotDto in data.inventory)
                    {
                        var item = FindItem(slotDto.itemId);
                        if (item == null)
                        {
                            Debug.LogWarning($"[Save] 跳过未知物品: {slotDto.itemId}");
                            continue;
                        }
                        InventorySystem.Instance.SetSlot(slotDto.index, item, slotDto.count);
                    }
                }

                // 地块
                var farmGrid = FindObjectOfType<FarmGrid>();
                if (farmGrid != null)
                {
                    farmGrid.ImportPlots(data.plots, FindCrop);
                }

                Debug.Log($"[Save] 已读档 <- {SavePath} (Day {data.day})");
                LoadedFromSave = true;
                OnLoaded?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 读档失败: {e.Message}");
                return false;
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            Debug.Log("[Save] 存档已删除");
        }

        private static string GetItemId(ItemData item) => string.IsNullOrEmpty(item.id) ? item.itemName : item.id;

        private ItemData FindItem(string itemId)
        {
            if (_itemCatalog == null) return null;
            foreach (var item in _itemCatalog)
            {
                if (item == null) continue;
                if (item.id == itemId || item.itemName == itemId) return item;
            }
            return null;
        }

        private CropData FindCrop(string cropName)
        {
            if (_cropCatalog == null || string.IsNullOrEmpty(cropName)) return null;
            foreach (var crop in _cropCatalog)
            {
                if (crop != null && crop.cropName == cropName) return crop;
            }
            return null;
        }
    }
}
