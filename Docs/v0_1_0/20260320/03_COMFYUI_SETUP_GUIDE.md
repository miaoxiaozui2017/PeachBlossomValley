# ComfyUI 新手安装与配置指南 (Setup Guide)

**目标**：从零开始搭建 ComfyUI 环境，并配置好用于生成像素画的所有模型。

---

## 1. 安装 ComfyUI (Windows)

最简单的方法是使用官方的“便携版” (Portable Version)，无需复杂的 Python 配置。

1.  **下载**：
    *   访问 ComfyUI 的 GitHub 发布页：`https://github.com/comfyanonymous/ComfyUI`
    *   找到 **"Direct link to download"** (通常在 Readme 顶部)。
    *   下载 `ComfyUI_windows_portable_nvidia_cu121_or_cpu.7z` (约 1.4GB)。
2.  **解压**：
    *   将压缩包解压到一个路径**不含中文和空格**的文件夹中 (例如 `D:\AI\ComfyUI_Portable`)。
3.  **启动**：
    *   双击 `run_nvidia_gpu.bat` (如果有 N 卡)。
    *   双击 `run_cpu.bat` (如果没有独显，速度会很慢)。
    *   浏览器会自动打开 `http://127.0.0.1:8188`，看到界面即成功。

---

## 2. 下载核心模型 (Checkpoints)

模型文件很大，请确保硬盘空间充足。

1.  **访问 Civitai**：`https://civitai.com`
2.  **搜索并下载**：
    *   **Pixel Art XL** (推荐): 搜索 `Pixel Art XL`，下载 `.safetensors` 文件 (约 6GB)。
    *   *备选*: `Retro Diffusion` (搜索 `Retro Diffusion`)。
3.  **放入文件夹**：
    *   将下载的文件移动到：`ComfyUI_Portable\ComfyUI\models\checkpoints\`

---

## 3. 下载 LoRA 模型 (可选但推荐)

LoRA 能让 AI 更懂“像素画”和“网格排列”。

1.  **搜索并下载**：
    *   **Pixel Art Sprite Sheet** (搜索 `Sprite Sheet` 或 `Pixel Game Asset`)。
    *   下载 `.safetensors` 文件 (通常 100MB 左右)。
2.  **放入文件夹**：
    *   将文件移动到：`ComfyUI_Portable\ComfyUI\models\loras\`

---

## 4. 安装插件 (Manager)

为了方便后续安装缺少的节点，强烈建议安装 **ComfyUI Manager**。

1.  进入 `ComfyUI_Portable\ComfyUI\custom_nodes\` 文件夹。
2.  在该文件夹内打开终端 (右键 -> 在终端打开)，输入：
    `git clone https://github.com/ltdrdata/ComfyUI-Manager.git`
    *(如果没有装 git，可以直接去该网址下载 ZIP，解压放进去)*
3.  重启 ComfyUI。界面右下角会出现一个 **"Manager"** 按钮。

---

## 5. 搭建像素工作流 (Building the Workflow)

现在回到浏览器界面：

1.  **清空**：点击右侧面板的 `Clear` 按钮。
2.  **加载默认**：点击 `Load Default`，你会看到基础的 `Load Checkpoint -> KSampler -> Save Image` 连线。
3.  **修改配置**：
    *   **Load Checkpoint**: 点击模型名称，选择你刚才下载的 `Pixel Art XL`。
    *   **Empty Latent Image**: 设置 `width` 和 `height` 为 `1024` (生成大图再缩小)。
    *   **CLIP Text Encode (Positive)**: 粘贴我在 `02_COMFYUI_WORKFLOW.md` 里给你的 Prompt。
    *   **KSampler**: `steps` 设为 30, `cfg` 设为 7.0。
4.  **添加缩放节点 (Pixel Perfect)**:
    *   双击空白处，搜索 `ImageScale`。
    *   将 KSampler 的 `IMAGE` 输出连到 `ImageScale` 的 `image` 输入。
    *   设置 `ImageScale` 的 `width` 为 `128` (1024 / 8)，`upscale_method` 选 `nearest-exact` (这是保持像素锐利的关键！)。
    *   将 `ImageScale` 的输出连到 `Save Image`。

---

## 6. 开始生成

点击右侧的 **"Queue Prompt"** 按钮。
稍等片刻，你的第一张像素素材就会出现在 `ComfyUI_Portable\ComfyUI\output\` 文件夹里了！

**咔唔~** (有问题随时问我！)
