using System.Windows;
using System.Windows.Input;

namespace CalcApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private string[] mProgramTitle = {                      //  プログラムタイトルリスト
            "計算式電卓",
            "フラクタル図形",
            "関数グラフ",
            "三次元関数グラフ",
            "表・グラフ化ツール",
            //"回帰分析",
        };

        public MainWindow()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ProgramList.Items.Clear();
            foreach (var title in mProgramTitle)
                ProgramList.Items.Add(title);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        /// <summary>
        /// 選択されたプログラムを実行する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgramList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Window programDlg = null;
            switch (ProgramList.SelectedIndex) {
                case 0: programDlg = new Calculate(); break;
                case 1: programDlg = new Fractal(); break;
                case 2: programDlg = new FuncPlot(); break;
                case 3: programDlg = new GLGraph(); break;
                case 4: programDlg = new SpreadSheet(); break;
                case 5: programDlg = new RegressionAnalysis(); break;
            }
            if (programDlg != null)
                programDlg.Show();
            //programDlg.ShowDialog();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 || Properties.Settings.Default.MainWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.MainWindowTop;
                this.Left = Properties.Settings.Default.MainWindowLeft;
                this.Width = Properties.Settings.Default.MainWindowWidth;
                this.Height = Properties.Settings.Default.MainWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = this.Top;
            Properties.Settings.Default.MainWindowLeft = this.Left;
            Properties.Settings.Default.MainWindowWidth = this.Width;
            Properties.Settings.Default.MainWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }
    }
}
