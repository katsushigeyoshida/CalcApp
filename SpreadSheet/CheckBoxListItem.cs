

namespace CalcApp
{
    /// <summary>
    /// リストボックスにチェックマークのデータを反映させるためのクラス
    /// </summary>
    public class CheckBoxListItem
    {
        public bool Checked { get; set; }
        public string Text { get; set; }
        public CheckBoxListItem(bool ch, string text)
        {
            Checked = ch;
            Text = text;
        }
    }
}
