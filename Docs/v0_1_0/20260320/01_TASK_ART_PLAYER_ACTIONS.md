# 任务：生成主角素体与基础服装
**日期**：2026/03/20
**状态**：待执行

## 目标
生成支持换装系统的 **分层角色素材 (Layered Sprites)**。
包含：男/女素体 (Base Body)、默认发型、默认粗布衣。

## 技术方案 (Layered System)
采用多层 Sprite 叠加渲染，实现换装与性别切换。
*   **层级结构 (Sorting Order)**：
    1.  **Body** (素体)
    2.  **Clothes_Bottom** (裤子/裙子)
    3.  **Clothes_Top** (上衣)
    4.  **Hair** (发型)
    5.  **Tool** (手持工具)

## 规格
*   **风格**：16-bit 像素风，古风。
*   **尺寸**：**32x32px** (画布尺寸，角色约占 16x28px)。
*   **色板**：GDD 16色主色板。

## 任务列表 (Task List)

### 1. 素体 (Base Body) - 2套
*(仅包含皮肤颜色，无任何衣物与头发)*
*   [ ] **男性素体** (Male Base)
    *   包含：站立、行走、锄地、浇水动作 (所有方向)。
*   [ ] **女性素体** (Female Base)
    *   包含：站立、行走、锄地、浇水动作 (所有方向)。
    *   *区别*：身形稍纤细，动作幅度微调。

### 2. 默认发型 (Hair) - 2套
*(适配素体头部，颜色可变)*
*   [ ] **男式束发** (Male Bun)
    *   包含所有动作的对应帧。
*   [ ] **女式垂髻** (Female Braid)
    *   包含所有动作的对应帧。

### 3. 默认服装 (Clothes) - 1套 (通用)
*(适配素体身形，一套粗布农服)*
*   [ ] **粗布上衣** (Tunic)
    *   包含所有动作的对应帧。
*   [ ] **粗布长裤** (Trousers)
    *   包含所有动作的对应帧。

## 输出文件要求
*   格式：`.png` (透明背景)
*   排列：统一 Grid 排列 (例如每行 8 帧，素体与衣服必须 **严格对齐**)。
*   命名：
    *   `spr_char_base_male.png`
    *   `spr_char_base_female.png`
    *   `spr_hair_male_01.png`
    *   `spr_hair_female_01.png`
    *   `spr_clothes_tunic_01.png`
    *   `spr_clothes_pants_01.png`
