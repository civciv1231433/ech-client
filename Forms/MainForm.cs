using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using EchWorkersManager.Models;
using EchWorkersManager.Services;
using EchWorkersManager.Routing;
using EchWorkersManager.Helpers;
using EchWorkersManager.UI;

namespace EchWorkersManager.Forms
{
    public partial class MainForm : Form
    {
        private WorkerService workerService;
        private HttpProxyService httpProxyService;
        private SystemProxyService systemProxyService;
        private RoutingManager routingManager;
        private TrayIconManager trayIconManager;
        
        private ProxyConfig config;
        private string echWorkersPath;

        public MainForm()
        {
            InitializeServices();
            InitializeComponent();
            InitializeTrayIcon();
            LoadConfiguration();
        }

        private void InitializeServices()
        {
            try
            {
                echWorkersPath = ResourceHelper.ExtractEchWorkers();
                workerService = new WorkerService(echWorkersPath);
                routingManager = new RoutingManager();
                httpProxyService = new HttpProxyService(routingManager);
                systemProxyService = new SystemProxyService();
                config = new ProxyConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeTrayIcon()
        {
            trayIconManager = new TrayIconManager(
                this,
                ShowMainWindow,
                BtnStart_Click,
                BtnStop_Click
            );
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            trayIconManager.Hide();
        }

        private void LoadConfiguration()
        {
            config = SettingsHelper.Load();
            
            ((TextBox)this.Controls["txtDomain"]).Text = config.Domain;
            ((TextBox)this.Controls["txtIP"]).Text = config.IP;
            ((TextBox)this.Controls["txtToken"]).Text = config.Token;
            ((TextBox)this.Controls["txtLocal"]).Text = config.LocalAddress;
            ((TextBox)this.Controls["txtHttpPort"]).Text = config.HttpProxyPort.ToString();
            
            ComboBox cmbRouting = (ComboBox)this.Controls["cmbRouting"];
            int index = cmbRouting.Items.IndexOf(config.RoutingMode);
            if (index >= 0)
            {
                cmbRouting.SelectedIndex = index;
            }
            
            routingManager.SetRoutingMode(config.RoutingMode);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.ClientSize = new Size(500, 480);
            this.Text = "ECH Workers Manager";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            CreateControls();

            this.Resize += Form1_Resize;
            this.FormClosing += Form1_FormClosing;
            this.ResumeLayout(false);
        }

        private void CreateControls()
        {
            // Domain
            AddLabel("域名:", 20, 20);
            AddTextBox("txtDomain", 130, 20, 340, "ech.sjwayrhz9.workers.dev:443");

            // IP
            AddLabel("IP:", 20, 60);
            AddTextBox("txtIP", 130, 60, 340, "saas.sin.fan");

            // Token
            AddLabel("Token:", 20, 100);
            AddTextBox("txtToken", 130, 100, 340, "miy8TMEisePcHp$K");

            // Local SOCKS5
            AddLabel("本地SOCKS5:", 20, 140);
            AddTextBox("txtLocal", 130, 140, 340, "127.0.0.1:30000");

            // HTTP Proxy Port
            AddLabel("HTTP代理端口:", 20, 170);
            AddTextBox("txtHttpPort", 130, 170, 340, "10809");

            // Routing Mode
            AddLabel("路由模式:", 20, 200);
            ComboBox cmbRouting = new ComboBox();
            cmbRouting.Name = "cmbRouting";
            cmbRouting.Location = new Point(130, 200);
            cmbRouting.Size = new Size(340, 20);
            cmbRouting.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRouting.Items.AddRange(new string[] { "全局模式", "绕过大陆", "直连模式" });
            cmbRouting.SelectedIndex = 1;
            cmbRouting.SelectedIndexChanged += (s, e) => {
                routingManager.SetRoutingMode(cmbRouting.SelectedItem.ToString());
            };
            this.Controls.Add(cmbRouting);

            // Buttons
            Button btnStart = new Button();
            btnStart.Name = "btnStart";
            btnStart.Text = "启动服务";
            btnStart.Location = new Point(130, 250);
            btnStart.Size = new Size(120, 40);
            btnStart.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            btnStart.BackColor = Color.LightGreen;
            btnStart.Click += (s, e) => BtnStart_Click();
            this.Controls.Add(btnStart);

            Button btnStop = new Button();
            btnStop.Name = "btnStop";
            btnStop.Text = "停止服务";
            btnStop.Location = new Point(270, 250);
            btnStop.Size = new Size(120, 40);
            btnStop.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            btnStop.BackColor = Color.LightCoral;
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => BtnStop_Click();
            this.Controls.Add(btnStop);

            Button btnSave = new Button();
            btnSave.Text = "保存配置";
            btnSave.Location = new Point(400, 250);
            btnSave.Size = new Size(70, 40);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Status Label
            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "状态: 未运行\nHTTP代理: 未启动\n系统代理: 未启用\n路由模式: 绕过大陆";
            lblStatus.Location = new Point(20, 310);
            lblStatus.Size = new Size(450, 100);
            lblStatus.ForeColor = Color.Blue;
            lblStatus.Font = new Font("Microsoft YaHei", 9F);
            this.Controls.Add(lblStatus);

            // Info Label
            Label lblInfo = new Label();
            lblInfo.Text = "💡 全局模式：代理所有(除内网)\n💡 绕过大陆：仅代理境外IP(除内网)\n💡 直连模式：不使用代理";
            lblInfo.Location = new Point(20, 410);
            lblInfo.Size = new Size(450, 60);
            lblInfo.ForeColor = Color.Green;
            lblInfo.Font = new Font("Microsoft YaHei", 8.5F);
            this.Controls.Add(lblInfo);
        }

        private void AddLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(100, 20);
            this.Controls.Add(label);
        }

        private void AddTextBox(string name, int x, int y, int width, string defaultText)
        {
            TextBox textBox = new TextBox();
            textBox.Name = name;
            textBox.Location = new Point(x, y);
            textBox.Size = new Size(width, 20);
            textBox.Text = defaultText;
            this.Controls.Add(textBox);
        }

        private void BtnStart_Click()
        {
            try
            {
                UpdateConfigFromUI();

                workerService.Start(config);
                Thread.Sleep(1000);

                httpProxyService.Start(config);
                
                if (config.RoutingMode != "直连模式")
                {
                    systemProxyService.Enable(config.HttpProxyPort);
                }

                ((Button)this.Controls["btnStart"]).Enabled = false;
                ((Button)this.Controls["btnStop"]).Enabled = true;
                trayIconManager.UpdateMenuState(true);
                
                string proxyStatus = config.RoutingMode == "直连模式" ? "未启用(直连)" : "已启用";
                UpdateStatusLabel($"✅ 状态: 运行中\n✅ HTTP代理: 127.0.0.1:{config.HttpProxyPort}\n✅ 系统代理: {proxyStatus}\n✅ 路由模式: {config.RoutingMode}");
                trayIconManager.UpdateText($"ECH Workers Manager - 运行中 ({config.RoutingMode})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click()
        {
            try
            {
                systemProxyService.Disable();
                httpProxyService.Stop();
                workerService.Stop();

                ((Button)this.Controls["btnStart"]).Enabled = true;
                ((Button)this.Controls["btnStop"]).Enabled = false;
                trayIconManager.UpdateMenuState(false);
                
                UpdateStatusLabel("❌ 状态: 已停止\n❌ HTTP代理: 已停止\n❌ 系统代理: 已禁用");
                trayIconManager.UpdateText("ECH Workers Manager - 已停止");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            UpdateConfigFromUI();
            SettingsHelper.Save(config);
            MessageBox.Show("配置已保存!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateConfigFromUI()
        {
            config.Domain = ((TextBox)this.Controls["txtDomain"]).Text;
            config.IP = ((TextBox)this.Controls["txtIP"]).Text;
            config.Token = ((TextBox)this.Controls["txtToken"]).Text;
            config.LocalAddress = ((TextBox)this.Controls["txtLocal"]).Text;
            config.HttpProxyPort = int.Parse(((TextBox)this.Controls["txtHttpPort"]).Text);
            config.RoutingMode = ((ComboBox)this.Controls["cmbRouting"]).SelectedItem.ToString();
            
            routingManager.SetRoutingMode(config.RoutingMode);
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

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                trayIconManager.Show();
                trayIconManager.ShowBalloonTip(1000, "ECH Workers Manager", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (workerService.IsRunning)
            {
                BtnStop_Click();
            }
            trayIconManager.Dispose();
        }
    }
}