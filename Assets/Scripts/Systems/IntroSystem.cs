using UnityEngine;
using UnityEngine.UI;
using PBV.Save;

namespace PBV.Systems
{
    /// <summary>
    /// 开场介绍：游戏首次启动时显示背景文字，按任意键关闭。
    /// 有存档时跳过。
    /// </summary>
    public class IntroSystem : MonoBehaviour
    {
        private GameObject _root;
        private bool _active;
        private float _delayTimer = 0.5f; // 防止开场瞬间误触关闭

        private void Start()
        {
            // 有存档时跳过开场
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile)
            {
                Destroy(gameObject);
                return;
            }
            ShowIntro();
        }

        private void Update()
        {
            if (!_active) return;
            _delayTimer -= Time.deltaTime;
            if (_delayTimer > 0) return;

            if (Input.anyKeyDown)
            {
                CloseIntro();
            }
        }

        private void ShowIntro()
        {
            _active = true;

            var canvasGo = new GameObject("IntroCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var img = _root.AddComponent<Image>();
            img.color = new Color(0.05f, 0.04f, 0.03f, 0.98f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(120, 120, 80, 80);
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root.transform, false);
            titleGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            var title = titleGo.AddComponent<Text>();
            title.font = font; title.fontSize = 36; title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.5f);
            title.text = "桃 花 谷";
            title.raycastTarget = false;

            // 正文
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(_root.transform, false);
            bodyGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 200);
            var body = bodyGo.AddComponent<Text>();
            body.font = font; body.fontSize = 20;
            body.alignment = TextAnchor.MiddleCenter;
            body.color = new Color(0.85f, 0.82f, 0.75f);
            body.lineSpacing = 1.5f;
            body.text = "大梦初醒，你发现自己置身于一个陌生的山谷。\n\n桃花漫山，溪水潺潺，几间茅屋炊烟袅袅。\n一位老农走来，说这片荒地三年无人耕种，如今归你了。\n\n锄地、播种、浇水、收获——\n造纸、烹饪、钓鱼、挖矿——\n桃花谷的故事，从你手中开始。\n\n（按任意键继续）";
            body.raycastTarget = false;
        }

        private void CloseIntro()
        {
            _active = false;
            if (_root != null) Destroy(_root);
            Destroy(gameObject);
        }
    }
}
