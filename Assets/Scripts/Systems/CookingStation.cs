using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    [RequireComponent(typeof(Collider2D))]
    public class CookingStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "厨房：打开烹饪";
        public void OnInteract() { CookingUI.Instance?.Toggle(); }
    }
}
