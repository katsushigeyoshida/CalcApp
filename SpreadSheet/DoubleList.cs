using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// 数値配列リスト(List<string[]>)の処理クラス</string>
    /// 機能
    /// 1) 指定列のデータをスケール倍する
    /// 2) 累積値→増分値(内部保存)
    /// 3) 増分値→累積値(内部保存)
    /// 4) 移動平均データ(内部保存)
    /// 5) 表データの領域(最小最大値)を求める
    /// 6) 行間の最小値を求める
    /// 7) 行と列の入れ替え
    /// </summary>
    class DoubleList
    {
        public List<double[]> mData;                        //  実数データ配列リスト
        public List<bool> mDisp = new List<bool>();         //  列単位の表示有効フラグリスト
        public string[] mDataTitle;                         //  データの種類名(凡例)
        public string[] mRowTitle;                          //  行タイトル
        public SheetData.DATATYPE mDataType;                //  列のデータタイプ
        public SheetData.DATATYPE mDataSubType;             //  列のデータタイプ
        public List<Brush> mColor = new List<Brush>();      //  列のカラーコード
        public List<double> mScale = new List<double>();    //  項目データのスケール値
        public List<double[]> mRegression = new List<double[]>();   //  回帰係数
        public Rect mArea = new Rect();                     //  配列リストの領域(最大最小値

        private YDrawingShapes ydraw = new YDrawingShapes();
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">数値配列リスト</param>
        public DoubleList(List<double[]> data)
        {
            mData = data;
            initDispFlag();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data"></param>
        public DoubleList(DoubleList data)
        {
            setData(data);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">数値データ</param>
        /// <param name="dataTitle">列タイトル</param>
        /// <param name="rowTitle">行タイトル</param>
        /// <param name="dataType">行タイトルのデータ種別</param>
        /// <param name="dataSubType">行タイトルのデータサブ種別</param>
        /// <param name="disp">表示可否リスト</param>
        /// <param name="color">色リスト</param>
        /// <param name="scale">スケールリスト</param>
        /// <param name="regression">回帰曲線データ</param>
        public DoubleList(List<double[]> data, string[] dataTitle, string[] rowTitle,
            SheetData.DATATYPE dataType, SheetData.DATATYPE dataSubType,
            List<bool> disp, List<Brush> color, List<double> scale, List<double[]> regression)
        {
            setData(data, dataTitle, rowTitle, dataType, dataSubType, disp, color, scale, regression);
        }

        /// <summary>
        /// 列単位の有効フラグの初期化
        /// </summary>
        private void initDispFlag()
        {
            mDisp.Clear();
            mColor.Clear();
            mScale.Clear();
            mRegression.Clear();
            for (int i = 0; i < mData[0].Length; i++) {
                mDisp.Add(true);
                mColor.Add(ydraw.getColor15(i));
                mScale.Add(1.0);
                mRegression.Add(null);
            }
        }

        /// <summary>
        /// データをコピーして取得
        /// </summary>
        /// <returns>コピーデータ</returns>
        public DoubleList fromCopy()
        {
            DoubleList doubleList = new DoubleList(mData, mDataTitle, mRowTitle,
                mDataType,mDataSubType, mDisp, mColor, mScale, mRegression);
            return doubleList;
        }

        /// <summary>
        /// データを複製して設定する
        /// </summary>
        /// <param name="data">数値配列データ</param>
        public void setData(DoubleList data)
        {
            mData = ListArrayCopy(data.mData);
            mDataTitle = new string[data.mDataTitle.Length];
            Array.Copy(data.mDataTitle, mDataTitle, data.mDataTitle.Length);
            mRowTitle = new string[data.mRowTitle.Length];
            Array.Copy(data.mRowTitle, mRowTitle, data.mRowTitle.Length);
            mDataType = data.mDataType;
            mDataSubType = data.mDataSubType;
            mDisp = new List<bool>(data.mDisp);
            mColor = new List<Brush>(data.mColor);
            mScale = new List<double>(data.mScale);
            mRegression = ListArrayCopy(data.mRegression);
            mArea = new Rect(data.mArea.X, data.mArea.Y, data.mArea.Width, data.mArea.Height);
        }

        /// <summary>
        /// データを複製して設定する
        /// </summary>
        /// <param name="data">数値データ</param>
        /// <param name="dataTitle">列タイトル</param>
        /// <param name="rowTitle">行タイトル</param>
        /// <param name="dataType">行タイトルのデータ種別</param>
        /// <param name="dataSubType">行タイトルのデータサブ種別</param>
        /// <param name="disp">表示可否リスト</param>
        /// <param name="color">色リスト</param>
        /// <param name="scale">スケールリスト</param>
        /// <param name="regression">回帰曲線データ</param>
        public void setData(List<double[]> data, string[] dataTitle, string[] rowTitle,
            SheetData.DATATYPE dataType, SheetData.DATATYPE dataSubType,
            List<bool> disp, List<Brush> color, List<double> scale, List<double[]> regression)
        {
            mData = ListArrayCopy(data);
            mDataTitle = new string[dataTitle.Length];
            Array.Copy(dataTitle, mDataTitle, dataTitle.Length);
            mRowTitle = new string[rowTitle.Length];
            Array.Copy(rowTitle, mRowTitle, rowTitle.Length);
            mDataType = dataType;
            mDataSubType = dataSubType;
            mDisp = new List<bool>(disp);
            mColor = new List<Brush>(color);
            mScale = new List<double>(scale);
            mRegression = ListArrayCopy(regression);
        }

        /// <summary>
        /// リスト配列データをディープコピー
        /// </summary>
        /// <param name="slist">ソース配列リスト</param>
        /// <returns>コピー配列リスト</returns>
        private List<double[]> ListArrayCopy(List<double[]> slist)
        {
            List<double[]> dlist = new List<double[]>();
            for (int i = 0; i < slist.Count; i++) {
                if (slist[i] == null) {
                    dlist.Add(null);
                } else {
                    double[] t = new double[slist[i].Length];
                    Array.Copy(slist[i], t, slist[i].Length);
                    dlist.Add(t);
                }
            }
            return dlist;
        }

        /// <summary>
        /// 指定列のデータをスケール倍する
        /// </summary>
        /// <param name="col">対象列</param>
        /// <param name="scale">倍率</param>
        /// <returns>変換後のデータ</returns>
        public void scaleedData(int col, double scale)
        {
            var oldScale = mScale[col];
            mScale[col] = scale;
            var relativeScale = scale / oldScale;
            List<double[]> doubleData = new List<double[]>();
            for (int i = 0; i < mData.Count; i++) {
                double[] dest = new double[mData[i].Length];
                for (int j = 0; j < mData[i].Length; j++) {
                    if (j == col) {
                        dest[j] = mData[i][j] * relativeScale;
                    } else {
                        dest[j] = mData[i][j];
                    }
                }
                doubleData.Add(dest);
            }
            mData = doubleData;
        }

        /// <summary>
        /// 回帰曲線の係数を求める
        /// </summary>
        /// <param name="col">対象列</param>
        public void setRegressionData(int col, bool regVariance = true)
        {
            List<Point> pointData = new List<Point>();
            for (int i = 0; i < mData.Count; i++) {
                pointData.Add(new Point(mData[i][0], mData[i][col]));
            }
            double a = ylib.getRegA(pointData);        //  係数a
            double b = ylib.getRegB(pointData);        //  係数b
            double[] regData = new double[] {
                a, b,
                ylib.getCorelation(pointData),                      //  相関係数
                ylib.getCoefficentDeterminatio(pointData, a, b),    //  決定係数
                regVariance ? ylib.getRegVariance(pointData, a, b) : 0.0    //  理論値に対する分散
            };
            mRegression[col] = regData;
        }

        /// <summary>
        /// 回帰曲線の係数を初期化(null)
        /// </summary>
        /// <param name="col"></param>
        public void clearRegressionData(int col)
        {
            mRegression[col] = null;
        }

        /// <summary>
        /// 回帰曲線の座標を求める
        /// </summary>
        /// <param name="col">対象列</param>
        /// <param name="x">Xの値</param>
        /// <param name="offset">Y方向のオフセット</param>
        /// <returns>座標</returns>
        public Point getRegressionData(int col, double x, double offset = 0)
        {
            double y = mRegression[col][0] * x + mRegression[col][1] + offset;
            return new Point(x, y);
        }

        /// <summary>
        /// 累積値→増分値(内部保存)
        /// </summary>
        /// <param name="sc">対象開始列</param>
        /// <param name="ec">対象終了列</param>
        /// <returns>変換後の数値配列リスト</returns>
        public void differntialData(int sc = 0, int ec = -1)
        {
            List<double[]> doubleData = new List<double[]>();
            ec = (ec < 0 || mData[0].Length <= ec) ? mData[0].Length - 1 : sc;
            for (int i = 0; i < mData.Count; i++) {
                double[] dest = new double[mData[i].Length];
                for (int j = 0; j < mData[i].Length; j++) {
                    if (0 < i && sc <= j && j <= ec) {
                        dest[j] = mData[i][j] - mData[i - 1][j];
                    } else {
                        dest[j] = mData[i][j];
                    }
                }
                doubleData.Add(dest);
            }
            mData = doubleData;
        }

        /// <summary>
        /// 増分値→累積値(内部保存)
        /// </summary>
        /// <param name="sc">対象開始列</param>
        /// <param name="ec">対象終了列</param>
        /// <returns>変換後の数値配列リスト</returns>
        public void accumulateData(int sc = 0, int ec = -1)
        {
            List<double[]> doubleData = new List<double[]>();
            ec = (ec < 0 || mData[0].Length <= ec) ? mData[0].Length - 1 : sc;
            for (int i = 0; i < mData.Count; i++) {
                double[] dest = new double[mData[i].Length];
                for (int j = 0; j < mData[i].Length; j++) {
                    if (0 < i && sc <= j && j <= ec) {
                        dest[j] = mData[i][j] + doubleData[i - 1][j];
                    } else {
                        dest[j] = mData[i][j];
                    }
                }
                doubleData.Add(dest);
            }
            mData = doubleData;
        }

        /// <summary>
        /// 移動平均データ(内部保存)
        /// </summary>
        /// <param name="aveSize">移動平均のデータサイズ</param>
        /// <param name="sc">対象開始列</param>
        /// <param name="ec">対象終了列</param>
        /// <returns>変換後の数値配列リスト</returns>
        public void smoothData(int aveSize = 7, int sc = 0, int ec = -1)
        {
            List<double[]> doubleData = new List<double[]>();
            List<double[]> transData = transeposeData(mData);
            ec = (ec < 0 || mData[0].Length <= ec) ? mData[0].Length - 1 : sc;
            for (int i = 0; i < transData.Count; i++) {
                if (sc <= i && i <= ec)
                    doubleData.Add(movingAverage(transData[i], aveSize));
                else
                    doubleData.Add(transData[i]);
            }
            mData = transeposeData(doubleData);
        }

        /// <summary>
        /// 一行の移動平均データを求める
        /// </summary>
        /// <param name="srcData">数値データ配列</param>
        /// <param name="averageSize">移動平均のデータサイズ</param>
        /// <returns>変換後の数値データ配列</returns>
        public double[] movingAverage(double[] srcData, int averageSize = 7)
        {
            double[] destData = new double[srcData.Length];
            int sp = -averageSize / 2;
            int ep = averageSize + sp;
            for (int i = 0; i < srcData.Length; i++) {
                destData[i] = 0.0;
                int count = 0;
                for (int j = Math.Max(0, i + sp); j < Math.Min(srcData.Length, i + ep); j++) {
                    destData[i] += srcData[j];
                    count++;
                }
                destData[i] /= count;
            }
            return destData;
        }

        /// <summary>
        /// 表データの領域(最小最大値)を求める
        /// X:列,Y:行
        /// </summary>
        /// <param name="stack">積上げ式データ</param>
        /// <param name="bar">棒グラフ</param>
        /// <param name="startRow">開始行</param>
        /// <param name="endRow">終了行</param>
        /// <param name="starCol">開始列(省略時1列目)</param>
        /// <param name="endCol">終了列</param>
        /// <returns>領域</returns>
        public Rect getArea(bool stack = false, bool bar = false, 
            int startRow = 0, int endRow = -1, int starCol = 1, int endCol = -1)
        {
            endRow = endRow < 0 ? mData.Count : endRow + 1;
            endCol = endCol < 0 ? mData[0].Length : endCol + 1;
            Rect area = new Rect();
            area.Y = mData[startRow][0];
            area.X = mData[startRow][starCol];
            area.Width = 0.0;
            double minX = mData[startRow][mDisp.IndexOf(true, starCol)];
            double maxX = minX; 
            for (int i = startRow; i < endRow; i++) {
                area.Y = Math.Min(area.Y, mData[i][0]);
                area.Height = Math.Max(area.Height, mData[i][0] - area.Y);
                double total = 0.0;
                for (int j = starCol; j < endCol; j++) {
                    if (mDisp[j]) {
                        area.X = Math.Min(area.X, mData[i][j]);
                        if (stack) {
                            total += mData[i][j];
                        } else {
                            minX = Math.Min(minX, mData[i][j]);
                            maxX = Math.Max(maxX, mData[i][j]);
                        }
                    }
                }
                if (stack) {
                    minX = Math.Min(minX, total);
                    maxX = Math.Max(maxX, total);
                }
            }
            area.X = minX;
            area.Width = maxX - minX;
            if (bar) {
                double barWidth = getMinmumRowDistance();
                area.Y -= barWidth / 2.0;
                area.Height += barWidth;
            }
            return area;
        }

        /// <summary>
        /// 行間の最小値を求める
        /// </summary>
        /// <returns>行間</returns>
        public double getMinmumRowDistance()
        {
            double minDis = 0.0;
            for (int i = 0; i < mData.Count - 1; i++) {
                double dis = mData[i + 1][0] - mData[i][0];
                if (0.0 < dis) {
                    if (0.0 < minDis)
                        minDis = Math.Min(dis, minDis);
                    else
                        minDis = dis;
                }
            }
            return minDis;
        }

        /// <summary>
        /// 行と列の入れ替え
        /// </summary>
        /// <param name="srcData">数値データリスト</param>
        /// <returns>数値データリスト</returns>
        public List<double[]> transeposeData(List<double[]> srcData)
        {
            List<double[]> doubleData = new List<double[]>();
            for (int i = 0; i < srcData[0].Length; i++) {
                double[] dest = new double[srcData.Count];
                for (int j = 0; j < srcData.Count; j++) {
                    dest[j] = srcData[j][i];
                }
                doubleData.Add(dest);
            }
            return doubleData;
        }
    }
}
