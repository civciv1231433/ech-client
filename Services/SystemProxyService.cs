using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EchWorkersManager.Services
{
    public class SystemProxyService
    {
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        public void Enable(int httpProxyPort)
        {
            try
            {
                string proxyServer = $"127.0.0.1:{httpProxyPort}";

                RegistryKey registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", proxyServer);
                registry.SetValue("ProxyOverride", "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>");
                registry.Close();

                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
            catch { }
        }

        public void Disable()
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("ProxyServer", "");
                registry.Close();

                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
            catch { }
        }
    }
}