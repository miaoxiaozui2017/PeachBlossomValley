# 0.1.0 程序任务清单 (Program TODO List v0.1.0)

*(从 Docs/05_PROGRAM_TODO_LIST.md 提炼，并根据 v0.1.0 风险评估优化)*

## 1. 核心框架 (Core)

*   [x] **Project Setup**：创建 Unity 工程，设置 2D URP 环境。
*   [x] **InputManager**：
    *   实现基于新 Input System 的按键映射 (WASD移动、E交互、Space工具)。
    *   **九宫格选择逻辑**：根据玩家面向，计算并高亮前方九宫格内最合适的目标地块。
    *   灰盒模式直读键盘（不依赖 .inputactions 资产），保留 InputValue 回调通道兼容 PlayerInput。

## 2. 角色与控制 (Player)

*   [x] [AI-Generated] **PlayerController**：
    *   实现 8 方向移动逻辑。
    *   实现基于 Animator 的状态切换 (Idle/Walk/Action) —— 参数已广播，待接入动画资源。
    *   **交互判定**：增加对出货箱、NPC 的 Raycast/OverlapCircle 检测（IInteractable 接口）。

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

*   [x] [AI-Generated] **TimeManager**：
    *   **调整转换率**：设定 `0.5 Real Seconds = 1 Game Minute`（一天约 9 分钟）。
    *   **跨日逻辑**：24:00 触发 `StartNewDay()`（重置到 06:00）。
    *   Global Light 颜色插值 (Color Gradient)。
    *   调试键：N 键跳过到次日。
    *   [ ] **强制睡眠逻辑**：02:00 自动触发 `EndDay()`（待实现，需接入"日期跨越到凌晨"判定）。

## 8. 场景与占位资源

*   [x] **SceneBuilder（Editor 一键构建）**：菜单 `PBV/构建灰盒场景 v0.1.0`
    *   自动生成 32x32 占位像素贴图（地块/角色/NPC/出货箱/作物4阶段/物品图标）
    *   自动生成 Tile 与 CropData/ItemData 资产（Assets/Resources/Data）
    *   自动搭建 01_FarmGreyBox 场景（Grid+三层Tilemap/玩家/NPC/出货箱/系统管理器/GlobalLight2D/相机跟随）
    *   幂等：可重复执行，不产生重复资产
*   [x] **HUD**：天数/时间/金钱显示 + 操作提示。
