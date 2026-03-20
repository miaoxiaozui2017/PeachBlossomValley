1. 核心原则：AI 作为“助理”而非“主美”

• 定位：AI 负责生成 素材草稿 和 批量填充，人类负责 风格把控 和 细节精修。

• 目标：将美术制作效率提升 3-5 倍，将人力集中在核心角色和关键动画上。

2. 工具链配置

环节	推荐工具	用途	输出格式
像素图生成	Aseprite (AI插件)	生成 16x16px 图块、角色草稿	.aseprite
概念图生成	Midjourney / Stable Diffusion	生成场景氛围、角色立绘参考	.png
3D 转 2D	Blockbench	制作 3D 模型后渲染为 2D 像素图	.png
动画制作	Spine / DragonBones	骨骼动画制作，AI 辅助补帧	.json + .png

3. 工作流 SOP (标准操作流程)

1. 需求输入：从 ART_TODO_LIST.md 中提取任务。

2. Prompt 编写：使用统一的风格描述词（如：pixel art, 16x16, top-down view, Chinese ancient style）。

3. AI 生成：生成 3-5 个变体。

4. 人工筛选：选择最符合的版本。

5. 精修导入：在 Aseprite 中手动调整像素点，确保风格统一。