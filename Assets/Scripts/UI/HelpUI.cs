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
                "1-0  调用快捷栏物品（选中格子并同步工具；左键使用工具/种子/肥料，食物左键或 F 食用）\n" +
                "J  任务日志      L  成就面板      O  声望/订单/增益\n" +
                "R  穿梭贸易槽位（三出三进）      Y  收集图鉴 / 鱼塘 / 彩蛋\n" +
                "C  全部制作配方总览（各建筑按 E 只看本台配方）    K  厨房    M  矿洞    U  铁匠铺\n" +
                "P  技能    V  人物面板    B  建造台\n" +
                "T  从现代一键回桃花谷（在桃花谷按 T 只显示提示）\n" +
                "H  帮助页（本页面）\n" +
                "F5  存档      F9  读档      N  跳到次日（只回一半体力，测试用）\n" +
                "Esc  关闭当前界面");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 18, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "【玩法指南】");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.85f, 0.82f, 0.75f),
                "1. 农耕：工具/种子/肥料都放进快捷栏格子，选中后左键使用——锄头锄地、种子播种、水壶浇水；\n" +
                "   N 跳日生长，成熟后左键直接收（不必切镰刀，也不用再浇水；未成熟作物需每天浇水才长）\n" +
                "2. 雨天：全田自动算作浇过水，地块显示湿润，不用拿水壶；次日仍要浇（除非又下雨）\n" +
                "3. 出货：走到出货箱按 E → 次日 06:00 入账，是现代/节日高价渠道\n" +
                "4. 出售规则：可卖 = 作物/鱼/矿石/纸品/料理（售价>0）；不可卖 = 工具、种子、无售价材料\n" +
                "5. 商店回收：杂货铺按 E 打开 → 点「回收背包物品」→ 按基础价当天即时到账（急用钱用）\n" +
                "6. 商店：杂货铺按 E 买种子/材料；种子买了就能种，选中种子格子左键播种\n" +
                "7. 制作台分工：每个建筑按 E 只显示「本台专属配方」，C 键打开全部配方总览（标注归属建筑）；\n" +
                "   造纸台(纸浆/宣纸)、染坊(扎染)、陶坊(青瓷)、药庐(药墨/制药)、木工坊(木梁/家具/储物箱)、\n" +
                "   磨坊(面粉/油)、酱园(酱油/陈醋/酱菜)、茶坊(茶饼/茶饮)、制糖坊(糖)、盐坊(盐)；\n" +
                "   酱菜：白菜/萝卜/黄瓜 ×2 + 盐 ×1 → 酱菜 ×2（酱园 E）；造纸台 E → 树皮制浆→抄纸→宣纸\n" +
                "8. 烹饪：厨房 E/K → 做出「菜」放进背包，不会立刻回体力；按 F 才吃，也能送礼或卖掉\n" +
                "   含 3 道鱼料理（清蒸鱼/鱼头萝卜汤/红烧鱼），任意钓到的鱼都能用\n" +
                "9. 钓鱼：钓鱼点(蓝色)按 E → 等「！快按 Space！」时按 Space 收竿；钓鱼中左键不会误锄地\n" +
                "10. 挖矿：矿洞 E/M → 消耗 10 体力获得矿石（铁匠铺升级要用）\n" +
                "11. 工具升级：铁匠铺 E/U → 锄头/水壶/镰刀/斧头减体力消耗（HUD 数字实时跟着变）\n" +
                "12. 房屋扩建：建造台 E/B → 用木梁/木家具扩建，解锁更多屋内装修格子（不与储物/体力挂钩）；\n" +
                "   房屋入口 E 进屋内装修。储物箱已独立：木工坊制作「储物箱」→ 选中后左键进入放置模式 →\n" +
                "   再左键点击地图放置（房屋内外皆可、可放多个、每箱 36 格、E 打开可改名改色）\n" +
                "13. 送礼：选中背包物品 → 靠近 NPC 按 G → 好感 +物品的好感值（最低 +1，上限 100）\n" +
                "   等级：0-9 陌生 / 10-29 认识 / 30-59 友好 / 60-89 亲密 / 90-100 挚友（NPC 名牌显示）\n" +
                "14. 穿梭贸易：北侧功能台区的传送门按 E 去现代；仍是同一片农场，不换场景、所有功能照常可用\n" +
                "   回桃花谷：按 T，或走回传送门再按 E\n" +
                "15. 售价怎么算：出货箱 = 物品售价 × 数量 ×（现代 ×2 / 桃花谷 ×1）×（节日 ×1.5），\n" +
                "   在「投入那一刻」锁定，次日 06:00 入账；商店回收只按基础价、不乘倍率\n" +
                "16. 灵兽：灵兽祠 E 领养猫 → 跟随 + 每日额外恢复体力\n" +
                "17. 技能：种植/采矿/钓鱼/烹饪各有等级，使用时得经验，Lv3 解锁加成\n" +
                "18. 季节：每季 28 天，作物有季节限定，第 28 天节日（售价 ×1.5）\n" +
                "    24 节气（每季 6 个）加成常驻显示在 HUD 右上角小字：\n" +
                "    雨水/谷雨=浇水不耗体力；立春/立夏/立秋=浇灌后多长 1 天；芒种/秋分/寒露=收获 +1；\n" +
                "    其余节气为农事平稳期，小字给出当令农事提示\n" +
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
                "35. 隐藏彩蛋：见闻 20 种 + 声望 200 + 成就 4 个 → 古画重现，奖 5000文");

            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 18, FontStyle.Bold, new Color(0.8f, 0.9f, 1f), "【货币 · 兑换 · 非遗】");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.85f, 0.82f, 0.75f),
                "36. 双币种：桃花谷(宋代)用「宋币/文」，现代用「人民币/¥」。HUD 与背包顶部显示「宋 X文  ¥Y」\n" +
                "37. 汇率：50 文 = ¥1（零损耗整额兑换）。现代卖货/节日/订单/贸易结算给 ¥；桃花谷卖货/订单给文\n" +
                "38. 兑换：时空驿站（传送门 E）面板里可「宋币全兑人民币 / 人民币全兑宋币」；穿梭世界不自动换汇\n" +
                "39. 现代高产资源：用 ¥ 在杂货铺买「活力饮料(+50体力)/能量胶(+100体力)/增产营养剂( harvest+1 增益)」\n" +
                "   增产营养剂按 F 吃下后，当天收获作物额外 +1（对应非遗/现代农业的高产思路）\n" +
                "40. 金银细工（非遗产业链）：矿洞深处挖到「金矿」→ 金银细工台(金银细工台 E) 3 金矿 → 1 金锭；\n" +
                "   再往下挖到「宝石矿」，1 金锭 + 1 宝石 → 1 花丝珠宝（高价出售 / 送礼，非遗花丝镶嵌工艺）\n" +
                "41. 货币锁定：出货箱在「投入那一刻」按当前世界锁定币种（现代→¥、桃花谷→文），次日 06:00 入账\n" +
                "42. 古董：矿洞越深越易挖到「古陶罐/青铜古器/古玉佩」等古董（文物），越深越珍贵；可带去现代卖高价\n" +
                "43. 文玩字画：把任意 NPC 好感送到「亲密/挚友」时，对方会赠你「名家字画/古玉文玩」（文物，现代高价）\n" +
                "44. 文物溢价：古董/字画属「珍贵文物」，穿梭贸易(R)带去现代结算 ×5，或直接在现代出货箱卖 ×2（¥）\n" +
                "45. 黑科技图纸：挣到的 ¥ 在现代杂货铺买「丰产/超导/商路图纸」，按 F 研究获得对应增益（持续3天）\n" +
                "   丰产=收获+1 / 超导=挖矿+1 / 商路=贸易+20%，用 ¥ 把古代宝藏变成现代生产力\n" +
                "46. 人物面板（V）：一屏看全 时日/天气/世界 · 宋币与人民币 · 体力/声望/见闻/成就 · 增益 ·\n" +
                "   四项技艺等级与加成 · 五条非遗产业链进度 · 全村 8 人好感排行。E/Esc 关闭\n" +
                "47. 陶瓷（非遗）：杂货铺买「陶土」→ 陶坊(E) 淘土制坯 → 拉坯成罐 / 烧青瓷盏 / 烧青花瓷瓶\n" +
                "48. 药墨·药酒（非遗）：杂货铺买「药材」→ 药庐(E) 和药制墨 / 浸酿药酒(回体力) / 熬制膏药\n" +
                "49. 住：木工坊(E) 木材→木梁→木家具（木家具可高价出售，也是上好的家礼）\n" +
                "50. 采集：野地采集/林间采集 按 E，每天各 3 次、跨日恢复，出野菜/野果/药材/木材/树皮/蘑菇\n" +
                "51. 家常菜：野菜汤(野菜×2) / 野果茶(野果×2)，不靠种子也能开局吃上热饭\n" +
                "52. 健康疾病：淋雨(雨天不睡觉)得风寒、喝酒过量醉酒、吃生野味腹泻；按 V 看健康状态\n" +
                "53. 用药治病：按 F 吃对应药——姜汤/风寒药治风寒、醒酒汤解醉、止泻药治腹泻、跌打膏治跌打\n" +
                "54. 制药·制毒：药庐(E) 药材/姜/酒/毒草 → 风寒药/止泻药/跌打膏/醒酒汤/毒饵/迷药\n" +
                "55. 采山：采山入口(E) 采山货/药材/野味，每天5次；稀有山参(4%)/云雾茶/野蜂蜜/毒草\n" +
                "56. 陷阱：布陷阱(E) 用毒饵/迷药下陷阱，次日随机捕获 野兔/野鸡/野鹿（做野味菜或卖钱）\n" +
                "57. 野味菜：野兔→红烧野兔、野鸡+山珍菌→野鸡菌汤、野鹿→烤鹿肉（大补，挖矿加成）\n" +
                "58. 衣柜换装：衣柜(E) 切换服装颜色（素衣/成衣/锦衣/夜行衣/道袍/罗裙）\n" +
                "59. 米酒：酿米酒(水稻×3+野果) 可作药引(醒酒汤)或待客，过量饮酒会醉\n" +
                "60. 磨坊/酱园/茶坊：小麦→面粉、大豆→豆油、玉米→玉米油（磨坊）；大豆→酱油/陈醋（酱园）；野茶→茶饼/茶饮（茶坊）\n" +
                "   小麦/大豆/玉米都可自己种（杂货铺买种子），也可在杂货铺直接买现成原料\n" +
                "61. 伐木采摘：Q 切换到斧头 → 砍树(E) 得木材(伐木Lv3+1)；不持斧只可采浆果\n" +
                "62. 笼箱抓水产：水域放笼箱(E) 用毒饵/野菜/蘑菇/水稻做饵，次日收 河虾/河蟹/河蚌/田螺/黄鳝\n" +
                "63. 海鲜菜：河虾→油焖河虾、河蟹→清蒸河蟹、河蚌→河蚌汤、田螺→爆炒田螺、黄鳝→红烧黄鳝\n" +
                "64. 面点主食：面粉→阳春面/白菜饺子/葱油饼，大豆→麻婆豆腐，茶叶酱油→茶叶蛋\n" +
                "65. 好感解锁食谱：与「药婆」交到亲密(60)解锁 药膳粥/药膳鸡汤/药酒酿鸭（强增益食物）\n" +
                "66. 可种植粮油作物：小麦（秋）/ 大豆（夏）/ 玉米（秋）都能种；小麦磨面粉、大豆/玉米榨油\n" +
                "67. 施肥：肥料是背包物品——Tab 打开背包选中「堆肥/现代复合肥」，左键对着「没有种子的已耕地」即可施肥；\n" +
                "   堆肥(产量+1)在堆肥桶(E)用稻草×3或树皮×2沤制，现代复合肥(产量×3)在现代商店用 ¥ 买\n" +
                "68. 土地污染：现代复合肥会污染土地——收获后该地块需「空置祛毒」3 天才能再种；\n" +
                "   期间不可播种，逐日自动恢复（堆肥是天然肥料，不污染）\n" +
                "69. 养殖业：畜栏(E) 买 鸡/鸭/牛/羊/猪 → 每天喂食(玉米/稻草/蔬菜) → 明早收 蛋/奶/毛\n" +
                "   连续喂 2 天再屠宰多得 1 块肉（鸡鸭→鸡肉、猪→猪肉、羊→羊肉、牛→牛肉）\n" +
                "70. 养殖菜谱：鸡蛋→炒鸡蛋/蒸蛋羹、鸭蛋→咸鸭蛋、牛奶→奶茶、猪肉→红烧肉、羊肉→羊肉汤、牛肉→牛肉面、鸡肉→烤鸡\n" +
                "71. 制糖制盐：甘蔗(夏)/甜菜(秋) 可种，制糖坊榨糖；矿洞挖「盐石」→ 盐坊煮盐（盐/糖是调味料）\n" +
                "72. 酒楼（大厨饱饱）：E 买卖 生鲜原料（蛋奶肉）+ 招牌菜 + 调味料；饱饱还收你种的菜和手艺\n" +
                "73. 老农小摊：卖 少量基础种子 + 自制堆肥（老农站在摊旁，交易走摊位）\n" +
                "74. 采矿遇怪：矿洞挖矿越深越易遇怪（矿工僵尸/岩洞蝙蝠/岩蛇），回合制战斗：攻击/用药/逃跑\n" +
                "   人物有血条(V 键面板可见)，战斗掉血、跌打膏回血；睡觉回满、跳日回一半\n" +
                "75. 晕倒：体力耗尽会晕倒，被送到药婆处——收诊费 200 文清醒、恢复 50% 体力；\n" +
                "   钱不够则「背债」欠药婆，下次有钱先还");
            CreateText(content.transform, 14, FontStyle.Normal, new Color(0.6f, 0.55f, 0.5f), "");
            CreateText(content.transform, 14, FontStyle.Italic, new Color(0.5f, 0.5f, 0.5f),
                "地块高亮颜色：黄=可锄  青=可种  蓝=需浇水  绿=可收获  暗红=不可耕种\n" +
                "【测试布局】所有交互点集中在地图北侧四排，每 4 格一个，头顶有「名字 + 快捷键」标签。\n" +
                "第1排：出货箱 / 杂货铺 / 造纸台 / 厨房 / 溪钓点 / 矿洞 / 铁匠铺\n" +
                "第2排：床 / 建造台 / 传送门 / 灵兽祠 / 老农 / 药婆 / 书生\n" +
                "第3排：染坊 / 河钓点 / 湖钓点 / 阿蓝 / 柳娘 / 守卷道人 / 老窑 / 饱饱(大厨) / 酒楼\n" +
                "第4排：金银细工台 / 陶坊 / 药庐 / 木工坊 / 野地采集 / 林间采集\n" +
                "第5排：采山入口 / 布陷阱 / 衣柜\n" +
                "第6排：磨坊 / 酱园 / 茶坊 / 砍树 ×2 / 放笼箱 / 堆肥桶\n" +
                "第7排：房屋入口 / 畜栏 / 制糖坊 / 盐坊\n" +
                "第8排：老农小摊（卖种子+堆肥）\n" +
                "农田在中间与南侧，功能台不占用耕地；地形生成留到下一阶段\n" +
                "NPC 有作息：白天(6-18点)在建筑上班，晚上回家休息");
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
