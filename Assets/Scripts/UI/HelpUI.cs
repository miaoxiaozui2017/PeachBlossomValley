using UnityEngine;
using UnityEngine.UI;

namespace PBV.UI
{
    /// <summary>帮助页：H 键开关，显示全部快捷键和玩法说明。</summary>
    public class HelpUI : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                _isOpen = !_isOpen;
                _canvasGo.SetActive(_isOpen);
            }
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("HelpCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 半透明背景
            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700, 600); rt.anchoredPosition = Vector2.zero;
            var img = _root.AddComponent<Image>();
            img.color = new Color(0.08f, 0.07f, 0.05f, 0.97f);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one; scrollRt.sizeDelta = Vector2.zero;
            scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGo.AddComponent<ScrollRect>();
            sr.horizontal = false;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one; vpRt.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            sr.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f); contentRt.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 4; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = contentRt;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 标题
            CreateText(content.transform, 24, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "帮助 — 谷中仙 (H 关闭)");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f),
                "《谷中仙》：在桃花谷种田造纸，穿梭现代做贸易");

            CreateText(content.transform, 18, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "【快捷键】");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.85f, 0.82f, 0.75f),
                "WASD/方向键  8方向移动\n" +
                "Space  使用当前工具（锄地/浇水/播种/收获）\n" +
                "Q  切换工具（锄头→水壶→镰刀→种子）\n" +
                "E  交互（对话/出货箱/商店/制作台/床等）\n" +
                "G  送礼（靠近NPC，送当前选中槽位物品）\n" +
                "Tab  开关背包\n" +
                "1-0  选择快捷栏槽位\n" +
                "J  任务日志\n" +
                "C  造纸台（制作界面）\n" +
                "K  厨房（烹饪界面）\n" +
                "M  矿洞（挖矿界面）\n" +
                "U  铁匠铺（工具升级）\n" +
                "P  技能面板\n" +
                "B  建造台（房屋扩建）\n" +
                "T  从现代一键回桃花谷（在桃花谷按 T 只显示提示）\n" +
                "H  帮助页（本页面）\n" +
                "F5  存档\n" +
                "F9  读档\n" +
                "N  跳到次日（测试用）\n" +
                "Esc  关闭当前界面");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 18, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "【玩法指南】");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.85f, 0.82f, 0.75f),
                "1. 农耕：用锄头(Q切)在草地上按Space锄地→切种子按Space播种→切水壶按Space浇水→按N跳日等作物生长→成熟后切镰刀按Space收获\n" +
                "2. 出货：走到出货箱按E投入可售物品→按N跳日→次日06:00金钱到账\n" +
                "3. 商店：走到杂货铺(上方)按E购买种子和材料\n" +
                "4. 造纸：走到造纸台按E或C→树皮制浆→抄纸→制宣纸（高价出售）\n" +
                "5. 烹饪：走到厨房按E或K→消耗食材恢复体力\n" +
                "6. 钓鱼：走到钓鱼点(蓝色)按E→等待提示→按Space钓鱼\n" +
                "7. 挖矿：走到矿洞按E或M→消耗10体力获得矿石\n" +
                "8. 工具升级：走到铁匠铺按E或U→矿石+金钱升级。锄头/水壶/镰刀减少体力消耗；\n" +
                "   种子包 Lv2 一次播3格+商店种子9折，Lv3 一次播5格+8折（升级不是买种子，是让种子更省更值）\n" +
                "9. 房屋扩建：走到建造台按E或B→花钱扩容背包/提升体力上限\n" +
                "10. 送礼：选中背包物品→靠近NPC按G→好感度提升（名牌显示等级）\n" +
                "11. 穿梭贸易：走到传送门(下方紫)按E前往现代；仍是同一片农场，不换场景、不移动，\n" +
                "   所有功能照常可用（种地/商店/制作/任务/存档），唯一区别是投入出货箱时售价x2；\n" +
                "   回桃花谷：按 T，或走回传送门再按一次 E（右上角横幅会显示当前世界和回程方式）\n" +
                "12. 灵兽：走到灵兽祠按E领养猫→猫跟随+每日额外恢复体力\n" +
                "13. 技能：种植/采矿/钓鱼/烹饪各有等级，使用时获得经验，Lv3解锁加成\n" +
                "14. 季节：每季28天，作物有季节限定，第28天是节日（售价x1.5）\n" +
                "15. 睡觉：走到床(紫色)按E→恢复满体力+跳到次日\n" +
                "16. 结局：第112天(1年)触发结局（归隐/富商/凡人）\n" +
                "17. 体力：左上角数值+绿条实时变化；体力不够时工具完全无效并弹出提示，\n" +
                "   吃料理(K)、睡觉(床E)、按N跳日或建房屋扩建(B)可恢复/提升\n" +
                "18. 任务：右上角常驻显示当前任务进度，实时跳动；按J看完整日志，完成任务自动领奖并解锁下一个");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 14, FontStyle.Italic, new Color(0.5f, 0.5f, 0.5f),
                "地块高亮颜色：黄=可锄  青=可种  蓝=需浇水  绿=可收获  暗红=不可耕种\n" +
                "场景物体：床(玩家旁) 出货箱(左上) 老农(右上) 药婆(左下) 书生(右下)\n" +
                "商人(上方) 造纸台(右下) 厨房(左下) 钓鱼点(左下蓝) 矿洞(右上深)\n" +
                "铁匠铺(左上) 建造台(右上) 传送门(下方紫) 灵兽祠(右下黄绿)");
        }

        private void CreateText(Transform parent, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAnchor.UpperLeft;
            t.color = color;
            t.text = content;
            t.lineSpacing = 1.5f;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
