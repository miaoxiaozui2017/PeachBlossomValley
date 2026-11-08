using UnityEngine;
using UnityEngine.UI;
using PBV.Player;
using PBV.Core;

namespace PBV.UI
{
    /// <summary>
    /// 交互提示 UI：玩家靠近可交互对象（NPC/出货箱）时，屏幕底部显示 "[E] xxx"。
    /// 订阅 PlayerController.OnNearbyChanged，自动显隐。
    /// 纯展示层：排序低于所有功能面板，不挂 GraphicRaycaster，不拦截按钮点击。
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        private GameObject _root;
        private Text _promptText;

        private void Awake()
        {
            UIEventSystemBootstrap.Ensure();
            BuildUI();
            SetVisible(false);
        }

        private void Start()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.OnNearbyChanged += OnNearbyChanged;
        }

        private void OnDestroy()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.OnNearbyChanged -= OnNearbyChanged;
        }

        private void OnNearbyChanged(IInteractable target)
        {
            if (target == null)
            {
                SetVisible(false);
                return;
            }
            _promptText.text = $"[E] {target.GetInteractPrompt()}";
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("InteractionPromptUI_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // 低于所有功能面板（23~30），不会盖住面板
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // 纯展示层不挂 GraphicRaycaster

            _root = new GameObject("Prompt");
            _root.transform.SetParent(canvasGo.transform, false);
            var rect = _root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(300f, 32f);
            rect.anchoredPosition = new Vector2(0f, 74f);

            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.04f, 0.85f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            _promptText = textGo.AddComponent<Text>();
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 17;
            _promptText.fontStyle = FontStyle.Bold;
            _promptText.color = new Color(1f, 0.93f, 0.7f);
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _promptText.verticalOverflow = VerticalWrapMode.Overflow;
            _promptText.raycastTarget = false;
        }
    }
}
