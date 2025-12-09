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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

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
        private int httpProxyPort = 10809;
        private NotifyIcon trayIcon;
        private string echWorkersPath;
        // 修改为 List 以便进行前缀匹配，而不是完全相等匹配
        private List<string> chinaIPPrefixes = new List<string>();
        private string routingMode = "绕过大陆"; // 默认模式

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        public Form1()
        {
            InitializeComponent();
            InitializeTrayIcon();
            ExtractEchWorkers();
            LoadSettings();
            InitializeRoutingData();
        }

        private void InitializeRoutingData()
        {
            // 初始化中国 IP 段前缀 (简化版)
            // 逻辑：如果目标 IP 以这些字符串开头，则认为是中国 IP
            // 注意：为了更精准，建议后续去网上找完整的 CN IP CIDR 列表并解析
            string[] chinaCIDR = {
                "1.0.", "14.", "27.", "36.", "42.",
                "58.", "59.", "60.", "61.",
                "110.", "111.", "112.", "113.",
                "114.", "115.", "116.", "117.",
                "118.", "119.", "120.", "121.",
                "122.", "123.", "124.", "125.",
                "180.", "182.", "183.", "202.", "203.",
                "210.", "211.", "218.", "219.", "220.", "221.", "222.", "223."
            };
            
            chinaIPPrefixes.Clear();
            chinaIPPrefixes.AddRange(chinaCIDR);
        }

        // 判断是否为私有/局域网 IP
        private bool IsPrivateIP(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true; // 127.0.0.1, ::1

            byte[] bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 169.254.0.0/16 (Link-local)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }
            return false;
        }

        // 判断是否为中国 IP
        private bool IsChinaIP(IPAddress ip)
        {
            string ipStr = ip.ToString();
            // 简单的前缀匹配
            foreach (var prefix in chinaIPPrefixes)
            {
                if (ipStr.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        // 核心路由逻辑修改
        private bool ShouldProxy(string host)
        {
            // 1. 直连模式：永远不走代理
            if (routingMode == "直连模式")
            {
                return false;
            }

            IPAddress targetIP = null;
            bool isIpAddr = IPAddress.TryParse(host, out targetIP);

            // 如果是域名且模式是"绕过大陆"，我们需要解析出 IP 才能判断它是不是中国 IP
            // 注意：DNS 解析可能会耗时，但这是基于 IP 规则分流的必要步骤
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
                    // 解析失败，为了保险起见，如果是绕过大陆模式，通常解析失败的都是被污染的国外域名
                    // 所以默认返回 true (走代理)
                    return true;
                }
            }

            // 2. 内网 IP 检查：无论全局还是绕过大陆，内网 IP 都不走代理
            if (targetIP != null && IsPrivateIP(targetIP))
            {
                return false;
            }

            // 3. 全局模式：排除内网后，全部走代理
            if (routingMode == "全局模式")
            {
                return true;
            }

            // 4. 绕过大陆模式
            if (routingMode == "绕过大陆")
            {
                // 如果我们获取到了 IP，并且它是中国 IP -> 直连(false)
                if (targetIP != null && IsChinaIP(targetIP))
                {
                    return false;
                }
                
                // 其他情况（国外 IP、无法解析的域名等）-> 代理(true)
                // 这样能确保 Youtube 等视频网站（非中国IP）一定走代理
                return true;
            }

            return true; // 默认
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string iconResourceName = "EchWorkersManager.app.ico";
                using (Stream iconStream = assembly.GetManifestResourceStream(iconResourceName))
                {
                    if (iconStream != null)
                    {
                        trayIcon.Icon = new System.Drawing.Icon(iconStream);
                        this.Icon = new System.Drawing.Icon(iconStream);
                    }
                    else
                    {
                        trayIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            trayIcon.Text = "ECH Workers Manager";
            trayIcon.Visible = false;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => ShowMainWindow();
            trayMenu.Items.Add(showItem);

            ToolStripMenuItem startItem = new ToolStripMenuItem("启动服务");
            startItem.Name = "startItem";
            startItem.Click += (s, e) => BtnStart_Click(null, null);
            trayMenu.Items.Add(startItem);

            ToolStripMenuItem stopItem = new ToolStripMenuItem("停止服务");
            stopItem.Name = "stopItem";
            stopItem.Enabled = false;
            stopItem.Click += (s, e) => BtnStop_Click(null, null);
            trayMenu.Items.Add(stopItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            };
            trayMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            trayIcon.Visible = false;
        }

        private void ExtractEchWorkers()
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
                
                echWorkersPath = Path.Combine(tempPath, "ech-workers.exe");
                
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
                        }
                        else
                        {
                            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ech-workers.exe");
                            if (File.Exists(localPath))
                            {
                                echWorkersPath = localPath;
                            }
                            else
                            {
                                MessageBox.Show("未找到 ech-workers.exe 文件!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ech-workers.exe");
                if (File.Exists(localPath))
                {
                    echWorkersPath = localPath;
                }
                else
                {
                    MessageBox.Show($"提取 ech-workers.exe 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.ClientSize = new System.Drawing.Size(500, 480);
            this.Text = "ECH Workers Manager";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

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

            // 路由模式选择
            Label lblRouting = new Label();
            lblRouting.Text = "路由模式:";
            lblRouting.Location = new System.Drawing.Point(20, 200);
            lblRouting.Size = new System.Drawing.Size(100, 20);
            this.Controls.Add(lblRouting);

            ComboBox cmbRouting = new ComboBox();
            cmbRouting.Name = "cmbRouting";
            cmbRouting.Location = new System.Drawing.Point(130, 200);
            cmbRouting.Size = new System.Drawing.Size(340, 20);
            cmbRouting.DropDownStyle = ComboBoxStyle.DropDownList;
            // 更新选项为新的三种模式
            cmbRouting.Items.AddRange(new string[] { "全局模式", "绕过大陆", "直连模式" });
            cmbRouting.SelectedIndex = 1; // 默认绕过大陆
            cmbRouting.SelectedIndexChanged += (s, e) => {
                routingMode = cmbRouting.SelectedItem.ToString();
            };
            this.Controls.Add(cmbRouting);

            Button btnStart = new Button();
            btnStart.Name = "btnStart";
            btnStart.Text = "启动服务";
            btnStart.Location = new System.Drawing.Point(130, 250);
            btnStart.Size = new System.Drawing.Size(120, 40);
            btnStart.Font = new System.Drawing.Font("Microsoft YaHei", 10F, System.Drawing.FontStyle.Bold);
            btnStart.BackColor = System.Drawing.Color.LightGreen;
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            Button btnStop = new Button();
            btnStop.Name = "btnStop";
            btnStop.Text = "停止服务";
            btnStop.Location = new System.Drawing.Point(270, 250);
            btnStop.Size = new System.Drawing.Size(120, 40);
            btnStop.Font = new System.Drawing.Font("Microsoft YaHei", 10F, System.Drawing.FontStyle.Bold);
            btnStop.BackColor = System.Drawing.Color.LightCoral;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "状态: 未运行\nHTTP代理: 未启动\n系统代理: 未启用\n路由模式: 绕过大陆";
            lblStatus.Location = new System.Drawing.Point(20, 310);
            lblStatus.Size = new System.Drawing.Size(450, 100);
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            lblStatus.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.Controls.Add(lblStatus);

            Button btnSave = new Button();
            btnSave.Text = "保存配置";
            btnSave.Location = new System.Drawing.Point(400, 250);
            btnSave.Size = new System.Drawing.Size(70, 40);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Label lblInfo = new Label();
            lblInfo.Text = "💡 全局模式：代理所有(除内网)\n💡 绕过大陆：仅代理境外IP(除内网)\n💡 直连模式：不使用代理";
            lblInfo.Location = new System.Drawing.Point(20, 410);
            lblInfo.Size = new System.Drawing.Size(450, 60);
            lblInfo.ForeColor = System.Drawing.Color.Green;
            lblInfo.Font = new System.Drawing.Font("Microsoft YaHei", 8.5F);
            this.Controls.Add(lblInfo);

            this.Resize += Form1_Resize;
            this.FormClosing += Form1_FormClosing;
            this.ResumeLayout(false);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(1000, "ECH Workers Manager", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
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
                ComboBox cmbRouting = (ComboBox)this.Controls["cmbRouting"];

                string[] parts = txtLocal.Text.Split(':');
                socksHost = parts[0];
                socksPort = int.Parse(parts[1]);
                httpProxyPort = int.Parse(txtHttpPort.Text);
                routingMode = cmbRouting.SelectedItem.ToString();

                string arguments = $"-f {txtDomain.Text} -ip {txtIP.Text} -token {txtToken.Text} -l {txtLocal.Text}";
                workerProcess = new Process();
                workerProcess.StartInfo.FileName = echWorkersPath;
                workerProcess.StartInfo.Arguments = arguments;
                workerProcess.StartInfo.UseShellExecute = false;
                workerProcess.StartInfo.CreateNoWindow = true;
                workerProcess.Start();

                Thread.Sleep(1000);

                StartHttpProxy();
                
                // 如果是直连模式，不设置系统代理
                if (routingMode != "直连模式")
                {
                    EnableSystemProxy();
                }

                isRunning = true;
                ((Button)this.Controls["btnStart"]).Enabled = false;
                ((Button)this.Controls["btnStop"]).Enabled = true;
                
                if (trayIcon.ContextMenuStrip != null)
                {
                    ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["startItem"]).Enabled = false;
                    ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["stopItem"]).Enabled = true;
                }
                
                string proxyStatus = routingMode == "直连模式" ? "未启用(直连)" : "已启用";
                UpdateStatusLabel($"✅ 状态: 运行中\n✅ HTTP代理: 127.0.0.1:{httpProxyPort}\n✅ 系统代理: {proxyStatus}\n✅ 路由模式: {routingMode}");
                trayIcon.Text = $"ECH Workers Manager - 运行中 ({routingMode})";
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

                string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return;

                string[] requestLine = lines[0].Split(' ');
                if (requestLine.Length < 3) return;

                string method = requestLine[0];
                string url = requestLine[1];

                // 提取目标主机
                string targetHost = "";
                if (method == "CONNECT")
                {
                    targetHost = url.Split(':')[0];
                }
                else
                {
                    try
                    {
                        Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                        targetHost = uri.Host;
                    }
                    catch { }
                }

                // 检查是否需要代理
                if (!ShouldProxy(targetHost))
                {
                    // 不走代理,直接连接
                    HandleDirectConnection(client, clientStream, buffer, bytesRead, method, url, targetHost);
                    return;
                }

                // 走代理
                if (method == "CONNECT")
                {
                    HandleConnectMethod(client, clientStream, url);
                }
                else
                {
                    HandleHttpMethod(client, clientStream, buffer, bytesRead, url);
                }
            }
            catch { }
        }

        private void HandleDirectConnection(TcpClient client, NetworkStream clientStream, byte[] buffer, int bytesRead, string method, string url, string targetHost)
        {
            try
            {
                if (method == "CONNECT")
                {
                    string[] hostPort = url.Split(':');
                    int targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

                    TcpClient targetClient = new TcpClient(targetHost, targetPort);
                    NetworkStream targetStream = targetClient.GetStream();

                    string successResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
                    byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                    clientStream.Write(successBytes, 0, successBytes.Length);

                    Thread forwardThread = new Thread(() => ForwardData(clientStream, targetStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(targetStream, clientStream);

                    targetClient.Close();
                }
                else
                {
                    Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                    int targetPort = uri.Port;

                    TcpClient targetClient = new TcpClient(targetHost, targetPort);
                    NetworkStream targetStream = targetClient.GetStream();

                    targetStream.Write(buffer, 0, bytesRead);

                    Thread forwardThread = new Thread(() => ForwardData(targetStream, clientStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(clientStream, targetStream);

                    targetClient.Close();
                }

                client.Close();
            }
            catch { }
        }

        private void HandleConnectMethod(TcpClient client, NetworkStream clientStream, string url)
        {
            try
            {
                string[] hostPort = url.Split(':');
                string targetHost = hostPort[0];
                int targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;

                TcpClient socksClient = new TcpClient(socksHost, socksPort);
                NetworkStream socksStream = socksClient.GetStream();

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
                    string successResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
                    byte[] successBytes = Encoding.UTF8.GetBytes(successResponse);
                    clientStream.Write(successBytes, 0, successBytes.Length);

                    Thread forwardThread = new Thread(() => ForwardData(clientStream, socksStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(socksStream, clientStream);
                }

                socksClient.Close();
                client.Close();
            }
            catch { }
        }

        private void HandleHttpMethod(TcpClient client, NetworkStream clientStream, byte[] buffer, int bytesRead, string url)
        {
            try
            {
                Uri uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                string targetHost = uri.Host;
                int targetPort = uri.Port;

                TcpClient socksClient = new TcpClient(socksHost, socksPort);
                NetworkStream socksStream = socksClient.GetStream();

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
                    socksStream.Write(buffer, 0, bytesRead);

                    Thread forwardThread = new Thread(() => ForwardData(socksStream, clientStream));
                    forwardThread.IsBackground = true;
                    forwardThread.Start();
                    ForwardData(clientStream, socksStream);
                }

                socksClient.Close();
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

        private void EnableSystemProxy()
        {
            try
            {
                string proxyServer = $"127.0.0.1:{httpProxyPort}";

                RegistryKey registry = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", proxyServer);
                // 确保对本地地址绕过系统代理设置
                registry.SetValue("ProxyOverride", "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>");
                registry.Close();

                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
            }
            catch { }
        }

        private void DisableSystemProxy()
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

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopAllServices();
        }

        private void StopAllServices()
        {
            try
            {
                DisableSystemProxy();

                httpProxyRunning = false;
                if (httpProxyListener != null)
                {
                    httpProxyListener.Stop();
                }

                if (workerProcess != null && !workerProcess.HasExited)
                {
                    workerProcess.Kill();
                    workerProcess.WaitForExit();
                }

                isRunning = false;
                ((Button)this.Controls["btnStart"]).Enabled = true;
                ((Button)this.Controls["btnStop"]).Enabled = false;
                
                if (trayIcon.ContextMenuStrip != null)
                {
                    ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["startItem"]).Enabled = true;
                    ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["stopItem"]).Enabled = false;
                }
                
                UpdateStatusLabel("❌ 状态: 已停止\n❌ HTTP代理: 已停止\n❌ 系统代理: 已禁用");
                trayIcon.Text = "ECH Workers Manager - 已停止";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                StopAllServices();
            }
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
    }
}