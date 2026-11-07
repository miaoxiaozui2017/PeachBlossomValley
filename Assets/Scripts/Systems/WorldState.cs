using System;
using UnityEngine;

namespace PBV.Systems
{
    public enum WorldType { Ancient, Modern }

    /// <summary>
    /// 世界状态：古代桃花谷 ⇄ 现代世界。
    /// 现代世界出货售价x2（跨时空倒卖核心玩法）。
    /// 存档支持。
    /// </summary>
    public class WorldState : MonoBehaviour
    {
        public static WorldState Instance { get; private set; }

        public WorldType Current { get; private set; } = WorldType.Ancient;
        public float SellMultiplier => Current == WorldType.Modern ? 2f : 1f;

        public event Action<WorldType> OnWorldChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public void Toggle()
        {
            Current = Current == WorldType.Ancient ? WorldType.Modern : WorldType.Ancient;
            OnWorldChanged?.Invoke(Current);
            Debug.Log($"[World] 穿越到 {(Current == WorldType.Ancient ? "古代·桃花谷" : "现代·现实世界")}（售价x{SellMultiplier}）");
        }

        public void Restore(int world) { Current = (WorldType)world; }

        public string WorldName => Current == WorldType.Ancient ? "桃花谷" : "现代";
    }
}
