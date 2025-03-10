using DiaryAssistant.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel; // Closing イベント用に追加

namespace DiaryAssistant.Views
{
    public class ApiKeySetupWindow : Window
    {
        private TextBox _apiKeyTextBox;
        private Button _saveButton;
        private ResourceService _resourceService;
        private bool _apiKeySaved = false; // APIキーが保存されたかどうかのフラグ

        public ApiKeySetupWindow()
        {
            Title = "Gemini APIキーの設定";
            Width = 650;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Style = (Style)FindResource("Windows11WindowStyle");

            Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            // リソースサービスの初期化
            _resourceService = new ResourceService();

            // ウィンドウのコンテンツ設定
            Content = CreateContent();

            // Closingイベントを追加
            Closing += ApiKeySetupWindow_Closing;
        }

        private void ApiKeySetupWindow_Closing(object sender, CancelEventArgs e)
        {
            // APIキーが保存されていない場合
            if (!_apiKeySaved)
            {
                var result = MessageBox.Show(
                    "APIキーが設定されていません。アプリケーションを終了しますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // アプリケーション全体を終了
                    Application.Current.Shutdown();
                }
                else
                {
                    // 閉じる操作をキャンセル
                    e.Cancel = true;
                }
            }
        }

        private UIElement CreateContent()
        {
            var grid = new Grid
            {
                Margin = new Thickness(20)
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // アイコンと説明
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // チュートリアル
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // APIキー入力
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // よくある質問
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // ボタン

            // アイコンと説明
            var iconAndDescPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20) // 上部マージンを追加
            };

            // ハッピーアイコン
            var iconBorder = new Border
            {
                Width = 100,
                Height = 100,
                CornerRadius = new CornerRadius(50),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 20, 0)
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

            // アイコンを読み込む（アシスタント固有のものを使用）
            var assistant = AssistantManager.Instance.GetCurrentAssistant();
            if (assistant != null && System.IO.File.Exists(assistant.NormalIconPath))
            {
                iconImage.Source = new BitmapImage(new Uri(assistant.NormalIconPath));
            }
            else
            {
                // 現在のアシスタントからアイコンが取得できない場合はデフォルトアイコン
                iconImage.Source = new BitmapImage(new Uri("pack://application:,,,/DiaryAssistant;component/app.ico"));
            }

            iconBorder.Child = iconImage;
            iconAndDescPanel.Children.Add(iconBorder);

            // 説明テキスト
            var descriptionPanel = new StackPanel
            {
                Width = 450,
                VerticalAlignment = VerticalAlignment.Center
            };

            var welcomeText = new TextBlock
            {
                Text = "ようこそ！日記作成アシスタントへ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var descriptionText = new TextBlock
            {
                Text = "このアプリを使用するには、Gemini APIキーが必要です。次の手順に従って、APIキーを取得して設定してください。",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };

            descriptionPanel.Children.Add(welcomeText);
            descriptionPanel.Children.Add(descriptionText);
            iconAndDescPanel.Children.Add(descriptionPanel);

            Grid.SetRow(iconAndDescPanel, 0);
            grid.Children.Add(iconAndDescPanel);

            // チュートリアル
            var tutorialPanel = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var tutorialContent = new StackPanel();

            var tutorialTitle = new TextBlock
            {
                Text = "APIキーの取得方法",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255))
            };
            tutorialContent.Children.Add(tutorialTitle);

            // 手順1
            var step1Panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var step1Title = new TextBlock
            {
                Text = "1. Google AI Studioにアクセス",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var step1Desc = new TextBlock
            {
                Text = "以下のボタンをクリックして、Google AI Studioのウェブサイトにアクセスします。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var step1Button = new Button
            {
                Content = "Google AI Studio で Gemini API キーを取得する",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 5, 0, 0),
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Height = 32
            };
            step1Button.Click += (s, e) => Process.Start("https://ai.google.dev/gemini-api/docs/api-key?hl=ja");

            step1Panel.Children.Add(step1Title);
            step1Panel.Children.Add(step1Desc);
            step1Panel.Children.Add(step1Button);
            tutorialContent.Children.Add(step1Panel);

            // 手順2
            var step2Panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var step2Title = new TextBlock
            {
                Text = "2. APIキーを作成",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var step2Desc = new TextBlock
            {
                Text = "Google AI Studioで「APIキーを作成」をクリックします。必要に応じてGoogleアカウントでログインしてください。",
                TextWrapping = TextWrapping.Wrap
            };
            step2Panel.Children.Add(step2Title);
            step2Panel.Children.Add(step2Desc);
            tutorialContent.Children.Add(step2Panel);

            // 手順3
            var step3Panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var step3Title = new TextBlock
            {
                Text = "3. APIキーをコピー",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var step3Desc = new TextBlock
            {
                Text = "生成されたAPIキーをコピーして、下記の入力欄に貼り付けてください。",
                TextWrapping = TextWrapping.Wrap
            };
            step3Panel.Children.Add(step3Title);
            step3Panel.Children.Add(step3Desc);
            tutorialContent.Children.Add(step3Panel);

            tutorialPanel.Child = tutorialContent;
            Grid.SetRow(tutorialPanel, 1);
            grid.Children.Add(tutorialPanel);

            // APIキー入力
            var apiKeyPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            var apiKeyLabel = new TextBlock
            {
                Text = "APIキーを入力",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _apiKeyTextBox = new TextBox
            {
                Style = (Style)FindResource("Windows11TextBoxStyle"),
                Padding = new Thickness(10),
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                Height = 40,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // TextChangedイベントを追加してボタンの状態を更新
            _apiKeyTextBox.TextChanged += ApiKeyTextBox_TextChanged;

            apiKeyPanel.Children.Add(apiKeyLabel);
            apiKeyPanel.Children.Add(_apiKeyTextBox);
            Grid.SetRow(apiKeyPanel, 2);
            grid.Children.Add(apiKeyPanel);

            // よくある質問
            var faqPanel = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var faqContent = new StackPanel();

            var faqTitle = new TextBlock
            {
                Text = "よくある質問",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            faqContent.Children.Add(faqTitle);

            // FAQ1
            var faq1Panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var faq1Question = new TextBlock
            {
                Text = "Q: Gemini APIは有料ですか？",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var faq1Answer = new TextBlock
            {
                Text = "A: 無料枠で使用することができます。 ※2025年3月時点　詳細は以下のリンクをご確認ください。",
                TextWrapping = TextWrapping.Wrap
            };
            var faq1Link = new TextBlock
            {
                Text = "Gemini API の料金について詳しく見る",
                Foreground = Brushes.Blue,
                TextDecorations = TextDecorations.Underline,
                Margin = new Thickness(0, 5, 0, 0)
            };
            faq1Link.MouseLeftButtonDown += (s, e) => Process.Start("https://ai.google.dev/gemini-api/docs/pricing?hl=ja");

            faq1Panel.Children.Add(faq1Question);
            faq1Panel.Children.Add(faq1Answer);
            faq1Panel.Children.Add(faq1Link);
            faqContent.Children.Add(faq1Panel);

            faqPanel.Child = faqContent;
            Grid.SetRow(faqPanel, 3);
            grid.Children.Add(faqPanel);

            // ボタンパネル
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // スキップボタンを追加
            var skipButton = new Button
            {
                Content = "スキップ",
                Width = 120,
                Height = 40,
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = new SolidColorBrush(Color.FromRgb(209, 213, 219)), // グレー
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 10, 0)
            };
            skipButton.Click += SkipApiKey_Click;

            _saveButton = new Button
            {
                Content = "保存して続ける",
                Width = 150,
                Height = 40,
                Style = (Style)FindResource("Windows11ButtonStyle"),
                Background = new SolidColorBrush(Color.FromRgb(209, 213, 219)), // 初期状態はグレー
                Foreground = Brushes.Black
            };
            _saveButton.Click += SaveApiKey_Click;

            buttonPanel.Children.Add(skipButton);
            buttonPanel.Children.Add(_saveButton);
            Grid.SetRow(buttonPanel, 4);
            grid.Children.Add(buttonPanel);

            return grid;
        }

        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // APIキーが入力されているかチェック
            if (!string.IsNullOrWhiteSpace(_apiKeyTextBox.Text))
            {
                // 入力があれば青色に
                _saveButton.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                _saveButton.Foreground = Brushes.White;
            }
            else
            {
                // 入力がなければグレーに
                _saveButton.Background = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                _saveButton.Foreground = Brushes.Black;
            }
        }

        private void SkipApiKey_Click(object sender, RoutedEventArgs e)
        {
            // APIキーなしでスキップする場合は通知
            MessageBox.Show(
                "API設定をスキップしました。\n\n" +
                "あとで設定画面からAPIキーを入力することもできます。",
                "スキップ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // APIキーはスキップされたがウィンドウは閉じる
            _apiKeySaved = true;
            DialogResult = true;
            Close();
        }

        private void SaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = _apiKeyTextBox.Text.Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show(
                    "APIキーが入力されていません。\n\n" +
                    "APIキーを入力してください。",
                    "入力エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 設定を取得して更新
            var settings = DatabaseService.GetSettings();
            if (settings == null)
            {
                settings = new Models.AppSettings
                {
                    UserName = Environment.UserName,
                    AiAssistantName = "ソフィア",
                    NotificationFrequencyMinutes = 5,
                    NotificationStartTime = new TimeSpan(9, 0, 0),
                    NotificationEndTime = new TimeSpan(1, 0, 0),
                    GetActiveWindowInfo = true,
                    RunAtStartup = false,
                    GeminiApiKey = apiKey,
                    GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
                    FontFamily = "Yu Gothic",
                    FontSize = 16.0
                };
            }
            else
            {
                settings.GeminiApiKey = apiKey;
            }

            // 設定を保存
            DatabaseService.SaveSettings(settings);

            // APIキーが保存されたことを記録
            _apiKeySaved = true;

            MessageBox.Show(
                "APIキーを保存しました。",
                "保存完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
    }
}