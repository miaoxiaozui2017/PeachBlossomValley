开发说明

使用本套文档的指南：

1. 启动项目时：

  ◦ 先阅读 03_GAME_DESIGN_DOCUMENT.md 了解整体设计

  ◦ 查看 06_VERSION_PLAN.md 确定当前阶段目标

2. 每日开发时：

  ◦ 查看 04_ART_TODO_LIST.md 了解美术任务

  ◦ 查看 05_PROGRAM_TODO_LIST.md 了解编程任务

  ◦ 优先完成P0优先级任务

3. 让AI编码时：

  ◦ 参考 01_AI_DEV_GUIDELINES.md 规范AI代码

  ◦ 使用 02_TODO_AI_IMPLEMENT.md 中的模板

  ◦ 给AI明确的任务描述和步骤

4. 版本管理时：

  ◦ 严格遵循 06_VERSION_PLAN.md 的里程碑

  ◦ 每个版本完成后进行测试

  ◦ 根据测试反馈调整后续计划

重要原则：

1. 单一数据源：03_GAME_DESIGN_DOCUMENT.md 是设计的唯一真理源

2. 小步快跑：每2周至少有一个可运行版本

3. 美术先行：美术资源进度领先程序1-2周

4. 测试驱动：每个功能完成后立即测试

5. 文档同步：设计变更时立即更新相关文档

项目结构建议：

PeachBlossomValley/
├── Assets/
│   ├── Art/                # 美术资源
│   │   ├── Sprites/         # 游戏内像素资源
│   │   ├── UI/             # 界面资源
│   │   ├── Cinematics/     # 过场动画资源 (新增文件夹)
│   │   └── Reality/        # 现实世界美术资源 (新增文件夹)
│   ├── Audio/              # 音效音乐
│   ├── Prefabs/            # 预制体
│   ├── Scenes/             # 场景
│   │   ├── 00_Cinematic    # 开场动画场景
│   │   ├── 01_Reality      # 现实世界场景
│   │   ├── 02_TaohuaValley # 桃花谷主场景
│   │   └── ...
│   ├── Scripts/            # 脚本
│   │   ├── Core/           # 核心系统
│   │   ├── UI/             # 界面系统
│   │   ├── Data/           # 数据管理
│   │   ├── Reality/        # 现实世界系统脚本 (新增文件夹)
│   │   ├── Tools/          # 开发工具
│   │   └── ...
│   └── StreamingAssets/   # 配置、对话JSON等
├── Docs/                  # 本文档集
├── Builds/                 # 构建版本
└── ProjectSettings/        # 项目设置


开发工具链：

• 引擎：Unity 2022 LTS

• 版本控制：Git + Git LFS

• 项目管理：Trello/Notion

• 美术工具：Aseprite, Photoshop

• 音效工具：Bosca Ceoil, BFXR

• 代码编辑器：Visual Studio 2022

• 文档工具：Markdown

最后更新：2026年3月18日

版本：3.0

状态：开发中