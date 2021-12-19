using System.Windows;
using System.Windows.Input;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// FuncMenu.xaml の相互作用ロジック
    /// 数式処理の関数選択メニューダイヤログ
    /// </summary>
    public partial class FuncMenu : Window
    {
        private bool mResult = false;
        private string[] mFuncItem = {
            "[#] 前回の計算結果",
            "[@] sum/product/repeatで使用する繰返し値",
            "[%] repeat()関数内で使用されるrepeat()関数の結果の値"
        };
        public string mResultFunc;

        public FuncMenu()
        {
            InitializeComponent();

            //  関数メニューの登録
            funcMenu.Items.Clear();
            foreach (string str in mFuncItem)
                funcMenu.Items.Add(str);
            foreach (string str in YCalc.mFuncList)
                funcMenu.Items.Add(str);
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.DialogResult = mResult;
        }

        /// <summary>
        /// [マウス左ボタン] 関数の設定と終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuncMenu_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (0 <= funcMenu.SelectedIndex) {
                mResultFunc = funcMenu.Items[funcMenu.SelectedIndex].ToString();
                mResult = true;
                this.Close();
            }
        }

        private void FuncMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
