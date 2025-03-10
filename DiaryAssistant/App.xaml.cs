using DiaryAssistant.Services;
using DiaryAssistant.Models;
using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DiaryAssistant
{
    public partial class App : Application
    {
        private NotificationService _notificationService;
        private Mutex _appMutex;
        private const string MutexName = "Global\\DiaryAssistantMutex";
        private bool _ownsMutex = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // リソース確認を最初に行う
            if (!VerifyRequiredResources())
            {
                MessageBox.Show(
                    "必要なリソースファイルが見つかりません。\n" +
                    "アプリケーションフォルダ内にresourcesフォルダと\n" +
                    "アイコン画像が存在することを確認してください。",
                    "起動エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
                return;
            }


            // アシスタントマネージャーの初期化 - 順序を変更
            AssistantManager.Instance.Initialize();

            // アプリケーションの多重起動を防止（改善版）
            try
            {
                // グローバルミューテックスの作成を試行
                // セキュリティ設定を追加してすべてのユーザーからのアクセスを許可
                var mutexSecurity = new MutexSecurity();
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                mutexSecurity.AddAccessRule(
                    new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));

                // 既存のミューテックスを開く、または新しいミューテックスを作成
                _appMutex = new Mutex(false, MutexName, out bool createdNew, mutexSecurity);

                // ミューテックスの取得を試みる（タイムアウト5秒）
                _ownsMutex = _appMutex.WaitOne(TimeSpan.FromSeconds(5), false);

                if (!_ownsMutex)
                {
                    // 既に実行中の場合
                    MessageBox.Show("アプリケーションは既に実行中です。\nタスクトレイを確認してください。", "多重起動エラー",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    // ミューテックスをクリーンアップ
                    if (_appMutex != null)
                    {
                        _appMutex.Close();
                        _appMutex = null;
                    }

                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                // ミューテックス作成エラーをログに記録
                LogError(new Exception($"ミューテックス作成エラー: {ex.Message}", ex));

                // エラーが発生しても起動を続行
                MessageBox.Show(
                    "多重起動チェックでエラーが発生しましたが、アプリケーションは起動します。\n" +
                    "他のインスタンスが実行中の場合、予期せぬ動作が発生する可能性があります。",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // 必要なディレクトリ作成
            EnsureDirectoriesExist();

            // リソースの初期化
            InitializeResources();

            // サービスの初期化（データベースを先に初期化）
            InitializeServices();

            // アシスタントマネージャーの初期化（データベース初期化後に実行）
            AssistantManager.Instance.Initialize();

            // グローバル例外ハンドラー
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // プロセス終了イベントを監視
            Current.Exit += Current_Exit;
        }

        // リソースの確認を行うメソッド
        private bool VerifyRequiredResources()
        {
            try
            {
                string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");

                // resourcesフォルダの存在確認
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }

                // sophiaフォルダの存在確認
                string sophiaPath = Path.Combine(resourcesPath, "sophia");
                if (!Directory.Exists(sophiaPath))
                {
                    Directory.CreateDirectory(sophiaPath);
                }

                // sophiaのiconsフォルダの存在確認
                string sophiaIconsPath = Path.Combine(sophiaPath, "icons");
                if (!Directory.Exists(sophiaIconsPath))
                {
                    Directory.CreateDirectory(sophiaIconsPath);
                }

                // sophiaのnormal.pngの存在確認
                if (!File.Exists(Path.Combine(sophiaIconsPath, "normal.png")))
                {
                    return false; // 必須リソースがない
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            // アプリケーション終了時にミューテックスを解放
            ReleaseMutex();
        }

        private void ReleaseMutex()
        {
            try
            {
                if (_ownsMutex && _appMutex != null)
                {
                    // ミューテックスが有効かどうかを確認してから解放する
                    try
                    {
                        _appMutex.ReleaseMutex();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 既に解放されている場合は無視
                    }
                    catch (ApplicationException)
                    {
                        // ミューテックスを所有していない場合のエラーは無視
                    }
                    _ownsMutex = false;
                }

                if (_appMutex != null)
                {
                    try
                    {
                        _appMutex.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 既にクローズされている場合は無視
                    }
                    _appMutex = null;
                }
            }
            catch (Exception ex)
            {
                // ミューテックス解放時のエラーをログに記録
                LogError(new Exception($"ミューテックス解放エラー: {ex.Message}", ex));
            }
        }

        private void EnsureDirectoriesExist()
        {
            // アプリケーションのデータディレクトリの確保
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiaryAssistant");
            EnsureDirectoryExists(appDataPath);

            // データディレクトリの確保
            string dataDirectory = Path.Combine(appDataPath, "data");
            EnsureDirectoryExists(dataDirectory);

            // リソースディレクトリの確保
            string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
            EnsureDirectoryExists(resourcesPath);

        }

        /// <summary>
        /// 指定されたパスのディレクトリが存在しない場合に作成します
        /// </summary>
        /// <param name="path">作成するディレクトリのパス</param>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void InitializeResources()
        {
            string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
            string sophiaPath = Path.Combine(resourcesPath, "sophia");
            string sophiaIconsPath = Path.Combine(sophiaPath, "icons");
            string sophiaPromptsPath = Path.Combine(sophiaPath, "prompts.json");
            string sophiaFallbackPath = Path.Combine(sophiaPath, "fallback.json");

            // フォルダ構造の確保
            EnsureDirectoryExists(resourcesPath);
            EnsureDirectoryExists(sophiaPath);
            EnsureDirectoryExists(sophiaIconsPath);

            // デフォルトのプロンプトファイル作成
            if (!File.Exists(sophiaPromptsPath))
            {
                string defaultPrompts = @"{
  ""systemPrompt"": ""あなたは、ユーザーの傍らにいるAIアシスタントです。名前は「{アシスタント名}」です。大学生の女性のような落ち着いた口調で、敬語を使って自然な独り言をつぶやきます。独り言は状況に合わせた短い感想や観察を述べるものです。\n\n返信は<response>タグ内に記述し、感情を表す属性を付けてください。例：<response emotion=\""happy\"">こんにちは！</response>\n\n感情の種類: normal, happy, sad, angry, surprised, thinking"",
  ""greetingPrompts"": [
    ""もう{時間}時ですか…時間が経つのは早いですね"",
    ""ふむ、なかなか面白そうなことをされていますね"",
    ""あ、{アプリ名}をお使いになっているんですね"",
    ""ちょっと小腹が空いてきましたね"",
    ""そろそろ休憩時間かもしれませんね"",
    ""ん、何か面白いことがありそうですね"",
  ],
  ""contextPrompts"": {
  ""default"": ""ユーザーは現在「{windowTitle}」というウィンドウを開いています。この情報を参考に自然な会話ができるかもしれません。""
},
  ""diaryPrompts"": [
    ""今日はどんな一日でしたか？"",
    ""最近、嬉しかったことや楽しかったことはありますか？"",
    ""何か悩んでいることはありますか？"",
    ""最近の趣味や関心事は何ですか？"",
    ""今週末の予定はありますか？""
  ],
  ""followUpPrompts"": [
    ""それについてもう少し詳しく教えていただけますか？"",
    ""それはいつ頃のことですか？"",
    ""そのときどう感じましたか？"",
    ""他に印象に残ったことはありますか？"",
    ""それは初めての経験でしたか？""
  ]
}";
                File.WriteAllText(sophiaPromptsPath, defaultPrompts);
            }

            // デフォルトのフォールバックファイル作成
            if (!File.Exists(sophiaFallbackPath))
            {
                string defaultFallback = @"{
  ""connectionErrors"": [
    ""申し訳ありません、ネットワーク接続に問題があるようです。また後でお話しましょう。"",
    ""インターネット接続が不安定なようです。少し経ってからもう一度お声かけします。"",
    ""通信エラーが発生しました。後ほど再試行します。""
  ],
  ""apiErrors"": [
    ""AI連携サービスに一時的な問題が発生しています。設定を確認するか、しばらく待ってからお試しください。"",
    ""APIキーに問題があるかもしれません。設定画面でAPIキーを確認してください。"",
    ""AIサービスへのリクエストに失敗しました。設定を確認してください。""
  ],
  ""generalErrors"": [
    ""予期せぬエラーが発生しました。アプリを再起動してみてください。"",
    ""問題が発生しました。しばらくしてからもう一度お試しください。"",
    ""エラーが発生しました。設定をご確認ください。""
  ]
}";
                File.WriteAllText(sophiaFallbackPath, defaultFallback);
            }
        }

        private void InitializeServices()
        {
            // データベースサービスの初期化
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiaryAssistant");
            string dbPath = Path.Combine(appDataPath, "data", "diary.db");

            DatabaseService.Initialize(dbPath);

            // 設定の初期化
            var settings = DatabaseService.GetSettings();
            if (settings == null)
            {
                // デフォルト設定の作成
                settings = new AppSettings
                {
                    UserName = Environment.UserName,
                    AiAssistantName = "ソフィア",
                    NotificationFrequencyMinutes = 5,
                    NotificationStartTime = new TimeSpan(9, 0, 0),
                    NotificationEndTime = new TimeSpan(1, 0, 0),
                    GetActiveWindowInfo = false,
                    RunAtStartup = false,
                    GeminiApiKey = "",
                    DatabasePath = dbPath,
                    GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent" // デフォルトのURLを設定
                };
                DatabaseService.SaveSettings(settings);
            }
            else if (string.IsNullOrEmpty(settings.GeminiApiBaseUrl))
            {
                // 既存の設定にベースURLが未設定の場合、デフォルト値を設定して保存
                settings.GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
                DatabaseService.SaveSettings(settings);
            }
            // 通知サービスの初期化 - シングルトンを使用
            _notificationService = NotificationService.Instance;
            _notificationService.Initialize();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError(e.ExceptionObject as Exception);

            // 未処理の例外発生時にもミューテックスを解放
            ReleaseMutex();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);
            e.Handled = true;
        }

        private void LogError(Exception ex)
        {
            if (ex == null) return;

            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiaryAssistant");

                string logPath = Path.Combine(appDataPath, "error.log");
                string logMessage = $"[{DateTime.Now}] Error: {ex.Message}\r\nStackTrace: {ex.StackTrace}\r\n\r\n";

                File.AppendAllText(logPath, logMessage);

                MessageBox.Show($"エラーが発生しました。詳細はログを確認してください：\n{ex.Message}",
                                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // ログ記録中のエラーは無視
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // リソースの開放
            _notificationService?.Dispose();

            // ミューテックスを解放（既に解放されている場合は二重解放を防ぐ）
            if (_ownsMutex && _appMutex != null)
            {
                try
                {
                    _appMutex.ReleaseMutex();
                    _ownsMutex = false;
                }
                catch (ObjectDisposedException)
                {
                    // 既に解放されている場合は無視
                }
                catch (ApplicationException)
                {
                    // ミューテックスを所有していない場合のエラーは無視
                }
            }

            // ミューテックスをクローズ
            if (_appMutex != null)
            {
                try
                {
                    _appMutex.Close();
                }
                catch (ObjectDisposedException)
                {
                    // 既にクローズされている場合は無視
                }
                _appMutex = null;
            }

            // データベース接続のクリーンアップ
            DatabaseService.Cleanup();

            base.OnExit(e);
        }
    }
}