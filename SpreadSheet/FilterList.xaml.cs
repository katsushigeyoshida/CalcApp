using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CalcApp
{
    /// <summary>
    /// FilterList.xaml の相互作用ロジック
    /// </summary>
    public partial class FilterList : Window
    {
        public List<CheckBoxListItem> mFilterList = new List<CheckBoxListItem>();

        public FilterList()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listDataSet();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void listDataSet()
        {
            LbFilterList.Items.Clear();
            foreach (CheckBoxListItem filterItem in mFilterList) {
                LbFilterList.Items.Add(filterItem);
            }
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CmCheckMenu_Click(object sender, RoutedEventArgs e)
        {
            int selItemNo = LbFilterList.SelectedIndex;
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CmAllCheck") == 0) {
                //  すべてにチェックをつける
                LbFilterList.ItemsSource = null;
                for (int i = 0; i < mFilterList.Count; i++)
                    mFilterList[i].Checked = true;
            } else if (menuItem.Name.CompareTo("CnAllNotCheck") == 0) {
                //  選択以外のすべてのチェックを外す
                LbFilterList.ItemsSource = null;
                for (int i = 0; i < mFilterList.Count; i++)
                    if (i == selItemNo)
                        mFilterList[i].Checked = true;
                    else
                        mFilterList[i].Checked = false;
            } else if (menuItem.Name.CompareTo("CmReverseCheck") == 0) {
                //  チェックを反転する
                LbFilterList.ItemsSource = null;
                for (int i = 0; i < mFilterList.Count; i++)
                    if (mFilterList[i].Checked)
                        mFilterList[i].Checked = false;
                    else
                        mFilterList[i].Checked = true;
            } else {
                return;
            }

            //  再表示
            LbFilterList.Items.Clear();
            LbFilterList.ItemsSource = mFilterList;

        }
    }
}
