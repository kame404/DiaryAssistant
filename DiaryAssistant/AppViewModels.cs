using DiaryAssistant.Models;
using DiaryAssistant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static DiaryAssistant.Models.DiaryEntry;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace DiaryAssistant.ViewModels
{
    // 設定画面用ビューモデル
    public class SettingsViewModel : ObservableObject
    {
        private AppSettings _settings;

        public string UserName
        {
            get => _settings.UserName;
            set
            {
                if (_settings.UserName != value)
                {
                    _settings.UserName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AiAssistantName
        {
            get => _settings.AiAssistantName;
            set
            {
                if (_settings.AiAssistantName != value)
                {
                    _settings.AiAssistantName = value;
                    OnPropertyChanged();
                }
            }
        }

        // 選択されたアシスタントID
        // AppViewModels.cs内のSettingsViewModelクラスのSelectedAssistantIdプロパティを修正
        public string SelectedAssistantId
        {
            get => _settings.SelectedAssistantId;
            set
            {
                if (_settings.SelectedAssistantId != value)
                {
                    _settings.SelectedAssistantId = value;

                    // アシスタント名も更新
                    if (_availableAssistants != null)
                    {
                        // SelectedAssistantも更新
                        var assistant = _availableAssistants.FirstOrDefault(a => a.Id == value);
                        if (assistant != null)
                        {
                            SelectedAssistant = assistant; // この行を追加
                            AiAssistantName = assistant.Config.Name;
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }

        // 利用可能なアシスタント一覧
        private List<AssistantInfo> _availableAssistants;
        public List<AssistantInfo> AvailableAssistants
        {
            get => _availableAssistants;
            set => SetProperty(ref _availableAssistants, value);
        }

        // 選択されたアシスタント情報
        private AssistantInfo _selectedAssistant;
        public AssistantInfo SelectedAssistant
        {
            get => _selectedAssistant;
            set
            {
                if (SetProperty(ref _selectedAssistant, value) && value != null)
                {
                    SelectedAssistantId = value.Id;
                    AiAssistantName = value.Config.Name;
                }
            }
        }

        public int NotificationFrequencyMinutes
        {
            get => _settings.NotificationFrequencyMinutes;
            set
            {
                if (_settings.NotificationFrequencyMinutes != value)
                {
                    _settings.NotificationFrequencyMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan NotificationStartTime
        {
            get => _settings.NotificationStartTime;
            set
            {
                if (_settings.NotificationStartTime != value)
                {
                    _settings.NotificationStartTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan NotificationEndTime
        {
            get => _settings.NotificationEndTime;
            set
            {
                if (_settings.NotificationEndTime != value)
                {
                    _settings.NotificationEndTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool GetActiveWindowInfo
        {
            get => _settings.GetActiveWindowInfo;
            set
            {
                if (_settings.GetActiveWindowInfo != value)
                {
                    _settings.GetActiveWindowInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RunAtStartup
        {
            get => _settings.RunAtStartup;
            set
            {
                if (_settings.RunAtStartup != value)
                {
                    _settings.RunAtStartup = value;
                    StartupService.SetStartup(value);
                    OnPropertyChanged();
                }
            }
        }

        public string GeminiApiKey
        {
            get => _settings.GeminiApiKey;
            set
            {
                if (_settings.GeminiApiKey != value)
                {
                    _settings.GeminiApiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GeminiApiBaseUrl
        {
            get => _settings.GeminiApiBaseUrl;
            set
            {
                if (_settings.GeminiApiBaseUrl != value)
                {
                    _settings.GeminiApiBaseUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ResetApiUrlCommand { get; }

        public string DatabasePath
        {
            get => _settings.DatabasePath;
            set
            {
                if (_settings.DatabasePath != value)
                {
                    _settings.DatabasePath = value;
                    OnPropertyChanged();
                }
            }
        }

        // フォント設定用のプロパティを追加
        public string FontFamily
        {
            get => _settings.FontFamily;
            set
            {
                if (_settings.FontFamily != value)
                {
                    _settings.FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        public double FontSize
        {
            get => _settings.FontSize;
            set
            {
                if (_settings.FontSize != value)
                {
                    _settings.FontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        // 使用可能なフォントファミリーのリスト
        public ObservableCollection<string> AvailableFonts { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseDatabasePathCommand { get; }

        public SettingsViewModel()
        {
            // 設定の読み込み
            _settings = DatabaseService.GetSettings() ?? new AppSettings();

            // コマンドの初期化
            SaveCommand = new RelayCommand(SaveSettings);
            CancelCommand = new RelayCommand(CancelChanges);
            BrowseDatabasePathCommand = new RelayCommand(BrowseDatabasePath);

            // スタートアップ設定の初期化
            _settings.RunAtStartup = StartupService.IsStartupEnabled();

            ResetApiUrlCommand = new RelayCommand(ResetApiUrl);

            // 利用可能なフォントの取得
            AvailableFonts = new ObservableCollection<string>();
            foreach (var fontFamily in System.Windows.Media.Fonts.SystemFontFamilies)
            {
                AvailableFonts.Add(fontFamily.Source);
            }

            // フォントをソート
            var sortedFonts = AvailableFonts.OrderBy(f => f).ToList();
            AvailableFonts.Clear();
            foreach (var font in sortedFonts)
            {
                AvailableFonts.Add(font);
            }

            // 利用可能なアシスタント一覧を取得
            _availableAssistants = AssistantManager.Instance.GetAllAssistants();

            // 現在選択されているアシスタントを取得
            _selectedAssistant = AssistantManager.Instance.GetCurrentAssistant();
        }

        private void ResetApiUrl()
        {
            GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        }

        public event EventHandler<string> AssistantNameChanged;



        private void SaveSettings()
        {
            // 変更前の名前を保持
            var oldName = DatabaseService.GetSettings()?.AiAssistantName;

            // 設定の保存
            DatabaseService.SaveSettings(_settings);

            // スタートアップの設定
            StartupService.SetStartup(_settings.RunAtStartup);

            // アシスタント名が変更された場合はイベントを発火
            if (oldName != _settings.AiAssistantName)
            {
                AssistantNameChanged?.Invoke(this, _settings.AiAssistantName);
            }

            // 設定ウィンドウを閉じる前に、アプリケーションの再起動を通知するダイアログを表示
            System.Windows.MessageBox.Show(
                "設定を保存しました。アプリケーションを再起動します。",
                "設定保存完了",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            // アプリケーションを再起動
            RestartApplication();
        }

        private void RestartApplication()
        {
            // 現在のプロセスのパスを取得
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // 新しいプロセスを開始
            System.Diagnostics.Process.Start(appPath);

            // 現在のアプリケーションを終了
            System.Windows.Application.Current.Shutdown();
        }

        private void CancelChanges()
        {
            // 変更をキャンセルして設定ウィンドウを閉じる
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        private void BrowseDatabasePath()
        {
            // データベースパスの選択ダイアログを表示
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "データベースファイルの保存先を選択",
                Filter = "LiteDB データベース (*.db)|*.db",
                DefaultExt = ".db",
                FileName = "diary.db",
                OverwritePrompt = false
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                DatabasePath = dialog.FileName;
            }
        }

        // AppViewModels.cs の SettingsViewModel クラスに以下のプロパティを追加

        public bool UseOllama
        {
            get => _settings.UseOllama;
            set
            {
                if (_settings.UseOllama != value)
                {
                    _settings.UseOllama = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OllamaApiUrl
        {
            get => _settings.OllamaApiUrl;
            set
            {
                if (_settings.OllamaApiUrl != value)
                {
                    _settings.OllamaApiUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OllamaModelName
        {
            get => _settings.OllamaModelName;
            set
            {
                if (_settings.OllamaModelName != value)
                {
                    _settings.OllamaModelName = value;
                    OnPropertyChanged();
                }
            }
        }




        public event EventHandler CloseWindow;
    }

    // 日記閲覧画面用ビューモデル
    public class DiaryViewModel : ObservableObject
    {
        private ObservableCollection<DiaryEntry> _diaryEntries;
        public ObservableCollection<DiaryEntry> DiaryEntries
        {
            get => _diaryEntries;
            set => SetProperty(ref _diaryEntries, value);
        }

        private DiaryEntry _selectedDiaryEntry;
        public DiaryEntry SelectedDiaryEntry
        {
            get => _selectedDiaryEntry;
            set
            {
                SetProperty(ref _selectedDiaryEntry, value);
                OnPropertyChanged(nameof(HasSelectedEntry));
                OnPropertyChanged(nameof(ConversationVisible));

                // 選択した日記の要約をSummaryTextにセット
                if (value != null)
                {
                    SummaryText = value.Summary ?? "";
                }
                else
                {
                    SummaryText = "";
                }
            }
        }

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    LoadDiaryEntry(value);
                }
            }
        }

        private bool _conversationVisible;
        public bool ConversationVisible
        {
            get => _conversationVisible;
            set => SetProperty(ref _conversationVisible, value);
        }

        private string _summaryText;
        public string SummaryText
        {
            get => _summaryText;
            set => SetProperty(ref _summaryText, value);
        }

        private ObservableCollection<BulletPoint> _bulletPoints;
        public ObservableCollection<BulletPoint> BulletPoints
        {
            get => _bulletPoints;
            set => SetProperty(ref _bulletPoints, value);
        }

        private string _generatedDiary;
        public string GeneratedDiary
        {
            get => _generatedDiary;
            set => SetProperty(ref _generatedDiary, value);
        }

        private bool _isEditingBulletPoint;
        public bool IsEditingBulletPoint
        {
            get => _isEditingBulletPoint;
            set => SetProperty(ref _isEditingBulletPoint, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private string _fontFamily;
        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        public bool HasSelectedEntry => SelectedDiaryEntry != null;

        public ICommand ExportCommand { get; }
        public ICommand ToggleConversationCommand { get; }
        public ICommand SaveSummaryCommand { get; }
        public ICommand GenerateDiaryCommand { get; }
        public ICommand SaveBulletPointsCommand { get; }
        public ICommand SaveGeneratedDiaryCommand { get; }
        public ICommand AddBulletPointCommand { get; }
        public ICommand RemoveBulletPointCommand { get; }
        public ICommand DeleteConversationMessageCommand { get; }

        public DiaryViewModel()
        {
            // 初期化
            _diaryEntries = new ObservableCollection<DiaryEntry>();
            _bulletPoints = new ObservableCollection<BulletPoint>();
            _selectedDate = DateTime.Today;
            _summaryText = "";
            _generatedDiary = "";

            // コマンドの初期化
            ExportCommand = new RelayCommand(ExportDiary);
            ToggleConversationCommand = new RelayCommand(ToggleConversation);
            SaveSummaryCommand = new RelayCommand(SaveSummary, () => HasSelectedEntry);
            GenerateDiaryCommand = new RelayCommand(GenerateDiary, () => HasSelectedEntry && BulletPoints.Count > 0);
            SaveBulletPointsCommand = new RelayCommand(SaveBulletPoints, () => HasSelectedEntry);
            SaveGeneratedDiaryCommand = new RelayCommand(SaveGeneratedDiary, () => HasSelectedEntry);
            AddBulletPointCommand = new RelayCommand(AddBulletPoint, () => HasSelectedEntry);
            RemoveBulletPointCommand = new RelayCommand<BulletPoint>(RemoveBulletPoint);
            DeleteConversationMessageCommand = new RelayCommand<ConversationMessage>(DeleteConversationMessage);

            // フォント設定を読み込み
            var settings = DatabaseService.GetSettings();
            if (settings != null)
            {
                FontFamily = settings.FontFamily;
            }
            else
            {
                FontFamily = "Yu Gothic"; // デフォルト値
            }

            // データの読み込み
            LoadAllDiaryEntries();
            LoadDiaryEntry(_selectedDate);
        }

        private void DeleteConversationMessage(ConversationMessage message)
        {
            if (message == null || SelectedDiaryEntry == null) return;

            // 確認ダイアログを表示
            var result = System.Windows.MessageBox.Show(
                "この会話を削除してもよろしいですか？この操作は元に戻せません。",
                "会話削除の確認",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // 会話を削除
                SelectedDiaryEntry.Conversation.Remove(message);

                // データベースに保存
                DatabaseService.SaveDiaryEntry(SelectedDiaryEntry);

                // UI更新のために一時的に選択を解除して再選択
                var tempEntry = SelectedDiaryEntry;
                SelectedDiaryEntry = null;
                SelectedDiaryEntry = tempEntry;

                // 会話表示が有効な場合は、表示状態を維持
                if (ConversationVisible)
                {
                    OnPropertyChanged(nameof(ConversationVisible));
                }
            }
        }


        private void LoadAllDiaryEntries()
        {
            var entries = DatabaseService.GetAllDiaryEntries();
            DiaryEntries = new ObservableCollection<DiaryEntry>(entries);

            // UI更新を確実に行うために通知を発火
            OnPropertyChanged(nameof(DiaryEntries));
        }

        private void LoadDiaryEntry(DateTime date)
        {
            // 選択した日付の日記エントリを取得
            SelectedDiaryEntry = DatabaseService.GetDiaryEntry(date);

            // 日記エントリが存在しない場合、新しいエントリを作成する（追加）
            if (SelectedDiaryEntry == null)
            {
                SelectedDiaryEntry = new DiaryEntry
                {
                    Date = date,
                    Conversation = new List<ConversationMessage>(),
                    BulletPoints = new List<BulletPoint>(),
                    ConversationSummaries = new List<ConversationSummary>()
                };
            }
            ConversationVisible = false;

            // 箇条書きの読み込み
            BulletPoints.Clear();
            if (SelectedDiaryEntry != null)
            {
                // 念のためnullチェック
                if (SelectedDiaryEntry.BulletPoints == null)
                {
                    SelectedDiaryEntry.BulletPoints = new List<BulletPoint>();
                }

                foreach (var point in SelectedDiaryEntry.BulletPoints)
                {
                    BulletPoints.Add(point);
                }

                // 生成された日記の読み込み
                GeneratedDiary = SelectedDiaryEntry.GeneratedDiary ?? "";
            }
            else
            {
                GeneratedDiary = "";
            }
        }


        private void ExportDiary()
        {
            // エクスポートダイアログを表示
            var exportDialog = new Window
            {
                Title = "日記のエクスポート",
                Width = 450,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };

            // メインコンテナ
            var mainContainer = new Grid();
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // スクロール可能な内容部分
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 5, 0)  // 右側にパディングを追加してスクロールバーのスペースを確保
            };
            Grid.SetRow(scrollViewer, 0);

            var contentGrid = new Grid { Margin = new Thickness(25) };
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // ヘッダー
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 期間選択
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 出力オプション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 形式選択

            // ヘッダー
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            var titleLabel = new TextBlock
            {
                Text = "日記のエクスポート",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(23, 23, 23))
            };

            var subtitleLabel = new TextBlock
            {
                Text = "エクスポートする期間と出力内容を選択してください",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap
            };

            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(subtitleLabel);
            Grid.SetRow(headerPanel, 0);
            contentGrid.Children.Add(headerPanel);

            // 期間選択セクション
            var dateSection = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var dateSectionContent = new StackPanel();

            var dateSectionTitle = new TextBlock
            {
                Text = "期間",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };

            // 日付選択パネル
            var dateSelectionPanel = new Grid();
            dateSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dateSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dateSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var startDatePicker = new DatePicker
            {
                Width = 130,
                SelectedDate = DateTime.Today.AddDays(-7),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var dateRangeSeparator = new TextBlock
            {
                Text = "～",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var endDatePicker = new DatePicker
            {
                Width = 130,
                SelectedDate = DateTime.Today
            };

            Grid.SetColumn(startDatePicker, 0);
            Grid.SetColumn(dateRangeSeparator, 1);
            Grid.SetColumn(endDatePicker, 2);

            dateSelectionPanel.Children.Add(startDatePicker);
            dateSelectionPanel.Children.Add(dateRangeSeparator);
            dateSelectionPanel.Children.Add(endDatePicker);

            // プリセットボタンパネル
            var presetPanel = new WrapPanel
            {
                Margin = new Thickness(0, 15, 0, 0)
            };

            // 各プリセットボタンを作成
            var todayButton = new Button
            {
                Content = "今日",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                BorderThickness = new Thickness(0)
            };

            var weekButton = new Button
            {
                Content = "今週",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                BorderThickness = new Thickness(0)
            };

            var monthButton = new Button
            {
                Content = "今月",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                BorderThickness = new Thickness(0)
            };

            var lastMonthButton = new Button
            {
                Content = "先月",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                BorderThickness = new Thickness(0)
            };

            // ボタンイベントハンドラー
            todayButton.Click += (s, e) => {
                startDatePicker.SelectedDate = DateTime.Today;
                endDatePicker.SelectedDate = DateTime.Today;
            };

            weekButton.Click += (s, e) => {
                DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
                DateTime today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - firstDayOfWeek)) % 7;
                startDatePicker.SelectedDate = today.AddDays(-diff);
                endDatePicker.SelectedDate = DateTime.Today;
            };

            monthButton.Click += (s, e) => {
                startDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                endDatePicker.SelectedDate = DateTime.Today;
            };

            lastMonthButton.Click += (s, e) => {
                DateTime firstDayOfLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                startDatePicker.SelectedDate = firstDayOfLastMonth;
                endDatePicker.SelectedDate = firstDayOfLastMonth.AddMonths(1).AddDays(-1);
            };

            presetPanel.Children.Add(todayButton);
            presetPanel.Children.Add(weekButton);
            presetPanel.Children.Add(monthButton);
            presetPanel.Children.Add(lastMonthButton);

            dateSectionContent.Children.Add(dateSectionTitle);
            dateSectionContent.Children.Add(dateSelectionPanel);
            dateSectionContent.Children.Add(presetPanel);
            dateSection.Child = dateSectionContent;

            Grid.SetRow(dateSection, 1);
            contentGrid.Children.Add(dateSection);

            // 出力オプションセクション
            var optionsSection = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var optionsSectionContent = new StackPanel();

            var optionsSectionTitle = new TextBlock
            {
                Text = "出力内容",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };

            var diaryTextCheck = new CheckBox
            {
                Content = "日記本文",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };

            var bulletPointsCheck = new CheckBox
            {
                Content = "箇条書きメモ",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };

            var conversationCheck = new CheckBox
            {
                Content = "会話履歴",
                IsChecked = false,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };

            optionsSectionContent.Children.Add(optionsSectionTitle);
            optionsSectionContent.Children.Add(diaryTextCheck);
            optionsSectionContent.Children.Add(bulletPointsCheck);
            optionsSectionContent.Children.Add(conversationCheck);
            optionsSection.Child = optionsSectionContent;

            Grid.SetRow(optionsSection, 2);
            contentGrid.Children.Add(optionsSection);

            // 形式選択セクション
            var formatSection = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var formatSectionContent = new StackPanel();

            var formatSectionTitle = new TextBlock
            {
                Text = "ファイル形式",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };

            var formatCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            formatCombo.Items.Add("テキスト (.txt)");
            formatCombo.Items.Add("Markdown (.md)");
            formatCombo.Items.Add("CSV (.csv)");
            formatCombo.SelectedIndex = 0;

            formatSectionContent.Children.Add(formatSectionTitle);
            formatSectionContent.Children.Add(formatCombo);
            formatSection.Child = formatSectionContent;

            Grid.SetRow(formatSection, 3);
            contentGrid.Children.Add(formatSection);

            // スクロールビューワーにコンテンツグリッドを設定
            scrollViewer.Content = contentGrid;
            mainContainer.Children.Add(scrollViewer);

            // ボタンパネル (固定位置)
            var buttonBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Padding = new Thickness(20, 15, 20, 15)
            };
            Grid.SetRow(buttonBorder, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "キャンセル",
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
                BorderThickness = new Thickness(0),
                Width = 120
            };

            var exportButton = new Button
            {
                Content = "エクスポート",
                Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                Width = 120
            };

            cancelButton.Click += (s, e) => exportDialog.Close();
            exportButton.Click += (s, e) => {
                // 選択された期間とオプションを取得
                DateTime startDate = startDatePicker.SelectedDate ?? DateTime.Today;
                DateTime endDate = endDatePicker.SelectedDate ?? DateTime.Today;
                bool includeDiary = diaryTextCheck.IsChecked ?? false;
                bool includeBullets = bulletPointsCheck.IsChecked ?? false;
                bool includeConversation = conversationCheck.IsChecked ?? false;
                int formatIndex = formatCombo.SelectedIndex;

                // 実際のエクスポート処理
                PerformExport(startDate, endDate, includeDiary, includeBullets, includeConversation, formatIndex);
                exportDialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(exportButton);
            buttonBorder.Child = buttonPanel;
            mainContainer.Children.Add(buttonBorder);

            exportDialog.Content = mainContainer;
            exportDialog.ShowDialog();
        }

        private void PerformExport(DateTime startDate, DateTime endDate, bool includeDiary, bool includeBullets, bool includeConversation, int formatIndex)
        {
            // 選択された期間の日記エントリを取得
            var entries = DatabaseService.GetDiaryEntriesByDateRange(startDate, endDate);
            if (entries.Count == 0)
            {
                MessageBox.Show("選択された期間に日記データがありません。", "エクスポート", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ファイル保存ダイアログの設定
            string extension = formatIndex == 0 ? "txt" : (formatIndex == 1 ? "md" : "csv");
            string filter = formatIndex == 0 ? "テキストファイル (*.txt)|*.txt" :
                           (formatIndex == 1 ? "Markdown (*.md)|*.md" : "CSV (*.csv)|*.csv");

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "日記をエクスポート",
                Filter = filter,
                DefaultExt = "." + extension,
                FileName = $"日記_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.{extension}"
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                StringBuilder content = new StringBuilder();

                // 選択されたフォーマットに基づいてコンテンツを生成
                if (formatIndex == 0) // テキスト形式
                {
                    foreach (var entry in entries.OrderBy(e => e.Date))
                    {
                        content.AppendLine($"=== {entry.Date:yyyy年MM月dd日} ({GetJapaneseDayOfWeek(entry.Date.DayOfWeek)}) ===");
                        content.AppendLine();

                        if (includeDiary && !string.IsNullOrEmpty(entry.GeneratedDiary))
                        {
                            content.AppendLine("【日記】");
                            content.AppendLine(entry.GeneratedDiary);
                            content.AppendLine();
                        }

                        if (includeBullets && entry.BulletPoints != null && entry.BulletPoints.Count > 0)
                        {
                            content.AppendLine("【メモ】");
                            foreach (var point in entry.BulletPoints)
                            {
                                content.AppendLine($"- {point.Content}");
                            }
                            content.AppendLine();
                        }

                        if (includeConversation && entry.Conversation != null && entry.Conversation.Count > 0)
                        {
                            content.AppendLine("【会話】");
                            foreach (var msg in entry.Conversation)
                            {
                                // メッセージの送信者名を取得
                                string prefix = msg.IsFromAI ? $"{msg.AssistantName}: " : "自分: ";
                                content.AppendLine($"{prefix}{msg.Content}");
                            }
                            content.AppendLine();
                        }

                        content.AppendLine("--------------------");
                        content.AppendLine();
                    }
                }
                else if (formatIndex == 1) // Markdown形式
                {
                    content.AppendLine($"# 日記 ({startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd})");
                    content.AppendLine();

                    foreach (var entry in entries.OrderBy(e => e.Date))
                    {
                        content.AppendLine($"## {entry.Date:yyyy年MM月dd日} ({GetJapaneseDayOfWeek(entry.Date.DayOfWeek)})");
                        content.AppendLine();

                        if (includeDiary && !string.IsNullOrEmpty(entry.GeneratedDiary))
                        {
                            content.AppendLine("### 日記");
                            content.AppendLine(entry.GeneratedDiary);
                            content.AppendLine();
                        }

                        if (includeBullets && entry.BulletPoints != null && entry.BulletPoints.Count > 0)
                        {
                            content.AppendLine("### メモ");
                            foreach (var point in entry.BulletPoints)
                            {
                                content.AppendLine($"* {point.Content}");
                            }
                            content.AppendLine();
                        }

                        if (includeConversation && entry.Conversation != null && entry.Conversation.Count > 0)
                        {
                            content.AppendLine("### 会話");
                            foreach (var msg in entry.Conversation)
                            {
                                string prefix = msg.IsFromAI ? "**AI**: " : "**自分**: ";
                                content.AppendLine($"{prefix}{msg.Content}");
                            }
                            content.AppendLine();
                        }

                        content.AppendLine("---");
                        content.AppendLine();
                    }
                }
                else if (formatIndex == 2) // CSV形式
                {
                    // CSVヘッダー
                    content.AppendLine("日付,曜日,日記,メモ,会話数");

                    foreach (var entry in entries.OrderBy(e => e.Date))
                    {
                        string date = entry.Date.ToString("yyyy/MM/dd");
                        string dayOfWeek = GetJapaneseDayOfWeek(entry.Date.DayOfWeek);
                        string diary = includeDiary ? "\"" + (entry.GeneratedDiary ?? "").Replace("\"", "\"\"") + "\"" : "";

                        string bullets = "";
                        if (includeBullets && entry.BulletPoints != null && entry.BulletPoints.Count > 0)
                        {
                            bullets = "\"" + string.Join(" / ", entry.BulletPoints.Select(p => p.Content.Replace("\"", "\"\""))) + "\"";
                        }

                        int conversationCount = (includeConversation && entry.Conversation != null) ? entry.Conversation.Count : 0;

                        content.AppendLine($"{date},{dayOfWeek},{diary},{bullets},{conversationCount}");
                    }
                }

                // ファイルに保存
                try
                {
                    File.WriteAllText(dialog.FileName, content.ToString());
                    MessageBox.Show("日記のエクスポートが完了しました。", "エクスポート成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"エクスポート中にエラーが発生しました: {ex.Message}", "エクスポートエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 曜日を日本語に変換するヘルパーメソッド
        private string GetJapaneseDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Sunday: return "日";
                case DayOfWeek.Monday: return "月";
                case DayOfWeek.Tuesday: return "火";
                case DayOfWeek.Wednesday: return "水";
                case DayOfWeek.Thursday: return "木";
                case DayOfWeek.Friday: return "金";
                case DayOfWeek.Saturday: return "土";
                default: return "";
            }
        }


        private void ToggleConversation()
        {
            ConversationVisible = !ConversationVisible;
        }

        private void SaveSummary()
        {
            if (SelectedDiaryEntry == null) return;

            // 編集された要約を保存
            SelectedDiaryEntry.Summary = SummaryText;
            DatabaseService.SaveDiaryEntry(SelectedDiaryEntry);

            // UI更新通知
            OnPropertyChanged(nameof(SelectedDiaryEntry));

            MessageBox.Show("要約を保存しました。", "保存完了",
                         MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void GenerateDiary()
        {
            if (SelectedDiaryEntry == null || BulletPoints.Count == 0) return;

            try
            {
                // 処理中表示
                IsProcessing = true;

                // 箇条書きの内容を抽出
                var points = BulletPoints.Select(p => p.Content).ToList();

                // GeminiServiceのインスタンスを作成
                var geminiService = new GeminiService();

                // 日記生成
                var diary = await geminiService.GenerateDiaryFromBulletPoints(
                    DatabaseService.GetSettings()?.UserName ?? "ユーザー",
                    points,
                    SelectedDiaryEntry.Date
                );

                // 結果を表示
                GeneratedDiary = diary;

                // 保存
                SelectedDiaryEntry.GeneratedDiary = diary;
                DatabaseService.SaveDiaryEntry(SelectedDiaryEntry);

                MessageBox.Show("日記を生成しました。", "生成完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"日記生成中にエラーが発生しました: {ex.Message}",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // 箇条書き保存
        private void SaveBulletPoints()
        {
            if (SelectedDiaryEntry == null) return;

            // 箇条書きリストの更新
            SelectedDiaryEntry.BulletPoints = BulletPoints.ToList();

            // 最終更新日時を更新
            SelectedDiaryEntry.LastModified = DateTime.Now;

            // データベースに保存
            DatabaseService.SaveDiaryEntry(SelectedDiaryEntry);

            // 日記一覧を更新（新規作成の場合に表示に反映させるため）
            LoadAllDiaryEntries();

            MessageBox.Show("箇条書きを保存しました。", "保存完了",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 生成日記保存
        private void SaveGeneratedDiary()
        {
            if (SelectedDiaryEntry == null) return;

            // 生成日記の更新
            SelectedDiaryEntry.GeneratedDiary = GeneratedDiary;

            // 最終更新日時を更新
            SelectedDiaryEntry.LastModified = DateTime.Now;

            // データベースに保存
            DatabaseService.SaveDiaryEntry(SelectedDiaryEntry);

            // 日記一覧を更新
            LoadAllDiaryEntries();

            MessageBox.Show("日記を保存しました。", "保存完了",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 箇条書き追加
        private void AddBulletPoint()
        {
            if (SelectedDiaryEntry == null) return;

            var newPoint = new BulletPoint
            {
                Content = "新しいポイント",
                Timestamp = DateTime.Now,
                IsUserEdited = true
            };

            BulletPoints.Add(newPoint);
            IsEditingBulletPoint = true;
        }

        // 箇条書き削除
        private void RemoveBulletPoint(BulletPoint point)
        {
            if (point == null) return;

            BulletPoints.Remove(point);
        }


    }

    // 通知・日記モード用ビューモデル
    public class NotificationViewModel : ObservableObject
    {
        private readonly GeminiService _geminiService;
        private readonly NotificationService _notificationService;
        private DiaryEntry _currentDiaryEntry;
        private const int MAX_CONVERSATION_MESSAGES = 10; // 一度に送信する最大メッセージ数
        private DateTime _sessionStartTime;

        // メッセージ数カウント用
        private int _messagesSinceLastSummary = 0;
        private const int MESSAGES_BEFORE_SUMMARY = 20; // 20メッセージごとに要約を生成

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _emotion;
        public string Emotion
        {
            get => _emotion;
            set => SetProperty(ref _emotion, value);
        }

        private string _userInput;
        public string UserInput
        {
            get => _userInput;
            set
            {
                if (SetProperty(ref _userInput, value))
                {
                    UpdateCommands();
                }
            }
        }

        private bool _isDiaryMode;
        public bool IsDiaryMode
        {
            get => _isDiaryMode;
            set
            {
                if (SetProperty(ref _isDiaryMode, value))
                {
                    OnPropertyChanged(nameof(IsGreetingMode));
                }
            }
        }

        public bool IsGreetingMode => !_isDiaryMode;

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ICommand StartDiaryCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand CloseCommand { get; }

        private string _assistantName;
        public string AssistantName
        {
            get => _assistantName;
            set => SetProperty(ref _assistantName, value);
        }

        private string _processingText = "処理中...";
        public string ProcessingText
        {
            get => _processingText;
            set => SetProperty(ref _processingText, value);
        }

        public NotificationViewModel(string initialMessage, string emotion, NotificationService notificationService)
        {
            _geminiService = new GeminiService();
            _notificationService = notificationService;
            _message = initialMessage;
            _emotion = emotion;
            _isDiaryMode = false;
            _sessionStartTime = DateTime.Now;

            // AIアシスタント名を設定から取得
            var settings = DatabaseService.GetSettings();
            _assistantName = settings?.AiAssistantName ?? "AIアシスタント";

            // コマンド初期化
            StartDiaryCommand = new RelayCommand(StartDiaryMode);
            SendMessageCommand = new RelayCommand(SendMessage, () => CanSendMessage());
            CloseCommand = new RelayCommand(Close);

            // 本日の日記データを取得または作成
            var today = DateTime.Today;
            _currentDiaryEntry = DatabaseService.GetDiaryEntry(today);

            if (_currentDiaryEntry == null)
            {
                _currentDiaryEntry = new DiaryEntry
                {
                    Date = today,
                    Conversation = new System.Collections.Generic.List<ConversationMessage>(),
                    ConversationSummaries = new System.Collections.Generic.List<ConversationSummary>(),
                    BulletPoints = new List<BulletPoint>()
                };
            }
            else if (_currentDiaryEntry.ConversationSummaries == null)
            {
                _currentDiaryEntry.ConversationSummaries = new System.Collections.Generic.List<ConversationSummary>();
            }
            else if (_currentDiaryEntry.BulletPoints == null)
            {
                _currentDiaryEntry.BulletPoints = new List<BulletPoint>();
            }

            // AIの初期メッセージを会話に追加
            AddAiMessageToConversation(initialMessage, emotion);


        }

        private void StartDiaryMode()
        {
            IsDiaryMode = true;

            // 日記モード開始を通知サービスに伝える
            _notificationService.ActivateDiaryMode();

            // 日記モード開始用のプロンプトを準備
            var settings = DatabaseService.GetSettings();
            if (settings == null) return;

            // AIに日記モード開始を伝える（非同期処理）
            IsProcessing = true;
            ProcessingText = "応答中...";

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    // 最新の会話のみを取得（最大10件）
                    var recentConversation = _currentDiaryEntry.Conversation
                    .OrderByDescending(m => m.Timestamp)
                    .Take(MAX_CONVERSATION_MESSAGES)
                    .OrderBy(m => m.Timestamp)
                    .GroupBy(m => m.Content) // 内容でグループ化
                    .Select(g => g.First())  // 各グループの最初の項目のみ取得
                    .ToList();

                    // 会話要約の取得
                    var conversationSummaries = _currentDiaryEntry.ConversationSummaries ??
                        new List<ConversationSummary>();

                    var response = await _geminiService.GetDiaryResponse(
                        settings.UserName,
                        "日記を書きたいです",
                        recentConversation,
                        conversationSummaries
                    );

                    // UI更新（Dispatcherを使用）
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // メッセージの解析と表示
                        string emotion = Utils.AiResponseParser.ExtractEmotion(response);
                        string message = Utils.AiResponseParser.ExtractMessage(response);

                        Emotion = emotion;
                        Message = message;

                        // 会話に追加
                        AddAiMessageToConversation(message, emotion);

                        IsProcessing = false;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"日記モード開始エラー: {ex.Message}");

                    // UI更新（Dispatcherを使用）
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Message = "通信エラーが発生しました。もう一度お試しください。";
                        Emotion = "sad";
                        IsProcessing = false;
                    });
                }
            });
        }

        private bool CanSendMessage()
        {
            return IsDiaryMode && !string.IsNullOrWhiteSpace(UserInput) && !IsProcessing;
        }

        // PropertyChanged イベントを発火して UI 更新を促す
        private void UpdateCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public void SendMessage()
        {
            if (!CanSendMessage()) return;

            // ユーザー入力を処理
            string userMessage = UserInput.Trim();

            // 一時的に保持するが、まだ会話には追加しない
            // 成功した場合のみ追加する

            // 入力欄をクリア
            UserInput = string.Empty;

            // AIレスポンスを取得（非同期処理）
            IsProcessing = true;
            ProcessingText = "応答中...";

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var settings = DatabaseService.GetSettings();
                    if (settings == null) return;

                    // APIキーがない場合は処理を中止し、会話に追加しない
                    if (string.IsNullOrEmpty(settings.GeminiApiKey) && !settings.UseOllama)
                    {
                        // UI更新（Dispatcherを使用）
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Message = "API設定が未完了です。設定画面からAPIキーを設定してください。";
                            Emotion = "sad";
                            IsProcessing = false;
                        });
                        return;
                    }

                    // 最新の会話のみを取得（最大10件）
                    var recentConversation = _currentDiaryEntry.Conversation
                        .OrderByDescending(m => m.Timestamp)
                        .Take(MAX_CONVERSATION_MESSAGES)
                        .OrderBy(m => m.Timestamp)
                        .ToList();

                    // 会話要約の取得
                    var conversationSummaries = _currentDiaryEntry.ConversationSummaries ??
                        new List<ConversationSummary>();

                    // ここでAPIリクエストを実行
                    var response = await _geminiService.GetDiaryResponse(
                        settings.UserName,
                        userMessage,
                        recentConversation,
                        conversationSummaries
                    );

                    // APIリクエストが成功した場合のみ会話を記録
                    // ここでユーザーメッセージを追加
                    AddUserMessageToConversation(userMessage);

                    // UI更新（Dispatcherを使用）
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // メッセージの解析と表示
                        string emotion = Utils.AiResponseParser.ExtractEmotion(response);
                        string message = Utils.AiResponseParser.ExtractMessage(response);

                        Emotion = emotion;
                        Message = message;

                        // 会話に追加
                        AddAiMessageToConversation(message, emotion);

                        // 過去イベントの検出
                        var eventUpdate = Utils.AiResponseParser.ExtractEventUpdate(response);
                        if (eventUpdate != null)
                        {
                            // 過去イベントの確認メッセージ表示
                            ShowPastEventConfirmation(eventUpdate);
                        }

                        // 会話内容をデータベースに保存
                        SaveConversation();

                        IsProcessing = false;

                        // メッセージカウンタを増やす
                        _messagesSinceLastSummary++;

                        // 一定数のメッセージが蓄積されたら要約を生成
                        if (_messagesSinceLastSummary >= MESSAGES_BEFORE_SUMMARY)
                        {
                            GenerateConversationSummary();
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"メッセージ送信エラー: {ex.Message}");

                    // UI更新（Dispatcherを使用）
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Message = "通信エラーが発生しました。もう一度お試しください。";
                        Emotion = "sad";
                        IsProcessing = false;

                        // ここでは会話に追加しない！
                    });
                }
            });
        }

        private async void GenerateConversationSummary()
        {
            try
            {
                // バックグラウンドで会話要約を生成
                var conversationSummary = await _geminiService.SummarizeConversation(
                    _currentDiaryEntry.Conversation,
                    _sessionStartTime
                );
                ProcessingText = "処理中...";

                if (conversationSummary != null)
                {
                    if (_currentDiaryEntry.ConversationSummaries == null)
                    {
                        _currentDiaryEntry.ConversationSummaries = new List<ConversationSummary>();
                    }

                    _currentDiaryEntry.ConversationSummaries.Add(conversationSummary);

                    // 要約を保存
                    DatabaseService.SaveDiaryEntry(_currentDiaryEntry);

                    // カウンタとセッション開始時間をリセット
                    _messagesSinceLastSummary = 0;
                    _sessionStartTime = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine("会話要約が生成されました。");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"会話要約生成エラー: {ex.Message}");
            }
        }

        private void AddUserMessageToConversation(string message)
        {
            // より厳密な重複チェック
            var duplicateMessage = _currentDiaryEntry.Conversation
                .Where(m => !m.IsFromAI && m.Content == message)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();

            if (duplicateMessage != null &&
                (DateTime.Now - duplicateMessage.Timestamp).TotalSeconds < 10)
            {
                // 過去10秒以内に同じ内容のユーザーメッセージがあれば追加しない
                return;
            }

            var userMessage = new ConversationMessage
            {
                IsFromAI = false,
                Content = message,
                Emotion = "",
                Timestamp = DateTime.Now,
                // ユーザーメッセージには関連付けられたアシスタント情報がないが、
                // 一貫性のためにnullではなく空文字を設定
                AssistantId = "",
                AssistantName = ""
            };

            _currentDiaryEntry.Conversation.Add(userMessage);
        }

        private void AddAiMessageToConversation(string message, string emotion)
        {
            // 現在のアシスタント情報を取得
            var currentAssistant = AssistantManager.Instance.GetCurrentAssistant();
            string assistantId = currentAssistant?.Id ?? "unknown";
            string assistantName = currentAssistant?.Config?.Name ?? _assistantName;

            var aiMessage = new ConversationMessage
            {
                IsFromAI = true,
                Content = message,
                Emotion = emotion,
                Timestamp = DateTime.Now,
                AssistantId = assistantId,
                AssistantName = assistantName
            };

            _currentDiaryEntry.Conversation.Add(aiMessage);
        }

        private void SaveConversation()
        {
            // 要約の生成は必要ない（会話終了時に生成）
            DatabaseService.SaveDiaryEntry(_currentDiaryEntry);
        }

        private void ShowPastEventConfirmation(EventUpdateInfo eventUpdate)
        {
            string message = $"{eventUpdate.Date:yyyy年MM月dd日}の日記に\n「{eventUpdate.Description}」\nを追記しますか？";

            var result = System.Windows.MessageBox.Show(
                message,
                "過去のイベント検出",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // 現在のアシスタント情報を取得
                var currentAssistant = AssistantManager.Instance.GetCurrentAssistant();
                string assistantId = currentAssistant?.Id ?? "unknown";
                string assistantName = currentAssistant?.Config?.Name ?? "AIアシスタント";

                // 過去の日記に追記
                var pastAiMessage = new ConversationMessage
                {
                    IsFromAI = true,
                    Content = $"【システム】{eventUpdate.TimeExpression}の出来事「{eventUpdate.Description}」を追記しました。",
                    Emotion = "normal",
                    Timestamp = DateTime.Now,
                    AssistantId = assistantId,
                    AssistantName = assistantName
                };

                DatabaseService.AppendToPastEvent(eventUpdate.Date, pastAiMessage);

                // 現在の会話にも記録を残す
                var currentAiMessage = new ConversationMessage
                {
                    IsFromAI = true,
                    Content = $"{eventUpdate.Date:yyyy年MM月dd日}の日記に「{eventUpdate.Description}」を追記しました。",
                    Emotion = "happy",
                    Timestamp = DateTime.Now,
                    AssistantId = assistantId,
                    AssistantName = assistantName
                };

                _currentDiaryEntry.Conversation.Add(currentAiMessage);
                SaveConversation();

                // 画面に表示
                Message = currentAiMessage.Content;
                Emotion = "happy";
            }
        }

        private async void Close()
        {
            // 日記モードの場合は要約を生成
            if (IsDiaryMode && _currentDiaryEntry.Conversation.Count > 0)
            {
                IsProcessing = true;

                ProcessingText = "会話のポイントを抽出しています...";

                try
                {
                    var settings = DatabaseService.GetSettings();
                    if (settings != null)
                    {
                        // 箇条書き要約の生成
                        var bulletPoints = await _geminiService.GenerateBulletPointSummary(
                            settings.UserName,
                            _currentDiaryEntry.Conversation
                        );

                        if (bulletPoints != null && bulletPoints.Count > 0)
                        {
                            // 現在の日時
                            var now = DateTime.Now;

                            // 手動編集された箇条書きだけを保持
                            var userEditedBulletPoints = _currentDiaryEntry.BulletPoints
                                .Where(bp => bp.IsUserEdited)
                                .ToList();

                            // 既存の箇条書きをクリア
                            _currentDiaryEntry.BulletPoints.Clear();

                            // 手動編集された箇条書きを戻す
                            foreach (var point in userEditedBulletPoints)
                            {
                                _currentDiaryEntry.BulletPoints.Add(point);
                            }

                            // 新しい箇条書きを追加
                            foreach (var point in bulletPoints)
                            {
                                _currentDiaryEntry.BulletPoints.Add(new BulletPoint
                                {
                                    Content = point,
                                    Timestamp = now,
                                    IsUserEdited = false
                                });
                            }

                            // 会話セッションの要約も生成（まだ要約されていない場合）
                            if (_messagesSinceLastSummary > 0)
                            {
                                var conversationSummary = await _geminiService.SummarizeConversation(
                                    _currentDiaryEntry.Conversation,
                                    _sessionStartTime
                                );

                                if (conversationSummary != null)
                                {
                                    if (_currentDiaryEntry.ConversationSummaries == null)
                                    {
                                        _currentDiaryEntry.ConversationSummaries = new List<ConversationSummary>();
                                    }

                                    _currentDiaryEntry.ConversationSummaries.Add(conversationSummary);
                                }
                            }

                            // 既存の日記本文は維持する
                            DatabaseService.SaveDiaryEntry(_currentDiaryEntry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"要約生成エラー: {ex.Message}");
                }

                IsProcessing = false;

                // 日記モード終了を通知サービスに伝える
                _notificationService.DeactivateDiaryMode();
            }

            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CloseWindow;
    }

    // コマンド実装
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    // RelayCommand<T>クラスを追加
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}