# ComfyUI 像素素材生产线设计 (Pixel Art Asset Pipeline)

**目标**：利用 ComfyUI 批量生成高质量、风格统一的游戏像素素材（Sprite Sheet）。
**适用对象**：主创本地部署的 ComfyUI 环境。

---

## 1. 核心模型推荐 (Checkpoints & LoRAs)

为了获得最佳的 16-bit 像素效果，建议下载以下模型（Civitai 可搜）：

*   **底模 (Checkpoint)**:
    *   **推荐 1**: `Pixel Art XL` (基于 SDXL，对 Prompt 理解极强，适合生成 Grid)。
    *   **推荐 2**: `Retro Diffusion` (SD1.5 时代的王者，如果显存小可用这个)。
*   **LoRA**:
    *   `Pixel Art Sprite Sheet` (用于强制生成网格排列)。
    *   `16-bit Style` (用于强化复古感)。

---

## 2. 工作流设计 (Workflow Design)

建议搭建一个包含 **"生成 -> 像素化后处理 -> 去背景"** 的自动化流。

### 节点逻辑 (Node Structure)
1.  **Load Checkpoint**: 加载 Pixel Art 模型。
2.  **CLIP Text Encode (Positive)**: 输入特定 Prompt (见下文)。
3.  **KSampler**:
    *   *Steps*: 30-40
    *   *CFG*: 7.0
    *   *Sampler*: `dpmpp_2m_karras` (细节好) 或 `euler_ancestral` (复古感强)。
4.  **Image Scale (Pixel Perfect)**: **关键步骤**。
    *   先生成高分辨率图 (如 1024x1024)。
    *   然后使用 `Pixelization` 节点或 Downscale (Nearest Neighbor) 缩放到目标像素尺寸 (如 128x128)，再 Upscale 回来预览。
    *   *目的*：保证像素颗粒清晰，没有模糊边缘。
5.  **Image Rembg (Optional)**: 连接 `RemBG` 节点自动去除背景。
6.  **Save Image**: 输出 PNG。

---

## 3. 提示词工程 (Prompt Engineering)

### 通用起手式 (Master Prompt)
> **Positive**:
> `pixel art sprite sheet, [Subject], [Action], game asset, 16-bit, flat color, clean lines, white background, grid layout, retro style`
>
> **Negative**:
> `blur, fuzzy, noise, anti-aliasing, realistic, photo, 3d render, gradient, shadow, messy, text, watermark`

### 针对今日任务的 Prompt (可以直接复制)

#### A. 男性素体 (Male Base Body)
```text
pixel art sprite sheet, male character base body, naked torso, simple beige pants, no hair, bald head, neutral skin tone, 4 views (front, back, side, side), idle stance, walking animation frames, game asset, 16-bit snes style, clean pixel art, grid layout, white background --ar 1:1
```

#### B. 女性素体 (Female Base Body)
```text
pixel art sprite sheet, female character base body, naked torso, simple beige pants, no hair, bald head, neutral skin tone, slender build, 4 views (front, back, side, side), idle stance, walking animation frames, game asset, 16-bit snes style, clean pixel art, grid layout, white background --ar 1:1
```

#### C. 粗布衣 (Tunic Outfit)
```text
pixel art sprite sheet, simple blue tunic and brown trousers outfit, clothing layout for game character, flat item view, front back side views, no character body, only clothes, game asset, 16-bit style, clean pixel art, white background --ar 1:1
```

---

## 4. 后处理建议 (Post-Processing)

ComfyUI 直出的图可能在像素网格上对不齐。建议导出后：
1.  使用 **Aseprite** 打开。
2.  手动调整位置，使其严格对齐到 32x32 的网格中。
3.  **颜色统一**：使用 Aseprite 的 "Replace Color" 功能，将 AI 生成的杂色统一为我们 GDD 里的 16 色盘。

---

**咔唔~** (这套流程能让您的 ComfyUI 变成全自动像素工厂！)
