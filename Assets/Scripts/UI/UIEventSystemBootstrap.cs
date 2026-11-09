using UnityEngine;
using UnityEngine.EventSystems;

namespace PBV.UI
{
    /// <summary>
    /// 运行时自动补建 EventSystem。
    /// 灰盒场景由 SceneBuilder 生成，场景里没有放置 EventSystem；没有它时 uGUI 的 Button
    /// （制作 / 购买 / 升级 / 建造等）完全收不到鼠标点击，表现就是「按钮明明亮着却点不动」。
    /// 由 HUD.Awake 调用一次，全局幂等，不依赖重新构建场景。
    /// </summary>
    public static class UIEventSystemBootstrap
    {
        private static bool _checked;

        // ===== E 键「谁开的谁关」：当前最上层功能面板的关闭回调 =====
        private static System.Action _closeTopPanel;

        /// <summary>功能面板打开时登记自己为栈顶；E 键会优先关它而不是再次触发场景交互</summary>
        public static void RegisterTopPanel(System.Action close) => _closeTopPanel = close;

        /// <summary>面板关闭时注销（只注销自己，避免误清后打开的面板）</summary>
        public static void UnregisterTopPanel(System.Action close)
        {
            if (_closeTopPanel == close) _closeTopPanel = null;
        }

        /// <summary>面板自行关闭时清空栈顶</summary>
        public static void ClearTopPanel() => _closeTopPanel = null;

        /// <summary>InputHandler 在 E 按下时先调用；返回 true 表示这次 E 已被 UI 消费</summary>
        public static bool TryCloseTopPanel()
        {
            if (_closeTopPanel == null) return false;
            var close = _closeTopPanel;
            _closeTopPanel = null;
            close();
            return true;
        }

        public static void Ensure()
        {
            if (_checked) return;
            _checked = true;

            // 兼容「还没重建场景」的旧存档场景：开场介绍是全屏黑底，会盖住主界面。
            // 先禁用组件（否则它的 Start 仍会执行），再销毁。
            foreach (var intro in UnityEngine.Object.FindObjectsOfType<PBV.Systems.IntroSystem>())
            {
                intro.enabled = false;
                UnityEngine.Object.Destroy(intro);
            }

            // 兼容「还没重建场景」：帮助页/成就页组件是后加的，运行时补挂，H / L 键立即可用
            if (UnityEngine.Object.FindObjectOfType<HelpUI>() == null)
                new GameObject("HelpUI (Auto)").AddComponent<HelpUI>();
            if (UnityEngine.Object.FindObjectOfType<AchievementUI>() == null)
                new GameObject("AchievementUI (Auto)").AddComponent<AchievementUI>();
            // 测试期布局：运行时把所有交互点排到北侧两排并挂名字标签，无需重建场景
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.StationLayoutBootstrap>() == null)
                new GameObject("StationLayout (Auto)").AddComponent<PBV.Systems.StationLayoutBootstrap>();
            // v0.3.0 内容扩展：补建 4 NPC / 2 钓点 / 染坊，并给商店上架蓝草
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.ContentExpansionBootstrap>() == null)
                new GameObject("ContentExpansion (Auto)").AddComponent<PBV.Systems.ContentExpansionBootstrap>();
            // v0.4.0：声望 / Buff / 节气事件 / 餐厅订单 / 穿梭贸易 与 O、R 两个面板
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.V04Bootstrap>() == null)
                new GameObject("V04Bootstrap (Auto)").AddComponent<PBV.Systems.V04Bootstrap>();
            // v0.6.0：收集图鉴 / 鱼塘 / 隐藏彩蛋 与 Y 面板
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.V06Bootstrap>() == null)
                new GameObject("V06Bootstrap (Auto)").AddComponent<PBV.Systems.V06Bootstrap>();
            // 双币种：时空驿站面板（传送门 E 打开，含穿梭 + 宋币⇄人民币兑换）
            if (UnityEngine.Object.FindObjectOfType<PortalUI>() == null)
                new GameObject("PortalUI (Auto)").AddComponent<PortalUI>();
            // 金银细工台面板（JewelryStation · E）
            if (UnityEngine.Object.FindObjectOfType<JewelryUI>() == null)
                new GameObject("JewelryUI (Auto)").AddComponent<JewelryUI>();
            // 人物属性面板（V）——时间/荷包/状态/技艺/非遗进度/好感总览
            if (UnityEngine.Object.FindObjectOfType<CharacterPanelUI>() == null)
                new GameObject("CharacterPanelUI (Auto)").AddComponent<CharacterPanelUI>();
            // 健康系统（疾病/吃药）+ 采山 + 陷阱 + 衣柜（运行时补建单例）
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.HealthSystem>() == null)
                new GameObject("HealthSystem (Auto)").AddComponent<PBV.Systems.HealthSystem>();
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.ForageMountainSystem>() == null)
                new GameObject("ForageMountainSystem (Auto)").AddComponent<PBV.Systems.ForageMountainSystem>();
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.TrapSystem>() == null)
                new GameObject("TrapSystem (Auto)").AddComponent<PBV.Systems.TrapSystem>();
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.WardrobeSystem>() == null)
                new GameObject("WardrobeSystem (Auto)").AddComponent<PBV.Systems.WardrobeSystem>();
            if (UnityEngine.Object.FindObjectOfType<PBV.Systems.CageSystem>() == null)
                new GameObject("CageSystem (Auto)").AddComponent<PBV.Systems.CageSystem>();
            if (UnityEngine.Object.FindObjectOfType<MountainUI>() == null)
                new GameObject("MountainUI (Auto)").AddComponent<MountainUI>();
            if (UnityEngine.Object.FindObjectOfType<WardrobeUI>() == null)
                new GameObject("WardrobeUI (Auto)").AddComponent<WardrobeUI>();

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem (Auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("[UI] 已自动创建 EventSystem：界面按钮现在可以点击");
        }
    }
}
