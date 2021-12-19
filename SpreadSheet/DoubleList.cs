using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// 数値配列リスト(List<string[]>)の処理クラス</string>
    /// </summary>
    class DoubleList
    {
        public List<double[]> mData;                //  実数データ配列リスト
        public List<bool> mDisp = new List<bool>(); //  列単位の有効フラグリスト
        public List<Brush> mColor = new List<Brush>();
        public List<double> mScale = new List<double>();    //  項目データのスケール値
        public Rect mArea = new Rect();             //  配列リストの領域(最大最小値

        private YDrawingShapes ydraw = new YDrawingShapes();

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
            mData = new List<double[]>(data.mData);
            mDisp = new List<bool>(data.mDisp);
            mColor = new List<Brush>(data.mColor);
            mScale = new List<double>(data.mScale);
            mArea = new Rect(data.mArea.X, data.mArea.Y, data.mArea.Width, data.mArea.Height);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">数値データ</param>
        /// <param name="disp">表示可否リスト</param>
        /// <param name="color">色リスト</param>
        /// <param name="scale">スケールリスト</param>
        public DoubleList(List<double[]> data, List<bool> disp, List<Brush> color, List<double> scale)
        {
            mData = new List<double[]>(data);
            mDisp = new List<bool>(disp);
            mColor = new List<Brush>(color);
            mScale = new List<double>(scale);
        }

        /// <summary>
        /// 列単位の有効フラグの初期化
        /// </summary>
        private void initDispFlag()
        {
            mDisp.Clear();
            mColor.Clear();
            mScale.Clear();
            for (int i = 0; i < mData[0].Length; i++) {
                mDisp.Add(true);
                mColor.Add(ydraw.getColor15(i));
                mScale.Add(1.0);            }
        }

        /// <summary>
        /// データをコピーして取得
        /// </summary>
        /// <returns>コピーデータ</returns>
        public DoubleList fromCopy()
        {
            DoubleList doubleList = new DoubleList(mData, mDisp, mColor, mScale);
            return doubleList;
        }

        /// <summary>
        /// データを複製して設定する
        /// </summary>
        /// <param name="data">数値配列リスト</param>
        public void setData(List<double[]> data, List<bool> disp, List<Brush> color, List<double> scale)
        {
            mData = new List<double[]>(data);
            mDisp = new List<bool>(disp);
            mColor = new List<Brush>(color);
            mScale = new List<double>(scale);
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
        /// 累積値→増分値(内部保存して返す)
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
        /// 増分値→累積値(内部保存して返す)
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
        /// 移動平均データ(内部保存して返す)
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
        /// 表データの領域(最小最大値)を求める
        /// X:列,Y:行
        /// </summary>
        /// <param name="stack">積上げ式データ</param>
        /// <param name="sr">開始行</param>
        /// <param name="er">終了行</param>
        /// <param name="sc">開始列(省略時1列目)</param>
        /// <param name="ec">終了列</param>
        /// <returns>領域</returns>
        public Rect getArea(bool stack = false, int sr = 0, int er = -1, int sc = 1, int ec = -1)
        {
            er = er < 0 ? mData.Count : er + 1;
            ec = ec < 0 ? mData[0].Length : ec + 1;
            Rect area = new Rect();
            area.Y = mData[sr][0];
            area.X = mData[sr][sc];
            area.Height = 0.0;
            area.Width = 0.0;
            for (int i = sr; i < er; i++) {
                area.Y = Math.Min(area.Y, mData[i][0]);
                area.Height= Math.Max(area.Height, mData[i][0] - area.Y);
                double total = 0.0;
                for (int j = sc; j < ec; j++) {
                    if (mDisp[j]) {
                        area.X = Math.Min(area.X, mData[i][j]);
                        if (stack) {
                            total += mData[i][j];
                        } else {
                            area.Width = Math.Max(area.Width, mData[i][j] - area.X);
                        }
                    }
                }
                if (stack)
                    area.Width = Math.Max(area.Width, total - area.X);
            }
            return area;
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

        /// <summary>
        /// 移動平均データを求める
        /// </summary>
        /// <param name="srcData">数値データ配列</param>
        /// <param name="averageSize">移動平均のデータサイズ</param>
        /// <returns>変換後の数値データ配列</returns>
        public double[] movingAverage(double[] srcData, int averageSize = 7)
        {
            double[] destData = new double[srcData.Length];
            int sp = -averageSize / 2;
            int ep = averageSize + sp;
            for (int i= 0; i < srcData.Length; i++) {
                destData[i] = 0.0;
                for (int j = Math.Max(0, i + sp); j < Math.Min(srcData.Length, i + ep); j++)
                    destData[i] += srcData[j];
            }
            return destData;
        }
    }
}
