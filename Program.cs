using System;
using System.Threading;
using AcrDesigner;
using Avalonia;

namespace AcrossReportDesigner
{
    internal class Program
    {
        private const string MutexName = "AcrossReportDesigner_SingleInstance";
        [STAThread]
        public static void Main(string[] args)
        {
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                ShowMessage();
                return;
            }
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        private static void ShowMessage()
        {
            // Windows専用
            System.Runtime.InteropServices.Marshal
                .ThrowExceptionForHR(
                    MessageBox(IntPtr.Zero,
                               "すでに起動しています。",
                               "Across Report Designer",
                               0));
        }
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int MessageBox(
            IntPtr hWnd,
            string text,
            string caption,
            int type);
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
