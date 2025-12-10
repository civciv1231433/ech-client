using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace EchWorkersManager.Helpers
{
    public static class ResourceHelper
    {
        public static string ExtractEchWorkers()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "EchWorkersManager.ech-workers.exe";
                
                string tempPath = Path.Combine(Path.GetTempPath(), "EchWorkersManager");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                
                string echWorkersPath = Path.Combine(tempPath, "ech-workers.exe");
                
                if (!File.Exists(echWorkersPath) || !IsProcessRunning("ech-workers"))
                {
                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream != null)
                        {
                            using (FileStream fileStream = new FileStream(echWorkersPath, FileMode.Create))
                            {
                                resourceStream.CopyTo(fileStream);
                            }
                            return echWorkersPath;
                        }
                        else
                        {
                            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ech-workers.exe");
                            if (File.Exists(localPath))
                            {
                                return localPath;
                            }
                            throw new FileNotFoundException("未找到 ech-workers.exe 文件!");
                        }
                    }
                }
                
                return echWorkersPath;
            }
            catch (Exception ex)
            {
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ech-workers.exe");
                if (File.Exists(localPath))
                {
                    return localPath;
                }
                throw new Exception($"提取 ech-workers.exe 失败: {ex.Message}");
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
    }
}