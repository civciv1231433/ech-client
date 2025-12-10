using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace EchWorkersManager.Routing
{
    public class RoutingManager
    {
        private readonly List<string> chinaIPPrefixes;
        private string routingMode;

        public RoutingManager()
        {
            chinaIPPrefixes = IPRangeData.GetChinaIPPrefixes();
            routingMode = "绕过大陆";
        }

        public void SetRoutingMode(string mode)
        {
            routingMode = mode;
        }

        public string GetRoutingMode()
        {
            return routingMode;
        }

        public bool ShouldProxy(string host)
        {
            if (routingMode == "直连模式")
            {
                return false;
            }

            IPAddress targetIP = null;
            bool isIpAddr = IPAddress.TryParse(host, out targetIP);

            if (!isIpAddr && routingMode == "绕过大陆")
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostAddresses(host);
                    if (ips.Length > 0)
                    {
                        targetIP = ips[0];
                    }
                }
                catch
                {
                    return true;
                }
            }

            if (targetIP != null && IsPrivateIP(targetIP))
            {
                return false;
            }

            if (routingMode == "全局模式")
            {
                return true;
            }

            if (routingMode == "绕过大陆")
            {
                if (targetIP != null && IsChinaIP(targetIP))
                {
                    return false;
                }
                return true;
            }

            return true;
        }

        private bool IsPrivateIP(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;

            byte[] bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }
            return false;
        }

        private bool IsChinaIP(IPAddress ip)
        {
            string ipStr = ip.ToString();
            foreach (var prefix in chinaIPPrefixes)
            {
                if (ipStr.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }
    }
}