using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// ファイル/Webアドレス管理クラス
    /// </summary>
    class FileManageData
    {
        public string mTitle = "";              //  タイトル
        public string mComment = "";            //  コメント
        public string mFilePath = "";           //  ファイルパス/Webアドレス
        public string mEncode = "UTF8";         //  エンコード(UTF8/SJIS/EUC)
        public string mReference = "";          //  参照パス/Webアドレス
        public string mSeperateType = "CSV";    //  区切り記号CSV(カンマ)/TSV(タブ)

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="address">ファイルパス/Webアドレス</param>
        public FileManageData(string title, string address)
        {
            mTitle = title;
            mFilePath = address;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="addressData">アドレスデータ</param>
        public FileManageData(string addressData)
        {
            setString(addressData);
        }

        /// <summary>
        /// ファイルパス/Webアドレスリストの取得(複数ファイル)
        /// </summary>
        /// <returns>ファイルリスト</returns>
        public List<string> getFileNames()
        {
            string[] fileNames = mFilePath.Split(',');
            char sp = mFilePath.IndexOf("http") == 0 ? '/' : '\\';
            string basePath = mFilePath.Substring(0, mFilePath.LastIndexOf(sp) + 1);
            List<string> addressList = new List<string>();
            addressList.Add(fileNames[0]);
            for (int i = 1; i < fileNames.Length; i++)
                addressList.Add(basePath + fileNames[i]);

            return addressList;
        }

        /// <summary>
        /// 文字列データ(';'セパレータ)をアドレスデータに設定する
        /// </summary>
        /// <param name="path">文字列データ</param>
        public void setString(string path)
        {
            string[] data = path.Split(';');
            int commentCount = 0;
            for (int i = 0; i < data.Length; i++) {
                if (2 < data[i].Length && data[i].IndexOf("$$") == 0) {
                    mReference = data[i].Substring(2).Trim();       //  参照パス
                } else if (1 < data[i].Length && data[i][0] == '$') {
                    if (commentCount == 0)
                        mTitle = data[i].Substring(1).Trim();       //  タイトル
                    else
                        mComment = data[i].Substring(1).Trim();     //  コメント
                    commentCount++;
                } else if (2 < data[i].Length && data[i].IndexOf("#%") == 0) {
                    mSeperateType = data[i].Substring(2).Trim();    //  区切り記号
                } else if (1 < data[i].Length && data[i][0] == '#') {
                    mEncode = data[i].Substring(1).Trim();          //  エンコード
                } else {
                    mFilePath = data[i];                             //  ファイルパス/Webアドレス
                }
            }
            if (mTitle.Length == 0) {
                //  タイトルがなかった場合、ファイル名をタイトルに設定
                if (mFilePath.IndexOf("http") == 0)
                    mTitle = mFilePath.Substring(mFilePath.LastIndexOf('/'));
                else
                    mTitle = Path.GetFileNameWithoutExtension(mFilePath);
            }
        }

        /// <summary>
        /// ファイルデータを文字列で取得
        /// </summary>
        /// <returns></returns>
        public string getString()
        {
            string buf = "";
            if (0 < mTitle.Length) buf += "$" + mTitle + ";";
            if (0 < mComment.Length) buf += "$" + mComment + ";";
            if (0 < mEncode.Length) buf += "#" + mEncode + ";";
            if (0 < mReference.Length) buf += "$$" + mReference + ";";
            if (0 < mSeperateType.Length) buf += "#%" + mSeperateType + ";";
            buf += mFilePath;
            return buf;
        }
    }

    /// <summary>
    /// SpreadSheet.xaml の相互作用ロジック
    /// </summary>
    public partial class SpreadSheet : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private string mFileTitle;          //  データファイルタイトル
        private string mFileListPath = "SpreadSheetFileList.csv";       //  データファイルパスリストのパス
        private int mFileListMax = 50;      //  データファイルリストの最大値
        private string mAppFolder;          //  アプリフォルダ
        private SheetData mData;            //  表示データ
        private SheetData mFileData;        //  ファイルデータ
        private List<SheetData> mDataList;  //  データリスト(履歴)
        private int mEncordType = 0;        //  文字コード 0: UTF8 1:Shift-Jis 2:Euc-Jp
        private string[] mEncodeTitle = new string[] { "UTF8", "SJIS", "EUC" };
        private List<FileManageData> mFileManageList;   //  データファイル管理リスト
        private string mConvFileFolder = "ConvDic";     //  データ置換えファイルフォルダ
        private string mDownLoadFolder = "DownLoad";    //  ダアンロードファイルの保存パス

        //  関数の配列化にデリゲートを使用
        class iClassFuc
        {
            public delegate void iFunction();
            public iFunction iFun;
            public string Title;
            public iClassFuc(iFunction fun, string title)
            {
                iFun = fun;
                Title = title;
            }
        }

        private List<iClassFuc> mFuncList;  //  操作関数リスト

        /// <summary>
        /// 操作関数のリストデータを設定
        /// </summary>
        private void setFuncList()
        {
            mFuncList = new List<iClassFuc>();
            mFuncList.Add(new iClassFuc(setTitleLine,       "タイトル行の設定"));
            mFuncList.Add(new iClassFuc(convPivot,          "集計処理"));
            mFuncList.Add(new iClassFuc(convDoubleData,     "数値化処理"));
            mFuncList.Add(new iClassFuc(zen2hanData,        "全角数値を半角に変換"));
            mFuncList.Add(new iClassFuc(transposeMatrix,    "縦横反転"));
            mFuncList.Add(new iClassFuc(accumulateData,     "増分⇒累積"));
            mFuncList.Add(new iClassFuc(defferntialData,    "累積⇒増分"));
            mFuncList.Add(new iClassFuc(sumData,            "合計値追加"));
            mFuncList.Add(new iClassFuc(verticalSumData,    "縦方向合計値"));
            //mFuncList.Add(new iClassFuc(addColumnDatas,     "[集計系列～集計項目の和]を追加"));
            //mFuncList.Add(new iClassFuc(multiColumnDatas,   "[集計系列～集計項目の積]を追加"));
            //mFuncList.Add(new iClassFuc(rateData,           "[集計系列]のセルの割合を追加"));
            //mFuncList.Add(new iClassFuc(calcColumnData,     "[集計系列]の演算処理"));
            mFuncList.Add(new iClassFuc(calcExpressData,    "行単位の数式処理"));
            mFuncList.Add(new iClassFuc(dateConvert,        "[集計系列]の日付を変換"));
            //mFuncList.Add(new iClassFuc(addColumnData,      "[集計系列 + 集計項目]を追加"));
            //mFuncList.Add(new iClassFuc(subColumnData,      "[集計系列 - 集計項目]を追加"));
            //mFuncList.Add(new iClassFuc(multiColumnData,    "[集計系列 * 集計項目]を追加"));
            //mFuncList.Add(new iClassFuc(divideColumnData,   "[集計系列 / 集計項目]を追加"));
            mFuncList.Add(new iClassFuc(dateConvertAdd,     "[集計系列]の日付を変換して追加"));
            //mFuncList.Add(new iClassFuc(dateYearConvert,    "[集計系列]を年単位にして追加"));
            //mFuncList.Add(new iClassFuc(dateMonthConvert,   "[集計系列]を月単位にして追加"));
            //mFuncList.Add(new iClassFuc(dateWeekConvert,    "[集計系列]を週単位にして追加"));
            //mFuncList.Add(new iClassFuc(dateWeekdayConvert, "[集計系列]を曜日にして追加"));
            mFuncList.Add(new iClassFuc(yearCompareData,    "年比較データ作成"));
            mFuncList.Add(new iClassFuc(selectLineMerge,    "選択行の結合"));
            mFuncList.Add(new iClassFuc(selectLineNotRemove,"選択行以外を削除"));
            mFuncList.Add(new iClassFuc(selectLineRemove,   "選択行削除"));
            mFuncList.Add(new iClassFuc(removeDataColumn,   "指定列間を削除"));
            mFuncList.Add(new iClassFuc(removeDataColumnTake, "指定列(集計系列)まで削除"));
            mFuncList.Add(new iClassFuc(removeDataColumnSkip, "指定列(集計系列)以降を削除"));
            mFuncList.Add(new iClassFuc(moveDataColumn,     "集計系列(縦軸)を集計項目(横軸)に移動"));
            mFuncList.Add(new iClassFuc(copyDataColumn,     "指定列間を右端に複写"));
            mFuncList.Add(new iClassFuc(joinDataColumn,     "指定列間の文字結合"));
            mFuncList.Add(new iClassFuc(addDataColumn,      "指定列間の加算結合"));
            mFuncList.Add(new iClassFuc(renameTitle,        "タイトル名変更"));
            mFuncList.Add(new iClassFuc(fileMerge,          "ファイルマージ"));
            mFuncList.Add(new iClassFuc(dataFilter,         "指定列のデータでフィルタリング"));
            mFuncList.Add(new iClassFuc(dataConvert,         "データの置換え"));

            //  コンボボックスに関数リストを設定
            CbOperation.Items.Clear();
            for (int i = 0; i < mFuncList.Count; i++)
                CbOperation.Items.Add(mFuncList[i].Title);
        }

        private YLib ylib;

        public SpreadSheet()
        {
            InitializeComponent();

            mWindowWidth = SpreadSheetWindw.Width;
            mWindowHeight = SpreadSheetWindw.Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();

            ylib = new YLib();

            mAppFolder = ylib.getAppFolderPath();   //  アプリフォルダ
            mFileListPath = Path.Combine(mAppFolder, mFileListPath);
            mConvFileFolder = Path.Combine(mAppFolder, mConvFileFolder);
            ylib.setEncording(0);
            loadPathList(mFileListPath, true);      //  ファイルリストの読み込み
            setAddressTitle();
            CbAddressTitle.SelectedIndex = -1;
            ylib.setEncording(mEncordType);         //  ファイル読込の文字コードを設定
            CbEncode.ItemsSource = mEncodeTitle;
            CbEncode.SelectedIndex = mEncordType;

            //  操作の設定
            setFuncList();
            //  データリスト
            mDataList = new List<SheetData>();
        }

        /// <summary>
        /// Windowの形状が変更したときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpreadSheetWindw_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (WindowState != mWindowState &&
                WindowState == WindowState.Maximized) {
                mWindowWidth = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (WindowState != mWindowState ||
                mWindowWidth != SpreadSheetWindw.Width ||
                mWindowHeight != SpreadSheetWindw.Height) {
                mWindowWidth = SpreadSheetWindw.Width;
                mWindowHeight = SpreadSheetWindw.Height;
            }
            mWindowState = this.WindowState;

            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            CbAddressTitle.Width += dx;
            TbComment.Width += dx / 2.0;
            TbReference.Width += dx / 2.0;
            TbAddress.Width += dx;
            mPrevWindowWidth = mWindowWidth;
        }

        /// <summary>
        /// アプリをクローズするときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ylib.setEncording(0);
            savePathList(mFileListPath);
            WindowFormSave();
        }

        /// <summary>
        /// Windowのサイズと位置を復元
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.SpreadSheetWindowWidth < 100 || 
                Properties.Settings.Default.SpreadSheetWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.SpreadSheetWindowHeight) {
                Properties.Settings.Default.SpreadSheetWindowWidth = mWindowWidth;
                Properties.Settings.Default.SpreadSheetWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.SpreadSheetWindowTop;
                Left = Properties.Settings.Default.SpreadSheetWindowLeft;
                Width = Properties.Settings.Default.SpreadSheetWindowWidth;
                Height = Properties.Settings.Default.SpreadSheetWindowHeight;
                double dy = this.Height - mWindowHeight;
            }
        }

        /// <summary>
        /// Windowのサイズと位置を保存
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.SpreadSheetWindowTop = Top;
            Properties.Settings.Default.SpreadSheetWindowLeft = Left;
            Properties.Settings.Default.SpreadSheetWindowWidth = Width;
            Properties.Settings.Default.SpreadSheetWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// ファイルパス・コンボボックスをダブルクリックしたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addressTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// [参照]ダブルクリックで参照を開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbReference_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 < TbReference.Text.Length)
                System.Diagnostics.Process.Start(TbReference.Text.ToLower());
        }

        /// <summary>
        /// [パス/アドレス]ダブルクリック ファイル選択を開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbAddress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            List<string> exts = new List<string>() { "csv", "json", "ndjson" };
            List<string> path = ylib.fileSelect("", exts);
            if (0 < path.Count) {
                TbAddress.Text = path[0];
                for (int i = 1; i < path.Count; i++) {
                    TbAddress.Text += "," + Path.GetFileName(path[i]);
                }
            }
        }

        /// <summary>
        /// タイトルを変更した時、データを設定し直す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbAddressTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbAddressTitle.SelectedIndex < 0)
                return;
            FileManageData addressData = findAddressTitle(CbAddressTitle.Items[CbAddressTitle.SelectedIndex].ToString());
            if (addressData != null) {
                int n = Array.IndexOf(mEncodeTitle, addressData.mEncode);
                CbEncode.SelectedIndex = n < 0  ? 0 : n;
                TbComment.Text = addressData.mComment;
                TbReference.Text = addressData.mReference;
                TbAddress.Text = addressData.mFilePath;
                CbTsv.IsChecked = addressData.mSeperateType.CompareTo("TSV") == 0;
            }
        }

        /// <summary>
        /// [読込]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void executeBtn_Click(object sender, RoutedEventArgs e)
        {
            executeFileRead();
        }

        /// <summary>
        /// 処理を選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int opration = CbOperation.SelectedIndex;
            if (0 <= opration && opration < mFuncList.Count && 
                mData != null && 0 < mData.getDataSize())
                mFuncList[opration].iFun();
            CbOperation.SelectedIndex = -1;
        }

        /// <summary>
        /// [戻す]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            setPreData();                   //  一つ前の状態に戻す
            CbOperation.SelectedIndex = -1;
        }

        /// <summary>
        /// [グラフ化]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtGraph_Click(object sender, RoutedEventArgs e)
        {
            if (mData == null)
                return;
            TableGraph graph = new TableGraph();
            graph.mWindowTitle = mFileTitle;
            graph.mSheetData = mData;
            graph.Show();
        }

        /// <summary>
        /// [CSV出力]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCsvOut_Click(object sender, RoutedEventArgs e)
        {
            string path = ylib.saveFileSelect("", "csv");
            if (0 < path.Length) {
                saveDataFile(path, mData);
            }
        }

        /// <summary>
        /// [読込]右ボタンのメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtEncordTypeMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
           if (menuItem.Name.CompareTo("removeFileMenu") == 0) {
                removeFileCommand();
            } else if (menuItem.Name.CompareTo("referenceFileMenu") == 0) {
                if (0 < TbReference.Text.Length)
                    System.Diagnostics.Process.Start(TbReference.Text.ToLower());
            } else if (menuItem.Name.CompareTo("downloadFileMenu") == 0) {
                executeFileRead(false);
            } else if (menuItem.Name.CompareTo("clearFileMenu") == 0) {
                clearDispData();
            } else if (menuItem.Name.CompareTo("registFileMenu") == 0) {
                setFileManageData();
            } else
                return;
        }

        /// <summary>
        /// [?]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            string[] pdfHelpFile = { "表・グラフ化ツール.pdf", "表・グラフ作成事例.pdf" };
            ylib.fileExecute(pdfHelpFile[0]);
            //HelpView help = new HelpView();
            //help.mHelpText = HelpText.mSpreadSheetHelp;
            //help.mPdfFile = pdfHelpFile;
            //help.Show();
        }

        /// <summary>
        /// [コンテキストメニュー]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("SelectCopyMenu") == 0) {
                data2ClipBoard();
            } else if (menuItem.Name.CompareTo("SelectDeleteMenu") == 0) {
                selectLineRemove();
            } else if (menuItem.Name.CompareTo("NotSelectDeleteMenu") == 0) {
                selectLineNotRemove();
            }
        }

        /// <summary>
        /// [ソート]データグリッドのソートカスタマイズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataSheet_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;   //  既存ソートを無効にする
            var sortDir = e.Column.SortDirection;   //  ソートの向き
            if (ListSortDirection.Ascending != sortDir)
                sortDir = ListSortDirection.Ascending;
            else
                sortDir = ListSortDirection.Descending;

            int n = 0;
            int end = e.Column.SortMemberPath.IndexOf(']');
            if (end < 2 || int.TryParse(e.Column.SortMemberPath.Substring(1, end - 1), out n) != true)
                return;
            if (ListSortDirection.Ascending == sortDir) {
                mData.Sort(n, true);
            } else {
                mData.Sort(n, false);
            }

            setData(mData.getData());

            foreach (var column in DgDataSheet.Columns) {
                if (column.SortMemberPath == e.Column.SortMemberPath) {
                    column.SortDirection = sortDir;
                }
            }
        }

        /// <summary>
        /// [セルのダブルクリック]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDataSheet_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgDataSheet.SelectedItem != null) {
                //  選択したセルの行・列No
                var row = DgDataSheet.SelectedIndex;
                var col = DgDataSheet.CurrentColumn.DisplayIndex;
                //MessageBox.Show("" + row + " " + col + " " + mData.getData()[row][col]);
                //  選択したセル内容の変更
                InputBox dlg = new InputBox();
                dlg.mEditText = mData.getData()[row][col];
                if (dlg.ShowDialog() == true) {
                    SheetData newData = mData.changeCellData(row, col, dlg.mEditText);
                    if (newData != null) {
                        mDataList.Add(newData);                 //  変換したデータを履歴に登録
                        setDataDisp(mDataList[mDataList.Count - 1]);
                    }
                }

            }
        }

        /// <summary>
        /// 選択行をタイトル行にする
        /// </summary>
        private void setTitleLine()
        {
            int titleLine = DgDataSheet.SelectedIndex;      //  選択行
            if (0 <= titleLine) {
                SheetData newData = mData.ChangeTitleLine(titleLine);
                if (newData != null) {
                    mDataList.Add(newData);                 //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 数値データに変換する
        /// </summary>
        private void convDoubleData()
        {
            int m = CbComboYList.SelectedIndex;         //  集計系列(縦軸)  開始列
            int n = CbComboXList.SelectedIndex;         //  集計項目(横軸)  終了列
            if (n < m)
                YLib.Swap(ref m, ref n);
            SheetData doubleData = mData.DoublDataTable(m, n);
            if (doubleData != null) {
                mDataList.Add(doubleData);              //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 全角数値を半角数値に変換
        /// </summary>
        private void zen2hanData()
        {
            SheetData hanData = mData.zen2hanData();
            if (hanData != null) {
                mDataList.Add(hanData);              //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 集計処理(指定列のデータをカウントしていく)
        /// </summary>
        private void convPivot()
        {
            if (0 < CbComboXList.Text.Length && 0 < CbComboYList.Text.Length) {
                // 集計系列と集計項目、集計データ値項目で集計する
                SheetData pivotData = mData.PivotTable(CbComboXList.Text, CbComboYList.Text, CbComboDataList.Text);
                if (pivotData != null) {
                    mDataList.Add(pivotData);               //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            } else if (0 == CbComboXList.Text.Length && 0 == CbComboYList.Text.Length) {
                //  パラメータの指定なし
                //  縦方向に左端データでまとめる(数値データを集計する)
                SheetData squeezeData = mData.SqueezeTable();
                if (squeezeData != null) {
                    mDataList.Add(squeezeData);           //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            } else if (0 < CbComboXList.Text.Length && 0 < CbComboYList.Text.Length && 0 < CbComboDataList.Text.Length) {

            }
        }

        /// <summary>
        /// 行と列を反転する
        /// </summary>
        private void transposeMatrix()
        {
            SheetData transposeData = mData.TransposeMatrix();
            if (transposeData != null) {
                mDataList.Add(transposeData);           //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 累積データを増分データに変換する
        /// 列指定がない時は全列、終了列がない時は解列のみ変換
        /// </summary>
        private void defferntialData()
        {
            int n = CbComboYList.SelectedIndex;         //  集計系列(縦軸)  開始列
            int m = CbComboXList.SelectedIndex;         //  集計項目(横軸)  終了列
            m = 0 <= m ? m : n;
            SheetData deffData = mData.DifferntialData(n, m);
            if (deffData != null) {
                mDataList.Add(deffData);                //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 増分データを累積データに変換する
        /// 列指定がない時は全列、終了列がない時は解列のみ変換
        /// </summary>
        private void accumulateData()
        {
            int n = CbComboYList.SelectedIndex;         //  集計系列(縦軸)  開始列
            int m = CbComboXList.SelectedIndex;         //  集計項目(横軸)  終了列
            m = 0 <= m ? m : n;
            SheetData accumuData = mData.AccumulateData(n, m);
            if (accumuData != null) {
                mDataList.Add(accumuData);              //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 行の合計を求める
        /// 開始列から終了列間の合計をもとめる
        /// 指定がない時は全列を対象とする
        /// </summary>
        private void sumData()
        {
            int n = CbComboYList.SelectedIndex;         //  集計系列(縦軸)　開始列
            int m = CbComboXList.SelectedIndex;         //  集計項目(横軸)  終了列
            SheetData sumData = mData.SumData(n, m);
            if (sumData != null) {
                mDataList.Add(sumData);                 //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 縦方向に合計値を求める
        /// </summary>
        private void verticalSumData()
        {
            int l = DgDataSheet.SelectedIndex;          //  選択行(合計値記入行、省略時は最終行に追加)
            int n = CbComboYList.SelectedIndex;         //  集計系列(縦軸)　開始列 省略時は全体
            int m = CbComboXList.SelectedIndex;         //  集計項目(横軸)  終了列 省略時は開始列のみ
            SheetData sumData = mData.VerticalSumData(n, m, l);
            if (sumData != null) {
                mDataList.Add(sumData);                 //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }


        /// <summary>
        /// 指定列のセルの割合を列末に追加
        /// </summary>
        private void rateData()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                SheetData rateData = mData.RateData(n);
                if (rateData != null) {
                    mDataList.Add(rateData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// [集計系列 + 集計項目列]を列末に追加
        /// </summary>
        private void addColumnData()
        {
            calcColumnsData('+');
        }

        /// <summary>
        /// [集計系列～集計項目列の和]を列末に追加
        /// </summary>
        private void addColumnDatas()
        {
            calcColumnsData('+', true);
        }

        /// <summary>
        /// [集計系列 - 集計項目列]を列末に追加
        /// </summary>
        private void subColumnData()
        {
            calcColumnsData('-');
        }

        /// <summary>
        /// [集計系列 * 集計項目列]を列末に追加
        /// </summary>
        private void multiColumnData()
        {
            calcColumnsData('*');
        }

        /// <summary>
        /// [集計系列～集計項目列の積]を列末に追加
        /// </summary>
        private void multiColumnDatas()
        {
            calcColumnsData('*', true);
        }

        /// <summary>
        /// [集計系列 / 集計項目列]を列末に追加
        /// </summary>
        private void divideColumnData()
        {
            calcColumnsData('/');
        }

        /// <summary>
        /// 集計系列と集計項目列の演算を行って列末に追加
        /// </summary>
        /// <param name="calcType">演算の種別</param>
        private void calcColumnsData(char calcType, bool multi = false)
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                int m = CbComboXList.SelectedIndex;
                SheetData calcData;
                if (multi)
                    calcData = mData.CalcDatas(n, m, calcType);
                else
                    calcData = mData.CalcData(n, m, calcType);
                if (calcData != null) {
                    mDataList.Add(calcData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 指定列に対して演算処理を行う
        /// 演算の種類と係数はダイヤログで指定
        /// </summary>
        private void calcColumnData()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                SpeadCalcInput dlg = new SpeadCalcInput();
                if (dlg.ShowDialog() == false)
                    return;
                double val = dlg.mInputVal;
                char calcType = dlg.mCalcType;
                SheetData calcData = mData.CalcData(n, val, calcType);
                if (calcData != null) {
                    mDataList.Add(calcData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 数式を使って演算処理を行う
        /// [Column:ColumnTitle:RelativeRow]を引数として処理する
        /// </summary>
        private void calcExpressData()
        {
            CalcExpressInput dlg = new CalcExpressInput();
            dlg.mColumnTitles = mData.getTitle();
            dlg.mFunctions = YCalc.mFuncList;
            if (dlg.ShowDialog() == false)
                return;
            SheetData calcData = mData.CalcExpressData(dlg.CbExpress.Text, dlg.TbTitle.Text);
            if (calcData != null) {
                mDataList.Add(calcData);              //  変換したデータを履歴に登録
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }

        /// <summary>
        /// 日付変換
        /// 日付を年、月、週単位に変換する
        /// </summary>
        private void dateConvert()
        {
            if (0 < CbComboYList.Text.Length) {
                int dateType = 0;
                SelectMenu selMenu = new SelectMenu();
                selMenu.Title = "日付変換を選択";
                string[] menuList = { "年単位", "月単位", "週単位", "yyyy/mm/dd" };
                selMenu.mMenuList = menuList;
                if (selMenu.ShowDialog() == true) {
                    dateType = Array.IndexOf(menuList, selMenu.mSelectItem);
                }
                int n = CbComboYList.SelectedIndex;         //  日付の列
                SheetData convertDate = mData.DateConvert(n, dateType);
                if (convertDate != null) {
                    mDataList.Add(convertDate);             //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                } else if (mData.getError()) {
                    MessageBox.Show(mData.getErrorMessage());
                }
            }
        }

        /// <summary>
        /// 日付を変換して行末に追加
        /// </summary>
        private void dateConvertAdd()
        {
            if (0 < CbComboYList.Text.Length) {
                int dateType = 0;
                SelectMenu selMenu = new SelectMenu();
                selMenu.Title = "日付を変換して行末に追加";
                string[] menuList = { "年単位", "月単位", "週単位", "曜日" };
                selMenu.mMenuList = menuList;
                if (selMenu.ShowDialog() == true) {
                    dateType = Array.IndexOf(menuList, selMenu.mSelectItem);
                    dateConvert(dateType);
                }
            }
        }

        /// <summary>
        /// 日付の指定列を年単位に変換して最後尾列に追加
        /// </summary>
        private void dateYearConvert()
        {
            dateConvert(0);
        }

        /// <summary>
        /// 日付の指定列を月単位に変換して最後尾列に追加
        /// </summary>
        private void dateMonthConvert()
        {
            dateConvert(1);
        }

        /// <summary>
        /// 日付の指定列を週単位に変換して最後尾列に追加
        /// </summary>
        private void dateWeekConvert()
        {
            dateConvert(2);
        }

        /// <summary>
        /// 日付の指定列を曜日に変換して最後尾列に追加
        /// </summary>
        private void dateWeekdayConvert()
        {
            dateConvert(3);
        }

        /// <summary>
        /// 日付の指定列を年単位/月単位/週単位/曜日に変換して最後尾列に追加
        /// 変換種別 0:年単位  1:月単位  2:週単位  3:曜日
        /// </summary>
        /// <param name="convertType"></param>
        private void dateConvert(int convertType)
        {
            if (0 < CbComboYList.Text.Length) {
                int offset = 0;
                if (convertType == 2) {
                    SelectMenu selMenu = new SelectMenu();
                    selMenu.Title = "週の開始曜日を選択";
                    string[] menuList = { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" };
                    selMenu.mMenuList = menuList;
                    if (selMenu.ShowDialog() == true) {
                        offset = Array.IndexOf(menuList, selMenu.mSelectItem);
                    }
                }
                int n = CbComboYList.SelectedIndex;         //  日付の列
                SheetData convertDate = mData.DateConvert(n, convertType, offset);
                if (convertDate != null) {
                    mDataList.Add(convertDate);             //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                } else if (mData.getError()) {
                    MessageBox.Show(mData.getErrorMessage());
                }
            }
        }

        /// <summary>
        /// 指定列(集計系列)削除
        /// </summary>
        private void removeDataColumn()
        {
            removeDataColumn(0);
        }

        /// <summary>
        /// 指定列(集計系列)まで削除
        /// </summary>
        private void removeDataColumnTake()
        {
            removeDataColumn(1);
        }

        /// <summary>
        /// 指定列間を削除
        /// </summary>
        private void removeDataColumnTakeWhile()
        {
            removeDataColumn(2);
        }

        /// <summary>
        /// 指定列(集計系列)以降を削除
        /// </summary>
        private void removeDataColumnSkip()
        {
            removeDataColumn(3);
        }

        /// <summary>
        /// コンボボックスの指定列を削除する
        /// type = 0 : 指定列でだけ削除  1: 指定列まで削除 2 : 指定列間を削除 3: 指定列以降を削除
        /// </summary>
        /// <param name="type"></param>
        private void removeDataColumn(int type)
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                int m = 0 <= CbComboXList.SelectedIndex ? CbComboXList.SelectedIndex : n;
                if (type == 2 && (m < 0 || mData.getTitleSize() < m))
                    return;
                int st = 0, en = 0;
                if (type == 0) {
                    st = n;
                    en = m;
                } else if (type == 1) {
                    st = 0;
                    en = n;
                } else if (type == 2) {
                    st = n;
                    en = m;
                } else if (type == 3) {
                    st = n;
                    en = mData.getTitleSize() - 1;
                } else
                    return;
                SheetData removeData = mData.RemoveDataColumn(st, en);
                if (removeData != null) {
                    mDataList.Add(removeData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 縦軸コンボボックスで指定した列とその右の列を結合する
        /// 結合には'-'を間に入れる
        /// </summary>
        private void joinDataColumn()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                int m = CbComboXList.SelectedIndex;
                m = m < n ? 0 : m - n;
                SheetData joinData = mData.CombineData(n, m, false);
                if (joinData != null) {
                    mDataList.Add(joinData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 指定列と次の列を加算して結合
        /// </summary>
        private void addDataColumn()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                int m = CbComboXList.SelectedIndex;
                if (n == m)
                    return;
                if (m < n)
                    YLib.Swap(ref n, ref m);
                m = m - n;
                if (mData.getDataType(n) == SheetData.DATATYPE.NUMBER && mData.getDataType(n + 1) == SheetData.DATATYPE.NUMBER) {
                    SheetData addData = mData.CombineData(n, m, true);
                    if (addData != null) {
                        mDataList.Add(addData);              //  変換したデータを履歴に登録
                        setDataDisp(mDataList[mDataList.Count - 1]);
                    }
                } else {
                    MessageBox.Show("数値データではないため加算結合できません", "エラーメッセージ");
                }
            }
        }

        /// <summary>
        /// 指定列を他の列に移動する
        /// </summary>
        private void moveDataColumn()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;         //  移動前の列
                int m = CbComboXList.SelectedIndex;         //  移動後の列
                SheetData moveData = mData.MoveData(n, m);
                if (moveData != null) {
                    mDataList.Add(moveData);              //  移動したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 指定列間のデータを右端にコピー
        /// </summary>
        private void copyDataColumn()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;         //  開始列
                int m = CbComboXList.SelectedIndex;         //  終了列
                SheetData addData = mData.CopyData(n, m);
                if (addData != null) {
                    mDataList.Add(addData);              //  変換したデータを履歴に登録
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// ファイルデータを読み込んでマージする
        /// </summary>
        private void fileMerge()
        {
            string path = ylib.fileSelect("", "csv");
            fileMerge(path, CbComboYList.SelectedIndex);
        }

        /// <summary>
        /// 既存のデータに新たなファイルデータを読み込んで結合する
        /// キー列を指定した場合はマージするファイルの最初の列と合わせる
        /// 指定しない場合は一致するタイトルを検索してキー列とする
        /// </summary>
        /// <param name="path">ファイルパスまたはWebアドレス</param>
        /// <param name="titleCol">マージのキータイトル列(省略可)</param>
        /// <returns>結果</returns>
        private bool fileMerge(string path, int titleCol = -1)
        {
            if (0 < path.Length) {
                setEncodeType(CbEncode.Text);
                SheetData sheetData = loadDataFile(path);
                if (sheetData != null) {
                    (int srcCol, int destCol) = mData.titleSearch(sheetData, titleCol);
                    if (srcCol < 0) {
                        MessageBox.Show(mData.getErrorMessage(), "共通タイトルが見つかりませんでした");
                        return false;
                    } else if (destCol < 0) {
                        SelectMenu selMenu = new SelectMenu();
                        selMenu.Title = "共有タイトルの選択";
                        string[] menuList = sheetData.getTitle().InsertTop("");
                        selMenu.mMenuList = menuList;
                        if (selMenu.ShowDialog() == true) {
                            destCol = Array.IndexOf(menuList, selMenu.mSelectItem) - 1;
                        } else {
                            return false;
                        }
                    }

                    SheetData mergeData = mData.MergeData(sheetData, titleCol, destCol);
                    if (mergeData != null) {
                        mDataList.Add(mergeData);              //  変換したデータを履歴に登録
                        setDataDisp(mDataList[mDataList.Count - 1]);
                    } else {
                        if (!sheetData.getError()) {
                            MessageBox.Show(mData.getErrorMessage(), "エラーメッセージ");
                            return false;
                        }
                    }
                } else {
                    MessageBox.Show("ファイルが読み込めませんでした", "エラーメッセージ");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 選択行以外を削除
        /// </summary>
        private void selectLineNotRemove()
        {
            IList selItems = DgDataSheet.SelectedItems;
            int[] selItemsNo = new int[selItems.Count];
            int i = 0;
            foreach (string[] data in selItems) {
                selItemsNo[i++] = mData.indexOfData(data);
            }
            if (0 < selItemsNo.Length) {
                SheetData sheetData = mData.NotRemoveDataTable(selItemsNo);
                if (sheetData != null) {
                    mDataList.Add(sheetData);
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 選択行を削除
        /// </summary>
        private void selectLineRemove()
        {
            IList selItems = DgDataSheet.SelectedItems;
            int[] selItemsNo = new int[selItems.Count];
            int i = 0;
            foreach (string[] data in selItems) {
                selItemsNo[i++] = mData.indexOfData(data);
            }
            if (0 < selItemsNo.Length) {
                SheetData sheetData = mData.RemoveDataTable(selItemsNo);
                if (sheetData != null) {
                    mDataList.Add(sheetData);
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 年ごとに比較できるようにデータを組み直す
        /// </summary>
        private void yearCompareData()
        {
            SheetData sheetData = mData.yearCompareData();
            if (sheetData != null) {
                mDataList.Add(sheetData);
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
        }


        /// <summary>
        /// 選択行と次の行をマージする
        /// </summary>
        private void selectLineMerge()
        {
            int n = DgDataSheet.SelectedIndex;
            IList selItems = DgDataSheet.SelectedItems;
            int m = selItems.Count;
            if (0 <= n && 0 < m) {
                SheetData sheetData = mData.MergeNextLine(n, m - 1);
                if (sheetData != null) {
                    mDataList.Add(sheetData);
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }

        /// <summary>
        /// 選択列のタイトルを変更する
        /// </summary>
        private void renameTitle()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;
                InputBox dlg = new InputBox();
                dlg.mEditText = mData.getTitle()[n];
                if (dlg.ShowDialog() == true) {
                    SheetData sheetData = mData.changeTitleData(n, dlg.mEditText);
                    if (sheetData != null) {
                        mDataList.Add(sheetData);
                        setDataDisp(mDataList[mDataList.Count - 1]);
                    }
                }
            }
        }


        /// <summary>
        /// 指定列のデータでフィルタリングする
        /// </summary>
        private void dataFilter()
        {
            if (0 < CbComboYList.Text.Length) {
                int n = CbComboYList.SelectedIndex;         //  フィルタリングする列
                //  フィルタリングする列のデータの取得
                List<string> colDataList = mData.getColDataList(n);
                FilterList filterList = new FilterList();
                foreach (string data in colDataList) {
                    filterList.mFilterList.Add(new CheckBoxListItem(true, data));
                }
                //  フィルタリングするデータの選択
                var result = filterList.ShowDialog();
                if (result == true) {
                    //  フィルタリングの実施反映
                    colDataList.Clear();
                    foreach (CheckBoxListItem item in filterList.mFilterList) {
                        if (item.Checked)
                            colDataList.Add(item.Text);
                    }
                    SheetData sheetData = mData.filteringData(n, colDataList);
                    if (sheetData != null) {
                        mDataList.Add(sheetData);
                        setDataDisp(mDataList[mDataList.Count - 1]);
                    }
                }
            }
        }

        /// <summary>
        /// タイトルまたは指定列のデータを変換辞書に従って変換する
        /// 変換列の指定がない時はタイトルを変換する
        /// </summary>
        private void dataConvert()
        {
            //string filePath = ylib.fileSelect("", "csv");
            ConvFileSelect dlg = new ConvFileSelect();
            dlg.mFolder = mConvFileFolder;
            if (dlg.ShowDialog() == true) {
                string filePath = dlg.mSelectFile;
                if (filePath.Length == 0)
                    return;
                int n = CbComboYList.SelectedIndex;         //  変換対象列
                SheetData sheetData = mData.dataConvert(filePath, n);
                if (sheetData != null) {
                    mDataList.Add(sheetData);
                    setDataDisp(mDataList[mDataList.Count - 1]);
                }
            }
        }


        /// <summary>
        /// 一つ前の状態に戻す
        /// </summary>
        private void setPreData()
        {
            if (1 < mDataList.Count) {
                setDataDisp(mDataList[mDataList.Count - 2]);
                mDataList.RemoveAt(mDataList.Count - 1);
            }
        }

        /// <summary>
        /// データをデータグリッドに設定
        /// </summary>
        /// <param name="data">リストデータ</param>
        private void setData(List<string[]> data)
        {
            //  データを再設定
            DgDataSheet.Items.Clear();
            for (int i = 0; i < data.Count; i++) {
                DgDataSheet.Items.Add(data[i]);
            }
        }

        /// <summary>
        /// リストのタイトルを設定
        /// </summary>
        private void setTitle(string[] title)
        {
            DgDataSheet.Columns.Clear();
            for (int i = 0; i < title.Length; i++) {
                var column = new DataGridTextColumn();
                column.Header = title[i];
                column.Binding = new Binding($"[{i}]");     //  カラム名を設定 [n]
                //column.IsReadOnly = true;                  //  編集可にする
                DgDataSheet.Columns.Add(column);
            }
        }

        /// <summary>
        /// タイトルデータの設定
        /// </summary>
        /// <param name="titles"></param>
        private void setAxisTitle(string[] titles, SheetData.DATATYPE[] dataType)
        {
            CbComboXList.Items.Clear();
            CbComboYList.Items.Clear();
            CbComboDataList.Items.Clear();
            for (int i = 0; i < titles.Length; i++) {
                CbComboXList.Items.Add(titles[i]);
                CbComboYList.Items.Add(titles[i]);
                if (dataType[i] == SheetData.DATATYPE.NUMBER)
                    CbComboDataList.Items.Add(titles[i]);
            }
        }

        /// <summary>
        /// 表の行数と列数をStatusBarに表示する
        /// </summary>
        private void setTableInfo()
        {
            TbRowInfo.Text = " 行数: " + mData.getDataSize().ToString("#,##0");
            TbColInfo.Text = " 列数: " + mData.getTitleSize().ToString("#,##0");
        }

        /// <summary>
        /// データファイルの読み込み
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param name="dataList">データリスト</param>
        /// <param name="titleList">タイトルリスト</param>
        /// <returns></returns>
        private SheetData loadDataFile(string path, bool tabSep = false)
        {
            if (!File.Exists(path))
                return null;
            List<string[]> dataList = null;
            if (Path.GetExtension(path).ToLower().CompareTo(".csv") == 0) {
                //  CSV形式
                dataList = ylib.loadCsvData(path, tabSep);
            } else if (Path.GetExtension(path).ToLower().CompareTo(".tsv") == 0) {
                //  TSV形式
                dataList = ylib.loadCsvData(path, true);
            } else if (Path.GetExtension(path).ToLower().CompareTo(".ndjson") == 0 ||
                Path.GetExtension(path).ToLower().CompareTo(".json") == 0) {
                //  JSON形式
                dataList = ylib.loadJsonData(path);
            }
            if (dataList == null || dataList.Count < 1) {
                if (ylib.getError())
                    MessageBox.Show(ylib.getErrorMessage(), "エラーメッセージ");
                return null;
            }
            //  1行目をタイトル行に設定
            string[] titleList = dataList[0];
            dataList.RemoveAt(0);
            SheetData fileData = new SheetData(dataList, titleList);
            return fileData;
        }

        /// <summary>
        /// データをCSV形式で保存する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileData"></param>
        private void saveDataFile(string path, SheetData fileData)
        {
            if (File.Exists(path))
                if (MessageBox.Show("ファイルが存在します。上書きしますか？", "確認", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            List<string[]> dataList = new List<string[]>();
            dataList.Add(fileData.getTitle());
            foreach (string[] data in fileData.getData()) {
                dataList.Add(data);
            }
            ylib.saveCsvData(path, dataList);
        }


        /// <summary>
        /// 表示されているデータでファイルの読込を行う
        /// </summary>
        /// <param name="download"></param>
        private void executeFileRead(bool download = true)
        {
            FileManageData fileManageData = setFileManageData();
            commandFileSet(fileManageData, download);
        }

        /// <summary>
        /// ファイル管理データの登録
        /// </summary>
        /// <returns>ファイル管理データ</returns>
        private FileManageData setFileManageData()
        {
            if (mFileManageList == null)
                mFileManageList = new List<FileManageData>();
            FileManageData fileManageData = getDispData();
            removeAddressData(fileManageData.mTitle);
            mFileManageList.Insert(0, fileManageData);
            setAddressTitle();
            CbAddressTitle.SelectedIndex = 0;
            return fileManageData;
        }

        /// <summary>
        /// アドレスデータからファイルを読み込む
        /// </summary>
        /// <param name="addressData">アドレスデータ</param>
        /// <param name="download">false時は既存ダウンロードデータを使う</param>
        private void commandFileSet(FileManageData addressData, bool download = true)
        {
            List<string> files = addressData.getFileNames();
            //  エンコード設定
            setEncodeType(addressData.mEncode);
            bool tabSep = CbTsv.IsChecked == true;  //  TSVデータ
            //  データファイル(csv/ndjson)の読込
            if (0 < files.Count) {
                mFileTitle = addressData.mTitle;
                if (fileRead(files[0], download, tabSep)) {
                    for (int i = 1; i < files.Count; i++) {
                        webFileAddRead(files[i], download);
                    }
                } else {
                    MessageBox.Show("ファイルが存在しないか、ダウンロードに失敗したと思われます");
                }
            }
        }

        /// <summary>
        /// ファイル読込時のエンコード設定
        /// </summary>
        /// <param name="encode">エンコード</param>
        private void setEncodeType(string encode)
        {
            mEncordType = Array.IndexOf(mEncodeTitle, encode);
            mEncordType = mEncordType < 0 ? 0 : mEncordType;
            ylib.setEncording(mEncordType);
        }

        /// <summary>
        /// Webアドレスのデータをダウンロードして既存のデータに結合する
        /// </summary>
        /// <param name="path">Webアドレス</param>
        /// <param name="download">false:ダウンロートしたファイルを使用(省略時はファイルをダウンロード)</param>
        /// <returns>結果</returns>
        private bool webFileAddRead(string path, bool download = true)
        {
            string filePath = fileDownLoad(path, download);
            if (filePath.Length < 1) {
                return false;
            }
            return fileMerge(filePath);
        }


        /// <summary>
        /// PC上またはWeb上のファイルを読み込んでデータをセットする
        /// ダウンロードなしにすると先にダウンロードした既存のファイルを使用
        /// 既存のファイルがない場合にはWebからダウンロードする
        /// </summary>
        /// <param name="path">ファイルパスまたはWebアドレス</param>
        /// <param name="download">ダウンロードの可否(省略可)</param>
        private bool fileRead(string path, bool download = true, bool tabSep = false)
        {
            //  Web上のファイルはダウンロードする
            string filePath = fileDownLoad(path, download);
            if (filePath.Length < 1 || !File.Exists(filePath))
                return false;

            // GZIPファイルであれば解凍する
            byte[] header = ylib.loadBinData(filePath, 2);
            if (header[0] == 0x1f && header[1] == 0x8b)     //  gzipファイルの確認
                ylib.gzipDecompress(filePath, filePath);    //  gzipファイルの解凍

            //  ファイルの読み込み
            TbRowInfo.Text = "データ読込中";
            TbColInfo.Text = "";
            ylib.DoEvents();
            mFileData = loadDataFile(filePath, tabSep);     //  CSV/NDJSONファイルからデータの取り込
            if (mFileData != null) {
                TbRowInfo.Text = "表データ作成中";
                ylib.DoEvents();
                mDataList.Add(mFileData.getSheetData());
                setDataDisp(mDataList[mDataList.Count - 1]);
            }
            return true;
        }

        /// <summary>
        /// ファイルパスがURLであればWebからダウンロードしてダウロードしたファイル名を返す
        /// URLでなければそのままファイル名を返す
        /// </summary>
        /// <param name="filePath">URL</param>
        /// <param name="download">ダウンロードフラグ(省略可)</param>
        /// <returns>ダウンロードしたファイル名</returns>
        private string  fileDownLoad(string filePath, bool download = true)
        {
            if (filePath.IndexOf("http://") == 0 || filePath.IndexOf("https://") == 0) {
                if (!Directory.Exists(mDownLoadFolder))
                    Directory.CreateDirectory(mDownLoadFolder);
                string downLoadFile = mDownLoadFolder + "\\" + filePath.Substring(filePath.LastIndexOf("/") + 1);
                if (download || !File.Exists(downLoadFile)) {
                    TbRowInfo.Text = "ダウンロード中";
                    TbColInfo.Text = "";
                    ylib.DoEvents();
                    if (!ylib.webFileDownload(filePath, downLoadFile)) {
                        MessageBox.Show("ファイルがダウンロードできませんでした\n" + ylib.getErrorMessage(), "エラー");
                        return "";
                    } else {
                        return downLoadFile;
                    }
                } else {
                    return downLoadFile;
                }
            } else {
                return filePath;
            }
        }

        /// <summary>
        /// 表データを表示データに設定する
        /// </summary>
        /// <param name="data"></param>
        private void setDataDisp(SheetData data)
        {
            mData = data;
            setTitle(mData.getTitle());                         //  タイトルデータを設定
            setData(mData.getData());                           //  シートデータを設定
            setAxisTitle(mData.getTitle(), mData.getDataType());//  列タイトルデータをコンボボックスに設定
            setTableInfo();                                     //  ステータスバーに表情報を表示
        }

        /// <summary>
        /// ファイル指定のコンボホックスに設定されているファイルパスまたはコマンド列を削除する
        /// </summary>
        private void removeFileCommand()
        {
            removeAddressData(CbAddressTitle.Text);
            setAddressTitle();
            CbAddressTitle.SelectedIndex = 0;
        }

        /// <summary>
        /// 入力データをクリアする
        /// </summary>
        private void clearDispData()
        {
            CbAddressTitle.Text = "";
            TbComment.Text = "";
            TbReference.Text = "";
            TbAddress.Text = "";
        }

        /// <summary>
        /// 表示データを AddressDataに設定して取得する
        /// </summary>
        /// <returns></returns>
        private FileManageData getDispData()
        {
            FileManageData addresData = new FileManageData(CbAddressTitle.Text, TbAddress.Text);
            addresData.mComment = TbComment.Text;
            addresData.mEncode = CbEncode.Text;
            addresData.mReference = TbReference.Text;
            addresData.mSeperateType = CbTsv.IsChecked == true ? "TSV" : "CSV";
            return addresData;
        }

        /// <summary>
        /// アドレスのタイトルを指定して AddressData を返す
        /// </summary>
        /// <param name="addressTitle">アドレスタイトル</param>
        /// <returns>アドレスデータ</returns>
        private FileManageData findAddressTitle(string addressTitle)
        {
            foreach (var address in mFileManageList) {
                if (address.mTitle.CompareTo(addressTitle) == 0)
                    return address;
            }
            return null;
        }


        /// <summary>
        /// アドレを指定して AddressData を返す
        /// </summary>
        /// <param name="addressTitle">アドレス</param>
        /// <returns>アドレスデータ</returns>
        private FileManageData findAddress(string address)
        {
            foreach (var addressData in mFileManageList) {
                if (addressData.mFilePath.CompareTo(address) == 0)
                    return addressData;
            }
            return null;
        }

        /// <summary>
        /// アドレスデータを追加する゜
        /// </summary>
        /// <param name="addressData"></param>
        private void addAddressData(FileManageData addressData)
        {
            if (findAddressTitle(addressData.mTitle) != null)
                removeAddressData(addressData.mTitle);
            mFileManageList.Insert(0, addressData);
        }

        /// <summary>
        /// アドレスタイトルの AddressDataを削除する
        /// </summary>
        /// <param name="title"></param>
        private void removeAddressData(string title)
        {
            FileManageData addressData = findAddressTitle(title);
            if (addressData != null)
                mFileManageList.Remove(addressData);
        }

        /// <summary>
        /// パス/Webアドレスデータのタイトルをコンボボックスに設定
        /// </summary>
        private void setAddressTitle()
        {
            if (mFileManageList != null) {
                CbAddressTitle.Items.Clear();
                foreach (FileManageData data in mFileManageList)
                    CbAddressTitle.Items.Add(data.mTitle);
            }
        }

        /// <summary>
        /// データファイルパスリストの取得
        /// この時点でファイルが存在しない時は登録しない
        /// </summary>
        /// <param name="path">保存ファイル名</param>
        private void loadPathList(string path, bool order = false)
        {
            List<string> list = ylib.loadListData(path);
            if (list != null) {
                if (mFileManageList == null) {
                    mFileManageList = new List<FileManageData>();
                } else {
                    mFileManageList.Clear();
                }
                foreach (var command in list) {
                    mFileManageList.Add(new FileManageData(command));
                }
            }
        }

        /// <summary>
        /// データファイルパスリストの保存
        /// </summary>
        /// <param name="path">保存ファイルパス</param>
        private void savePathList(string path)
        {
            List<string> list = new List<string>();
            int n = 0;
            foreach (var address in mFileManageList) {
                if (0 < address.mTitle.Length) {
                    list.Add(address.getString());
                    if (mFileListMax < n++)
                        break;
                }
            }
            ylib.saveListData(path, list);
        }

        /// <summary>
        /// データをクリップボードにコピーする
        /// </summary>
        private void data2ClipBoard()
        {
            string buf = "";
            //  タイトル行
            for (int i=0; i< DgDataSheet.Columns.Count; i++) {
                buf += "\""+DgDataSheet.Columns[i].Header.ToString()+"\",";
            }
            buf += "\n";
            IList selItems = DgDataSheet.SelectedItems;
            if (0 < selItems.Count) {
                foreach (string[] data in selItems) {
                    buf += ylib.array2csvString(data) + "\n";
                }
                Clipboard.SetText(buf);
            }
        }
    }
}
