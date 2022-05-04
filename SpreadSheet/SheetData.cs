using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfLib;

namespace CalcApp
{
    public class SheetData
    {
        public enum DATATYPE { NON, STRING, NUMBER, DATE, WEEK, MONTH, YEAR, TIME, WEEKDAY }; //  データの種別

        private List<string[]> mData;       //  ファイルデータ
        private string[] mDataTitle;        //  ファイルデータのタイトル
        private DATATYPE[] mDataType;       //  データタイプ
        private DATATYPE[] mDataSubType;    //  データサブタイプ
        private List<double[]> mDoubleData; //  数値データ(0列めは縦軸データ)
        private Rect mArea;                 //  数値データの領域

        private bool mError = false;
        private string mErrorMessage;

        private YLib ylib = new YLib();

        public SheetData()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">リストデータ</param>
        /// <param name="title">データタイトル</param>
        public SheetData(List<string[]> data, string[] title)
        {
            mData = data;                                   //  データリスト
            mDataTitle = setTitle(title);                   //  タイトル行
            mDataType = new DATATYPE[mDataTitle.Length];    //  列のデータタイプ
            mDataSubType = new DATATYPE[mDataTitle.Length]; //  列の詳細データタイプ
            //  初期値設定
            for (int i = 0; i < mDataTitle.Length; i++) {
                mDataSubType[i] = getDataType(mData, i);
                mDataType[i] = (mDataSubType[i] == DATATYPE.WEEK || mDataSubType[i] == DATATYPE.MONTH
                    || mDataSubType[i] == DATATYPE.YEAR) ? DATATYPE.DATE : mDataSubType[i];
            }
            stringTable2doubleTable();
            dataCheck();                                    //  データを行単位でサイズチェック
        }

        /// <summary>
        /// タイトルの設定(タイトルのコピー作成)
        /// タイトル数がデータの最大列数に合わせる
        /// </summary>
        /// <param name="title">元タイトル</param>
        /// <returns>コピータイトル</returns>
        private string[] setTitle(string[] title)
        {
            int maxColumn = getMaxColumnSize();
            if (title.Length < maxColumn) {
                string[] dataTitle = new string[maxColumn];
                for (int i = 0; i < maxColumn; i++) {
                    if (i < title.Length)
                        dataTitle[i] = title[i];
                    else
                        dataTitle[i] = "";
                }
                return dataTitle;
            } else {
                return title;                             //  タイトル行
            }
        }

        /// <summary>
        /// 元データの中で配列サイズがタイトルの配列サイズより小さいものがあれば
        /// 空白データを追加する
        /// </summary>
        private void dataCheck()
        {
            for (int i = 0; i < mData.Count; i++) {
                if (mData[i].Length < mDataTitle.Length) {
                    string[] dummy = new string[mDataTitle.Length];
                    for (int j = 0; j < mDataTitle.Length; j++) {
                        if (j < mData[i].Length) {
                            dummy[j] = mData[i][j];
                        } else {
                            dummy[j] = "";
                        }
                    }
                    mData[i] = dummy;
                }
            }
        }

        /// <summary>
        /// ERRORの発生を取得
        /// 取得後はERRORを解除
        /// </summary>
        /// <returns></returns>
        public bool getError()
        {
            bool error = mError;
            mError = false;
            return error;
        }

        /// <summary>
        /// ERROR Messageの取得、
        /// </summary>
        /// <returns></returns>
        public string getErrorMessage()
        {
            return mErrorMessage;
        }

        /// <summary>
        /// シートデータを取得
        /// </summary>
        /// <returns></returns>
        public SheetData getSheetData()
        {
            SheetData destData = new SheetData();
            destData.mData = mData;
            destData.mDataTitle = mDataTitle;
            //destData.mDataScale = mDataScale;
            destData.mDataType = mDataType;
            destData.mDataSubType = mDataSubType;
            destData.mDoubleData = mDoubleData;
            destData.mArea = mArea;
            return destData;
        }

        /// <summary>
        /// シートデータをコピーして取得
        /// </summary>
        /// <returns></returns>
        public SheetData copy2SheetData()
        {
            SheetData destData = new SheetData();
            destData.mData = new List<string[]>();
            foreach (string[] data in mData) {
                string[] dest = new string[data.Length];
                Array.Copy(data, dest, data.Length);
                destData.mData.Add(dest);
            }
            destData.mDataTitle = new string[mDataTitle.Length];
            Array.Copy(mDataTitle, destData.mDataTitle, mDataTitle.Length);
            destData.mDataType = new DATATYPE[mDataType.Length];
            Array.Copy(mDataType, destData.mDataType, mDataType.Length);
            destData.mDataSubType = new DATATYPE[mDataSubType.Length];
            Array.Copy(mDataSubType, destData.mDataSubType, mDataSubType.Length);
            destData.mArea = new Rect(mArea.Location, mArea.Size);
            return destData;
        }

        /// <summary>
        /// リストデータサイズ
        /// </summary>
        /// <returns>サイズ(行数)</returns>
        public int getDataSize()
        {
            return mData.Count;
        }

        /// <summary>
        /// n行目のデータを配列で取得
        /// </summary>
        /// <param name="n">行</param>
        /// <returns>配列データ</returns>
        public string[] getData(int n)
        {
            if (0 <= n && n < mData.Count)
                return mData[n];
            else
                return null;
        }

        /// <summary>
        /// n列目のデータを配列で取得
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public string[] getColumnData(int n)
        {
            return getColumnData(n, -1);
        }

        /// <summary>
        /// n列目のデータを配列で取得
        /// </summary>
        /// <param name="n">n列目</param>
        /// <param name="m">取得最大行(0>の時はすべて)</param>
        /// <returns></returns>
        public string[] getColumnData(int n, int m)
        {
            m = m < 0 ? mData.Count : Math.Min(m, mData.Count);
            string[] columnData = new string[m];
            for (int i = 0; i < m; i++) {
                columnData[i] = mData[i][n];
            }
            return columnData;
        }

        /// <summary>
        /// データ行で最大の列数を求める
        /// </summary>
        /// <returns>列数</returns>
        private int getMaxColumnSize()
        {
            int maxColumnSize = 0;
            foreach (string[] data in mData)
                maxColumnSize = Math.Max(data.Length, maxColumnSize);
            return maxColumnSize;
        }

        /// <summary>
        /// リストデータ全体
        /// </summary>
        /// <returns>リストデータ</returns>
        public List<string[]> getData()
        {
            return mData;
        }

        /// <summary>
        /// データを検索して行を返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int indexOfData(string[] data)
        {
            return mData.IndexOf(data);
        }

        /// <summary>
        /// タイトル数の取得
        /// </summary>
        /// <returns>タイトル数</returns>
        public int getTitleSize()
        {
            return mDataTitle.Length;
        }

        /// <summary>
        /// 列タイトルの取得
        /// </summary>
        /// <returns>タイトル配列</returns>
        public string[] getTitle()
        {
            return mDataTitle;
        }

        /// <summary>
        /// 指定列のデータを取得する
        /// </summary>
        /// <param name="col">対象列</param>
        /// <returns>データ配列</returns>
        public string[] getRowData(int col)
        {
            string[] rowData = new string[mData.Count];
            for (int i = 0; i < mData.Count; i++)
                rowData[i] = mData[i][col];
            return rowData;
        }

        /// <summary>
        /// データの種別の取得
        /// </summary>
        /// <returns></returns>
        public DATATYPE[] getDataType()
        {
            return mDataType;
        }

        /// <summary>
        /// データの種別の取得
        /// </summary>
        /// <param name="n">列番</param>
        /// <returns></returns>
        public DATATYPE getDataType(int n)
        {
            return mDataType[n];
        }

        /// <summary>
        /// データの種別(詳細)を取得
        /// DATE → YEAR,MONTH
        /// </summary>
        /// <param name="n">列番</param>
        /// <returns></returns>
        public DATATYPE getDataSubType(int n)
        {
            return mDataSubType[n];
        }

        /// <summary>
        /// 列ごとのデータの種類を設定
        /// </summary>
        /// <param name="dataList">データ</param>
        /// <param name="n">データの列番</param>
        /// <returns>データタイプの配列</returns>
        private DATATYPE getDataType(List<string[]> dataList, int n)
        {
            DATATYPE dataType = DATATYPE.NON;
            int count = 0;
            foreach (string[] data in dataList) {
                if (1000 < count++)                 //  データ数が多いと時間がかかるので1000件程度で打ち切る
                    break;
                if (n < data.Length && 0 < data[n].Length) {
                    if (ylib.IsDateString(data[n])) {
                        //  日付(yyyy年mm月dd日,yyyy/mm/dd,yyyy-mm-dd,dd/mm/yyyy)
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.DATE;
                        else if (dataType == DATATYPE.DATE)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsDateWeekString(data[n])) {
                        //  日付(yyyy年mm月ww週,yyyy年ww週)
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.WEEK;
                        else if (dataType == DATATYPE.WEEK)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsDateMonthString(data[n])) {
                        //  日付(yyyy年mm月)
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.MONTH;
                        else if (dataType == DATATYPE.MONTH)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsDateYearString(data[n])) {
                        //  日付(yyyy年)
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.YEAR;
                        else if (dataType == DATATYPE.YEAR)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsWeekday(data[n])) {
                        //  曜日
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.WEEKDAY;
                        else if (dataType == DATATYPE.WEEKDAY)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsTime(data[n])) {
                        //  時間
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.TIME;
                        else if (dataType == DATATYPE.TIME)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else if (ylib.IsNumberString(data[n])) {
                        //  数値
                        if (dataType == DATATYPE.NON)
                            dataType = DATATYPE.NUMBER;
                        else if (dataType == DATATYPE.NUMBER)
                            continue;
                        else
                            return DATATYPE.STRING;
                    } else {
                        //  文字列
                        return DATATYPE.STRING;
                    }
                }
            }
            if (dataType == DATATYPE.WEEK || dataType == DATATYPE.MONTH || dataType == DATATYPE.YEAR)
                dataType = DATATYPE.DATE;
            return dataType;
        }

        /// <summary>
        /// 縦軸タイトルにデータのない行を除外する
        /// </summary>
        public void dataSqueeze()
        {
            for (int i = mData.Count - 1; 0 <= i; i--) {
                if (mDataType[0] != DATATYPE.STRING)
                    if (mData[i][0].Length < 1)
                        mData.RemoveAt(i);
            }
        }

        /// <summary>
        /// データのソート
        /// </summary>
        /// <param name="n">ソート列</param>
        /// <param name="ascending">ソートの向き</param>
        public void Sort(int n, bool ascending)
        {
            if (0 <= n && n < mDataType.Length) {
                if (ascending) {
                    if (mDataType[n] == DATATYPE.NUMBER)
                        mData.Sort((a, b) => Math.Sign(ylib.string2Double(a[n]) - ylib.string2Double(b[n])));
                    else if (mDataType[n] == DATATYPE.DATE)
                        mData.Sort((a, b) => (int)(ylib.date2JulianDay(a[n]) - ylib.date2JulianDay(b[n])));
                    else if (mDataType[n] == DATATYPE.WEEKDAY)
                        mData.Sort((a, b) => ylib.WeekNo(a[n]) - ylib.WeekNo(b[n]));
                    else
                        mData.Sort((a, b) => a[n].CompareTo(b[n]));
                } else {
                    if (mDataType[n] == DATATYPE.NUMBER)
                        mData.Sort((a, b) => Math.Sign(ylib.string2Double(b[n]) - ylib.string2Double(a[n])));
                    else if (mDataType[n] == DATATYPE.DATE)
                        mData.Sort((a, b) => (int)(ylib.date2JulianDay(b[n]) - ylib.date2JulianDay(a[n])));
                    else if (mDataType[n] == DATATYPE.WEEKDAY)
                        mData.Sort((a, b) => ylib.WeekNo(b[n]) - ylib.WeekNo(a[n]));
                    else
                        mData.Sort((a, b) => b[n].CompareTo(a[n]));
                }
            }
        }

        /// <summary>
        /// 指定された行(配列)以外を削除して新たなシートテーブルを作成
        /// </summary>
        /// <param name="lines">削除しない行の配列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData NotRemoveDataTable(int[] lines)
        {
            if (0 < lines.Length) {
                string[] title = mDataTitle;
                List<string[]> data = new List<string[]>();
                for (int i = 0; i < mData.Count; i++) {
                    if (0 <= Array.IndexOf(lines, i))
                        data.Add(mData[i]);
                }
                SheetData sheetData = new SheetData(data, title);
                return sheetData;
            }
            return null;
        }

        /// <summary>
        /// 指定された行をデータテーブルから削除して新たなシートテーブルを作成
        /// </summary>
        /// <param name="lines">削除する行の配列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData RemoveDataTable(int[] lines)
        {
            if (0 < lines.Length) {
                string[] title = mDataTitle;
                List<string[]> data = new List<string[]>();
                for (int i = 0; i < mData.Count; i++) {
                    if (0 > Array.IndexOf(lines, i))
                        data.Add(mData[i]);
                }
                SheetData sheetData = new SheetData(data, title);
                return sheetData;
            }
            return null;
        }

        /// <summary>
        /// 指定行と次の行のデータをマージしてデータを作る
        /// </summary>
        /// <param name="n">行番号</param>
        /// <returns>テーブルデータ</returns>
        public SheetData MergeNextLine(int n, int m)
        {
            List<string[]> data = new List<string[]>();
            string[] title = new string[mDataTitle.Length];
            if (n == 0 && m ==0) {
                for (int i = 0; i < mDataTitle.Length; i++) {
                    title[i] = mDataTitle[i] + (0 < mData[0][i].Length ? " " : "") + mData[0][i];
                }
                for (int i = 1; i < mData.Count; i++) {
                    data.Add(mData[i]);
                }
            } else {
                for (int i = 0; i < mDataTitle.Length; i++) {
                    title[i] = mDataTitle[i];
                }
                for (int i = 0; i < mData.Count; i++) {
                    if (i == n) {
                        string[] mergedata = new string[mData[i].Length];
                        for (int j = 0; j < mData[i].Length; j++) {
                            mergedata[j] = mData[i][j];
                        }
                        for (int k = 0; k < m; k++) {
                            for (int j = 0; j < mData[i].Length; j++) {
                                mergedata[j] += (0 < mData[i][j].Length ? " " : "") + mData[i + 1][j];
                            }
                            i++;
                        }
                        data.Add(mergedata);
                    } else {
                        data.Add(mData[i]);
                    }
                }
            }
            SheetData sheetData = new SheetData(data, title);
            return sheetData;
        }

        /// <summary>
        /// タイトル行を変更する
        /// </summary>
        /// <param name="n">新たなタイトル行</param>
        /// <returns>テーブルデータ</returns>
        public SheetData ChangeTitleLine(int n)
        {
            if (n < mData.Count) {
                string[] title = mData[n];
                List<string[]> data = new List<string[]>();
                for (int i = n + 1; i < mData.Count; i++)
                    data.Add(mData[i]);
                SheetData sheetData = new SheetData(data, title);
                return sheetData;
            }
            return null;
        }

        /// <summary>
        /// 集計データの作成
        /// 
        /// </summary>
        /// <param name="XTitle">集計対象の横軸データ</param>
        /// <param name="YTitle">集計対象の縦軸データ</param>
        /// <param name="dataTitle">集計データ値項目</param>
        /// <returns>テーブルデータ</returns>
        public SheetData PivotTable(string XTitle, string YTitle, string dataTitle)
        {
            string[] destTitle = makeTitle(XTitle, YTitle);
            if (destTitle != null) {
                List<string[]> destData = makeTable(destTitle, XTitle, YTitle, dataTitle);
                SheetData sheetData = new SheetData(destData, destTitle);
                return sheetData;
            }
            return null;
        }

        /// <summary>
        /// 縦方向に左端データでまとめる(数値データを累積する)
        /// </summary>
        /// <returns>テーブルデータ</returns>
        public SheetData SqueezeTable()
        {
            Dictionary<string, string[]> sqeezeDictionary = new Dictionary<string, string[]>();
            for (int i = 0; i < mData.Count; i++) {
                if (sqeezeDictionary.ContainsKey(mData[i][0])) {
                    for (int j = 1; j < mData[i].Length; j++) {
                        if (mDataType[j] == DATATYPE.NUMBER) {
                            double temp = ylib.string2double(sqeezeDictionary[mData[i][0]][j]) + ylib.string2double(mData[i][j]);
                            sqeezeDictionary[mData[i][0]][j] = temp.ToString("#,##0");
                        }
                    }
                } else {
                    sqeezeDictionary.Add(mData[i][0], mData[i].ToArray());
                }
            }
            List<string[]> squeezeTable = new List<string[]>();
            foreach (string[] data in sqeezeDictionary.Values) {
                squeezeTable.Add(data);
            }
            SheetData sheetData = new SheetData(squeezeTable, mDataTitle.ToArray());
            return sheetData;
        }

        /// <summary>
        /// データテーブルを一度数値データに変換してからテキストデータシートとして取得
        /// </summary>
        /// <returns>テーブルデータ</returns>
        public SheetData DoublDataTable(int scol = 0, int ecol = -1)
        {
            ecol = ecol < 0 ? mData[0].Length : ecol;
            SheetData sheetData;
            if (scol == 0 && ecol == mData[0].Length) {
                //  全列変換
                stringTable2doubleTable();
                sheetData = new SheetData(doubleData2String(), mDataTitle);
            } else {
                //  指定列変換
                sheetData = new SheetData(stringData2DoubleData(scol, ecol), mDataTitle);
            }
            return sheetData;

        }

        /// <summary>
        /// 列と行を反転する
        /// </summary>
        /// <returns>テーブルデータ</returns>
        public SheetData TransposeMatrix()
        {
            List<string[]> transData = new List<string[]>();
            //  タイトル
            string[] dataTitle = new string[mData.Count + 1];
            dataTitle[0] = mDataTitle[0];
            for (int i = 0; i < mData.Count; i++)
                dataTitle[i + 1] = mData[i][0];
            //  データの行列を反転
            for (int i = 1; i < mDataTitle.Length; i++) {
                string[] data = new string[mData.Count + 1];
                data[0] = mDataTitle[i];
                for (int j = 0; j < mData.Count; j++)
                    data[j + 1] = mData[j][i];
                transData.Add(data);
            }
            SheetData sheetData = new SheetData(transData, dataTitle);
            return sheetData;
        }

        /// <summary>
        /// 累積データを増分データに変換する
        /// n～m 列のデータを変換する
        /// n < 0 の場合は全列を変換
        /// </summary>
        /// <param name="n">変換する開始列</param>
        /// <param name="m">変換する終了列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData DifferntialData(int n, int m)
        {
            List<string[]> defferentialData = new List<string[]>();
            string[] data = mData[0];
            defferentialData.Add(data);
            for (int i = 0; i < mData.Count - 1; i++) {
                data = convDeffData(mData[i + 1], mData[i], mDataTitle.Length, n, m);
                defferentialData.Add(data);
            }
            SheetData sheetData = new SheetData(defferentialData, mDataTitle);
            return sheetData;
        }

        /// <summary>
        /// 増分データを累積データに変換する
        /// n～m 列のデータを変換する
        /// n < 0 の場合は全列を変換
        /// </summary>
        /// <param name="n">変換する開始列</param>
        /// <param name="m">変換する終了列</param>
        /// <param name="n">変換する列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData AccumulateData(int n, int m)
        {
            List<string[]> accumuData = new List<string[]>();
            string[] data = new string[mDataTitle.Length];
            data = mData[0];
            accumuData.Add(data);
            for (int i = 0; i < mData.Count - 1; i++) {
                data = convAccumulateData(mData[i + 1], data, mDataTitle.Length, n, m);
                accumuData.Add(data);
            }
            SheetData sheetData = new SheetData(accumuData, mDataTitle);
            return sheetData;
        }

        /// <summary>
        /// 一行の合計値を追加する
        /// 開始列と終了列の指定がない時は全列を対象
        /// </summary>
        /// <param name="sp">開始列</param>
        /// <param name="ep">終了列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData SumData(int sp = 0, int ep = 0)
        {
            List<string[]> sumData = new List<string[]>();
            string[] title = new string[mDataTitle.Length + 1];
            sp = Math.Min(Math.Max(sp, 0), mDataTitle.Length - 1);
            ep = ep <= 0 ? mDataTitle.Length - 1 : Math.Min(ep, mDataTitle.Length - 1);
            //  タイトルに「合計」を追加
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[mDataTitle.Length] = (sp == 0 ? "" : mDataTitle[sp]) + (0 < sp && 0 < ep ? "-" : "") +
                (sp == 0 && ep == mDataTitle.Length - 1 ? "" : "-" + mDataTitle[ep] + "\n") + "合計";
            //  データの合計をも求める
            for (int i = 0; i < mData.Count; i++) {
                string[] data = convSumData(mData[i], sp, ep);
                sumData.Add(data);
            }
            SheetData sheetData = new SheetData(sumData, title);
            return sheetData;
        }

        /// <summary>
        /// 縦方向の合計値を求める
        /// </summary>
        /// <param name="sp">集計開始列(省略時は全体)</param>
        /// <param name="ep">集計終了列(省略時は開始列のみ)</param>
        /// <param name="line">合計値記入行(省略時最終行に追加)</param>
        /// <returns>テーブルデータ</returns>
        public SheetData VerticalSumData(int sp = -1, int ep = -1, int line = -1)
        {
            double[] sumData = new double[mDataTitle.Length];
            for (int i = 0; i < mDataTitle.Length; i++) {
                if (mDataSubType[i] == DATATYPE.NUMBER && 
                    (sp < 0 || (sp == i && ep < 0) || (sp <= i && i <= ep))) {
                    sumData[i] = 0;
                    for (int j = 0; j < mData.Count; j++) {
                        if (j != line)
                            sumData[i] += ylib.string2double(mData[j][i]);
                    }
                } else {
                    sumData[i] = double.NaN;
                }
            }
            string[] stringData = new string[mDataTitle.Length];
            stringData[0] = "合計";
            SheetData sheetData = copy2SheetData();
            for (int i = 0; i < mDataTitle.Length; i++) {
                if (!double.IsNaN(sumData[i])) {
                    if (line < 0)
                        stringData[i] = sumData[i].ToString();
                    else
                        sheetData.mData[line][i] = sumData[i].ToString();
                }
            }
            if (line < 0)
                sheetData.mData.Add(stringData);

            return sheetData;
        }

        /// <summary>
        /// 指定列のセルのしめる割合を列末に追加する
        /// </summary>
        /// <param name="col"></param>
        /// <returns>テーブルデータ</returns>
        public SheetData RateData(int col)
        {
            double sum = 0;
            for (int i = 0; i < mData.Count; i++) {
                sum += mDoubleData[i][col];
            }
            string[] title = new string[mDataTitle.Length + 1];
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[mDataTitle.Length] = title[col] + "比率";
            List<string[]> newData = new List<string[]>();
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length + 1];
                for (int j = 0; j < mData[i].Length; j++)
                    data[j] = mData[i][j];
                data[mData[i].Length] = (mDoubleData[i][col] / sum).ToString();
                newData.Add(data);
            }
            SheetData sheetData = new SheetData(newData, title);
            return sheetData;
        }

        /// <summary>
        /// 列同士の演算を行い結果を列に追加する
        /// </summary>
        /// <param name="col1">第1列</param>
        /// <param name="col2">第2列</param>
        /// <param name="calcType">演算の種類</param>
        /// <returns>テーブルデータ</returns>
        public SheetData CalcData(int col1, int col2, char calcType)
        {
            List<string[]> calcData = new List<string[]>();
            string[] title = new string[mDataTitle.Length + 1];
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[mDataTitle.Length] = title[col1] + calcType + title[col2];
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length + 1];
                for (int j = 0; j < mData[i].Length; j++)
                    data[j] = mData[i][j];
                double val = 0;
                if (calcType == '+') {
                    val = mDoubleData[i][col1] + mDoubleData[i][col2];
                } else if (calcType == '-') {
                    val = mDoubleData[i][col1] - mDoubleData[i][col2];
                } else if (calcType == '*') {
                    val = mDoubleData[i][col1] * mDoubleData[i][col2];
                } else if (calcType == '/') {
                    if (mDoubleData[i][col2] == 0)
                        val = double.NaN;
                    else
                        val = mDoubleData[i][col1] / mDoubleData[i][col2];
                } else {
                    val = double.NaN;
                }
                data[mData[i].Length] = val.ToString();
                calcData.Add(data);
            }
            SheetData sheetData = new SheetData(calcData, title);
            return sheetData;
        }

        /// <summary>
        /// 連続した列の演算を行い結果を列に追加する
        /// </summary>
        /// <param name="col1">第1列</param>
        /// <param name="col2">第2列</param>
        /// <param name="calcType">演算の種類</param>
        /// <returns>テーブルデータ</returns>
        public SheetData CalcDatas(int col1, int col2, char calcType)
        {
            if (col2 < col1)
                YLib.Swap(ref col1, ref col2);
            List<string[]> calcData = new List<string[]>();
            string[] title = new string[mDataTitle.Length + 1];
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[mDataTitle.Length] = title[col1] + calcType + "..." + title[col2];
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length + 1];
                for (int j = 0; j < mData[i].Length; j++)
                    data[j] = mData[i][j];
                double val = mDoubleData[i][col1];
                for (int j = col1 + 1; j < col2; j++) {
                    if (calcType == '+') {
                        val += mDoubleData[i][j];
                    } else if (calcType == '-') {
                        val -= mDoubleData[i][j];
                    } else if (calcType == '*') {
                        val *= mDoubleData[i][j];
                    } else if (calcType == '/') {
                        if (mDoubleData[i][j] == 0)
                            val = double.NaN;
                        else
                            val /= mDoubleData[i][j];
                    } else {
                        val = double.NaN;
                    }
                }
                data[mData[i].Length] = val.ToString();
                calcData.Add(data);
            }
            SheetData sheetData = new SheetData(calcData, title);
            return sheetData;
        }

        /// <summary>
        /// 指定列に対して四則演算処理をおこなう
        /// </summary>
        /// <param name="col">列番</param>
        /// <param name="num">演算係数</param>
        /// <param name="calcType">演算の種類</param>
        /// <returns>テーブルデータ</returns>
        public SheetData CalcData(int col, double num, char calcType)
        {
            List<string[]> calcData = new List<string[]>();
            string[] title = new string[mDataTitle.Length];
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[col] += calcType + num.ToString();
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length];
                for (int j = 0; j < mData[i].Length; j++)
                    data[j] = mData[i][j];
                double val = 0;
                if (calcType == '+') {
                    val = mDoubleData[i][col] + num;
                } else if (calcType == '-') {
                    val = mDoubleData[i][col] - num;
                } else if (calcType == '*') {
                    val = mDoubleData[i][col] * num;
                } else if (calcType == '/') {
                    if (num == 0)
                        val = double.NaN;
                    else
                        val = mDoubleData[i][col] / num;
                } else {
                    val = double.NaN;
                }
                data[col] = val.ToString();
                calcData.Add(data);
            }
            SheetData sheetData = new SheetData(calcData, title);
            return sheetData;
        }

        /// <summary>
        /// 数式処理でデータの演算を行い列の最後に追加する
        /// [ColNo:ColTitle:RowRelNo]
        /// </summary>
        /// <param name="express">数式</param>
        /// <param name="title">追加する列のタイトル</param>
        /// <returns>テーブルデータ</returns>
        public SheetData CalcExpressData(string express, string title = "")
        {
            List<string[]> calcData = new List<string[]>();
            string[] titles = new string[mDataTitle.Length + 1];
            for (int i = 0; i < mDataTitle.Length; i++)
                titles[i] = mDataTitle[i];
            titles[mDataTitle.Length] = 0 < title.Length ? title : "演算結果";
            //  数式設定
            YCalc calc = new YCalc();
            //  sum関数のみ別途仕様で抽出
            Dictionary<string, string> argList = getAreaArgList(express);
            foreach (KeyValuePair<string, string> kvp in argList) {
                express = express.Replace(kvp.Value, kvp.Key);
            }
            calc.setExpression(express);        //  数式を登録
            string[] key = calc.getArgKey();    //  変数キーの取出し

            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length + 1];
                //  行コピー
                for (int j = 0; j < mData[i].Length; j++)
                    data[j] = mData[i][j];
                //  パラメータの値設定
                for (int j = 0; j < key.Length; j++) {
                    //  sumなどの範囲指定関数の処理
                    bool areaFunc = false;
                    foreach (KeyValuePair<string, string> kvp in argList) {
                        if (key[j].CompareTo(kvp.Key) == 0) {
                            var v = areaFuncCalc(kvp.Value, i);
                            calc.setArgValue(key[j], v.ToString());
                            areaFunc = true;
                        }
                    }
                    if (areaFunc)
                        continue;
                    // 引数キーから列データを数式処理に設定
                    (var col, var rrow) = getColRow(key[j]);
                    //  数式処理のパラメータに値を設定
                    if (rrow + i < 0 || mData.Count <= rrow + i || col < 0)
                        calc.setArgValue(key[j], "0");
                    else
                        calc.setArgValue(key[j], mData[i + rrow][col].Replace(",", ""));
                }
                double val = calc.calculate();              //  演算処理
                data[mDataTitle.Length] = val.ToString();
                calcData.Add(data);
            }

            SheetData sheetData = new SheetData(calcData, titles);
            return sheetData;
        }

        /// <summary>
        /// 範囲指定関数の抽出しキーワードと式のリストを作成
        /// sum(数式,セルの範囲)             指定した範囲のセルの値を数式で処理して合計を出す
        /// sum(f[@], [col:title:relRow]:[10:title:relRow])
        /// repeat(数式,初期値,セルの範囲)
        /// repeat(min([@],[result]),[1:Aichi],[1:Aichi]:[48:Yamanashi])    指定範囲のセルの値を数式で繰り返し処理
        ///     [result]:数式内で使用する変数で初期値と都度の演算結果が入る
        ///     初期値:定数(演算結果も含む)またはセルの値
        ///     セルの範囲:数式に代入[@]するセルの範囲
        /// </summary>
        /// <param name="express">数式</param>
        /// <returns>キーワードリスト</returns>
        private Dictionary<string, string> getAreaArgList(string express, int argNo = 0)
        {
            Dictionary<string, string> argList = new Dictionary<string, string>();
            YCalc calc = new YCalc();
            List<string> expList = calc.expressList(express);
            int n = argNo;
            for (int i = 0; i < expList.Count; i++) {
                if (0 == expList[i].IndexOf("sum(")) {
                    var argArray = calc.getFuncArgArray(expList[i]);    //  引数の数を求める
                    if (argArray.Length == 2 && !argList.ContainsValue(expList[i])) {
                        argList.Add("[sum" + n + "]", expList[i]);
                        n++;
                    }
                } else if (0 == expList[i].IndexOf("repeat(")) {
                    var argArray = calc.getFuncArgArray(expList[i]);    //  引数の数を求める
                    if (argArray.Length == 3 && !argList.ContainsValue(expList[i])) {
                        argList.Add("[repeat" + n + "]", expList[i]);
                        n++;
                    }
                } else if (0 <= expList[i].IndexOf("(")) {
                    string str = calc.getBracketString(expList[i]);
                    if (0 < str.Length) {
                        Dictionary<string, string>  argList2 = getAreaArgList(str, n + 1);
                        foreach (var arg in argList2) {
                            if (!argList.ContainsKey(arg.Key)) {
                                argList.Add(arg.Key, arg.Value);
                            }
                        }
                        n += argList2.Count;
                    }

                }
            }
            return argList;
        }

        /// <summary>
        /// 行の指定範囲を計算式で和を求める
        /// sum(f[@], [col:title:relRow]:[10:title:relRow])
        /// repeat(f([@],[result]),initVal,[col:title:relRow]:[col:title:relRow])
        /// </summary>
        /// <param name="funcStr">数式</param>
        /// <param name="row">対象行</param>
        /// <returns>計算結果</returns>
        private double areaFuncCalc(string funcStr, int row)
        {
            YCalc calc = new YCalc();
            int sp = funcStr.IndexOf("(") + 1;
            string func = funcStr.Substring(0, sp - 1).Trim();  //  関数名
            var strArray = calc.getFuncArgArray(funcStr);
            string express = strArray[0];                       //  関数内の数式
            //  関数内の範囲指定引数
            calc.setExpression(express);
            string[] key = calc.getArgKey();
            if (func.CompareTo("sum") == 0) {
                //  sum関数の処理
                double sum = 0.0;
                sp = strArray[1].IndexOf("[");
                int ep = strArray[1].IndexOf("]", sp);
                (int fcol, int frow) = getColRow(strArray[1].Substring(sp, ep - sp + 1));
                sp = strArray[1].IndexOf("[", ep);
                ep = strArray[1].IndexOf("]", sp);
                (int scol, int srow) = getColRow(strArray[1].Substring(sp, ep - sp + 1));
                for (int i = frow; i <= srow; i++) {
                    for (int j = fcol; j <= scol; j++) {
                        for (int k = 0; k < key.Length; k++) {
                            if (row + i < 0 || mData.Count <= row + i)
                                calc.setArgValue(key[k], "0");
                            else
                                calc.setArgValue(key[k], mData[row + i][j]);
                        }
                        sum += calc.calculate();
                    }
                }
                return sum;
            } else if (func.CompareTo("repeat") == 0) {
                //  repeat関数の処理
                double result = argCalc(strArray[1], row);
                sp = strArray[2].IndexOf("[");
                int ep = strArray[2].IndexOf("]", sp);
                (int fcol, int frow) = getColRow(strArray[2].Substring(sp, ep - sp + 1));
                sp = strArray[2].IndexOf("[", ep);
                ep = strArray[2].IndexOf("]", sp);
                (int scol, int srow) = getColRow(strArray[2].Substring(sp, ep - sp + 1));
                for (int i = frow; i <= srow; i++) {
                    for (int j = fcol; j <= scol; j++) {
                        for (int k = 0; k < key.Length; k++) {
                            if (key[k].CompareTo("[result]") == 0) {
                                calc.setArgValue(key[k], result.ToString());
                            } else {
                                if (row + i < 0 || mData.Count <= row + i)
                                    calc.setArgValue(key[k], "0");
                                else
                                    calc.setArgValue(key[k], mData[row + i][j]);
                            }
                        }
                        result = calc.calculate();
                    }
                }
                return result;
            }
            return 0.0;
        }

        /// <summary>
        /// セル(行列指定)の値を数式処理をする
        /// </summary>
        /// <param name="express">数式</param>
        /// <param name="row">対象行</param>
        /// <returns>計算結果</returns>
        private double argCalc(string express, int row)
        {
            YCalc calc = new YCalc();
            calc.setExpression(express);
            string[] key = calc.getArgKey();
            for (int k = 0; k < key.Length; k++) {
                if (0 <= key[k].IndexOf("[")) {
                    (int col, int rrow) = getColRow(key[k]);    //  引数のセルの位置を求める
                    if (row + rrow < 0 || mData.Count <= row + rrow)
                        calc.setArgValue(key[k], "0");
                    else
                        calc.setArgValue(key[k], mData[row + rrow][col]);
                }
            }
            return calc.calculate();
        }

        /// <summary>
        /// パラメータから列と相対行を求める
        /// [col:columnTitle:relativeRow]
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>(column,relative row)</returns>
        private (int col, int rrow) getColRow(string arg)
        {
            int sepNo = arg.IndexOf(":");
            int col = ylib.intParse(arg.Substring(1, sepNo - 1));    //  列指定
            int sepNo2 = arg.IndexOf(":", sepNo + 1);
            int row = 0;
            if (0 <= sepNo2) {
                //  相対行
                row = ylib.intParse(arg.Substring(sepNo2 + 1, arg.Length - sepNo2 - 2));
            }
            return (col, row);
        }

        /// <summary>
        /// 日付を年単位/月単位/週単位に変換して最終列に追加
        /// 変換種別 0:年単位  1:月単位  2:週単位  3:yyyy/mm/dd
        /// </summary>
        /// <param name="col">変換列</param>
        /// <param name="convType">変換単位</param>
        /// <returns>変換後のデータ</returns>
        public SheetData DateConvert(int col, int convType)
        {
            if (convType < 0 || mDataType[col] != DATATYPE.DATE) {
                mError = true;
                mErrorMessage = "選択列に日付以外が含まれているか変換種別があっていません";
                return null;
            }
            int[] dateType = new int[] { 6, 5, 9, 0 };
            List<string[]> convertData = new List<string[]>();
            string[] title = new string[mDataTitle.Length];
            Array.Copy(mDataTitle, title, mDataTitle.Length);
            for (int i = 0;i < mData.Count; i++) {
                string[] data = new string[mData[i].Length];
                for (int j = 0; j < mData[i].Length; j++) {
                    if (j == col) {
                        int jd = ylib.date2JulianDay(mData[i][j]);
                        data[j] = ylib.JulianDay2DateYear(jd, dateType[convType % 4]);
                    } else {
                        data[j] = mData[i][j];
                    }
                }
                convertData.Add(data);
            }
            SheetData sheetData = new SheetData(convertData, title);
            return sheetData;
        }


        /// <summary>
        /// 日付を年単位/月単位/週単位/曜日に変換して最終列に追加
        /// 変換種別 0:年単位  1:月単位  2:週単位  3:曜日
        /// </summary>
        /// <param name="col">変換元の列</param>
        /// <param name="convType">変換種別</param>
        /// <returns>テーブルデータ</returns>
        public SheetData DateConvert(int col, int convType, int offset)
        {
            string[] dateTitle = { "年単位", "月単位", "週単位", "曜日" };
            if (convType < 0 || dateTitle.Length <= convType || mDataType[col]!= DATATYPE.DATE) {
                mError = true;
                mErrorMessage = "選択列に日付以外が含まれているか変換種別があっていません";
                return null;
            }
            List<string[]> convertData = new List<string[]>();
            string[] title = new string[mDataTitle.Length + 1];
            for (int i = 0; i < mDataTitle.Length; i++)
                title[i] = mDataTitle[i];
            title[mDataTitle.Length] = dateTitle[convType];
            offset = (7 - offset) % 7;
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mData[i].Length + 1];
                for (int j = 0; j < mData[i].Length; j++) {
                    data[j] = mData[i][j];
                }
                int jd = ylib.date2JulianDay(data[col]);
                string date = ylib.JulianDay2DateYear(jd, true);
                string year = date.Substring(0, 4);
                string month = date.Substring(5, 2);
                string day = date.Substring(8, 2);
                switch (convType) {
                    case 0:     //  年単位
                        data[mData[i].Length] = ylib.JulianDay2DateYear(jd, 6); //  yyyy年
                        break;
                    case 1:     //  月単位
                        data[mData[i].Length] = ylib.JulianDay2DateYear(jd, 5); //  yyyy年mm月
                        break;
                    case 2:     //  週単位
                        data[mData[i].Length] = ylib.JulianDay2DateYear(jd+ offset, 8); //  yyyy年ww週
                        break;
                    case 3:     //  曜日
                        date = ylib.getWeekday(jd+1, 3);
                        data[mData[i].Length] = date;
                        break;
                    default:
                        data[mData[i].Length] = "";
                        break;
                }
                convertData.Add(data);
            }
            SheetData sheetData = new SheetData(convertData, title);
            return sheetData;
        }

        /// <summary>
        /// n列目を削除する
        /// </summary>
        /// <param name="st">開始列</param>
        /// <param name="en">終了列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData RemoveDataColumn(int st, int en)
        {
            if (st > en || st < 0 || mDataTitle.Length <= en)
                return null;
            List<string[]> removeData = new List<string[]>();
            string[] title = new string[mDataTitle.Length - 1 - (en - st)];
            for (int i = 0, j = 0; i < mDataTitle.Length; i++)
                if (i < st || en < i)
                    title[j++] = mDataTitle[i];
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[title.Length];
                for (int k = 0, j = 0; k < mData[i].Length; k++)
                    if (k < st || en < k)
                        data[j++] = mData[i][k];
                removeData.Add(data);
            }
            SheetData sheetData = new SheetData(removeData, title);
            return sheetData;
        }

        /// <summary>
        /// ファルマージをおこなうためのタイトル比較
        /// </summary>
        /// <param name="sheetData">マージファイルデータ</param>
        /// <param name="col">キータイトル列(-1の時は共通タイトルを検索)</param>
        /// <returns>キータイトル列番(既存データ、マージデータ)</returns>
        public (int srcCol, int desCol ) titleSearch(SheetData sheetData, int col = -1)
        {
            if (0 <= col) {
                for (int i = 0; i < sheetData.mDataTitle.Length; i++) {
                    if (mDataTitle[col].CompareTo(sheetData.mDataTitle[i]) == 0)
                        return (col, i);
                }
                return (col, -1);
            } else {
                for (int i = 0; i < sheetData.mDataTitle.Length; i++) {
                    for (int j = 0; j < mDataTitle.Length; j++) {
                        if (mDataTitle[j].CompareTo(sheetData.mDataTitle[i]) == 0)
                            return (j, i);
                    }
                }
                return (-1, -1);
            }
        }

        /// <summary>
        /// 既存のデータに新たなデータをマージする
        /// </summary>
        /// <param name="sheetData">追加するテーブルデータ</param>
        /// <param name="col">キータイトルの列番号</param>
        /// <returns>テーブルデータ</returns>
        public SheetData MergeData(SheetData sheetData, int col = -1, int destCol = -1)
        {
            if (0 <= col && 0 <= destCol) {
                //  キータイトルを設定()
                sheetData.mDataTitle[destCol] = mDataTitle[col];
            }

            //  タイトルのマージデータ作成
            HashSet<string> titleSet = new HashSet<string>();
            string keyTitle = "";
            foreach (string item in mDataTitle)
                titleSet.Add(item);
            int commonTitleCount = 0;   //  共通タイトル数
            foreach (string item in sheetData.mDataTitle) {
                if (titleSet.Contains(item)) {
                    commonTitleCount++;
                    if (keyTitle.Length == 0)
                        keyTitle = item;
                } else {
                    titleSet.Add(item);
                }
            }

            //  共通タイトルがない場合は処理中止
            if (commonTitleCount == 0) {
                mError = true;
                mErrorMessage = "共通タイトルが存在しません";
                return null;
            }

            //  タイトルをHashSetから配列に変換
            string[] title = new string[titleSet.Count];
            title[0] = keyTitle;
            int n = 1;
            foreach (string item in titleSet) {
                if (title[0].CompareTo(item) != 0 && n < titleSet.Count)
                    title[n++] = item;
            }

            //  データのマージ
            Dictionary<string, string[]> dataSet = new Dictionary<string, string[]>();
            //  ソースデータのタイトルインデックスを作成
            int[] srcTitleIndex = new int[titleSet.Count];
            for (int i = 0; i < title.Length; i++) {
                srcTitleIndex[i] = Array.IndexOf(mDataTitle, title[i]);
            }

            //  ソースデータをコピー
            foreach ( string[] srcData in mData) {
                string[] data = new string[titleSet.Count];
                for (int i = 0; i < srcTitleIndex.Length; i++) {
                    if (0 <= srcTitleIndex[i])
                        data[i] = srcData[srcTitleIndex[i]];
                    else
                        data[i] = "";
                }
                //  新規データのみ追加
                if (!dataSet.ContainsKey(data[0]))
                    dataSet.Add(data[0], data);
            }

            //  追加データのタイトルインデックスを作成
            int[] addTitleIndex = new int[titleSet.Count];
            for (int i = 0; i < title.Length; i++) {
                addTitleIndex[i] = Array.IndexOf(sheetData.mDataTitle, title[i]);
            }

            //  追加データをマージ
            foreach (string[] addData in sheetData.mData) {
                if (dataSet.ContainsKey(addData[addTitleIndex[0]])){
                    for (int i = 1; i < addTitleIndex.Length; i++) {
                        if (0 <= addTitleIndex[i])
                            dataSet[addData[addTitleIndex[0]]][i] = addData[addTitleIndex[i]];
                    }
                } else {
                    string[] data = new string[titleSet.Count];
                    for (int i = 0; i < addTitleIndex.Length; i++) {
                        if (0 <= addTitleIndex[i])
                            data[i] = addData[addTitleIndex[i]];
                        else
                            data[i] = "";
                    }
                    dataSet.Add(data[0], data);
                }
            }

            List<string[]> mergeData = dataSet.Values.ToList();
            SheetData sheet = new SheetData(mergeData, title);
            return sheet;
        }

        /// <summary>
        /// 指定列と右側列を結合する
        /// 結合するときに数値データは加算し、文字データは連結する
        /// </summary>
        /// <param name="col">列番</param>
        /// <param name="m">結合する列数</param>
        /// <param name="num">数値加算</param>
        /// <returns>表データ</returns>
        public SheetData CombineData(int col, int m, bool num)
        {
            if (col < 0 || m <= 0 || mDataTitle.Length <= m || (mDataTitle.Length - m) <= col)
                return null;
            //  タイトル行の列結合
            string[] title = new string[mDataTitle.Length - m];
            for (int i = 0, j = 0; i < mDataTitle.Length; i++) {
                if (i == col) {
                    title[j] = mDataTitle[i];
                } else if (col < i && i < col + m + 1) {
                    title[j] +=  "-" + mDataTitle[i];
                } else {
                    if (i == col + m + 1)
                        j++;
                    title[j++] = mDataTitle[i];
                }
            }
            //  データの列結合
            List<string[]> joinData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length - m];
                for (int i = 0, j = 0; i < mDataTitle.Length; i++) {
                    if (i == col) {
                        data[j] = srcData[i];
                    } else if (col < i && i < col + m + 1) {
                        if (num && ylib.IsNumberString(data[j]) && ylib.IsNumberString(srcData[i]))
                            data[j] = (ylib.string2double(data[j]) + ylib.string2double(srcData[i])).ToString("#,##0");
                        else
                            data[j] += "-" + srcData[i];
                    } else {
                        if (i == col + m + 1)
                            j++;
                        data[j++] = srcData[i];
                    }
                }
                joinData.Add(data);
            }
            SheetData sheet = new SheetData(joinData, title);
            return sheet;
        }

        /// <summary>
        /// 指定列のデータ移動
        /// </summary>
        /// <param name="n">移動する列番</param>
        /// <param name="m">移動先の列番</param>
        /// <returns>テーブルデータ</returns>
        public SheetData MoveData(int n, int m)
        {
            if (n == m)
                return null;
            //  タイトルの移動
            string[] title = new string[mDataTitle.Length];
            for (int i = 0, j = 0; i < mDataTitle.Length; i++) {
                if (i == n) {
                } else if (i == m) {
                    title[j++] = mDataTitle[n];
                    title[j++] = mDataTitle[i];
                } else {
                    title[j++] = mDataTitle[i];
                }
            }
            //  データの移動
            List<string[]> moveData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length];
                for (int i = 0, j = 0; i < mDataTitle.Length; i++) {
                    if (i == n) {
                    } else if (i == m) {
                        data[j++] = srcData[n];
                        data[j++] = srcData[i];
                    } else {
                        data[j++] = srcData[i];
                    }
                }
                moveData.Add(data);
            }
            SheetData sheet = new SheetData(moveData, title);
            return sheet;
        }

        /// <summary>
        /// 指定列間のデータを右端にコピー
        /// </summary>
        /// <param name="n">開始列</param>
        /// <param name="m">終了列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData CopyData(int n, int m)
        {
            n = Math.Min(Math.Max(0, n), mDataTitle.Length - 1);
            m = m < n ? n : Math.Min(m, mDataTitle.Length - 1);
            //  タイトルの追加
            string[] title = new string[mDataTitle.Length + m - n  + 1];
            for (int i = 0; i < mDataTitle.Length; i++) {
                    title[i] = mDataTitle[i];
            }
            for (int i = n, j = mDataTitle.Length; i <= m; i++) {
                int count = 2;
                int prevCount;
                string ttile;
                do {
                    prevCount = count;
                    ttile = title[i] + "(" + count + ")";
                    for (int k = 0; k < mDataTitle.Length; k++)
                        if (ttile.CompareTo(title[k]) == 0) {
                            count++;
                            break;
                        }
                } while (count != prevCount);
                title[j++] = ttile; 
            }
            //  データのコピー
            List<string[]> copyData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length + m - n + 1];
                for (int i = 0; i < mDataTitle.Length; i++) {
                        data[i] = srcData[i];
                }
                for (int i = 0; i < m - n + 1; i++) {
                    data[mDataTitle.Length + i] = srcData[n + i];
                }
                copyData.Add(data);
            }

            SheetData sheet = new SheetData(copyData, title);
            return sheet;
        }

        /// <summary>
        /// シート内の全角数値(.+-も含む)を半角文字に変換する
        /// </summary>
        /// <returns>テーブルデータ</returns>
        public SheetData zen2hanData()
        {
            List<string[]> hanData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length];
                for (int i = 0, j = 0; i < mDataTitle.Length; i++) {
                    data[j++] =　ylib.strNumZne2Han(srcData[i]);
                }
                hanData.Add(data);
            }
            SheetData sheet = new SheetData(hanData, mDataTitle);
            return sheet;
        }

        /// <summary>
        /// 複数年のデータを年ごとに別のデータとして登録し直す
        /// </summary>
        /// <returns>テーブルデータ</returns>
        public SheetData yearCompareData()
        {
            if (mDataType[0] != DATATYPE.DATE)
                return null;
            //  データのある年をピックアップ
            HashSet<string> yearList = new HashSet<string>();
            for (int i = 0; i < mData.Count; i++) {
                yearList.Add(getYearData(mData[i][0]));
            }
            //  年ごとのデータタイトルを作成(年(yyyy)　+ タイトル)
            string[] title = new string[(mDataTitle.Length - 1) * yearList.Count + 1];
            title[0] = mDataTitle[0];
            int n = 1;
            foreach (string year in yearList) {
                for (int j = 1; j < mDataTitle.Length; j++) {
                    title[n++] = year + " " + mDataTitle[j];
                }
            }
            //  現在の年を求める
            DateTime now = DateTime.Now;
            string nowYear = now.ToString().Substring(0, 4);
            //  年ごとのデータを追加
            Dictionary<string, string[]> yearData = new Dictionary<string, string[]>();
            for (int i = 0; i < mData.Count; i++) {
                string dataYear = getYearData(mData[i][0]);
                string keyData = getYearDataReplase(mData[i][0], nowYear);
                if (yearData.ContainsKey(keyData)) {
                    string[] data = yearData[keyData];
                    for (int j = 1; j < mData[i].Length; j++) {
                        int titleNo = Array.IndexOf(title, dataYear + " " + mDataTitle[j]);
                        data[titleNo] = mData[i][j];
                    }
                    yearData[keyData] = data;
                } else {
                    string[] data = (new string[title.Length]).Select(str => "").ToArray();
                    data[0] = keyData;
                    for (int j = 1; j < mData[i].Length; j++) {
                        int titleNo = Array.IndexOf(title, dataYear + " " + mDataTitle[j]);
                        data[titleNo] = mData[i][j];
                    }
                    yearData.Add(keyData, data);
                }
            }
            //  ハッシュデータからリストデータに移す
            List<string[]> yearTable = new List<string[]>();
            foreach (string[] data in yearData.Values) {
                yearTable.Add(data);
            }
            SheetData sheetData = new SheetData(yearTable, title);
            return sheetData;
        }

        /// <summary>
        /// シート内の指定列のデータで合致する行だけを残す
        /// </summary>
        /// <param name="n">フィルタリング対象列</param>
        /// <param name="filterList">フィルタワード</param>
        /// <returns>テーブルデータ</returns>
        public SheetData filteringData(int n, List<string> filterList)
        {
            List<string[]> filteredData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length];
                foreach (string filterData in filterList) {
                    if (srcData[n].CompareTo(filterData) == 0) {
                        filteredData.Add(srcData);
                        break;
                    }
                }
            }
            SheetData sheet = new SheetData(filteredData, mDataTitle);
            return sheet;
        }

        /// <summary>
        /// セルのデータを変更する
        /// </summary>
        /// <param name="row">行番号</param>
        /// <param name="col">列番号</param>
        /// <param name="convText">変更する文字列</param>
        /// <returns>テーブルデータ</returns>
        public SheetData changeCellData(int row, int col, string convText)
        {
            List<string[]> convData = new List<string[]>();
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mDataTitle.Length];
                for (int j = 0; j < mDataTitle.Length; j++) {
                    if (i == row && j == col) {
                        data[j] = convText;
                    } else if (j < mData[i].Length){
                        data[j] = mData[i][j];
                    } else {
                        data[j] = "";
                    }
                }
                convData.Add(data);
            }
            SheetData sheet = new SheetData(convData, mDataTitle);
            return sheet;
        }

        /// <summary>
        /// 指定列のタイトル名を変更する
        /// </summary>
        /// <param name="col">列No</param>
        /// <param name="convText">変更タイトル</param>
        /// <returns>データテーブル</returns>
        public SheetData changeTitleData(int col, string convText)
        {
            string[] convTitle = new string[mDataTitle.Length];
            for (int i = 0; i < mDataTitle.Length; i++) {
                if (i  == col) {
                    convTitle[i] = convText;
                } else {
                    convTitle[i] = mDataTitle[i];
                }
            }
            List<string[]> convData = new List<string[]>();
            for (int i = 0; i < mData.Count; i++) {
                string[] data = new string[mDataTitle.Length];
                for (int j = 0; j < mDataTitle.Length; j++) {
                    if (j < mData[i].Length) {
                        data[j] = mData[i][j];
                    } else {
                        data[j] = "";
                    }
                }
                convData.Add(data);
            }
            SheetData sheet = new SheetData(convData, convTitle);
            return sheet;
        }


        /// <summary>
        /// 辞書データを使ってデータの変換を行う
        /// 変換対象列が < 0 の場合タイトルを変換する
        /// </summary>
        /// <param name="path">辞書データのパス</param>
        /// <param name="n">変換列</param>
        /// <returns>データテーブル</returns>
        public SheetData dataConvert(string path, int n)
        {
            Dictionary<string, string> convDic = getConvertDic(path);
            string[] convTitle = new string[mDataTitle.Length];
            for (int i = 0; i < mDataTitle.Length; i++) {
                if (n < 0 && convDic.ContainsKey(mDataTitle[i])) {
                    convTitle[i] = convDic[mDataTitle[i]];
                } else {
                    convTitle[i] = mDataTitle[i];
                }
            }

            List<string[]> convData = new List<string[]>();
            foreach (string[] srcData in mData) {
                string[] data = new string[mDataTitle.Length];
                for (int i = 0; i < mDataTitle.Length; i++) {
                    if (i == n && convDic.ContainsKey(srcData[i])) {
                        data[i] = convDic[srcData[i]];
                    } else {
                        data[i] = srcData[i];
                    }
                }
                convData.Add(data);
            }
            SheetData sheet = new SheetData(convData, convTitle);
            return sheet;
        }

        /// <summary>
        /// データ変換用の辞書データの取得
        /// </summary>
        /// <param name="path">ファイル名</param>
        /// <returns>辞書データ</returns>
        private Dictionary<string, string> getConvertDic(string path)
        {
            ylib.setEncording(1);
            Dictionary<string, string> convDic = new Dictionary<string, string>();
            List<string[]> dicList = ylib.loadCsvData(path);
            foreach (string[] data in dicList) {
                if (1 < data.Length && !convDic.ContainsKey(data[0]) && 0 < data[1].Length)
                    convDic.Add(data[0], data[1]);
            }
            return convDic;
        }

        /// <summary>
        /// 日付データの年を付け替える
        /// </summary>
        /// <param name="date">日付データ</param>
        /// <param name="year">入れ替える年データ</param>
        /// <returns>年を付け替えたデータ</returns>
        private string getYearDataReplase(string date, string year)
        {
            int jd = ylib.date2JulianDay(date);
            return year + ylib.JulianDay2DateYear(jd, 0).Substring(4);
        }

        /// <summary>
        /// 日付データから年を取得
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string getYearData(string date)
        {
            int jd = ylib.date2JulianDay(date);
            return ylib.JulianDay2DateYear(jd, 0).Substring(0,4);
        }

        /// <summary>
        /// 前の行との差分データを求める(累積値→差分地)
        /// </summary>
        /// <param name="secondData">次データ配列</param>
        /// <param name="firstData">対象データ配列</param>
        /// <param name="size">配列の大きさ</param>
        /// <param name="col">対象列(<0の時は全列)</param>
        /// <param name="col2">対象列の終わり</param>
        /// <returns></returns>
        private string[] convDeffData(string[] secondData, string[] firstData, int size, int col, int col2)
        {
            string[] data = new string[size];
            col2 = col2 < col ? col : col2;
            for (int i = 0; i < size; i++) {
                if (mDataType[i] == DATATYPE.NUMBER && (col < 0 || (col <= i && i <= col2))) {
                    data[i] = (ylib.string2double(secondData[i]) - ylib.string2double(firstData[i])).ToString("#,##0");
                } else {
                    data[i] = secondData[i];
                }
            }
            return data;
        }

        /// <summary>
        /// 前の行のデータに加算して累積値データを求める
        /// </summary>
        /// <param name="secondData">次データ配列</param>
        /// <param name="firstData">対象データ配列</param>
        /// <param name="size">配列の大きさ</param>
        /// <param name="col">対象列(<0の時は全列)</param>
        /// <param name="col2">対象列の終わり</param>
        /// <returns></returns>
        private string[] convAccumulateData(string[] secondData, string[] firstData, int size, int col, int col2)
        {
            string[] data = new string[size];
            for (int i = 0; i < size; i++) {
                if (mDataType[i] == DATATYPE.NUMBER && (col < 0 || (col <= i && i <= col2))) {
                    data[i] = (ylib.string2double(secondData[i]) + ylib.string2double(firstData[i])).ToString("#,##0");
                } else {
                    data[i] = secondData[i];
                }
            }
            return data;
        }

        /// <summary>
        /// 一行の合計値を求める
        /// </summary>
        /// <param name="srcData">元テーブルデータ</param>
        /// <param name="sp">集計開始列</param>
        /// <param name="ep">集計終了列</param>
        /// <returns>テーブルデータ</returns>
        private string[] convSumData(string[] srcData, int sp, int ep)
        {
            string[] data = new string[srcData.Length + 1];
            double sum = 0;
            for (int i = 0; i < srcData.Length; i++) {
                data[i] = srcData[i];
                if (mDataType[i] == DATATYPE.NUMBER && sp <= i && i <= ep)
                    sum += ylib.string2double(srcData[i]);
            }
            data[srcData.Length] = sum.ToString("#,##0");
            return data;
        }

        /// <summary>
        /// 集計データのタイトルの抽出
        /// 対象タイトルのデータから集計用のタイトルを抽出する
        /// </summary>
        /// <param name="XTitle">集計対象となる横軸タイトル</param>
        /// <param name="YTitle">集計対象となる縦軸タイトル</param>
        /// <returns>横軸タイトルの配列</returns>
        private string[] makeTitle(string XTitle, string YTitle)
        {
            string[] destTitle;                                     //  集計表の列タイトル
            SortedSet<string> xtitles = new SortedSet<string>();    //  横軸データ
            int xcol = Array.IndexOf(mDataTitle, XTitle);           //  縦軸の列番
            if (0 < xcol) {
                //  横軸データの種類を抽出
                foreach (string[] data in mData) {
                    xtitles.Add(data[xcol]);
                }
                //  タイトル作成
                destTitle = new string[xtitles.Count + 1];
                destTitle[0] = YTitle;
                int i = 1;
                foreach (string title in xtitles)
                    destTitle[i++] = title;

                return destTitle;
            }
            return null;
        }

        /// <summary>
        /// 集計データの作成
        /// </summary>
        /// <param name="title">横軸タイトルの配列</param>
        /// <param name="XTitle">集計項目</param>
        /// <param name="YTitle">集計系列</param>
        /// <param name="dataTitle">集計データ値項目</param>
        /// <returns></returns>
        private List<string[]> makeTable(string[] title, string XTitle, string YTitle, string dataTitle)
        {
            Dictionary<string, double[]> dataTable = new Dictionary<string, double[]>();
            int xcol = Array.IndexOf(mDataTitle, XTitle);           //  縦軸(集計項目)の列番
            int ycol = Array.IndexOf(mDataTitle, YTitle);           //  横軸(集計系列)の列番
            int dcol = 0 < dataTitle.Length ? Array.IndexOf(mDataTitle, dataTitle) : -1;    //  集計データの列番

            //  縦方向に選択タイトルのデータをデータ名ごとにカウントしていく
            foreach (string[] data in mData) {
                if (dataTable.ContainsKey(data[ycol])) {
                    //  データが存在する場合、データをカウントアップする
                    double[] count = dataTable[data[ycol]];
                    int n = Array.IndexOf(title, data[xcol]);
                    if (0 <= dcol) {
                        count[n] += ylib.string2Double(data[dcol]);
                    } else {
                        count[n]++;
                    }
                } else {
                    //  データがない時は新規追加
                    double[] count = new double[title.Length];
                    int n = Array.IndexOf(title, data[xcol]);
                    if (0 <= dcol) {
                        count[n] += ylib.string2Double(data[dcol]);
                    } else {
                        count[n]++;
                    }
                    dataTable.Add(data[ycol], count);
                }
            }
            //  DictionaryからListに変換
            //  データ名を横軸にしてカウントした数値で表を作る
            List<string[]> dataList = new List<string[]>();
            if (0 < dataTable.Count) {
                foreach(KeyValuePair<string, double[]> item in dataTable) {
                    string[] datas = new string[title.Length];
                    datas[0] = item.Key;
                    for (int i = 1; i < title.Length; i++)
                        datas[i] = item.Value[i].ToString();
                    dataList.Add(datas);
                }
                return dataList;
            }
            return null;
        }

        /// <summary>
        /// 指定列のデータをリストで取得
        /// </summary>
        /// <param name="n">列番号</param>
        /// <returns>データリスト</returns>
        public List<string> getColDataList(int n)
        {
            List<string> colDataList = new List<string>();
            foreach (string[] data in mData) {
                if (!colDataList.Contains(data[n])) {
                    colDataList.Add(data[n]);
                }
            }
            return colDataList;
        }

        /// <summary>
        /// データテーブルを数値リストで取得
        /// </summary>
        /// <returns>数値データリスト</returns>
        public List<double[]> getDoubleTable()
        {
            List<double[]> graphData = new List<double[]>();
            foreach (double[] doubleData in mDoubleData) {
                double[] datas = new double[doubleData.Length];
                for (int i = 0; i < doubleData.Length; i++) {
                    datas[i] = doubleData[i];
                }
                graphData.Add(datas);
            }

            return graphData;
        }

        /// <summary>
        /// 指定列を文字列データから数値文字列データに変換する
        /// </summary>
        /// <param name="scol">開始列</param>
        /// <param name="ecol">終了列</param>
        /// <returns>変換後データ</returns>
        private List<string[]> stringData2DoubleData(int scol, int ecol)
        {
            List<string[]> stringDatas = new List<string[]>();
            for (int row = 0; row < mData.Count; row++) {
                string[] data = new string[mData[row].Length];
                for (int col = 0; col < mData[row].Length;col++) {
                    if (scol <= col && col <= ecol) {
                        data[col] = double2String(string2Double(mData[row][col], row, mDataType[col]), mDataType[col]);
                    } else {
                        data[col] = mData[row][col];
                    }
                }
                stringDatas.Add(data);
            }
            return stringDatas;
        }


        /// <summary>
        /// 数値データテーブルをテキストデータテーブルに変換する
        /// </summary>
        /// <returns></returns>
        private List<string[]> doubleData2String()
        {
            List<string[]> stringDatas = new List<string[]>();
            foreach(double[] doubleData in mDoubleData) {
                string[] stringData = doubleArray2stringArray(doubleData, mDataType);
                stringDatas.Add(stringData);
            }
            return stringDatas;
        }

        /// <summary>
        /// テキストデータテーブルを数値データテーブルに変換する
        /// </summary>
        public void stringTable2doubleTable()
        {
            mDoubleData = stringTable2doubleTable(mData, mDataType);
            mArea = getMinMaxTableArea(mDoubleData, 0, mDoubleData.Count - 1);
        }

        /// <summary>
        /// グラフの領域を縦横反転する
        /// </summary>
        /// <returns>領域データ</returns>
        public Rect transpositionArea(Rect area)
        {
            Rect rect = new Rect(area.Y, area.X, area.Height, area.Width);
            return rect;
        }

        /// <summary>
        /// テキストデータのリストを数値データに変換する
        /// </summary>
        /// <param name="stringTable">テキストデータリスト</param>
        /// <param name="dateType">データタイプ配列</param>
        /// <returns>数値データリスト</returns>
        public List<double[]> stringTable2doubleTable(List<string[]> stringTable, DATATYPE[] dateType)
        {
            List<double[]> doubleTable = new List<double[]>();
            for (int i = 0; i < stringTable.Count; i++) {
                doubleTable.Add(stringArray2doubleArray(i, stringTable[i], dateType));
            }
            return doubleTable;
        }

        /// <summary>
        /// テキストデータ配列を数値データ配列に変換する
        /// 文字データは行番号に置き換える
        /// </summary>
        /// <param name="row">行番号</param>
        /// <param name="stringArray">テキストデータ配列</param>
        /// <param name="dateType">データタイプ配列</param>
        /// <returns>数値データ配列</returns>
        private double[] stringArray2doubleArray(int row, string[] stringArray, DATATYPE[] dataType)
        {
            double[] doubleArray = new double[stringArray.Length];
            for (int i = 0; i < stringArray.Length; i++) {
                doubleArray[i] = string2Double(stringArray[i], row, dataType[i]);
            }
            return doubleArray;
        }

        /// <summary>
        /// 数値データ配列をテキストデータ配列に変換する
        /// </summary>
        /// <param name="doubleArray"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private string[] doubleArray2stringArray(double[] doubleArray, DATATYPE[] dataType)
        {
            string[] stringArray = new string[doubleArray.Length];
            for (int i = 0; i < doubleArray.Length; i++) {
                stringArray[i] = double2String(doubleArray[i], dataType[i]);
            }
            return stringArray;
        }

        /// <summary>
        /// 文字列のタイプにあわせて文字列を数値に変換
        /// </summary>
        /// <param name="stringData">文字列データ</param>
        /// <param name="row">対象行</param>
        /// <param name="dataType">文字列の種類</param>
        /// <returns>変換後の数値</returns>
        private double string2Double(string stringData, int row, DATATYPE dataType)
        {
            double doubleData;
            if (dataType == DATATYPE.DATE) {
                doubleData = ylib.date2JulianDay(stringData);
            } else if (dataType == DATATYPE.WEEKDAY) {
                doubleData = ylib.WeekNo(stringData);
            } else if (dataType == DATATYPE.TIME) {
                doubleData = ylib.time2Seconds(stringData);
            } else if (dataType == DATATYPE.STRING) {
                doubleData = row;                         //  文字データは行番号にする
            } else {
                doubleData = ylib.string2double(stringData);
            }
            return doubleData;
        }

        /// <summary>
        /// 数値を文字列タイプに合わせて文字列に変換
        /// </summary>
        /// <param name="doubleData">数値データ</param>
        /// <param name="dataType">文字列タイプ</param>
        /// <returns>文字列</returns>
        private string double2String(double doubleData, DATATYPE dataType)
        {
            string stringData;
            if (dataType == DATATYPE.DATE) {
                stringData = ylib.JulianDay2DateYear((int)doubleData, true);
            } else if (dataType == DATATYPE.TIME) {
                stringData = ylib.second2String((int)doubleData, false);
            } else {
                if (doubleData % 1 == 0)
                    stringData = doubleData.ToString("#,##0");
                else
                    stringData = doubleData.ToString();
            }
            return stringData;
        }

        /// <summary>
        /// 数値データ(1列目を除く)の最小最大値から領域をもとめる
        /// </summary>
        /// <param name="doubleTable">数値データ</param>
        /// <param name="startPos">開始位置</param>
        /// <param name="endPos">終了位置</param>
        /// <returns>領域</returns>
        private Rect getMinMaxTableArea(List<double[]> doubleTable, int startPos, int endPos)
        {
            if (startPos < 0 || doubleTable.Count <= startPos)
                startPos = 0;
            if (endPos <= startPos || doubleTable.Count <= endPos)
                endPos = doubleTable.Count - 1;

            Rect area = getMinMaxPoint(doubleTable[startPos]);
            for (int i = startPos + 1; i <= endPos; i++) {
                area = getMinMaxPoint(doubleTable[i], area);
            }
            return area;
        }

        /// <summary>
        /// 1列目を除くデータの最小最大値
        /// </summary>
        /// <param name="doubleArray"></param>
        /// <returns></returns>
        private Rect getMinMaxPoint(double[] doubleArray)
        {
            Rect minmax = new Rect();
            //  縦軸の最小最大初期値
            minmax.Y = doubleArray[0];
            minmax.Height = 0;
            if (doubleArray.Length <= 1)
                return minmax;
            //  横軸最小値
            minmax.X = Math.Min(doubleArray[1], 0);
            minmax.X = doubleArray[1];
            //  横軸最大値
            double max = minmax.X;
            for (int i = 1; i < doubleArray.Length; i++) {
                minmax.X = Math.Min(doubleArray[i], minmax.X);
                max = Math.Max(doubleArray[i], max);
            }
            minmax.Width = max - minmax.X;
            return minmax;
        }

        /// <summary>
        /// 1列目を除くデータの最小最大値の更新
        /// </summary>
        /// <param name="doubleArray"></param>
        /// <param name="minmax"></param>
        /// <returns></returns>
        private Rect getMinMaxPoint(double[] doubleArray, Rect minmax)
        {
            //  縦軸の最小最大値更新
            minmax.Y = Math.Min(doubleArray[0], minmax.Y);
            minmax.Height = Math.Max(doubleArray[0] - minmax.Y, minmax.Height);
            //  横軸の最小最大値更新
            double max = minmax.X + minmax.Width;
            for (int i = 1; i < doubleArray.Length; i++) {
                minmax.X = Math.Min(doubleArray[i], minmax.X);
                max = Math.Max(doubleArray[i], max);
            }
            minmax.Width = max - minmax.X;
            return minmax;
        }
    }
}
