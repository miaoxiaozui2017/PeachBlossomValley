using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 货币类型：宋币（桃花谷·宋代，单位「文」）/ 人民币（现代，单位「元」）
    /// </summary>
    public enum Currency { Song, RMB }

    /// <summary>
    /// 货币兑换中心：维持宋币 ⇄ 人民币的汇率换算。
    /// 实际兑换交互在 PortalUI（时空驿站）中完成，本类只提供纯换算与统一文案。
    /// </summary>
    public static class ExchangeSystem
    {
        /// <summary>汇率：50 文 = 1 元（1 贯 = 10 元）。桃花谷物产便宜，现代资源贵。</summary>
        public const int SONG_PER_RMB = 50;

        public static int SongToRMB(int song) => Mathf.FloorToInt((float)song / SONG_PER_RMB);
        public static int RMBToSong(int rmb) => rmb * SONG_PER_RMB;

        public static string Format(int amount, Currency c)
            => c == Currency.RMB ? $"¥{amount}" : $"{amount} 文";

        public static string RateText() => $"{SONG_PER_RMB} 文 = ¥1";
    }
}
