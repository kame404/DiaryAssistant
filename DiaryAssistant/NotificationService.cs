using DiaryAssistant.Models;
using DiaryAssistant.Utils;
using DiaryAssistant.Views;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace DiaryAssistant.Services
{
    // 通知サービス
    public class NotificationService : IDisposable
    {
        private static NotificationService _instance;
        private static readonly object _lockObject = new object();

        // シングルトンインスタンスを取得するためのプロパティ
        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new NotificationService();
                        }
                    }
                }
                return _instance;
            }
        }

        private Timer _notificationTimer;
        private readonly Random _random = new Random();
        private DateTime _nextNotificationTime;
        private GeminiService _geminiService;
        private WindowInfoService _windowInfoService;
        private ResourceService _resourceService;

        private bool _isDiaryModeActive = false;

        public event EventHandler<string> NotificationReceived;
        public event EventHandler<string> DiaryModeActivated;

        private bool _isPaused = false;

        public bool IsPaused
        {
            get { return _isPaused; }
        }

        public void TogglePauseNotifications()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                // 一時停止時はタイマーを停止
                _notificationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Debug.WriteLine("通知が一時停止されました");
            }
            else
            {
                // 再開時は次の通知をスケジュールしてタイマーを再開
                ScheduleNextNotification();
                _notificationTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                Debug.WriteLine("通知が再開されました");
            }

            // 設定に保存
            var settings = DatabaseService.GetSettings();
            if (settings != null)
            {
                settings.NotificationsPaused = _isPaused;
                DatabaseService.SaveSettings(settings);
            }

            // 通知状態変更イベントを発火
            NotificationStatusChanged?.Invoke(this, _isPaused);
        }

        // 通知状態変更イベントを追加
        public event EventHandler<bool> NotificationStatusChanged;

        // コンストラクタを private に変更してインスタンス化を制限
        private NotificationService()
        {
            _geminiService = new GeminiService();
            _windowInfoService = new WindowInfoService();
            _resourceService = new ResourceService();
        }

        public void Initialize()
        {
            // 既に初期化済みの場合は何もしない
            if (_notificationTimer != null)
            {
                return;
            }

            // 設定から一時停止状態を読み込む
            var settings = DatabaseService.GetSettings();
            if (settings != null)
            {
                _isPaused = settings.NotificationsPaused;
            }

            // 初期設定
            ScheduleNextNotification();

            // タイマー設定（一時停止中でない場合のみアクティブに）
            if (!_isPaused)
            {
                _notificationTimer = new Timer(NotificationTimerCallback, null,
                                             TimeSpan.FromSeconds(30),
                                             TimeSpan.FromSeconds(30));
            }
            else
            {
                _notificationTimer = new Timer(NotificationTimerCallback, null,
                                             Timeout.Infinite,
                                             Timeout.Infinite);
            }
        }

        private void NotificationTimerCallback(object state)
        {
            try
            {
                var now = DateTime.Now;

                // 一時停止中または日記モード中は通知をスキップ
                if (_isPaused || _isDiaryModeActive)
                {
                    return;
                }

                // 現在の時刻が通知時刻に達したかチェック
                if (now >= _nextNotificationTime)
                {
                    // 通知時間帯内かチェック
                    var settings = DatabaseService.GetSettings();
                    if (settings != null)
                    {
                        TimeSpan currentTime = now.TimeOfDay;
                        bool isWithinTimeRange = IsWithinNotificationTimeRange(currentTime, settings.NotificationStartTime, settings.NotificationEndTime);

                        if (isWithinTimeRange)
                        {
                            ShowNotification();
                        }
                        else
                        {
                            // 時間帯外でも通知はスキップされたとしてログに記録
                            Debug.WriteLine($"通知時刻に達しましたが、通知時間帯外のためスキップしました: {now}");
                        }

                        // 通知表示の有無にかかわらず次回の通知時刻を更新
                        ScheduleNextNotification();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"通知タイマーエラー: {ex.Message}");

                // エラー発生時も次回の通知時刻を再設定
                try
                {
                    ScheduleNextNotification();
                }
                catch
                {
                    // 二重エラー防止
                }
            }
        }

        // 日またぎの時間設定に対応する時間帯チェックメソッド
        private bool IsWithinNotificationTimeRange(TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
        {
            // 開始時間が終了時間より後の場合（例：21:00-03:00）は日をまたぐ設定
            if (startTime > endTime)
            {
                // 開始時間から翌日の終了時間までが対象（例：21:00-23:59、00:00-03:00）
                return currentTime >= startTime || currentTime <= endTime;
            }
            else
            {
                // 通常の時間帯（例：09:00-18:00）
                return currentTime >= startTime && currentTime <= endTime;
            }
        }

        private void ScheduleNextNotification()
        {
            var settings = DatabaseService.GetSettings();
            if (settings == null)
            {
                _nextNotificationTime = DateTime.Now.AddMinutes(30);
                Debug.WriteLine($"設定が見つからないため、デフォルト時間で設定しました: {_nextNotificationTime}");
                return;
            }

            // 前回の通知時刻を記録（デバッグ用）
            var previousNotificationTime = _nextNotificationTime;

            // 現在時刻
            var now = DateTime.Now;

            // 通知時間帯内かチェック
            TimeSpan currentTime = now.TimeOfDay;
            bool isWithinTimeRange = IsWithinNotificationTimeRange(currentTime, settings.NotificationStartTime, settings.NotificationEndTime);

            if (isWithinTimeRange)
            {
                // ゆらぎなし - 設定された頻度で正確に次の通知時間を設定
                _nextNotificationTime = now.AddMinutes(settings.NotificationFrequencyMinutes);

                // 通知時間帯を超える場合のチェック
                TimeSpan nextTime = _nextNotificationTime.TimeOfDay;
                bool nextIsInRange = IsWithinNotificationTimeRange(nextTime, settings.NotificationStartTime, settings.NotificationEndTime);

                if (!nextIsInRange)
                {
                    // 次の通知時間が範囲外になる場合の処理
                    if (settings.NotificationStartTime > settings.NotificationEndTime)
                    {
                        // 日をまたぐ設定の場合
                        if (currentTime > settings.NotificationEndTime && currentTime < settings.NotificationStartTime)
                        {
                            // 現在が範囲外で次も範囲外なら、当日の開始時間に設定
                            _nextNotificationTime = now.Date.Add(settings.NotificationStartTime);
                            Debug.WriteLine($"次回の通知が時間帯外になるため、本日の開始時間に設定: {_nextNotificationTime}");
                        }
                        else
                        {
                            // 現在が範囲内で次が範囲外なら、翌日の時間帯開始まで待機
                            _nextNotificationTime = now.Date.AddDays(1).Add(settings.NotificationStartTime);
                            Debug.WriteLine($"次回の通知が時間帯外になるため、翌日の開始時間に設定: {_nextNotificationTime}");
                        }
                    }
                    else
                    {
                        // 通常の設定の場合
                        if (nextTime > settings.NotificationEndTime)
                        {
                            // 終了時間を過ぎる場合は翌日の開始時間に設定
                            _nextNotificationTime = now.Date.AddDays(1).Add(settings.NotificationStartTime);
                            Debug.WriteLine($"次回の通知が時間帯外になるため、翌日の開始時間に設定: {_nextNotificationTime}");
                        }
                    }
                }
            }
            else
            {
                // 通知時間帯外の場合、次の開始時間に設定
                if (settings.NotificationStartTime > settings.NotificationEndTime)
                {
                    // 日をまたぐ設定の場合
                    if (currentTime > settings.NotificationEndTime && currentTime < settings.NotificationStartTime)
                    {
                        // 現在の時刻が通知範囲外の場合（例：3:00-21:00の間）は、当日の開始時間に設定
                        _nextNotificationTime = now.Date.Add(settings.NotificationStartTime);
                        Debug.WriteLine($"現在時刻が時間帯外のため、本日の開始時間に設定: {_nextNotificationTime}");
                    }
                    else
                    {
                        // この状態はありえないはず（isWithinTimeRangeがfalseで、かつ上記条件に合致しない）
                        // 念のため翌日の開始時間を設定
                        _nextNotificationTime = now.Date.AddDays(1).Add(settings.NotificationStartTime);
                        Debug.WriteLine($"未定義の状態のため、翌日の開始時間に設定: {_nextNotificationTime}");
                    }
                }
                else
                {
                    // 通常の設定の場合
                    if (currentTime < settings.NotificationStartTime)
                    {
                        // 開始時間前なら当日の開始時間
                        _nextNotificationTime = now.Date.Add(settings.NotificationStartTime);
                        Debug.WriteLine($"現在時刻が開始時間前のため、本日の開始時間に設定: {_nextNotificationTime}");
                    }
                    else
                    {
                        // 終了時間後なら翌日の開始時間
                        _nextNotificationTime = now.Date.AddDays(1).Add(settings.NotificationStartTime);
                        Debug.WriteLine($"現在時刻が終了時間後のため、翌日の開始時間に設定: {_nextNotificationTime}");
                    }
                }
            }

            // 次の通知時間が過去になっていないか確認
            if (_nextNotificationTime < now)
            {
                // 過去の時刻になっていた場合は即時通知の予定を入れる
                _nextNotificationTime = now.AddMinutes(1);
                Debug.WriteLine($"次回通知時刻が過去になっていたため、1分後に再設定: {_nextNotificationTime}");
            }

            // 通知時刻の更新情報をログに出力
            Debug.WriteLine($"通知時刻を更新: {previousNotificationTime} -> {_nextNotificationTime}");

            // ログファイルへの記録を無効化
            /*
            try
            {
                string logMessage = $"[{DateTime.Now}] 次回通知予定時刻: {_nextNotificationTime}, " +
                                   $"設定間隔: {settings.NotificationFrequencyMinutes}分, " +
                                   $"時間帯: {settings.NotificationStartTime} - {settings.NotificationEndTime}, " +
                                   $"日またぎ設定: {(settings.NotificationStartTime > settings.NotificationEndTime ? "はい" : "いいえ")}";

                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DiaryAssistant", "notification_schedule.log");

                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // ログ記録中のエラーは無視
            }
            */
        }

        public async void ShowNotification()
        {
            try
            {
                var settings = DatabaseService.GetSettings();
                if (settings == null || (string.IsNullOrEmpty(settings.GeminiApiKey) && !settings.UseOllama))
                {
                    ShowErrorNotification("API設定が未完了です。設定画面からAPIキーを設定してください。");
                    return;
                }

                // アクティブウィンドウ情報を取得（設定有効時）
                string activeWindowInfo = null;
                if (settings.GetActiveWindowInfo)
                {
                    activeWindowInfo = _windowInfoService.GetActiveWindowTitle();

                    // 自身のアプリウィンドウの場合は別の情報を収集
                    if (string.IsNullOrEmpty(activeWindowInfo))
                    {
                        // 代わりに現在の時間帯に関連した情報を使用
                        var currentHour = DateTime.Now.Hour;
                        if (currentHour >= 5 && currentHour < 12)
                        {
                            activeWindowInfo = "朝の時間帯";
                        }
                        else if (currentHour >= 12 && currentHour < 18)
                        {
                            activeWindowInfo = "昼の時間帯";
                        }
                        else
                        {
                            activeWindowInfo = "夜の時間帯";
                        }
                    }
                }

                // AI通信
                var response = await _geminiService.GetGreetingResponse(
                    settings.UserName,
                    activeWindowInfo
                );

                // 通知表示
                if (!string.IsNullOrEmpty(response))
                {
                    // XML解析
                    var (message, emotion) = ParseAiResponse(response);

                    // 通知イベント発火
                    NotificationReceived?.Invoke(this, response);

                    // トースト通知表示
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var notificationWindow = new NotificationWindow(message, emotion, this);
                        notificationWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"通知表示エラー: {ex.Message}");
                ShowErrorNotification("通知の表示中にエラーが発生しました。");
            }
        }

        private void ShowErrorNotification(string message)
        {
            try
            {
                // フォールバックメッセージの取得
                string fallbackMessage = _resourceService.GetRandomFallbackMessage("generalErrors");
                if (string.IsNullOrEmpty(fallbackMessage))
                {
                    fallbackMessage = "エラーが発生しました。設定を確認してください。";
                }

                // トースト通知表示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var notificationWindow = new NotificationWindow(
                        string.IsNullOrEmpty(message) ? fallbackMessage : message,
                        "sad",
                        this
                    );
                    notificationWindow.Show();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"エラー通知表示エラー: {ex.Message}");
            }
        }

        public void ActivateDiaryMode()
        {
            _isDiaryModeActive = true;

            // 日記モード開始時にタイマーを一時停止する
            _notificationTimer.Change(Timeout.Infinite, Timeout.Infinite);

            DiaryModeActivated?.Invoke(this, null);
        }

        public void DeactivateDiaryMode()
        {
            _isDiaryModeActive = false;

            // 一時停止中でない場合のみ通知をスケジュール
            if (!_isPaused)
            {
                // 日記モード終了時に即座に次の通知をスケジュールする
                ScheduleNextNotification();

                // タイマーを再開する
                _notificationTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }
        }

        private (string message, string emotion) ParseAiResponse(string response)
        {
            try
            {
                // <response>タグの抽出
                var responseMatch = Regex.Match(response, @"<response\s+emotion=""([^""]+)"">(.*?)<\/response>", RegexOptions.Singleline);
                if (responseMatch.Success)
                {
                    string emotion = responseMatch.Groups[1].Value;

                    // タグの前のテキストとタグ内のテキストを結合
                    int tagStart = response.IndexOf("<response");
                    string textBeforeTag = tagStart > 0 ? response.Substring(0, tagStart).Trim() : "";
                    string tagContent = responseMatch.Groups[2].Value.Trim();

                    // 両方のテキストを結合（間に空白を入れて）
                    string fullMessage = string.IsNullOrEmpty(textBeforeTag)
                        ? tagContent
                        : textBeforeTag + " " + tagContent;

                    return (fullMessage, emotion);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AI応答解析エラー: {ex.Message}");
            }

            return (response, "normal");
        }

        public void Dispose()
        {
            _notificationTimer?.Dispose();
            _notificationTimer = null;
        }
    }
}