using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;

namespace CsAC_Client
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private bool isExiting = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTray();

            // WebView2 初始化完成后设置
            webView.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                if (args.IsSuccess && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

                    webView.CoreWebView2.NewWindowRequested += (s, e) =>
                    {
                        e.Handled = true;
                        webView.CoreWebView2.Navigate(e.Uri);
                    };

                    webView.CoreWebView2.PermissionRequested += (s, e) =>
                    {
                        if (e.PermissionKind == CoreWebView2PermissionKind.Microphone)
                            e.State = CoreWebView2PermissionState.Allow;
                    };

                    webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                }
            };
        }

        private void InitializeTray()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            Icon trayIcon;

            try
            {
                trayIcon = File.Exists(iconPath)
                    ? new Icon(iconPath)
                    : System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                trayIcon = SystemIcons.Application;
            }

            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = trayIcon,
                Visible = true,
                Text = "CsAC Client"
            };

            notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("打开主界面", null, (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            contextMenu.Items.Add("退出", null, (s, e) =>
            {
                isExiting = true;
                notifyIcon.Visible = false;
                Application.Current.Shutdown();
            });
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 托盘菜单“退出”直接关闭，不询问
            if (isExiting)
            {
                notifyIcon.Visible = false;
                base.OnClosing(e);
                return;
            }

            // 检查是否已记住“不再提醒”
            if (Properties.Settings.Default.CloseOptionRemembered)
            {
                string action = Properties.Settings.Default.CloseOptionAction;
                if (action == "Exit")
                {
                    isExiting = true;
                    notifyIcon.Visible = false;
                    base.OnClosing(e);
                }
                else // Minimize
                {
                    e.Cancel = true;
                    Hide();
                }
            }
            else
            {
                // 弹出选项窗口
                var dlg = new CloseOptionWindow { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    if (dlg.RememberChoice)
                    {
                        Properties.Settings.Default.CloseOptionRemembered = true;
                        Properties.Settings.Default.CloseOptionAction =
                            dlg.Action == CloseAction.MinimizeToTray ? "Minimize" : "Exit";
                        Properties.Settings.Default.Save();
                    }

                    if (dlg.Action == CloseAction.MinimizeToTray)
                    {
                        e.Cancel = true;
                        Hide();
                    }
                    else if (dlg.Action == CloseAction.Exit)
                    {
                        isExiting = true;
                        notifyIcon.Visible = false;
                        base.OnClosing(e);
                    }
                    else
                    {
                        e.Cancel = true; // Cancel
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(message))
            {
                Dispatcher.Invoke(() =>
                {
                    if (message == "notification")
                        FlashWindowHelper.Flash(this);
                    else if (message == "notification_clear")
                        FlashWindowHelper.StopFlash(this);
                });
            }
        }
    }
}