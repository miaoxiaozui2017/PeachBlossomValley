using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 任务日志 UI：J 键开关，列出所有任务及进度。
    /// 灰盒自建。
    /// </summary>
    public class QuestLogUI : MonoBehaviour
    {
        private GameObject _root;
        private Transform _listContent;
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            BuildUI();
            _root.SetActive(false);
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestsChanged += Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                _isOpen = !_isOpen;
                _root.SetActive(_isOpen);
                if (_isOpen) Refresh();
            }
        }

        private void Refresh()
        {
            if (QuestManager.Instance == null || _listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            foreach (var q in QuestManager.Instance.Quests)
            {
                if (q.status == QuestStatus.Locked) continue;
                CreateQuestRow(q);
            }
        }

        private void CreateQuestRow(Quest q)
        {
            var row = new GameObject($"Quest_{q.id}");
            row.transform.SetParent(_listContent, false);
            var rt = row.AddComponent<RectTransform>();

            var img = row.AddComponent<Image>();
            Color c;
            switch (q.status)
            {
                case QuestStatus.Active: c = new Color(0.15f, 0.18f, 0.12f, 0.95f); break;
                case QuestStatus.Completed: c = new Color(0.12f, 0.2f, 0.12f, 0.95f); break;
                case QuestStatus.Rewarded: c = new Color(0.1f, 0.1f, 0.1f, 0.7f); break;
                default: c = new Color(0.1f, 0.1f, 0.1f, 0.95f); break;
            }
            img.color = c;

            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 6, 6);
            vlg.spacing = 2;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            // 标题行
            string statusIcon;
            switch (q.status)
            {
                case QuestStatus.Active: statusIcon = "[进行中]"; break;
                case QuestStatus.Completed: statusIcon = "[已完成]"; break;
                case QuestStatus.Rewarded: statusIcon = "[已领奖]"; break;
                default: statusIcon = ""; break;
            }
            var title = CreateText(row.transform, "Title", 16, FontStyle.Bold,
                q.status == QuestStatus.Rewarded ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.92f, 0.7f),
                $"{statusIcon} {q.title}");
            var titleRt = title.rectTransform;
            titleRt.sizeDelta = new Vector2(0, 22);

            // 描述
            var desc = CreateText(row.transform, "Desc", 13, FontStyle.Normal,
                new Color(0.75f, 0.7f, 0.62f), q.description);
            var descRt = desc.rectTransform;
            descRt.sizeDelta = new Vector2(0, 18);

            // 进度+奖励
            string progressText = q.status == QuestStatus.Active
                ? $"进度: {q.currentCount}/{q.targetCount}   奖励: ¥{q.rewardMoney}"
                : $"奖励: ¥{q.rewardMoney}";
            var prog = CreateText(row.transform, "Prog", 13, FontStyle.Normal,
                new Color(1f, 0.85f, 0.4f), progressText);
            var progRt = prog.rectTransform;
            progRt.sizeDelta = new Vector2(0, 18);
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("QuestLogCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(400, 500);
            rt.anchoredPosition = new Vector2(-20, 0);
            var img = _root.AddComponent<Image>();
            img.color = new Color(0.1f, 0.08f, 0.06f, 0.94f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var title = CreateText(_root.transform, "Title", 22, FontStyle.Bold,
                new Color(1f, 0.9f, 0.6f), "任务日志 (J)");
            var titleRt = title.rectTransform;
            titleRt.sizeDelta = new Vector2(0, 30);

            var hint = CreateText(_root.transform, "Hint", 12, FontStyle.Normal,
                new Color(0.6f, 0.55f, 0.5f), "完成任务自动领奖，解锁下一个");
            var hintRt = hint.rectTransform;
            hintRt.sizeDelta = new Vector2(0, 16);

            // 滚动列表
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0, 420);
            scrollGo.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.5f);
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 0);
            var cvlg = content.AddComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(4, 4, 4, 4);
            cvlg.spacing = 4;
            cvlg.childControlWidth = true;
            cvlg.childForceExpandWidth = true;
            var cf = content.AddComponent<ContentSizeFitter>();
            cf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = contentRt;
            _listContent = content.transform;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = _font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.UpperLeft;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
