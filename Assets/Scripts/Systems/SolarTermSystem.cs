using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 24节气系统：根据当前季节+天数计算节气名称。
    /// 纯静态计算，无需 MonoBehaviour/存档。
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

        /// <summary>当前节气是否有浇水增益（雨水/谷雨）</summary>
        public static bool HasWaterBonus => CurrentTerm == "雨水" || CurrentTerm == "谷雨";

        /// <summary>当前节气是否有收获增益（芒种/秋分）</summary>
        public static bool HasHarvestBonus => CurrentTerm == "芒种" || CurrentTerm == "秋分";
    }
}
