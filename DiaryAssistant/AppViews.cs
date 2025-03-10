using DiaryAssistant.ViewModels;
using DiaryAssistant.Models;
using DiaryAssistant.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static DiaryAssistant.Models.DiaryEntry;
using System.Windows.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Input;

namespace DiaryAssistant.Views
{
    // 設定画面
    public class SettingsWindow : Window
    {
        private SettingsViewModel _viewModel;
        private WindowInfoService _windowInfoService;
        private DispatcherTimer _windowInfoTimer;
        private TextBlock _activeWindowInfoText;

        public SettingsWindow()
        {
            Title = "設定";
            Width = 480;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Style = (Style)FindResource("Windows11WindowStyle");
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            // アシスタントから正しいアイコンを取得
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                var iconUri = new Uri(assistant.NormalIconPath);
                Icon = new BitmapImage(iconUri);
            }
            else
            {
                // デフォルトのアプリケーションアイコン
                try
                {
                    Icon = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
                }
                catch
                {
                    // アイコン設定に失敗しても続行
                }
            }

            // ビューモデルの初期化
            _viewModel = new SettingsViewModel();
            _viewModel.CloseWindow += (s, e) => Close();
            DataContext = _viewModel;

            // ウィンドウのコンテンツ設定
            Content = CreateContent();

            _windowInfoService = new WindowInfoService();

            // ウィンドウが閉じられるときにタイマーを停止
            Closing += SettingsWindow_Closing;
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // タイマーを停止
            if (_windowInfoTimer != null)
            {
                _windowInfoTimer.Stop();
                _windowInfoTimer = null;
            }
        }

        // SettingsWindow クラスの CreateContent メソッドを改善
        private UIElement CreateContent()
        {
            // メインのグリッド - スクロール対象のコンテンツと固定ボタンエリアを分けます
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // スクロール領域
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 固定ボタン領域

            // スクロール可能なコンテンツエリア
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(5, 0, 15, 0),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(scrollViewer, 0);

            var contentGrid = new Grid
            {
                Margin = new Thickness(20)
            };

            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // タイトル
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // AIアシスタント設定セクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // ユーザーセクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 通知セクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // スタートアップセクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // API設定セクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // フォント設定セクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // データベース設定セクション
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // Ollama設定セクション

            // タイトル
            var titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            var titleLabel = new TextBlock
            {
                Text = "設定",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(23, 23, 23))
            };

            var subtitleLabel = new TextBlock
            {
                Text = "アプリケーションの動作をカスタマイズします",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 4, 0, 0)
            };

            titlePanel.Children.Add(titleLabel);
            titlePanel.Children.Add(subtitleLabel);
            Grid.SetRow(titlePanel, 0);
            contentGrid.Children.Add(titlePanel);


            // ユーザー名設定
            var userSectionPanel = CreateSectionPanel("ユーザー情報", "\uE77B");
            Grid.SetRow(userSectionPanel, 1);
            contentGrid.Children.Add(userSectionPanel);

            var userContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // ユーザー名設定のみに簡素化
            var userNamePanel = CreateInputField("ユーザー名", "UserName", "あなたの名前を入力してください");
            userContentPanel.Children.Add(userNamePanel);

            ((StackPanel)userSectionPanel.Child).Children.Add(userContentPanel);

            // AIアシスタント設定セクション
            var assistantSectionPanel = CreateSectionPanel("AIアシスタント設定", "\uE716"); // AIアイコン
            Grid.SetRow(assistantSectionPanel, 2); // 行番号を2に設定
            contentGrid.Children.Add(assistantSectionPanel);

            var assistantContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // コンボボックスの設定 - 最上部に配置
            var comboBoxPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            var comboBoxLabel = new TextBlock
            {
                Text = "アシスタントを選択",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var assistantComboBox = new ComboBox
            {
                Width = 300,
                DisplayMemberPath = "Config.Name",
                SelectedValuePath = "Id",
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            assistantComboBox.SetBinding(ComboBox.ItemsSourceProperty, "AvailableAssistants");
            assistantComboBox.SetBinding(ComboBox.SelectedValueProperty, "SelectedAssistantId");

            comboBoxPanel.Children.Add(comboBoxLabel);
            comboBoxPanel.Children.Add(assistantComboBox);
            assistantContentPanel.Children.Add(comboBoxPanel);

            // アシスタント情報表示パネル
            var assistantPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };

            // アシスタント画像と情報を横に並べるパネル
            var assistantInfoContainer = new Grid();
            assistantInfoContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 画像用
            assistantInfoContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) }); // スペース
            assistantInfoContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 情報用

            // 左側: アシスタントアイコン（少し小さくする）
            var previewIconBorder = new Border
            {
                Width = 64, // 小さめのサイズに変更
                Height = 64, // 小さめのサイズに変更
                CornerRadius = new CornerRadius(12), // 角丸も調整
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0)
            };

            var previewIconImage = new Image
            {
                Width = 64, // 内部の画像サイズも調整
                Height = 64,
                Stretch = Stretch.Uniform
            };

            // 角丸のクリップを適用
            var clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 64, 64), // サイズに合わせて調整
                RadiusX = 12,
                RadiusY = 12
            };
            previewIconImage.Clip = clipGeometry;

            // 選択されたアシスタントのnormal.pngをバインディング
            previewIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("SelectedAssistant")
            {
                Converter = new AssistantNormalIconConverter()
            });

            previewIconBorder.Child = previewIconImage;
            Grid.SetColumn(previewIconBorder, 0);
            assistantInfoContainer.Children.Add(previewIconBorder);

            // 右側: アシスタント情報パネル
            var assistantInfoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 240  // 最大幅を設定して長いテキストが適切に折り返されるようにする
            };

            var assistantNameLabel = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap  // 名前も長い場合に備えて折り返し設定
            };
            assistantNameLabel.SetBinding(TextBlock.TextProperty, "SelectedAssistant.Config.Name");

            var assistantDescLabel = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            assistantDescLabel.SetBinding(TextBlock.TextProperty, "SelectedAssistant.Config.Description");

            var assistantPersonalityLabel = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            assistantPersonalityLabel.SetBinding(TextBlock.TextProperty, "SelectedAssistant.Config.Personality");

            var assistantAuthorLabel = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,  // 作成者情報も長い場合に備えて
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            assistantAuthorLabel.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SelectedAssistant.Config.Author")
            {
                StringFormat = "作成者: {0}"
            });

            assistantInfoPanel.Children.Add(assistantNameLabel);
            assistantInfoPanel.Children.Add(assistantDescLabel);
            assistantInfoPanel.Children.Add(assistantPersonalityLabel);
            assistantInfoPanel.Children.Add(assistantAuthorLabel);
            Grid.SetColumn(assistantInfoPanel, 2);
            assistantInfoContainer.Children.Add(assistantInfoPanel);

            assistantPanel.Children.Add(assistantInfoContainer);
            assistantContentPanel.Children.Add(assistantPanel);

            // SelectionChangedイベント
            assistantComboBox.SelectionChanged += (s, e) => {
                if (assistantComboBox.SelectedItem is AssistantInfo selectedAssistant)
                {
                    // ViewModelのSelectedAssistantを更新
                    _viewModel.SelectedAssistant = selectedAssistant;

                    // UIの更新をトリガー
                    if (previewIconImage != null)
                    {
                        // 画像の再読み込みを強制
                        string normalIconPath = selectedAssistant.NormalIconPath;
                        if (File.Exists(normalIconPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(normalIconPath);
                            bitmap.EndInit();
                            bitmap.Freeze();
                            previewIconImage.Source = bitmap;
                        }
                    }
                }
            };

            ((StackPanel)assistantSectionPanel.Child).Children.Add(assistantContentPanel);




            // 通知セクション
            var notificationSectionPanel = CreateSectionPanel("通知設定", "\uE7E7");
            Grid.SetRow(notificationSectionPanel, 3);
            contentGrid.Children.Add(notificationSectionPanel);

            var notificationContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // 通知頻度
            var frequencyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var frequencyLabel = new TextBlock
            {
                Text = "通知頻度",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var frequencySubLabel = new TextBlock
            {
                Text = "通知を表示する間隔を設定します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var frequencyControlPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var frequencySlider = new Slider
            {
                Minimum = 1,
                Maximum = 120,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Width = 240,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            frequencySlider.SetBinding(Slider.ValueProperty, "NotificationFrequencyMinutes");

            var frequencyValuePanel = new StackPanel { Orientation = Orientation.Horizontal };
            var frequencyValueLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            frequencyValueLabel.SetBinding(TextBlock.TextProperty, "NotificationFrequencyMinutes");
            var minutesLabel = new TextBlock { Text = "分", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };

            frequencyValuePanel.Children.Add(frequencyValueLabel);
            frequencyValuePanel.Children.Add(minutesLabel);

            frequencyControlPanel.Children.Add(frequencySlider);
            frequencyControlPanel.Children.Add(frequencyValuePanel);

            frequencyPanel.Children.Add(frequencyLabel);
            frequencyPanel.Children.Add(frequencySubLabel);
            frequencyPanel.Children.Add(frequencyControlPanel);
            notificationContentPanel.Children.Add(frequencyPanel);

            // 通知時間帯
            var timeRangePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var timeRangeLabel = new TextBlock
            {
                Text = "通知時間帯",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var timeRangeSubLabel = new TextBlock
            {
                Text = "通知を表示する時間帯を設定します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var timeRangeControlPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var startTimeControl = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Width = 80,
                Padding = new Thickness(8),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            startTimeControl.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("NotificationStartTime")
            {
                Converter = new DiaryAssistant.Utils.TimeSpanToStringConverter()
            });

            var timeRangeSeparator = new TextBlock
            {
                Text = "～",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0)
            };

            var endTimeControl = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Width = 80,
                Padding = new Thickness(8),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            endTimeControl.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("NotificationEndTime")
            {
                Converter = new DiaryAssistant.Utils.TimeSpanToStringConverter()
            });

            timeRangeControlPanel.Children.Add(startTimeControl);
            timeRangeControlPanel.Children.Add(timeRangeSeparator);
            timeRangeControlPanel.Children.Add(endTimeControl);

            timeRangePanel.Children.Add(timeRangeLabel);
            timeRangePanel.Children.Add(timeRangeSubLabel);
            timeRangePanel.Children.Add(timeRangeControlPanel);
            notificationContentPanel.Children.Add(timeRangePanel);

            // アクティブウィンドウ情報取得設定
            var getActiveWindowPanel = CreateToggleOption(
                "アクティブウィンドウのタイトルを取得",
                "現在開いているウィンドウのタイトルをアシスタントの応答に利用します",
                "GetActiveWindowInfo");
            notificationContentPanel.Children.Add(getActiveWindowPanel);

            ((StackPanel)notificationSectionPanel.Child).Children.Add(notificationContentPanel);

            // スタートアップセクション
            // var startupSectionPanel = CreateSectionPanel("起動設定", "\uE7E8");
            // Grid.SetRow(startupSectionPanel, 4);
            // contentGrid.Children.Add(startupSectionPanel);
            // 
            // var startupContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };
            // 
            // var startupPanel = CreateToggleOption(
            // "Windowsスタートアップ時に自動起動",
            // "Windows起動時に自動的にアプリを起動します",
            // "RunAtStartup");
            // startupContentPanel.Children.Add(startupPanel);
            // 
            // ((StackPanel)startupSectionPanel.Child).Children.Add(startupContentPanel);

            // API設定セクション
            var apiSectionPanel = CreateSectionPanel("API設定", "\uE774");
            Grid.SetRow(apiSectionPanel, 5);
            contentGrid.Children.Add(apiSectionPanel);

            var apiContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // APIキー設定
            var apiKeyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var apiKeyLabel = new TextBlock
            {
                Text = "Gemini APIキー",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var apiKeySubLabel = new TextBlock
            {
                Text = "Google AI Studioから取得したAPIキーを入力します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            // APIキー用TextBox
            var apiKeyTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4),
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            apiKeyTextBox.SetBinding(TextBox.TextProperty, "GeminiApiKey");

            // セキュリティに関する注意書き
            var apiKeySecurityNote = new TextBlock
            {
                Text = "※ APIキーは絶対に他人と共有しないでください。",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                MaxWidth = 400
            };

            apiKeyPanel.Children.Add(apiKeyLabel);
            apiKeyPanel.Children.Add(apiKeySubLabel);
            apiKeyPanel.Children.Add(apiKeyTextBox);
            apiKeyPanel.Children.Add(apiKeySecurityNote);
            apiContentPanel.Children.Add(apiKeyPanel);

            // API URL設定
            var apiUrlPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var apiUrlLabel = new TextBlock
            {
                Text = "Gemini API ベースURL",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var apiUrlSubLabel = new TextBlock
            {
                Text = "変更する必要がある場合のみ編集してください",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apiUrlPanel2 = new StackPanel { Orientation = Orientation.Horizontal };
            var apiUrlTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 8, 0),
                Width = 300
            };
            apiUrlTextBox.SetBinding(TextBox.TextProperty, "GeminiApiBaseUrl");

            // アイコンボタンに変更
            var resetUrlButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 0),
                Width = 40,
                Height = 40,
                ToolTip = "デフォルト設定に戻す",
                Command = _viewModel.ResetApiUrlCommand
            };

            // アイコンの追加
            var resetIcon = new TextBlock
            {
                Text = "\uE777", // リセットアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            resetUrlButton.Content = resetIcon;

            apiUrlPanel2.Children.Add(apiUrlTextBox);
            apiUrlPanel2.Children.Add(resetUrlButton);

            apiUrlPanel.Children.Add(apiUrlLabel);
            apiUrlPanel.Children.Add(apiUrlSubLabel);
            apiUrlPanel.Children.Add(apiUrlPanel2);
            apiContentPanel.Children.Add(apiUrlPanel);

            ((StackPanel)apiSectionPanel.Child).Children.Add(apiContentPanel);

            // フォント設定セクション
            var fontSectionPanel = CreateSectionPanel("フォント設定", "\uE185");
            Grid.SetRow(fontSectionPanel, 6);
            contentGrid.Children.Add(fontSectionPanel);

            var fontContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // フォントファミリー設定
            var fontFamilyPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var fontFamilyLabel = new TextBlock
            {
                Text = "フォント",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var fontFamilySubLabel = new TextBlock
            {
                Text = "アプリ全体で使用するフォントを選択します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var fontFamilyComboBox = new ComboBox
            {
                Width = 300,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            fontFamilyComboBox.SetBinding(ComboBox.ItemsSourceProperty, "AvailableFonts");
            fontFamilyComboBox.SetBinding(ComboBox.SelectedValueProperty, "FontFamily");

            fontFamilyPanel.Children.Add(fontFamilyLabel);
            fontFamilyPanel.Children.Add(fontFamilySubLabel);
            fontFamilyPanel.Children.Add(fontFamilyComboBox);
            fontContentPanel.Children.Add(fontFamilyPanel);

            // フォントサイズ設定
            var fontSizePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var fontSizeLabel = new TextBlock
            {
                Text = "フォントサイズ",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var fontSizeSubLabel = new TextBlock
            {
                Text = "アプリ全体で使用するフォントサイズを設定します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var fontSizeControlPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var fontSizeSlider = new Slider
            {
                Minimum = 8,
                Maximum = 24,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Width = 240,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            fontSizeSlider.SetBinding(Slider.ValueProperty, "FontSize");

            var fontSizeValuePanel = new StackPanel { Orientation = Orientation.Horizontal };
            var fontSizeValueLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            fontSizeValueLabel.SetBinding(TextBlock.TextProperty, "FontSize");
            var pxLabel = new TextBlock { Text = "px", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };

            fontSizeValuePanel.Children.Add(fontSizeValueLabel);
            fontSizeValuePanel.Children.Add(pxLabel);

            fontSizeControlPanel.Children.Add(fontSizeSlider);
            fontSizeControlPanel.Children.Add(fontSizeValuePanel);

            fontSizePanel.Children.Add(fontSizeLabel);
            fontSizePanel.Children.Add(fontSizeSubLabel);
            fontSizePanel.Children.Add(fontSizeControlPanel);
            fontContentPanel.Children.Add(fontSizePanel);

            // フォントプレビュー
            var previewPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            var previewLabel = new TextBlock
            {
                Text = "プレビュー",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var previewBorder = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 8, 0, 0)
            };

            var previewText = new TextBlock
            {
                Text = "こんにちは、これはフォントプレビューです。The quick brown fox jumps over the lazy dog.",
                TextWrapping = TextWrapping.Wrap
            };
            previewText.SetBinding(TextBlock.FontFamilyProperty, "FontFamily");
            previewText.SetBinding(TextBlock.FontSizeProperty, "FontSize");

            previewBorder.Child = previewText;
            previewPanel.Children.Add(previewLabel);
            previewPanel.Children.Add(previewBorder);
            fontContentPanel.Children.Add(previewPanel);

            ((StackPanel)fontSectionPanel.Child).Children.Add(fontContentPanel);

            // データベース設定セクション
            var dbSectionPanel = CreateSectionPanel("データベース設定", "\uE1A5");
            Grid.SetRow(dbSectionPanel, 7);
            contentGrid.Children.Add(dbSectionPanel);

            var dbContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // データベースパス設定
            var dbPathPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var dbPathLabel = new TextBlock
            {
                Text = "データベースファイルの場所",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var dbPathSubLabel = new TextBlock
            {
                Text = "日記データの保存先を指定します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dbPathPanel2 = new StackPanel { Orientation = Orientation.Horizontal };
            var dbPathTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 8, 0),
                IsReadOnly = true,
                Width = 300
            };
            dbPathTextBox.SetBinding(TextBox.TextProperty, "DatabasePath");

            // アイコンボタンに変更
            var browseButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 0),
                Width = 40,
                Height = 40,
                ToolTip = "参照",
                Command = _viewModel.BrowseDatabasePathCommand
            };

            // アイコンの追加
            var folderIcon = new TextBlock
            {
                Text = "\uE8B7", // フォルダアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            browseButton.Content = folderIcon;

            dbPathPanel2.Children.Add(dbPathTextBox);
            dbPathPanel2.Children.Add(browseButton);

            dbPathPanel.Children.Add(dbPathLabel);
            dbPathPanel.Children.Add(dbPathSubLabel);
            dbPathPanel.Children.Add(dbPathPanel2);
            dbContentPanel.Children.Add(dbPathPanel);

            ((StackPanel)dbSectionPanel.Child).Children.Add(dbContentPanel);

            // Ollama設定セクション
            var ollamaSection = CreateSectionPanel("Ollama設定 (実験的)", "\uE9CE");
            Grid.SetRow(ollamaSection, 8);
            contentGrid.Children.Add(ollamaSection);

            var ollamaContentPanel = new StackPanel { Margin = new Thickness(32, 8, 0, 16) };

            // Ollama使用設定
            var useOllamaPanel = CreateToggleOption(
                "Ollama APIを使用する (実験的機能)",
                "チェックするとGemini APIの代わりにOllamaを使用します",
                "UseOllama");
            ollamaContentPanel.Children.Add(useOllamaPanel);

            // Ollama API URL設定
            var ollamaUrlPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 16) };
            var ollamaUrlLabel = new TextBlock
            {
                Text = "Ollama API URL",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var ollamaUrlSubLabel = new TextBlock
            {
                Text = "OllamaサーバーのURLを入力します (例: http://localhost:11434/api/chat)",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var ollamaUrlTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 4),
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            ollamaUrlTextBox.SetBinding(TextBox.TextProperty, "OllamaApiUrl");

            ollamaUrlPanel.Children.Add(ollamaUrlLabel);
            ollamaUrlPanel.Children.Add(ollamaUrlSubLabel);
            ollamaUrlPanel.Children.Add(ollamaUrlTextBox);
            ollamaContentPanel.Children.Add(ollamaUrlPanel);

            // Ollamaモデル選択
            var ollamaModelPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
            var ollamaModelLabel = new TextBlock
            {
                Text = "Ollamaモデル",
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var ollamaModelSubLabel = new TextBlock
            {
                Text = "使用するOllamaモデルを選択します",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };

            // コンボボックスとテキストボックスを作成
            var ollamaModelComboBox = new ComboBox
            {
                // TextBoxのStyleは使わない
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 8, 0),
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
                IsEditable = true  // 手動入力も可能に
            };
            ollamaModelComboBox.SetBinding(ComboBox.TextProperty, "OllamaModelName");

            // モデル取得ボタンをアイコンボタンに変更
            var refreshModelsButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(8),
                Width = 40,
                Height = 40,
                ToolTip = "モデル一覧を取得",
                VerticalAlignment = VerticalAlignment.Top
            };

            // アイコンの追加
            var refreshIcon = new TextBlock
            {
                Text = "\uE72C", // 更新/リフレッシュアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            refreshModelsButton.Content = refreshIcon;

            var loadingText = new TextBlock
            {
                Text = "モデル取得中...",
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(8, 4, 0, 0),
                Visibility = Visibility.Collapsed
            };

            // コンボボックスとボタンを横に並べるパネル
            var modelSelectionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            modelSelectionPanel.Children.Add(ollamaModelComboBox);
            modelSelectionPanel.Children.Add(refreshModelsButton);

            // モデル取得ボタンのクリックイベント
            refreshModelsButton.Click += async (s, e) => {
                try
                {
                    // URL取得
                    string ollamaUrl = ollamaUrlTextBox.Text;
                    if (string.IsNullOrEmpty(ollamaUrl))
                    {
                        MessageBox.Show("Ollama API URLを入力してください", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ローディング表示
                    refreshModelsButton.IsEnabled = false;
                    loadingText.Visibility = Visibility.Visible;

                    // モデル一覧取得
                    var geminiService = new GeminiService();
                    var models = await geminiService.GetOllamaModels(ollamaUrl);

                    // コンボボックスに表示
                    ollamaModelComboBox.Items.Clear();
                    if (models.Count > 0)
                    {
                        foreach (var model in models)
                        {
                            ollamaModelComboBox.Items.Add(model);
                        }
                        ollamaModelComboBox.SelectedIndex = 0;
                        MessageBox.Show($"{models.Count}個のモデルを取得しました", "取得成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("モデルが見つかりませんでした。URLが正しいか確認してください", "取得エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"モデル取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // 処理完了
                    refreshModelsButton.IsEnabled = true;
                    loadingText.Visibility = Visibility.Collapsed;
                }
            };

            ollamaModelPanel.Children.Add(ollamaModelLabel);
            ollamaModelPanel.Children.Add(ollamaModelSubLabel);
            ollamaModelPanel.Children.Add(modelSelectionPanel);
            ollamaModelPanel.Children.Add(loadingText);
            ollamaContentPanel.Children.Add(ollamaModelPanel);

            // 注意書き
            var ollamaWarningPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            var ollamaWarningTextBlock = new TextBlock
            {
                Text = "※ この機能は実験的です。Ollamaサーバーのセットアップとモデルのダウンロードが必要です。",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            ollamaWarningPanel.Children.Add(ollamaWarningTextBlock);
            ollamaContentPanel.Children.Add(ollamaWarningPanel);

            ((StackPanel)ollamaSection.Child).Children.Add(ollamaContentPanel);

            // スクロールビューアーにコンテンツグリッドを追加
            scrollViewer.Content = contentGrid;
            mainGrid.Children.Add(scrollViewer);

            // 固定ボタンパネル (下部に固定)
            var buttonBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Padding = new Thickness(20, 15, 20, 15),
                Height = 60
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
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 12, 0),
                MinWidth = 100,
                Command = _viewModel.CancelCommand
            };

            var saveButton = new Button
            {
                Content = "保存",
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                MinWidth = 100,
                Command = _viewModel.SaveCommand
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            buttonBorder.Child = buttonPanel;
            mainGrid.Children.Add(buttonBorder);

            return mainGrid;
        }

        // セクションパネルを作成するヘルパーメソッド
        private Border CreateSectionPanel(string title, string iconCode)
        {
            var sectionBorder = new Border
            {
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(0, 0, 0, 8),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235))
            };

            var sectionPanel = new StackPanel();

            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var iconTextBlock = new TextBlock
            {
                Text = iconCode,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235))
            };

            var titleTextBlock = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55))
            };

            titlePanel.Children.Add(iconTextBlock);
            titlePanel.Children.Add(titleTextBlock);

            sectionPanel.Children.Add(titlePanel);
            sectionBorder.Child = sectionPanel;

            return sectionBorder;
        }

        // 入力フィールドを作成するヘルパーメソッド
        private StackPanel CreateInputField(string label, string bindingPath, string placeholder = null)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            var titleLabel = new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };

            var textBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(8),
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            if (placeholder != null)
            {
                textBox.Tag = placeholder;
            }

            textBox.SetBinding(TextBox.TextProperty, bindingPath);

            panel.Children.Add(titleLabel);
            panel.Children.Add(textBox);

            return panel;
        }

        // トグルオプションを作成するヘルパーメソッド
        private StackPanel CreateToggleOption(string title, string description, string bindingPath)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            var togglePanel = new StackPanel { Orientation = Orientation.Horizontal };

            var checkbox = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 12, 0)
            };
            checkbox.SetBinding(CheckBox.IsCheckedProperty, bindingPath);

            var textPanel = new StackPanel();

            var titleLabel = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold
            };

            var descriptionLabel = new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap
            };

            textPanel.Children.Add(titleLabel);
            textPanel.Children.Add(descriptionLabel);

            togglePanel.Children.Add(checkbox);
            togglePanel.Children.Add(textPanel);

            panel.Children.Add(togglePanel);

            // アクティブウィンドウ取得設定の場合、現在のタイトルを表示するテキストブロックを追加
            if (bindingPath == "GetActiveWindowInfo")
            {
                _activeWindowInfoText = new TextBlock
                {
                    Text = "ここに現在アクティブなウィンドウタイトルが表示されます",
                    Margin = new Thickness(0, 8, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                };

                // チェックボックスの状態変化を監視するイベントハンドラを追加
                checkbox.Checked += (s, e) => StartWindowInfoTimer();
                checkbox.Unchecked += (s, e) => StopWindowInfoTimer();

                // 初期状態に基づいてタイマーを開始または停止
                if ((bool)checkbox.IsChecked)
                {
                    StartWindowInfoTimer();
                }

                panel.Children.Add(_activeWindowInfoText);
            }

            return panel;
        }
        // ウィンドウ情報更新タイマーを開始
        private void StartWindowInfoTimer()
        {
            if (_windowInfoTimer == null)
            {
                _windowInfoTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _windowInfoTimer.Tick += WindowInfoTimer_Tick;
            }

            _windowInfoTimer.Start();
            UpdateActiveWindowInfo(); // 即時に一度更新
        }

        // ウィンドウ情報更新タイマーを停止
        private void StopWindowInfoTimer()
        {
            if (_windowInfoTimer != null)
            {
                _windowInfoTimer.Stop();
            }

            // テキストをクリア
            if (_activeWindowInfoText != null)
            {
                _activeWindowInfoText.Text = "アクティブウィンドウタイトルの取得は無効化されています";
            }
        }

        // タイマーのTick毎に実行
        private void WindowInfoTimer_Tick(object sender, EventArgs e)
        {
            UpdateActiveWindowInfo();
        }

        // アクティブウィンドウ情報を更新
        private void UpdateActiveWindowInfo()
        {
            if (_activeWindowInfoText != null)
            {
                string windowTitle = _windowInfoService.GetActiveWindowTitle();

                if (string.IsNullOrEmpty(windowTitle))
                {
                    _activeWindowInfoText.Text = "現在のウィンドウ: (取得できません)";
                }
                else
                {
                    _activeWindowInfoText.Text = $"アシスタントに渡されるテキスト: {windowTitle}";
                }
            }
        }
    }

    // 通知ウィンドウ
    public class NotificationWindow : Window
    {
        private NotificationViewModel _viewModel;
        private ResourceService _resourceService;
        private DispatcherTimer _fadeOutTimer;
        private bool _isClosing = false;

        public NotificationWindow(string message, string emotion, NotificationService notificationService)
        {
            Title = "AIアシスタント";
            // 初期サイズを設定するが、コンテンツに合わせて伸縮可能にする
            Width = 400; // 幅を少し広く
            MinWidth = 380;
            MaxWidth = 600; // 最大幅を増やす
                            // 高さは初期値のみ設定し、最大値を制限
            Height = 250; // 初期高さも大きく
            MinHeight = 200;
            MaxHeight = 600;

            // サイズを自動調整するよう設定
            SizeToContent = SizeToContent.Height;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent; // 透明背景に設定
            WindowStartupLocation = WindowStartupLocation.Manual;
            this.Topmost = true;

            // フォント設定を取得
            var settings = DatabaseService.GetSettings();
            if (settings != null)
            {
                FontFamily = new FontFamily(settings.FontFamily);
                FontSize = settings.FontSize;
            }

            // 初期位置を設定
            PositionWindowAtBottomRight();

            // ウィンドウサイズが変わったら位置を調整
            SizeChanged += (s, e) => PositionWindowAtBottomRight();

            // リソースサービスの初期化
            _resourceService = new ResourceService();

            // ビューモデルの初期化
            _viewModel = new NotificationViewModel(message, emotion, notificationService);
            _viewModel.CloseWindow += (s, e) => Close();
            DataContext = _viewModel;

            // ウィンドウのコンテンツ設定
            Content = CreateRoundedWindowContent();

            // タイマー設定
            _fadeOutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _fadeOutTimer.Tick += (s, e) =>
            {
                if (!_viewModel.IsDiaryMode)
                {
                    BeginClose();
                }
            };
            _fadeOutTimer.Start();

            // 日記モード通知ハンドラー登録
            notificationService.DiaryModeActivated += (s, e) => _viewModel.IsDiaryMode = true;
        }

        // 右下にウィンドウを配置するメソッド
        private void PositionWindowAtBottomRight()
        {
            // 画面の作業領域を取得
            var workArea = SystemParameters.WorkArea;

            // ウィンドウの右下を画面の右下に合わせる（固定マージンを確保）
            Left = workArea.Width - ActualWidth - 40;
            Top = workArea.Height - ActualHeight - 40;
        }

        private UIElement CreateRoundedWindowContent()
        {
            // 角丸のウィンドウを作成するためのボーダー
            var mainBorder = new Border
            {
                CornerRadius = new CornerRadius(15), // 角丸の半径
                Background = new SolidColorBrush(Color.FromArgb(249, 241, 245, 249)),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 200, 200, 200)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 3,
                    Opacity = 0.2,
                    BlurRadius = 10
                }
            };

            // 実際のコンテンツを生成
            mainBorder.Child = CreateContent();

            return mainBorder;
        }

        private UIElement CreateContent()
        {
            var grid = new Grid
            {
                Margin = new Thickness(20) // 全体のマージンを増やす
            };


            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // タイトル部分（サイズ調整）
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15) // マージン増加
            };

            // AIアイコン（サイズ調整）
            var aiIconBorder = new Border
            {
                Width = 128,
                Height = 128,
                // BorderのCornerRadiusは必要に応じて保持
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Margin = new Thickness(0, 0, 12, 0)
            };

            var aiIconImage = new Image
            {
                Width = 128,
                Height = 128,
                Margin = new Thickness(0),
                Stretch = Stretch.UniformToFill
            };

            // 角丸のクリップを適用
            var clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 128, 128),
                RadiusX = 16,  // 角の丸みの半径X
                RadiusY = 16   // 角の丸みの半径Y
            };
            aiIconImage.Clip = clipGeometry;

            aiIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Emotion")
            {
                Converter = new EmotionToIconConverter()
            });

            aiIconBorder.Child = aiIconImage;
            titlePanel.Children.Add(aiIconBorder);

            // タイトルテキスト（フォントサイズ増加）
            var titleText = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                FontSize = FontSize + 4, // フォントサイズ増加
                VerticalAlignment = VerticalAlignment.Center
            };
            // 名前をバインディングで取得
            titleText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("AssistantName"));
            titlePanel.Children.Add(titleText);

            // 閉じるボタン（サイズ調整）
            var closeButton = new Button
            {
                Content = "×",
                Width = 30, // サイズ増加
                Height = 30, // サイズ増加
                FontSize = FontSize + 6, // フォントサイズ増加
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                Command = _viewModel.CloseCommand
            };

            Grid.SetRow(titlePanel, 0);
            Grid.SetRow(closeButton, 0);
            grid.Children.Add(titlePanel);
            grid.Children.Add(closeButton);

            // メッセージ部分（フォントサイズ調整）
            var messageScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 350,  // 最大高さを増加
                Margin = new Thickness(0, 0, 0, 15) // マージン増加
            };

            var messagePanel = new Border
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10), // パディング増加
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                CornerRadius = new CornerRadius(10) // 角丸増加
            };

            var messageTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = FontSize + 2, // フォントサイズ増加
                LineHeight = (FontSize + 2) * 1.5 // 行間調整
            };
            messageTextBlock.SetBinding(TextBlock.TextProperty, "Message");

            messagePanel.Child = messageTextBlock;
            messageScrollViewer.Content = messagePanel;
            Grid.SetRow(messageScrollViewer, 1);
            grid.Children.Add(messageScrollViewer);

            // 入力フィールド（日記モード時のみ表示）
            var inputPanel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 15) // マージン増加
            };
            inputPanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsDiaryMode")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            var inputTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Height = 80, // 高さ増加
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = FontSize + 2, // フォントサイズ増加
                Padding = new Thickness(10) // パディング増加
            };
            inputTextBox.SetBinding(TextBox.TextProperty, "UserInput");

            // キー入力イベントを追加
            inputTextBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter &&
                   (Keyboard.Modifiers == ModifierKeys.Shift ||
                    Keyboard.Modifiers == ModifierKeys.Control ||
                    Keyboard.Modifiers == ModifierKeys.Alt))
                {

                    _viewModel.SendMessage();
                    e.Handled = true;
                }
            };

            var sendButton = new Button
            {
                Content = "送信",
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Margin = new Thickness(0, 10, 0, 0), // マージン増加
                Padding = new Thickness(15), // パディング増加
                FontSize = FontSize + 2, // フォントサイズ増加
                MinWidth = 100, // 最小幅設定
                MinHeight = 36 // 最小高さ設定
            };

            // 直接SendMessageメソッドを呼び出す
            sendButton.Click += (s, e) => {
                sendButton.IsEnabled = false;
                _viewModel.SendMessage();
                // 短い遅延後に再度有効化
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (s2, e2) => {
                    sendButton.IsEnabled = true;
                    timer.Stop();
                };
                timer.Start();
            };

            inputPanel.Children.Add(inputTextBox);
            inputPanel.Children.Add(sendButton);
            Grid.SetRow(inputPanel, 2);
            grid.Children.Add(inputPanel);

            // ボタンパネル（ボタンサイズ調整）
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0) // マージン増加
            };
            buttonPanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsGreetingMode")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            var laterButton = new Button
            {
                Content = "またあとで",
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Width = 120, // 幅増加
                Height = 40, // 高さ増加
                FontSize = FontSize + 2, // フォントサイズ増加
                Margin = new Thickness(0, 0, 15, 0), // マージン増加
                Padding = new Thickness(10), // パディング増加
                Command = _viewModel.CloseCommand
            };

            var diaryButton = new Button
            {
                Content = "日記を書く",
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Width = 120, // 幅増加
                Height = 40, // 高さ増加
                FontSize = FontSize + 2, // フォントサイズ増加
                Padding = new Thickness(10), // パディング増加
                Command = _viewModel.StartDiaryCommand
            };

            buttonPanel.Children.Add(laterButton);
            buttonPanel.Children.Add(diaryButton);
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            // 処理中表示（変更なし）
            var processingPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            processingPanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsProcessing")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            var processingText = new TextBlock
            {
                // バインディングを追加して状態に応じて表示テキストを変更
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontSize = FontSize + 4 // フォントサイズ増加
            };
            processingText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ProcessingText"));

            processingPanel.Child = processingText;
            Grid.SetRowSpan(processingPanel, 4);
            grid.Children.Add(processingPanel);

            return grid;
        }

        private void BeginClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            _fadeOutTimer.Stop();

            // フェードアウトアニメーション
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(2)
            };
            animation.Completed += (s, e) => Close();

            BeginAnimation(System.Windows.UIElement.OpacityProperty, animation);
        }
    }

    public class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "バージョン情報";
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Style = (Style)FindResource("Windows11WindowStyle");
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            // アシスタントから正しいアイコンを取得
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                var iconUri = new Uri(assistant.NormalIconPath);
                Icon = new BitmapImage(iconUri);
            }
            else
            {
                // デフォルトのアプリケーションアイコン
                try
                {
                    Icon = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
                }
                catch
                {
                    // アイコン設定に失敗しても続行
                }
            }

            // ウィンドウのコンテンツ設定
            Content = CreateContent();
        }

        private UIElement CreateContent()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(5, 0, 15, 0)
            };

            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // アプリアイコンとタイトル
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 30)
            };

            var iconBorder = new Border
            {
                Width = 100,
                Height = 100,
                CornerRadius = new CornerRadius(50),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var iconImage = new Image
            {
                Width = 90,
                Height = 90,
                Stretch = Stretch.Uniform
            };

            // 円形のクリップを適用
            var clipGeometry = new RectangleGeometry
            {
                Rect = new Rect(0, 0, 90, 90),
                RadiusX = 45,
                RadiusY = 45
            };
            iconImage.Clip = clipGeometry;

            // アシスタントからアイコンを読み込む
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                iconImage.Source = new BitmapImage(new Uri(assistant.NormalIconPath));
            }
            else
            {
                // デフォルトのアプリアイコン
                iconImage.Source = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
            }
            iconBorder.Child = iconImage;

            // アセンブリ情報から各種情報を取得
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var appName = "日記作成アシスタント";

            // バージョン情報を取得
            Version version = assembly.GetName().Version;
            string versionText = $"Version {version.Major}.{version.Minor}.{version.Build}";

            // 著作権情報を取得
            string copyright = GetAssemblyAttribute<System.Reflection.AssemblyCompanyAttribute>(assembly)?.Company ?? "開発チーム";
            string copyrightText = $"© {DateTime.Now.Year} kame404";

            // タイトルとバージョン
            var titleLabel = new TextBlock
            {
                Text = appName,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var versionLabel = new TextBlock
            {
                Text = versionText,
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            headerPanel.Children.Add(iconBorder);
            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(versionLabel);
            mainPanel.Children.Add(headerPanel);

            // 説明
            var descriptionPanel = CreateSectionPanel("このアプリについて");
            var descriptionText = new TextBlock
            {
                Text = "日記作成アシスタントは、あなたの日々の振り返りをサポートするAIアシスタントアプリです。" +
                       "定期的な通知で日常の小さな出来事を記録し、あなたの思い出を大切に残すお手伝いをします。",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };
            descriptionPanel.Children.Add(descriptionText);
            mainPanel.Children.Add(descriptionPanel);

            // コピーライト
            var copyrightPanel = CreateSectionPanel("コピーライト");
            var copyrightTextBlock = new TextBlock
            {
                Text = $"{copyrightText} All Rights Reserved.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };
            copyrightPanel.Children.Add(copyrightTextBlock);
            mainPanel.Children.Add(copyrightPanel);

            // ライブラリライセンス情報
            var licensesPanel = CreateSectionPanel("使用ライブラリとライセンス");

            // 各ライブラリのライセンス情報
            var libraryInfos = new[]
            {
            new { Name = "LiteDB", Version = "5.0.21", License = "MIT License", Url = "https://github.com/mbdavid/LiteDB" },
            new { Name = "Newtonsoft.Json", Version = "13.0.3", License = "MIT License", Url = "https://github.com/JamesNK/Newtonsoft.Json" },
            new { Name = "RestSharp", Version = "112.1.0", License = "Apache License 2.0", Url = "https://github.com/restsharp/RestSharp" },
            new { Name = "Microsoft.Toolkit.Uwp.Notifications", Version = "7.1.3", License = "MIT License", Url = "https://github.com/CommunityToolkit/WindowsCommunityToolkit" },
            new { Name = "Fody", Version = "6.8.2", License = "MIT License", Url = "https://github.com/Fody/Fody" },
            new { Name = "Costura.Fody", Version = "6.0.0", License = "MIT License", Url = "https://github.com/Fody/Costura" }
        };

            foreach (var library in libraryInfos)
            {
                var libraryPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240))
                };

                var libraryStack = new StackPanel();

                var libraryNameAndVersion = new TextBlock
                {
                    Text = $"{library.Name} ({library.Version})",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var libraryLicense = new TextBlock
                {
                    Text = $"ライセンス: {library.License}",
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var libraryUrlPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                var urlLabel = new TextBlock
                {
                    Text = "URL: ",
                    VerticalAlignment = VerticalAlignment.Center
                };

                var urlLink = new TextBlock
                {
                    Text = library.Url,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    TextDecorations = TextDecorations.Underline,
                    VerticalAlignment = VerticalAlignment.Center
                };

                string url = library.Url;
                urlLink.MouseLeftButtonDown += (s, e) => Process.Start(url);

                libraryUrlPanel.Children.Add(urlLabel);
                libraryUrlPanel.Children.Add(urlLink);

                libraryStack.Children.Add(libraryNameAndVersion);
                libraryStack.Children.Add(libraryLicense);
                libraryStack.Children.Add(libraryUrlPanel);

                libraryPanel.Child = libraryStack;
                licensesPanel.Children.Add(libraryPanel);
            }

            mainPanel.Children.Add(licensesPanel);

            // Gemini API利用情報
            var apiInfoPanel = CreateSectionPanel("Gemini API");
            var apiInfoTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // 通常のテキスト部分
            var normalText = new Run("このアプリケーションはGoogle Gemini APIを利用します。\nGoogle AI Studioで取得したAPIキーが必要です。\n詳細は");
            apiInfoTextBlock.Inlines.Add(normalText);

            // リンク部分
            var linkText = new Hyperlink();
            linkText.Inlines.Add("Google AI Studioのドキュメント");
            linkText.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            linkText.Click += (s, e) => Process.Start("https://ai.google.dev/docs");
            apiInfoTextBlock.Inlines.Add(linkText);

            // 残りのテキスト
            apiInfoTextBlock.Inlines.Add(new Run("をご参照ください。"));

            apiInfoPanel.Children.Add(apiInfoTextBlock);
            mainPanel.Children.Add(apiInfoPanel);

            scrollViewer.Content = mainPanel;
            return scrollViewer;
        }

        private StackPanel CreateSectionPanel(string title)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleLabel = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };

            titleBorder.Child = titleLabel;
            panel.Children.Add(titleBorder);

            return panel;
        }

        // アセンブリ属性を取得するヘルパーメソッド
        private static T GetAssemblyAttribute<T>(System.Reflection.Assembly assembly) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
            if (attributes == null || attributes.Length == 0)
                return null;
            return (T)attributes[0];
        }
    }

    // 日記閲覧画面
    public class DiaryBrowserWindow : Window
    {
        private DiaryViewModel _viewModel;

        public DiaryBrowserWindow()
        {
            Title = "日記閲覧";
            Width = 900;
            Height = 650;
            MinWidth = 700; // 最小幅を設定
            MinHeight = 500; // 最小高さを設定
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Style = (Style)FindResource("Windows11WindowStyle");
            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            // アシスタントから正しいアイコンを取得
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                var iconUri = new Uri(assistant.NormalIconPath);
                Icon = new BitmapImage(iconUri);
            }
            else
            {
                // デフォルトのアプリケーションアイコン
                try
                {
                    Icon = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
                }
                catch
                {
                    // アイコン設定に失敗しても続行
                }
            }

            // ビューモデルの初期化
            _viewModel = new DiaryViewModel();
            DataContext = _viewModel;

            // ウィンドウのコンテンツ設定
            Content = CreateContent();
        }

        private UIElement CreateContent()
        {
            // メインコンテナとして Grid を使用
            var mainGrid = new Grid
            {
                Margin = new Thickness(20)
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // ヘッダー
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // コンテンツ

            // ヘッダー
            var headerPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // タイトルアイコン
            var titleIcon = new TextBlock
            {
                Text = "\uE8F1", // 日記/ノートアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 24,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            // タイトル
            var titleLabel = new TextBlock
            {
                Text = "日記閲覧",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                VerticalAlignment = VerticalAlignment.Center
            };

            titlePanel.Children.Add(titleIcon);
            titlePanel.Children.Add(titleLabel);

            // サブタイトル
            var subtitleLabel = new TextBlock
            {
                Text = "あなたの日常を振り返りましょう",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(34, 0, 0, 0),
                FontSize = 14
            };

            headerPanel.Children.Add(titlePanel);
            headerPanel.Children.Add(subtitleLabel);

            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // コンテンツエリア
            var contentGrid = new Grid();


            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto), MinWidth = 250 });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 400 });

            // 左側パネル（日付選択）
            var leftPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 15, 0),
                Padding = new Thickness(15),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };

            var leftStack = new StackPanel();

            // 年月日表示を上に移動
            var selectedDatePanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var selectedDateStack = new StackPanel();

            var selectedDateIcon = new TextBlock
            {
                Text = "\uE787", // カレンダーアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var selectedDateLabel = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };
            selectedDateLabel.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SelectedDate")
            {
                StringFormat = "{0:yyyy年MM月dd日}"
            });

            var selectedDateWeekday = new TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            selectedDateWeekday.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("SelectedDate")
            {
                StringFormat = "{0:dddd}"
            });

            selectedDateStack.Children.Add(selectedDateIcon);
            selectedDateStack.Children.Add(selectedDateLabel);
            selectedDateStack.Children.Add(selectedDateWeekday);
            selectedDatePanel.Child = selectedDateStack;

            // 見出し
            var calendarHeader = new TextBlock
            {
                Text = "日付を選択",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            // カレンダー
            var calendar = new Calendar
            {
                SelectionMode = CalendarSelectionMode.SingleDate,
                Margin = new Thickness(0, 0, 0, 15),
                BorderThickness = new Thickness(0),
                FontSize = 12
            };
            calendar.SetBinding(Calendar.SelectedDateProperty, "SelectedDate");

            // 日記データのある日付をハイライト表示するスタイル設定
            calendar.CalendarDayButtonStyle = new Style(typeof(CalendarDayButton))
            {
                BasedOn = (Style)calendar.CalendarDayButtonStyle?.BasedOn ?? new Style(typeof(CalendarDayButton)),
                Setters = {
                new Setter(CalendarDayButton.BackgroundProperty, new System.Windows.Data.Binding("Date")
                {
                    Converter = new DiaryAssistant.Utils.CalendarDayToBrushConverter()
                })
            }
            };

            // エクスポートボタンを下段に移動
            var exportButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Padding = new Thickness(12, 8, 12, 8),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var exportStackPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var exportIcon = new TextBlock
            {
                Text = "\uE78C", // エクスポートアイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(0, 0, 6, 0)
            };
            var exportText = new TextBlock { Text = "エクスポート" };
            exportStackPanel.Children.Add(exportIcon);
            exportStackPanel.Children.Add(exportText);
            exportButton.Content = exportStackPanel;
            exportButton.Command = _viewModel.ExportCommand;

            // 左側パネルに各要素を順番に追加
            leftStack.Children.Add(selectedDatePanel);
            leftStack.Children.Add(calendarHeader);
            leftStack.Children.Add(calendar);
            leftStack.Children.Add(exportButton);
            leftPanel.Child = leftStack;

            Grid.SetColumn(leftPanel, 0);
            contentGrid.Children.Add(leftPanel);

            // 右側パネル（日記コンテンツ）
            var rightPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };

            var tabControl = new TabControl
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15)
            };

            // タブコントロールのスタイルを設定
            var tabStyle = new Style(typeof(TabItem));
            tabStyle.Setters.Add(new Setter(TabItem.TemplateProperty, CreateTabControlTemplate()));
            tabStyle.Setters.Add(new Setter(TabItem.BackgroundProperty, Brushes.Transparent));
            tabStyle.Setters.Add(new Setter(TabItem.BorderThicknessProperty, new Thickness(0)));
            tabStyle.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(15, 10, 15, 10)));
            tabStyle.Setters.Add(new Setter(TabItem.MarginProperty, new Thickness(0, 0, 2, 0)));
            tabStyle.Setters.Add(new Setter(TabItem.FontSizeProperty, 14.0));
            tabStyle.Setters.Add(new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            tabControl.Resources.Add(typeof(TabItem), tabStyle);

            // 箇条書きタブ
            var bulletPointsTab = new TabItem
            {
                Header = CreateTabHeader("\uE8FD", "箇条書きメモ　") // リストアイコン
            };
            bulletPointsTab.Content = CreateBulletPointsPanel();

            // 日記タブ
            var diaryTab = new TabItem
            {
                Header = CreateTabHeader("\uED63", "日記　") // ドキュメントアイコン
            };
            diaryTab.Content = CreateDiaryPanel();

            // 会話タブ
            var conversationTab = new TabItem
            {
                Header = CreateTabHeader("\uE8F2", "会話履歴　") // チャットアイコン
            };
            conversationTab.Content = CreateConversationPanel();

            tabControl.Items.Add(bulletPointsTab);
            tabControl.Items.Add(diaryTab);
            tabControl.Items.Add(conversationTab);

            rightPanel.Child = tabControl;
            Grid.SetColumn(rightPanel, 1);
            contentGrid.Children.Add(rightPanel);

            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            return mainGrid;
        }

        private ControlTemplate CreateTabControlTemplate()
        {
            var template = new ControlTemplate(typeof(TabItem));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4, 4, 0, 0));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TabItem.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TabItem.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TabItem.BorderThicknessProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new Thickness(2));

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // トリガーの追加
            var selectedTrigger = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)), "Border"));
            selectedTrigger.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 2), "Border"));
            selectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246)), "Border"));
            selectedTrigger.Setters.Add(new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246))));
            template.Triggers.Add(selectedTrigger);

            return template;
        }

        private UIElement CreateTabHeader(string iconCode, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var icon = new TextBlock
            {
                Text = iconCode,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);

            return stackPanel;
        }

        // 箇条書きパネルの作成
        private UIElement CreateBulletPointsPanel()
        {
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 箇条書きリスト
            var listBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var listScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var bulletPointsList = new ItemsControl();
            bulletPointsList.SetBinding(ItemsControl.ItemsSourceProperty, "BulletPoints");

            bulletPointsList.ItemTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Grid));
            factory.SetValue(Grid.MarginProperty, new Thickness(0, 0, 0, 8));

            var rowDef1 = new FrameworkElementFactory(typeof(RowDefinition));
            rowDef1.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            factory.AppendChild(rowDef1);

            var itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            itemBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            itemBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(10));
            itemBorder.SetValue(Grid.RowProperty, 0);

            // DockPanelを使用してコンテンツをレイアウト
            var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
            dockPanel.SetValue(DockPanel.LastChildFillProperty, true); // 最後の子要素が残りのスペースを埋める

            // 削除ボタンをDockPanel.Rightに配置
            var removeFactory = new FrameworkElementFactory(typeof(Button));
            removeFactory.SetValue(Button.ContentProperty, "\uE74D"); // 削除アイコン
            removeFactory.SetValue(Button.FontFamilyProperty, new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"));
            removeFactory.SetValue(Button.StyleProperty, (Style)FindResource("Windows11ButtonStyle"));
            removeFactory.SetValue(Button.HeightProperty, 32.0);
            removeFactory.SetValue(Button.WidthProperty, 32.0);
            removeFactory.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Top);
            removeFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            removeFactory.SetValue(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139)));
            removeFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            removeFactory.SetValue(Button.MarginProperty, new Thickness(5, -3, -3, 0));
            removeFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);
            removeFactory.SetValue(DockPanel.DockProperty, Dock.Right); // DockPanel.Rightに配置
            removeFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                var button = s as Button;
                var bulletPoint = button.DataContext as DiaryEntry.BulletPoint;
                _viewModel.RemoveBulletPointCommand.Execute(bulletPoint);
            }));

            // テキストボックスがDockPanel内の残りのスペースを占める
            var textFactory = new FrameworkElementFactory(typeof(TextBox));
            textFactory.SetValue(TextBox.StyleProperty, (Style)FindResource("Windows11TextBoxStyle"));
            textFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            textFactory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            textFactory.SetValue(TextBox.AcceptsReturnProperty, true);
            textFactory.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            textFactory.SetValue(TextBox.FontSizeProperty, 14.0);
            textFactory.SetValue(TextBox.MinWidthProperty, 100.0); // 最小幅を設定

            // 親コンテナの幅に基づいて最大幅をバインド
            var maxWidthBinding = new System.Windows.Data.Binding("ActualWidth")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScrollViewer), 1),
                Converter = new MaxWidthConverter() // 後で定義するコンバーター
            };
            textFactory.SetBinding(TextBox.MaxWidthProperty, maxWidthBinding);

            textFactory.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Content"));
            // フォントファミリーを設定から取得
            textFactory.SetBinding(TextBox.FontFamilyProperty,
                new System.Windows.Data.Binding("DataContext.FontFamily")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DiaryBrowserWindow), 1)
                });

            // DockPanelに要素を追加（順序が重要）
            dockPanel.AppendChild(removeFactory); // 最初に追加された要素がDock位置に配置される
            dockPanel.AppendChild(textFactory);   // 最後の子要素が残りのスペースを占める

            itemBorder.AppendChild(dockPanel);
            factory.AppendChild(itemBorder);

            bulletPointsList.ItemTemplate.VisualTree = factory;

            listScrollViewer.Content = bulletPointsList;
            listBorder.Child = listScrollViewer;
            Grid.SetRow(listBorder, 0);
            panel.Children.Add(listBorder);

            // ツールバー - シンプルなデザインに変更
            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 0),
                Height = 30
            };

            // 追加ボタン - シンプルなデザイン
            var addButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Command = _viewModel.AddBulletPointCommand
            };

            var addStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var addIcon = new TextBlock
            {
                Text = "\uE710", // 追加アイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(2, 2, 4, 2)
            };
            var addText = new TextBlock { Text = "追加" };
            addStackPanel.Children.Add(addIcon);
            addStackPanel.Children.Add(addText);
            addButton.Content = addStackPanel;

            // 保存ボタン - シンプルなデザイン
            var saveButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Command = _viewModel.SaveBulletPointsCommand
            };

            var saveStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var saveIcon = new TextBlock
            {
                Text = "\uE74E", // 保存アイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(2, 2, 4, 2)
            };
            var saveText = new TextBlock { Text = "保存" };
            saveStackPanel.Children.Add(saveIcon);
            saveStackPanel.Children.Add(saveText);
            saveButton.Content = saveStackPanel;

            // 日記生成ボタン - シンプルなデザイン
            var generateButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Padding = new Thickness(12, 8, 12, 8),
                Command = _viewModel.GenerateDiaryCommand
            };

            var generateStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var generateIcon = new TextBlock
            {
                Text = "\uE771",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(2, 2, 4, 2)
            };
            var generateText = new TextBlock { Text = "日記生成" };
            generateStackPanel.Children.Add(generateIcon);
            generateStackPanel.Children.Add(generateText);
            generateButton.Content = generateStackPanel;

            toolbarPanel.Children.Add(addButton);
            toolbarPanel.Children.Add(saveButton);
            toolbarPanel.Children.Add(generateButton);
            Grid.SetRow(toolbarPanel, 1);
            panel.Children.Add(toolbarPanel);

            // プロセス表示
            var processingPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 41, 59)),
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };
            processingPanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsProcessing")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            var processingStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var processingIcon = new TextBlock
            {
                Text = "\uE895", // 処理中アイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 24,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var processingText = new TextBlock
            {
                Text = "処理中...",
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            processingStack.Children.Add(processingIcon);
            processingStack.Children.Add(processingText);
            processingPanel.Child = processingStack;
            Grid.SetRowSpan(processingPanel, 2);
            panel.Children.Add(processingPanel);

            return panel;
        }

        // 日記パネルの作成
        private UIElement CreateDiaryPanel()
        {
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 日記テキスト
            var diaryBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var diaryTextBox = new TextBox
            {
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                FontSize = 14.0,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            diaryTextBox.SetBinding(TextBox.TextProperty, "GeneratedDiary");
            diaryTextBox.SetBinding(TextBox.FontFamilyProperty, "FontFamily");

            diaryBorder.Child = diaryTextBox;
            Grid.SetRow(diaryBorder, 0);
            panel.Children.Add(diaryBorder);

            // ツールバー - シンプルなデザイン
            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 0),
                Height = 30
            };

            // 保存ボタン - シンプルなデザイン
            var saveButton = new Button
            {
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Padding = new Thickness(12, 8, 12, 8),
                Command = _viewModel.SaveGeneratedDiaryCommand
            };

            var saveStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var saveIcon = new TextBlock
            {
                Text = "\uE74E", // 保存アイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                Margin = new Thickness(2, 2, 4, 2)
            };
            var saveText = new TextBlock { Text = "保存" };
            saveStackPanel.Children.Add(saveIcon);
            saveStackPanel.Children.Add(saveText);
            saveButton.Content = saveStackPanel;

            toolbarPanel.Children.Add(saveButton);
            Grid.SetRow(toolbarPanel, 1);
            panel.Children.Add(toolbarPanel);

            return panel;
        }

        // 会話履歴パネルの作成
        private UIElement CreateConversationPanel()
        {
            var conversationBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };

            var conversationScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled // 横スクロールを無効化
            };

            var conversationContent = new ItemsControl();
            conversationContent.SetBinding(ItemsControl.ItemsSourceProperty, "SelectedDiaryEntry.Conversation");
            conversationContent.ItemTemplate = new DataTemplate();

            var conversationFactory = new FrameworkElementFactory(typeof(Grid));
            conversationFactory.SetValue(Grid.MarginProperty, new Thickness(0, 0, 0, 15));

            // 行定義を追加（メッセージ部分と時刻表示用）
            var rowDef1 = new FrameworkElementFactory(typeof(RowDefinition));
            rowDef1.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            conversationFactory.AppendChild(rowDef1);

            var rowDef2 = new FrameworkElementFactory(typeof(RowDefinition));
            rowDef2.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            conversationFactory.AppendChild(rowDef2);

            // 列定義 
            var colDef1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto); // アイコン用
            conversationFactory.AppendChild(colDef1);

            var colDef2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star)); // メッセージ用
            conversationFactory.AppendChild(colDef2);

            var colDef3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef3.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto); // 削除ボタン用
            conversationFactory.AppendChild(colDef3);

            // AIアイコン（AIからのメッセージの場合のみ表示）
            var aiIconBorder = new FrameworkElementFactory(typeof(Border));
            aiIconBorder.SetValue(Grid.ColumnProperty, 0);
            aiIconBorder.SetValue(Grid.RowProperty, 0);
            aiIconBorder.SetValue(Border.WidthProperty, 64.0);
            aiIconBorder.SetValue(Border.HeightProperty, 64.0);
            aiIconBorder.SetValue(Border.MarginProperty, new Thickness(0, 0, 10, 0));
            aiIconBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4)); // 角の丸みを減らす
            aiIconBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            aiIconBorder.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Top);
            aiIconBorder.SetValue(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsFromAI")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            var aiIconImage = new FrameworkElementFactory(typeof(Image));
            aiIconImage.SetValue(Image.WidthProperty, 64.0);
            aiIconImage.SetValue(Image.HeightProperty, 64.0);
            aiIconImage.SetValue(Image.StretchProperty, Stretch.Uniform);
            aiIconImage.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            aiIconImage.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Center);

            // 感情とアシスタントIDに基づいたアイコン表示
            var multiBinding = new System.Windows.Data.MultiBinding
            {
                Converter = new AssistantIconConverter()
            };
            multiBinding.Bindings.Add(new System.Windows.Data.Binding("Emotion"));
            multiBinding.Bindings.Add(new System.Windows.Data.Binding("AssistantId"));
            aiIconImage.SetBinding(Image.SourceProperty, multiBinding);

            aiIconBorder.AppendChild(aiIconImage);
            conversationFactory.AppendChild(aiIconBorder);

            // メッセージ表示用のボーダー
            var messageBorderFactory = new FrameworkElementFactory(typeof(Border));
            messageBorderFactory.SetValue(Grid.ColumnProperty, 1);
            messageBorderFactory.SetValue(Grid.RowProperty, 0);
            messageBorderFactory.SetValue(Border.PaddingProperty, new Thickness(12));
            messageBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            // MaxWidthを相対値に変更（親コンテナの80%が目安）
            // 固定値の代わりに、バインディングを使用して親の幅に基づいて調整
            var maxWidthBinding = new System.Windows.Data.MultiBinding();
            maxWidthBinding.Converter = new MessageMaxWidthConverter();
            var parentWidthBinding = new System.Windows.Data.Binding("ActualWidth")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScrollViewer), 1)
            };
            maxWidthBinding.Bindings.Add(parentWidthBinding);
            var isFromAIBinding = new System.Windows.Data.Binding("IsFromAI");
            maxWidthBinding.Bindings.Add(isFromAIBinding);
            messageBorderFactory.SetBinding(FrameworkElement.MaxWidthProperty, maxWidthBinding);

            // 背景色のバインディング
            var backgroundBinding = new System.Windows.Data.Binding("IsFromAI");
            backgroundBinding.Converter = new BooleanToAIBackgroundConverter();
            messageBorderFactory.SetBinding(Border.BackgroundProperty, backgroundBinding);

            // メッセージの水平位置
            var horizontalAlignmentBinding = new System.Windows.Data.Binding("IsFromAI");
            horizontalAlignmentBinding.Converter = new IsFromAIToAlignmentConverter();
            messageBorderFactory.SetBinding(FrameworkElement.HorizontalAlignmentProperty, horizontalAlignmentBinding);

            // メッセージテキスト
            var messageTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            messageTextFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Content"));
            messageTextFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            messageTextFactory.SetValue(TextBlock.FontSizeProperty, 14.0);

            // 設定からフォントファミリーをバインド
            messageTextFactory.SetBinding(TextBlock.FontFamilyProperty,
                new System.Windows.Data.Binding("DataContext.FontFamily")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DiaryBrowserWindow), 1)
                });

            // テキスト色のバインディングを追加
            var foregroundBinding = new System.Windows.Data.Binding("IsFromAI");
            foregroundBinding.Converter = new IsFromAIToForegroundConverter();
            messageTextFactory.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

            messageBorderFactory.AppendChild(messageTextFactory);
            conversationFactory.AppendChild(messageBorderFactory);

            // 時刻表示用テキストブロック
            var timeTextFactory = new FrameworkElementFactory(typeof(StackPanel));
            timeTextFactory.SetValue(Grid.ColumnProperty, 1);
            timeTextFactory.SetValue(Grid.RowProperty, 1);
            timeTextFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            timeTextFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, horizontalAlignmentBinding);
            timeTextFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 2, 8, 0));

            // アシスタント名表示（AIからのメッセージの場合のみ）
            var assistantNameFactory = new FrameworkElementFactory(typeof(TextBlock));
            assistantNameFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            assistantNameFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(107, 114, 128)));
            assistantNameFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 8, 0));
            assistantNameFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("AssistantName"));
            assistantNameFactory.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsFromAI")
            {
                Converter = new DiaryAssistant.Utils.BooleanToVisibilityConverter()
            });

            // 時刻のテキストブロック
            var timeTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            timeTextBlockFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            timeTextBlockFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
            // 時刻のバインディング
            timeTextBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Timestamp")
            {
                StringFormat = "{0:HH:mm}"
            });

            // StackPanelに子要素を追加
            timeTextFactory.AppendChild(assistantNameFactory);
            timeTextFactory.AppendChild(timeTextBlockFactory);

            conversationFactory.AppendChild(timeTextFactory);

            // 削除ボタン
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Grid.ColumnProperty, 2);
            deleteButtonFactory.SetValue(Grid.RowProperty, 0);
            deleteButtonFactory.SetValue(Button.StyleProperty, (Style)FindResource("Windows11ButtonStyle"));
            deleteButtonFactory.SetValue(Button.ContentProperty, "\uE74D"); // 削除アイコン
            deleteButtonFactory.SetValue(Button.FontFamilyProperty, new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"));
            deleteButtonFactory.SetValue(Button.WidthProperty, 32.0);
            deleteButtonFactory.SetValue(Button.HeightProperty, 32.0);
            deleteButtonFactory.SetValue(Button.FontSizeProperty, 14.0);
            deleteButtonFactory.SetValue(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
            deleteButtonFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            deleteButtonFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            deleteButtonFactory.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Top);
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(5, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);

            // 削除コマンドをバインド
            deleteButtonFactory.SetBinding(Button.CommandProperty, new System.Windows.Data.Binding("DataContext.DeleteConversationMessageCommand")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ItemsControl), 1)
            });
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, new System.Windows.Data.Binding());

            conversationFactory.AppendChild(deleteButtonFactory);

            conversationContent.ItemTemplate.VisualTree = conversationFactory;

            conversationScrollViewer.Content = conversationContent;
            conversationBorder.Child = conversationScrollViewer;

            return conversationBorder;
        }
    }

        // メインウィンドウ
        public class MainWindow : Window
    {
        private NotificationService _notificationService;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isExitingFromTray = false; // タスクトレイからの終了フラグ


        private List<string> _emotions = new List<string> { "normal", "happy", "sad", "angry", "surprised", "thinking" };
        private int _clickCount = 0;
        private System.Windows.Controls.Image _aiIconImage;

        public MainWindow()
        {
            Title = "日記作成アシスタント";
            Width = 440;
            Height = 400;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 標準ウィンドウスタイルを使用
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanMinimize;
            Background = Brushes.White;

            // アシスタントから正しいアイコンを取得するように変更
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                var iconUri = new Uri(assistant.NormalIconPath);
                Icon = new BitmapImage(iconUri);
            }
            else
            {
                // デフォルトのアプリケーションアイコン
                try
                {
                    Icon = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
                }
                catch
                {
                    // アイコン設定に失敗しても続行
                }
            }

            // 通知サービスの初期化
            _notificationService = NotificationService.Instance;

            // トレイアイコンの設定
            InitializeNotifyIcon();

            // 初期状態を反映
            UpdateNotificationPauseStatus(_notificationService.IsPaused);

            // ウィンドウの初期コンテンツ設定
            Content = CreateContent();

            // ウィンドウを通常状態で起動
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;

            // ウィンドウイベントハンドラー
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        private System.Windows.Forms.ToolStripMenuItem _pauseNotificationItem;

        private void InitializeNotifyIcon()
        {
            var settings = DatabaseService.GetSettings();
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            string assistantName = assistant?.Config.Name ?? "日記作成アシスタント";

            // アイコンパスを取得
            string iconPath = assistant?.NormalIconIcoPath;
            if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
            {
                // デフォルトのアイコンを使用できない場合はエラーハンドリング
                System.Diagnostics.Debug.WriteLine("通知アイコンが見つかりません。デフォルトアイコンを使用します。");
                // アプリケーションのデフォルトアイコンパスを使用
                iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            }

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconPath),
                Visible = true,
                Text = assistantName // アシスタント名を使用
            };

            // コンテキストメニューの設定
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var showMainWindowItem = new System.Windows.Forms.ToolStripMenuItem("メイン画面を表示");
            showMainWindowItem.Click += (s, e) => ShowMainWindow();

            var showSettingsItem = new System.Windows.Forms.ToolStripMenuItem("設定");
            showSettingsItem.Click += (s, e) => ShowSettingsWindow();

            var showDiaryItem = new System.Windows.Forms.ToolStripMenuItem("日記閲覧");
            showDiaryItem.Click += (s, e) => ShowDiaryBrowserWindow();

            // 通知の一時停止トグル項目を追加
            _pauseNotificationItem = new System.Windows.Forms.ToolStripMenuItem("通知を一時停止");
            _pauseNotificationItem.Click += (s, e) => TogglePauseNotifications();

            var showNotificationItem = new System.Windows.Forms.ToolStripMenuItem("今すぐ通知");
            showNotificationItem.Click += (s, e) => _notificationService.ShowNotification();

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("終了");
            exitItem.Click += (s, e) => {
                _isExitingFromTray = true; // タスクトレイからの終了フラグを設定
                Close();
            };

            // メニュー項目を追加
            contextMenu.Items.Add(showMainWindowItem);
            contextMenu.Items.Add(showSettingsItem);
            contextMenu.Items.Add(showDiaryItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(_pauseNotificationItem);
            contextMenu.Items.Add(showNotificationItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // ダブルクリックイベントをメイン画面表示に変更
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            // 通知サービスの状態変更イベントを購読
            _notificationService.NotificationStatusChanged += NotificationService_StatusChanged;
        }

        // メイン画面を表示するメソッド
        private void ShowMainWindow()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Show();
            Activate(); // ウィンドウをアクティブにする
            Topmost = true; // 一時的に最前面に
            Topmost = false; // すぐに解除
        }

        private UIElement CreateContent()
        {
            var grid = new Grid
            {
                Margin = new Thickness(20)
            };

            // グリッド行定義
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // アイコンとタイトル
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 説明テキスト
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // アイコンボタン
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 説明領域

            // 情報ボタンを右上に追加
            var infoButton = new Button
            {
                Content = "\uE946", // 情報アイコン
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                Width = 32,
                Height = 32,
                Padding = new Thickness(5),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 5, 0)
            };
            infoButton.Click += (s, e) => ShowAboutWindow();

            grid.Children.Add(infoButton);

            // 設定からAIアシスタント名を取得
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            string assistantName = assistant?.Config.Name ?? "日記作成アシスタント";

            // タイトルとアイコンを含むパネル
            var titlePanel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var titleContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // AIアイコンの表示
            var aiIconBorder = new Border
            {
                Width = 100,
                Height = 100,
                CornerRadius = new CornerRadius(50), // 円形に
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 15, 0)
            };

            var aiIconImage = new Image
            {
                Width = 90,
                Height = 90,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
            _aiIconImage = aiIconImage; // フィールド変数に参照を保存

            // 角丸のクリップを適用
            var clipGeometry = new System.Windows.Media.RectangleGeometry
            {
                Rect = new Rect(0, 0, 90, 90),
                RadiusX = 45,
                RadiusY = 45
            };
            aiIconImage.Clip = clipGeometry;

            // normalアイコンを読み込む
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                aiIconImage.Source = new BitmapImage(new Uri(assistant.NormalIconPath));
            }
            else
            {
                // デフォルトの埋め込みリソースを試す
                try
                {
                    aiIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
                }
                catch
                {
                    // リソースのロードに失敗した場合
                    System.Diagnostics.Debug.WriteLine("アイコンをロードできませんでした");
                }
            }

            aiIconBorder.MouseLeftButtonDown += AiIconBorder_MouseLeftButtonDown;

            aiIconBorder.Child = aiIconImage;
            titleContainer.Children.Add(aiIconBorder);

            var titleTextPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleLabel = new TextBlock
            {
                Text = "日記作成アシスタント",
                FontSize = 16,

                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
            };

            var subtitleLabel = new TextBlock
            {
                Text = assistantName,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 5, 0, 0)
            };

            titleTextPanel.Children.Add(titleLabel);
            titleTextPanel.Children.Add(subtitleLabel);

            titleContainer.Children.Add(titleTextPanel);
            titlePanel.Children.Add(titleContainer);

            Grid.SetRow(titlePanel, 0);
            grid.Children.Add(titlePanel);

            // 説明テキスト
            var descriptionText = new TextBlock
            {
                Text = "忙しい日常に、小さな振り返りの時間を作ります",
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 30),
                FontSize = 14
            };

            Grid.SetRow(descriptionText, 1);
            grid.Children.Add(descriptionText);

            // アイコンボタン（横並び）
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // 各ボタンの情報（アイコン、タイトル、説明）
            var buttonInfos = new[]
            {
                new { Icon = "\uE8BD", Title = "今すぐ通知", Description = "アシスタントとの会話を開始します", Action = new Action(() => _notificationService.ShowNotification()) },
                new { Icon = "\uE8F1", Title = "日記閲覧", Description = "記録した日記を閲覧・編集します", Action = new Action(() => ShowDiaryBrowserWindow()) },

                new { Icon = "\uE713", Title = "設定", Description = "アプリケーションの設定を変更します", Action = new Action(() => ShowSettingsWindow()) }
    };

            // 説明表示用のテキストブロック（ヒント領域）を外部変数として定義
            var hintPanel = new Border
            {
                Margin = new Thickness(32, 0, 32, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                MinHeight = 64
            };

            var hintStack = new StackPanel();

            var hintTitle = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5),
                Text = "ヒント"
            };

            // デフォルトのヒントテキスト
            const string defaultHintTitle = "ヒント";
            const string defaultHintText = "アプリはシステムに常駐します\nタスクトレイから右クリックで操作できます";

            var hintText = new TextBlock
            {
                Text = defaultHintText,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
            };

            hintStack.Children.Add(hintTitle);
            hintStack.Children.Add(hintText);
            hintPanel.Child = hintStack;

            // 各ボタンを作成
            foreach (var info in buttonInfos)
            {
                var button = new Button
                {
                    Style = (Style)FindResource("Windows11ButtonStyle"),
                    Width = 80,
                    Height = 80,
                    Margin = new Thickness(10, 0, 10, 0),
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    ToolTip = info.Title
                };

                var icon = new TextBlock
                {
                    Text = info.Icon,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 28,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                button.Content = icon;

                // クリックイベント
                button.Click += (s, e) => info.Action();

                // ホバーイベント - 説明文表示
                button.MouseEnter += (s, e) => {
                    button.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                    hintTitle.Text = info.Title;
                    hintText.Text = info.Description;
                };

                button.MouseLeave += (s, e) => {
                    button.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));

                    // デフォルトのヒントテキストに戻す
                    hintTitle.Text = defaultHintTitle;
                    hintText.Text = defaultHintText;
                };

                buttonPanel.Children.Add(button);
            }

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            // ヒントパネル
            Grid.SetRow(hintPanel, 3);
            grid.Children.Add(hintPanel);

            return grid;
        }

        private void ShowAboutWindow()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void AiIconBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2) // ダブルクリックを検出
            {
                _clickCount++;

                if (_clickCount > 1) // 初回以降はランダムに変更
                {
                    // 現在のアイコンと異なるアイコンをランダムに選択
                    string currentEmotion = "normal";
                    if (_aiIconImage.Source is BitmapImage bitmapImage)
                    {
                        string path = bitmapImage.UriSource.ToString();
                        currentEmotion = System.IO.Path.GetFileNameWithoutExtension(path);
                    }

                    string newEmotion;
                    do
                    {
                        int randomIndex = new Random().Next(_emotions.Count);
                        newEmotion = _emotions[randomIndex];
                    } while (newEmotion == currentEmotion);

                    // 新しいアイコンを設定
                    var assistant = AssistantManager.Instance.GetCurrentAssistant();
                    if (assistant != null)
                    {
                        string iconPath = System.IO.Path.Combine(assistant.IconsPath, $"{newEmotion}.png");
                        if (File.Exists(iconPath))
                        {
                            _aiIconImage.Source = new BitmapImage(new Uri(iconPath));
                        }
                    }

                    // デバッグ情報
                    Debug.WriteLine($"イースターエッグ: アイコンを {newEmotion} に変更しました");
                }
            }
        }

        private void ShowSettingsWindow()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsWindow.ShowDialog();
        }

        private void ShowDiaryBrowserWindow()
        {
            var diaryBrowserWindow = new DiaryBrowserWindow();
            diaryBrowserWindow.Owner = this;
            diaryBrowserWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            diaryBrowserWindow.ShowDialog();
        }

        private void TogglePauseNotifications()
        {
            _notificationService.TogglePauseNotifications();
        }

        private void NotificationService_StatusChanged(object sender, bool isPaused)
        {
            // UIスレッドで実行
            if (Application.Current.Dispatcher.CheckAccess())
            {
                UpdateNotificationPauseStatus(isPaused);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => UpdateNotificationPauseStatus(isPaused));
            }
        }

        private void UpdateNotificationPauseStatus(bool isPaused)
        {
            // メニュー項目のテキストを更新
            _pauseNotificationItem.Text = isPaused ? "通知を再開する" : "通知を一時停止";

            // アイコンを更新
            string iconName = isPaused ? "paused" : "normal";

            try
            {
                // 現在のアシスタントからアイコンを取得
                var assistant = AssistantManager.Instance.GetCurrentAssistant();

                if (assistant != null)
                {
                    string iconPath = System.IO.Path.Combine(assistant.IconsPath, $"{iconName}.ico");

                    if (System.IO.File.Exists(iconPath))
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    }
                }

                // ツールチップテキストも更新
                var settings = DatabaseService.GetSettings();
                string assistantName = settings?.AiAssistantName ?? "日記作成アシスタント";
                _notifyIcon.Text = isPaused ?
                    $"{assistantName} (通知一時停止中)" :
                    assistantName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"アイコン更新エラー: {ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初回起動時にAPIキーが設定されていない場合はAPI設定ウィンドウを表示
            var settings = DatabaseService.GetSettings();
            if (settings == null ||
                (string.IsNullOrEmpty(settings.GeminiApiKey) && !settings.UseOllama))
            {
                var apiKeySetupWindow = new Views.ApiKeySetupWindow();

                // モーダルで表示
                apiKeySetupWindow.ShowDialog();
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // 最小化時はそのままの状態を維持（タスクバーに表示されたまま）
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // タスクトレイの「終了」から呼ばれた場合は、そのまま終了する
            if (_isExitingFromTray)
            {
                // トレイアイコンの解放
                _notifyIcon.Dispose();
                return;
            }

            // ウィンドウの×ボタンをクリックした場合
            // アプリケーションを終了せず、タスクトレイに格納
            e.Cancel = true; // 閉じる操作をキャンセル
            Hide(); // タスクバーから非表示（最小化はしない）
        }
    }

    // 変換クラス
    public class EmotionToIconConverter : System.Windows.Data.IValueConverter
    {
        private readonly ResourceService _resourceService = new ResourceService();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string emotion)
            {
                string assistantId = parameter as string;
                return _resourceService.GetEmotionIcon(emotion, assistantId);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToAIBackgroundConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isFromAI)
            {
                if (isFromAI)
                {
                    // AIメッセージは単色のまま
                    return new SolidColorBrush(Color.FromRgb(241, 245, 249));
                }
                else
                {
                    // ユーザーメッセージは青いグラデーション
                    var gradient = new LinearGradientBrush();
                    gradient.StartPoint = new Point(0, 0); // 上端
                    gradient.EndPoint = new Point(0, 1);   // 下端

                    // 明るめの青から少し暗めの青へのグラデーション
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(14, 165, 233), 0.0)); // 上部の色
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(59, 130, 246), 1.0)); // 下部の色

                    return gradient;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 新しいコンバーター: テキストの色を変換するためのコンバーター
    public class IsFromAIToForegroundConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isFromAI)
            {
                return isFromAI ?
                    Brushes.Black :  // AIからのメッセージは黒文字
                    Brushes.White;   // 自分のメッセージは白文字
            }
            return Brushes.Black;  // デフォルトは黒
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConversationVisibilityConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool hasSelectedEntry && values[1] is bool isConversationVisible)
            {
                return (hasSelectedEntry && isConversationVisible) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // メッセージの水平位置を変換するコンバーター
    public class IsFromAIToAlignmentConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isFromAI)
            {
                return isFromAI ?
                    HorizontalAlignment.Left :  // AIからのメッセージは左寄せ
                    HorizontalAlignment.Right;  // 自分のメッセージは右寄せ
            }
            return HorizontalAlignment.Left;  // デフォルトは左
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageMaxWidthConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is double parentWidth) || !(values[1] is bool isFromAI))
                return 300.0; // デフォルト値

            // アイコンのスペース(64px)と余白(10px + 12px)を考慮
            double padding = isFromAI ? 100.0 : 60.0;

            // 親コンテナの80%を最大幅として計算（最小値は200px、最大値は450px）
            double calculatedWidth = Math.Max(200.0, Math.Min(450.0, (parentWidth - padding) * 0.8));

            return calculatedWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // アシスタントIDからアイコンに変換するコンバーター
    public class AssistantIdToIconConverter : IValueConverter
    {
        private readonly ResourceService _resourceService = new ResourceService();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string assistantId)
            {
                string emotion = parameter as string ?? "normal";

                // アシスタントIDからアシスタント情報を取得
                var assistantManager = AssistantManager.Instance;
                var allAssistants = assistantManager.GetAllAssistants();
                var assistant = allAssistants.FirstOrDefault(a => a.Id == assistantId);

                if (assistant != null)
                {
                    // アシスタントが存在する場合
                    string iconPath = System.IO.Path.Combine(assistant.IconsPath, $"{emotion}.png");
                    if (File.Exists(iconPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(iconPath);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }

                // アシスタントが見つからないか、アイコンが見つからない場合は白いアイコンを返す
                return CreatePlaceholderIcon();
            }

            return null;
        }

        private BitmapImage CreatePlaceholderIcon()
        {
            // 白い画像を表示する（または適当なプレースホルダー画像へのパス）
            string placeholderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "placeholder.png");
            if (File.Exists(placeholderPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(placeholderPath);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }

            // プレースホルダーもない場合は空の白いビットマップを作成
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 感情とアシスタントIDからアイコンに変換するコンバーター
    public class AssistantIconConverter : System.Windows.Data.IMultiValueConverter
    {
        private readonly ResourceService _resourceService = new ResourceService();

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string && values[1] is string)
            {
                string emotion = (string)values[0];
                string assistantId = (string)values[1];
                return _resourceService.GetEmotionIcon(emotion, assistantId);
            }
            else if (values.Length >= 1 && values[0] is string)
            {
                string emotion = (string)values[0];
                return _resourceService.GetEmotionIcon(emotion);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // アシスタントのnormal.pngパスをImageSourceに変換するコンバーター
    public class AssistantNormalIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is AssistantInfo assistant && assistant != null)
            {
                string iconPath = assistant.NormalIconPath;
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    try
                    {
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
                        System.Diagnostics.Debug.WriteLine($"画像読み込みエラー: {ex.Message}");
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // ScrollViewerの幅からTextBoxの最大幅を計算するコンバーター
    public class MaxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double containerWidth && containerWidth > 0)
            {
                // ボタンの幅とマージンを考慮して、コンテナ幅から約60px引いた値を最大幅に設定
                return Math.Max(100, containerWidth - 60);
            }
            return 400.0; // デフォルト値
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}