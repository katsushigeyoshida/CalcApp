using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// TableGraph.xaml の相互作用ロジック
    /// </summary>
    public partial class TableGraph : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private double mPrevWindowHeight;
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private YWorldShapes ydraw;                     //  グラフィックライブラリ
        private YLib mYlib = new YLib();                //  単なるライブラリ
        public SheetData mSheetData;                    //  グラフの元データ
        private DoubleList mDoubleList;                 //  グラフ用数値データクラス
        //private string[] mDataTitle;                    //  データの種類名
        private Stack<DoubleList> mStackDoubeData = new Stack<DoubleList>();    //  数値データスタック

        public string mWindowTitle="";
        private int mStartPos;                          //  表示開始位置
        private int mEndPos;                            //  表示終了位置
        private double mTextSize = 15;                  //  文字の大きさ
        private double mStepYsize;                      //  グラフの補助線の間隔
        private double mStepXsize;                      //  グラフの補助線の間隔
        private Rect mArea;                             //  グラフの表示領域
        private double mBarWidth;                       //  棒グラフの棒の幅
        //private SheetData.DATATYPE mDataType;           //  各列のデータタイプ
        //private SheetData.DATATYPE mDataSubType;        //  各列のデータタイプ

        public enum GRAPHTYPE {LINE_GRAPH, BAR_GRAPH, STACKEDLINE_GRAPH, STACKEDBAR_GRAPH}
        private GRAPHTYPE mGraphType = GRAPHTYPE.LINE_GRAPH;
        private string[] mGraphTypeTitle = { "折線", "棒グラフ", "積上げ式折線", "積上棒グラフ" };
        private string[] mGraphOperationTitle = {
            "増分→累積", "累積→増分", "スムージング", "元に戻す"
        };
        private int mMovingAveSize = 0;                 //  移動平均のデータ数
        private List<CheckBoxListItem> mLegendItems = new List<CheckBoxListItem>();
        private int mBackColor = 138;                   //  WhiteSmoke 141色の138番目
        private string[] mScaleFormat = new string[] { "#,##0", "#,##0.###", "0.######" };


        public TableGraph()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;
            WindowFormLoad();                   //  Windowの位置とサイズを復元

            ydraw = new YWorldShapes(canvas);

            //  グラフの種類設定
            CbGrphType.Items.Clear();
            CbGrphType.ItemsSource = mGraphTypeTitle;
            CbGrphType.SelectedIndex = 0;
            //  操作設定
            CbOperation.Items.Clear();
            CbOperation.ItemsSource = mGraphOperationTitle;
            CbOperation.SelectedIndex = -1;
            //  背景色の色種類の設定
            CbBackColor.Items.Clear();
            CbBackColor.ItemsSource = ydraw.getColorTitle(); ;
            CbBackColor.SelectedIndex = mBackColor;
            //  移動平均の設定
            CbMovingAve.Items.Clear();
            CbMovingAve.Items.Add("なし");
            for (int i = 2; i < 50; i++)
                CbMovingAve.Items.Add(i.ToString());
            CbMovingAve.SelectedIndex = 0;
        }

        /// <summary>
        /// Windowのロード8初期化処理)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = mWindowTitle;
            InitData();
            DrawGraph();
        }

        /// <summary>
        /// Window のサイズ変更時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (WindowState != mWindowState &&
                WindowState == WindowState.Maximized) {
                mWindowWidth = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (WindowState != mWindowState ||
                mWindowWidth != this.Width ||
                mWindowHeight != this.Height) {
                mWindowWidth = this.Width;
                mWindowHeight = this.Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = this.WindowState;
                return;
            }

            //  ウィンドウの大きさに合わせてコントロールの高さを変更する
            if (WindowState == mWindowState && WindowState == WindowState.Maximized)
                return;
            double dy = mWindowHeight - mPrevWindowHeight;
            LbTitleList.Height += dy;

            mWindowState = this.WindowState;
            mPrevWindowWidth = mWindowWidth;
            mPrevWindowHeight = mWindowHeight;

            DrawGraph();
        }

        /// <summary>
        /// Window のクローズ処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();           //  Window の位置とサイズを保存
        }


        /// <summary>
        /// Windowのサイズと位置を復元
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.TableGraphWindowWidth < 100 || Properties.Settings.Default.TableGraphWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.TableGraphWindowHeight) {
                Properties.Settings.Default.TableGraphWindowWidth = mWindowWidth;
                Properties.Settings.Default.TableGraphWindowHeight = mWindowHeight;
            } else {
                this.Top = Properties.Settings.Default.TableGraphWindowTop;
                this.Left = Properties.Settings.Default.TableGraphWindowLeft;
                this.Width = Properties.Settings.Default.TableGraphWindowWidth;
                this.Height = Properties.Settings.Default.TableGraphWindowHeight;
                double dy = this.Height - mWindowHeight;
            }
        }

        /// <summary>
        /// Windowのサイズと位置を保存
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.TableGraphWindowTop = this.Top;
            Properties.Settings.Default.TableGraphWindowLeft = this.Left;
            Properties.Settings.Default.TableGraphWindowWidth = this.Width;
            Properties.Settings.Default.TableGraphWindowHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// グラフの種類の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbGrphType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int n = CbGrphType.SelectedIndex;
            switch (n) {
                case 0: mGraphType = GRAPHTYPE.LINE_GRAPH; break;       //  折線
                case 1: mGraphType = GRAPHTYPE.BAR_GRAPH; break;        //  棒グラフ
                case 2: mGraphType = GRAPHTYPE.STACKEDLINE_GRAPH; break;//  積上げ折れ線
                case 3: mGraphType = GRAPHTYPE.STACKEDBAR_GRAPH; break; //  積上げ棒グラフ
            }

            DrawGraph();
        }

        /// <summary>
        /// 開始位置の設定む
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbStartPos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbStartPos.SelectedIndex < mEndPos) {
                mStartPos = CbStartPos.SelectedIndex;
                DrawGraph();
            } else {
                CbStartPos.SelectedIndex = mStartPos;
            }
        }

        /// <summary>
        /// 終了位置の設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbEndPos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (mStartPos < CbEndPos.SelectedIndex) {
                mEndPos = CbEndPos.SelectedIndex;
                DrawGraph();
            } else {
                CbEndPos.SelectedIndex = mEndPos;
            }
        }

        /// <summary>
        /// グラフデータ操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             switch(CbOperation.SelectedIndex) {
                case 0:         //  増分→累積
                    mStackDoubeData.Push(mDoubleList);
                    mDoubleList = mDoubleList.fromCopy();
                    mDoubleList.accumulateData(1);
                    break;
                case 1:         //  累積→増分
                    mStackDoubeData.Push(mDoubleList);
                    mDoubleList = mDoubleList.fromCopy();
                    mDoubleList.differntialData(1);
                    break;
                case 2:         //  スムージング
                    InputBox dlg = new InputBox();
                    dlg.mEditText = "7";
                    var result = dlg.ShowDialog();
                    if (result == true) {
                        mStackDoubeData.Push(mDoubleList);
                        mDoubleList = mDoubleList.fromCopy();
                        mDoubleList.smoothData(mYlib.intParse(dlg.mEditText), 1);
                    }
                    break;
                case 3:         //  元に戻す
                    if (0 < mStackDoubeData.Count)
                        mDoubleList = mStackDoubeData.Pop();
                    break;

            }
            DrawGraph();
            CbOperation.SelectedIndex = -1;
        }

        /// <summary>
        /// 移動平均の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbMovingAve_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 < CbMovingAve.SelectedIndex) {
                mMovingAveSize = (int)mYlib.string2double(CbMovingAve.Items[CbMovingAve.SelectedIndex].ToString());
            } else {
                mMovingAveSize = 0;
            }
            DrawGraph();
        }

        /// <summary>
        /// データ種別コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmCheckMenu_Click(object sender, RoutedEventArgs e)
        {
            int selItemNo = LbTitleList.SelectedIndex;
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CmAllCheck") == 0) {
                //  すべてにチェックをつける
                LbTitleList.ItemsSource = null;
                for (int i = 0; i < mLegendItems.Count; i++)
                    mLegendItems[i].Checked = true;
            } else if (menuItem.Name.CompareTo("CnAllNotCheck") == 0) {
                //  選択以外のすべてのチェックを外す
                LbTitleList.ItemsSource = null;
                for (int i = 0; i < mLegendItems.Count; i++)
                    if (i == selItemNo)
                        mLegendItems[i].Checked = true;
                    else
                        mLegendItems[i].Checked = false;
            } else if (menuItem.Name.CompareTo("CmReverseCheck") == 0) {
                //  チェックを反転する
                LbTitleList.ItemsSource = null;
                for (int i = 0; i < mLegendItems.Count; i++)
                    if (mLegendItems[i].Checked)
                        mLegendItems[i].Checked = false;
                    else
                        mLegendItems[i].Checked = true;
            } else if (menuItem.Name.CompareTo("CmColorSet") == 0) {
                SelectMenu selMenu = new SelectMenu();
                selMenu.Title = "色を選択";
                string[] menuList = ydraw.getColor15Title();
                selMenu.mMenuList = menuList;
                if (selMenu.ShowDialog() == true) {
                    mStackDoubeData.Push(mDoubleList);
                    mDoubleList = mDoubleList.fromCopy();
                    mDoubleList.mColor[selItemNo + 1] = ydraw.getColor15(selMenu.mSelectIndex);
                    DrawGraph();
                }
                return;
            } else if (menuItem.Name.CompareTo("CmScaleReset") == 0) {
                //  実値に戻す
                mStackDoubeData.Push(mDoubleList);
                mDoubleList = mDoubleList.fromCopy();
                mDoubleList.scaleedData(selItemNo + 1, 1.0);
                DrawGraph();
                return;
            } else if (menuItem.Name.CompareTo("CmScale") == 0) {
                InputBox dlg = new InputBox();
                dlg.Title = "スケール値の設定";
                dlg.mEditText = mDoubleList.mScale[selItemNo + 1].ToString();
                if (dlg.ShowDialog() == true) {
                    mStackDoubeData.Push(mDoubleList);
                    mDoubleList = mDoubleList.fromCopy();
                    mDoubleList.scaleedData(selItemNo + 1, mYlib.string2double(dlg.mEditText));
                    DrawGraph();
                }
                return;
            } else {
                return;
            }
            //  表示するものがないとエラーになるのでチェックが0の時は最初の項目にチェックを入れる
            if (useTitleCount() == 0)
                mLegendItems[0].Checked = true;
            //  再表示
            LbTitleList.Items.Clear();
            LbTitleList.ItemsSource = mLegendItems;
            DrawGraph();
        }

        /// <summary>
        /// 背景色の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbBackColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mBackColor = CbBackColor.SelectedIndex;
            DrawGraph();
        }

        /// <summary>
        /// データ名のチェックの入り切り処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (0 < useTitleCount())
                DrawGraph();
        }

        /// <summary>
        /// データ名のリストボックスをダブルクリックしたときにチェックを反転させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbTitleList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LbTitleList.ItemsSource = null;
            for (int i = 0; i < mLegendItems.Count; i++)
                if (mLegendItems[i].Checked)
                    mLegendItems[i].Checked = false;
                else
                    mLegendItems[i].Checked = true;
            if (useTitleCount() == 0)
                mLegendItems[0].Checked = true;
            LbTitleList.Items.Clear();
            LbTitleList.ItemsSource = mLegendItems;
            DrawGraph();
        }

        /// <summary>
        /// データの初期化(前処理)
        /// </summary>
        private void InitData()
        {
            //  データの前処理
            mSheetData.dataSqueeze();                                   //  1列目に不要なデータのある行は削除する
            if (mSheetData.getDataType(0) != SheetData.DATATYPE.STRING)
                mSheetData.Sort(0, true);                               //  縦軸タイトルが文字列以外はタイトル列目でソート
            mSheetData.stringTable2doubleTable();                       //  数値データに変換
            mDoubleList = new DoubleList(mSheetData.getDoubleTable());  //  グラフデータの取得
            mDoubleList.mDataTitle = mSheetData.getTitle();             //  データ列タイトル取得
            mDoubleList.mRowTitle = mSheetData.getRowData(0);           //  行タイトルの取得
            mDoubleList.mDataType  = mSheetData.getDataType(0);         //  列データの種類取得
            mDoubleList.mDataSubType = mSheetData.getDataSubType(0);    //  列データの種類取得

            //  開始位置と終了位置を設定
            CbStartPos.Items.Clear();
            CbStartPos.ItemsSource = mSheetData.getColumnData(0);
            CbStartPos.SelectedIndex = 0;
            CbEndPos.Items.Clear();
            CbEndPos.ItemsSource = mSheetData.getColumnData(0);
            CbEndPos.SelectedIndex = mSheetData.getDataSize() - 1;
            mStartPos = 0;
            mEndPos = mSheetData.getDataSize() - 1;

            //  凡例の選択リスト
            for(int i = 1; i < mDoubleList.mDataTitle.Length; i++) {
                mLegendItems.Add(new CheckBoxListItem(true, mDoubleList.mDataTitle[i]));
            }
            LbTitleList.Items.Clear();
            LbTitleList.ItemsSource = mLegendItems;
        }

        /// <summary>
        /// グラフの表示
        /// </summary>
        private void DrawGraph()
        {
            if (mDoubleList == null || mLegendItems.Count < 1 || useTitleCount() < 1)
                return;

            DrawInit();                                 //  初期データの設定
            if (mStepXsize <= 0 || mStepYsize <= 0)
                return;
            setGraphArea();                             //  グラフエリアの設定
            setAxis();                                  //  目盛り線の表示
            setChartLegend();                           //  凡例表示

            DrawGraphData();                            //  グラフデータを表示
        }

        /// <summary>
        /// グラフデータの表示
        /// </summary>
        private void DrawGraphData()
        {
            ydraw.setThickness(1);
            int startPos = mStartPos;
            //  棒グラフのバーの幅の設定
            double barWidth = mBarWidth;
            double barXOffset = 0;
            if (mGraphType == GRAPHTYPE.BAR_GRAPH) {
                barWidth *= 0.9;
                barXOffset = barWidth / 2;
                barWidth /= useTitleCount();
                startPos--;
            } else if (mGraphType == GRAPHTYPE.STACKEDBAR_GRAPH) {
                barWidth *= 0.8;
                barXOffset = barWidth / 2;
                startPos--;
            }

            //  グラフデータの表示
            for (int i = startPos; i < mEndPos; i++) {
                double[] ps = mDoubleList.mData[i < 0 ? 0 : i];
                double[] pe = mDoubleList.mData[i + 1];
                double ys = mArea.Y < 0 ? 0 : mArea.Y;
                double ye = 0;
                int n = 0;
                for (int j = 1; j < mDoubleList.mDataTitle.Length; j++) {
                    //  データの種別確認
                    if (!mLegendItems[j - 1].Checked)
                        continue;
                    if (mGraphType == GRAPHTYPE.LINE_GRAPH) {
                        //  折れ線グラフ
                        ydraw.setColor(mDoubleList.mColor[j]);
                        ydraw.drawLine(ps[0], ps[j], pe[0], pe[j]);
                    } else if (mGraphType == GRAPHTYPE.STACKEDLINE_GRAPH) {
                        //  積上げ式折線グラフ
                        ys += ps[j];
                        ye += pe[j];
                        ydraw.setColor(mDoubleList.mColor[j]);
                        ydraw.drawLine(ps[0], ys, pe[0], ye);
                    } else if (mGraphType == GRAPHTYPE.BAR_GRAPH) {
                        //  棒グラフ
                        ye = pe[j];
                        if (200 < (mEndPos - mStartPos) * mDoubleList.mDataTitle.Length)
                            ydraw.setColor(mDoubleList.mColor[j]);
                        ydraw.setFillColor(mDoubleList.mColor[j]);
                        ydraw.drawRectangle(new Point(pe[0] - barXOffset + barWidth * n, ys),
                            new Point(pe[0] - barXOffset + barWidth * (n + 1), ye), 0);
                        n++;
                    } else if (mGraphType == GRAPHTYPE.STACKEDBAR_GRAPH) {
                        //  積上げ式棒グラフ
                        ye += pe[j];
                        if (200 < (mEndPos - mStartPos))
                            ydraw.setColor(mDoubleList.mColor[j]);
                        ydraw.setFillColor(mDoubleList.mColor[j]);
                        ydraw.drawRectangle(new Point(pe[0] - barXOffset, ys),
                            new Point(pe[0] - barXOffset + barWidth, ye), 0);
                        ys = ye;
                    }
                }
            }
            if (1 < mMovingAveSize) {
                //  移動平均の折線追加
                if (mGraphType == GRAPHTYPE.BAR_GRAPH) {
                    for (int j = 1; j < mDoubleList.mDataTitle.Length; j++) {
                        if (!mLegendItems[j - 1].Checked)
                            continue;
                        double[] barData = getBarData(j);
                        ydraw.setThickness(2);
                        ydraw.setColor(mDoubleList.mColor[j]);
                        DrawMovingAverage(barData, mMovingAveSize);
                    }
                } else if (mGraphType == GRAPHTYPE.STACKEDBAR_GRAPH) {
                    //  積上げ式棒グラフの時は累積値のみ表示
                    double[] stackedData = getStackedData();
                    ydraw.setThickness(3);
                    ydraw.setColor(Brushes.OrangeRed);
                    DrawMovingAverage(stackedData, mMovingAveSize);
                }
            }
        }

        /// <summary>
        /// 移動平均を折線で表示
        /// </summary>
        /// <param name="data">データ列</param>
        /// <param name="aveSize">平均値のデータ数</param>
        private void DrawMovingAverage(double[] data, int aveSize)
        {
            double[] movingAveData = mYlib.getMovingAverage(data, aveSize, false);
            for (int i = mStartPos; i < mEndPos; i++) {
                double ps = mDoubleList.mData[i][0];
                double pe = mDoubleList.mData[i + 1][0];
                ydraw.drawLine(ps, movingAveData[i], pe, movingAveData[i + 1]);
            }
        }

        /// <summary>
        /// 列ごとのデータを配列で取得
        /// </summary>
        /// <param name="n">列番号</param>
        /// <returns>データ配列</returns>
        private double[] getBarData(int n)
        {
            double[] barData = new double[mDoubleList.mData.Count];

            for (int i = 0; i < mDoubleList.mData.Count; i++) {
                barData[i] = mDoubleList.mData[i][n];
            }
            return barData;

        }

        /// <summary>
        /// 積上げデータを求める
        /// </summary>
        /// <returns>積上げ値のデータ配列</returns>
        private double[] getStackedData()
        {
            double[] stackedData = new double[mDoubleList.mData.Count];

            for (int i= 0; i < mDoubleList.mData.Count; i++) {
                stackedData[i] = 0;
                for (int j = 1; j < mDoubleList.mDataTitle.Length; j++) {
                    if (!mLegendItems[j - 1].Checked)
                        continue;
                    stackedData[i] += mDoubleList.mData[i][j];
                 }
            }
            return stackedData;
        }


        /// <summary>
        /// 初期データの設定
        /// </summary>
        private void DrawInit()
        {
            //  Window領域の設定
            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);

            //  タイトル列の使用の有無を設定
            for (int i = 0; i < mLegendItems.Count; i++)
                mDoubleList.mDisp[i + 1] = mLegendItems[i].Checked;

            //  グラフ領域の設定
            bool stack = mGraphType == GRAPHTYPE.STACKEDLINE_GRAPH || mGraphType == GRAPHTYPE.STACKEDBAR_GRAPH;
            bool bar = mGraphType == GRAPHTYPE.BAR_GRAPH || mGraphType == GRAPHTYPE.STACKEDBAR_GRAPH;
            mArea = mDoubleList.getArea(stack, bar, mStartPos, mEndPos);
            mArea = new Rect(mArea.Y, mArea.X, mArea.Height, mArea.Width);  //  領域データの縦横を反転
            double y = mArea.Y;
            if (0 < mArea.Y) {
                //  基底を0に設定
                mArea.Y = 0;
                mArea.Height += y - mArea.Y;
            }

            //  補助線の間隔を設定
            mStepYsize = mYlib.graphStepSize(mArea.Height, 5);  //  縦軸目盛り線の間隔
            mStepXsize = mYlib.graphStepSize(mArea.Width, 10);  //  横軸目盛り線の間隔

            //  グラフ高さの調整
            if (mArea.Y < 0) {
                //  基底が負の時の下限値
                mArea.Y = ((int)(mArea.Y / mStepYsize) - 1) * mStepYsize;
                mArea.Height += y - mArea.Y;
            }

            //  上部のマージンを追加
            int n = 2;
            while (mArea.Height > mStepYsize * n++) ;
            mArea.Height = mStepYsize * n;

            //  アスペクト比無効
            ydraw.setAspectFix(false);
        }


        /// <summary>
        /// グラフエリアと領域のマージンを設定
        /// </summary>
        private void setGraphArea()
        {
            //  グラフエリアの仮設定
            ydraw.setWorldWindow(mArea.X, mArea.Y, mArea.Right, mArea.Y - mArea.Height);

            //  グラフエリアのマージンを求める
            ydraw.setScreenTextSize(mTextSize);
            double leftMargine   = 0;
            double bottomMargine = 0;
            double rightMargine = Math.Abs(30 / ydraw.world2screenXlength(1));
            double topMargine  = Math.Abs(30 / ydraw.world2screenYlength(1));
            int scaleFormatType = mArea.Height > 100 ? 0 : (mArea.Height > 1 ? 1 : 2);

            //  縦軸の目盛り文字列の最大幅を求める(leftMargine)
            for (double y = mArea.Y; y <= mArea.Y + mArea.Height; y += mStepYsize) {
                Size size = ydraw.measureText(y.ToString(mScaleFormat[scaleFormatType]));
                leftMargine = Math.Max(leftMargine, size.Width);
            }
            leftMargine += rightMargine;
            //  横軸の目盛り文字列の最大幅を求める(bottomMargine)
            for (double x = mArea.X; x <= mArea.X + mArea.Width; x += mStepXsize) {
                if (mDoubleList.mDataType != SheetData.DATATYPE.STRING || x < mSheetData.getDataSize()) {
                    Size size = ydraw.measureText(YTitle2String(x, mDoubleList.mDataType));
                    bottomMargine = Math.Max(bottomMargine, size.Width);
                }
            }
            bottomMargine = Math.Abs(ydraw.screen2worldYlength(ydraw.world2screenXlength(bottomMargine)));
            bottomMargine += topMargine;

            //  棒グラフのバー幅
            mBarWidth = mDoubleList.getMinmumRowDistance();

            //  グラフエリアの設定
            ydraw.setWorldWindow(
                mArea.X - leftMargine, mArea.Y + mArea.Height + topMargine,
                mArea.X + mArea.Width + rightMargine, mArea.Y - bottomMargine);
        }

        /// <summary>
        /// 目盛と補助線の表示
        /// </summary>
        private void setAxis()
        {
            int scaleFormatType = mArea.Height > 100 ? 0 : (mArea.Height > 1 ? 1 : 2);
            ydraw.backColor(ydraw.getColor(mBackColor));
            ydraw.setScreenTextSize(mTextSize);
            ydraw.setThickness(1);
            
            ydraw.clear();
            //  縦軸の目盛りと補助線の表示
            ydraw.setColor(Brushes.Gray);
            for (double y = mArea.Y; y <= mArea.Y + mArea.Height; y += mStepYsize) {
                //  補助線
                ydraw.drawLine(mArea.X, y, mArea.X + mArea.Width, y);
                //  目盛
                ydraw.drawText(y.ToString(mScaleFormat[scaleFormatType]), 
                    new Point(mArea.X, y), 0, 
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            //  横軸軸の目盛りと補助線の表示
            ydraw.setColor(Brushes.Aqua);
            double textMargine = Math.Abs(3 / ydraw.world2screenYlength(1));            //  目盛上マージン
            double textDistance = Math.Abs(mTextSize / ydraw.world2screenXlength(1));   //  目盛最低間隔(距離)
            double axisLine = mDoubleList.mData[mStartPos][0];                          //  補助線位置
            for (int i = mStartPos; i <= mEndPos; i++) {
                double x = mDoubleList.mData[i][0];
                //  補助線表示
                if (axisLine <= x || i == mEndPos)
                    ydraw.drawLine(x, mArea.Y, x, mArea.Y + mArea.Height);
                //  目盛表示
                if (axisLine <= x && textDistance < mDoubleList.mData[mEndPos][0] - x   //  指定ステップ
                    || i == mEndPos                                                     //  最終位置
                    || (mDoubleList.mDataType == SheetData.DATATYPE.STRING && x < mSheetData.getDataSize()) //  目盛種別が文字
                    //|| mDoubleList.mDataType == SheetData.DATATYPE.WEEKDAY                                //  目盛種別が曜日
                    )
                    ydraw.drawText(YTitle2String(x, mDoubleList.mDataType), new Point(x, mArea.Y - textMargine),
                        -Math.PI / 2, HorizontalAlignment.Left, VerticalAlignment.Center);
                if (axisLine <= x)
                    axisLine += mStepXsize;
            }

            //  グラフ枠の表示
            ydraw.setColor(Brushes.Black);
            ydraw.setFillColor(null);
            ydraw.drawRectangle(mArea.X, mArea.Y + mArea.Height, mArea.Width, mArea.Height, 0);
        }

        /// <summary>
        /// 数値をデータタイプに合わせて文字列にする
        /// </summary>
        /// <param name="x">数値</param>
        /// <param name="type">データタイプ</param>
        /// <returns>文字列</returns>
        private string YTitle2String(double x, SheetData.DATATYPE type)
        {
            string xTitle;
            if (type == SheetData.DATATYPE.DATE)
                xTitle = mYlib.JulianDay2DateYear((int)x, true);
            else if (type == SheetData.DATATYPE.WEEKDAY)
                xTitle = mYlib.getWeekday((int)x, 2);
            else if (type == SheetData.DATATYPE.STRING)
                xTitle = mDoubleList.mRowTitle[(int)x];
            else
                xTitle = x.ToString();
            return xTitle;
        }

        /// <summary>
        /// 凡例表示
        /// </summary>
        private void setChartLegend()
        {
            if (mLegendItems == null || mLegendItems.Count < 1)
                return;
            Rect rect = new Rect();
            rect.X = mArea.X + Math.Abs(mTextSize / ydraw.world2screenXlength(1));
            rect.Y = mArea.Y + mArea.Height - Math.Abs(10 / ydraw.world2screenYlength(1));
            rect.Width = Math.Abs(10 / ydraw.world2screenXlength(1));
            rect.Height = Math.Abs(10 / ydraw.world2screenYlength(1));

            for (int i = mDoubleList.mDataTitle.Length - 1; 0 < i ; i--) {
                if (mLegendItems[i - 1].Checked) {
                    ydraw.setFillColor(mDoubleList.mColor[i]);
                    ydraw.drawRectangle(rect, 0);
                    string title = mDoubleList.mDataTitle[i];
                    if (1 != mDoubleList.mScale[i])
                        title += "(" + mDoubleList.mScale[i] + "培値)";
                    ydraw.drawText(title, rect.Right + Math.Abs(2 / ydraw.world2screenXlength(1)),
                        rect.Y + Math.Abs(mTextSize / 2 / ydraw.world2screenYlength(1)), 0);
                    rect.Y -= Math.Abs(mTextSize / ydraw.world2screenYlength(1));
                }
            }
        }

        /// <summary>
        /// 有効なタイトル(凡例)数の数
        /// </summary>
        /// <returns></returns>
        private int useTitleCount()
        {
            int use = 0;
            for (int i = 0; i < mLegendItems.Count; i++)
                if (mLegendItems[i].Checked)
                    use++;
            return use;
        }
    }
}
