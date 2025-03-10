using DiaryAssistant.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace DiaryAssistant.Services
{
    // リソースサービス
    public class ResourceService
    {
        private static readonly Random _random = new Random();

        public PromptSettings GetPromptSettings()
        {
            try
            {
                // 現在のアシスタントからプロンプト設定を取得
                var assistant = AssistantManager.Instance.GetCurrentAssistant();
                string promptsPath;

                if (assistant != null)
                {
                    promptsPath = assistant.PromptsPath;
                }
                else
                {
                    promptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "sophia", "prompts.json");
                }

                if (File.Exists(promptsPath))
                {
                    string json = File.ReadAllText(promptsPath);
                    return JsonConvert.DeserializeObject<PromptSettings>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"プロンプト設定読み込みエラー: {ex.Message}");
            }

            return null;
        }

        public FallbackMessages GetFallbackMessages()
        {
            try
            {
                // 現在のアシスタントからフォールバック設定を取得
                var assistant = AssistantManager.Instance.GetCurrentAssistant();
                string fallbackPath;

                if (assistant != null)
                {
                    fallbackPath = assistant.FallbackPath;
                }
                else
                {
                    fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "sophia", "fallback.json");
                }

                if (File.Exists(fallbackPath))
                {
                    string json = File.ReadAllText(fallbackPath);
                    return JsonConvert.DeserializeObject<FallbackMessages>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"フォールバックメッセージ読み込みエラー: {ex.Message}");
            }

            return null;
        }

        public string GetRandomFallbackMessage(string category)
        {
            try
            {
                var fallback = GetFallbackMessages();
                if (fallback != null)
                {
                    List<string> messages = null;

                    if (category == "connectionErrors")
                    {
                        messages = fallback.ConnectionErrors;
                    }
                    else if (category == "apiErrors")
                    {
                        messages = fallback.ApiErrors;
                    }
                    else
                    {
                        messages = fallback.GeneralErrors;
                    }

                    if (messages != null && messages.Count > 0)
                    {
                        return messages[_random.Next(messages.Count)];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"フォールバックメッセージ取得エラー: {ex.Message}");
            }

            return "エラーが発生しました。しばらくしてからもう一度お試しください。";
        }

        public BitmapImage GetEmotionIcon(string emotion, string assistantId = null)
        {
            try
            {
                // 指定されたアシスタントIDからアシスタントを取得
                AssistantInfo assistant = null;

                if (!string.IsNullOrEmpty(assistantId))
                {
                    var allAssistants = AssistantManager.Instance.GetAllAssistants();
                    assistant = allAssistants.FirstOrDefault(a => a.Id == assistantId);
                }

                // 指定がないか見つからない場合は現在のアシスタントを使用
                if (assistant == null)
                {
                    assistant = AssistantManager.Instance.GetCurrentAssistant();
                }

                string iconPath;

                if (assistant != null)
                {
                    // アシスタント固有のアイコンフォルダからパスを構築
                    iconPath = Path.Combine(assistant.IconsPath, $"{emotion}.png");
                }
                else
                {
                    // レガシーフォールバックを切り捨て、null を返す
                    return null;
                }

                if (!File.Exists(iconPath))
                {
                    // デフォルトアイコン
                    emotion = "normal";
                    if (assistant != null)
                    {
                        iconPath = assistant.NormalIconPath;
                    }
                    else
                    {
                        return null;
                    }

                    if (!File.Exists(iconPath))
                    {
                        return null;
                    }
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(iconPath);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"感情アイコン読み込みエラー: {ex.Message}");
                return null;
            }
        }
    }
}