using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 24节气系统：根据当前季节+天数计算节气名称，并给出该节气的加成与小字提示。
    /// 纯静态计算，无需 MonoBehaviour/存档。
    ///
    /// 三类实际生效的加成（HUD 会用小字提示当前节气加成）：
    /// - 雨水/谷雨：浇水不耗体力（FarmGrid 浇水分支读取 HasWaterBonus）
    /// - 立春/立夏/立秋：作物额外多长 1 天（CropManager.OnNewDay 读取 GrowthBonus）
    /// - 芒种/秋分/寒露：收获产量 +1（CropManager 收获分支读取 HarvestBonus）
    /// 其余节气为农事平稳期，无数值加成，但仍有提示文案。
    /// </summary>
    public static class SolarTermSystem
    {
        private static readonly string[,] Terms =
        {
            { "立春", "雨水", "惊蛰", "春分", "清明", "谷雨" },
            { "立夏", "小满", "芒种", "夏至", "小暑", "大暑" },
            { "立秋", "处暑", "白露", "秋分", "寒露", "霜降" },
            { "立冬", "小雪", "大雪", "冬至", "小寒", "大寒" }
        };

        /// <summary>当前节气名称</summary>
        public static string CurrentTerm
        {
            get
            {
                if (SeasonSystem.Instance == null) return "";
                int seasonIdx = (int)SeasonSystem.Instance.CurrentSeason;
                int day = SeasonSystem.Instance.DayOfSeason;
                // 28天/季 分6个节气: 1-4/5-9/10-14/15-19/20-24/25-28
                int termIdx = Mathf.Min(5, day / 5);
                return Terms[seasonIdx, termIdx];
            }
        }

        /// <summary>浇水增益：雨水/谷雨，浇水不耗体力</summary>
        public static bool HasWaterBonus => CurrentTerm == "雨水" || CurrentTerm == "谷雨";

        /// <summary>收获增益：芒种/秋分/寒露，产量 +1</summary>
        public static bool HasHarvestBonus => HarvestBonus > 0;

        /// <summary>收获额外产量</summary>
        public static int HarvestBonus =>
            (CurrentTerm == "芒种" || CurrentTerm == "秋分" || CurrentTerm == "寒露") ? 1 : 0;

        /// <summary>生长增益：立春/立夏/立秋，浇水后额外多长 1 天</summary>
        public static int GrowthBonus =>
            (CurrentTerm == "立春" || CurrentTerm == "立夏" || CurrentTerm == "立秋") ? 1 : 0;

        /// <summary>
        /// 当前节气加成的小字提示（HUD 常驻显示）。
        /// 有数值加成的节气写明具体效果，其余给简短农事提示。
        /// </summary>
        public static string BonusText
        {
            get
            {
                switch (CurrentTerm)
                {
                    // 有数值加成
                    case "雨水": case "谷雨": return "浇水不耗体力";
                    case "立春": case "立夏": case "立秋": return "浇灌后作物多长 1 天";
                    case "芒种": case "秋分": case "寒露": return "收获产量 +1";
                    // 农事平稳期
                    case "惊蛰": return "春雷动·宜整地备种";
                    case "春分": return "昼夜均·宜播种";
                    case "清明": return "气清景明·宜施肥";
                    case "小满": return "麦粒渐盈·宜浇水";
                    case "夏至": return "日最长·作物需水多";
                    case "小暑": case "大暑": return "暑气盛·注意体力";
                    case "处暑": return "暑气止·宜秋播";
                    case "白露": return "露凝而白·宜收秋菜";
                    case "霜降": return "初霜·宜抢收";
                    case "立冬": return "入冬·宜囤粮";
                    case "小雪": case "大雪": return "雨雪·农事平稳";
                    case "冬至": return "日最短·宜休整";
                    case "小寒": case "大寒": return "严寒·农事平稳";
                    default: return "农事平稳·无特殊加成";
                }
            }
        }
    }
}
