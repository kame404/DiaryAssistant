using DiaryAssistant.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiaryAssistant.Services
{
    public class AssistantManager
    {
        private static AssistantManager _instance;
        private static readonly object _lockObject = new object();

        // シングルトンインスタンスを取得
        public static AssistantManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new AssistantManager();
                        }
                    }
                }
                return _instance;
            }
        }

        // 利用可能なアシスタント一覧
        private Dictionary<string, AssistantInfo> _assistants = new Dictionary<string, AssistantInfo>();

        // 現在選択されているアシスタント
        private AssistantInfo _currentAssistant;

        // リソースのベースパス
        private string _resourcesPath;

        private AssistantManager()
        {
            _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
        }

        // アシスタントスキャン・初期化
        public void Initialize()
        {
            ScanAssistants();

            // 設定から選択済みアシスタントIDを取得
            var settings = DatabaseService.GetSettings();
            string selectedAssistantId = settings?.SelectedAssistantId;

            // 選択済みアシスタントを設定
            if (!string.IsNullOrEmpty(selectedAssistantId) && _assistants.ContainsKey(selectedAssistantId))
            {
                _currentAssistant = _assistants[selectedAssistantId];
            }
            else if (_assistants.ContainsKey("sophia")) // デフォルトはソフィア
            {
                _currentAssistant = _assistants["sophia"];
            }
            else if (_assistants.Count > 0) // ソフィアもなければ最初のアシスタント
            {
                _currentAssistant = _assistants.Values.First();
            }
            else
            {
                // 有効なアシスタントがない場合は初期アシスタントを作成
                CreateDefaultAssistant();
                if (_assistants.ContainsKey("sophia"))
                {
                    _currentAssistant = _assistants["sophia"];
                }
            }

            // 選択されたアシスタントを設定に保存
            if (_currentAssistant != null &&
                (settings == null || settings.SelectedAssistantId != _currentAssistant.Id))
            {
                SaveSelectedAssistant(_currentAssistant.Id);
            }
        }

        // アシスタントフォルダをスキャン
        public void ScanAssistants()
        {
            _assistants.Clear();

            try
            {
                // リソースフォルダが存在しない場合は作成
                if (!Directory.Exists(_resourcesPath))
                {
                    Directory.CreateDirectory(_resourcesPath);
                }

                // すべてのサブフォルダを取得
                var directories = Directory.GetDirectories(_resourcesPath);

                foreach (var dir in directories)
                {
                    var assistant = AssistantInfo.FromFolder(dir);
                    if (assistant != null)
                    {
                        _assistants[assistant.Id] = assistant;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"アシスタントスキャンエラー: {ex.Message}");
            }
        }

        // デフォルトアシスタント（ソフィア）を作成
        private void CreateDefaultAssistant()
        {
            try
            {
                string sophiaPath = Path.Combine(_resourcesPath, "sophia");
                string legacyIconsPath = Path.Combine(_resourcesPath, "icons");
                string sophiaIconsPath = Path.Combine(sophiaPath, "icons");
                string legacyPromptsPath = Path.Combine(_resourcesPath, "prompts.json");
                string sophiaPromptsPath = Path.Combine(sophiaPath, "prompts.json");
                string legacyFallbackPath = Path.Combine(_resourcesPath, "fallback.json");
                string sophiaFallbackPath = Path.Combine(sophiaPath, "fallback.json");
                string sophiaConfigPath = Path.Combine(sophiaPath, "config.json");
                string legacyConfigPath = Path.Combine(_resourcesPath, "config.json");

                // ソフィアフォルダが存在しない場合は作成
                if (!Directory.Exists(sophiaPath))
                {
                    Directory.CreateDirectory(sophiaPath);
                }

                // アイコンフォルダを移動/コピー
                if (Directory.Exists(legacyIconsPath))
                {
                    if (!Directory.Exists(sophiaIconsPath))
                    {
                        Directory.CreateDirectory(sophiaIconsPath);
                    }

                    // アイコンファイルをコピー
                    foreach (var file in Directory.GetFiles(legacyIconsPath))
                    {
                        string destFile = Path.Combine(sophiaIconsPath, Path.GetFileName(file));
                        if (!File.Exists(destFile))
                        {
                            File.Copy(file, destFile);
                        }
                    }
                }
                else
                {
                    // アイコンフォルダが存在しない場合は新規作成
                    if (!Directory.Exists(sophiaIconsPath))
                    {
                        Directory.CreateDirectory(sophiaIconsPath);
                    }
                }

                // プロンプトファイルをコピー
                if (File.Exists(legacyPromptsPath))
                {
                    // ソフィアプロンプトパスの親ディレクトリが存在するか確認
                    string sophiaPromptsDir = Path.GetDirectoryName(sophiaPromptsPath);
                    if (!Directory.Exists(sophiaPromptsDir))
                    {
                        Directory.CreateDirectory(sophiaPromptsDir);
                    }
                    File.Copy(legacyPromptsPath, sophiaPromptsPath, true);
                }
                else
                {
                    // プロンプトファイルがない場合はデフォルト作成
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

                // フォールバックファイルをコピー
                if (File.Exists(legacyFallbackPath))
                {
                    File.Copy(legacyFallbackPath, sophiaFallbackPath, true);
                }
                else
                {
                    // フォールバックファイルがない場合はデフォルト作成
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

                // 設定ファイルの処理
                if (File.Exists(legacyConfigPath))
                {
                    // ソフィア設定パスの親ディレクトリが存在するか確認
                    string sophiaConfigDir = Path.GetDirectoryName(sophiaConfigPath);
                    if (!Directory.Exists(sophiaConfigDir))
                    {
                        Directory.CreateDirectory(sophiaConfigDir);
                    }
                    File.Copy(legacyConfigPath, sophiaConfigPath, true);
                }
                else
                {
                    // 設定ファイルをデフォルト内容で新規作成
                    // ソフィア設定パスの親ディレクトリが存在するか確認
                    string sophiaConfigDir = Path.GetDirectoryName(sophiaConfigPath);
                    if (!Directory.Exists(sophiaConfigDir))
                    {
                        Directory.CreateDirectory(sophiaConfigDir);
                    }
                    string defaultConfig = @"{
  ""Name"": ""ソフィア"",
  ""Description"": ""落ち着いた口調で会話するアシスタント"",
  ""Personality"": ""優しく丁寧、穏やかな性格"",
  ""Author"": ""kame404"",
  ""Version"": ""1.0""
}";
                    File.WriteAllText(sophiaConfigPath, defaultConfig);
                }

                // アシスタント一覧に追加
                var assistant = AssistantInfo.FromFolder(sophiaPath);
                if (assistant != null)
                {
                    _assistants[assistant.Id] = assistant;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"デフォルトアシスタント作成エラー: {ex.Message}");
            }
        }

        private void SaveSelectedAssistant(string assistantId)
        {
            try
            {
                var settings = DatabaseService.GetSettings();
                if (settings == null)
                {
                    settings = new AppSettings
                    {
                        UserName = Environment.UserName,
                        AiAssistantName = GetAssistantName(assistantId),
                        SelectedAssistantId = assistantId,
                        NotificationFrequencyMinutes = 5,
                        NotificationStartTime = new TimeSpan(9, 0, 0),
                        NotificationEndTime = new TimeSpan(1, 0, 0),
                        GetActiveWindowInfo = true,
                        RunAtStartup = false,
                        GeminiApiKey = "",
                        GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
                        FontFamily = "Yu Gothic",
                        FontSize = 16.0
                    };
                }
                else
                {
                    settings.SelectedAssistantId = assistantId;
                    settings.AiAssistantName = GetAssistantName(assistantId);
                }

                // データベースが正しく初期化されているかチェック
                if (!string.IsNullOrEmpty(settings.DatabasePath) && System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(settings.DatabasePath)))
                {
                    DatabaseService.SaveSettings(settings);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("データベースパスが初期化されていないため、設定は保存されません。");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"アシスタント設定保存エラー: {ex.Message}");
            }
        }

        // アシスタントIDからアシスタント名を取得
        private string GetAssistantName(string assistantId)
        {
            if (_assistants.TryGetValue(assistantId, out var assistant))
            {
                return assistant.Config.Name;
            }
            return assistantId; // アシスタント情報がなければIDをそのまま返す
        }

        // 現在選択されているアシスタントを取得
        public AssistantInfo GetCurrentAssistant()
        {
            return _currentAssistant;
        }

        // 利用可能なすべてのアシスタント情報を取得
        public List<AssistantInfo> GetAllAssistants()
        {
            return _assistants.Values.ToList();
        }

        // アシスタントを切り替え
        public bool SwitchAssistant(string assistantId)
        {
            if (_assistants.TryGetValue(assistantId, out var assistant))
            {
                _currentAssistant = assistant;
                SaveSelectedAssistant(assistantId);
                return true;
            }
            return false;
        }
    }
}