using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// ConvFileSelect.xaml の相互作用ロジック
    /// 置換データ選択ダイヤログ
    /// </summary>
    public partial class ConvFileSelect : Window
    {
        private double mWindowWidth;        //  ウィンドウの高さ
        private double mWindowHeight;       //  ウィンドウ幅

        public string mFolder = "";         //  データフォルダ
        public string mSelectFile = "";     //  変換ファイルパス

        YLib ylib = new YLib();

        public ConvFileSelect()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;

            WindowFormLoad();       //  Windowの位置とサイズを復元
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            setFiles();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.ConvFileSelectWidth < 100 ||
                Properties.Settings.Default.ConvFileSelectHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.ConvFileSelectHeight) {
                Properties.Settings.Default.ConvFileSelectWidth = mWindowWidth;
                Properties.Settings.Default.ConvFileSelectHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.ConvFileSelectTop;
                Left = Properties.Settings.Default.ConvFileSelectLeft;
                Width = Properties.Settings.Default.ConvFileSelectWidth;
                Height = Properties.Settings.Default.ConvFileSelectHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.ConvFileSelectTop = Top;
            Properties.Settings.Default.ConvFileSelectLeft = Left;
            Properties.Settings.Default.ConvFileSelectWidth = Width;
            Properties.Settings.Default.ConvFileSelectHeight = Height;
            Properties.Settings.Default.Save();
        }

        private void LbFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LbFileList.SelectedIndex) {
                mSelectFile = LbFileList.Items[LbFileList.SelectedIndex].ToString();
                mSelectFile = Path.Combine(mFolder, mSelectFile);
                Process.Start(mSelectFile);
            }
        }

        /// <summary>
        /// [インポート]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtImport_Click(object sender, RoutedEventArgs e)
        {
            string filePath = ylib.fileSelect("", "csv");
            if (0 < filePath.Length) {
                string destPath = Path.Combine(mFolder, Path.GetFileName(filePath));
                if (File.Exists(destPath)) {
                    var result = MessageBox.Show("ファイルが存在しますが上書きしてもいいですか?", "確認", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK) {
                        File.Copy(filePath, destPath, true);
                    } else
                        return;
                } else {
                    File.Copy(filePath, destPath);
                }
                setFiles();
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= LbFileList.SelectedIndex) {
                //  選択ファイル(置換データ)を設定
                mSelectFile = LbFileList.Items[LbFileList.SelectedIndex].ToString();
                mSelectFile = Path.Combine(mFolder, mSelectFile);
            } else
                mSelectFile = "";
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// [Cancel]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// データフォルダのファイル名をリストボックスに登録
        /// </summary>
        private void setFiles()
        {
            if (!Directory.Exists(mFolder))
                Directory.CreateDirectory(mFolder);
            string[] files = ylib.getFiles(mFolder + "\\*.csv");
            for (int i = 0; i < files.Length; i++) {
                files[i] = Path.GetFileName(files[i]);
            }
            LbFileList.ItemsSource = files;
        }
    }
}
