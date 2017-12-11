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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BrokenRailMonitorViaWiFi
{
    /// <summary>
    /// Interaction logic for Rail.xaml
    /// </summary>
    public partial class Rail : UserControl
    {
        private readonly int _railNumber;
        private RailStates _railState = RailStates.IsError;
        public int RailNumber
        {
            get
            {
                return _railNumber;
            }
        }

        public RailStates RailState
        {
            get
            {
                return _railState;
            }

            set
            {
                _railState = value;
            }
        }

        public Rail()
        {
            InitializeComponent();
        }
        public Rail(int railNumber)
        {
            InitializeComponent();
            this._railNumber = railNumber;
        }
        public void Error()
        {
            this.RailState = RailStates.IsError;
            this.recRail.Fill = new SolidColorBrush(Colors.Red);
        }

        public void Normal()
        {
            this.RailState = RailStates.IsNormal;
            this.recRail.Fill = new SolidColorBrush(Colors.LightGreen);
        }

        public void Different()
        {
            this.RailState = RailStates.IsDifferent;
            this.recRail.Fill = new SolidColorBrush(Colors.Orange);
        }
        public void Timeout()
        {
            this.RailState = RailStates.IsTimeout;
            this.recRail.Fill = new SolidColorBrush(Colors.Gray);
        }

        public void ContinuousInterference()
        {
            this.RailState = RailStates.IsContinuousInterference;
            this.recRail.Fill = new SolidColorBrush(Colors.LightBlue);
        }
        private void railUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }

    public enum RailStates
    {
        IsError,
        IsNormal,
        IsDifferent,
        IsTimeout,
        IsContinuousInterference
    }
}
