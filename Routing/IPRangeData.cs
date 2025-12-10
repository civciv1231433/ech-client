using System.Collections.Generic;

namespace EchWorkersManager.Routing
{
    public static class IPRangeData
    {
        public static List<string> GetChinaIPPrefixes()
        {
            return new List<string>
            {
                "1.0.", "14.", "27.", "36.", "42.",
                "58.", "59.", "60.", "61.",
                "110.", "111.", "112.", "113.",
                "114.", "115.", "116.", "117.",
                "118.", "119.", "120.", "121.",
                "122.", "123.", "124.", "125.",
                "180.", "182.", "183.", "202.", "203.",
                "210.", "211.", "218.", "219.", "220.", "221.", "222.", "223."
            };
        }
    }
}