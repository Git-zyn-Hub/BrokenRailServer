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
    /// Interaction logic for PointConfigInfoWindow.xaml
    /// </summary>
    public partial class PointConfigInfoWindow : Window
    {
        private int _terminalNo;
        private int _neighbourSmallSecondary;
        private int _neighbourSmall;
        private int _neighbourBig;
        private int _neighbourBigSecondary;
        private bool _flashIsValid;

        public PointConfigInfoWindow(int terminalNo, int neighbourSmallSecondary, int neighbourSmall, int neighbourBig, int neighbourBigSecondary, bool flashIsValid)
        {
            InitializeComponent();
            this._terminalNo = terminalNo;
            this._neighbourSmallSecondary = neighbourSmallSecondary;
            this._neighbourSmall = neighbourSmall;
            this._neighbourBig = neighbourBig;
            this._neighbourBigSecondary = neighbourBigSecondary;
            this._flashIsValid = flashIsValid;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.lblTerminalNo.Content = _terminalNo.ToString() + "号终端";
            this.lblNeighbourSmallSecondary.Content = _neighbourSmallSecondary.ToString();
            this.lblNeighbourSmall.Content = _neighbourSmall.ToString();
            this.lblNeighbourBig.Content = _neighbourBig.ToString();
            this.lblNeighbourBigSecondary.Content = _neighbourBigSecondary.ToString();
            this.lblFlashIsValid.Content = _flashIsValid ? "有效" : "无效";
            this.lblFlashIsValid.Background = _flashIsValid ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red);
        }
    }
}
