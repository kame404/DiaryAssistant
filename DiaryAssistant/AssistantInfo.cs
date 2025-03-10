using Newtonsoft.Json;
using System;
using System.IO;

namespace DiaryAssistant.Models
{
    public class AssistantConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Personality { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
    }

    public class AssistantInfo
    {
        // フォルダ名がIDとなる
        public string Id { get; private set; }
        public string FolderPath { get; private set; }
        public AssistantConfig Config { get; private set; }

        // アイコンパス
        public string IconsPath => Path.Combine(FolderPath, "icons");
        // プロンプトファイルパス
        public string PromptsPath => Path.Combine(FolderPath, "prompts.json");
        // フォールバックファイルパス
        public string FallbackPath => Path.Combine(FolderPath, "fallback.json");
        // 設定ファイルパス
        public string ConfigPath => Path.Combine(FolderPath, "config.json");

        // ノーマルアイコンのパス
        public string NormalIconPath => Path.Combine(IconsPath, "normal.png");
        public string NormalIconIcoPath => Path.Combine(IconsPath, "normal.ico");

        // 既存のフォルダからアシスタント情報を読み込む
        public static AssistantInfo FromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return null;

            string id = Path.GetFileName(folderPath);

            // 必須ファイルとフォルダの確認
            string iconsPath = Path.Combine(folderPath, "icons");
            string promptsPath = Path.Combine(folderPath, "prompts.json");
            string fallbackPath = Path.Combine(folderPath, "fallback.json");
            string configPath = Path.Combine(folderPath, "config.json");
            string normalIconPath = Path.Combine(iconsPath, "normal.png");

            // アイコンフォルダとノーマルアイコンは必須
            if (!Directory.Exists(iconsPath) || !File.Exists(normalIconPath))
                return null;

            var assistant = new AssistantInfo
            {
                Id = id,
                FolderPath = folderPath,
                Config = new AssistantConfig { Name = id } // デフォルト名はフォルダ名
            };

            // config.jsonが存在すれば読み込む
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    assistant.Config = JsonConvert.DeserializeObject<AssistantConfig>(json)
                        ?? new AssistantConfig { Name = id };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"アシスタント設定ファイル読み込みエラー: {ex.Message}");
                }
            }

            return assistant;
        }

        // config.jsonを生成する
        public void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"アシスタント設定保存エラー: {ex.Message}");
            }
        }
    }
}