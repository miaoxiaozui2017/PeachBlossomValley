using System;
using System.Collections.Generic;
using PBV.Systems;

namespace PBV.Save
{
    /// <summary>
    /// 存档根结构（JsonUtility 序列化）
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        public int version = 1;
        public int day;                 // 当前天数
        public float playerX;
        public float playerY;
        public int money;               // 兼容旧档：旧版单一货币（读档时并入宋币）
        public int song;                // 宋币（文）
        public int rmb;                 // 人民币（元）
        public int shippingPending;     // 出货箱待结算金额
        public int stamina;             // 玩家体力
        public int hp;                  // 玩家血量（战斗用）
        public int debt;                // 晕倒欠药婆的诊费（背债）
        public int currentSeason;       // 当前季节（0春1夏2秋3冬）
        public int dayOfSeason;         // 当季第几天
        public int currentTool;         // 当前工具（ToolType 枚举值）
        public int weather;             // 天气（Weather 枚举值）
        public int bagLevel;            // 背包扩建等级
        public int staminaLevel;        // 体力上限扩建等级
        public int houseLevel;          // 房屋扩建等级（0-2，家具扩建）
        public int world;               // 当前世界（WorldType 枚举值）
        public int hasPet;              // 是否有宠物（0/1）
        public int achievements;        // 成就位掩码
        public int reputation;          // 宗族声望
        public int buffType;            // 当前菜品增益
        public int buffDays;            // 增益剩余天数
        public int health;              // 疾病状态（类型*10 + 剩余天数，0=无病）
        public int trap;                // 陷阱已布饵天数（0=未布）
        public int cage;                // 笼箱已布天数（0=未布）
        public int outfit;              // 衣柜当前服装索引
        public string[] tradeSlots;     // 穿梭贸易 3 个携带槽位（物品 id）

        public List<SlotDTO> inventory = new List<SlotDTO>(); // 桃花谷背包（旧档兼容）
        public List<SlotDTO> modernInventory = new List<SlotDTO>(); // 现代背包（两界独立）
        public List<SlotDTO> storage = new List<SlotDTO>(); // 旧版：房屋扩建解锁的单一储物箱（迁移用）
        public List<StorageChestDTO> chests = new List<StorageChestDTO>(); // 新版：可放置的多个储物箱
        public List<PlotDTO> plots = new List<PlotDTO>(); // 稀疏存储：只保存改变过的地块
        public List<QuestDTO> quests = new List<QuestDTO>();
        public List<AffectionDTO> affections = new List<AffectionDTO>();
        public List<SkillDTO> skills = new List<SkillDTO>();
        public List<LivestockDTO> livestock = new List<LivestockDTO>(); // 养殖牲畜
    }

    /// <summary>牲畜存档（type = LivestockType 枚举值）</summary>
    [Serializable]
    public class LivestockDTO
    {
        public int type;
        public int count;
        public int fedDays;
    }

    /// <summary>单个储物箱存档（新版多实例）</summary>
    [Serializable]
    public class StorageChestDTO
    {
        public string uid;
        public float x;
        public float y;
        public string label;
        public int colorIndex;
        public List<SlotDTO> items = new List<SlotDTO>();
    }

    /// <summary>
    /// 背包槽位（槽位索引 + 物品引用 id + 数量）
    /// </summary>
    [Serializable]
    public class SlotDTO
    {
        public int index;
        public string itemId;
        public int count;
    }

    /// <summary>
    /// 地块数据（坐标 + 状态 + 作物）
    /// </summary>
    [Serializable]
    public class PlotDTO
    {
        public int x;
        public int y;
        public int status;          // TileStatus 枚举值
        public int growthDays;
        public bool wateredToday;
        public int fertilized;      // 施肥等级：0=无肥，1=堆肥，6=现代肥(+6，产量×3)
        public int polluted;        // 土地污染剩余天数（现代肥料所致），0=无污染
        public string cropName;     // 空串 = 无作物
    }
}
