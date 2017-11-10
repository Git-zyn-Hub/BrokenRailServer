using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace BrokenRailMonitorViaWiFi.Windows
{
    /// <summary>
    /// Interaction logic for InitialInfoConfigWindow.xaml
    /// </summary>
    public partial class InitialInfoConfigWindow : Window, INotifyPropertyChanged
    {
        private int _neighbourSmallSecondary;
        private int _neighbourSmall;
        private int _terminalNo;
        private int _neighbourBig;
        private int _neighbourBigSecondary;

        public int NeighbourSmallSecondary
        {
            get
            {
                return _neighbourSmallSecondary;
            }

            set
            {
                if (_neighbourSmallSecondary != value)
                {
                    _neighbourSmallSecondary = value;
                    OnPropertyChanged("NeighbourSmallSecondary");
                }
            }
        }

        public int NeighbourSmall
        {
            get
            {
                return _neighbourSmall;
            }

            set
            {
                if (_neighbourSmall != value)
                {
                    _neighbourSmall = value;
                    OnPropertyChanged("NeighbourSmall");
                }
            }
        }

        public int TerminalNo
        {
            get
            {
                return _terminalNo;
            }

            set
            {
                if (_terminalNo != value)
                {
                    _terminalNo = value;
                    OnPropertyChanged("TerminalNo");
                }
            }
        }

        public int NeighbourBig
        {
            get
            {
                return _neighbourBig;
            }

            set
            {
                if (_neighbourBig != value)
                {
                    _neighbourBig = value;
                    OnPropertyChanged("NeighbourBig");
                }
            }
        }

        public int NeighbourBigSecondary
        {
            get
            {
                return _neighbourBigSecondary;
            }

            set
            {
                if (_neighbourBigSecondary != value)
                {
                    _neighbourBigSecondary = value;
                    OnPropertyChanged("NeighbourBigSecondary");
                }
            }
        }

        public InitialInfoConfigWindow()
        {
            InitializeComponent();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.txtNeighbourSmallSecondary.Text, out _neighbourSmallSecondary))
            {
                if (_neighbourSmallSecondary < 0 || _neighbourSmallSecondary > 255)
                {
                    MessageBox.Show("'次级相邻小终端'必须在0到255之间");
                    this.txtNeighbourSmallSecondary.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'次级相邻小终端'必须输入0~255的整数");
                this.txtNeighbourSmallSecondary.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtNeighbourSmall.Text, out _neighbourSmall))
            {
                if (_neighbourSmall < 0 || _neighbourSmall > 255)
                {
                    MessageBox.Show("'相邻小终端'必须在0到255之间");
                    this.txtNeighbourSmall.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'相邻小终端'必须输入0~255的整数");
                this.txtNeighbourSmall.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtTerminalNo.Text, out _terminalNo))
            {
                if (_terminalNo < 0 || _terminalNo > 255)
                {
                    MessageBox.Show("'本终端'必须在0到255之间");
                    this.txtTerminalNo.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'本终端'必须输入0~255的整数");
                this.txtTerminalNo.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtNeighbourBig.Text, out _neighbourBig))
            {
                if (_neighbourBig < 0 || _neighbourBig > 255)
                {
                    MessageBox.Show("'相邻大终端'必须在0到255之间");
                    this.txtNeighbourBig.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'相邻大终端'必须输入0~255的整数");
                this.txtNeighbourBig.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtNeighbourBigSecondary.Text, out _neighbourBigSecondary))
            {
                if (_neighbourBigSecondary < 0 || _neighbourBigSecondary > 255)
                {
                    MessageBox.Show("'次级相邻大终端'必须在0到255之间");
                    this.txtNeighbourBigSecondary.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'次级相邻大终端'必须输入0~255的整数");
                this.txtNeighbourBigSecondary.Text = string.Empty;
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            /*e.Key != Key.Back && e.Key != Key.Decimal && e.Key != Key.OemPeriod && e.Key != Key.Return
            * Key.Back 退格键
            * Key.Return 回车
            * Key.Tab 键
            */
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Back || e.Key == Key.Return || e.Key == Key.Tab)
            {
                e.Handled = false;
            }
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //屏蔽中文输入和非法字符粘贴输入
            try
            {
                TextBox textBox = sender as TextBox;
                TextChange[] change = new TextChange[e.Changes.Count];
                e.Changes.CopyTo(change, 0);

                int offset = change[0].Offset;
                if (change[0].AddedLength > 0)
                {
                    //这里只做Double类型转换的检测，如果是Int或者其他类型需要改变num的类型，和TryParse前面类型。
                    int num = 0;
                    if (!int.TryParse(textBox.Text, out num))
                    {
                        textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                        textBox.Select(offset, 0);
                    }
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
                throw;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private void txtNeighbourSmallSecondary_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtNeighbourSmallSecondary.SelectAll();
        }

        private void txtNeighbourSmall_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtNeighbourSmall.SelectAll();
        }

        private void txtTerminalNo_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtTerminalNo.SelectAll();
        }

        private void txtNeighbourBig_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtNeighbourBig.SelectAll();
        }

        private void txtNeighbourBigSecondary_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtNeighbourBigSecondary.SelectAll();
        }
    }
}
