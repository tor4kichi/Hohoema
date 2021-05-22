using System.ComponentModel;

namespace NiconicoToolkit.Video.Ranking
{
    public enum RankingTerm
    {
        [Description("hour")]
        Hour,
        [Description("24h")]
        Day,
        [Description("week")]
        Week,
        [Description("month")]
        Month,
        [Description("total")]
        Total,
    }
}
