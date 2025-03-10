using System.Windows;

namespace DiaryAssistant
{
    public partial class MainWindow : Window
    {
        private Views.MainWindow _mainWindowImpl;

        public MainWindow()
        {
            InitializeComponent();

            // 実装クラスのインスタンスを作成
            _mainWindowImpl = new Views.MainWindow();

            // MainWindowの配置を引き継ぐ前に、サイズと位置が設定されていることを確認
            double width = this.Width > 0 ? this.Width : 400;
            double height = this.Height > 0 ? this.Height : 440;

            // 明示的にサイズを設定
            _mainWindowImpl.Width = width;
            _mainWindowImpl.Height = height;
            _mainWindowImpl.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 実装ウィンドウを表示して、このプレースホルダーを閉じる
            _mainWindowImpl.Show();
            this.Close();
        }
    }
}