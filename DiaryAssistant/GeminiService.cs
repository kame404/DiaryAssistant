using DiaryAssistant.Models;
using DiaryAssistant.Properties;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiaryAssistant.Services
{
    // Gemini APIサービス
    public class GeminiService
    {
        private readonly ResourceService _resourceService = new ResourceService();
        private const string DEFAULT_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        // 共通APIリクエスト処理メソッド
        private async Task<string> ExecuteGeminiRequest(string prompt, double temperature = 0.7, int maxTokens = 800)
        {
            var settings = DatabaseService.GetSettings();
            if (settings == null)
            {
                return _resourceService.GetRandomFallbackMessage("apiErrors");
            }

            // Ollamaが優先されるか確認
            if (settings.UseOllama)
            {
                return await ExecuteOllamaRequest(prompt, temperature, maxTokens);
            }

            // Gemini API処理
            if (string.IsNullOrEmpty(settings.GeminiApiKey))
            {
                return _resourceService.GetRandomFallbackMessage("apiErrors");
            }

            // APIのベースURLを設定から取得
            string apiBaseUrl = !string.IsNullOrEmpty(settings.GeminiApiBaseUrl)
                ? settings.GeminiApiBaseUrl
                : DEFAULT_API_URL;

            try
            {
                var client = new RestClient($"{apiBaseUrl}?key={settings.GeminiApiKey}");
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Content-Type", "application/json");

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = maxTokens
                    }
                };

                request.AddJsonBody(JsonConvert.SerializeObject(requestPayload));
                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    var responseData = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    string text = responseData.candidates[0].content.parts[0].text;
                    LogGeminiCommunication(prompt, text);
                    return text;
                }
                else
                {
                    Debug.WriteLine($"Gemini API エラー: {response.Content}");
                    return _resourceService.GetRandomFallbackMessage("apiErrors");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return _resourceService.GetRandomFallbackMessage("connectionErrors");
            }
        }

        // Ollama APIリクエスト処理メソッド
        private async Task<string> ExecuteOllamaRequest(string prompt, double temperature = 0.5, int maxTokens = 800)
        {
            var settings = DatabaseService.GetSettings();
            if (settings == null || string.IsNullOrEmpty(settings.OllamaApiUrl) || string.IsNullOrEmpty(settings.OllamaModelName))
            {
                return _resourceService.GetRandomFallbackMessage("apiErrors");
            }

            try
            {
                var client = new RestClient(settings.OllamaApiUrl);
                var request = new RestRequest("", Method.Post);
                request.AddHeader("Content-Type", "application/json");

                // Ollamaの正しいリクエスト形式に修正
                var requestPayload = new
                {
                    model = settings.OllamaModelName,
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
                    stream = false,  // ストリーミングを無効化
                    temperature = temperature,
                    max_tokens = maxTokens
                };

                string requestJson = JsonConvert.SerializeObject(requestPayload);
                Debug.WriteLine($"Ollamaリクエスト: {requestJson}");  // デバッグ用

                request.AddJsonBody(requestJson);
                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Debug.WriteLine($"Ollamaレスポンス: {response.Content}");  // デバッグ用

                    var responseData = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    string text = "";

                    // レスポンス形式のパース方法を改善
                    try
                    {
                        // 最初のアプローチ - 一般的なLLMのレスポンス形式
                        text = responseData.message.content;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"第1パース方法エラー: {ex.Message}");

                        try
                        {
                            // 第2のアプローチ
                            text = responseData.choices[0].message.content;
                        }
                        catch (Exception ex2)
                        {
                            Debug.WriteLine($"第2パース方法エラー: {ex2.Message}");

                            // 第3のアプローチ
                            try
                            {
                                text = responseData.response;
                            }
                            catch (Exception ex3)
                            {
                                Debug.WriteLine($"第3パース方法エラー: {ex3.Message}");
                                // フォールバック: JSONをそのまま返す
                                text = response.Content;
                            }
                        }
                    }

                    LogGeminiCommunication(prompt, text);
                    return text;
                }
                else
                {
                    Debug.WriteLine($"Ollama API エラー: {response.StatusCode} - {response.Content}");
                    return $"Ollama通信エラー: {response.StatusCode} - {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ollama通信エラー: {ex.Message}");
                return $"Ollama通信エラー: {ex.Message}";
            }
        }

        public async Task<string> GetGreetingResponse(string userName, string activeWindowInfo = null)
        {
            try
            {
                // プロンプト作成
                var promptData = _resourceService.GetPromptSettings();
                if (promptData == null)
                {
                    return _resourceService.GetRandomFallbackMessage("generalErrors");
                }

                var settings = DatabaseService.GetSettings();
                if (settings == null) return _resourceService.GetRandomFallbackMessage("generalErrors");

                // システムプロンプト
                string systemPrompt = promptData.SystemPrompt.Replace("{アシスタント名}", settings.AiAssistantName);

                // 最終プロンプト作成
                string finalPrompt = $"{systemPrompt}\n\n";
                finalPrompt += $"アシスタント名: {settings.AiAssistantName}\n";
                finalPrompt += $"ユーザー名: {userName}\n";
                finalPrompt += $"現在時刻: {DateTime.Now}\n";

                if (!string.IsNullOrEmpty(activeWindowInfo))
                {
                    finalPrompt += $"アクティブウィンドウ: {activeWindowInfo}\n";
                }

                finalPrompt += $"モード: 独り言\n\n";
                finalPrompt += "あなたは独り言モードで、ユーザーに話しかけるのではなく、ユーザーの状況に合わせた自然な独り言をつぶやきます。\n";
                finalPrompt += "日記を書くよう促したり、質問をするのではなく、アクティブウィンドウや時間帯、過去の記録などから連想される思いつきや感想を述べてください。\n";
                finalPrompt += "ユーザーが「この後会話を続けるかもしれない」とは考えず、一度きりの独り言として自然な文章を生成してください。\n\n";

                // 本日の日記データを取得（箇条書き情報を含む）
                var todayEntry = DatabaseService.GetDiaryEntry(DateTime.Today);
                if (todayEntry != null && todayEntry.BulletPoints != null && todayEntry.BulletPoints.Count > 0)
                {
                    finalPrompt += "## 本日記録した箇条書きメモ：\n";
                    foreach (var point in todayEntry.BulletPoints)
                    {
                        finalPrompt += $"- {point.Content}\n";
                    }
                    finalPrompt += "\n";
                }

                // 過去一週間の日記データを含める
                var pastWeekEntries = DatabaseService.GetAllDiaryEntries()
                    .Where(e => e.Date >= DateTime.Today.AddDays(-7) && e.Date < DateTime.Today)
                    .OrderByDescending(e => e.Date)
                    .Take(3) // 最新3件のみ
                    .ToList();

                if (pastWeekEntries.Count > 0)
                {
                    finalPrompt += "## 過去数日の記録：\n";
                    foreach (var entry in pastWeekEntries)
                    {
                        if (entry.BulletPoints != null && entry.BulletPoints.Count > 0)
                        {
                            finalPrompt += $"[{entry.Date:yyyy/MM/dd}] ポイント: ";
                            finalPrompt += string.Join(", ", entry.BulletPoints.Take(3).Select(p => p.Content));
                            finalPrompt += "\n";
                        }
                    }
                    finalPrompt += "\n";
                }

                // 現在の時間帯に応じたコンテキストを追加
                var currentHour = DateTime.Now.Hour;
                if (currentHour >= 5 && currentHour < 10)
                {
                    finalPrompt += "現在は朝の時間帯です。\n";
                }
                else if (currentHour >= 10 && currentHour < 15)
                {
                    finalPrompt += "現在は昼の時間帯です。\n";
                }
                else if (currentHour >= 15 && currentHour < 19)
                {
                    finalPrompt += "現在は夕方の時間帯です。\n";
                }
                else
                {
                    finalPrompt += "現在は夜の時間帯です。\n";
                }

                finalPrompt += "次のような独り言を<response>タグ内に生成してください：\n";
                finalPrompt += "1. 短めの文章（1〜3文程度）\n";
                finalPrompt += "2. 日記を書かせようとする質問や促しはNG\n";
                finalPrompt += "3. ユーザーの現在の状況（アクティブウィンドウ、時間帯）に関連した内容\n";
                finalPrompt += "4. 敬語で、まるでそばにいるAIアシスタントがつぶやいているような印象\n";
                finalPrompt += "5. 感情属性（emotion）を適切なものにする\n\n";

                // 共通APIリクエスト処理を使用
                return await ExecuteGeminiRequest(finalPrompt, 0.8, 800);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return _resourceService.GetRandomFallbackMessage("connectionErrors");
            }
        }

        public async Task<string> GetDiaryResponse(string userName, string userInput, List<ConversationMessage> recentConversation = null, List<ConversationSummary> conversationSummaries = null)
        {
            try
            {
                // プロンプト設定を取得
                var promptData = _resourceService.GetPromptSettings();
                if (promptData == null)
                {
                    return _resourceService.GetRandomFallbackMessage("generalErrors");
                }

                var settings = DatabaseService.GetSettings();
                if (settings == null) return _resourceService.GetRandomFallbackMessage("generalErrors");

                // システムプロンプト
                string systemPrompt = promptData.SystemPrompt.Replace("{アシスタント名}", settings.AiAssistantName);

                // 最終プロンプト作成
                string finalPrompt = $"{systemPrompt}\n\n";
                finalPrompt += $"アシスタント名: {settings.AiAssistantName}\n";
                finalPrompt += $"ユーザー名: {userName}\n";
                finalPrompt += $"現在時刻: {DateTime.Now}\n";
                finalPrompt += $"現在の曜日: {GetJapaneseDayOfWeek(DateTime.Now.DayOfWeek)}\n";
                finalPrompt += $"モード: 日記\n\n";

                // 日記モードの指示
                finalPrompt += "あなたは日記モードで、ユーザーが日記を書くのを手助けします。\n";
                finalPrompt += "1. ユーザーの発言に適切に応答しながら、新たな話題を提供してください\n";
                finalPrompt += "2. 過去の会話や記録を参照して、関連する質問をしてください\n";
                finalPrompt += "3. 簡潔でオープンエンドな質問で、ユーザーが考えを深められるよう促してください\n";
                finalPrompt += "4. 日常の出来事、感情、考え、人間関係などについて質問してください\n";
                finalPrompt += "5. ユーザーが沈黙していても、新しい話題を提供できるよう準備してください\n";
                finalPrompt += "6. <response>タグ内に応答を、emotion属性に適切な感情を設定してください\n\n";

                // 本日の日記データを取得（箇条書き情報を含む）
                var todayEntry = DatabaseService.GetDiaryEntry(DateTime.Today);
                if (todayEntry != null && todayEntry.BulletPoints != null && todayEntry.BulletPoints.Count > 0)
                {
                    finalPrompt += "## 本日記録した箇条書きメモ：\n";
                    foreach (var point in todayEntry.BulletPoints)
                    {
                        finalPrompt += $"- {point.Content}\n";
                    }
                    finalPrompt += "\n";
                }

                // 過去一週間の日記データを含める - ここを追加
                var pastWeekEntries = DatabaseService.GetAllDiaryEntries()
                    .Where(e => e.Date >= DateTime.Today.AddDays(-7) && e.Date < DateTime.Today)
                    .OrderByDescending(e => e.Date)
                    .Take(3) // 最新3件のみ
                    .ToList();

                if (pastWeekEntries.Count > 0)
                {
                    finalPrompt += "## 過去数日の記録：\n";
                    foreach (var entry in pastWeekEntries)
                    {
                        if (entry.BulletPoints != null && entry.BulletPoints.Count > 0)
                        {
                            finalPrompt += $"[{entry.Date:yyyy/MM/dd}] ポイント: ";
                            finalPrompt += string.Join(", ", entry.BulletPoints.Take(3).Select(p => p.Content));
                            finalPrompt += "\n";
                        }
                    }
                    finalPrompt += "\n";
                }

                // 日記プロンプトからランダムに選択（まだ会話がない場合）
                if ((recentConversation == null || recentConversation.Count == 0) &&
                    !string.IsNullOrEmpty(userInput) &&
                    userInput.Contains("日記を書きたい"))
                {
                    if (promptData.DiaryPrompts != null && promptData.DiaryPrompts.Count > 0)
                    {
                        Random random = new Random();
                        int index = random.Next(promptData.DiaryPrompts.Count);
                        finalPrompt += $"最初の質問として、次のプロンプトを使用してください: \"{promptData.DiaryPrompts[index]}\"\n\n";
                    }
                }

                // 会話の追加と重複チェック
                finalPrompt += "## 現在の会話：\n";

                // 最後のユーザーメッセージが現在のユーザー入力と同じかどうかを確認
                bool userInputAlreadyInConversation = false;

                // 最近の会話を追加（重複チェック付き）
                if (recentConversation != null && recentConversation.Count > 0)
                {
                    // 最後のユーザーメッセージが現在の入力と同じかチェック
                    var lastUserMessage = recentConversation
                        .Where(m => !m.IsFromAI)
                        .OrderByDescending(m => m.Timestamp)
                        .FirstOrDefault();

                    if (lastUserMessage != null && lastUserMessage.Content == userInput)
                    {
                        userInputAlreadyInConversation = true;
                    }

                    // 会話内容の追加（重複を避ける）
                    foreach (var msg in recentConversation)
                    {
                        if (msg.IsFromAI)
                        {
                            // アシスタント名をメッセージから取得、なければ設定から
                            string assistantName = !string.IsNullOrEmpty(msg.AssistantName) ?
                                msg.AssistantName : settings.AiAssistantName;
                            finalPrompt += $"{assistantName}: {msg.Content}\n";
                        }
                        else
                        {
                            finalPrompt += $"ユーザー: {msg.Content}\n";
                        }
                    }
                }

                // 最新のユーザー入力（会話に既に含まれていない場合のみ追加）
                if (!userInputAlreadyInConversation)
                {
                    finalPrompt += $"ユーザー: {userInput}\n";
                }
                finalPrompt += $"{settings.AiAssistantName}: ";

                // APIリクエスト処理
                string response = await ExecuteGeminiRequest(finalPrompt, 0.7, 800);

                // レスポンスが<response>タグを含まない場合は追加
                if (!response.Contains("<response"))
                {
                    response = $"<response emotion=\"normal\">{response}</response>";
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return _resourceService.GetRandomFallbackMessage("connectionErrors");
            }
        }

        // 曜日を日本語に変換するヘルパーメソッド
        private string GetJapaneseDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Sunday: return "日曜日";
                case DayOfWeek.Monday: return "月曜日";
                case DayOfWeek.Tuesday: return "火曜日";
                case DayOfWeek.Wednesday: return "水曜日";
                case DayOfWeek.Thursday: return "木曜日";
                case DayOfWeek.Friday: return "金曜日";
                case DayOfWeek.Saturday: return "土曜日";
                default: return "";
            }
        }

        public async Task<List<string>> GenerateBulletPointSummary(string userName, List<ConversationMessage> conversation)
        {
            try
            {
                // 設定を取得
                var settings = DatabaseService.GetSettings();

                // プロンプト作成
                string finalPrompt = "以下の会話からユーザーが報告した事実や出来事のみを箇条書き形式で抽出してください。\n";
                finalPrompt += "あなたの質問、提案、感想などは含めないでください。\n";
                finalPrompt += "各ポイントは「- 」で始め、1行に1つの事実を簡潔に記述してください。\n";
                finalPrompt += "ユーザーが言及した具体的な行動、経験、感情に焦点を当ててください。\n\n";
                finalPrompt += "会話内容：\n";

                // 会話内容の追加
                foreach (var msg in conversation)
                {
                    if (msg.IsFromAI)
                    {
                        // アシスタント名を使用
                        string assistantName = !string.IsNullOrEmpty(msg.AssistantName) ?
                            msg.AssistantName : settings?.AiAssistantName ?? "AI";
                        finalPrompt += $"{assistantName}: {msg.Content}\n";
                    }
                    else
                    {
                        finalPrompt += $"ユーザー: {msg.Content}\n";
                    }
                }

                // APIリクエスト処理
                string text = await ExecuteGeminiRequest(finalPrompt, 0.3, 500);

                // 箇条書きを抽出
                var bulletPoints = new List<string>();
                var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();
                    // マイナス記号や箇条書き記号で始まる場合はその記号を取り除く
                    if (trimmedLine.StartsWith("・"))
                    {
                        bulletPoints.Add(trimmedLine.Substring(1).Trim());
                    }
                    else if (trimmedLine.StartsWith("- "))
                    {
                        bulletPoints.Add(trimmedLine.Substring(2).Trim());
                    }
                    else if (trimmedLine.StartsWith("-") && !trimmedLine.StartsWith("- "))
                    {
                        bulletPoints.Add(trimmedLine.Substring(1).Trim());
                    }
                    else if (trimmedLine.StartsWith("* "))
                    {
                        bulletPoints.Add(trimmedLine.Substring(2).Trim());
                    }
                    else if (bulletPoints.Count > 0 || char.IsDigit(trimmedLine.FirstOrDefault()))
                    {
                        // 箇条書き以外の形式も取り込む（そのまま）
                        bulletPoints.Add(trimmedLine);
                    }
                }

                return bulletPoints;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return new List<string> { "要約生成に失敗しました。" };
            }
        }

        // 箇条書きから日記本文を生成するメソッド
        public async Task<string> GenerateDiaryFromBulletPoints(string userName, List<string> bulletPoints, DateTime date)
        {
            try
            {
                // プロンプト作成
                string finalPrompt = $"以下の箇条書きメモを元に、{date:yyyy年MM月dd日}の日記を作成してください。\n";
                finalPrompt += "「～です、～ました」のような丁寧な書き方ではなく、「～だ、～した」という普通体で書いてください。\n";
                finalPrompt += "一人称は「私」を使ってください。\n";
                finalPrompt += "段落に分けて読みやすく作成し、時系列順に整理してください。\n\n";
                finalPrompt += "メモ：\n";

                // 箇条書きの追加
                foreach (var point in bulletPoints)
                {
                    finalPrompt += $"- {point}\n";
                }

                // APIリクエスト処理
                return await ExecuteGeminiRequest(finalPrompt, 0.7, 1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return "日記生成に失敗しました。";
            }
        }

        public async Task<string> GetDiarySummary(string userName, List<ConversationMessage> conversation)
        {
            try
            {
                // プロンプト作成
                var promptData = _resourceService.GetPromptSettings();
                if (promptData == null)
                {
                    return "要約生成に失敗しました。";
                }

                var settings = DatabaseService.GetSettings();
                if (settings == null) return "要約生成に失敗しました。";

                // システムプロンプト
                string systemPrompt = promptData.SystemPrompt.Replace("{アシスタント名}", settings.AiAssistantName);

                // 最終プロンプト作成
                string finalPrompt = $"{systemPrompt}\n\n";
                finalPrompt += $"ユーザー名: {userName}\n";
                finalPrompt += $"現在時刻: {DateTime.Now}\n";
                finalPrompt += $"モード: 要約\n\n";
                finalPrompt += "以下の会話から、日記の要約を<summary>タグ内に生成してください。\n\n";

                // 会話内容の追加
                if (conversation != null && conversation.Count > 0)
                {
                    foreach (var msg in conversation)
                    {
                        if (msg.IsFromAI)
                        {
                            finalPrompt += $"AI: {msg.Content}\n";
                        }
                        else
                        {
                            finalPrompt += $"ユーザー: {msg.Content}\n";
                        }
                    }
                }

                // APIリクエスト処理
                string text = await ExecuteGeminiRequest(finalPrompt, 0.3, 500);

                // 要約の抽出
                var summaryMatch = Regex.Match(text, @"<summary>(.*?)<\/summary>", RegexOptions.Singleline);
                if (summaryMatch.Success)
                {
                    return summaryMatch.Groups[1].Value.Trim();
                }

                return text;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Gemini通信エラー: {ex.Message}");
                return "要約生成に失敗しました。";
            }
        }

        public async Task<ConversationSummary> SummarizeConversation(List<ConversationMessage> conversation, DateTime startTime)
        {
            try
            {
                // 設定を取得
                var settings = DatabaseService.GetSettings();

                // 要約用プロンプト作成
                string summaryPrompt = "以下の会話を要約し、主要なトピックを抽出してください。\n" +
                                       "出力形式：\n" +
                                       "<summary>会話の要約文</summary>\n" +
                                       "<topics>トピック1,トピック2,トピック3</topics>\n\n" +
                                       "会話内容：\n";

                // 会話内容をプロンプトに追加
                foreach (var msg in conversation)
                {
                    // 設定が取得できない場合のフォールバック
                    string assistantName = settings?.AiAssistantName ?? "AI";

                    // 各メッセージのアシスタント名を使用
                    string speaker = msg.IsFromAI ?
                        (string.IsNullOrEmpty(msg.AssistantName) ? assistantName : msg.AssistantName) :
                        "ユーザー";

                    summaryPrompt += $"{speaker}: {msg.Content}\n";
                }

                string text = await ExecuteGeminiRequest(summaryPrompt, 0.3, 500);

                // 要約テキストの抽出
                var summaryMatch = Regex.Match(text, @"<summary>(.*?)<\/summary>", RegexOptions.Singleline);
                var topicsMatch = Regex.Match(text, @"<topics>(.*?)<\/topics>", RegexOptions.Singleline);

                string summaryText = summaryMatch.Success ? summaryMatch.Groups[1].Value.Trim() : "要約なし";
                string topicsText = topicsMatch.Success ? topicsMatch.Groups[1].Value.Trim() : "";

                var keyTopics = topicsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                return new ConversationSummary
                {
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    SummaryText = summaryText,
                    KeyTopics = keyTopics,
                    MessageCount = conversation.Count
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"会話要約生成エラー: {ex.Message}");
                return null;
            }
        }

        private void LogGeminiCommunication(string prompt, string response)
        {
            try
            {
                // ログフォルダのパスを作成
                string logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

                // ログフォルダが存在しない場合は作成
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                // 日付フォーマットのファイル名を作成 (yyyyMMdd.txt)
                string logFileName = DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string logFilePath = Path.Combine(logFolderPath, logFileName);

                string logEntry = $"[{DateTime.Now}]\n" +
                                $"====== PROMPT ======\n{prompt}\n\n" +
                                $"====== RESPONSE ======\n{response}\n\n" +
                                $"====================\n\n";

                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ログ記録エラー: {ex.Message}");
            }
        }

        // Ollamaから利用可能なモデル一覧を取得するメソッド
        public async Task<List<string>> GetOllamaModels(string ollamaApiBaseUrl)
        {
            var models = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(ollamaApiBaseUrl))
                {
                    return models;
                }

                // APIベースURLからモデル一覧取得用のURLを構築
                string modelsUrl = ollamaApiBaseUrl.TrimEnd('/');
                // URLを調整（/api/chatではなく/api/tagsを使用）
                if (modelsUrl.EndsWith("/api/chat"))
                {
                    modelsUrl = modelsUrl.Replace("/api/chat", "/api/tags");
                }
                else if (!modelsUrl.EndsWith("/api/tags"))
                {
                    // もしURLの最後が/api/で終わるような場合
                    if (modelsUrl.EndsWith("/api"))
                    {
                        modelsUrl += "/tags";
                    }
                    // 完全に独自のURLの場合
                    else
                    {
                        modelsUrl = Path.Combine(modelsUrl, "api/tags");
                    }
                }

                var client = new RestClient(modelsUrl);
                var request = new RestRequest("", Method.Get);

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    try
                    {
                        var responseData = JsonConvert.DeserializeObject<dynamic>(response.Content);
                        // モデル一覧の取得（JSONの構造に依存）
                        foreach (var model in responseData.models)
                        {
                            string modelName = model.name;
                            models.Add(modelName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ollamaモデル情報のパースエラー: {ex.Message}");
                        // パースに失敗した場合は空のリストを返す
                    }
                }
                else
                {
                    Debug.WriteLine($"Ollamaモデル取得エラー: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ollamaモデル取得中のエラー: {ex.Message}");
            }

            return models;
        }
    }
}