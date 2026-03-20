1. 核心原则：AI 作为“素材库”与“灵感助手”

• 定位：AI 负责生成 基础音效 和 旋律片段，人类负责 混音、剪辑、风格统一。

• 目标：解决音效重复度高、音乐制作成本高的问题，将人力集中在核心 BGM 和关键音效的打磨上。

2. 工具链配置

环节	推荐工具	用途	输出格式
音效生成	Audo.ai / Boomy	生成环境音、动作音效、UI 音效	.wav / .mp3
音乐生成	Suno AI / AIVA	生成背景音乐 (BGM) 片段、主题旋律	.midi / .wav
音频处理	Audacity / Reaper	对 AI 生成素材进行降噪、剪辑、混音	.wav
Unity 插件	FMOD / Wwise	专业音频引擎，管理复杂的音频逻辑	.bank

3. 工作流 SOP (标准操作流程)

1. 需求输入：从 GAME_DESIGN_DOCUMENT.md 中提取音频需求（如：需要“古风笛子+流水声”的 BGM）。

2. Prompt 编写：使用统一的描述词（如：Chinese ancient, guzheng, peaceful, 80 BPM）。

3. AI 生成：生成 3-5 个变体，导出为 .wav 或 .midi。

4. 人工筛选与微调：在 Audacity 或 DAW 中剪辑、降噪、调整音量。

5. 导入 Unity：设置正确的导入参数（见下文）。