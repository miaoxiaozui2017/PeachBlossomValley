一、 给AI Agent的开发规范文档

文件名： 01_AI_DEV_GUIDELINES.md

位置： 项目根目录

目标： AI在编写任何代码前必须阅读此文档
------

AI 开发规范 v2.1 (主创-AI 协同版)

# 1. 协同工作流 (Workflow)

为了高效推进《桃花谷》开发，我们采用以下协同工作流：

## 1.1 版本拆分 (Version Splitting)
1.  **读取计划**：从 `Docs/06_VERSION_PLAN.md` 获取当前阶段的版本目标。
2.  **创建版本目录**：在 `Docs/` 下创建名为 `vX_X_X` (如 `v0_1_0`) 的文件夹。
3.  **拆分文档**：将当前版本需要完成的 **游戏设计**、**美术Todo**、**程序Todo** 从大文档中提取出来，存入该版本文件夹。

## 1.2 每日任务拆解 (Daily Task Breakdown) [新增]
*   **子文件夹**：在版本目录下，按日期创建子文件夹 (如 `Docs/v0_1_0/20260320/`)。
*   **原子化任务**：将当天任务拆解为 **极小颗粒度** 的任务列表 (如 `01_TASK_ART_PLAYER_ACTIONS.md`)。
*   **同类聚合**：单一任务文件应聚焦于 **相似类型** 的工作 (例如：仅包含主角动作生成的任务，不混入代码编写)，以便批量高效处理。

## 1.3 分工与实现 (Roles & Implementation)
*   **主创 (User)**：负责 **架构搭建**。主创会编写好脚本、类、方法的框架，定义好接口和数据结构。
*   **AI制作人 (You)**：负责 **逻辑填充**。在主创提供的框架内，寻找 `[AI-Generated]` 标记，填入具体的实现代码或资源。
    *   *程序*：在标记处填入函数逻辑。
    *   *美术/音乐*：根据标记生成对应的像素画或音频资源。

## 1.4 审核与发布 (Review & Publish)
1.  **AI实现**：AI 完成代码或资源生成后，需保留 `[AI-Generated]` 标记，以便审核。
2.  **人工审核**：主创对 AI 生成的内容进行 Code Review 或 艺术审查。
3.  **发布**：审核通过后，移除标记或确认为最终版本，方可进行 Git 提交或发布。

## 1.5 任务留档 (Archiving)
*   所有完成的任务文档（如 `Docs/v0_1_0/TODO_LIST.md`）需按日期归档，保留开发历史。

---

# 2. 基本代码原则 (Code Principles)

1.  **你只实现函数内部逻辑，不修改架构**。
2.  **遵循现有代码风格**。
3.  **函数原子化**：单个函数严格控制在 **50行** 以内。
4.  **鲁棒性**：必须包含错误处理和空值检查。
5.  **禁止硬编码**：所有数值必须提取为常量或配置。

# 3. 代码生成格式 (Code Generation Format)

## 模板：函数实现
```csharp
// [你的注释：功能描述]
public ReturnType FunctionName(Parameters)
{
    // [AI-Generated] Start
    // 步骤1：...
    // 步骤2：...
    // 返回结果
    // [AI-Generated] End
}
```

## 模板：数据类
```csharp
[System.Serializable]
public class ClassName
{
    public Type field;
    
    // [AI-Generated] Start
    public override string ToString()
    {
        // AI实现区域
    }
    // [AI-Generated] End
}
```

# 4. 命名规范 (Naming Conventions)

• 私有字段：`_camelCase`
• 局部变量：`camelCase`
• 常量：`UPPER_SNAKE_CASE`
• 布尔变量：`isXxx` / `hasXxx` / `canXxx`

# 5. 禁止行为 (Forbidden Actions)

❌ **不要修改函数签名**
❌ **不要添加未授权的 public 方法**
❌ **不要删除已有代码** (除非是替换占位符)
❌ **不要引入新依赖**
❌ **不要使用魔数（Magic Numbers）**

# 6. 必须包含 (Must Include)

✅ **XML注释**
✅ **空值检查 (Null Checks)**
✅ **错误日志 (Debug.LogError)**
✅ **返回值验证**
