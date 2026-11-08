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

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem (Auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("[UI] 已自动创建 EventSystem：界面按钮现在可以点击");
        }
    }
}
