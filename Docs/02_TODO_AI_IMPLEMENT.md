二、 AI介入内容标注规范

文件名： 02_TODO_AI_IMPLEMENT.md

位置： 项目根目录

用途： 标记需要AI实现的部分
------

AI 待实现任务

优先级 P0（核心玩法）

1. FarmingSystem.cs

public class FarmingSystem
{
    // [AI待实现] 种植作物
    public bool PlantCrop(Vector2Int gridPos, int cropID)
    {
        // AI_TODO: 实现种植逻辑
        // 输入：gridPos(网格位置), cropID(作物ID)
        // 步骤：
        // 1. 检查gridPos是否有效
        // 2. 检查地块状态是否为Tilled
        // 3. 检查背包是否有cropID的种子
        // 4. 从背包移除种子
        // 5. 在地块创建作物实例
        // 6. 更新地块状态为Planted
        // 返回：bool(是否成功)
        // AI实现开始
        
        // AI实现结束
    }
    
    // [AI待实现] 浇水
    public void WaterPlot(Vector2Int gridPos) { /* AI_TODO */ }
    
    // [AI待实现] 收获
    public HarvestResult HarvestPlot(Vector2Int gridPos) { /* AI_TODO */ }
}


2. InventorySystem.cs

// [AI待实现] 添加物品
public int AddItem(int itemID, int count)
{
    // AI_TODO: 实现物品添加逻辑
    // 1. 查找是否存在相同物品的格子
    // 2. 如果存在且可堆叠，则增加数量
    // 3. 如果不存在，查找空格子
    // 4. 如果没有空格子，返回0
    // 5. 触发OnInventoryChanged事件
}


优先级 P1（辅助系统）

3. TimeSystem.cs

// [AI待实现] 时间流逝
private void AdvanceTime()
{
    // AI_TODO: 处理时间推进
    // 1. 增加游戏时间
    // 2. 检查是否到第二天
    // 3. 如果是，调用OnDayPassed()
    // 4. 更新所有时间相关系统
}


如何让AI实现

1. 复制带[AI待实现]注释的代码块

2. 给AI明确的步骤说明

3. 测试生成的代码

4. 如果失败，回滚并重新描述