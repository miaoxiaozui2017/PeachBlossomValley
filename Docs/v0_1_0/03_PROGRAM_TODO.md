# 0.1.0 程序任务清单 (Program TODO List v0.1.0)

*(从 Docs/05_PROGRAM_TODO_LIST.md 提炼，并根据 v0.1.0 风险评估优化)*

## 1. 核心框架 (Core)

*   [ ] **Project Setup**：创建 Unity 工程，设置 2D URP 环境。
*   [ ] **InputManager**：
    *   实现基于新 Input System 的按键映射 (WASD移动、E交互、Space工具)。
    *   **九宫格选择逻辑**：根据玩家面向，计算并高亮前方九宫格内最合适的目标地块。

## 2. 角色与控制 (Player)

*   [ ] [AI-Generated] **PlayerController**：
    *   实现 8 方向移动逻辑。
    *   实现基于 Animator 的状态切换 (Idle/Walk/Action)。
    *   **交互判定**：增加对出货箱、NPC 的 Raycast/OverlapCircle 检测。

## 3. 种植系统 (Farming - Core)

*   [x] [AI-Generated] **FarmGrid**：
    *   基于 Tilemap 的网格管理。
    *   坐标转换 (World <-> Grid)。
    *   **数据结构扩展**：使用 `Dictionary` 或 `ScriptableObject` 存储地块数据，预留未来扩展属性。
*   [x] [AI-Generated] **CropManager**：
    *   作物数据结构 (ScriptableObject)。
    *   生长计时逻辑 (Day Update)。
*   [ ] **Tools Interaction**：
    *   [x] 锄头：`TileType.Dirt` -> `TileType.Tilled`。
    *   [x] 水壶：`TileType.Tilled` -> `TileType.Watered`。
    *   [x] 种子包：`TileType.Tilled` -> `HasCrop`（消耗背包种子，ItemData.cropToPlant 关联作物）。
    *   [x] 镰刀：`Crop.Mature` -> `DropItem`（收获进背包）。

## 4. 物品与经济 (Inventory & Economy - MVP)

*   [x] [AI-Generated] **InventorySystem**：
    *   24格数据结构。
    *   添加/移除/堆叠逻辑。
    *   种子查找（FindSeedSlot）与数量统计（CountItem）。
*   [x] [AI-Generated] **ShippingBinSystem (新增)**：
    *   交互逻辑：E 键投入背包中全部可出售物品（跳过工具/种子）。
    *   结算逻辑：每日 06:00 (TimeManager.OnDayChanged) 结算总价值并入账 `PlayerWallet`。
*   [x] **UI_Inventory**：
    *   简单的网格显示（24 格 6x4 面板，Tab 开关，灰盒 UI 自动构建）。
    *   点击选中/快捷栏映射（底部常驻快捷栏槽位 0-9，数字键 1-0 选择）。

## 5. NPC与对话 (Dialogue - Basic)

*   [x] [AI-Generated] **SimpleDialogueSystem**：
    *   显示文本框（灰盒 UI 自动构建，后续可替换）。
    *   逐字显示效果。
    *   按键翻页（E，打字中先补全再翻页）。
    *   **NPCController**：挂载即用的基础 NPC（碰撞体 + 对话数据）。

## 6. 存档系统 (Save - Optimized)

*   [x] [AI-Generated] **SaveManager**：
    *   定义 `SaveData` 类 (包含 PlayerPos, InventoryList, Money)。
    *   **稀疏矩阵存储**：地块数据仅存储 `List<ChangedPlotData>`，跳过未改变的空地块，减小文件体积。
    *   序列化为 JSON 并写入本地文件 (persistentDataPath)。
    *   每日 06:00 自动存档；F5 手动存档 / F9 读档。
    *   恢复范围：天数/玩家位置/金钱/背包(含槽位索引)/地块/出货箱待结算。

## 7. 昼夜循环 (Time)

*   [ ] [AI-Generated] **TimeManager**：
    *   **调整转换率**：设定 `2 Real Seconds = 1 Game Minute`。
    *   **强制睡眠逻辑**：02:00 自动触发 `EndDay()`。
    *   Global Light 颜色插值 (Color Gradient)。
