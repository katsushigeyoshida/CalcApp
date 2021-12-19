using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CalcApp
{
    /// <summary>
    /// RegressionAnalysis.xaml の相互作用ロジック
    /// </summary>
    public partial class RegressionAnalysis : Window
    {
        List<string[]> mSheetData = new List<string[]>();
        string[] mTitle = new string[3];

        public RegressionAnalysis()
        {
            InitializeComponent();

        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            setHeader();
            //initTable();
            //string[] title = new string[] { "title", "title1", "title2" };
            //List<string[]> listData = new List<string[]>() {
            //    new string[]{"1", "title1", "title2" },
            //    new string[]{"2", "22", "33" },

            //};
            //DgDataSheet.AutoGenerateColumns = false;
            //DgDataSheet.SelectionUnit = DataGridSelectionUnit.Cell;

            //setTitle(title);
            //setData(listData);
            //DgDataSheet.Columns[0].Header = "タイトル";
            //DgDataSheet.Columns[0].IsReadOnly = false;
        }

        private void executeBtn_Click(object sender, RoutedEventArgs e)
        {
            setRow();
        }


        //  ヘッダの追加
        private void setHeader()
        {
            DgDataList.AutoGenerateColumns = false;
            DgDataList.Columns.Clear();
            for (int i = 0; i < 3; i++) {
                var columns = new DataGridTextColumn();
                columns.Header = $"列{i}";
                columns.Binding = new Binding($"{(i)}");
                columns.IsReadOnly = true;
                DgDataList.Columns.Add(columns);
            }
        }

        //  行の追加
        private void setRow()
        {
            //var dataList = new ObservableCollection<List<string>>();
            mSheetData.Clear();
            for (int i = 0; i < 2; i++) {
                int[] data = new int[3];
                data[0] = 1;
                data[1] = 2;
                data[2] = 3;
                //mSheetData.Add(data);
                DgDataList.Items.Add(data[i]);
            }
            //DgDataSheet.ItemsSource = mSheetData;
        }
    }
}
