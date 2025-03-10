using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DiaryAssistant.Services
{
    // スタートアップサービス
    public class StartupService
    {
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "DiaryAssistant";

        public static void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string appPath = Process.GetCurrentProcess().MainModule.FileName;
                            key.SetValue(AppName, appPath);
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                            {
                                key.DeleteValue(AppName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"スタートアップ設定エラー: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey))
                {
                    return key?.GetValue(AppName) != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"スタートアップ状態確認エラー: {ex.Message}");
                return false;
            }
        }
    }
}