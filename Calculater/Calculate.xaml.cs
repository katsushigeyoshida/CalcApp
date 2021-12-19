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
    /// Calculate.xaml の相互作用ロジック
    /// </summary>
    public partial class Calculate : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private String mAppFolder;
        private String mDataFileName = "Calculate.csv";
        private int mStacPanelSize;
        private double mStackPanelHeight;
        private int mFontSize = 12;                          //  文字サイズ
        private Dictionary<String, String> mDataTable = new Dictionary<String, String>();       //  関数式データ
        private Dictionary<String, String> mCommentTable = new Dictionary<String, String>();    //  コメントデータ
        private Label[] mArgLabel = new Label[10];
        private TextBox[] mArgData = new TextBox[10];
        private StackPanel[] mStackPanel = new StackPanel[10];
        private string[] mFirstData = new string[] { 
            "計算式を入力して計算する",
            "計算式を入れて[計算]ボタンを押すと計算します。" +
            "タイトルで登録されている計算式が選択できます。" };

        private YCalc mCalc = new YCalc();
        private YLib mYlib = new YLib();

        public Calculate()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width;
            mWindowHeight = WindowForm.Height;
            mPrevWindowWidth = mWindowWidth;

            windowFormLoad();

            calculateForm.FontSize = mFontSize;

            //  実行ファイルのフォルダを取得しワークフォルダとする
            mAppFolder = System.AppDomain.CurrentDomain.BaseDirectory;

            loadDataFile();
            setDataName();              //  ComboBoxにデータを登録
            if (mDataTable.Count == 0) {
                calculateName.Items.Add("計算式を入力して計算する");
                calculateName.SelectedIndex = 0;
                comment.Text = "計算式を入れて[計算]ボタンを押すと計算します。" +
                                "タイトルで登録されている計算式が選択できます。";
                calculateForm.Text = "";
            } else {
                calculateName.SelectedIndex = 0;
                if (mCommentTable.ContainsKey(calculateName.Text))
                    comment.Text = mCommentTable[calculateName.Text];
                else
                    comment.Text = "";
                calculateForm.Text = mDataTable[calculateName.Text];
            }
            resultLine.Content = "";
            mStacPanelSize = stackPanel.Children.Count;
        }

        /// <summary>
        /// Window形状が変更された時の処理
        /// 引数の入力がStackPanelに追加になった時にWindowの大きさを調整する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_LayoutUpdated(object sender, EventArgs e)
        {
            //  StackPanel(引数入力)の初期の高さを保存
            if (mStackPanelHeight == 0)
                mStackPanelHeight = stackPanel.ActualHeight;
            //  引数入力が追加された時
            if (mStackPanelHeight < stackPanel.ActualHeight) {
                int n = mCalc.getArgDic().Count;
                if (0 < n) {
                    double dy = mStackPanel[0].ActualHeight * n;
                    WindowForm.Height = mWindowHeight + dy + 10;
                } else {
                    WindowForm.Height = mWindowHeight;
                }
            }

            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                //mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != WindowForm.Width ||
                mWindowHeight != WindowForm.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = WindowForm.Width;
                //mWindowHeight = WindowForm.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }
            double dx = mWindowWidth - mPrevWindowWidth;
            calculateName.Width += dx;
            comment.Width += dx;
            calculateForm.Width += dx;
            mPrevWindowWidth = mWindowWidth;

            mWindowState = this.WindowState;
        }

        /// <summary>
        /// クローズ処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            windowFormSave();
            //  計算式データをファイルに保存する
            saveDataFile();
        }

        /// <summary>
        /// Windowの位置とサイズを復元
        /// </summary>
        private void windowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.CalculateWindowWidth < 100 || Properties.Settings.Default.CalculateWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.CalculateWindowHeight) {
                Properties.Settings.Default.CalculateWindowWidth = mWindowWidth;
                //Properties.Settings.Default.CalculateWindowHeight = mWindowHeight;
            } else {
                WindowForm.Top = Properties.Settings.Default.CalculateWindowTop;
                WindowForm.Left = Properties.Settings.Default.CalculateWindowLeft;
                WindowForm.Width = Properties.Settings.Default.CalculateWindowWidth;
                //WindowForm.Height = Properties.Settings.Default.CalculateWindowHeight;
                //double dy = WindowForm.Height - mWindowHeight;
            }
        }

        /// <summary>
        /// Windowの位置とサイズを保存
        /// </summary>
        private void windowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.CalculateWindowTop = WindowForm.Top;
            Properties.Settings.Default.CalculateWindowLeft = WindowForm.Left;
            Properties.Settings.Default.CalculateWindowWidth = WindowForm.Width;
            Properties.Settings.Default.CalculateWindowHeight = WindowForm.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [登録]ボタン 登録処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (0 < calculateName.Text.Length) {
                if (mDataTable.ContainsKey(calculateName.Text)) {
                    mCommentTable[calculateName.Text] = comment.Text;
                    mDataTable[calculateName.Text] = calculateForm.Text;
                } else {
                    mDataTable.Add(calculateName.Text, calculateForm.Text);
                }
            }
            setDataName();
        }

        /// <summary>
        /// [削除]ボタン 計算式の削除処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            mDataTable.Remove(calculateName.Text);
            mCommentTable.Remove(calculateName.Text);
            setDataName();
        }

        /// <summary>
        /// [関数]ボタン　関数の選択メニューダイヤログの表示と計算式への挿入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuncBtn_Click(object sender, RoutedEventArgs e)
        {
            FuncMenu dlg = new FuncMenu();
            if (dlg.ShowDialog() == true) {
                string buf1 = calculateForm.Text.Substring(0, calculateForm.SelectionStart);
                string buf2 = calculateForm.Text.Substring(calculateForm.SelectionStart + calculateForm.SelectionLength);
                calculateForm.Text = buf1 + dlg.mResultFunc.Substring(0, dlg.mResultFunc.IndexOf(' ')) + buf2;
            }
        }

        /// <summary>
        /// [コピー]ボタン 計算結果をクリップボードにコピーする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(resultLine.Content.ToString());
        }

        /// <summary>
        /// [クリア]ボタン 計算式をクリアする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (0 < resultLine.Content.ToString().Length) {
                resultLine.Content = "";
            } else {
                calculateForm.Text = "";
            }
        }

        /// <summary>
        /// [計算]ボタン 計算処理を実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalBtn_Click(object sender, RoutedEventArgs e)
        {
            calculation();
        }

        /// <summary>
        /// [(+)]ボタン　計算式のフォントサイズを大きくする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGZoomUp_Click(object sender, RoutedEventArgs e)
        {
            mFontSize++;
            calculateForm.FontSize = mFontSize;
        }

        /// <summary>
        /// [(-)]ボタン　計算式のフォントサイズを小さくする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGZoomDown_Click(object sender, RoutedEventArgs e)
        {
            mFontSize--;
            calculateForm.FontSize = mFontSize;
        }

        /// <summary>
        /// [?]ボタン ヘルプファイルを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            string[] pdfHelpFile = { "計算式電卓.pdf" };
            //HelpView help = new HelpView();
            //help.mHelpText = HelpText.mCalcurateHelp;
            //help.mPdfFile = pdfHelpFile;
            //help.Show();
            mYlib.fileExecute(pdfHelpFile[0]);
            //WpfLib.PdfView pdfView = new WpfLib.PdfView();
            //pdfView.mPdfFile = pdfHelpFile[0];
            //pdfView.Show();
        }

        /// <summary>
        /// タイトル項目を変更したときの処理
        /// 計算式に引数のあるものは引数の入力表示をさせる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculateName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < calculateName.Items.Count) {
                if (0 <= calculateName.SelectedIndex) {
                    String key = calculateName.Items[calculateName.SelectedIndex].ToString();
                    if (mDataTable.ContainsKey(key)) {
                        if (mCommentTable.ContainsKey(key))
                            comment.Text = mCommentTable[key];
                        calculateForm.Text = mDataTable[key];
                        setArgForm(mDataTable[key]);
                    }
                }
            }
        }

        /// <summary>
        /// 計算枠のReturnで計算実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculateForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) {
                calculation();
            }
        }

        /// <summary>
        /// 計算を実施
        /// </summary>
        private void calculation()
        {
            if (0 == calculateForm.Text.Length)
                return;
            if (!mYlib.IsNumberString(resultLine.Content.ToString(), true))
                resultLine.Content = "";

            double result;
            int n = stackPanel.Children.Count - mStacPanelSize;
            if (0 < n) {
                //  引数付きの計算式
                for (int i = 0; i < n; i++) {
                    if (mArgLabel[i] != null && mArgData[i] != null)
                        if (0 < mArgData[i].Text.Length)
                            mCalc.setArgValue(mArgLabel[i].Content.ToString(), mArgData[i].Text);
                }
                result = mCalc.calculate();
            } else {
                //  通常の計算式
                string express = calculateForm.Text;
                while (0 <= express.IndexOf("[#]")) {
                    express = express.Replace("[#]", mYlib.string2StringNum(resultLine.Content.ToString()));
                }
                result = mCalc.expression(express);
            }
            //  結果表示
            if (mCalc.mError) {
                resultLine.Content = "ERROR " + mCalc.mErrorMsg;
            } else {
                resultLine.Content = mYlib.setDigitSeparator(result.ToString());    //  3桁区切り表示
            }
        }

        /// <summary>
        /// データテーブルのタイトルをコンボボックスに登録
        /// </summary>
        private void setDataName()
        {
            if (0 < mDataTable.Count) {
                calculateName.Items.Clear();
                foreach (String key in mDataTable.Keys) {
                    calculateName.Items.Add(key);
                }
            }
        }

        /// <summary>
        /// 計算式内の引数を取り出して入力コントロールを作成しStackPanelに挿入する
        /// </summary>
        /// <param name="str"></param>
        private void setArgForm(String str)
        {
            if (0 < mStacPanelSize) {
                clearArgForm();
                if (0 < mCalc.setExpression(str)) {
                    int i = 0;
                    foreach (KeyValuePair<String, String> kvp in mCalc.getArgDic()) {
                        if (kvp.Key.CompareTo("[@]") != 0 && kvp.Key.CompareTo("[%]") != 0 
                            && kvp.Key.CompareTo("[#]") != 0) {
                            mArgLabel[i] = new Label();
                            mArgLabel[i].Content = kvp.Key;
                            mArgLabel[i].Margin = new Thickness(10, 0, 0, 0);
                            mArgLabel[i].Width = 150;
                            mArgData[i] = new TextBox();
                            mArgData[i].Text = kvp.Value;
                            mArgData[i].Width = 100;
                            mStackPanel[i] = new StackPanel();
                            mStackPanel[i].Orientation = Orientation.Horizontal;
                            mStackPanel[i].Children.Add(mArgLabel[i]);
                            mStackPanel[i].Children.Add(mArgData[i]);
                            int n = stackPanel.Children.Count;
                            stackPanel.Children.Insert(n - 1, mStackPanel[i]);
                            i++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 引数の入力コントロールを削除する
        /// </summary>
        private void clearArgForm()
        {
            int n = stackPanel.Children.Count;
            while (mStacPanelSize < n) {
                stackPanel.Children.RemoveAt(n - 2);
                n = stackPanel.Children.Count;
            }

        }

        /// <summary>
        /// 計算式のリストをファイルに保存
        /// </summary>
        private void saveDataFile()
        {
            String filePath = mAppFolder + "\\" + mDataFileName;
            if (0 < mDataTable.Count) {
                System.IO.StreamWriter dataFile = new System.IO.StreamWriter(filePath, false);
                foreach (KeyValuePair<String, String> kvp in mDataTable) {
                    dataFile.WriteLine("\"" + kvp.Key + "\",\"" + kvp.Value + "\",\"" +
                        (mCommentTable.ContainsKey(kvp.Key) ? mCommentTable[kvp.Key] : "") + "\"");
                }
                dataFile.Close();
            }
        }

        /// <summary>
        /// ファイルから計算式を取り込む
        /// </summary>
        private void loadDataFile()
        {
            String filePath = mAppFolder + "\\" + mDataFileName;
            if (System.IO.File.Exists(filePath)) {
                System.IO.StreamReader dataFile = new System.IO.StreamReader(filePath);
                mDataTable.Clear();
                mCommentTable.Clear();
                mDataTable.Add(mFirstData[0], "");
                mCommentTable.Add(mFirstData[0], mFirstData[1]);
                String line;
                while ((line = dataFile.ReadLine()) != null) {
                    String[] buf = mYlib.seperateString(line);
                    if (!mDataTable.ContainsKey(buf[0])) {
                        if (1 < buf.Length)
                            mDataTable.Add(buf[0], buf[1]);
                        else if (0 < buf.Length)
                            mDataTable.Add(buf[0], "");
                        if (2 < buf.Length)
                            mCommentTable.Add(buf[0], buf[2]);
                    }
                }
                dataFile.Close();
            }
        }
    }
}
