using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CsAC_Client
{
    public static class FlashWindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_TIMERNOFG = 12;

        public static void Flash(Window window)
        {
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            var fInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = hwnd,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = uint.MaxValue,
                dwTimeout = 0
            };
            FlashWindowEx(ref fInfo);
        }

        public static void StopFlash(Window window)
        {
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            var fInfo = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO)),
                hwnd = hwnd,
                dwFlags = FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };
            FlashWindowEx(ref fInfo);
        }
    }
}