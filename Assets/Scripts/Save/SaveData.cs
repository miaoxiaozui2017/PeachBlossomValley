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
        public int money;               // 玩家金钱
        public int shippingPending;     // 出货箱待结算金额
        public int stamina;             // 玩家体力
        public int currentSeason;       // 当前季节（0春1夏2秋3冬）
        public int dayOfSeason;         // 当季第几天
        public int currentTool;         // 当前工具（ToolType 枚举值）
        public int weather;             // 天气（Weather 枚举值）
        public int bagLevel;            // 背包扩建等级
        public int staminaLevel;        // 体力上限扩建等级
        public int world;               // 当前世界（WorldType 枚举值）
        public int hasPet;              // 是否有宠物（0/1）
        public int achievements;        // 成就位掩码
        public int reputation;          // 宗族声望
        public int buffType;            // 当前菜品增益
        public int buffDays;            // 增益剩余天数
        public string[] tradeSlots;     // 穿梭贸易 3 个携带槽位（物品 id）

        public List<SlotDTO> inventory = new List<SlotDTO>();
        public List<PlotDTO> plots = new List<PlotDTO>(); // 稀疏存储：只保存改变过的地块
        public List<QuestDTO> quests = new List<QuestDTO>();
        public List<AffectionDTO> affections = new List<AffectionDTO>();
        public List<SkillDTO> skills = new List<SkillDTO>();
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
        public string cropName;     // 空串 = 无作物
    }
}
