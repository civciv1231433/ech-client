using System;
using System.Diagnostics;
using EchWorkersManager.Models;

namespace EchWorkersManager.Services
{
    public class WorkerService
    {
        private Process workerProcess;
        private readonly string echWorkersPath;

        public bool IsRunning { get; private set; }

        public WorkerService(string echWorkersPath)
        {
            this.echWorkersPath = echWorkersPath;
            IsRunning = false;
        }

        public void Start(ProxyConfig config)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Worker service is already running");
            }

            string arguments = $"-f {config.Domain} -ip {config.IP} -token {config.Token} -l {config.LocalAddress}";
            
            workerProcess = new Process();
            workerProcess.StartInfo.FileName = echWorkersPath;
            workerProcess.StartInfo.Arguments = arguments;
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.CreateNoWindow = true;
            workerProcess.Start();

            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            if (workerProcess != null && !workerProcess.HasExited)
            {
                workerProcess.Kill();
                workerProcess.WaitForExit();
            }

            IsRunning = false;
        }
    }
}