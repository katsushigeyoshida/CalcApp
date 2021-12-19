using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfLib;

namespace CalcApp
{
    /// <summary>
    /// Fractal.xaml の相互作用ロジック
    /// 
    /// Fractal.xaml の相互作用ロジック
    /// やさしいフラクタル(工学社 工学選書10)
    /// に掲載されているフラクタル図形を表示
    /// </summary>
    public partial class Fractal : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;
        private double mWindowXmargine = 16;
        private double mWindowYmargine = 40;
        private double mListBoxHeight = 30;
        private WindowState mWindowState = WindowState.Normal;

        private Random r = new Random(1000);
        private double mRotateX = 0;
        private double mRotateY = 0;
        private double mRotateZ = 0;

        private int mFractalType = 0;
        private int mOrd = 10;
        private int[] mDefaultOrd = { 10, 10, 11, 7, 7, 6, 4, 6, 5, 5, 3 };
        private YTurtle turtle;

        public Fractal()
        {
            InitializeComponent();

            mWindowWidth = WindowForm.Width - mWindowXmargine;
            mWindowHeight = WindowForm.Height - mWindowYmargine;
            mListBoxHeight = comboBox.Height;
            comboBox.Items.Add("TREE");
            comboBox.Items.Add("BAOBAB");
            comboBox.Items.Add("FLORA ふたまたの木 (3Dフラクタル)");
            comboBox.Items.Add("FOLIA ミツマタの木 (3Dフラクタル)");
            comboBox.Items.Add("STERA 4次元のふたまたの木 (3Dフラクタル)");
            comboBox.Items.Add("HILBELT ヒルベルト");
            comboBox.Items.Add("CUBIC 8分岐の空間重点曲線 (3Dフラクタル)");
            comboBox.Items.Add("MAYA 4分岐の空間重点曲線");
            comboBox.Items.Add("TETRA MAYAの3次元拡張版(3Dフラクタル)");
            comboBox.Items.Add("PYRAMID MAYAの3次元拡張版(3Dフラクタル)");
            comboBox.Items.Add("RIEUL ペアノ曲線");

            for (int i = 1; i < 15; i++)
                ordComboBox.Items.Add(i.ToString());

            turtle = new YTurtle(canvas, mWindowWidth, mWindowHeight - mListBoxHeight);
            fractal(mFractalType);
            //image.AddVisual(turtle);
            //this.AddVisualChild(turtle);
        }

        /// <summary>
        /// Windowサイズ変更時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_LayoutUpdated(object sender, EventArgs e)
        {
            //  最大化時の処理
            if (this.WindowState != mWindowState &&
                this.WindowState == WindowState.Maximized) {
                double WindowWidth = System.Windows.SystemParameters.WorkArea.Width;
                double WindowHeight = System.Windows.SystemParameters.WorkArea.Height;
                double dx = WindowWidth - mWindowWidth;
                double dy = WindowHeight - mWindowHeight;
                turtle.setWindowSize(mWindowWidth + dx, mWindowHeight + dy
                    - mListBoxHeight - mWindowYmargine);
                fractal(mFractalType);
            } else if (this.WindowState != mWindowState ||
                (mWindowWidth != (WindowForm.Width - mWindowXmargine) ||
                mWindowHeight != (WindowForm.Height - mWindowYmargine))) {
                mWindowWidth = WindowForm.Width - mWindowXmargine;
                mWindowHeight = WindowForm.Height - mWindowYmargine;
                turtle.setWindowSize(mWindowWidth, mWindowHeight - mListBoxHeight);
                fractal(mFractalType);
            }
            mWindowState = this.WindowState;
        }

        private void WindowForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        /// <summary>
        /// フラクタル図形の選択処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mFractalType = comboBox.SelectedIndex;
            mRotateX = 0;
            mRotateY = 0;
            mRotateZ = 0;
            if (mFractalType < mDefaultOrd.Length)
                ordComboBox.SelectedIndex = mDefaultOrd[mFractalType] - 1;
            else
                ordComboBox.SelectedIndex = 5;
            mOrd = ordComboBox.SelectedIndex;
            fractal(mFractalType);
        }

        /// <summary>
        /// フラクタクル図形の次数を設定する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrdComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mOrd = ordComboBox.SelectedIndex;
            fractal(mFractalType);
        }

        /// <summary>
        /// [?]ボタン ヘルプファイルを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpBt_Click(object sender, RoutedEventArgs e)
        {
            HelpView help = new HelpView();
            help.mHelpText = HelpText.mFractalHelp;
            help.Show();
        }

        /// <summary>
        /// 図形部分をマウスで押した時の処理
        /// 3D図形を回転させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (0 <= comboBox.SelectedIndex) {
                Point pt = e.GetPosition(this);
                //  マウスのクリック位置で上下左右の回転を行う Z軸の回転ができない
                //if (mWindowWidth / 2 < pt.X) {
                //    if (pt.Y > (pt.X * mWindowHeight / mWindowWidth))
                //        mRotateX += 4;
                //    else if (pt.Y < (mWindowHeight - pt.X * mWindowHeight / mWindowWidth))
                //        mRotateX -= 4;
                //    else
                //        mRotateY += 4;
                //} else {
                //    if (pt.Y < pt.X * mWindowHeight / mWindowWidth)
                //        mRotateX -= 4;
                //    else if (pt.Y > (mWindowHeight - pt.X * mWindowHeight / mWindowWidth))
                //        mRotateX += 4;
                //    else
                //        mRotateY -= 4;
                //}
                //  ボタンの左・中・右でX・Z・Yの回転を行う
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
                    mRotateY += 4;
                } else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed) {
                    mRotateZ += 4;
                } else if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed) {
                    mRotateX += 4;
                }
                mRotateX = mRotateX % 360;
                mRotateY = mRotateY % 360;
                mRotateZ = mRotateZ % 360;

                fractal(mFractalType);
            }
        }

        /// <summary>
        /// フラクタル図形の表示
        /// </summary>
        /// <param name="fractal">図形の種類</param>
        public void fractal(int fractal)
        {
            switch (fractal) {
                case 0:
                    Tree tree = new Tree(turtle);
                    tree.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 1:
                    Baobab baobab = new Baobab(turtle);
                    baobab.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 2:
                    Flora flora = new Flora(turtle);
                    flora.display(0, mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 3:
                    Flora flora4 = new Flora(turtle);
                    flora4.display(4, mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 4:
                    Stera stera = new Stera(turtle);
                    stera.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 5:
                    Hilbert hilbert = new Hilbert(turtle);
                    hilbert.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 6:
                    Cubic cubic = new Cubic(turtle);
                    cubic.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 7:
                    Maya maya = new Maya(turtle);
                    maya.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 8:
                    //Tetra();
                    Tetra tetra = new Tetra(turtle);
                    tetra.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 9:
                    //PYRAMID();
                    Pyramid pyramid = new Pyramid(turtle);
                    pyramid.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
                case 10:
                    //RIEUL();
                    Rieul rieul = new Rieul(turtle);
                    rieul.display(mOrd, mRotateX, mRotateY, mRotateZ);
                    break;
            }
        }
    }

    /// <summary>
    /// 0.フラクタクル図形TREE
    /// </summary>
    class Tree
    {
        private YTurtle mTurtle;

        public Tree(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double length = 110;            //  最初の枝の長さ
            double ratio = 0.65;            //  枝が先に行くにつれて短くなっていく比(0～1)
            double angle = 25;              //  枝分かれの角度(度)

            mTurtle.graphicstart();

            mTurtle.pmoveto(320, 360);
            mTurtle.right(90);
            stem(ord, length, ratio, angle);

            //mTurtle.close();
        }

        private void stem(int ord, double scale, double ratio, double angle)
        {
            double xd = mTurtle.getX();
            double yd = mTurtle.getY();

            setColor(ord);
            mTurtle.move(scale);
            if (0 < ord) {
                mTurtle.left(angle);
                stem(ord - 1, scale * ratio, ratio, angle);
                mTurtle.right(2 * angle);
                stem(ord - 1, scale * ratio, ratio, angle);
                mTurtle.left(angle);
            }
            mTurtle.pmoveto(xd, yd);
        }

        private void setColor(int ord)
        {
            if (1 < ord)
                mTurtle.setColor(Brushes.Brown);
            else
                mTurtle.setColor(Brushes.Green);
        }
    }

    /// <summary>
    /// 1.フラクタクル図形 BAOBAB
    /// </summary>
    class Baobab
    {
        private YTurtle mTurtle;

        public Baobab(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double th0 = 25;
            double th1 = 1.7;
            double ratio = 0.65;
            double length = 110;
            mTurtle.graphicstart();
            //  幹の太さ
            double r = treeRadius(ord - 1, length, ratio, th1 * Math.PI / 180d, th0 * Math.PI / 180d);
            mTurtle.right(90);
            mTurtle.pmoveto(320 + r, 399);
            stem2(ord, length, ratio, th0, th1);

            //mTurtle.close();
        }

        private void stem2(int ord, double scale, double ratio, double th0, double th1)
        {
            if (0 < ord) {
                mTurtle.right(th1);
                setColor(ord);
                mTurtle.move(scale);
                mTurtle.left(th0 + th1);
                stem2(ord - 1, scale * ratio, ratio, th0, th1);
                mTurtle.right(2 * th0);
                stem2(ord - 1, scale * ratio, ratio, th0, th1);
                mTurtle.left(th0 + th1);
                setColor(ord);
                mTurtle.move(-scale);
                mTurtle.right(th1);
            }
        }

        private void setColor(int ord)
        {
            if (1 < ord)
                mTurtle.setColor(Brushes.Brown);
            else
                mTurtle.setColor(Brushes.Green);
        }

        /// <summary>
        /// 1.BAOBABの木の半径を求める
        /// </summary>
        /// <param name="ord">次数　0～</param>
        /// <param name="l">最初の長さ</param>
        /// <param name="ratio">次の長さの比</param>
        /// <param name="th0">幹の角度</param>
        /// <param name="th1">枝の角度</param>
        /// <returns></returns>
        public double treeRadius(int ord, double l, double ratio, double th0, double th1)
        {
            if (ord <= 0) {
                return l * Math.Sin(th0);
            } else {
                return l * Math.Sin(th0) + 2d * treeRadius(ord - 1, l * ratio, ratio, th0, th1) * Math.Cos(th1);
            }
        }
    }

    /// <summary>
    /// 2.FLORA  三次元のふたまたの木  3.FOLIA{FLORA(4)}  三次元のミツマタの木
    /// </summary>
    class Flora
    {
        private YTurtle mTurtle;
        private Random mRandom = new Random(1000);

        private double stemratio = 0.65;    //  枝が先に行くにつれて短くなる比
        private double stemangle = 30;      //  枝分かれの角度
        private double kratio = 5;          //  花の大きさを決める
        private int generatemax = 30;       //  花が咲く確率の逆数、大きいほど確率が低い

        public Flora(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int type, int ord, double rotateX, double rotateY, double rotateZ)
        {
            double turn0 = 40;      //  図形全体が回転する角度
            double turn1 = 135;     //  図形全体が回転する角度
            double down = -250;     //  図形を画面の下の方に移動させる距離
            double length = 140;    //  最初の枝の長さ
            if (type == 4) {        //  FOLIA(ミツマタの木の係数)
                stemratio = 0.52;
                stemangle = 28;
                kratio = 2;
                generatemax = 10;
                length = 200;
            } else {                //  FLORA(ふたまたの木の係数)
                stemratio = 0.65;
                stemangle = 30;
                kratio = 5;
                generatemax = 30;
            }

            mTurtle.world(3, 3);
            mTurtle.graphicstart();

            mTurtle.turn(0, 1, 90 + rotateZ, ref mTurtle.Base);      //  Z軸
            mTurtle.turn(0, 2, turn0 + rotateX, ref mTurtle.Base);   //  X軸
            mTurtle.turn(1, 2, turn1 + rotateY, ref mTurtle.Base);   //  Y軸
            mTurtle.mov(0, down, mTurtle.Base, ref mTurtle.Pos);
            mTurtle.MPOS();
            if (type == 4)
                stem4(ord, length, mTurtle.Base, mTurtle.Pos);
            else
                stem3(ord, length, mTurtle.Base, mTurtle.Pos);

            //mTurtle.close();
        }

        /// <summary>
        /// 2.FLORAの再帰処理
        /// </summary>
        /// <param name="ord">次数</param>
        /// <param name="size">枝の長さ</param>
        /// <param name="b">変換マトリックス</param>
        /// <param name="p">3Dベクタ</param>
        private void stem3(int ord, double size, matrix b, vector p)
        {
            vector pc = new vector(p);
            matrix bc = new matrix(b);
            if (1 < ord)
                mTurtle.setColor(Brushes.Brown);
            else
                mTurtle.setColor(Brushes.Green);
            mTurtle.MPOS();
            mTurtle.mov(0, size, b, ref p);
            mTurtle.DPOS();

            if (0 == ord) {
                flor(size * kratio, b, p);
            } else {
                mTurtle.turn(1, 2, 90, ref b);
                mTurtle.turn(0, 1, stemangle, ref b);
                stem3(ord - 1, size * stemratio, b, p);
                mTurtle.turn(1, 0, 2 * stemangle, ref b);
                stem3(ord - 1, size * stemratio, b, p);
            }
            p.set(pc);
            b.set(bc);
        }

        /// <summary>
        /// 2.Floraの花の作成
        /// </summary>
        /// <param name="size">花のサイズ</param>
        /// <param name="b">変換マトリックス</param>
        /// <param name="p">3Dベクタ</param>
        private void flor(double size, matrix b, vector p)
        {
            if (0 == mRandom.Next(generatemax)) {
                mTurtle.setColor(Brushes.Red);
                for (int i = 0; i < 5; i++) {
                    mTurtle.turn(1, 2, 72, ref b);
                    petel(size, b, p);
                }
            }
        }

        /// <summary>
        /// 2.Floraの花びらの作成
        /// </summary>
        /// <param name="size">花のサイズ</param>
        /// <param name="b">変換マトリックス</param>
        /// <param name="p">3Dベクタ</param>
        private void petel(double size, matrix b, vector p)
        {
            mTurtle.turn(0, 1, 60, ref b);
            mTurtle.turn(0, 2, 10, ref b);
            mTurtle.mov(0, size, b, ref p);
            mTurtle.DPOS();
            mTurtle.turn(0, 2, 105, ref b);
            mTurtle.mov(0, size * 0.5176380902, b, ref p);
            mTurtle.DPOS();
            mTurtle.turn(0, 2, 105, ref b);
            mTurtle.mov(0, size, b, ref p);
            mTurtle.DPOS();
        }

        /// <summary>
        /// 3.FOLIAの再帰処理
        /// </summary>
        /// <param name="ord">次数</param>
        /// <param name="size">枝の長さ</param>
        /// <param name="b">変換マトリックス</param>
        /// <param name="p">3Dベクタ</param>
        private void stem4(int ord, double size, matrix b, vector p)
        {
            vector pc = new vector(p);
            matrix bc = new matrix(b);

            mTurtle.setColor(Brushes.Brown);
            mTurtle.MPOS();
            mTurtle.mov(0, size, b, ref p);
            mTurtle.DPOS();
            if (0 == ord)
                leaf(size * kratio, b, p);
            else {
                for (int i = 0; i < 3; i++) {
                    mTurtle.turn(1, 2, 120, ref b);
                    mTurtle.turn(0, 1, stemangle, ref b);
                    stem4(ord - 1, size * stemratio, b, p);
                    mTurtle.turn(1, 0, stemangle, ref b);
                }
            }
            p.set(pc);
            b.set(bc);
        }

        /// <summary>
        /// 3.FOLIAの葉の作成
        /// </summary>
        /// <param name="size">葉のサイズ</param>
        /// <param name="b">変換マトリックス</param>
        /// <param name="p">3Dベクタ</param>
        private void leaf(double size, matrix b, vector p)
        {
            if (0 == mRandom.Next(generatemax)) {
                mTurtle.setColor(Brushes.Green);
                mTurtle.turn(1, 2, 90, ref b);
                mTurtle.turn(1, 0, 10, ref b);
                mTurtle.mov(0, size, b, ref p);
                mTurtle.DPOS();
                mTurtle.turn(0, 1, 20, ref b);
                mTurtle.mov(0, size, b, ref p);
                mTurtle.DPOS();
                mTurtle.turn(1, 0, 20, ref b);
                mTurtle.mov(0, -size, b, ref p);
                mTurtle.DPOS();
                mTurtle.turn(0, 1, 20, ref b);
                mTurtle.mov(0, -size, b, ref p);
                mTurtle.DPOS();
            }
        }

    }

    /// <summary>
    /// 4.STERA 4次元のふたまたの木
    /// </summary>
    class Stera
    {
        private YTurtle mTurtle;
        private Random mRandom = new Random(1000);

        public Stera(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double size = 100;
            mTurtle.world(3, 4);
            mTurtle.graphicstart();

            mTurtle.turn(0, 1, 55 + rotateZ, ref mTurtle.Base);  //  Z軸
            mTurtle.turn(0, 3, 90, ref mTurtle.Base);
            mTurtle.turn(1, 2, 30 + rotateY, ref mTurtle.Base);  //  Y軸
            mTurtle.turn(2, 0, rotateX, ref mTurtle.Base);       //  X軸
            mTurtle.turn(1, 3, 30, ref mTurtle.Base);
            mTurtle.turn(2, 3, 110, ref mTurtle.Base);
            stem(ord, size, mTurtle.Base, mTurtle.Pos);

            //mTurtle.close();
        }

        private void stem(int ord, double size, matrix b, vector p)
        {
            vector pc = new vector(p);
            matrix bc = new matrix(b);
            double RATIO = 0.8;

            setColor(ord);
            mTurtle.MPOS();
            mTurtle.mov(0, size, b, ref p);
            mTurtle.DPOS();
            if (0 == ord) {
                stel(size * 0.5, b, p);
            } else {
                mTurtle.turn(0, 1 + ord % 3, 20, ref b);
                stem(ord - 1, size * RATIO, b, p);
                mTurtle.turn(1 + ord % 3, 0, 40, ref b);
                stem(ord - 1, size * RATIO, b, p);
            }
            p.set(pc);
            b.set(bc);
        }

        private void stel(double size, matrix b, vector p)
        {
            mTurtle.turn(1, 2, mRandom.Next(360), ref b);
            mTurtle.turn(1, 3, mRandom.Next(360), ref b);
            mTurtle.turn(2, 3, mRandom.Next(360), ref b);
            mTurtle.turn(0, 1, 18, ref b);
            for (int i = 0; i < 5; i++) {
                mTurtle.mov(0, size, b, ref p);
                mTurtle.DPOS();
                mTurtle.turn(0, 1, 72, ref b);
                mTurtle.mov(0, size, b, ref p);
                mTurtle.DPOS();
                mTurtle.turn(1, 0, 144, ref b);
            }
        }

        private void setColor(int ord)
        {
            if (1 < ord)
                mTurtle.setColor(Brushes.Brown);
            else
                mTurtle.setColor(Brushes.Green);
        }
    }

    /// <summary>
    /// 5.HILBERT ヒルベルト曲線
    /// </summary>
    class Hilbert
    {
        private YTurtle mTurtle;

        public Hilbert(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double size = 0;
            mTurtle.graphicstart();
            mTurtle.pmoveto(160, 360);

            for (int i = 0; i < ord; i++, size = 1 + 2 * size) ;
            size = 320 / size;
            recurse(ord, -90, size);

            //mTurtle.close();
        }

        private void recurse(int ord, double angle, double size)
        {
            if (0 < ord) {
                mTurtle.left(angle);
                recurse(ord - 1, -angle, size);
                cmove(ord, size);
                mTurtle.right(angle);
                recurse(ord - 1, angle, size);
                cmove(ord, size);
                recurse(ord - 1, angle, size);
                mTurtle.right(angle);
                cmove(ord, size);
                recurse(ord - 1, -angle, size);
                mTurtle.left(angle);
            }
        }

        private void cmove(int ord, double size)
        {
            if (0 == ord)
                mTurtle.setColor(Brushes.Black);
            else
                mTurtle.setColor(Brushes.Green);

            mTurtle.move(size);
        }
    }

    /// <summary>
    /// 6.CUBIC 8分岐の空間重点曲線(ヒルベルト曲線の三次元化)
    /// </summary>
    class Cubic
    {
        private YTurtle mTurtle;

        public Cubic(YTurtle turtle)
        {
            this.mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double size = 250;

            mTurtle.world(3, 3);
            mTurtle.graphicstart();

            mTurtle.turn(0, 2, 5 + rotateY, ref mTurtle.Base);       //  Y軸
            mTurtle.turn(2, 1, -2.5 + rotateX, ref mTurtle.Base);    //  X軸
            mTurtle.turn(1, 0, rotateZ, ref mTurtle.Base);           //  Z軸
            int i, j;
            for (i = 0; i < 3; mTurtle.mov(i, -size / 2, mTurtle.Base, ref mTurtle.Pos), i++) ;
            mTurtle.MPOS();
            for (i = 0, j = 0; i < ord; j = j * 2 + 1, i++) ;
            size /= j;
            recurse(ord, 1, 2, 3, size);

            //mTurtle.close();

        }

        private void recurse(int ord, int a, int b, int c, double size)
        {
            if (0 < ord) {
                recurse(ord - 1, b, a, c, size); link(ord, a, size);
                recurse(ord - 1, b, c, a, size); link(ord, c, size);
                recurse(ord - 1, b, c, a, size); link(ord, -a, size);
                recurse(ord - 1, -a, b, -c, size); link(ord, b, size);
                recurse(ord - 1, -a, b, -c, size); link(ord, a, size);
                recurse(ord - 1, -b, -c, a, size); link(ord, -c, size);
                recurse(ord - 1, -b, -c, a, size); link(ord, -a, size);
                recurse(ord - 1, -b, -a, c, size);
            }
        }

        private void link(int ord, int dir, double size)
        {
            if (0 > dir)
                mTurtle.mov(-dir - 1, -size, mTurtle.Base, ref mTurtle.Pos);
            else
                mTurtle.mov(dir - 1, size, mTurtle.Base, ref mTurtle.Pos);
            mTurtle.setColor(Brushes.Black);
            mTurtle.DPOS();
        }

    }

    /// <summary>
    /// 7.MAYA 4分岐の空間重点曲線
    /// </summary>
    class Maya
    {
        private YTurtle mTurtle;

        private Random mRandom = new Random(1000);
        private int[] dx = { -1, -1, 1, 1 };
        private int[] dy = { -1, 1, 1, -1 };
        private int color = 2;

        public Maya(YTurtle turtle)
        {
            this.mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double size = 0;
            double x = 140;
            double y = 380;

            mTurtle.graphicstart();

            mTurtle.pmoveto(x, y);
            for (int i = 0; i < ord; i++, size = 1 + 2 * size) ;
            size = 180 / size;

            switch (mRandom.Next(3)) {
                case 0:
                    recurse(ord, 0, 1, 2, 3, size);
                    break;
                case 1:
                    recurse(ord, 0, 2, 1, 3, size);
                    break;
                case 2:
                    recurse(ord, 0, 1, 3, 2, size);
                    break;

            }

            //mTurtle.close();

        }

        private void recurse(int ord, int a, int b, int c, int d, double size)
        {
            if (0 < ord) {
                if (0 == mRandom.Next(2))
                    recurse(ord - 1, a, d, c, b, size);
                else
                    recurse(ord - 1, a, c, d, b, size);
                link(ord, a, b, size);

                if (0 == mRandom.Next(2))
                    recurse(ord - 1, a, d, b, c, size);
                else
                    recurse(ord - 1, a, b, d, c, size);
                link(ord, b, c, size);

                if (0 == mRandom.Next(2))
                    recurse(ord - 1, b, c, a, d, size);
                else
                    recurse(ord - 1, b, a, c, d, size);
                link(ord, c, d, size);

                if (0 == mRandom.Next(2))
                    recurse(ord - 1, c, b, a, d, size);
                else
                    recurse(ord - 1, c, a, b, d, size);
            }
        }

        private void link(int ord, int from, int to, double size)
        {
            if (0 == color)
                mTurtle.setColor(mTurtle.getColor(ord));
            else
                mTurtle.setColor(mTurtle.getColor(color));

            double rdx = size * (dx[to] - dx[from]);
            double rdy = size * (dy[to] - dy[from]);
            mTurtle.rlineto(rdx, -rdy);
        }
    }


    /// <summary>
    /// 8.TETRA MAYAの3次元版
    /// </summary>
    class Tetra
    {
        YTurtle mTurtle;


        public Tetra(YTurtle turtle)
        {
            this.mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double size = 120;
            int color = 2;
            int[,] array = { { 1, -1, -1, 1 }, { 1, 1, -1, -1 }, { 1, -1, 1, -1 } };    //  int[3,4]
            double[,] dx = new double[3, 4];
            double th1 = 100;
            double th2 = 20;
            double th3 = 30;

            mTurtle.world(3, 3);
            mTurtle.graphicstart();

            mTurtle.turn(0, 1, th1 + rotateZ, ref mTurtle.Base);    //  Z軸
            mTurtle.turn(0, 2, th2 + rotateY, ref mTurtle.Base);    //  Y軸
            mTurtle.turn(1, 2, th3 + rotateX, ref mTurtle.Base);    //  X軸
            makearraydx(4, ref dx, array);
            for (int i = 0; i < 3; mTurtle.Pos.e[i] += size * dx[i, 0], i++) {
                mTurtle.MPOS();
            }
            int j = 0;
            for (int i = 0; i < ord; i++, j = 1 + 2 * j) ;
            size /= j;
            recurse(ord, 0, 1, 2, 3, size, dx, color);

            //mTurtle.close();

        }

        private void makearraydx(int a, ref double[,] dx, int[,] array)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < a; j++) {
                    dx[i, j] = 0;
                    for (int k = 0; k < 3; k++)
                        dx[i, j] += mTurtle.Base.e[i, k] * array[k, j];
                }
        }

        private void recurse(int ord, int a, int b, int c, int d, double size, double[,] dx, int color)
        {
            if (0 < ord) {
                recurse(ord - 1, a, c, d, b, size, dx, color); link(ord, a, b, size, dx, color);
                recurse(ord - 1, a, b, d, c, size, dx, color); link(ord, b, c, size, dx, color);
                recurse(ord - 1, b, a, c, d, size, dx, color); link(ord, c, d, size, dx, color);
                recurse(ord - 1, c, a, b, d, size, dx, color);
            }
        }

        private void link(int ord, int from, int to, double size, double[,] dx, int color)
        {
            for (int i = 0; i < 3; i++)
                mTurtle.Pos.e[i] += size * (dx[i, to] - dx[i, from]);
            //if (0 == color)
            //    turtle.setColor(turtle.getColor(ord));
            //else
            mTurtle.setColor(Brushes.Black);
            mTurtle.DPOS();
        }

    }

    /// <summary>
    /// 9.PYRAMID MAYAのバリエーション
    /// ピラミッド形の内部を一筆書きにする5分岐の再帰曲線
    /// </summary>
    class Pyramid
    {
        YTurtle mTurtle;


        public Pyramid(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        /// <summary>
        /// 表示のメインルーチン
        /// </summary>
        /// <param name="ord">次数</param>
        /// <param name="rotateX">X軸回転</param>
        /// <param name="rotateY">Y軸回転</param>
        /// <param name="rotateZ">Z軸回転</param>
        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            double[,] array = { { 1, 1, 0, -1, -1 }, { -1, 1, 0, 1, -1 }, { 0, 0, Math.Sqrt(2d), 0, 0 } };
            double[,] dx = new double[3, 5];
            double size = 150;
            int color = 2;
            double th1 = 0;         //  Z軸
            double th2 = 40.0;      //  Y軸
            double th3 = 87.0;      //  X軸

            mTurtle.world(3, 3);
            mTurtle.graphicstart();

            mTurtle.turn(2, 1, th3 + rotateX, ref mTurtle.Base);    //  X軸
            mTurtle.turn(0, 2, th2 + rotateY, ref mTurtle.Base);    //  Y軸
            mTurtle.turn(0, 1, th1 + rotateZ, ref mTurtle.Base);    //  Z軸
            makearraydx(5, ref dx, array);
            mTurtle.mov(0, size, mTurtle.Base, ref mTurtle.Pos);
            mTurtle.mov(1, -size, mTurtle.Base, ref mTurtle.Pos);
            mTurtle.mov(2, size * -0.5, mTurtle.Base, ref mTurtle.Pos);
            mTurtle.MPOS();
            int j = 0;
            for (int i = 0; i < ord; i++, j = 1 + 2 * j) ;
            size /= j;
            recurse(ord, 0, 1, 2, 3, 4, size, dx, color);

            //mTurtle.close();

        }

        private void recurse(int ord, int a, int b, int c, int d, int e, double size, double[,] dx, int color)
        {
            if (0 < ord) {
                recurse(ord - 1, a, e, c, d, b, size, dx, color); link(ord, a, b, size, dx, color);
                recurse(ord - 1, a, e, b, d, c, size, dx, color); link(ord, b, c, size, dx, color);
                recurse(ord - 1, b, a, c, e, d, size, dx, color); link(ord, c, d, size, dx, color);
                recurse(ord - 1, c, d, b, a, e, size, dx, color); link(ord, d, e, size, dx, color);
                recurse(ord - 1, d, b, c, a, e, size, dx, color);
            }
        }

        private void makearraydx(int a, ref double[,] dx, double[,] array)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < a; j++) {
                    dx[i, j] = 0;
                    for (int k = 0; k < 3; k++)
                        dx[i, j] += mTurtle.Base.e[i, k] * array[k, j];
                }
        }

        private void link(int ord, int from, int to, double size, double[,] dx, int color)
        {
            for (int i = 0; i < 3; i++)
                mTurtle.Pos.e[i] += size * (dx[i, to] - dx[i, from]);
            //if (0 == color)
            //    turtle.setColor(turtle.getColor(ord));
            //else
            mTurtle.setColor(Brushes.Black);
            mTurtle.DPOS();
        }
    }

    /// <summary>
    /// 10.ペアノ曲線
    /// 正方形を埋め尽くす9分岐の空間充填曲線
    /// </summary>
    class Rieul
    {
        YTurtle mTurtle;

        double krat = 0;
        double hrat = 1;
        int color = 2;


        public Rieul(YTurtle turtle)
        {
            mTurtle = turtle;
        }

        public void display(int ord, double rotateX, double rotateY, double rotateZ)
        {
            mTurtle.graphicstart();

            double x = 140;
            double y = 380;
            mTurtle.pmoveto(x, y);
            krat = (rotateY / 4) % 2;
            hrat = (rotateZ / 20) % 5 - 2.5;
            double size = krat;
            for (int i = 0; i < ord; i++, size = 3 * size + 2 * hrat) ;
            size = 360 / size;
            krat *= size;
            hrat *= size;
            recurse(ord, 1, 1);

            //mTurtle.close();
        }

        private void recurse(int ord, int a, int b)
        {
            if (0 == ord)
                link(0, krat, a, b);
            else {
                proc(ord, a, b, 0, b);
                proc(ord, -a, b, 0, b);
                proc(ord, a, b, a, 0);
                proc(ord, a, -b, 0, -b);
                proc(ord, -a, -b, 0, -b);
                proc(ord, a, -b, a, 0);
                proc(ord, a, b, 0, b);
                proc(ord, -a, b, 0, b);
                recurse(ord - 1, a, b);
            }
        }

        private void link(int ord, double mag, int a, int b)
        {
            if (0 == color)
                mTurtle.setColor(Brushes.Black);
            else
                mTurtle.setColor(Brushes.Black);
            mTurtle.rlineto(mag * a, -mag * b);
        }

        private void proc(int ord, int a, int b, int c, int d)
        {
            recurse(ord - 1, a, b);
            link(ord, hrat, c, d);
        }
    }
}
