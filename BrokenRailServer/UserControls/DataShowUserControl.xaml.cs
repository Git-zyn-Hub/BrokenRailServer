using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for DataShowUserControl.xaml
    /// </summary>
    public partial class DataShowUserControl : UserControl
    {
        private bool _stopScroll = false;
        public DataShowUserControl()
        {
            InitializeComponent();
        }

        public void AddShowData(string data, DataLevel dataLevel)
        {
            try
            {
                string nowTime = System.DateTime.Now.ToString("HH:mm:ss.fff");
                TextBox txtData = new TextBox();
                txtData.BorderBrush = null;
                txtData.BorderThickness = new Thickness(0);
                txtData.IsReadOnly = true;
                txtData.TextWrapping = TextWrapping.Wrap;
                switch (dataLevel)
                {
                    case DataLevel.Default:
                        txtData.Foreground = new SolidColorBrush(Colors.Black);
                        break;
                    case DataLevel.Normal:
                        txtData.Foreground = new SolidColorBrush(Colors.Green);
                        break;
                    case DataLevel.Warning:
                        txtData.Foreground = new SolidColorBrush(Colors.Orange);
                        //txtData.FontWeight = FontWeights.Bold;
                        break;
                    case DataLevel.Error:
                        txtData.Foreground = new SolidColorBrush(Colors.Red);
                        break;
                    case DataLevel.Timeout:
                        txtData.Foreground = new SolidColorBrush(Colors.Gray);
                        break;
                    case DataLevel.ContinuousInterference:
                        txtData.Foreground = new SolidColorBrush(Colors.LightBlue);
                        break;
                    default:
                        txtData.Foreground = new SolidColorBrush(Colors.Black);
                        break;
                }
                txtData.Text = nowTime + " -> " + data;
                this.stpContainer.Children.Add(txtData);
                //this.scrollViewer.Focus();
                ScrollControl();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void ClearContainer()
        {
            this.stpContainer.Children.Clear();
        }

        private void ScrollControl()
        {
            if (!_stopScroll)
            {
                //自动滚动到底部
                this.scrollViewer.ScrollToEnd();
            }
        }
        private void tbtnPin_Checked(object sender, RoutedEventArgs e)
        {
            _stopScroll = true;
        }

        private void tbtnPin_Unchecked(object sender, RoutedEventArgs e)
        {
            _stopScroll = false;
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(0);
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            ToggleButton b = sender as ToggleButton;
            if (b != null)
            {
                b.BorderThickness = new Thickness(1);
            }
        }
    }

    public enum DataLevel
    {
        Default,
        Normal,
        Warning,
        Error,
        Timeout,
        ContinuousInterference
    }
}
