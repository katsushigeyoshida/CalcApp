using System.Windows;

namespace CalcApp
{
    /// <summary>
    /// SpeadCalcInput.xaml の相互作用ロジック
    /// </summary>
    public partial class SpeadCalcInput : Window
    {
        public char mCalcType = '+';
        public double mInputVal = 0;

        public SpeadCalcInput()
        {
            InitializeComponent();

            RbAdd.IsChecked = true;
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            if (RbAdd.IsChecked == true)
                mCalcType = '+';
            if (RbSub.IsChecked == true)
                mCalcType = '-';
            if (RbMul.IsChecked == true)
                mCalcType = '*';
            if (RbDiv.IsChecked == true)
                mCalcType = '/';
            if (!double.TryParse(TbNumber.Text, out mInputVal)) {
                MessageBox.Show("数値に変換できない文字が含まれています");
            } else {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
