using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EchWorkersManager.UI
{
    public class TrayIconManager
    {
        private NotifyIcon trayIcon;
        private Form parentForm;
        private Action onShowMainWindow;
        private Action onStart;
        private Action onStop;

        public TrayIconManager(Form parentForm, Action onShowMainWindow, Action onStart, Action onStop)
        {
            this.parentForm = parentForm;
            this.onShowMainWindow = onShowMainWindow;
            this.onStart = onStart;
            this.onStop = onStop;
            
            Initialize();
        }

        private void Initialize()
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
                        trayIcon.Icon = new Icon(iconStream);
                        parentForm.Icon = new Icon(iconStream);
                    }
                    else
                    {
                        trayIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch
            {
                trayIcon.Icon = SystemIcons.Application;
            }
            
            trayIcon.Text = "ECH Workers Manager";
            trayIcon.Visible = false;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, e) => onShowMainWindow?.Invoke();
            trayMenu.Items.Add(showItem);

            ToolStripMenuItem startItem = new ToolStripMenuItem("启动服务");
            startItem.Name = "startItem";
            startItem.Click += (s, e) => onStart?.Invoke();
            trayMenu.Items.Add(startItem);

            ToolStripMenuItem stopItem = new ToolStripMenuItem("停止服务");
            stopItem.Name = "stopItem";
            stopItem.Enabled = false;
            stopItem.Click += (s, e) => onStop?.Invoke();
            trayMenu.Items.Add(stopItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            };
            trayMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => onShowMainWindow?.Invoke();
        }

        public void Show()
        {
            trayIcon.Visible = true;
        }

        public void Hide()
        {
            trayIcon.Visible = false;
        }

        public void UpdateText(string text)
        {
            trayIcon.Text = text;
        }

        public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
        {
            trayIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        public void UpdateMenuState(bool isRunning)
        {
            if (trayIcon.ContextMenuStrip != null)
            {
                ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["startItem"]).Enabled = !isRunning;
                ((ToolStripMenuItem)trayIcon.ContextMenuStrip.Items["stopItem"]).Enabled = isRunning;
            }
        }

        public void Dispose()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
    }
}