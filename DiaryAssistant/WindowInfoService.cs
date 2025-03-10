using System;
using System.Diagnostics;
using System.Text;

namespace DiaryAssistant.Services
{
    // ウィンドウ情報サービス
    public class WindowInfoService
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        public string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                int length = GetWindowTextLength(handle);

                if (length == 0)
                {
                    return null;
                }

                var builder = new StringBuilder(length + 1);
                GetWindowText(handle, builder, builder.Capacity);

                return builder.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"アクティブウィンドウ取得エラー: {ex.Message}");
                return null;
            }
        }
    }
}