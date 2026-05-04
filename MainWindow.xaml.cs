using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Text.Json;   // ← 新增，用于解析 JSON

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
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
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
            string raw = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(raw)) return;

            // 简单消息兼容
            if (raw == "notification_clear") { Dispatcher.Invoke(() => FlashWindowHelper.StopFlash(this)); return; }
            if (raw == "notification") { Dispatcher.Invoke(() => FlashWindowHelper.Flash(this)); return; }

            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 解析 JSON，对大小写不敏感
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var info = JsonSerializer.Deserialize<NotificationMsg>(raw, options);

                    string alertText = info?.Text ?? raw;  // 解析失败则显示原始字符串

                    // 如果窗口隐藏或最小化，先恢复
                    if (WindowState == WindowState.Minimized || Visibility != Visibility.Visible)
                    {
                        Show();
                        WindowState = WindowState.Normal;
                        Activate();
                    }

                    FlashWindowHelper.Flash(this);

                    notifyIcon.BalloonTipTitle = "CsAC 聊天";
                    notifyIcon.BalloonTipText = alertText;
                    notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                    notifyIcon.ShowBalloonTip(5000);

                    //if (info?.Type == "at" || info?.Type == "reply")
                    //    System.Media.SystemSounds.Exclamation.Play();
                    //else
                    //    System.Media.SystemSounds.Asterisk.Play();
                }
                catch (Exception ex)
                {
                    // 如果发生任何异常，用最原始方式弹出来
                    notifyIcon.BalloonTipTitle = "CsAC 聊天";
                    notifyIcon.BalloonTipText = ex.Message;
                    notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
                    notifyIcon.ShowBalloonTip(5000);
                }
            });
        }
    }

    // 放在 MainWindow 类的最后一个 } 之后，namespace 的最后一个 } 之前
    public class NotificationMsg
    {
        public string Type { get; set; }   // "at", "reply", "new", "notice"
        public string Text { get; set; }   // 气泡显示的文本
    }
}