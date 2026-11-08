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
                "鼠标左键  使用当前工具；成熟作物左键直接收（不必切镰刀）；点在界面上不触发\n" +
                "Space  只在钓鱼时收竿，不再触发农事（与工具彻底分离）\n" +
                "Q  切换工具（锄头→水壶→镰刀→种子）\n" +
                "E  交互 / 关闭由 E 打开的功能面板（Esc 也可关）\n" +
                "G  送礼（靠近NPC，送当前选中槽位物品）\n" +
                "F  吃选中的料理（选中槽不是食物则自动找背包第一份）\n" +
                "Tab  开关背包\n" +
                "1-0  选择快捷栏槽位\n" +
                "J  任务日志      L  成就面板      O  声望/订单/增益\n" +
                "R  穿梭贸易槽位（三出三进）      Y  收集图鉴 / 鱼塘 / 彩蛋\n" +
                "C  造纸台    K  厨房    M  矿洞    U  铁匠铺    P  技能    B  建造台\n" +
                "T  从现代一键回桃花谷（在桃花谷按 T 只显示提示）\n" +
                "H  帮助页（本页面）\n" +
                "F5  存档      F9  读档      N  跳到次日（只回一半体力，测试用）\n" +
                "Esc  关闭当前界面");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 18, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "【玩法指南】");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.85f, 0.82f, 0.75f),
                "1. 农耕：Q 切锄头左键锄地→切种子左键播种→切水壶左键浇水→N 跳日生长→成熟后左键直接收\n" +
                "   （成熟作物不必切镰刀，也不用再浇水；只有未成熟作物需要每天浇水才长）\n" +
                "2. 雨天：全田自动算作浇过水，地块显示湿润，不用拿水壶；次日仍要浇（除非又下雨）\n" +
                "3. 出货：走到出货箱按 E → 次日 06:00 入账，是现代/节日高价渠道\n" +
                "4. 出售规则：可卖 = 作物/鱼/矿石/纸品/料理（售价>0）；不可卖 = 工具、种子、无售价材料\n" +
                "5. 商店回收：杂货铺按 E 打开 → 点「回收背包物品」→ 按基础价当天即时到账（急用钱用）\n" +
                "6. 商店：杂货铺按 E 买种子/材料；种子包升级后买种子打折\n" +
                "7. 造纸：造纸台 E/C → 树皮制浆→抄纸→宣纸（高价出售）\n" +
                "8. 烹饪：厨房 E/K → 做出「菜」放进背包，不会立刻回体力；按 F 才吃，也能送礼或卖掉\n" +
                "   含 3 道鱼料理（清蒸鱼/鱼头萝卜汤/红烧鱼），任意钓到的鱼都能用\n" +
                "9. 钓鱼：钓鱼点(蓝色)按 E → 等「！快按 Space！」时按 Space 收竿；钓鱼中左键不会误锄地\n" +
                "10. 挖矿：矿洞 E/M → 消耗 10 体力获得矿石（铁匠铺升级要用）\n" +
                "11. 工具升级：铁匠铺 E/U → 锄头/水壶/镰刀减体力消耗（HUD 数字实时跟着变）；\n" +
                "   种子包 Lv2 一次播 3 格 + 商店种子 9 折，Lv3 一次播 5 格 + 8 折\n" +
                "12. 房屋扩建：建造台 E/B → 背包 24→36→48 格（Tab 里格子会真实变多）/ 体力上限 100→150→200\n" +
                "13. 送礼：选中背包物品 → 靠近 NPC 按 G → 好感 +物品的好感值（最低 +1，上限 100）\n" +
                "   等级：0-9 陌生 / 10-29 认识 / 30-59 友好 / 60-89 亲密 / 90-100 挚友（NPC 名牌显示）\n" +
                "14. 穿梭贸易：北侧功能台区的传送门按 E 去现代；仍是同一片农场，不换场景、所有功能照常可用\n" +
                "   回桃花谷：按 T，或走回传送门再按 E\n" +
                "15. 售价怎么算：出货箱 = 物品售价 × 数量 ×（现代 ×2 / 桃花谷 ×1）×（节日 ×1.5），\n" +
                "   在「投入那一刻」锁定，次日 06:00 入账；商店回收只按基础价、不乘倍率\n" +
                "16. 灵兽：灵兽祠 E 领养猫 → 跟随 + 每日额外恢复体力\n" +
                "17. 技能：种植/采矿/钓鱼/烹饪各有等级，使用时得经验，Lv3 解锁加成\n" +
                "18. 季节：每季 28 天，作物有季节限定，第 28 天节日（售价 ×1.5）\n" +
                "19. 睡觉 vs 跳日：床 E = 体力回满 + 跳次日；N 直接跳日只回一半体力\n" +
                "20. 成就：按 L 看 7 个成就与解锁进度，达成时屏幕弹提示，进度会存档\n" +
                "21. 体力：左上角数值 + 绿条实时变化；体力不足时工具完全无效并弹提示\n" +
                "22. 任务：右上角常驻进度实时跳动；J 看完整日志，完成自动领奖并解锁下一个\n" +
                "23. 结局：第 112 天(1年)触发结局（归隐/富商/凡人）\n" +
                "24. 四大节日：每季第 28 天，春=春节(送礼好感×2) / 夏=端午(稀有鱼+25%) /\n" +
                "   秋=中秋(料理×1.5) / 冬=元宵(挖矿+1)，均出货×1.5 并发现金节礼\n" +
                "25. 矿洞 10 层：M 面板「下潜一层」，越深稀有矿概率越高，单次体力 10→28\n" +
                "26. 三条钓点：溪/河/湖，鱼池依次更稀有；共 12 种鱼，任意鱼可做 3 道鱼料理\n" +
                "27. 草木扎染：杂货铺买蓝草 → 造纸台/染坊做 素色布→纹样布→成衣（高价/送礼）\n" +
                "28. 宗族声望：完成订单/穿梭贸易提升，出货售价最高 +50%（按 O 查看）\n" +
                "29. 餐厅订单：按 O 看今日 NPC 点的菜，做出后点「交付订单」领钱+声望+增益\n" +
                "30. 菜品增益：按 F 吃菜后获得钓鱼/挖矿/收获/贸易加成，持续到次日\n" +
                "31. 穿梭贸易：按 R 装入最多 3 件货物 → 传送门 E 穿越时自动结算；\n" +
                "   文物×5 / 古代珍品×3 / 违禁品×0.5 / 普通×1.5，去现代再×2\n" +
                "32. 节气随机事件：每天随机一个（商店溢价/鱼汛/赠礼/声望/矿脉），按 O 查看\n" +
                "33. 收集图鉴：按 Y 看图鉴，物品进过背包就永久记录（共 70+ 种）\n" +
                "34. 鱼塘：按 Y → 选中背包里的鱼点「投入」→ 跳次日 → 点「收获」得 2 条\n" +
                "35. 隐藏彩蛋：见闻 20 种 + 声望 200 + 成就 4 个 → 古画重现，奖 ¥5000");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 14, FontStyle.Italic, new Color(0.5f, 0.5f, 0.5f),
                "地块高亮颜色：黄=可锄  青=可种  蓝=需浇水  绿=可收获  暗红=不可耕种\n" +
                "【测试布局】所有交互点集中在地图北侧三排，每 4 格一个，头顶有「名字 + 快捷键」标签。\n" +
                "第1排：出货箱 / 杂货铺 / 造纸台 / 厨房 / 溪钓点 / 矿洞 / 铁匠铺\n" +
                "第2排：床 / 建造台 / 传送门 / 灵兽祠 / 老农 / 药婆 / 书生\n" +
                "第3排：染坊 / 河钓点 / 湖钓点 / 阿蓝 / 柳娘 / 守卷道人 / 老窑\n" +
                "农田在中间与南侧，功能台不占用耕地；地形生成留到下一阶段");
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
