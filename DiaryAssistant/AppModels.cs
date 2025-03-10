using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;

namespace DiaryAssistant.Models
{
    // 通知可能な基底クラス
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // アプリケーション設定
    public class AppSettings : ObservableObject
    {
        [BsonId]
        public int Id { get; set; } = 1;

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private bool _notificationsPaused;
        public bool NotificationsPaused
        {
            get => _notificationsPaused;
            set => SetProperty(ref _notificationsPaused, value);
        }

        private string _aiAssistantName = "AIアシスタント";
        public string AiAssistantName
        {
            get => _aiAssistantName;
            set => SetProperty(ref _aiAssistantName, value);
        }


        private string _selectedAssistantId = "sophia";
        public string SelectedAssistantId
        {
            get => _selectedAssistantId;
            set => SetProperty(ref _selectedAssistantId, value);
        }

        private int _notificationFrequencyMinutes;
        public int NotificationFrequencyMinutes
        {
            get => _notificationFrequencyMinutes;
            set => SetProperty(ref _notificationFrequencyMinutes, value);
        }

        private TimeSpan _notificationStartTime;
        public TimeSpan NotificationStartTime
        {
            get => _notificationStartTime;
            set => SetProperty(ref _notificationStartTime, value);
        }

        private TimeSpan _notificationEndTime;
        public TimeSpan NotificationEndTime
        {
            get => _notificationEndTime;
            set => SetProperty(ref _notificationEndTime, value);
        }

        private bool[] _selectedNotificationHours = new bool[24];
        public bool[] SelectedNotificationHours
        {
            get => _selectedNotificationHours;
            set => SetProperty(ref _selectedNotificationHours, value);
        }

        private bool _getActiveWindowInfo;
        public bool GetActiveWindowInfo
        {
            get => _getActiveWindowInfo;
            set => SetProperty(ref _getActiveWindowInfo, value);
        }

        private bool _runAtStartup;
        public bool RunAtStartup
        {
            get => _runAtStartup;
            set => SetProperty(ref _runAtStartup, value);
        }

        private string _geminiApiKey;
        public string GeminiApiKey
        {
            get => _geminiApiKey;
            set => SetProperty(ref _geminiApiKey, value);
        }

        private string _geminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        public string GeminiApiBaseUrl
        {
            get => _geminiApiBaseUrl;
            set => SetProperty(ref _geminiApiBaseUrl, value);
        }

        private string _databasePath;
        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value);
        }

        private string _fontFamily = "Yu Gothic";
        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        private double _fontSize = 16.0;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }



        private bool _useOllama = false;
        public bool UseOllama
        {
            get => _useOllama;
            set => SetProperty(ref _useOllama, value);
        }

        private string _ollamaApiUrl = "http://localhost:11434/api/chat";
        public string OllamaApiUrl
        {
            get => _ollamaApiUrl;
            set => SetProperty(ref _ollamaApiUrl, value);
        }

        private string _ollamaModelName = "";
        public string OllamaModelName
        {
            get => _ollamaModelName;
            set => SetProperty(ref _ollamaModelName, value);
        }
    }

    // 日記エントリ
    public class DiaryEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public DateTime Date { get; set; }

        public List<ConversationMessage> Conversation { get; set; } = new List<ConversationMessage>();

        public string Summary { get; set; }

        public List<ConversationSummary> ConversationSummaries { get; set; } = new List<ConversationSummary>();

        public List<string> EmotionTags { get; set; } = new List<string>();

        // 最終更新日時
        public DateTime LastModified { get; set; }

        // DiaryEntry クラスに以下のプロパティを追加
        public List<BulletPoint> BulletPoints { get; set; } = new List<BulletPoint>();
        public string GeneratedDiary { get; set; } // 生成された日記本文

        // 箇条書き要約を表すクラスを新規追加
        public class BulletPoint
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsUserEdited { get; set; } = false;
        }
    }

    // 会話要約クラス
    public class ConversationSummary
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string SummaryText { get; set; }
        public List<string> KeyTopics { get; set; } = new List<string>();
        public int MessageCount { get; set; }
    }

    // 会話メッセージ
    public class ConversationMessage
    {
        public bool IsFromAI { get; set; }
        public string Content { get; set; }
        public string Emotion { get; set; }
        public DateTime Timestamp { get; set; }

        public string AssistantId { get; set; }
        public string AssistantName { get; set; }
    }

    // 過去イベント更新情報
    public class EventUpdateInfo
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string TimeExpression { get; set; }
    }

    // プロンプト設定
    public class PromptSettings
    {
        public string SystemPrompt { get; set; }
        public List<string> GreetingPrompts { get; set; }
        public Dictionary<string, string> ContextPrompts { get; set; }
        public List<string> DiaryPrompts { get; set; }
        public List<string> FollowUpPrompts { get; set; }
    }

    // フォールバックメッセージ
    public class FallbackMessages
    {
        public List<string> ConnectionErrors { get; set; }
        public List<string> ApiErrors { get; set; }
        public List<string> GeneralErrors { get; set; }
    }
}