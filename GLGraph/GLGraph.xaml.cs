using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Wpf3DLib;
using WpfLib;
using MessageBox = System.Windows.MessageBox;

namespace CalcApp
{
    /// <summary>
    /// GLGraph.xaml の相互作用ロジック
    /// 
    /// OpenGLを利用するにはNuGetより、OpenTKとOpenTK.GLControlのインストールが必要
    /// OpenTKの表示コンテナにはWindowsFormsHostを使用する
    /// WindowsFormsHostを使用するには参照でWindowsFormsIIntegrationを追加しておく
    /// GLControlでMouseEventArgsを使用するため、参照でSystem.Windows.Formsを追加しておく
    /// GL.ViewPortを使用するためには参照でSystem.Drawingを追加しておく
    /// </summary>
    public partial class GLGraph : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private System.Windows.WindowState mWindowState = System.Windows.WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private string mAppFolder;                              //  アプリケーションのフォルダ
        private string mDataFileName = "Func3DPlot.csv";        //  計算式保存ファイル名


        private List<string[]> mFuncList;                       //  計算式リスト
        private string[] mFuncListTitle = {                     //  計算式リストのタイトル
            "タイトル", "関数式", "Xmin", "Xmax", "X分割数", "Ymin", "Ymax", "Y分割数", "種別", "Zmin", "Zmax", "Z自動"
        };
        private Dictionary<string, string[]> mFuncData = new Dictionary<string, string[]>(); //  計算式リストデータ
        private enum FUNCTION_TYPE { Normal, Parametric, Polar, Non };   //  関数の種類(一般、媒介変数,極方程式)
        private string mFunction = "50*cos(PI/100*sqrt([x]^2+[y]^2))+5*sin(PI/25*sqrt([x]^2+[y]^2))";
        //private List<string> mTitleList = new List<string>();
        //private List<string> mExpressionList = new List<string>();
        private bool mTitleSelectOn = true;
        private bool mFuncSelectOn = true;

        private List<Vector3[,]> mPositionList;     //  座標データリスト
        private int mXDivideCount = 40;             //  X方向の分割数
        private int mYDivideCount = 40;             //  Y方向の分割数
        private double mXStart = -100;              //  X方向の開始位置
        private double mXEnd = 100;                 //  X方向の終了位置
        private double mYStart = -100;              //  Y方向の開始位置
        private double mYEnd = 100;                 //  Y方向の終了位置
        private Vector3 mMin;                       //  表示領域の最小値
        private Vector3 mMax;                       //  表示領域の最大値
        private Vector3 mManMin;
        private Vector3 mManMax;

        private bool mError = false;
        private string mErrorMsg = "";
        private GLControl glControl;                //  OpenTK.GLcontrol
        private GL3DLib m3Dlib;                     //  三次元表示ライブラリ
        private YLib ylib = new YLib();             //  単なるライブラリ


        public GLGraph()
        {
            InitializeComponent();

            mWindowWidth = GLWindow.Width;
            mWindowHeight = GLWindow.Height;
            mPrevWindowWidth = mWindowWidth;
            WindowFormLoad();

            //  実行ファイルのフォルダを取得しワークフォルダとする
            mAppFolder = AppDomain.CurrentDomain.BaseDirectory;
            mDataFileName = mAppFolder + "\\" + mDataFileName;    //  ファイルパス

            //  計算式の取り込みコンボボックスに登録する
            loadFuncList(mDataFileName);
            //loadDataFile();                         //  計算式リストの取込み
            setDataComboBox();

            minX.Text = "" + mXStart;
            maxX.Text = "" + mXEnd;
            diveXCount.Text = "" + mXDivideCount;
            minY.Text = "" + mYStart;
            maxY.Text = "" + mYEnd;
            diveYCount.Text = "" + mYDivideCount;
            //titleList.Text = "サンプル";
            functionList.Text = mFunction;
            rbNormal.IsChecked = true;

            glControl = new GLControl();
            m3Dlib = new GL3DLib(glControl);
            m3Dlib.initPosition(1.5f, -70f, 0f, 10f);

            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseUp += glControl_MouseUp;
            glControl.MouseMove += glControl_MosueMove;
            glControl.MouseWheel += glControl_MouseWheel;

            glGraph.Child = glControl;      //  OpenGLをWindowsに接続
        }

        /// <summary>
        /// ウィンドウの領域が変化したときの処理
        /// ウィンドウの大きさに合わせてコンボボックスの長さを変える
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWindow_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.WindowState != mWindowState &&
                this.WindowState == System.Windows.WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                mWindowHeight = System.Windows.SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != GLWindow.Width ||
                mWindowHeight != GLWindow.Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = GLWindow.Width;
                mWindowHeight = GLWindow.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }
            double dx = mWindowWidth - mPrevWindowWidth;
            titleList.Width += dx;
            functionList.Width += dx;
            mPrevWindowWidth = mWindowWidth;

            mWindowState = this.WindowState;
        }

        /// <summary>
        /// アプリケーションを閉じるときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
            saveFuncList(mDataFileName);
            //saveDataFile();     //  計算式リストの保存
        }

        /// <summary>
        /// Windowの状態を前回の状態にする 
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.GLGraphWindowWidth < 100 || Properties.Settings.Default.GLGraphWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.GLGraphWindowHeight) {
                Properties.Settings.Default.GLGraphWindowWidth = mWindowWidth;
                Properties.Settings.Default.GLGraphWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.GLGraphWindowTop;
                Left = Properties.Settings.Default.GLGraphWindowLeft;
                Width = Properties.Settings.Default.GLGraphWindowWidth;
                Height = Properties.Settings.Default.GLGraphWindowHeight;
                double dy = GLWindow.Height - mWindowHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.GLGraphWindowTop = Top;
            Properties.Settings.Default.GLGraphWindowLeft = Left;
            Properties.Settings.Default.GLGraphWindowWidth = Width;
            Properties.Settings.Default.GLGraphWindowHeight = Height;
            Properties.Settings.Default.Save();
        }


        /// <summary>
        /// OpenGLのLoad 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Lighting);    //  光源の使用

            GL.PointSize(3.0f);                 //  点の大きさ
            GL.LineWidth(1.5f);                 //  線の太さ

            setParameter();
            makePlotData(mFunction);            //  座標データの作成

            //throw new NotImplementedException();
        }

        /// <summary>
        /// OpenGLの描画 都度呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Windowのサイズが変わった時、glControl_Paintも呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(glControl.ClientRectangle);

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスホイールによるzoom up/down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float delta = (float)e.Delta / 1000f;// - wheelPrevious;
            m3Dlib.setZoom(delta);

            renderFrame();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 視点(カメラ)の回転と移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MosueMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (m3Dlib.moveObject(e.X, e.Y))
                renderFrame();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスダウン 視点(カメラ)の回転開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                m3Dlib.setMoveStart(true, e.X, e.Y);
            } else if (e.Button == MouseButtons.Right) {
                m3Dlib.setMoveStart(false, e.X, e.Y);
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスアップ 視点(カメラ)の回転終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m3Dlib.setMoveEnd();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 三次元データ表示
        /// </summary>
        private void renderFrame()
        {
            m3Dlib.renderFrameStart();
            foreach (Vector3[,] position in mPositionList) {
                //  3Dグラフの表示
                if (!dispModel.IsChecked == true)
                    m3Dlib.drawWireShape(position);
                else
                    m3Dlib.drawSurfaceShape(position);
            }
            m3Dlib.setAreaFrameDisp(areaFrame.IsChecked == true);
            m3Dlib.drawAxis();
            m3Dlib.rendeFrameEnd();
        }

        /// <summary>
        /// [実行]ボタン 計算式の実行と表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void executeBtn_Click(object sender, RoutedEventArgs e)
        {
            //  パラメータの取得
            setParameter();
            makePlotData(mFunction);                    //  座標データの作成
            setHeightParameter();

            if (0 < mPositionList.Count) {
                renderFrame();                          //  座標データから三次元表示
                dataRegist();                           //  設定値登録
                functionList.Text = mFunction;
            }
        }

        /// <summary>
        /// [削除]ボタン 計算式をリストから削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (0 < functionList.Items.Count) {
                mFuncList.RemoveAt(functionList.SelectedIndex);
                setDataComboBox();              //  コンボボックスのデータを更新
            }
        }

        /// <summary>
        /// [関数]ボタン 使用できる関数を表示し計算式欄にセットする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void funcMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            FuncMenu dlg = new FuncMenu();
            if (dlg.ShowDialog() == true) {
                functionList.Text += dlg.mResultFunc.Substring(0, dlg.mResultFunc.IndexOf(' '));
            }
        }

        /// <summary>
        /// [リセット]ボタンの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            m3Dlib.initPosition(1.5f, -70f, 0f, 10f);
            renderFrame();
        }

        /// <summary>
        /// [?]ボタン ヘルプファイルを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mGraph3DHelp;
            help.Show();
        }

        /// <summary>
        /// [サーフェス]チェックボックス
        /// ワイヤーフレームとサーフェイスモデルの表示切替で再表示をする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispModel_Click(object sender, RoutedEventArgs e)
        {
            renderFrame();                          //  座標データから三次元表示
        }

        /// <summary>
        /// [背景色黒]チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backBlack_Click(object sender, RoutedEventArgs e)
        {
            if (backBlack.IsChecked == true)
                m3Dlib.setBackColor(Color4.Black);
            else
                m3Dlib.setBackColor(Color4.White);
            renderFrame();
        }

        /// <summary>
        /// 関数の種別切り替えでタイトルを変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FunctionType_Click(object sender, RoutedEventArgs e)
        {
            setFunctionTypeTitle();
        }

        /// <summary>
        /// [自動]チェックボックス  Z方向の範囲自動の切り替えで呼ばれる
        /// 入力値を使う設定で入力値が設定されていない時は再表示しない
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoHeight_Click(object sender, RoutedEventArgs e)
        {
            minZ.IsEnabled = (autoHeight.IsChecked != true);
            maxZ.IsEnabled = (autoHeight.IsChecked != true);
            if (autoHeight.IsChecked != true && (minZ.Text.Length < 1 || maxZ.Text.Length < 1))
                return;
            setHeightParameter();
            renderFrame();
        }

        /// <summary>
        /// 標示領域の枠表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void areaFrame_Click(object sender, RoutedEventArgs e)
        {
            renderFrame();
        }

        /// <summary>
        /// タイトルの選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void titleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < titleList.Items.Count) {
                if (0 <= titleList.SelectedIndex && mTitleSelectOn) {
                    dataFuncSet(titleList.SelectedIndex);
                    mFuncSelectOn = false;
                    functionList.SelectedIndex = titleList.SelectedIndex;
                }
            }
            mTitleSelectOn = true;
        }

        /// <summary>
        /// 計算式の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void functionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < functionList.Items.Count) {
                if (0 <= functionList.SelectedIndex && mFuncSelectOn) {
                    dataFuncSet(functionList.SelectedIndex);
                    mTitleSelectOn = false;
                    titleList.SelectedIndex = functionList.SelectedIndex;
                }
            }
            mFuncSelectOn = true;
        }

        /// <summary>
        /// コンボボックスに計算式を登録する
        /// </summary>
        private void setDataComboBox()
        {
            if (0 < mFuncList.Count) {
                titleList.Items.Clear();
                functionList.Items.Clear();
                foreach (string[] data in mFuncList) {
                    titleList.Items.Add(data[0]);
                    functionList.Items.Add(data[1]);
                }
                //  ItemSourceを使うとデータの更新ができない
                //titleList.ItemsSource = mTitleList;
                //functionList.ItemsSource = mExpressionList;
            }
        }

        /// <summary>
        /// 計算式のデータに登録
        /// </summary>
        private void dataRegist()
        {
            string[] data = new string[mFuncListTitle.Length];
            data[0] = titleList.Text.Trim();
            data[1] = functionList.Text.Trim();
            data[2] = minX.Text;
            data[3] = maxX.Text;
            data[4] = diveXCount.Text;
            data[5] = minY.Text;
            data[6] = maxY.Text;
            data[7] = diveYCount.Text;
            data[8] = functionType2String(rbParametric.IsChecked == true ? FUNCTION_TYPE.Parametric : FUNCTION_TYPE.Normal);
            data[9] = minZ.Text;
            data[10] = maxZ.Text;
            data[11] = autoHeight.IsChecked.ToString();
            if (0 <= titleList.SelectedIndex && data[0].CompareTo(mFuncList[titleList.SelectedIndex][0]) == 0) {
                mFuncList.RemoveAt(titleList.SelectedIndex);
            }
            mFuncList.Insert(0, data);
            setDataComboBox();      //  計算式をコンボボックスに登録
        }


        /// <summary>
        /// 関数リストのデータをコントロールに
        /// </summary>
        /// <param name="n">関数リストの位置</param>
        private void dataFuncSet(int n)
        {
            string[] data = mFuncList[n];
            //  変数の範囲を設定
            if (7 < data.Length) {
                minX.Text = data[2];        //  X min
                maxX.Text = data[3];        //  X max
                diveXCount.Text = data[4];  //  分割数
                minY.Text = data[5];        //  Y min
                maxY.Text = data[6];        //  Y max
                diveYCount.Text = data[7];  //  分割数
            }
            if (8 < data.Length) {
                FUNCTION_TYPE type = string2FunctionType(data[8]);
                if (type == FUNCTION_TYPE.Parametric)
                    rbParametric.IsChecked = true;
                else
                    rbNormal.IsChecked = true;
            }
            if (11 < data.Length) {
                minZ.Text = data[9];        //  Z min
                maxZ.Text = data[10];       //  Z max
                autoHeight.IsChecked = data[11].CompareTo(true.ToString()) == 0 ? true : false;
                minZ.IsEnabled = (autoHeight.IsChecked != true);
                maxZ.IsEnabled = (autoHeight.IsChecked != true);
            }
            setFunctionTypeTitle();
        }

        /// <summary>
        /// 関数の種別のタイトル変更
        /// </summary>
        private void setFunctionTypeTitle()
        {
            if (rbNormal.IsChecked == true) {
                functionTitle.Text = "関数 z=f(x,y)";
                rengeXTitle.Text = "範囲 x min";
                rengeYTitle.Text = "範囲 y min";
            } else {
                functionTitle.Text = "関数 x=f(s,t),y=g(s,t),z=h(s,t)";
                rengeXTitle.Text = "範囲 s min";
                rengeYTitle.Text = "範囲 t min";
            }
        }

        /// <summary>
        /// 計算式やデータ範囲などをグローバル変数に設定
        /// 変数の初期値は計算式からも求められるようにした
        /// </summary>
        private void setParameter()
        {
            YCalc calc = new YCalc();
            mXStart = calc.expression(minX.Text);                   //  X開始値
            mXEnd = calc.expression(maxX.Text);                     //  X終了値
            mXDivideCount = (int)calc.expression(diveXCount.Text);  //  X分割数
            mYStart = calc.expression(minY.Text);                   //  Y開始値
            mYEnd = calc.expression(maxY.Text);                     //  Y終了値
            mYDivideCount = (int)calc.expression(diveYCount.Text);  //  Y分割数
            mFunction = functionList.Text;                          //  数式、
        }

        /// <summary>
        /// Z方向の範囲の設定
        /// 高さ自動出ない場合は入力値を使用、入力値が設定されていない場合は
        /// 計算で求めたZ方向の最大最小値を入力値に設定
        /// </summary>
        private void setHeightParameter()
        {
            mManMin = mMin;
            mManMax = mMax;
            if (autoHeight.IsChecked != true && 0 < minZ.Text.Length && 0 < maxZ.Text.Length) {
                YCalc calc = new YCalc();
                mManMin.Z = (float)calc.expression(minZ.Text);
                mManMax.Z = (float)calc.expression(maxZ.Text);
            } else {
                if (minZ.Text.Length == 0 && mMin != null) {
                    minZ.Text = mMin.Z.ToString();
                    mManMin.Z = mMin.Z;
                }
                if (maxZ.Text.Length == 0 && mMax != null) {
                    maxZ.Text = mMax.Z.ToString();
                    mManMax.Z = mMax.Z;
                }
            }
            m3Dlib.setArea(mManMin, mManMax);
        }

        /// <summary>
        /// 計算式から座標データを作成する
        /// </summary>
        private void makePlotData(string function)
        {
            string errorMsg = "";
            //  計算式の種類を求める
            FUNCTION_TYPE funcType = getFunctionType(function);
            //  計算式リストを作成
            List<string> functions = getFunctionList(function);

            if (funcType == FUNCTION_TYPE.Parametric && functions.Count < 3) {
                MessageBox.Show(errorMsg, "パラメトリックで計算式が足りません");
                return;
            } else if (funcType == FUNCTION_TYPE.Non) {
                MessageBox.Show(errorMsg, "計算式が定まっていません");
                return;
            }
            //  プロットのステップ幅をもとめる
            double dx = (mXEnd - mXStart) / mXDivideCount;
            double dy = (mYEnd - mYStart) / mYDivideCount;

            YCalc[] calc = new YCalc[functions.Count];
            for (int i = 0; i < functions.Count; i++) {
                calc[i] = new YCalc();
                calc[i].setExpression(functions[i]);
            }

            //  プロットデータの配列領域の取得
            mPositionList = new List<Vector3[,]>();
            bool arearInit = true;
            //  座標計算と表示領域を求める
            for (int n = 0; n < calc.Length; n++) {
                Vector3[,] position = new Vector3[mYDivideCount + 1, mXDivideCount + 1];
                for (int i = 0; i <= mYDivideCount; i++) {
                    for (int j = 0; j <= mXDivideCount; j++) {
                        Vector3 pos;
                        if (funcType == FUNCTION_TYPE.Normal) {
                            pos = functionNormal(calc[n], mXStart + dx * j, mYStart + dy * i);
                        } else if (funcType == FUNCTION_TYPE.Parametric && n + 2 < calc.Length) {
                            pos = functionParametric(calc[n], calc[n + 1], calc[n + 2], mXStart + dx * j, mYStart + dy * i);
                        } else {
                            return;
                        }
                        if (!mError) {
                            position[i, j] = pos;
                            //  表示領域の取得
                            if (arearInit) {
                                m3Dlib.setArea(new Vector3(position[i, j]), new Vector3(position[i, j]));
                                arearInit = false;
                            } else {
                                m3Dlib.extendArea(position[i, j]);
                            }
                        } else {
                            if (0 < j)
                                position[i, j] = new Vector3(position[i, j - 1]);
                            errorMsg = mErrorMsg;
                        }
                    }
                }
                if (funcType == FUNCTION_TYPE.Parametric)
                    n += 2;
                mPositionList.Add(position);
            }
            //  表示領域をチェックする
            m3Dlib.areaCheck();
            //  表示領域を取得
            mManMin = mMin = m3Dlib.getAreaMin();
            mManMax = mMax = m3Dlib.getAreaMax();
            //  カラーレベルの設定
            m3Dlib.setColorLevel(mMin.Z, mMax.Z);

            if (0 < errorMsg.Length)
                System.Windows.MessageBox.Show(errorMsg, "計算式エラー");
            else {
                minmax.Text = "(" + mMin.Z + "," + mMax.Z + ")";
            }
        }

        /// <summary>
        /// 計算式から関数の種別を求める(Norma/Parametric/Non)
        /// </summary>
        /// <param name="function">計算式</param>
        /// <returns>種別</returns>
        private FUNCTION_TYPE getFunctionType(string function)
        {
            FUNCTION_TYPE funcType = FUNCTION_TYPE.Non;
            string[] functions = function.Split(';');
            for (int i = 0; i < functions.Length; i++) {
                if (functions[i][0] == '$') {
                    //  先頭に$がある場合はコメント扱い
                    continue;
                } else if (0 <= functions[i].IndexOf("[x]") || 0 <= functions[i].IndexOf("[y]")) {
                    //  通常の3次元方程式(z = f(x,y)
                    if (funcType == FUNCTION_TYPE.Non || funcType == FUNCTION_TYPE.Normal)
                        funcType = FUNCTION_TYPE.Normal;
                    else {
                        funcType = FUNCTION_TYPE.Non;
                        break;
                    }
                } else if (0 <= functions[i].IndexOf("[s]") || 0 <= functions[i].IndexOf("[t]")) {
                    //  パラメトリック方程式
                    if (funcType == FUNCTION_TYPE.Non || funcType == FUNCTION_TYPE.Parametric)
                        funcType = FUNCTION_TYPE.Parametric;
                    else {
                        funcType = FUNCTION_TYPE.Non;
                        break;
                    }
                }
            }
            return funcType;
        }

        /// <summary>
        /// 計算式から計算式リストを作成し、代入文も反映させる
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        private List<string> getFunctionList(string function)
        {
            List<string> functionList = new List<string>();     //  計算式リスト
            List<string> substitutionList = new List<string>(); //  代入式リスト
            //  計算式と代入文のリスト化
            string[] functions = function.Split(';');
            for (int i = 0; i < functions.Length; i++) {
                if (functions[i][0] == '$') {
                    //  先頭に$がある場合はコメント扱い
                    continue;
                } else if (0 <= functions[i].IndexOf("[x]") || 0 <= functions[i].IndexOf("[y]")) {
                    functionList.Add(functions[i]);
                } else if (0 <= functions[i].IndexOf("[s]") || 0 <= functions[i].IndexOf("[t]")) {
                    functionList.Add(functions[i]);
                } else if (0 <= functions[i].IndexOf("=")) {
                    substitutionList.Add(functions[i]);
                }
            }
            //  代入文の辞書リストを作成
            Dictionary<string, string> substitutions = getSubstitutions(substitutionList);
            //  代入文を反映した計算式リストを作成
            List<string> funcOutList = new List<string>();
            if (0 < substitutions.Count) {
                foreach (string func in functionList) {
                    string f = replcaeSubstitution(func, substitutions);
                    funcOutList.Add(f);
                }
                return funcOutList;
            } else
                return functionList;
        }
        /// <summary>
        /// 計算式(Normal)から座標を求める
        /// </summary>
        /// <param name="calc">計算式</param>
        /// <param name="x">座標値(x)</param>
        /// <param name="y">座標値(y)</param>
        /// <returns>三次元座標</returns>
        private Vector3 functionNormal(YCalc calc, double x, double y)
        {
            Vector3 pos = new Vector3();
            mError = false;
            mErrorMsg = "";
            //  Normal 陽関数
            pos.X = (float)x;
            pos.Y = (float)y;
            calc.setArgValue("[x]", "(" + x.ToString() + ")");
            calc.setArgValue("[y]", "(" + y.ToString() + ")");
            pos.Z = (float)calc.calculate();
            mError = calc.mError;
            mErrorMsg = calc.mErrorMsg;
            return pos;
        }

        /// <summary>
        /// パラメトリックの計算式から座標を求める
        /// </summary>
        /// <param name="calcX">f(st)</param>
        /// <param name="calcY">g(s,t)</param>
        /// <param name="calcZ">h(s,t)</param>
        /// <param name="s">媒介変数(s)</param>
        /// <param name="t">媒介変数(t)</param>
        /// <returns>三次元座標</returns>
        private Vector3 functionParametric(YCalc calcX, YCalc calcY, YCalc calcZ, double s, double t)
        {
            Vector3 pos = new Vector3();
            mError = false;
            mErrorMsg = "";
            //  Parametric 媒介変数
            calcX.setArgValue("[s]", "(" + s.ToString() + ")");
            calcX.setArgValue("[t]", "(" + t.ToString() + ")");
            pos.X = (float)calcX.calculate();
            calcY.setArgValue("[s]", "(" + s.ToString() + ")");
            calcY.setArgValue("[t]", "(" + t.ToString() + ")");
            pos.Y = (float)calcY.calculate();
            calcZ.setArgValue("[s]", "(" + s.ToString() + ")");
            calcZ.setArgValue("[t]", "(" + t.ToString() + ")");
            pos.Z = (float)calcZ.calculate();
            mError = calcX.mError || calcY.mError || calcZ.mError;
            mErrorMsg = calcX.mErrorMsg + calcY.mErrorMsg + calcZ.mErrorMsg;
            return pos;
        }


        /// <summary>
        /// 代入文のリストから代入文の辞書リストを作成
        /// </summary>
        /// <param name="substitutionList">代入文リスト</param>
        /// <returns>代入文の辞書化リスト</returns>
        private Dictionary<string, string> getSubstitutions(List<string> substitutionList)
        {
            //  代入文の辞書化
            Dictionary<string, string> substitutions = new Dictionary<string, string>();
            foreach (string function in substitutionList) {
                string[] keyValue = function.Split('=');
                if (keyValue.Length < 2)
                    continue;
                string key = keyValue[0].Trim();
                string val = keyValue[1].Trim();
                if (!substitutions.ContainsKey(key))
                    substitutions.Add(key, val);
            }
            return substitutions;
        }

        /// <summary>
        /// 計算式に入っている代入文の置き換えを行う
        /// </summary>
        /// <param name="function">計算式</param>
        /// <param name="substitution">代入文の辞書化リスト</param>
        /// <returns></returns>
        private string replcaeSubstitution(string function, Dictionary<string, string> substitution)
        {
            List<string> args = getArg(function);
            while (0 < args.Count) {
                foreach (string arg in args) {
                    if (substitution.ContainsKey(arg)) {
                        function = function.Replace(arg, substitution[arg]);
                    } else {
                        return null;
                    }
                }
                args = getArg(function);
            }
            return function;
        }

        /// <summary>
        /// 計算式に含まれる代入文のリストを作成
        /// </summary>
        /// <param name="function">計算式</param>
        /// <returns>代入文リスト</returns>
        private List<string> getArg(string function)
        {
            string withoutArg = "[x][y][s][t]";
            List<string> args = new List<string>();
            int startIndex = 0;
            while (0 <= startIndex) {
                startIndex = function.IndexOf("[", startIndex);
                if (0 <= startIndex) {
                    int endIndex = function.IndexOf("]", startIndex);
                    string arg = function.Substring(startIndex, endIndex - startIndex + 1);
                    if (0 > withoutArg.IndexOf(arg))
                        args.Add(arg);
                    startIndex = endIndex;
                }
            }
            return args;
        }

        /// <summary>
        /// 計算式から保存キーワードを抽出する
        /// </summary>
        /// <param name="function">計算式</param>
        /// <returns>キーワード</returns>
        private string getFunctionKey(string function)
        {
            string[] key = function.Split(';');
            if (key[0][0] == '$') {
                return key[0].Trim();       //  タイトル/コメント
            } else if (0 <= key[0].IndexOf("[x]") || 0 <= key[0].IndexOf("[y]")) {
                return key[0].Trim();       //  陽関数
            } else if (2 < key.Length && (0 <= key[0].IndexOf("[s]") || 0 <= key[0].IndexOf("[t]"))) {
                return key[0].Trim() + ";" + key[1].Trim() + ";" + key[2].Trim();   //  パラメトリック関数
            }
            return function.Trim();
        }

        /// <summary>
        /// 文字列を計算式の種別に変換
        /// </summary>
        /// <param name="val">文字列</param>
        /// <returns>種別</returns>
        private FUNCTION_TYPE string2FunctionType(string val)
        {
            if (val.CompareTo(FUNCTION_TYPE.Normal.ToString()) == 0)
                return FUNCTION_TYPE.Normal;
            else if (val.CompareTo(FUNCTION_TYPE.Parametric.ToString()) == 0)
                return FUNCTION_TYPE.Parametric;
            else if (val.CompareTo(FUNCTION_TYPE.Polar.ToString()) == 0)
                return FUNCTION_TYPE.Polar;
            else
                return FUNCTION_TYPE.Non;
        }

        /// <summary>
        /// 計算式の種類を文字列に変換
        /// </summary>
        /// <param name="funcType">計算式の種類</param>
        /// <returns>文字列</returns>
        private string functionType2String(FUNCTION_TYPE funcType)
        {
            return funcType.ToString();
        }

        /// <summary>
        /// 関数リストをファイルから読み出す
        /// </summary>
        /// <param name="path">ファイル名</param>
        private void loadFuncList(string path)
        {
            List<string[]> funcList = ylib.loadCsvData(path, mFuncListTitle, true);
            if (funcList == null)
                return;
            if (mFuncList == null)
                mFuncList = new List<string[]>();
            else
                mFuncList.Clear();
            for (int i = 0; i < funcList.Count; i++) {
                if (funcList[i].Length < 2)
                    continue;
                string[] func = new string[funcList[i].Length];
                if (funcList[i][0].Length == 0) {
                    //  タイトル欄が存在しない時
                    if (funcList[i][1][0] == '$') {
                        func[0] = funcList[i][1].Substring(1, funcList[i][1].IndexOf(";") - 1);
                        func[1] = funcList[i][1].Substring(funcList[i][1].IndexOf(";") + 1);
                    } else {
                        func[0] = funcList[i][1];
                        func[1] = funcList[i][1];
                    }
                } else {
                    //  タイトル欄が存在する
                    func[0] = funcList[i][0];
                    func[1] = funcList[i][1];
                }
                for (int j = 2; j < funcList[i].Length; j++)
                    func[j] = funcList[i][j];
                mFuncList.Add(func);
            }
        }

        /// <summary>
        /// 関数リストをファイルに保存
        /// </summary>
        /// <param name="path">ファイル名</param>
        private void saveFuncList(string path)
        {
            ylib.saveCsvData(path, mFuncListTitle, mFuncList);
        }
    }
}
