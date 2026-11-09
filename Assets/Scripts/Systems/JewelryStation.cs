using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>金银细工台：E 打开面板，金矿冶炼成金锭、金锭+宝石镶嵌成花丝珠宝（非遗产业链）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class JewelryStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "金银细工台：炼金 · 镶嵌珠宝 (E)";

        public void OnInteract()
        {
            if (JewelryUI.Instance != null) JewelryUI.Instance.Open();
        }
    }
}
