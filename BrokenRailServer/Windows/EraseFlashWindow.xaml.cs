using BrokenRailServer.UserControls;
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
    /// Interaction logic for EraseFlashWindow.xaml
    /// </summary>
    public partial class EraseFlashWindow : Window, INotifyPropertyChanged
    {
        private int _terminalSmall = 0;
        private int _terminalBig = 254;
        private List<MasterControl> _masterControlList = new List<MasterControl>();
        private bool _findTerminalSmall = false;
        private bool _findTerminalBig = false;
        private int _startSectorNo = 0;
        private int _endSectorNo = 4095;

        public int TerminalSmall
        {
            get
            {
                return _terminalSmall;
            }

            set
            {
                if (_terminalSmall != value)
                {
                    _terminalSmall = value;
                    OnPropertyChanged("TerminalSmall");
                }
            }
        }

        public int TerminalBig
        {
            get
            {
                return _terminalBig;
            }

            set
            {
                if (_terminalBig != value)
                {
                    _terminalBig = value;
                    OnPropertyChanged("TerminalBig");
                }
            }
        }

        public List<MasterControl> MasterControlList
        {
            get
            {
                return _masterControlList;
            }

            set
            {
                _masterControlList = value;
            }
        }

        public int StartSectorNo
        {
            get
            {
                return _startSectorNo;
            }

            set
            {
                _startSectorNo = value;
            }
        }

        public int EndSectorNo
        {
            get
            {
                return _endSectorNo;
            }

            set
            {
                _endSectorNo = value;
            }
        }

        public EraseFlashWindow(List<MasterControl> masterControlList)
        {
            InitializeComponent();
            this._masterControlList = masterControlList;
            if (_masterControlList.Count > 0)
            {
                this.TerminalSmall = _masterControlList[0].TerminalNumber;
                this.TerminalBig = _masterControlList[_masterControlList.Count - 1].TerminalNumber;
            }
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.txtTerminalSmall.Text, out _terminalSmall))
            {
                if (_terminalSmall < 0 || _terminalSmall > 255)
                {
                    MessageBox.Show("'终端号'必须在0到255之间");
                    this.txtTerminalSmall.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'终端号'必须输入0~255的整数");
                this.txtTerminalSmall.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtTerminalBig.Text, out _terminalBig))
            {
                if (_terminalBig < 0 || _terminalBig > 255)
                {
                    MessageBox.Show("'终端号'必须在0到255之间");
                    this.txtTerminalBig.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'终端号'必须输入0~255的整数");
                this.txtTerminalBig.Text = string.Empty;
                return;
            }
            if (_terminalSmall > _terminalBig)
            {
                MessageBox.Show("左边的终端号必须小于等于右边的终端号！");
                this.txtTerminalSmall.Text = string.Empty;
                this.txtTerminalBig.Text = string.Empty;
                return;
            }
            if (this.MasterControlList != null)
            {
                foreach (var item in this.MasterControlList)
                {
                    if (item.TerminalNumber == _terminalSmall)
                    {
                        _findTerminalSmall = true;
                    }
                    if (item.TerminalNumber == _terminalBig)
                    {
                        _findTerminalBig = true;
                        break;
                    }
                }
                if (!_findTerminalSmall)
                {
                    MessageBox.Show("小终端号不在终端列表中，请重新填写！");
                    this.txtTerminalSmall.Text = string.Empty;
                    return;
                }
                if (!_findTerminalBig)
                {
                    MessageBox.Show("大终端号不在终端列表中，请重新填写！");
                    this.txtTerminalBig.Text = string.Empty;
                    return;
                }
            }
            if (int.TryParse(this.txtStartSector.Text, out _startSectorNo))
            {
                if (_startSectorNo < 0 || _startSectorNo > 4095)
                {
                    MessageBox.Show("'起始扇区号'必须在0到4095之间");
                    this.txtStartSector.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'起始扇区号'必须输入0~4095的整数");
                this.txtStartSector.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtEndSector.Text, out _endSectorNo))
            {
                if (_endSectorNo < 0 || _endSectorNo > 4095)
                {
                    MessageBox.Show("'终点扇区号'必须在0到4095之间");
                    this.txtEndSector.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'终点扇区号'必须输入0~4095的整数");
                this.txtEndSector.Text = string.Empty;
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
                this._findTerminalSmall = false;
                this._findTerminalBig = false;
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
        private void txtTerminalSmall_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtTerminalSmall.SelectAll();
        }

        private void txtTerminalBig_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtTerminalBig.SelectAll();
        }

        private void txtStartSector_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtStartSector.SelectAll();
        }

        private void txtEndSector_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txtEndSector.SelectAll();
        }

        private void eraseFlashWin_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtTerminalSmall.Focus();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
