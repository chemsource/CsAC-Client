using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CsAC_Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 记录启动日志
            Log("程序启动");

            // 捕获 UI 线程异常
            this.DispatcherUnhandledException += (s, ex) =>
            {
                Log("UI 线程异常: " + ex.Exception.ToString());
                ex.Handled = true; // 阻止崩溃，但可能窗口仍无法显示
                MessageBox.Show("发生错误，详见桌面上的 CsAC_error.log", "错误");
            };

            // 捕获后台线程异常
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                Log("未处理异常: " + ex.ExceptionObject.ToString());
            };

            base.OnStartup(e);
        }

        private static void Log(string msg)
        {
            // 1. 获取程序所在目录（exe 所在位置）
            string appDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. 拼接 logs 子文件夹路径
            string logDir = Path.Combine(appDir, "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // 3. 生成文件名：日期-时间.log
            string fileName = DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".log";
            string fullPath = Path.Combine(logDir, fileName);

            // 4. 追加写入日志
            File.AppendAllText(fullPath, $"[{DateTime.Now}] {msg}{Environment.NewLine}");
        }
    }
}