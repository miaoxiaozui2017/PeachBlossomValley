using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>技能面板 UI：P 键开关，显示4个技能等级/经验/加成。</summary>
    public class SkillPanelUI : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas，关闭时一起隐藏
        private bool _isOpen;
        private static Font _font;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P) || (_isOpen && Input.GetKeyDown(KeyCode.Escape)))
            {
                _isOpen = !_isOpen;
                if (_root != null)
                {
                    _canvasGo.SetActive(_isOpen);
                    if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(ClosePanel);
                    else UIEventSystemBootstrap.ClearTopPanel();
                    if (_isOpen) Refresh();
                }
            }
        }

        private void ClosePanel()
        {
            _isOpen = false;
            if (_canvasGo != null) _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            if (SkillSystem.Instance == null) return;
            var content = _root.transform.Find("Content");
            if (content == null) return;

            // 清空旧内容（保留标题）
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
            {
                int level = SkillSystem.Instance.GetLevel(skill);
                int exp = SkillSystem.Instance.GetExp(skill);
                int expNext = SkillSystem.Instance.GetExpToNext(skill);
                string name = SkillSystem.SkillName(skill);
                string bonus = GetBonusText(skill, level);

                var row = new GameObject($"Skill_{skill}");
                row.transform.SetParent(content, false);
                var rt = row.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 56);
                var img = row.AddComponent<Image>();
                img.color = new Color(0.12f, 0.15f, 0.1f, 0.9f);

                var text = row.AddComponent<Text>();
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.font = _font;
                text.fontSize = 15;
                text.alignment = TextAnchor.MiddleLeft;
                text.color = new Color(1f, 0.92f, 0.75f);
                text.supportRichText = true;
                text.text = level >= 10
                    ? $"<b>{name}</b>  <color=#ffcc66>Lv.10 满级</color>\n{bonus}"
                    : $"<b>{name}</b>  Lv.{level}  ({exp}/{expNext})\n{bonus}";
                text.raycastTarget = false;
            }
        }

        private static string GetBonusText(SkillType skill, int level)
        {
            switch (skill)
            {
                case SkillType.Farming:
                    return level >= 3 ? "<color=#88ff88>已解锁: 收获产量+1</color>" : "Lv.3解锁: 产量+1";
                case SkillType.Mining:
                    return level >= 3 ? "<color=#88ff88>已解锁: 矿石产量+1</color>" : "Lv.3解锁: 产量+1";
                case SkillType.Fishing:
                    return level >= 3 ? "<color=#88ff88>已解锁: 稀有鱼+20%</color>" : "Lv.3解锁: 稀有鱼+20%";
                case SkillType.Cooking:
                    return level >= 3 ? "<color=#88ff88>已解锁: 体力恢复+20%</color>" : "Lv.3解锁: 恢复+20%";
                default: return "";
            }
        }

        private void Awake()
        {
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("SkillPanelCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 24;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(340, 300);
            rt.anchoredPosition = new Vector2(-20, 0);
            var img = _root.AddComponent<Image>();
            img.color = new Color(0.1f, 0.09f, 0.07f, 0.94f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(0, 28);
            var title = titleGo.AddComponent<Text>();
            title.font = _font; title.fontSize = 20; title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft; title.color = new Color(0.8f, 0.9f, 1f);
            title.text = "技能 (P/Esc 关闭)"; title.raycastTarget = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(_root.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(0, 250);
            contentGo.AddComponent<VerticalLayoutGroup>().spacing = 4;
            var cvlg = contentGo.GetComponent<VerticalLayoutGroup>();
            cvlg.childControlWidth = true; cvlg.childForceExpandWidth = true;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
