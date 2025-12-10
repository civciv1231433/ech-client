using EchWorkersManager.Models;
using Microsoft.Win32;

namespace EchWorkersManager.Helpers
{
    public static class SettingsHelper
    {
        private const string RegistryPath = "Software\\EchWorkersManager";

        public static void Save(ProxyConfig config)
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.CreateSubKey(RegistryPath);
                registry.SetValue("Domain", config.Domain);
                registry.SetValue("IP", config.IP);
                registry.SetValue("Token", config.Token);
                registry.SetValue("Local", config.LocalAddress);
                registry.SetValue("HttpPort", config.HttpProxyPort.ToString());
                registry.SetValue("RoutingMode", config.RoutingMode);
                registry.Close();
            }
            catch { }
        }

        public static ProxyConfig Load()
        {
            ProxyConfig config = new ProxyConfig();
            
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (registry != null)
                {
                    string domain = registry.GetValue("Domain") as string;
                    string ip = registry.GetValue("IP") as string;
                    string token = registry.GetValue("Token") as string;
                    string local = registry.GetValue("Local") as string;
                    string httpPort = registry.GetValue("HttpPort") as string;
                    string routingMode = registry.GetValue("RoutingMode") as string;

                    if (!string.IsNullOrEmpty(domain)) config.Domain = domain;
                    if (!string.IsNullOrEmpty(ip)) config.IP = ip;
                    if (!string.IsNullOrEmpty(token)) config.Token = token;
                    if (!string.IsNullOrEmpty(local)) config.LocalAddress = local;
                    if (!string.IsNullOrEmpty(httpPort)) config.HttpProxyPort = int.Parse(httpPort);
                    if (!string.IsNullOrEmpty(routingMode)) config.RoutingMode = routingMode;

                    registry.Close();
                }
            }
            catch { }
            
            return config;
        }
    }
}