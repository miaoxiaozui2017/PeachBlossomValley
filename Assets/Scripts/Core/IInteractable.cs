namespace PBV.Core
{
    /// <summary>
    /// 可交互对象统一接口：NPC、出货箱、商店等挂载实现
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 玩家按 E 触发交互
        /// </summary>
        void OnInteract();

        /// <summary>
        /// 交互提示文本（显示在头顶，如"打开出货箱"）
        /// </summary>
        string GetInteractPrompt();
    }
}
