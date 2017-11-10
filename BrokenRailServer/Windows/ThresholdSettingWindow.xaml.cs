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

namespace BrokenRailMonitorViaWiFi.Windows
{
    /// <summary>
    /// Interaction logic for ThresholdSettingWindow.xaml
    /// </summary>
    public partial class ThresholdSettingWindow : Window
    {
        private int _thresholdRail1;
        private int _thresholdRail2;

        public int ThresholdRail1
        {
            get
            {
                return _thresholdRail1;
            }

            set
            {
                _thresholdRail1 = value;
            }
        }

        public int ThresholdRail2
        {
            get
            {
                return _thresholdRail2;
            }

            set
            {
                _thresholdRail2 = value;
            }
        }

        public ThresholdSettingWindow()
        {
            InitializeComponent();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.txtRail1Threshold.Text, out _thresholdRail1))
            {
                if (_thresholdRail1 < 0 || _thresholdRail1 > 255)
                {
                    MessageBox.Show("'铁轨1门限'必须在0到255之间");
                    this.txtRail1Threshold.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'铁轨1门限'必须输入0~255的整数");
                this.txtRail1Threshold.Text = string.Empty;
                return;
            }
            if (int.TryParse(this.txtRail2Threshold.Text, out _thresholdRail2))
            {
                if (_thresholdRail2 < 0 || _thresholdRail2 > 255)
                {
                    MessageBox.Show("'铁轨2门限'必须在0到255之间");
                    this.txtRail2Threshold.Text = string.Empty;
                    return;
                }
            }
            else
            {
                MessageBox.Show("'铁轨2门限'必须输入0~255的整数");
                this.txtRail2Threshold.Text = string.Empty;
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
    }
}
