using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

namespace EchWorkersManager
{
    public partial class Form1 : Form
    {
        private Process workerProcess;
        private bool isRunning = false;
        private Thread httpProxyThread;
        private TcpListener httpProxyListener;
        private bool httpProxyRunning = false;
        private string socksHost = "127.0.0.1";
        private int socksPort = 30000;
        private int httpProxyPort = 10809; // HTTP代理端口

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        public Form1()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form
            this.ClientSize = new System.Drawing.Size(500, 500);
            this.Text = "ECH Workers Manager";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Domain Label & TextBox
            Label lblDomain = new Label();
            lblDomain.Text = "域名:";
            lblDomain.Location = new System.Drawing.Point(20, 20);
            lblDomain.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblDomain);

            TextBox txtDomain = new TextBox();
            txtDomain.Name = "txtDomain";
            txtDomain.Location = new System.Drawing.Point(130, 20);
            txtDomain.Size = new System.Drawing.Size(340, 20);
            txtDomain.Text = "ech.sjwayrhz9.workers.dev:443";
            this.Controls.Add(txtDomain);

            // IP Label & TextBox
            Label lblIP = new Label();
            lblIP.Text = "IP:";
            lblIP.Location = new System.Drawing.Point(20, 60);
            lblIP.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblIP);

            TextBox txtIP = new TextBox();
            txtIP.Name = "txtIP";
            txtIP.Location = new System.Drawing.Point(130, 60);
            txtIP.Size = new System.Drawing.Size(340, 20);
            txtIP.Text = "saas.sin.fan";
            this.Controls.Add(txtIP);

            // Token Label & TextBox
            Label lblToken = new Label();
            lblToken.Text = "Token:";
            lblToken.Location = new System.Drawing.Point(20, 100);
            lblToken.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblToken);

            TextBox txtToken = new TextBox();
            txtToken.Name = "txtToken";
            txtToken.Location = new System.Drawing.Point(130, 100);
            txtToken.Size = new System.Drawing.Size(340, 20);
            txtToken.Text = "miy8TMEisePcHp$K";
            this.Controls.Add(txtToken);

            // Local Address Label & TextBox
            Label lblLocal = new Label();
            lblLocal.Text = "本地SOCKS5:";
            lblLocal.Location = new System.Drawing.Point(20, 140);
            lblLocal.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblLocal);

            TextBox txtLocal = new TextBox();
            txtLocal.Name = "txtLocal";
            txtLocal.Location = new System.Drawing.Point(130, 140);
            txtLocal.Size = new System.Drawing.Size(340, 20);
            txtLocal.Text = "127.0.0.1:30000";
            this.Controls.Add(txtLocal);

            // HTTP Proxy Port Label & TextBox
            Label lblHttpPort = new Label();
            lblHttpPort.Text = "HTTP代理端口:";
            lblHttpPort.Location = new System.Drawing.Point(20, 170);
            lblHttpPort.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblHttpPort);

            TextBox txtHttpPort = new TextBox();
            txtHttpPort.Name = "txtHttpPort";
            txtHttpPort.Location = new System.Drawing.Point(130, 170);
            txtHttpPort.Size = new System.Drawing.Size(340, 20);
            txtHttpPort.Text = "10809";
            this.Controls.Add(txtHttpPort);

            // Start Button
            Button btnStart = new Button();
            btnStart.Name = "btnStart";
            btnStart.Text = "启动服务";
            btnStart.Location = new System.Drawing.Point(130, 210);
            btnStart.Size = new System.Drawing.Size(100, 30);
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            // Stop Button
            Button btnStop = new Button();
            btnStop.Name = "btnStop";
            btnStop.Text = "停止服务";
            btnStop.Location = new System.Drawing.Point(250, 210);
            btnStop.Size = new System.Drawing.Size(100, 30);
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            // Set Proxy Button
            Button btnSetProxy = new Button();
            btnSetProxy.Name = "btnSetProxy";
            btnSetProxy.Text = "启用系统代理";
            btnSetProxy.Location = new System.Drawing.Point(130, 260);
            btnSetProxy.Size = new System.Drawing.Size(120, 35);
            btnSetProxy.Click += BtnSetProxy_Click;
            this.Controls.Add(btnSetProxy);

            // Clear Proxy Button
            Button btnClearProxy = new Button();
            btnClearProxy.Name = "btnClearProxy";
            btnClearProxy.Text = "禁用系统代理";
            btnClearProxy.Location = new System.Drawing.Point(270, 260);
            btnClearProxy.Size = new System.Drawing.Size(120, 35);
            btnClearProxy.Click += BtnClearProxy_Click;
            this.Controls.Add(btnClearProxy);

            // Status Label
            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "状态: 未运行\nHTTP代理: 未启动";
            lblStatus.Location = new System.Drawing.Point(20, 310);
            lblStatus.Size = new System.Drawing.Size(450, 100);
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            this.Controls.Add(lblStatus);

            // Save Button
            Button btnSave = new Button();
            btnSave.Text = "保存配置";
            btnSave.Location = new System.Drawing.Point(370, 210);
            btnSave.Size = new System.Drawing.Size(100, 30);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Info Label
            Label lblInfo = new Label();
            lblInfo.Text = "💡 提示: 点击\"启用系统代理\"后,所有浏览器将自动使用代理";
            lblInfo.Location = new System.Drawing.Point(20, 420);
            lblInfo.Size = new System.Drawing.Size(450, 40);
            lblInfo.ForeColor = System.Drawing.Color.Green;
            this.Controls.Add(lblInfo);

            this.FormClosing += Form1_FormClosing;
            this.ResumeLayout(false);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                TextBox txtDomain = (TextBox)this.Controls["txtDomain"];
                TextBox txtIP = (TextBox)this.Controls["txtIP"];
                TextBox txtToken = (TextBox)this.Controls["txtToken"];
                TextBox txtLocal = (TextBox)this.Controls["txtLocal"];
                TextBox txtHttpPort = (TextBox)this.Controls["txtHttpPort"];

                // 解析SOCKS5地址
                string[] parts = txtLocal.Text.Split(':');
                socksHost = parts[0];
                socksPort = int.Parse(parts[1]);
                httpProxyPort = int.Parse(txtHttpPort.Text);

                // 启动 ech-workers
                string arguments = $"-f {txtDomain.Text} -ip {txtIP.Text} -token {txtToken.Text} -l {txtLocal.Text}";
                workerProcess = new Process();
                workerProcess.StartInfo.FileName = "ech-workers.exe";
                workerProcess.StartInfo.Arguments = arguments;
                workerProcess.StartInfo.UseShellExecute = false;
                workerProcess.StartInfo.CreateNoWindow = true;
                workerProcess.Start();

                // 等待SOCKS5服务启动
                Thread.Sleep(1000);

                // 启动HTTP代理转换器
                StartHttpProxy();

                isRunning = true;
                ((Button)this.Controls["btnStart"]).Enabled = false;
                ((Button)this.Controls["btnStop"]).Enabled = true;
                UpdateStatusLabel($"状态: 运行中\nSOCKS5: {txtLocal.Text}\nHTTP代理: 127.0.0.1:{httpProxyPort}");

                MessageBox.Show($"服务已启动!\n\nSOCKS5: {txtLocal.Text}\nHTTP代理: 127.0.0.1:{httpProxyPort}\n\n现在可以点击\"启用系统代理\"", 
                    "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartHttpProxy()
        {
            try
            {
                httpProxyRunning = true;
                httpProxyListener = new TcpListener(IPAddress.Loopback, httpProxyPort);
                httpProxyListener.Start();

                httpProxyThread = new Thread(() =>
                {
                    while (httpProxyRunning)
                    {
                        try
                        {
                            if (httpProxyListener.Pending())
                            {
                                TcpClient client = httpProxyListener.AcceptTcpClient();
                                Thread clientThread = new Thread(() => HandleHttpProxyClient(client));
                                clientThread.IsBackground = true;
                                clientThread.Start();
                            }
                            else
                            {
                                Thread.Sleep(100);
                            }
                        }
                        catch { }
                    }
                });
                httpProxyThread.IsBackground = true;
                httpProxyThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动HTTP代理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleHttpProxyClient(TcpClient client)
        {
            try
            {
                NetworkStream clientStream = client.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // 解析HTTP请求
                string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return;

                string[] requestLine = lines[0].Split(' ');
                if (requestLine.Length < 3) return;

                string method = requestLine[0];
                string url = requestLine[1];

                // 处理CONNECT方法(HTTPS)
                if (method == "CONNECT")
                {
                    string[] hostPort = url.Split(':');
                    string targetHost = hostPort[0];
                    int targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

                    // 连接到SOCKS5代理
                    TcpClient socksClient = new TcpClient(socksHost, socksPort);
                    NetworkStream socksStream = socksClient.GetStream();

                    // SOCKS5握手
                    socksStream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);
                    byte[] response = new byte[2];
                    socksStream.Read(response, 0, 2);

                    // SOCKS5连接请求
                    byte[] hostBytes = Encoding.ASCII.GetBytes(targetHost);
                    byte[] connectRequest = new byte[7 + hostBytes.Length];
                    connectRequest[0] = 0x05; // SOCKS版本
                    connectRequest[1] = 0x01; // CONNECT命令
                    connectRequest[2] = 0x00; // 保留
                    connectRequest[3] = 0x03; // 域名类型
                    connectRequest[4] = (byte)hostBytes.Length;
                    Array.Copy(hostBytes, 0, connectRequest, 5, hostBytes.Length);
                    connectRequest[5 + hostBytes.Length] = (byte)(targetPort >> 8);
                    connectRequest[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);

                    socksStream.Write(connectRequest, 0, connectRequest.Length);
                    byte[] connectResponse = new byte[10];
                    socksStream.Read(connectResponse, 0, 10);

                    if (connectResponse[1] == 0x00)
                    {
                        // 连接成功,返回200给客户端
                        string successResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
                        byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                        clientStream.Write(successBytes, 0, successBytes.Length);

                        // 开始双向转发
                        Thread forwardThread = new Thread(() => ForwardData(clientStream, socksStream));
                        forwardThread.IsBackground = true;
                        forwardThread.Start();
                        ForwardData(socksStream, clientStream);
                    }

                    socksClient.Close();
                }
                else
                {
                    // HTTP请求处理
                    Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                    string targetHost = uri.Host;
                    int targetPort = uri.Port;

                    TcpClient socksClient = new TcpClient(socksHost, socksPort);
                    NetworkStream socksStream = socksClient.GetStream();

                    // SOCKS5握手和连接
                    socksStream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);
                    byte[] response = new byte[2];
                    socksStream.Read(response, 0, 2);

                    byte[] hostBytes = Encoding.ASCII.GetBytes(targetHost);
                    byte[] connectRequest = new byte[7 + hostBytes.Length];
                    connectRequest[0] = 0x05;
                    connectRequest[1] = 0x01;
                    connectRequest[2] = 0x00;
                    connectRequest[3] = 0x03;
                    connectRequest[4] = (byte)hostBytes.Length;
                    Array.Copy(hostBytes, 0, connectRequest, 5, hostBytes.Length);
                    connectRequest[5 + hostBytes.Length] = (byte)(targetPort >> 8);
                    connectRequest[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);

                    socksStream.Write(connectRequest, 0, connectRequest.Length);
                    byte[] connectResponse = new byte[10];
                    socksStream.Read(connectResponse, 0, 10);

                    if (connectResponse[1] == 0x00)
                    {
                        // 转发原始HTTP请求
                        socksStream.Write(buffer, 0, bytesRead);

                        // 转发响应
                        Thread forwardThread = new Thread(() => ForwardData(socksStream, clientStream));
                        forwardThread.IsBackground = true;
                        forwardThread.Start();
                        ForwardData(clientStream, socksStream);
                    }

                    socksClient.Close();
                }

                client.Close();
            }
            catch { }
        }

        private void ForwardData(NetworkStream from, NetworkStream to)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = from.Read(buffer, 0, buffer.Length)) > 0)
                {
                    to.Write(buffer, 0, bytesRead);
                }
            }
            catch { }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                // 停止HTTP代理
                httpProxyRunning = false;
                if (httpProxyListener != null)
                {
                    httpProxyListener.Stop();
                }

                // 停止ech-workers
                if (workerProcess != null && !workerProcess.HasExited)
                {
                    workerProcess.Kill();
                    workerProcess.WaitForExit();
                }

                isRunning = false;
                ((Button)this.Controls["btnStart"]).Enabled = true;
                ((Button)this.Controls["btnStop"]).Enabled = false;
                UpdateStatusLabel("状态: 已停止\nHTTP代理: 已停止");

                MessageBox.Show("服务已停止!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSetProxy_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isRunning)
                {
                    MessageBox.Show("请先启动服务!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string proxyServer = $"127.0.0.1:{httpProxyPort}";

                // 设置Windows系统代理
                RegistryKey registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", proxyServer);
                registry.SetValue("ProxyOverride", "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>");
                registry.Close();

                // 刷新Internet选项
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                UpdateStatusLabel($"状态: 运行中\nHTTP代理: 127.0.0.1:{httpProxyPort}\n✅ 系统代理已启用");

                MessageBox.Show($"系统代理已启用!\n\n代理地址: {proxyServer}\n\n现在所有浏览器都会使用此代理", 
                    "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置代理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearProxy_Click(object sender, EventArgs e)
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

                UpdateStatusLabel("状态: 运行中\nHTTP代理: 127.0.0.1:" + httpProxyPort + "\n❌ 系统代理已禁用");
                MessageBox.Show("系统代理已禁用!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除代理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatusLabel(string text)
        {
            Label lblStatus = (Label)this.Controls["lblStatus"];
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => lblStatus.Text = text));
            }
            else
            {
                lblStatus.Text = text;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("配置已保存!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveSettings()
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.CreateSubKey("Software\\EchWorkersManager");
                registry.SetValue("Domain", ((TextBox)this.Controls["txtDomain"]).Text);
                registry.SetValue("IP", ((TextBox)this.Controls["txtIP"]).Text);
                registry.SetValue("Token", ((TextBox)this.Controls["txtToken"]).Text);
                registry.SetValue("Local", ((TextBox)this.Controls["txtLocal"]).Text);
                registry.SetValue("HttpPort", ((TextBox)this.Controls["txtHttpPort"]).Text);
                registry.Close();
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\EchWorkersManager");
                if (registry != null)
                {
                    string domain = registry.GetValue("Domain") as string;
                    string ip = registry.GetValue("IP") as string;
                    string token = registry.GetValue("Token") as string;
                    string local = registry.GetValue("Local") as string;
                    string httpPort = registry.GetValue("HttpPort") as string;

                    if (!string.IsNullOrEmpty(domain)) ((TextBox)this.Controls["txtDomain"]).Text = domain;
                    if (!string.IsNullOrEmpty(ip)) ((TextBox)this.Controls["txtIP"]).Text = ip;
                    if (!string.IsNullOrEmpty(token)) ((TextBox)this.Controls["txtToken"]).Text = token;
                    if (!string.IsNullOrEmpty(local)) ((TextBox)this.Controls["txtLocal"]).Text = local;
                    if (!string.IsNullOrEmpty(httpPort)) ((TextBox)this.Controls["txtHttpPort"]).Text = httpPort;

                    registry.Close();
                }
            }
            catch { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRunning)
            {
                var result = MessageBox.Show("服务正在运行,确定要退出吗?\n建议先禁用系统代理。", "确认", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    BtnStop_Click(null, null);
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}