﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// FuncPlot.xaml の相互作用ロジック
    /// 
    /// 計算式をグラフ表示する
    /// FuncPlot.xaml の相互作用ロジック
    /// 
    /// 与えられた関数でグラフの描画を行う。
    /// 1.一般的な一次元の方程式(直交座標) y = f(x)
    /// 2.媒介変数(パラメトリック)を使った式 x = f(t); y = g(t)
    /// 3.極方程式(極座標) r = f(t)
    /// 記述例: $複数関数;f1([x]);f2([x]);f3([x]);$コメント
    ///         $変数指定;f([a],[x]);[a]=3;$[a]を変数として登録できる
    ///         $パラメトリック;f([t]);g([t])
    ///         $極方程式;f[t])
    /// 画面
    ///     関数式(f(x)の選択・入力、x または t の設定範囲と分割数、関数の種類
    ///     y の表示範囲、yの範囲を自動設定、、アスペクト比の固定(1:1)の設定
    ///     関数、実行、削除ボタン
    /// 関数式
    ///     関数式は;で区切れば複数設定可能
    ///     関数式の先頭に$をつけるとタイトルまたはコメントとして扱われ計算式から除外
    ///     関数式の中で[]で囲まれたものは変数として扱われる[x]または[t]はグラフのx軸の値となる
    ///     その他の[]は式の中で変数の値を設定できる。式の中に=が入っていると式ではなく代入文として扱う
    ///     例 変数代入: $タイトル;sin([x])*[a];[a]=0.2    ⇒ y=sin(x)*0.2と同じ
    ///     　 複数の式: $タイトル;[x]^2+[x];[x]^2;[x]     ⇒ y=x^2+xとy=x^2、y=xの3つの関数をグラフ表示する
    /// 種別
    /// 　　y=f(x) : 一般式 [x]を変数としてyを求めxyグラフ表示する
    /// 　　x=f(t);y=g(t) : [t]を媒介変数とする2つの式からxyグラフ表示をする
    /// 　　　　　　　　　　2つの式をセットで扱い複数の式にも対応している
    /// 　　r=f(t) : tを角度(Radian)、rを半径とする極座標からグラフを表示する
    /// 登録キーワード
    ///     先頭の関数式が$で始まるコメントであればそれがキーワードして登録される
    ///     戦闘がコメントでなければ最初の関数式がキーワードとして登録、パロメトリックで
    ///     あれば最初の2つの関数式がキーワードして登録
    ///     キーワードして登録した後の関数式などは変更した場合上書きされる
    ///     キーワードを変更した場合、重複がなければ新規に登録される
    /// </summary>
    public partial class FuncPlot : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)
        private String mAppFolder;                              //  アプリケーションのフォルダ
        private String mDataFileName = "FuncPlot.csv";          //  計算式保存ファイル名
        private Dictionary<String, String[]> mFuncData = new Dictionary<String, String[]>(); //  計算式リストデータ
        private String[] mFunctionType = { "Normal", "Parametric", "Polar" };   //  関数の種類(一般、媒介変数,極方程式)

        private YWorldShapes ydraw;                     //  グラフィックライブラリ
        private YLib mYlib = new YLib();                //  単なるライブラリ

        private double mXmin = 0;           //  グラフ表示エリア
        private double mXmax = 100;
        private double mYmin = 0;
        private double mYmax = 100;
        private int mDivCount = 50;         //  関数グラフの分割数
        private List<Point[]> mPlotDatas;   //  表示用座標(x,y)データ

        public FuncPlot()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            ydraw = new YWorldShapes(canvas);

            //  実行ファイルのフォルダを取得しワークフォルダとする
            mAppFolder = System.AppDomain.CurrentDomain.BaseDirectory;

            //  初期値の設定
            functionList.Text = "sqrt([x])";
            setFunctionType(mFunctionType[0]);
            minX.Text = mXmin.ToString();
            maxX.Text = mXmax.ToString();
            diveCount.Text = mDivCount.ToString();
            minY.Text = mYmin.ToString();
            maxY.Text = mYmax.ToString();
            autoHeight.IsChecked = true;
            minY.IsEnabled = (autoHeight.IsChecked != true);
            maxY.IsEnabled = (autoHeight.IsChecked != true);

            //  計算式の取り込みコンボボックスに登録する
            loadDataFile();
            setDataComboBox();
        }

        /// <summary>
        /// ウィンドウが表示された時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
        }

        /// <summary>
        /// ウィンドウの大きさが変わった時の処理
        /// ウィンドウのサイズを取得してグラフを再表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != this.Width ||
                mWindowHeight != this.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = this.Width;
                mWindowHeight = this.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }

            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            functionList.Width += dx;
            mPrevWindowWidth = mWindowWidth;

            mWindowState = this.WindowState;
            DrawGraph();        //  グラフ表示
        }

        /// <summary>
        /// ウィンドウを閉じる 現状のウィンドウのサイズと位置は保存しておく
        /// クローズする時に計算式をファイルに保存する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveDataFile();     //  計算式の保存
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.FuncPlotWindowWidth < 100 || Properties.Settings.Default.FuncPlotWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.FuncPlotWindowHeight) {
                Properties.Settings.Default.FuncPlotWindowWidth = mWindowWidth;
                Properties.Settings.Default.FuncPlotWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.FuncPlotWindowTop;
                this.Left = Properties.Settings.Default.FuncPlotWindowLeft;
                this.Width = Properties.Settings.Default.FuncPlotWindowWidth;
                this.Height = Properties.Settings.Default.FuncPlotWindowHeight;
                double dy = this.Height - mWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.FuncPlotWindowTop = this.Top;
            Properties.Settings.Default.FuncPlotWindowLeft = this.Left;
            Properties.Settings.Default.FuncPlotWindowWidth = this.Width;
            Properties.Settings.Default.FuncPlotWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [削除]ボタン コンボボックスに表示されている計算式をデータリストから削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (0 < functionList.Items.Count) {
                string key = getFunctionKey(functionList.Text, getCurFunctionType());
                if (mFuncData.ContainsKey(key)) {
                    mFuncData.Remove(key);          //  データの削除
                    setDataComboBox();              //  コンボボックスのデータを更新
                }
            }
        }

        /// <summary>
        /// [関数]ボタン 関数のメニューを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuncMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            FuncMenu dlg = new FuncMenu();
            if (dlg.ShowDialog() == true) {
                functionList.Text += dlg.mResultFunc.Substring(0, dlg.mResultFunc.IndexOf(' '));
            }
        }

        /// <summary>
        /// [関数の種別]のラジオボタン 関数の種別の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FunctionType_Click(object sender, RoutedEventArgs e)
        {
            setFunctionTypeTitle();
        }

        /// <summary>
        /// [?]ボタン ヘルプを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mFuncGraphHelp;
            help.Show();
        }

        /// <summary>
        /// 計算式の選択を変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FunctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < functionList.Items.Count) {
                if (0 <= functionList.SelectedIndex) {
                    //  計算式から登録キーワードを抽出
                    String key = getKeyFuncData(functionList.Items[functionList.SelectedIndex].ToString());
                    //  キーワードからデータを画面に設定
                    if (key != null && 0 < key.Length) {
                        if (mFuncData.ContainsKey(key)) {
                            string[] data = mFuncData[key];
                            //  変数の範囲を設定
                            if (3 < data.Length) {
                                minX.Text = data[1];        //  X min
                                maxX.Text = data[2];        //  X max
                                diveCount.Text = data[3];   //  分割数
                            }
                            if (4 < data.Length) {
                                //  関数の種類を設定
                                setFunctionType(data[4]);
                            }
                            if (7 < data.Length) {
                                //  高さ方向の設定
                                minY.Text = data[5];        //  Y min
                                maxY.Text = data[6];        //  Y max
                                autoHeight.IsChecked = data[7].CompareTo(false.ToString()) == 0 ? false : true;
                            }
                            setFunctionTypeTitle();
                        }
                    }
                    setAutoHeight();
                }
            }
        }

        /// <summary>
        /// [実行]ボタン 計算式からX,Yのデータを求めてグラフ表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            YCalc calc = new YCalc();
            mXmin = calc.expression(minX.Text);
            mXmax = calc.expression(maxX.Text);
            mDivCount = (int)calc.expression(diveCount.Text);
            string function = functionList.Text;

            if (rbParametric.IsChecked == true) {
                makeParametricData(function);           //  パラメトリック方程式
            } else if (rbPolar.IsChecked == true) {
                makePolarData(function);                //  極方程式
            } else {
                makeFunctionData(function);             //  一般的な方程式
            }
            if (0 < mPlotDatas.Count) {
                DrawGraph();                            //  グラフ表示
                dataRegist();                           //  設定値登録
                functionList.Text = function;
            }
        }

        /// <summary>
        /// [高さ自動] 高さ方向を自動にする設定のチェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoHeight_Click(object sender, RoutedEventArgs e)
        {
            setAutoHeight();
        }

        /// <summary>
        /// [アスペクト比固定] 設定チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AspectFix_Click(object sender, RoutedEventArgs e)
        {
            DrawGraph();
        }

        /// <summary>
        /// 高さ方向の状態を確認して再表示する
        /// </summary>
        private void setAutoHeight()
        {
            minY.IsEnabled = (autoHeight.IsChecked != true);
            maxY.IsEnabled = (autoHeight.IsChecked != true);
            if (autoHeight.IsChecked != true && (minY.Text.Length < 1 || maxY.Text.Length < 1))
                return;
            DrawGraph();
        }

        /// <summary>
        /// 方程式からグラフデータを作成
        /// </summary>
        /// <param name="function">方程式</param>
        private void makeFunctionData(String function)
        {
            String[] buf = function.Split(';');
            String errorMsg = "";
            YCalc calc = new YCalc();
            mPlotDatas = new List<Point[]>();
            double xStep = (mXmax - mXmin) / mDivCount;
            bool firstPoint = true;
            List<string[]> argValues = getArguments(function);

            for (int i = 0; i < buf.Length; i++) {
                if (0 < buf[i].Length && !(buf[i][0] == '$') && buf[i].IndexOf('=') < 0) {
                    List<Point> plotDataList = new List<Point>();
                    //  計算式の引数が一つの場合にのみ計算式を実行
                    calc.setExpression(buf[i]);
                    foreach (string[] val in argValues) {
                        calc.setArgValue(val[0], "(" + val[1] + ")");
                    }
                    int n = 0;
                    for (double x = mXmin; x < mXmax + xStep && n <= mDivCount; x += xStep) {
                        if (mXmax < x)
                            x = mXmax;
                        calc.setArgValue("[x]", "(" + x.ToString() + ")");
                        double y = calc.calculate();
                        //  エラーデータ、無限大、
                        if (!calc.mError) {
                            Point point = new Point(x, y);
                            plotDataList.Add(point);
                            if (!double.IsInfinity(y) && !double.IsNaN(y)) {
                                if (firstPoint) {
                                    mYmin = mYmax = y;
                                    firstPoint = false;
                                } else {
                                    mYmin = Math.Min(mYmin, y);
                                    mYmax = Math.Max(mYmax, y);
                                }
                            } else {
                                errorMsg = "値不定か無限大が存在します";
                            }
                        } else {
                            errorMsg = calc.mErrorMsg;
                        }
                    }
                    if (0 < errorMsg.Length)
                        MessageBox.Show(errorMsg, "計算式エラー");
                    if (0 < plotDataList.Count) {
                        Point[] plotDatas = plotDataList.ToArray();
                        mPlotDatas.Add(plotDatas);
                    }
                }
            }
        }

        /// <summary>
        /// 媒介変数による方程式(x=f(t);y=g(t))からグラフデータを作成
        /// </summary>
        /// <param name="function">方程式(x=f(t);y=g(t))</param>
        private void makeParametricData(String function)
        {
            String[] buf = function.Split(';');
            String errorMsg = "";
            YCalc calcX = new YCalc();
            YCalc calcY = new YCalc();
            mPlotDatas = new List<Point[]>();
            double tStep = (mXmax - mXmin) / mDivCount;
            bool firstPoint = true;

            List<string[]> argValues = getArguments(function);
            int n = 0;
            int fx = -1, fy = -1;
            while (n < buf.Length) {
                int i;
                for (i = n; i < buf.Length; i++) {
                    if (0 < buf[i].Length && !(buf[i][0] == '$') && buf[i].IndexOf('=') < 0) {
                        fx = i;
                        break;
                    }
                }
                if (fx < n)
                    return;
                n = i + 1;
                for (i = n; i < buf.Length; i++) {
                    if (0 < buf[i].Length && !(buf[i][0] == '$') && buf[i].IndexOf('=') < 0) {
                        fy = i;
                        break;
                    }
                }
                if (fy < n)
                    return;
                n = i + 1;

                List<Point> plotDataList = new List<Point>();
                calcX.setExpression(buf[fx]);
                calcY.setExpression(buf[fy]);

                double tmin = mXmin;
                double tmax = mXmax;
                for (double t = tmin; t < tmax && n <= mDivCount; t += tStep) {
                    if (tmax < t)
                        t = tmax;
                    foreach (string[] val in argValues) {
                        calcX.setArgValue(val[0], "(" + val[1] + ")");
                        calcY.setArgValue(val[0], "(" + val[1] + ")");
                    }
                    calcX.setArgValue("[t]", "(" + t.ToString() + ")");
                    calcY.setArgValue("[t]", "(" + t.ToString() + ")");
                    double x = calcX.calculate();
                    double y = calcY.calculate();
                    //  エラーデータ、無限大、
                    if (!calcX.mError && !calcY.mError) {
                        Point point = new Point(x, y);
                        plotDataList.Add(point);
                        if (!double.IsInfinity(x) && !double.IsNaN(x) &&
                            !double.IsInfinity(y) && !double.IsNaN(y)) {
                            if (firstPoint) {
                                mXmin = mXmax = x;
                                mYmin = mYmax = y;
                                firstPoint = false;
                            } else {
                                mXmin = Math.Min(mXmin, x);
                                mXmax = Math.Max(mXmax, x);
                                mYmin = Math.Min(mYmin, y);
                                mYmax = Math.Max(mYmax, y);
                            }
                        } else {
                            errorMsg = "値不定か無限大が存在します";
                        }
                    } else {
                        if (calcX.mError)
                            errorMsg = calcX.mErrorMsg;
                        else if (calcY.mError)
                            errorMsg = calcY.mErrorMsg;
                    }
                }
                if (0 < errorMsg.Length)
                    MessageBox.Show(errorMsg, "計算式エラー");
                if (0 < plotDataList.Count) {
                    Point[] plotDatas = plotDataList.ToArray();
                    mPlotDatas.Add(plotDatas);
                }
            }
        }

        /// <summary>
        /// 極方程式からグラフデータを作成
        /// </summary>
        /// <param name="function">方程式</param>
        /// <returns></returns>
        private void makePolarData(String function)
        {
            String[] buf = function.Split(';');
            String errorMsg = "";
            YCalc calcR = new YCalc();
            mPlotDatas = new List<Point[]>();
            double tStep = (mXmax - mXmin) / mDivCount;
            bool firstPoint = true;
            List<string[]> arguments = getArguments(function);

            for (int i = 0; i < buf.Length; i++) {
                if (0 < buf[i].Length && !(buf[i][0] == '$') && buf[i].IndexOf('=') < 0) {
                    List<Point> plotDataList = new List<Point>();
                    //  計算式の引数が一つの場合にのみ計算式を実行
                    calcR.setExpression(buf[i]);
                    int n = 0;
                    double tmin = mXmin;
                    double tmax = mXmax;
                    for (double t = tmin; t < tmax + tStep && n <= mDivCount; t += tStep) {
                        if (tmax < t)
                            t = tmax;
                        foreach (string[] val in arguments) {
                            calcR.setArgValue(val[0], "(" + val[1] + ")");
                        }
                        calcR.setArgValue("[t]", "(" + t.ToString() + ")");
                        double r = calcR.calculate();
                        //  エラーデータ、無限大、
                        if (!calcR.mError) {
                            if (!double.IsInfinity(r) && !double.IsNaN(r)) {
                                //  極座標から直交座標に変換
                                double x = r * Math.Cos(t);
                                double y = r * Math.Sin(t);
                                Point point = new Point(x, y);
                                plotDataList.Add(point);
                                if (firstPoint) {
                                    mXmin = mXmax = x;
                                    mYmin = mYmax = y;
                                    firstPoint = false;
                                } else {
                                    mXmin = Math.Min(mXmin, x);
                                    mXmax = Math.Max(mXmax, x);
                                    mYmin = Math.Min(mYmin, y);
                                    mYmax = Math.Max(mYmax, y);
                                }
                            } else {
                                Point point = new Point(double.NaN, double.NaN);
                                plotDataList.Add(point);
                                errorMsg = "値不定か無限大が存在します";
                            }
                        } else {
                            errorMsg = calcR.mErrorMsg;
                        }
                    }
                    if (0 < errorMsg.Length)
                        MessageBox.Show(errorMsg, "計算式エラー");
                    if (0 < plotDataList.Count) {
                        Point[] plotDatas = plotDataList.ToArray();
                        mPlotDatas.Add(plotDatas);
                    }
                }
            }
        }

        /// <summary>
        /// 計算式の登録
        /// </summary>
        private void dataRegist()
        {
            String[] data = new String[8];
            data[0] = functionList.Text.Trim();
            data[1] = minX.Text;
            data[2] = maxX.Text;
            data[3] = diveCount.Text;
            data[4] = getCurFunctionType();
            data[5] = minY.Text;
            data[6] = maxY.Text;
            data[7] = autoHeight.IsChecked.ToString();
            string key = getFunctionKey(functionList.Text, getCurFunctionType());
            if (mFuncData.ContainsKey(key)) {
                mFuncData[key] = data;
            } else {
                mFuncData.Add(key, data);
            }
            setDataComboBox();      //  計算式をコンボボックスに登録
        }

        /// <summary>
        /// 関数式から引数リストを抽出する
        /// </summary>
        /// <param name="function">関数式</param>
        /// <returns>引数リスト</returns>
        private List<string[]> getArguments(string function)
        {
            List<string[]> arguments = new List<string[]>();
            String[] buf = function.Split(';');
            for (int i = 0; i < buf.Length; i++) {
                if (0 < buf[i].Length && 0 < buf[i].IndexOf('=')) {
                    string[] argVal = new string[2];
                    string[] arg = buf[i].Split('=');
                    if (1 < arg.Length && 0 <= arg[0].IndexOf('[')) {
                        argVal[0] = arg[0].Trim();
                        argVal[1] = arg[1].Trim();
                        arguments.Add(argVal);
                    }
                }
            }
            return arguments;
        }

        /// <summary>
        /// 関数式からタイトルキーワードの抽出
        /// </summary>
        /// <param name="function">関数式</param>
        /// <param name="type">関数の種別</param>
        /// <returns></returns>
        private string getFunctionKey(string function, string type)
        {
            string[] key = function.Split(';');
            if (1 < key.Length && type.CompareTo(mFunctionType[1]) == 0 && (key[0][0] != '$')) {
                if (0 <= key[0].IndexOf("[t]") && 0 <= key[1].IndexOf("[t]"))
                    return key[0] + ";" + key[1];
            }
            return key[0].Trim();
        }


        /// <summary>
        /// 方程式の種類をラジオボタンから取得
        /// </summary>
        /// <returns>方程式の種類</returns>
        private string getCurFunctionType()
        {
            return mFunctionType[rbParametric.IsChecked == true ? 1 : (rbPolar.IsChecked == true ? 2 : 0)];
        }

        /// <summary>
        /// 方程式の種類をラジオボタンに反映
        /// </summary>
        /// <param name="type">方程式の種類</param>
        private void setFunctionType(string type)
        {
            if (type.CompareTo(mFunctionType[1]) == 0)
                rbParametric.IsChecked = true;
            else if (type.CompareTo(mFunctionType[2]) == 0)
                rbPolar.IsChecked = true;
            else
                rbNormal.IsChecked = true;
        }

        /// <summary>
        /// 関数の種別のタイトル変更
        /// </summary>
        private void setFunctionTypeTitle()
        {
            if (rbNormal.IsChecked == true) {
                functionTitle.Text = "関数 f(x)";
                rengeTitle.Text = "範囲 x min";
            } else {
                functionTitle.Text = "関数 f(t)";
                rengeTitle.Text = "範囲 t min";
            }
        }

        /// <summary>
        /// 計算式のリストデータをコンボボックスに登録する
        /// </summary>
        private void setDataComboBox()
        {
            if (0 < mFuncData.Count) {
                List<string> functionData = new List<string>();
                foreach (String key in mFuncData.Keys) {
                    functionData.Add(key);
                }
                functionData.Sort();
                functionList.Items.Clear();
                foreach (String key in functionData) {
                    //  登録キーワードと計算式が異なる場合があるので都度計算式を取得
                    string[] data = mFuncData[key];
                    functionList.Items.Add(data[0]);
                }
            }
        }

        /// <summary>
        /// 計算式でできたX,Yデータをグラフ表示する
        /// </summary>
        private void DrawGraph()
        {
            if (mPlotDatas == null || mPlotDatas.Count < 1) {
                //MessageBox.Show("データが作成されていません", "描画エラー");
                return;
            }
            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            //  アスペクト比無効
            ydraw.setAspectFix(aspectFix.IsChecked == true);
            //  高さ自動
            double ymin = mYmin;
            double ymax = mYmax;
            if (autoHeight.IsChecked != true) {
                YCalc calc = new YCalc();
                ymin = calc.expression(minY.Text);
                ymax = calc.expression(maxY.Text);
            }
            double dx = (mXmax - mXmin);    //  グラフエリアの範囲
            double dy = (ymax - ymin);
            if (dx <= 0 || dy <= 0) {
                MessageBox.Show("領域が求められていません", "描画エラー");
                return;
            }

            //  グラフエリアの設定
            ydraw.setWorldWindow(mXmin - dx * 0.1, ymax + dy * 0.05, mXmax + dx * 0.05, ymin - dy * 0.1);
            ydraw.clear();

            //  目盛り付き補助線の描画
            double x, y;
            ydraw.setScreenTextSize(15);
            ydraw.setTextColor(Brushes.Black);
            ydraw.setColor(Brushes.Aqua);
            ydraw.drawRectangle(mXmin, ymax, dx, dy, 0);
            double auxDx = mYlib.graphStepSize(dx, 5);
            double auxDy = mYlib.graphStepSize(dy, 5);
            for (x = Math.Floor(mXmin / auxDx) * auxDx; x <= mXmax; x += auxDx) {
                if (mXmin <= x && x <= mXmax) {
                    if (x == 0) {
                        //  原点を通る線
                        ydraw.setColor(Brushes.Red);
                        ydraw.setTextColor(Brushes.Red);
                    } else {
                        ydraw.setColor(Brushes.Aqua);
                        ydraw.setTextColor(Brushes.Black);
                    }
                    ydraw.drawLine(x, ymin, x, ymax);
                    ydraw.drawText(x.ToString("F"), new Point(x, ymin), 0, HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }
            for (y = Math.Floor(ymin / auxDy) * auxDy; y <= ymax; y += auxDy) {
                if (ymin <= y && y <= ymax) {
                    if (y == 0) {
                        //  原点を通る線
                        ydraw.setColor(Brushes.Red);
                        ydraw.setTextColor(Brushes.Red);
                    } else {
                        ydraw.setColor(Brushes.Aqua);
                        ydraw.setTextColor(Brushes.Black);
                    }
                    ydraw.drawLine(mXmin, y, mXmax, y);
                    ydraw.drawText(y.ToString("F"), new Point(mXmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
                }
            }

            //  計算式のX,Yデータをプロット
            for (int i = 0; i < mPlotDatas.Count; i++) {
                ydraw.setColor(ydraw.getColor15(i * 2));
                Point[] plotData = mPlotDatas[i];
                for (int j = 0; j < plotData.Length - 1; j++) {
                    if (!double.IsNaN(plotData[j].X) && !double.IsNaN(plotData[j].Y) &&
                        !double.IsInfinity(plotData[j].X) && !double.IsInfinity(plotData[j].Y) &&
                        !double.IsNaN(plotData[j + 1].X) && !double.IsNaN(plotData[j + 1].Y) &&
                        !double.IsInfinity(plotData[j + 1].X) && !double.IsInfinity(plotData[j + 1].Y))
                        clipingLine(plotData[j], plotData[j + 1], ymin, ymax);
                }

            }
        }

        /// <summary>
        /// 上下方向でクリッピングして線分を表示する
        /// </summary>
        /// <param name="ps">線分の始点</param>
        /// <param name="pe">線分の終点</param>
        /// <param name="ymin">クリッピングの下端</param>
        /// <param name="ymax">クリッピングの上端</param>
        private void clipingLine(Point ps, Point pe, double ymin, double ymax)
        {
            //  両端が領域内はそのまま表示
            if (ymin <= ps.Y && ps.Y <= ymax && ymin <= pe.Y && pe.Y <= ymax) {
                ydraw.drawLine(ps, pe);
                return;
            }
            //  線分が領域を跨内場合は表示しない
            if ((ps.Y < ymin && pe.Y < ymin) || (ymax < ps.Y && ymax < pe.Y))
                return;
            //  領域をまたぐ線分をクリッピングする
            if (pe.Y < ps.Y)
                swapPoint(ref ps, ref pe);
            double a = (pe.Y - ps.Y) / (pe.X - ps.X);
            double b = ps.Y - a * ps.X;
            if (ps.Y < ymin) {
                ps.X = (ymin - b) / a;
                ps.Y = ymin;
            }
            if (ymax < pe.Y) {
                pe.X = (ymax - b) / a;
                pe.Y = ymax;
            }
            ydraw.drawLine(ps, pe);
        }

        /// <summary>
        /// 点座標の入れ替え
        /// </summary>
        /// <param name="ps">始点</param>
        /// <param name="pe">終点</param>
        private void swapPoint(ref Point ps, ref Point pe)
        {
            Point p = new Point(ps.X, ps.Y);
            ps.X = pe.X;
            ps.Y = pe.Y;
            pe.X = p.X;
            pe.Y = p.Y;
        }

        /// <summary>
        /// 計算式から登録キーワードを検索する
        /// </summary>
        /// <param name="function">計算式</param>
        /// <returns>登録キーワード</returns>
        private String getKeyFuncData(String function)
        {
            foreach (KeyValuePair<String, String[]> kvp in mFuncData) {
                if (kvp.Value[0].CompareTo(function) == 0)
                    return kvp.Key;
            }
            return null;
        }

        /// <summary>
        /// 計算式のリストをファイルに保存(CSV形式)
        /// </summary>
        private void saveDataFile()
        {
            String filePath = mAppFolder + "\\" + mDataFileName;    //  ファイルパス
            if (0 < mFuncData.Count) {
                System.IO.StreamWriter dataFile = new System.IO.StreamWriter(filePath, false);
                dataFile.WriteLine("関数式,Xmin,Xmax,分割数,種別,Ymin,Ymax,高さ自動");
                foreach (KeyValuePair<String, String[]> kvp in mFuncData) {
                    dataFile.WriteLine("\"" + kvp.Value[0] + "\",\"" + kvp.Value[1] + "\",\"" +
                         kvp.Value[2] + "\",\"" + kvp.Value[3] + "\",\"" + kvp.Value[4] + "\",\"" +
                        kvp.Value[5] + "\",\"" + kvp.Value[6] + "\",\"" + kvp.Value[7] + "\"");
                }
                dataFile.Close();
            }
        }

        /// <summary>
        /// ファイルから計算式を取り込む
        /// </summary>
        private void loadDataFile()
        {
            String filePath = mAppFolder + "\\" + mDataFileName;    //  ファイルパス
            if (System.IO.File.Exists(filePath)) {
                System.IO.StreamReader dataFile = new System.IO.StreamReader(filePath);
                mFuncData.Clear();
                String line;
                while ((line = dataFile.ReadLine()) != null) {
                    String[] buf = mYlib.seperateString(line);
                    if (buf[0].CompareTo("関数式") != 0) {
                        String[] data = new string[8];
                        for (int i = 0; i < data.Length; i++)
                            data[i] = "";
                        if (3 < buf.Length) {
                            data[0] = buf[0];           //  関数式
                            data[1] = buf[1];           //  Xmin
                            data[2] = buf[2];           //  Xmax
                            data[3] = buf[3];           //  分割数
                            if (4 < buf.Length)         //  種別
                                data[4] = buf[4];
                            else
                                data[4] = mFunctionType[0];
                            if (7 < buf.Length) {
                                data[5] = buf[5];       //  Ymin
                                data[6] = buf[6];       //  Ymax
                                data[7] = buf[7];       //  高さ自動
                            }
                            string key = getFunctionKey(buf[0], data[4]);
                            if (!mFuncData.ContainsKey(key))
                                mFuncData.Add(key, data);
                        }
                    }
                }
                dataFile.Close();
            }
        }
    }
}
