namespace EchWorkersManager.Models
{
    public class ProxyConfig
    {
        public string Domain { get; set; } = "ech.sjwayrhz9.workers.dev:443";
        public string IP { get; set; } = "saas.sin.fan";
        public string Token { get; set; } = "miy8TMEisePcHp$K";
        public string LocalAddress { get; set; } = "127.0.0.1:30000";
        public int HttpProxyPort { get; set; } = 10809;
        public string RoutingMode { get; set; } = "绕过大陆";

        public string SocksHost
        {
            get
            {
                var parts = LocalAddress.Split(':');
                return parts.Length > 0 ? parts[0] : "127.0.0.1";
            }
        }

        public int SocksPort
        {
            get
            {
                var parts = LocalAddress.Split(':');
                return parts.Length > 1 ? int.Parse(parts[1]) : 30000;
            }
        }
    }
}