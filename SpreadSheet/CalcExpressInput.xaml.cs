using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// CalcExpressInput.xaml の相互作用ロジック
    /// </summary>
    public partial class CalcExpressInput : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅

        private List<string[]> mExpressList = new List<string[]>();
        private string mCalcExpressPath = "CalcExpress.csv";
        private string[] mExpressListTitle = new string[] { "Title", "Express" };
        public string[] mColumnTitles;
        public string[] mFunctions;
        public int mFontSize = 12;                          //  文字サイズ


        private YLib ylib = new YLib();

        public CalcExpressInput()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元
            mCalcExpressPath = Path.Combine(ylib.getAppFolderPath(), mCalcExpressPath);
            loadExpreesList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CbExpress.Items.Clear();
            foreach(string[] express in mExpressList) {
                CbExpress.Items.Add(express[1]);
            }
            CbColumn.ItemsSource = mColumnTitles;
            CbFunction.ItemsSource = mFunctions;
            //  文字サイズ
            TbTitle.FontSize = mFontSize;
            CbExpress.FontSize = mFontSize;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveExpressList();
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.CalcExpressInputWidth < 100 ||
                Properties.Settings.Default.CalcExpressInputHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.CalcExpressInputHeight) {
                Properties.Settings.Default.CalcExpressInputWidth = mWindowWidth;
                Properties.Settings.Default.CalcExpressInputHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.CalcExpressInputTop;
                this.Left = Properties.Settings.Default.CalcExpressInputLeft;
                this.Width = Properties.Settings.Default.CalcExpressInputWidth;
                this.Height = Properties.Settings.Default.CalcExpressInputHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.CalcExpressInputTop = this.Top;
            Properties.Settings.Default.CalcExpressInputLeft = this.Left;
            Properties.Settings.Default.CalcExpressInputWidth = this.Width;
            Properties.Settings.Default.CalcExpressInputHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 数式変更時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbExpress_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbExpress.SelectedIndex && TbTitle.Text.Length == 0 && 0 < mExpressList.Count) {
                TbTitle.Text = mExpressList[CbExpress.SelectedIndex][0];    //  タイトル設定
            }
        }

        /// <summary>
        /// 選択した列名を数式に追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbColumn_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //string buf1 = CbExpress.Text.Substring(0, CbExpress.SelectionStart);
            //string buf2 = CbExpress.Text.Substring(CbExpress.SelectionStart + CbExpress.SelectionLength);
            int n = Array.IndexOf(mColumnTitles, CbColumn.Text);
            CbExpress.Text += "[" + n + ":" + CbColumn.Text + "]";
        }

        /// <summary>
        /// 選択した関数名を数式に追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFunction_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CbExpress.Text += CbFunction.Text.Substring(0, CbFunction.Text.IndexOf(" "));
        }

        /// <summary>
        /// [OK}処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < mExpressList.Count; i++) {
                if (mExpressList[i][1].CompareTo(CbExpress.Text.Trim()) == 0) {
                    mExpressList.RemoveAt(i);
                    break;
                }
            }
            string[] data = new string[] { TbTitle.Text.Trim(), CbExpress.Text.Trim() };
            mExpressList.Insert(0, data);
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// [Cancel]処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 数式をファイルから取り込む
        /// </summary>
        private void loadExpreesList()
        {
            List<string[]> expressList = ylib.loadCsvData(mCalcExpressPath, mExpressListTitle);
            if (expressList == null)
                return;
            mExpressList.Clear();
            //  数式の重複なしで設定
            foreach (string[] data in expressList) {
                bool contain = false;
                foreach (string[] arg in mExpressList) {
                    if (arg[1].CompareTo(data[1]) == 0) {
                        contain = true;
                        break;
                    }
                }
                if (!contain)
                    mExpressList.Add(data);
            }
        }

        /// <summary>
        /// 数式をファイルに保存
        /// </summary>
        private void saveExpressList()
        {
            ylib.saveCsvData(mCalcExpressPath, mExpressListTitle, mExpressList);
        }

        /// <summary>
        /// 文字サイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("BtGZoomDown") == 0) {
                mFontSize--;
            } else if (bt.Name.CompareTo("BtGZoomUp") == 0) {
                mFontSize++;
            }
            TbTitle.FontSize = mFontSize;
            CbExpress.FontSize = mFontSize;
        }

        /// <summary>
        /// 表示中の数式を削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtRemove_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < mExpressList.Count; i++) {
                if (mExpressList[i][1].CompareTo(CbExpress.Text.Trim()) == 0) {
                    mExpressList.RemoveAt(i);
                    break;
                }
            }
            CbExpress.Items.Clear();
            foreach (string[] express in mExpressList) {
                CbExpress.Items.Add(express[1]);
            }
            CbExpress.Text = "";
            TbTitle.Text = "";
        }
    }
}
